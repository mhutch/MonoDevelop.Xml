//
// XmlTagNameState.cs
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

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public class XmlNameState : XmlParserState
	{
		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			var namedObject = context.Nodes.Peek () as INamedXObject;
			if (namedObject == null || namedObject.Name.HasPrefix || namedObject.Name.IsValid) {
				throw new InvalidParserStateException ("XmlNameState can only operate on an INamedXObject without a name");
			}

			Debug.Assert (context.CurrentStateLength > 0 || XmlChar.IsFirstNameChar (c), "First character pushed to a XmlTagNameState must be a letter.");
			Debug.Assert (context.CurrentStateLength > 0 || context.KeywordBuilder.Length == 0, "Keyword builder must be empty when state begins.");

			if (isEndOfFile || XmlChar.IsWhitespace (c) || c == '<' || c == '>' || c == '/' || c == '=') {
				replayCharacter = true;
				if (context.KeywordBuilder.Length == 0) {
					namedObject.Name = XName.Empty;
				} else {
					string s = context.KeywordBuilder.ToString ();
					int i = s.IndexOf (':');
					if (i < 0) {
						namedObject.Name = s;
					} else {
						if (i == 0) {
							context.Diagnostics?.Add (XmlCoreDiagnostics.ZeroLengthNamespace, context.CurrentStateSpanExcludingCurrentChar);
							namedObject.Name = new XName (s.Substring (i + 1));
						} else if (i == s.Length - 1) {
							context.Diagnostics?.Add (XmlCoreDiagnostics.ZeroLengthNameWithNamespace, context.CurrentStateSpanExcludingCurrentChar);
							namedObject.Name = new XName (s.Substring (0, i));
						} else {
							namedObject.Name = new XName (s.Substring (0, i), s.Substring (i + 1));
						}
					}
				}

				return Parent;
			}

			if (c == ':') {
				if (context.KeywordBuilder.ToString ().IndexOf (':') > 0 && context.BuildTree)
					context.Diagnostics?.Add (XmlCoreDiagnostics.MultipleNamespaceSeparators, context.PositionBeforeCurrentChar);
				context.KeywordBuilder.Append (c);
				return null;
			}

			if (XmlChar.IsNameChar (c)) {
				context.KeywordBuilder.Append (c);
				return null;
			}

			replayCharacter = true;
			context.Diagnostics?.Add (XmlCoreDiagnostics.InvalidNameCharacter, context.PositionBeforeCurrentChar, c);
			return Parent;
		}

		public override XmlParserContext TryRecreateState (ref XObject xobject, int position)
			=> throw new InvalidOperationException ("State has no corresponding XObject");
	}
}
