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

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlTextState : XmlParserState
	{
		public override XmlParserState PushChar (char c, IXmlParserContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 0) {
				context.Nodes.Push (new XText (context.Position));
				// StateTag is tracking the last non-whitespace char
				context.StateTag = context.Position;
			}

			//FIXME: handle entities?

			if (c == '<') {
				if (context.BuildTree) {
					var node = (XText)context.Nodes.Pop ();

					//trim the text down to the node length and add it
					context.KeywordBuilder.Length = context.StateTag - node.Span.Start + 1;
					node.End (context.KeywordBuilder.ToString ());

					((XContainer)context.Nodes.Peek ()).AddChildNode (node);
				}
				rollback = string.Empty;
				return Parent;
			}

			if (!XmlChar.IsWhitespace (c)) {
				context.StateTag = context.Position;
			}

			context.KeywordBuilder.Append (c);
			
			return null;
		}

		public override XmlParserContext TryRecreateState (XObject xobject, int position)
		{
			if (xobject is XText text && position >= text.Span.Start && position < text.Span.End) {

				// truncate to length
				var sb = new System.Text.StringBuilder (text.Text);
				sb.Length = position - text.Span.Start;

				var parents = NodeStack.FromParents (text);
				if (sb.Length > 0) {
					parents.Push (new XText (text.Span.Start));
				}

				return new XmlParserContext {
					CurrentState = this,
					Position = position,
					PreviousState = Parent,
					CurrentStateLength = sb.Length,
					KeywordBuilder = sb,
					Nodes = parents,
					StateTag = position
				};
			}

			return null;
		}
	}
}
