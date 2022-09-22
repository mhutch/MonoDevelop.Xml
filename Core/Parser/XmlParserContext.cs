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
using System.Text;

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlParserContext
	{
		public XmlParserContext (XmlParserState? previousState, XmlParserState currentState, int currentStateLength = 0, int stateTag = 0, StringBuilder? keywordBuilder = null, int position = 0, NodeStack? nodes = null, bool buildTree = false, List<XmlDiagnosticInfo>? diagnostics = null)
		{
			PreviousState = previousState;
			CurrentState = currentState;
			CurrentStateLength = currentStateLength;
			StateTag = stateTag;
			KeywordBuilder = keywordBuilder ?? new StringBuilder ();
			Position = position;
			Nodes = nodes ?? new NodeStack ();
			BuildTree = buildTree;
			Diagnostics = diagnostics;
		}

		public XmlParserState? PreviousState { get; set; }
		public XmlParserState CurrentState { get; set; }
		public int CurrentStateLength { get; set; }
		public int StateTag { get; set; }
		public StringBuilder KeywordBuilder { get; private set; }
		public int Position { get; set; }
		public NodeStack Nodes { get; }
		public bool BuildTree { get; set; }
		public List<XmlDiagnosticInfo>? Diagnostics { get; set; }

		public void ConnectNodes ()
		{
			XNode? prev = null;
			foreach (XObject o in Nodes) {
				XContainer? container = o as XContainer;
				if (prev != null && container != null && prev.IsComplete)
					container.AddChildNode (prev);
				if (o.Parent != null)
					break;
				prev = o as XNode;
			}
		}

		internal XmlParserContext ShallowCopy () => new (
				previousState: PreviousState,
				currentState: CurrentState,
				currentStateLength: CurrentStateLength,
				stateTag: StateTag,
				keywordBuilder: new StringBuilder (KeywordBuilder.ToString ()),
				position: Position,
				nodes: Nodes.ShallowCopy (),
				buildTree: false,
				diagnostics: null);

		public void EndAll (bool pop)
		{
			int popCount = 0;
			foreach (XObject ob in Nodes) {
				if (!ob.IsEnded && !(ob is XDocument)) {
					ob.End (Position);
					popCount++;
				} else {
					break;
				}
			}

			if (pop) {
				for (; popCount > 0; popCount--) {
					Nodes.Pop ();
				}
			}
		}

		public override string ToString ()
		{
			var builder = new StringBuilder ();
			builder.AppendFormat ("[XmlParserContext Location={0} CurrentStateLength={1}", Position, CurrentStateLength);
			builder.AppendLine ();

			builder.Append (' ', 2);
			builder.AppendLine ("Stack=");

			XObject? rootOb = null;
			foreach (XObject ob in Nodes) {
				rootOb = ob;
				builder.Append (' ', 4);
				builder.Append (ob.ToString ());
				builder.AppendLine ();
			}

			builder.Append (' ', 2);
			builder.AppendLine ("States=");
			XmlParserState? s = CurrentState;
			while (s != null) {
				builder.Append (' ', 4);
				builder.Append (s.ToString ());
				builder.AppendLine ();
				s = s.Parent;
			}

			if (BuildTree && rootOb != null) {
				builder.Append (' ', 2);
				builder.AppendLine ("Tree=");
				rootOb.BuildTreeString (builder, 3);
			}

			if (BuildTree && Diagnostics?.Count > 0) {
				builder.Append (' ', 2);
				builder.AppendLine ("Errors=");
				foreach (XmlDiagnosticInfo err in Diagnostics) {
					builder.Append (' ', 4);
					builder.AppendLine ($"[{err.Severity}@{err.Span}: {err.Message}]");
				}
			}

			builder.AppendLine ("]");
			return builder.ToString ();
		}

		public void ResetKeywordBuilder ()
		{
			//FIXME: use a pool here?
			if (KeywordBuilder.Length < 50) {
				KeywordBuilder.Length = 0;
			} else {
				KeywordBuilder = new StringBuilder ();
			}
		}
	}
}
