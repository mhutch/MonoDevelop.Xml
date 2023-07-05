// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;
using MonoDevelop.Xml.Tests.Parser;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace MonoDevelop.Xml.Tests.Completion
{
	[TestFixture]
	public class CompletionTriggerTests
	{
		[Test]
		// params are: document text, typedChar, trigger reason, trigger result, length
		//    typedChar, trigger reason and length can be omitted
		//    typedChar defaults to \0
		//    trigger reason defaults to insertion if typedChar is non-null, else invocation
		//    length defaults to 0
		//    if typedChar is provided, it's added to the document text
		[TestCase ("", XmlCompletionTrigger.ElementValue, 0)]
		[TestCase("<", XmlCompletionTrigger.Tag, 0, XmlReadForward.TagStart)]
		[TestCase("", XmlTriggerReason.Backspace, XmlCompletionTrigger.None, 0)]
		[TestCase("<", 'a', XmlCompletionTrigger.ElementName, 1)]
		[TestCase("<abc", XmlCompletionTrigger.ElementName, 1, XmlReadForward.XmlName)]
		[TestCase("<abc", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase("<", XmlTriggerReason.Backspace, XmlCompletionTrigger.Tag, 0)]
		[TestCase ("", '<', XmlCompletionTrigger.Tag, 0)]
		[TestCase ("sometext", '<', XmlCompletionTrigger.Tag, 8)]
		[TestCase ("sometext", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("", 'a', XmlCompletionTrigger.None)]
		[TestCase("\"", '"', XmlCompletionTrigger.None)]
		[TestCase("<foo", '"', XmlCompletionTrigger.None)]
		[TestCase ("<foo", ' ', XmlCompletionTrigger.AttributeName, 5)]
		[TestCase ("<foo bar='1'   ", ' ', XmlCompletionTrigger.AttributeName, 16, XmlReadForward.None)]
		[TestCase ("<foo ", XmlCompletionTrigger.AttributeName, XmlReadForward.XmlName)]
		[TestCase ("<foo a", XmlCompletionTrigger.AttributeName, 5, XmlReadForward.XmlName)]
		[TestCase ("<foo bar", XmlCompletionTrigger.AttributeName, 5, XmlReadForward.XmlName)]
		[TestCase ("", '&', XmlCompletionTrigger.Entity)]
		[TestCase ("&", XmlCompletionTrigger.Entity, 0, XmlReadForward.Entity)]
		[TestCase ("&", 'a', XmlCompletionTrigger.None)]
		[TestCase ("&", XmlTriggerReason.Backspace, XmlCompletionTrigger.Entity, 0)]
		[TestCase ("&blah", XmlCompletionTrigger.Entity, 0, XmlReadForward.Entity)]
		[TestCase ("&blah", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo ", '&', XmlCompletionTrigger.None)]
		[TestCase ("<foo bar='", '&', XmlCompletionTrigger.Entity)]
		[TestCase ("<", '!', XmlCompletionTrigger.DeclarationOrCDataOrComment, 2)]
		[TestCase ("<!", XmlCompletionTrigger.DeclarationOrCDataOrComment, 2)]
		[TestCase ("<!DOCTYPE foo", XmlCompletionTrigger.DocType, 0, XmlReadForward.DocType)]
		[TestCase ("<!DOC", XmlCompletionTrigger.DocType, 0, XmlReadForward.DocType)]
		[TestCase ("<foo bar=\"", XmlCompletionTrigger.AttributeValue, XmlReadForward.AttributeValue)]
		[TestCase ("<foo bar='", XmlCompletionTrigger.AttributeValue, XmlReadForward.AttributeValue)]
		[TestCase ("<foo bar='", XmlTriggerReason.Backspace, XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='abc", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=", '"', XmlCompletionTrigger.AttributeValue, 10)]
		[TestCase ("<foo bar=", '\'', XmlCompletionTrigger.AttributeValue, 10)]
		[TestCase ("<foo bar='wxyz", XmlCompletionTrigger.AttributeValue, 10, XmlReadForward.AttributeValue)]
		[TestCase ("<foo bar=wxyz", XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=wxyz", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=wxyz", '\'', XmlCompletionTrigger.None)]
		public void TriggerTests (object[] args)
		{
			int argIdx = 0;
			bool TryGetArg<T> (out T result, T defaultVal) where T: notnull
			{
				if (argIdx < args.Length && args[argIdx] is T r) {
					argIdx++;
					result = r;
					return true;
				}
				result = defaultVal;
				return false;
			}

			//first arg is the doc
			string doc = (string)args[argIdx++];

			//next arg can be typed char or a trigger reason
			XmlTriggerReason reason;
			if (TryGetArg (out char typedChar, defaultVal: '\0')) {
				reason = XmlTriggerReason.TypedChar;
			} else{
				TryGetArg (out reason, defaultVal: XmlTriggerReason.Invocation);
			}

			int triggerPos = doc.Length;

			XmlCompletionTrigger expectedTrigger = (XmlCompletionTrigger) args[argIdx++];
			TryGetArg (out int expectedSpanStart, defaultVal: triggerPos);
			TryGetArg (out XmlReadForward expectedReadForward, defaultVal: XmlReadForward.None);

			if (typedChar != '\0') {
				doc += typedChar;
			}

			var spine = new XmlSpineParser (new XmlRootState ());
			spine.Parse (doc);

			var result = XmlCompletionTriggering.GetTrigger (spine, reason, typedChar);
			Assert.AreEqual (expectedTrigger, result.kind);
			if (expectedTrigger != XmlCompletionTrigger.None) {
				Assert.AreEqual (expectedSpanStart, result.spanStart);
				Assert.AreEqual (expectedReadForward, result.spanReadForward);
			}
		}
	}
}
