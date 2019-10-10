// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlTextState : XmlParserState
	{
		public override XmlParserState PushChar (char c, XmlParserContext context, ref string rollback)
		{
			if (context.CurrentStateLength == 0) {
				context.Nodes.Push (new XText (context.Position));
				// StateTag is tracking the last non-whitespace char
				context.StateTag = context.Position;
			}

			//FIXME: handle entities?

			if (c == '<') {
				var node = (XText)context.Nodes.Pop ();

				if (context.BuildTree) {
					//trim the text down to the node length and add it
					var length = context.StateTag - node.Span.Start + 1;
					context.KeywordBuilder.Length = length;
					node.End (context.KeywordBuilder.ToString ());
					((XContainer)context.Nodes.Peek ()).AddChildNode (node);
				} else {
					node.End (context.StateTag + 1);
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
