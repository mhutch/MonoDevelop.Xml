// 
// XmlCDataState.cs
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

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlCDataState : XmlParserState
	{
		const int STARTOFFSET = 9; // "<![CDATA[";

		const int NOMATCH = 0;
		const int SINGLE_BRACKET = 1;
		const int DOUBLE_BRACKET = 2;
		
		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFIle)
		{
			if (context.CurrentStateLength == 0) {
				context.Nodes.Push (new XCData (context.Position - STARTOFFSET));
			}

			if (isEndOfFIle) {
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteCDataEof, context.PositionBeforeCurrentChar);
				return EndAndPop ();
			}

			if (c == ']') {
				//make sure we know when there are two ']' chars together
				if (context.StateTag == NOMATCH)
					context.StateTag = SINGLE_BRACKET;
				else if (context.StateTag == SINGLE_BRACKET)
					context.StateTag = DOUBLE_BRACKET;
				else {
					// not any part of a ']]>', so make sure matching is reset
					context.StateTag = NOMATCH;
					context.KeywordBuilder.Append (c);
				}
				return null;
			} else if (c == '>' && context.StateTag == DOUBLE_BRACKET) {
				// if the ']]' is followed by a '>', the state has ended
				// so attach a node to the DOM and end the state
				return EndAndPop ();
			} else {
				// not any part of a ']]>', so make sure matching is reset
				if (context.StateTag == SINGLE_BRACKET)
					context.KeywordBuilder.Append (']');
				else if (context.StateTag == DOUBLE_BRACKET)
					context.KeywordBuilder.Append ("]]");

				context.KeywordBuilder.Append (c);
				context.StateTag = NOMATCH;
				return null;
			}

			XmlParserState? EndAndPop ()
			{
				var cdata = (XCData)context.Nodes.Pop ();
				cdata.InnerText = context.KeywordBuilder.ToString ();
				cdata.End (context.PositionAfterCurrentChar);

				if (context.BuildTree) {
					((XContainer)context.Nodes.Peek ()).AddChildNode (cdata);
				}
				return Parent;
			}
		}

		public override XmlParserContext? TryRecreateState (ref XObject xobject, int position)
		{
			if (xobject is XCData cd && position >= cd.Span.Start + STARTOFFSET && position < cd.Span.End) {
				var parents = NodeStack.FromParents (cd);

				var length = position - cd.Span.Start + STARTOFFSET;
				if (length > 0) {
					parents.Push (new XCData (cd.Span.Start));
				}

				return new XmlParserContext (
					previousState: Parent,
					currentState: this,
					position: position,
					currentStateLength: length,
					keywordBuilder: new System.Text.StringBuilder (),
					stateTag: position == cd.Span.End - 3 ? SINGLE_BRACKET : (position == cd.Span.End - 2 ? DOUBLE_BRACKET : NOMATCH),
					nodes: parents
				);
			}

			return null;
		}
	}
}
