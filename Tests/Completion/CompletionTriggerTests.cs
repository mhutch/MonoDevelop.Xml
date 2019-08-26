// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;
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
		[TestCase ("", XmlCompletionTrigger.ElementWithBracket)]
		[TestCase("<", XmlCompletionTrigger.Element)]
		[TestCase("", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase("<", 'a', XmlCompletionTrigger.Element, 1)]
		[TestCase("<abc", XmlCompletionTrigger.Element, 3)]
		[TestCase("<abc", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase("<", XmlTriggerReason.Backspace, XmlCompletionTrigger.Element)]
		[TestCase ("", '<', XmlCompletionTrigger.Element)]
		[TestCase ("sometext", '<', XmlCompletionTrigger.Element)]
		[TestCase ("sometext", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("", 'a', XmlCompletionTrigger.None)]
		[TestCase("\"", '"', XmlCompletionTrigger.None)]
		[TestCase("<foo", '"', XmlCompletionTrigger.None)]
		[TestCase ("<foo", ' ', XmlCompletionTrigger.Attribute)]
		[TestCase ("<foo bar='1'   ", ' ', XmlCompletionTrigger.Attribute)]
		[TestCase ("<foo ", XmlCompletionTrigger.Attribute)]
		[TestCase ("<foo bar", XmlCompletionTrigger.Attribute, 3)]
		[TestCase ("", '&', XmlCompletionTrigger.Entity)]
		[TestCase ("&", XmlCompletionTrigger.Entity)]
		[TestCase ("&", 'a', XmlCompletionTrigger.None)]
		[TestCase ("&", XmlTriggerReason.Backspace, XmlCompletionTrigger.Entity)]
		[TestCase ("&blah", XmlCompletionTrigger.Entity, 4)]
		[TestCase ("&blah", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo ", '&', XmlCompletionTrigger.None)]
		[TestCase ("<foo bar='", '&', XmlCompletionTrigger.Entity)]
		[TestCase ("<", '!', XmlCompletionTrigger.DeclarationOrCDataOrComment, 2)]
		[TestCase ("<!", XmlCompletionTrigger.DeclarationOrCDataOrComment, 2)]
		[TestCase ("<!DOCTYPE foo", XmlCompletionTrigger.DocType, 13)]
		[TestCase ("<!DOC", XmlCompletionTrigger.DocType, 5)]
		[TestCase ("<foo bar=\"", XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='", XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='", XmlTriggerReason.Backspace, XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='abc", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=", '"', XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar=", '\'', XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='wxyz", XmlCompletionTrigger.AttributeValue, 4)]
		[TestCase ("<foo bar=wxyz", XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=wxyz", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=wxyz", "'", XmlCompletionTrigger.None)]
		public void TriggerTests (object[] args)
		{
			//first arg is the doc
			string doc = (string)args[0];

			XmlTriggerReason reason;
			char typedChar;

			//next arg can be typed char or a trigger reason
			//this would make a nice switch expression w/c#8
			if (args[1] is XmlTriggerReason r) {
				reason = r;
				typedChar = '\0';
			} else if (args[1] is char c) {
				reason = XmlTriggerReason.TypedChar;
				typedChar = c;
			} else {
				reason = XmlTriggerReason.Invocation;
				typedChar = '\0';
			}

			//expected trigger will be last unless length is provided, then it's penultimate
			var expectedTrigger = args[args.Length - 1] is XmlCompletionTrigger
				? args[args.Length - 1]
				: args[args.Length - 2];

			//length is optional, but if provided it's always last
			int expectedLength = args[args.Length - 1] as int? ?? 0;

			if (typedChar != '\0') {
				doc += typedChar;
			}

			var spine = new XmlParser (new XmlRootState (), false);
			spine.Parse (new StringReader (doc));

			var result = XmlCompletionTriggering.GetTrigger (spine, reason, typedChar);
			Assert.AreEqual (expectedTrigger, result.kind);
			Assert.AreEqual (expectedLength, result.length);
		}
	}
}
