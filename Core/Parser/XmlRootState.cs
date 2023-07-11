// 
// XmlFreeState.cs
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
	public class XmlRootState : XmlParserState
	{
		const int FREE = 0;
		const int BRACKET = FREE + 1;
		const int BRACKET_EXCLAM = BRACKET + 1;
		const int COMMENT = BRACKET_EXCLAM + 1;
		const int CDATA = COMMENT + 1;
		const int DOCTYPE = CDATA + 1;
		const int MAXCONST = DOCTYPE;

		public XmlRootState () : this (new XmlTagState (), new XmlClosingTagState ()) { }

		public XmlRootState (XmlTagState tagState, XmlClosingTagState closingTagState)
			: this (tagState, closingTagState, new XmlCommentState (), new XmlCDataState (),
			  new XmlDocTypeState (), new XmlProcessingInstructionState (), new XmlTextState ())
		{ }

		public XmlRootState (
			XmlTagState tagState,
			XmlClosingTagState closingTagState,
			XmlCommentState commentState,
			XmlCDataState cDataState,
			XmlDocTypeState docTypeState,
			XmlProcessingInstructionState processingInstructionState,
			XmlTextState textState)
		{
			TagState = Adopt (tagState);
			ClosingTagState = Adopt (closingTagState);
			CommentState = Adopt (commentState);
			CDataState = Adopt (cDataState);
			DocTypeState = Adopt (docTypeState);
			ProcessingInstructionState = Adopt (processingInstructionState);
			TextState = Adopt (textState);
		}

		protected XmlTagState TagState { get; }
		protected XmlClosingTagState ClosingTagState { get; }
		protected XmlCommentState CommentState { get; }
		protected XmlCDataState CDataState { get; }
		protected XmlDocTypeState DocTypeState { get; }
		protected XmlProcessingInstructionState ProcessingInstructionState { get; }
		protected XmlTextState TextState { get; }

		public override XmlParserState? PushChar (char c, XmlParserContext context, ref string? rollback)
		{
			if (c == '<') {
				if (context.StateTag != FREE)
					context.Diagnostics?.Add (
						XmlCoreDiagnostics.MalformedTagOpening,
						TextSpan.FromBounds (context.Position + 1 - LengthFromOpenBracket (context), context.Position + 1),
						'<'
					);
				context.StateTag = BRACKET;
				return null;
			}

			switch (context.StateTag) {
			case FREE:
				if (!XmlChar.IsWhitespace (c)) {
					rollback = string.Empty;
					return TextState;
				}

				return null;

			case BRACKET:
				switch (c) {
				case '?':
					rollback = string.Empty;
					return ProcessingInstructionState;
				case '!':
					context.StateTag = BRACKET_EXCLAM;
					return null;
				case '/':
					return ClosingTagState;
				}
				if (char.IsLetter (c) || c == '_' || char.IsWhiteSpace (c)) {
					rollback = string.Empty;
					return TagState;
				}
				break;

			case BRACKET_EXCLAM:
				switch (c) {
				case '[':
					context.StateTag = CDATA;
					return null;
				case '-':
					context.StateTag = COMMENT;
					return null;
				case 'D':
					context.StateTag = DOCTYPE;
					return null;
				}
				break;

			case COMMENT:
				if (c == '-')
					return CommentState;
				break;

			case CDATA:
				string cdataStr = "CDATA[";
				if (c == cdataStr [context.KeywordBuilder.Length]) {
					context.KeywordBuilder.Append (c);
					if (context.KeywordBuilder.Length < cdataStr.Length)
						return null;
					return CDataState;
				}
				context.KeywordBuilder.Length = 0;
				break;

			case DOCTYPE:
				string docTypeStr = "OCTYPE";
				if (c == docTypeStr [context.KeywordBuilder.Length]) {
					context.KeywordBuilder.Append (c);
					if (context.KeywordBuilder.Length < docTypeStr.Length)
						return null;
					return DocTypeState;
				} else {
					context.KeywordBuilder.Length = 0;
				}
				break;
			}

			context.Diagnostics?.Add (XmlCoreDiagnostics.MalformedTagOpening,
				TextSpan.FromBounds (context.Position - LengthFromOpenBracket (context), context.Position),
				c
			);

			context.StateTag = FREE;
			return null;
		}

		static int LengthFromOpenBracket (XmlParserContext context)
			=> context.StateTag switch {
				BRACKET => 1,
				BRACKET_EXCLAM => 2,
				COMMENT => 3,
				CDATA or DOCTYPE => 3 + context.KeywordBuilder.Length,
				_ => 1
			};

		public virtual XDocument CreateDocument () => new ();

		public override XmlParserContext? TryRecreateState (XObject xobject, int position)
		{
			return
				TagState.TryRecreateState (xobject, position)
				?? ClosingTagState.TryRecreateState (xobject, position)
				?? CommentState.TryRecreateState (xobject, position)
				?? CDataState.TryRecreateState (xobject, position)
				?? DocTypeState.TryRecreateState (xobject, position)
				?? ProcessingInstructionState.TryRecreateState (xobject, position)
				?? TextState.TryRecreateState (xobject, position)
				?? new (
					currentState: this,
					position: xobject.Span.Start,
					previousState: Parent,
					currentStateLength: 0,
					nodes: NodeStack.FromParents (xobject),
					stateTag: FREE
				);
		}

		internal static bool IsFree (XmlParserContext context) => context.CurrentState is XmlRootState && context.StateTag == FREE;
		internal static bool MaybeTag (XmlParserContext context) => context.CurrentState is XmlRootState && context.StateTag == BRACKET;
		internal static bool MaybeCData (XmlParserContext context) => context.CurrentState is XmlRootState && context.StateTag == CDATA;
		internal static bool MaybeDocType (XmlParserContext context) => context.CurrentState is XmlRootState && context.StateTag == DOCTYPE;
		internal static bool MaybeComment (XmlParserContext context) => context.CurrentState is XmlRootState && context.StateTag == COMMENT;
		internal static bool MaybeCDataOrCommentOrDocType (XmlParserContext context) => context.CurrentState is XmlRootState && context.StateTag == BRACKET_EXCLAM;
		internal static bool IsNotFree (XmlParserContext context) => context.CurrentState is XmlRootState && context.StateTag != FREE;
	}
}
