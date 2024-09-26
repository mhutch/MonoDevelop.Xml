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

using System;
using System.Diagnostics;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlSpineParser : XmlParser, ICloneable
	{
		public XmlSpineParser (XmlRootState rootState) : base (rootState) { }

		public XmlSpineParser (XmlParserContext context, XmlRootState rootState) : base (context, rootState)
		{
			Debug.Assert (!context.BuildTree);
		}

		//TODO: these should only be exposed on XmlSpineParser, not XmlTreeParser
		public XmlParserState CurrentState => Context.CurrentState;
		public int CurrentStateLength => Context.CurrentStateLength;
		public NodeStack Spine => Context.Nodes;
		public int Position => Context.Position;

		public XmlSpineParser Clone () => new XmlSpineParser (Context.ShallowCopy (), RootState);

		object ICloneable.Clone () => Clone ();

		public XmlTreeParser GetTreeParser () => new (this);

		/// <summary>
		/// Efficiently creates a spine parser using information from an existing document. The position of
		/// the parser is not guaranteed but will not exceed <paramref name="maximumPosition" />.
		/// </summary>
		/// <returns></returns>
		public static XmlSpineParser FromDocumentPosition (XmlRootState stateMachine, XDocument xdocument, int maximumPosition)
		{
			// Recovery must be based on the node before the target position.
			// The state for the node at the position won't be entered until the character at (i.e. after) the position is processed.
			var node = maximumPosition == 0? xdocument : xdocument.FindAtOrBeforeOffset (maximumPosition - 1);
			if (node is XObject obj && stateMachine.TryRecreateState (ref obj, maximumPosition) is XmlParserContext ctx) {
				return new XmlSpineParser (ctx, stateMachine);
			}
			return new XmlSpineParser (stateMachine); ;
		}
	}
}
