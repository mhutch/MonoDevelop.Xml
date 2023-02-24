// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Completion;

/// <summary>
/// Completion provider for XML Schemas that defers the loading/compiling the schema until the provider is used.
/// </summary>
class LazyXmlSchemaCompletionProvider : LazyXmlCompletionProvider, IXmlCompletionProvider
{
	readonly XmlSchemaLoader loader;

	/// <summary>
	/// Creates completion data from the schema passed in 
	/// via the reader object.
	/// </summary>
	public LazyXmlSchemaCompletionProvider (TextReader reader, ILogger logger, string? baseUri = null) : this (new XmlSchemaLoader (reader, fileName: "", logger, baseUri)) { }

	/// <summary>
	/// Creates the completion data from the specified schema file and uses
	/// the specified baseUri to resolve any referenced schemas.
	/// </summary>
	public LazyXmlSchemaCompletionProvider (string fileName, ILogger logger, string? baseUri = null) : this (new XmlSchemaLoader (fileName, logger, baseUri)) { }

	LazyXmlSchemaCompletionProvider (XmlSchemaLoader loader) => this.loader = loader;

	protected override Task<IXmlCompletionProvider> CreateProviderAsync (CancellationToken token) => Task.Run<IXmlCompletionProvider> (() => XmlSchemaCompletionProvider.Create (loader));
}