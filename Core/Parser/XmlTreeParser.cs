// 
// Parser.cs
// 
// Author:
//   Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.IO;
using System.Threading;

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlTreeParser : XmlParser
	{
		public XmlTreeParser (XmlRootState rootState) : base (rootState)
		{
			Context.BuildTree = true;
			Context.Diagnostics = new List<XmlDiagnosticInfo> ();
		}

		internal XmlTreeParser (XmlSpineParser fromSpine)
			: base (fromSpine.GetContext ().ShallowCopy(), fromSpine.RootState)
		{
			Context.BuildTree = true;
			Context.ConnectNodes ();
			Context.Diagnostics = new List<XmlDiagnosticInfo> ();
		}

		/// <summary>
		/// Pushes all the chars in the reader and returns the finalized document.
		/// </summary>
		/// <param name="c">The character</param>
		public (XDocument document, List<XmlDiagnosticInfo> diagnostic) Parse (TextReader reader, CancellationToken cancellationToken = default)
		{
			int i = reader.Read ();
			while (i >= 0) {
				char c = (char)i;
				Push (c);
				i = reader.Read ();
				cancellationToken.ThrowIfCancellationRequested ();
			}
			return FinalizeDocument ();
		}

		/// <summary>
		/// Call this after done parsing using the Push(char) API. It will add errors for unclosed elements and incomplete nodes on the stack.
		/// </summary>
		public (XDocument document, List<XmlDiagnosticInfo> diagnostic) FinalizeDocument ()
		{
			var diagnostics = Context.Diagnostics!;

			bool loggedEof = false;
			while (Context.Nodes.Count > 1) {
				var node = Context.Nodes.Pop ();
				if (node is XElement el && !el.IsClosed) {
					diagnostics.LogError ($"Unclosed tag '{el.Name.FullName}'", el.Span);
				}
				if (!node.IsEnded) {
					node.End (Context.Position);
					if (!loggedEof) {
						diagnostics.LogError ("Unexpected end of file", Context.Position);
						loggedEof = true;
					}
				}
			}

			var doc = (XDocument)Context.Nodes.Pop ();
			doc.End (Context.Position);

			return (doc, diagnostics);
		}
	}
}
