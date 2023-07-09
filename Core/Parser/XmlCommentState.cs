// 
// XmlCommentState.cs
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
	public class XmlCommentState : XmlParserState
	{
		const int STARTOFFSET = 4; // "<!--"

		const int NOMATCH = 0;
		const int SINGLE_DASH = 1;
		const int DOUBLE_DASH = 2;
		
		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			if (context.CurrentStateLength == 0) {
				context.Nodes.Push (new XComment (context.Position - STARTOFFSET));
			}

			if (isEndOfFile) {
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteCommentEof, context.PositionBeforeCurrentChar);
				return EndAndPop ();
			}

			if (c == '-') {
				//make sure we know when there are two '-' chars together
				if (context.StateTag == NOMATCH)
					context.StateTag = SINGLE_DASH;
				else
					context.StateTag = DOUBLE_DASH;
				
			} else if (context.StateTag == DOUBLE_DASH) {
				if (c == '>') {
					// if the '--' is followed by a '>', the state has ended
					// so attach a node to the DOM and end the state
					return EndAndPop ();
				} else {
					context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteEndComment, context.Position);
					context.StateTag = NOMATCH;
				}
			} else {
				// not any part of a '-->', so make sure matching is reset
				context.StateTag = NOMATCH;
			}
			
			return null;

			XmlParserState? EndAndPop ()
			{
				var comment = (XComment)context.Nodes.Pop ();

				comment.End (context.PositionAfterCurrentChar);
				if (context.BuildTree) {
					((XContainer)context.Nodes.Peek ()).AddChildNode (comment);
				}
				return Parent;
			}
		}

		public override XmlParserContext? TryRecreateState (XObject xobject, int position)
		{
			if (xobject is XComment comment && position >= comment.Span.Start + STARTOFFSET && position < comment.Span.End) {
				var parents = NodeStack.FromParents (comment);

				var length = position - comment.Span.Start + STARTOFFSET;
				if (length > 0) {
					parents.Push (new XComment (comment.Span.Start));
				}

				return new XmlParserContext (
					currentState: this,
					position: position,
					previousState: Parent,
					currentStateLength: length,
					keywordBuilder: new System.Text.StringBuilder (),
					stateTag: position == comment.Span.End - 3 ? SINGLE_DASH : (position == comment.Span.End - 2 ? DOUBLE_DASH: NOMATCH),
					nodes: parents
				);
			}

			return null;
		}
	}
}
