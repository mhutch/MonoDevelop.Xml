//
// XmlClosingTagState.cs
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

using System.Diagnostics;

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlClosingTagState : XmlParserState
	{
		const int STARTOFFSET = 2; // "</"

		readonly XmlNameState NameState;

		public XmlClosingTagState ()
			: this (new XmlNameState ())
		{
		}

		public XmlClosingTagState (XmlNameState nameState)
		{
			NameState = Adopt (nameState);
		}

		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			var ct = context.Nodes.Peek () as XClosingTag;

			if (ct == null) {
				Debug.Assert (context.CurrentStateLength == 0, "IncompleteNode must not be an XClosingTag when CurrentStateLength is 0");

				ct = new XClosingTag (context.Position - STARTOFFSET);
				context.Nodes.Push (ct);
			}

			//if tag closed
			if (c == '>' || isEndOfFile) {
				context.Nodes.Pop ();
				ct.End (context.PositionAfterCurrentChar);

				if (ct.Name.IsValid) {
					ct.End (context.Position + 1);

					// walk up tree of parents looking for matching tag
					int popCount = 0;
					bool found = false;
					foreach (XObject node in context.Nodes) {
						popCount++;
						if (node is XElement element && element.Name == ct.Name) {
							found = true;
							break;
						}
					}
					if (!found)
						popCount = 0;

					//clear the stack of intermediate unclosed tags
					while (popCount > 1) {
						if (context.Nodes.Pop () is XElement el && el.Name.IsValid) {
							context.Diagnostics?.Add (XmlCoreDiagnostics.UnclosedTag, el.Span, el.Name.FullName);
						}
						popCount--;
					}

					//close the start tag, if we found it
					if (popCount > 0) {
						// close it even if not in tree mode, as some spines may want to know whether an element was closed after advancing the parser
						((XElement) context.Nodes.Pop ()).Close (ct);
					} else {
						if (context.BuildTree) {
							context.Diagnostics?.Add (XmlCoreDiagnostics.UnmatchedClosingTag, ct.Span, ct.Name.FullName);
							// add it into the tree anyway so it's accessible
							if (context.Nodes.TryPeek (out XContainer? parent)) {
								if (!parent.IsEnded) {
									parent = context.Nodes.TryPeek<XContainer> (1);
								}
								parent?.AddChildNode (ct);
							}
						}
					}
				}

				if (isEndOfFile) {
					context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteClosingTagEof, context.PositionBeforeCurrentChar);
				}
				else if (!ct.IsNamed) {
					context.Diagnostics?.Add (XmlCoreDiagnostics.UnnamedClosingTag, ct.Span);
				}

				return Parent;
			}

			if (c == '<') {
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteClosingTag, context.PositionBeforeCurrentChar, '<');
				context.Nodes.Pop ();
				replayCharacter = true;
				return Parent;
			}

			if (XmlChar.IsWhitespace (c)) {
				return null;
			}

			if (!ct.IsNamed && (char.IsLetter (c) || c == '_')) {
				replayCharacter = true;
				return NameState;
			}

			replayCharacter = true;
			context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteClosingTag, context.PositionBeforeCurrentChar, c);
			context.Nodes.Pop ();
			return Parent;
		}

		public override XmlParserContext? TryRecreateState (ref XObject xobject, int position)
		{
			// recreating name builder state is a pain
			// for now, let parent recreate state at start of tag
			return null;
		}
	}
}
