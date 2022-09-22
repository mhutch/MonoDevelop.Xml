//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#nullable enable

#if NETFRAMEWORK
#nullable disable warnings
#endif

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

using Microsoft.Extensions.Logging;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

namespace MonoDevelop.Xml.Editor.Completion
{
	/// <summary>
	/// Holds the completion (intellisense) data for an xml schema.
	/// </summary>
	class XmlSchemaCompletionProvider : IXmlSchemaCompletionProvider
	{
		readonly XmlSchema schema;

		public static IXmlSchemaCompletionProvider Create (XmlSchemaLoader loader)
		{
			var schema = loader.GetCompiled ();
			if (schema is null) {
				return new EmptyXmlCompletionProvider ();
			}
			return new XmlSchemaCompletionProvider (schema, loader.Filename);
		}

		/// <summary>
		/// Creates completion data from the schema passed in 
		/// via the reader object.
		/// </summary>
		public static IXmlSchemaCompletionProvider Create (TextReader reader, ILogger logger, string? baseUri = null) => Create (new XmlSchemaLoader (reader, fileName: "", logger, baseUri));

		/// <summary>
		/// Creates the completion data from the specified schema file and uses
		/// the specified baseUri to resolve any referenced schemas.
		/// </summary>
		public static IXmlSchemaCompletionProvider Create (string fileName, ILogger logger, string baseUri) => Create (new XmlSchemaLoader (fileName, logger, baseUri));

		XmlSchemaCompletionProvider (XmlSchema compiledSchema, string? fileName)
		{
			schema = compiledSchema;
			FileName = fileName;
			NamespaceUri = schema.TargetNamespace;
		}

		public XmlSchema? Schema => schema;
		public string? FileName { get; }
		public string? NamespaceUri { get; }

		/// <summary>
		/// Converts the filename into a valid Uri.
		/// </summary>
		internal static string GetUri (string fileName)
		{
			return string.IsNullOrEmpty (fileName) ? "" : new Uri (fileName).AbsoluteUri;
		}

		public Task<CompletionContext> GetChildElementCompletionDataAsync (IAsyncCompletionSource source, string tagName, CancellationToken token) => Task.Run (
			() => {
				var list = new XmlSchemaCompletionBuilder (source);
				if (schema.FindElement (tagName) is XmlSchemaElement element) {
					list.AddChildElements (schema, element, "");
				}
				return new CompletionContext (list.GetItems ());
			}, token);

		public Task<CompletionContext> GetAttributeCompletionDataAsync (IAsyncCompletionSource source, string tagName, CancellationToken token) => Task.Run (
			() => {
				var list = new XmlSchemaCompletionBuilder (source);
				if (schema.FindElement (tagName) is XmlSchemaElement element) {
					list.AddAttributes (schema, element);
				}
				return new CompletionContext (list.GetItems ());
			}, token);

		public Task<CompletionContext> GetAttributeValueCompletionDataAsync (IAsyncCompletionSource source, string tagName, string name, CancellationToken token) => Task.Run (
			() => {
				var list = new XmlSchemaCompletionBuilder (source);
				if (schema.FindElement (tagName) is XmlSchemaElement element) {
					list.AddAttributeValues (schema, element, name);
				}
				return new CompletionContext (list.GetItems ());
			}, token);

		/// <summary>
		/// Gets the possible root elements for an xml document using this schema.
		/// </summary>
		public Task<CompletionContext> GetElementCompletionDataAsync (IAsyncCompletionSource source, CancellationToken token) => GetElementCompletionDataAsync (source, "", token);

		/// <summary>
		/// Gets the possible root elements for an xml document using this schema.
		/// </summary>
		public Task<CompletionContext> GetElementCompletionDataAsync (IAsyncCompletionSource source, string namespacePrefix, CancellationToken token) => Task.Run (
			() => {
				var builder = new XmlSchemaCompletionBuilder (source);
				foreach (XmlSchemaElement element in schema.Elements.Values) {
					if (element.Name is string elementName) {
						builder.AddElement (elementName, namespacePrefix, element.Annotation);
					} else {
						// Do not add reference element.
					}
				}
				return new CompletionContext (builder.GetItems ());
			}, token);

		/// <summary>
		/// Gets the attribute completion data for the xml element that exists
		/// at the end of the specified path.
		/// </summary>
		public Task<CompletionContext> GetAttributeCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, CancellationToken token) => Task.Run (
			() => {
				var builder = new XmlSchemaCompletionBuilder (source, path.Namespaces);
				if (schema.FindElement (path) is XmlSchemaElement element) {
					builder.AddAttributes (schema, element);
				}
				return new CompletionContext (builder.GetItems ());
			}, token);

		/// <summary>
		/// Gets the child element completion data for the xml element that exists
		/// at the end of the specified path.
		/// </summary>
		public Task<CompletionContext> GetChildElementCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, CancellationToken token) => Task.Run (
			() => {
				var builder = new XmlSchemaCompletionBuilder (source, path.Namespaces);
				if (schema.FindElement (path) is XmlSchemaElement element) {
					var last = path.Elements.LastOrDefault ();
					builder.AddChildElements (schema, element, last?.Prefix ?? "");
				}
				return new CompletionContext (builder.GetItems ());
			}, token);

		/// <summary>
		/// Gets the autocomplete data for the specified attribute value.
		/// </summary>
		public Task<CompletionContext> GetAttributeValueCompletionDataAsync (IAsyncCompletionSource source, XmlElementPath path, string name, CancellationToken token) => Task.Run (
			() => {
				var builder = new XmlSchemaCompletionBuilder (source, path.Namespaces);
				if (schema.FindElement (path) is XmlSchemaElement element) {
					builder.AddAttributeValues (schema, element, name);
				}
				return new CompletionContext (builder.GetItems ());
			}, token);

		/// <summary>
		/// Takes the name and creates a qualified name using the namespace of this
		/// schema.
		/// </summary>
		/// <remarks>If the name is of the form myprefix:mytype then the correct 
		/// namespace is determined from the prefix. If the name is not of this
		/// form then no prefix is added.</remarks>
		public QualifiedName CreateQualifiedName (string name)
		{
			int index = name.IndexOf (":");
			if (index >= 0) {
				string prefix = name.Substring (0, index);
				name = name.Substring (index + 1);

				//FIXME: look these up from the document's namespaces
				foreach (XmlQualifiedName xmlQualifiedName in schema.Namespaces.ToArray ()) {
					if (xmlQualifiedName.Name == prefix) {
						return new QualifiedName (name, xmlQualifiedName.Namespace, prefix);
					}
				}
			}

			// Default behaviour just return the name with the namespace uri.
			return new QualifiedName (name, NamespaceUri);
		}

		/// <summary>
		/// Finds the simple type with the specified name.
		/// </summary>
		public XmlSchemaSimpleType? FindSimpleType (string name)
		{
			var qualifiedName = new XmlQualifiedName (name, NamespaceUri);
			return schema.FindSimpleType (qualifiedName);
		}
	}
}