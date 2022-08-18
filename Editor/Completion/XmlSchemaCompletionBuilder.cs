// 
// XmlCompletionDataList.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Novell, Inc.
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Schema;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;

namespace MonoDevelop.Xml.Editor.Completion
{
	class XmlSchemaCompletionBuilder
	{
		readonly List<CompletionItem> items = new ();

		readonly HashSet<string> names = new ();
		readonly XmlNamespacePrefixMap nsMap;
		readonly IAsyncCompletionSource source;

		public XmlSchemaCompletionBuilder (IAsyncCompletionSource source, XmlNamespacePrefixMap nsMap)
		{
			this.nsMap = nsMap;
			this.source = source;
		}

		public XmlSchemaCompletionBuilder (IAsyncCompletionSource source) : this (source, new XmlNamespacePrefixMap ())
		{
		}

		internal XmlSchemaCompletionBuilder AddNamespace (string namespaceUri)
		{
			var item = new CompletionItem (namespaceUri, source, XmlImages.Namespace);
			items.Add (item);
			return this;
		}

		public XmlSchemaCompletionBuilder AddAttribute (XmlSchemaAttribute attribute)
		{
			string? name = attribute.Name;
			if (name is null) {
				var ns = attribute.RefName.Namespace;
				if (string.IsNullOrEmpty (ns)) {
					return this;
				}
				if (nsMap.GetPrefix (ns) is not string prefix) {
					if (ns == "http://www.w3.org/XML/1998/namespace") {
						prefix = "xml";
					} else {
						return this;
					}
				}
				name = attribute.RefName.Name;
				if (prefix.Length > 0)
					name = prefix + ":" + name;
			}
			if (!names.Add (name)) {
				return this;
			}
			var item = new CompletionItem (name, source, XmlImages.Attribute);
			if (attribute.Annotation is XmlSchemaAnnotation annotation) {
				item.AddDocumentation (annotation);
			}
			items.Add (item);
			return this;
		}

		public XmlSchemaCompletionBuilder AddAttributeValue (string valueText)
		{
			var item = new CompletionItem (valueText, source, XmlImages.AttributeValue);
			items.Add (item);
			return this;
		}
		
		public XmlSchemaCompletionBuilder AddAttributeValue (string valueText, XmlSchemaAnnotation? annotation)
		{
			var item = new CompletionItem (valueText, source, XmlImages.AttributeValue);
			if (annotation is not null) {
				item.AddDocumentation (annotation);
			}
			items.Add (item);
			return this;
		}

		/// <summary>
		/// Adds an element completion data to the collection if it does not 
		/// already exist.
		/// </summary>
		public XmlSchemaCompletionBuilder AddElement (string name, string prefix, string documentation)
		{
			//FIXME: don't accept a prefix, accept a namespace and resolve it to a prefix
			if (prefix.Length > 0)
				name = string.Concat (prefix, ":", name);

			if (!names.Add (name))
				return this;

			var item = new CompletionItem (name, source, XmlImages.Element);
			item.AddDocumentation (documentation);
			items.Add (item);
			return this;
		}
		
		/// <summary>
		/// Adds an element completion data to the collection if it does not 
		/// already exist.
		/// </summary>
		public XmlSchemaCompletionBuilder AddElement (string name, string prefix, XmlSchemaAnnotation? annotation)
		{
			//FIXME: don't accept a prefix, accept a namespace and resolve it to a prefix
			if (prefix.Length > 0)
				name = string.Concat (prefix, ":", name);

			if (!names.Add (name))
				return this;

			var item = new CompletionItem (name, source, XmlImages.Element);
			if (annotation is not null) {
				item.AddDocumentation (annotation);
			}
			items.Add (item);
			return this;
		}

		public ImmutableArray<CompletionItem> GetItems () => ImmutableArray<CompletionItem>.Empty.AddRange (items);
	}
}

