// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlTextState : XmlParserState
	{
		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			if (context.CurrentStateLength == 0) {
				context.Nodes.Push (new XText (context.Position));
				// StateTag is tracking the last non-whitespace char
				context.StateTag = context.Position;
			}

			//FIXME: handle entities?

			if (c == '<' || isEndOfFile) {
				EndAndPop ();
				replayCharacter = true;
				return Parent;
			}

			if (!XmlChar.IsWhitespace (c)) {
				context.StateTag = context.Position;
			}

			context.KeywordBuilder.Append (c);
			
			return null;

			void EndAndPop ()
			{
				var node = (XText)context.Nodes.Pop ();

				//trim the text down to the last non-whitespace char and add it
				var length = context.StateTag - node.Span.Start + 1;
				context.KeywordBuilder.Length = length;
				node.End (context.KeywordBuilder.ToString ());

				if (context.BuildTree) {
					((XContainer)context.Nodes.Peek ()).AddChildNode (node);
				}
			}
		}

		public override XmlParserContext? TryRecreateState (ref XObject xobject, int position)
		{
			if (xobject is XText text && position >= text.Span.Start && position < text.Span.End) {

				// truncate to length
				var sb = new System.Text.StringBuilder (text.Text);
				sb.Length = position - text.Span.Start;

				var parents = NodeStack.FromParents (text);
				if (sb.Length > 0) {
					parents.Push (new XText (text.Span.Start));
				}

				int lastNonWhitespace = position;
				for (int i = sb.Length - 1; i >= 0; i--) {
					if (!XmlChar.IsWhitespace (text.Text[i])) {
						lastNonWhitespace = position - sb.Length + i;
						break;
					}
				}

				return new (
					currentState: this,
					position: position,
					previousState: Parent,
					currentStateLength: sb.Length,
					keywordBuilder: sb,
					nodes: parents,
					stateTag: lastNonWhitespace
				);
			}

			return null;
		}
	}
}
