// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.IO;
using System.Xml;
using System.Xml.Schema;

using Microsoft.Extensions.Logging;

using MonoDevelop.Xml.Logging;

namespace MonoDevelop.Xml.Editor.Completion;

/// <summary>
/// Represents a schema that may be in several states: NotLoaded, Loaded, Compiled, or Failed.
/// </summary>
partial class XmlSchemaLoader
{
	readonly ILogger logger;
	readonly string? baseUri;
	readonly string fileName;

	XmlSchema? schema;
	XmlSchemaLoaderState state;

	public XmlSchemaLoader (string fileName, ILogger logger, string? baseUri = null)
	{
		this.logger = logger;
		this.baseUri = baseUri;
		this.fileName = fileName;
	}

	/// <summary>
	/// Loads the schema from the TextReader, skipping the NotLoaded state.
	/// </summary>
	public XmlSchemaLoader (TextReader reader, string fileName, ILogger logger, string? baseUri = null) : this (fileName, logger, baseUri)
	{
		LoadSchema (reader);
	}

	public XmlSchemaLoaderState State => state;
	public string Filename => fileName;

	/// <summary>
	/// Returns the loaded or compiled schema. Returns null if in the Failed state.
	/// </summary>
	public XmlSchema? GetLoaded ()
	{
		if (State == XmlSchemaLoaderState.NotLoaded) {
			using var reader = new StreamReader (fileName, true);
			LoadSchema (reader);
		}
		return schema;
	}

	/// <summary>
	/// Returns the compiled schema. Returns null if in the Failed state.
	/// </summary>
	public XmlSchema? GetCompiled ()
	{
		GetLoaded ();

		if (State == XmlSchemaLoaderState.Loaded && schema is not null) {
			CompileSchema (schema, CreateResolver (), CreateValidationHandler ());
			state = XmlSchemaLoaderState.Compiled;
		}
		return schema;
	}

	void LoadSchema (TextReader reader)
	{
		schema = ReadSchema (reader, baseUri, CreateResolver (), CreateValidationHandler ());
		if (schema is null) {
			LogSchemaLoadFailed (logger, fileName);
			state = XmlSchemaLoaderState.Failed;
		} else {
			state = XmlSchemaLoaderState.Loaded;
		}
	}

	LocalOnlyXsdResolver CreateResolver () => new (logger, Filename);
	ValidationEventHandler CreateValidationHandler () => (_, e) => LogSchemaValidationError (logger, Filename, e.Message);

	static XmlSchema? ReadSchema (TextReader reader, string? baseUri, LocalOnlyXsdResolver resolver, ValidationEventHandler validationHandler)
	{
		// The default resolve can cause exceptions loading
		// xhtml1-strict.xsd because of the referenced dtds. It also has the
		// possibility of blocking on referenced remote URIs.
		// Instead we only resolve local xsds.

		using var xmlReader = XmlReader.Create (
			reader,
			new XmlReaderSettings {
				XmlResolver = resolver,
				DtdProcessing = DtdProcessing.Ignore,
				ValidationType = ValidationType.None
			},
			baseUri
		);

		return XmlSchema.Read (xmlReader, validationHandler);
	}

	static void CompileSchema (XmlSchema schema, LocalOnlyXsdResolver resolver, ValidationEventHandler validationHandler)
	{
		//TODO: should we evaluate unresolved imports against other registered schemas?
		//will be messy because we'll have to re-evaluate if any schema is added, removed or changes
		//maybe we should just force users to use schemaLocation in their includes
		var sset = new XmlSchemaSet { XmlResolver = resolver };
		sset.Add (schema);
		sset.ValidationEventHandler += validationHandler;
		sset.Compile ();
	}

	[LoggerMessage (EventId = 1, Level = LogLevel.Warning, Message = "Validation error in schema '{schemaFile}': {validationMessage}")]
	static partial void LogSchemaValidationError (ILogger logger, UserIdentifiableFileName schemaFile, UserIdentifiableString validationMessage);

	[LoggerMessage (EventId = 2, Level = LogLevel.Error, Message = "Failed to load schema '{schemaFile}'")]
	static partial void LogSchemaLoadFailed (ILogger logger, UserIdentifiableFileName schemaFile);
}

enum XmlSchemaLoaderState
{
	NotLoaded,
	Loaded,
	Compiled,
	Failed
}