//
// XmlAttributeValueState.cs
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
	public class XmlAttributeValueState : XmlParserState
	{
		const int FREE = 0;
		const int UNQUOTED = 1;
		const int SINGLEQUOTE = 2;
		const int DOUBLEQUOTE = 3;

		//derived classes should use these if they need to store info in the tag
		protected const int TagMask = 3;
		protected const int TagShift = 2;

		public override XmlParserState? PushChar (char c, XmlParserContext context, ref bool replayCharacter, bool isEndOfFile)
		{
			System.Diagnostics.Debug.Assert (((XAttribute) context.Nodes.Peek ()).Value == null);

			if (c == '<') {
				//the parent state should report the error
				var att = (XAttribute)context.Nodes.Peek ();
				att.Value = context.KeywordBuilder.ToString ();
				replayCharacter = true;
				return Parent;
			}

			if (isEndOfFile) {
				//the parent state should report the error
				context.Diagnostics?.Add (XmlCoreDiagnostics.IncompleteAttributeEof, context.PositionBeforeCurrentChar);
				var att = (XAttribute)context.Nodes.Peek ();
				att.Value = context.KeywordBuilder.ToString ();
				return Parent;
			}

			if (context.CurrentStateLength == 0) {
				if (c == '"') {
					context.StateTag = DOUBLEQUOTE;
					return null;
				}
				if (c == '\'') {
					context.StateTag = SINGLEQUOTE;
					return null;
				}
				context.StateTag = UNQUOTED;
			}

			int maskedTag = context.StateTag & TagMask;

			if (maskedTag == UNQUOTED) {
				return BuildUnquotedValue (c, context, ref replayCharacter);
			}

			if ((c == '"' && maskedTag == DOUBLEQUOTE) || c == '\'' && maskedTag == SINGLEQUOTE) {
				//ending the value
				var att = (XAttribute) context.Nodes.Peek ();
				att.Value = context.KeywordBuilder.ToString ();
				return Parent;
			}

			context.KeywordBuilder.Append (c);
			return null;
		}

		public static bool IsUnquotedValueChar (char c) => char.IsLetterOrDigit (c) || c == '_' || c == '.' || c == '-';

		XmlParserState? BuildUnquotedValue (char c, XmlParserContext context, ref bool replayCharacter)
		{
			// even though unquoted values aren't legal, attempt to build a value anyway
			if (IsUnquotedValueChar (c)) {
				context.KeywordBuilder.Append (c);
				return null;
			}

			// if first char is not something we can handle as an unquoted char, just reject it for parent to deal with
			if (context.KeywordBuilder.Length == 0) {
				if (context.Diagnostics is not null && context.Nodes.Peek () is XAttribute badAtt && badAtt.Name.IsValid) {
					context.Diagnostics.Add (XmlCoreDiagnostics.IncompleteAttributeValue, context.Position, badAtt.Name!.FullName, c);
				}
				replayCharacter = true;
				return Parent;
			}

			var att = (XAttribute)context.Nodes.Peek ();
			att.Value = context.KeywordBuilder.ToString ();

			if (context.Diagnostics is not null && att.Name.IsValid) {
				context.Diagnostics.Add (XmlCoreDiagnostics.UnquotedAttributeValue, new TextSpan (context.Position - att.Value.Length, att.Value.Length), att.Name.FullName);
			}

			replayCharacter = true;
			return Parent;
		}

		public override XmlParserContext TryRecreateState (ref XObject xobject, int position)
			=> throw new InvalidOperationException ("State has no corresponding XObject");

		public static char? GetDelimiterChar (XmlParserContext context)
			=> context.CurrentState is XmlAttributeValueState
			? (context.StateTag & TagMask) switch {
				SINGLEQUOTE => '\'',
				DOUBLEQUOTE => '"',
				_ => (char?) null
			}
			: null;
	}
}
