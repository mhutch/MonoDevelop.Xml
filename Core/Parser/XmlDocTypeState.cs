//
// XmlDocTypeState.cs
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

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlDocTypeState : XmlParserState
	{
		const int STARTOFFSET = 9; // "<!DOCTYPE";

		readonly XmlNameState nameState;

		public XmlDocTypeState ()
		{
			nameState = Adopt (new XmlNameState ());
		}

		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			var doc = context.Nodes.Peek () as XDocType;
			if (doc == null) {
				doc = new XDocType (context.Position - STARTOFFSET);
				context.Nodes.Push (doc);
			}

			if (isEndOfFile) {
				// skip to ending the node
			} else if (!doc.RootElement.IsValid) {
				if (XmlChar.IsWhitespace (c))
					return null;
				else if (XmlChar.IsFirstNameChar (c)) {
					replayCharacter = true;
					return nameState;
				}
			}
			else if (doc.PublicFpi == null) {
				if (context.StateTag == 0) {
					if (c == 's' || c == 'S') {
						context.StateTag = 1;
						return null;
					} else if (c == 'p' || c == 'P') {
						context.StateTag = -1;
						return null;
					} if (XmlChar.IsWhitespace (c)) {
						return null;
					}
				} else if (Math.Abs (context.StateTag) < 6) {
					if (context.StateTag > 0) {
						if ("YSTEM"[context.StateTag - 1] == c || "ystem"[context.StateTag - 1] == c) {
							context.StateTag++;
							if (context.StateTag == 6) {
								context.StateTag = 0;
								doc.PublicFpi = "";
							}
							return null;
						}
					} else {
						int absState = Math.Abs (context.StateTag) - 1;
						if ("UBLIC"[absState] == c || "ublic"[absState] == c) {
							context.StateTag--;
							return null;
						}
					}
				} else {
					if (context.KeywordBuilder.Length == 0) {
						if (XmlChar.IsWhitespace (c))
							return null;
						else if (c == '"') {
							context.KeywordBuilder.Append (c);
							return null;
						}
					} else {
						if (c == '"') {
							context.KeywordBuilder.Remove (0,1);
							doc.PublicFpi = context.KeywordBuilder.ToString ();
							context.KeywordBuilder.Length = 0;
							context.StateTag = 0;
						} else {
							context.KeywordBuilder.Append (c);
						}
						return null;
					}
				}
			}
			else if (doc.Uri == null) {
				if (context.KeywordBuilder.Length == 0) {
					if (XmlChar.IsWhitespace (c))
						return null;
					else if (c == '"') {
						context.KeywordBuilder.Append (c);
						return null;
					}
				} else {
					if (c == '"') {
						context.KeywordBuilder.Remove (0,1);
						doc.Uri = context.KeywordBuilder.ToString ();
						context.KeywordBuilder.Length = 0;
					} else {
						context.KeywordBuilder.Append (c);
					}
					return null;
				}
			}
			else if (doc.InternalDeclarationRegion.Length == 0) {
				if (XmlChar.IsWhitespace (c))
						return null;
				switch (context.StateTag) {
				case 0:
					if (c == '[') {
						doc.InternalDeclarationRegion = new TextSpan (context.Position + 1, 0);
						context.StateTag = 1;
						return null;
					}
					break;
				case 1:
					if (c == '<') {
						context.StateTag = 2;
						return null;
					} else if (c == ']') {
						context.StateTag = 0;
						doc.InternalDeclarationRegion = TextSpan.FromBounds (doc.InternalDeclarationRegion.Start, context.Position);
						return null;
					}
					break;
				case 2:
					if (c == '>') {
						context.StateTag = 1;
					}
					return null;
				default:
					throw new InvalidOperationException ();
				}
			}

			doc = (XDocType)context.Nodes.Pop ();
			if (c == '<') {
				replayCharacter = true;
			}

			if (isEndOfFile) {
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteDocTypeEof, context.PositionBeforeCurrentChar, c);
			}
			else if (c != '>') {
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteDocType, context.PositionBeforeCurrentChar, c);
			}

			doc.End (context.PositionBeforeCurrentChar);
			if (context.BuildTree) {
				((XContainer) context.Nodes.Peek ()).AddChildNode (doc);
			}
			return Parent;
		}

		public override XmlParserContext? TryRecreateState (ref XObject xobject, int position)
		{
			// we could attempt recovery but it's kinda complex due to URL keyword builder
			// for now, let parent recreate state at start of tag
			return null;
		}
	}
}

