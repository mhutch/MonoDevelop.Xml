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
		public static (XmlCompletionTrigger kind, int spanStart, XmlReadForward spanReadForward) GetTrigger (XmlSpineParser parser, XmlTriggerReason reason, char typedCharacter)
		{
			int triggerPosition = parser.Position;
			bool isExplicit = reason == XmlTriggerReason.Invocation;
			bool isTypedChar = reason == XmlTriggerReason.TypedChar;
			bool isBackspace = reason == XmlTriggerReason.Backspace;
			if (isTypedChar) {
				Debug.Assert (typedCharacter != '\0');
			}

			var context = parser.GetContext ();

			// explicit invocation in element name
			if (isExplicit && context.CurrentState is XmlNameState && context.CurrentState.Parent is XmlTagState) {
				int start = triggerPosition - context.CurrentStateLength;
				return (XmlCompletionTrigger.ElementName, start, XmlReadForward.XmlName);
			}

			//auto trigger after < in free space
			if ((isTypedChar || isBackspace) && XmlRootState.MaybeTag (context)) {
				return (XmlCompletionTrigger.Tag, triggerPosition - 1, XmlReadForward.None);
			}

			//auto trigger after typing first char after < or first char of attribute
			if (isTypedChar && context.CurrentStateLength == 1 && context.CurrentState is XmlNameState && XmlChar.IsFirstNameChar (typedCharacter)) {
				if (context.CurrentState.Parent is XmlTagState) {
					return (XmlCompletionTrigger.ElementName, triggerPosition - 1, XmlReadForward.None);
				}
				if (context.CurrentState.Parent is XmlAttributeState) {
					return (XmlCompletionTrigger.AttributeName, triggerPosition - 1, XmlReadForward.None);
				}
				return (XmlCompletionTrigger.None, 0, XmlReadForward.None);
			}

			// trigger on explicit invocation after <
			if (isExplicit && XmlRootState.MaybeTag (context)) {
				return (XmlCompletionTrigger.Tag, triggerPosition - 1, XmlReadForward.TagStart);
			}

			//doctype/cdata completion, explicit trigger after <! or type ! after <
			if ((isExplicit || typedCharacter == '!') && XmlRootState.MaybeCDataOrCommentOrDocType (context)) {
				return (XmlCompletionTrigger.DeclarationOrCDataOrComment, triggerPosition, XmlReadForward.None);
			}

			//explicit trigger in existing doctype
			if (isExplicit && (XmlRootState.MaybeDocType (context) || context.Nodes.Peek () is XDocType)) {
				int length = context.CurrentState is XmlRootState ? context.CurrentStateLength : context.Position - ((XDocType)context.Nodes.Peek ()).Span.Start;
				return (XmlCompletionTrigger.DocType, triggerPosition - length, XmlReadForward.DocType);
			}

			//explicit trigger in attribute name
			if (isExplicit && context.CurrentState is XmlNameState && context.CurrentState.Parent is XmlAttributeState) {
				return (XmlCompletionTrigger.AttributeName, triggerPosition - context.CurrentStateLength, XmlReadForward.XmlName);
			}

			//typed space or explicit trigger in tag
			if ((isExplicit || typedCharacter == ' ') && XmlTagState.IsFree (context)) {
				return (XmlCompletionTrigger.AttributeName, triggerPosition, isExplicit? XmlReadForward.XmlName : XmlReadForward.None);
			}

			//attribute value completion
			if (XmlAttributeValueState.GetDelimiterChar (context).HasValue) {
				if (isExplicit) {
					return (XmlCompletionTrigger.AttributeValue, triggerPosition - context.CurrentStateLength + 1, XmlReadForward.AttributeValue);
				}
				//auto trigger on quote regardless
				if (context.CurrentStateLength == 1) {
					return (XmlCompletionTrigger.AttributeValue, triggerPosition, XmlReadForward.None);
				}
			}

			//entity completion
			if (context.CurrentState is XmlTextState || context.CurrentState is XmlAttributeValueState) {
				if (typedCharacter == '&')
					return (XmlCompletionTrigger.Entity, triggerPosition - 1, isExplicit? XmlReadForward.Entity : XmlReadForward.None);

				var text = parser.GetContext ().KeywordBuilder;

				if (isBackspace && text.Length > 0 && text[text.Length - 1] == '&') {
					return (XmlCompletionTrigger.Entity, triggerPosition - 1, isExplicit ? XmlReadForward.Entity : XmlReadForward.None);
				}

				if (isExplicit) {
					for (int i = 0; i < text.Length; i++) {
						var c = text[text.Length - i - 1];
						if (c == '&') {
							return (XmlCompletionTrigger.Entity, triggerPosition - i - 1, XmlReadForward.Entity);
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
				return (XmlCompletionTrigger.ElementValue, triggerPosition, XmlReadForward.None);
			}

			return (XmlCompletionTrigger.None, triggerPosition, XmlReadForward.None);
		}
	}

	/// <summary>
	/// Describes how to read forward from the completion span start to get the completion span
	/// </summary>
	enum XmlReadForward
	{
		None,
		XmlName,
		TagStart,
		DocType,
		AttributeValue,
		Entity
	}

	enum XmlCompletionTrigger
	{
		None,

		/// <summary>An XML tag, which may be an element with leading angle bracket, comment etc</summary> 
		Tag,

		ElementName,
		ElementValue,
		AttributeName,
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
