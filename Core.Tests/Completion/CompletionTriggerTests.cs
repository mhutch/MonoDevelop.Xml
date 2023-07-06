// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
		// params are: document text, typedChar, trigger reason, trigger result
		//    typedChar, trigger reason and length can be omitted
		//    typedChar defaults to \0
		//    trigger reason defaults to insertion if typedChar is non-null, else invocation
		// the document text may use the following optional marker chars:
		//    | - cursor position, if not provided, defaults to end of document
		//    ^ - span start. if not provided, defaults to cursor position
		//    $ - span end. if not provided, defaults to cursor position, unless trigger reason is 'insertion', in which case it defaults to cursor position + 1
		[TestCase ("", XmlCompletionTrigger.ElementValue)]
		[TestCase("^<", XmlCompletionTrigger.Tag)]
		[TestCase("", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase("<^a|", XmlTriggerReason.TypedChar, XmlCompletionTrigger.ElementName)]
		[TestCase("<^abc|de$ ", XmlCompletionTrigger.ElementName)]
		[TestCase("<abc", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase("^<", XmlTriggerReason.Backspace, XmlCompletionTrigger.Tag)]
		[TestCase ("^<", XmlTriggerReason.TypedChar, XmlCompletionTrigger.Tag)]
		[TestCase ("sometext^<", XmlTriggerReason.TypedChar, XmlCompletionTrigger.Tag)]
		[TestCase ("sometext", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("a", XmlTriggerReason.TypedChar, XmlCompletionTrigger.None)]
		[TestCase("\"\"", XmlTriggerReason.TypedChar, XmlCompletionTrigger.None)]
		[TestCase("<foo\"", XmlTriggerReason.TypedChar, XmlCompletionTrigger.None)]
		[TestCase("^<|foo$ bar", XmlTriggerReason.Invocation, XmlCompletionTrigger.Tag)]
		[TestCase("^<|!--$ bar", XmlTriggerReason.Invocation, XmlCompletionTrigger.Tag)]
		[TestCase("^<|![CDATA[$ bar", XmlTriggerReason.Invocation, XmlCompletionTrigger.Tag)]
		[TestCase ("<foo ^|", XmlTriggerReason.TypedChar, XmlCompletionTrigger.AttributeName)]
		[TestCase ("<foo bar='1'  ^|  ", XmlTriggerReason.TypedChar, XmlCompletionTrigger.AttributeName)]
		[TestCase ("<foo ^| ", XmlCompletionTrigger.AttributeName)]
		[TestCase ("<foo ^a|bc$ ", XmlCompletionTrigger.AttributeName)]
		[TestCase ("<foo ^bar|baz$=", XmlCompletionTrigger.AttributeName)]
		[TestCase ("^&", XmlTriggerReason.TypedChar, XmlCompletionTrigger.Entity)]
		[TestCase ("^&", XmlCompletionTrigger.Entity)]
		[TestCase ("&a", XmlTriggerReason.TypedChar, XmlCompletionTrigger.Entity)]
		[TestCase ("^&", XmlTriggerReason.Backspace, XmlCompletionTrigger.Entity)]
		[TestCase ("^&blah", XmlCompletionTrigger.Entity)]
		[TestCase ("&blah", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo &", XmlTriggerReason.TypedChar, XmlCompletionTrigger.None)]
		[TestCase ("<foo bar='&", XmlTriggerReason.TypedChar, XmlCompletionTrigger.Entity)]
		[TestCase ("^<!", XmlTriggerReason.TypedChar, XmlCompletionTrigger.DeclarationOrCDataOrComment)]
		[TestCase ("^<!", XmlCompletionTrigger.DeclarationOrCDataOrComment)]
		[TestCase ("^<!DOCTYPE foo", XmlCompletionTrigger.DocType)]
		[TestCase ("^<!DOC", XmlCompletionTrigger.DocType)]
		[TestCase ("<foo bar=\"", XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='", XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='", XmlTriggerReason.Backspace, XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='abc", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=\"^|", XmlTriggerReason.TypedChar, XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar='1'   ^|  ", XmlTriggerReason.TypedChar, XmlCompletionTrigger.AttributeName)]
		[TestCase ("<foo bar='^wxyz|a12$'", XmlCompletionTrigger.AttributeValue)]
		[TestCase ("<foo bar=wxyz", XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=wxyz", XmlTriggerReason.Backspace, XmlCompletionTrigger.None)]
		[TestCase ("<foo bar=wxyz'", XmlTriggerReason.TypedChar, XmlCompletionTrigger.None)]
		public void TriggerTests (object[] args)
		{
			int argIdx = 0;

			//first arg is the doc
			if (args.Length == 0 || args[argIdx++] is not string doc) {
				throw new ArgumentException ("First argument must be an string");
			}
			if (argIdx < args.Length && args[argIdx] is XmlTriggerReason triggerReason) {
				argIdx++;
			} else {
				triggerReason = XmlTriggerReason.Invocation;
			}
			if (argIdx != args.Length - 1 || args[argIdx] is not XmlCompletionTrigger expectedTrigger) {
				throw new ArgumentException ("Last argument must be an XmlCompletionTrigger");
			}

			var caretMarkerIndex = doc.IndexOf ('|');
			if (caretMarkerIndex > -1) {
				doc = doc.Remove (caretMarkerIndex, 1);
			}
			var spanStartMarkerIndex = doc.IndexOf ('^');
			if (spanStartMarkerIndex > -1) {
				doc = doc.Remove (spanStartMarkerIndex, 1);
				if (caretMarkerIndex > -1 && caretMarkerIndex >= spanStartMarkerIndex) {
					caretMarkerIndex--;
				}
			}
			var spanEndMarkerIndex = doc.IndexOf ('$');
			if (spanEndMarkerIndex > -1) {
				doc = doc.Remove (spanEndMarkerIndex, 1);
				if (caretMarkerIndex > -1 && caretMarkerIndex >= spanEndMarkerIndex) {
					caretMarkerIndex--;
				}
				if (spanStartMarkerIndex > -1 && spanStartMarkerIndex >= spanEndMarkerIndex) {
					spanStartMarkerIndex--;
				}
			}

			var triggerPos = caretMarkerIndex > -1 ? caretMarkerIndex : doc.Length;

			int expectedSpanStart = spanStartMarkerIndex > -1 ? spanStartMarkerIndex : (triggerReason == XmlTriggerReason.TypedChar)? triggerPos - 1 : triggerPos;

			int expectedSpanEnd = spanEndMarkerIndex > -1 ? spanEndMarkerIndex : triggerPos;
			int expectedSpanLength = expectedSpanEnd - expectedSpanStart;

			char typedChar = triggerReason == XmlTriggerReason.TypedChar ? doc[triggerPos - 1] : '\0';

			var spine = new XmlSpineParser (new XmlRootState ());
			for (int i = spine.Position; i < triggerPos; i++) {
				spine.Push (doc [i]);
			}

			var result = XmlCompletionTriggering.GetTriggerAndSpan (spine, triggerReason, typedChar, new StringTextSource (doc));

			Assert.AreEqual (expectedTrigger, result.kind);
			if (expectedTrigger != XmlCompletionTrigger.None) {
				Assert.AreEqual (expectedSpanStart, result.spanStart);
				Assert.AreEqual (expectedSpanLength, result.spanLength);
			}
		}
	}
}
