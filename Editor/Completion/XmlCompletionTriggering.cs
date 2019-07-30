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
		public static (XmlCompletionTrigger kind, int length) GetTrigger (XmlParser spine, XmlTriggerReason reason, char typedCharacter)
		{
			int stateTag = ((IXmlParserContext)spine).StateTag;
			bool isExplicit = reason == XmlTriggerReason.Invocation;
			bool isTypedChar = reason == XmlTriggerReason.TypedChar;
			bool isBackspace = reason == XmlTriggerReason.Backspace;
			Debug.Assert (!isTypedChar || typedCharacter == '\0');

			// explicit invocation in element name
			if (isExplicit && spine.CurrentState is XmlNameState && spine.Nodes.Peek () is XElement el && !el.IsNamed) {
				int length = spine.CurrentStateLength;
				return (XmlCompletionTrigger.Element, length);
			}

			//auto trigger after < in free space
			if (spine.CurrentState is XmlRootState && stateTag == XmlRootState.BRACKET) {
				return (XmlCompletionTrigger.Element, 0);
			}

			// trigger on explicit invocation after <
			if (isExplicit && spine.CurrentState is XmlRootState && stateTag == XmlRootState.BRACKET) {
				return (XmlCompletionTrigger.Element, 0);
			}

			//doctype/cdata completion, explicit trigger after <! or type ! after <
			if ((isExplicit || typedCharacter == '!') && spine.CurrentState is XmlRootState && stateTag == XmlRootState.BRACKET_EXCLAM) {
				return (XmlCompletionTrigger.DocTypeOrCData, 2);
			}

			//explicit trigger in existing doctype
			if (isExplicit && ((spine.CurrentState is XmlRootState && stateTag == XmlRootState.DOCTYPE) || spine.Nodes.Peek () is XDocType)) {
				int length = spine.CurrentState is XmlRootState ? spine.CurrentStateLength : spine.Position - ((XDocType)spine.Nodes.Peek ()).Span.Start;
				return (XmlCompletionTrigger.DocType, length);
			}

			//explicit trigger in attribute name
			if (isExplicit && spine.CurrentState is XmlNameState && spine.CurrentState.Parent is XmlAttributeState) {
				return (XmlCompletionTrigger.Attribute, spine.CurrentStateLength);
			}

			//typed space or explicit trigger in tag
			if ((isExplicit || typedCharacter == ' ') && spine.CurrentState is XmlTagState && stateTag == XmlTagState.FREE) {
				return (XmlCompletionTrigger.Attribute, 0);
			}

			//attribute value completion
			if (spine.CurrentState is XmlAttributeValueState) {
				var kind = stateTag & XmlAttributeValueState.TagMask;
				if (kind == XmlAttributeValueState.DOUBLEQUOTE || kind == XmlAttributeValueState.SINGLEQUOTE) {
					//auto trigger on quote regardless
					if (spine.CurrentStateLength == 1) {
						return (XmlCompletionTrigger.AttributeValue, 0);
					}
					if (isExplicit) {
						return (XmlCompletionTrigger.AttributeValue, spine.CurrentStateLength - 1);
					}
				}
			}

			//entity completion
			if (spine.CurrentState is XmlTextState || spine.CurrentState is XmlAttributeValueState) {
				if (typedCharacter == '&')
					return (XmlCompletionTrigger.Entity, 0);

				var text = ((IXmlParserContext)spine).KeywordBuilder;

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
				spine.CurrentState is XmlTextState
				|| (spine.CurrentState is XmlRootState && stateTag == XmlRootState.FREE)
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
		DocTypeOrCData
	}

	public enum XmlTriggerReason
	{
		Invocation,
		TypedChar,
		Backspace
	}
}
