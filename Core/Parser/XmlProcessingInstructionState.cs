// 
// XmlProcessingInstructionState.cs
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
	public class XmlProcessingInstructionState : XmlParserState
	{
		const int STARTOFFSET = 2; // "<?"
		const int NOMATCH = 0;
		const int QUESTION = 1;
		
		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			if (context.CurrentStateLength == 0) {
				context.Nodes.Push (new XProcessingInstruction (context.Position - QUESTION));
			}

			if (isEndOfFile) {
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteProcessingInstructionEof, context.PositionAfterCurrentChar);
			}

			if (c == '?') {
				if (context.StateTag == NOMATCH) {
					context.StateTag = QUESTION;
					return null;
				}
			} else if ((c == '>' && context.StateTag == QUESTION) || isEndOfFile) {
				// if the '?' is followed by a '>', the state has ended
				// so attach a node to the DOM and end the state
				var xpi = (XProcessingInstruction) context.Nodes.Pop ();

				// at this point the position isn't incremented to include the '>' yet
				// so make sure to include the closing '>' in the span
				xpi.End (context.PositionAfterCurrentChar);

				if (context.BuildTree) {
					((XContainer) context.Nodes.Peek ()).AddChildNode (xpi); 
				}
				return Parent;
			} else {
				context.StateTag = NOMATCH;
			}
			
			return null;
		}

		public override XmlParserContext? TryRecreateState (ref XObject xobject, int position)
		{
			if (xobject is XProcessingInstruction pi && position >= pi.Span.Start + STARTOFFSET && position < pi.Span.End) {
				var parents = NodeStack.FromParents (pi);

				var length = position - pi.Span.Start - 1;
				if (length > 0) {
					parents.Push (new XProcessingInstruction (pi.Span.Start));
				}

				return new (
					currentState: this,
					position: position,
					previousState: Parent,
					currentStateLength: length,
					stateTag: position == pi.Span.End - 1? QUESTION : NOMATCH,
					nodes: parents
				);
			}

			return null;
		}
	}
}
