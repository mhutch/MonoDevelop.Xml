// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Completion
{
	class XmlCompletionTriggering
	{
		//FIXME: the length should do a readahead to capture the whole token
		public static (XmlCompletionTrigger kind, int length) GetTrigger (XmlSpineParser parser, XmlTriggerReason reason, char typedCharacter)
		{
			bool isExplicit = reason == XmlTriggerReason.Invocation;
			bool isTypedChar = reason == XmlTriggerReason.TypedChar;
			bool isBackspace = reason == XmlTriggerReason.Backspace;
			if (isTypedChar) {
				Debug.Assert (typedCharacter != '\0');
			}

			var context = parser.GetContext ();

			// explicit invocation in element name
			if (isExplicit && context.CurrentState is XmlNameState && context.CurrentState.Parent is XmlTagState) {
				int length = context.CurrentStateLength;
				return (XmlCompletionTrigger.Element, length);
			}

			//auto trigger after < in free space
			if ((isTypedChar || isBackspace) && XmlRootState.MaybeTag (context)) {
				return (XmlCompletionTrigger.Element, 0);
			}

			//auto trigger after typing first char after < or fist char of attribute
			if (isTypedChar && context.CurrentStateLength == 1 && context.CurrentState is XmlNameState && XmlChar.IsFirstNameChar (typedCharacter)) {
				if (context.CurrentState.Parent is XmlTagState) {
					return (XmlCompletionTrigger.Element, 1);
				}
				if (context.CurrentState.Parent is XmlAttributeState) {
					return (XmlCompletionTrigger.Attribute, 1);
				}
			}

			// trigger on explicit invocation after <
			if (isExplicit && XmlRootState.MaybeTag (context)) {
				return (XmlCompletionTrigger.Element, 0);
			}

			//doctype/cdata completion, explicit trigger after <! or type ! after <
			if ((isExplicit || typedCharacter == '!') && XmlRootState.MaybeCDataOrCommentOrDocType (context)) {
				return (XmlCompletionTrigger.DeclarationOrCDataOrComment, 2);
			}

			//explicit trigger in existing doctype
			if (isExplicit && (XmlRootState.MaybeDocType (context) || context.Nodes.Peek () is XDocType)) {
				int length = context.CurrentState is XmlRootState ? context.CurrentStateLength : context.Position - ((XDocType)context.Nodes.Peek ()).Span.Start;
				return (XmlCompletionTrigger.DocType, length);
			}

			//explicit trigger in attribute name
			if (isExplicit && context.CurrentState is XmlNameState && context.CurrentState.Parent is XmlAttributeState) {
				return (XmlCompletionTrigger.Attribute, context.CurrentStateLength);
			}

			//typed space or explicit trigger in tag
			if ((isExplicit || typedCharacter == ' ') && XmlTagState.IsFree (context)) {
				return (XmlCompletionTrigger.Attribute, 0);
			}

			//attribute value completion
			if (XmlAttributeValueState.GetDelimiterChar (context).HasValue) {
				//auto trigger on quote regardless
				if (context.CurrentStateLength == 1) {
					return (XmlCompletionTrigger.AttributeValue, 0);
				}
				if (isExplicit) {
					return (XmlCompletionTrigger.AttributeValue, context.CurrentStateLength - 1);
				}
			}

			//entity completion
			if (context.CurrentState is XmlTextState || context.CurrentState is XmlAttributeValueState) {
				if (typedCharacter == '&')
					return (XmlCompletionTrigger.Entity, 0);

				var text = parser.GetContext ().KeywordBuilder;

				if (isBackspace && text[text.Length-1] == '&') {
					return (XmlCompletionTrigger.Entity, 0);
				}

				if (isExplicit) {
					for (int i = 0; i < text.Length; i++) {
						var c = text[text.Length - i - 1];
						if (c == '&') {
							return (XmlCompletionTrigger.Entity, i);
						}
						if (!XmlChar.IsNameChar (c)) {
							break;
						}
					}
				}
			}

			//explicit invocation in free space
			if (isExplicit && (
				context.CurrentState is XmlTextState
				|| XmlRootState.IsFree (context)
			)) {
				return (XmlCompletionTrigger.ElementWithBracket, 0);
			}

			return (XmlCompletionTrigger.None, 0);
		}
	}

	enum XmlCompletionTrigger
	{
		None,
		Element,
		ElementWithBracket,
		Attribute,
		AttributeValue,
		Entity,
		DocType,
		DeclarationOrCDataOrComment
	}

	public enum XmlTriggerReason
	{
		Invocation,
		TypedChar,
		Backspace
	}
}
