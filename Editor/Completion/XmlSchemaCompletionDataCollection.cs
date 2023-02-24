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

using System.Collections.Generic;

namespace MonoDevelop.Xml.Editor.Completion
{
	interface IXmlSchemaCompletionDataCollection: IEnumerable<IXmlCompletionProvider>
	{
		XmlSchemaCompletionProvider? this [string namespaceUri] { get; }
		void GetNamespaceCompletionData (XmlSchemaCompletionBuilder builder);
		XmlSchemaCompletionProvider? GetSchemaFromFileName (string fileName);
	}
	
	class XmlSchemaCompletionDataCollection : List<IXmlCompletionProvider>, IXmlSchemaCompletionDataCollection
	{
		public XmlSchemaCompletionProvider? this [string namespaceUri] {
			get {
				foreach (XmlSchemaCompletionProvider item in this)
					if (item.NamespaceUri == namespaceUri)
						return item;
				return null;
			}
		}
		
		public void GetNamespaceCompletionData (XmlSchemaCompletionBuilder builder)
		{
			foreach (XmlSchemaCompletionProvider schema in this) {
				if (schema.NamespaceUri is not null) {
					builder.AddNamespace (schema.NamespaceUri);
				}
			}
		}
		
		public XmlSchemaCompletionProvider? GetSchemaFromFileName (string fileName)
		{
			foreach (XmlSchemaCompletionProvider schema in this)
				if (schema.FileName == fileName)
					return schema;
			return null;
		}
	}
	
	class MergedXmlSchemaCompletionDataCollection : IXmlSchemaCompletionDataCollection
	{
		readonly XmlSchemaCompletionDataCollection builtin;
		readonly XmlSchemaCompletionDataCollection user;
		
		public MergedXmlSchemaCompletionDataCollection (
		    XmlSchemaCompletionDataCollection builtin,
		    XmlSchemaCompletionDataCollection user)
		{
			this.user = user;
			this.builtin = builtin;
		}
		
		public XmlSchemaCompletionProvider? this [string namespaceUri] => user[namespaceUri] ?? builtin[namespaceUri];

		public void GetNamespaceCompletionData (XmlSchemaCompletionBuilder builder)
		{
			foreach (XmlSchemaCompletionProvider schema in builtin) {
				if (schema.NamespaceUri is string ns) {
					builder.AddNamespace (ns);
				}
			}
			foreach (XmlSchemaCompletionProvider schema in user) {
				if (schema.NamespaceUri is string ns) {
					builder.AddNamespace (ns);
				}
			}
		}

		public XmlSchemaCompletionProvider? GetSchemaFromFileName (string fileName)
			=> user.GetSchemaFromFileName (fileName) ?? builtin.GetSchemaFromFileName (fileName);
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () => GetEnumerator ();

		public IEnumerator<IXmlCompletionProvider> GetEnumerator ()
		{
			foreach (XmlSchemaCompletionProvider x in builtin) {
				if (x.NamespaceUri is string ns && user[ns] == null) {
					yield return x;
				}
			}
			foreach (XmlSchemaCompletionProvider x in user) {
				yield return x;
			}
		}
	}
}
