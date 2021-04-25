// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Classification;
using MonoDevelop.Xml.Tests.Completion;
using MonoDevelop.Xml.Tests.EditorTestHelpers;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests
{
	[TestFixture]
	public class ClassifierTests : EditorTestBase
	{
		protected override string ContentTypeName => XmlContentTypeNames.XmlCore;

		protected override (EditorEnvironment, EditorCatalog) InitializeEnvironment () => XmlTestEnvironment.EnsureInitialized ();

		[Test]

		[TestCase("<!--x-->", @"[0..2) xml - delimiter
[2..7) xml - comment
[7..8) xml - delimiter")]

		[TestCase(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<X>
<n:T></n:T>
<X/>
<A.B></A.B>
<A B=""a""></A>
<A>&#x03C0;</A>
<A>a &lt;</A>
<A><![CDATA[b]]></A>
<!-- c -->
</X>",
@"[0..1) xml - delimiter
[1..54) xml - processing instruction
[54..55) xml - delimiter
[55..57) text
[57..58) xml - delimiter
[58..59) xml - name
[59..60) xml - delimiter
[60..62) text
[62..63) xml - delimiter
[63..66) xml - name
[66..69) xml - delimiter
[69..72) xml - name
[72..73) xml - delimiter
[73..75) text
[75..76) xml - delimiter
[76..77) xml - name
[77..79) xml - delimiter
[79..81) text
[81..82) xml - delimiter
[82..85) xml - name
[85..88) xml - delimiter
[88..91) xml - name
[91..92) xml - delimiter
[92..94) text
[94..95) xml - delimiter
[95..96) xml - name
[96..97) text
[97..98) xml - attribute name
[98..99) xml - delimiter
[99..100) xml - attribute quotes
[100..101) xml - attribute value
[101..102) xml - attribute quotes
[102..105) xml - delimiter
[105..106) xml - name
[106..107) xml - delimiter
[107..109) text
[109..110) xml - delimiter
[110..111) xml - name
[111..112) xml - delimiter
[112..114) xml - entity reference
[114..119) xml - text
[119..120) xml - entity reference
[120..122) xml - delimiter
[122..123) xml - name
[123..124) xml - delimiter
[124..126) text
[126..127) xml - delimiter
[127..128) xml - name
[128..129) xml - delimiter
[129..131) xml - text
[131..132) xml - entity reference
[132..134) xml - text
[134..135) xml - entity reference
[135..137) xml - delimiter
[137..138) xml - name
[138..139) xml - delimiter
[139..141) text
[141..142) xml - delimiter
[142..143) xml - name
[143..153) xml - delimiter
[153..155) xml - cdata section
[155..159) xml - delimiter
[159..160) xml - name
[160..161) xml - delimiter
[161..163) text
[163..165) xml - delimiter
[165..172) xml - comment
[172..173) xml - delimiter
[173..175) text
[175..177) xml - delimiter
[177..178) xml - name
[178..179) xml - delimiter")]

		public void TestClassifier (string xml, string expectedClassifications)
		{
			var provider = new XmlClassifierProvider (Catalog.ClassificationTypeRegistryService);
			var buffer = base.CreateTextBuffer (xml);
			var classifier = new XmlClassifier (buffer, provider.Types);
			var snapshot = buffer.CurrentSnapshot;

			var classificationSpans = classifier.GetClassificationSpans (new SnapshotSpan (snapshot, 0, snapshot.Length));
			string actual = GetClassificationsText (classificationSpans);
			Assert.AreEqual (expectedClassifications, actual);

			var snapshotSpans = SplitIntoSpans (snapshot);
			classificationSpans = Join (snapshotSpans.Select (s => classifier.GetClassificationSpans (s)));
			actual = GetClassificationsText (classificationSpans);
			Assert.AreEqual (expectedClassifications, actual);
		}

		private static string GetClassificationsText (IList<ClassificationSpan> classificationSpans)
		{
			return string.Join ("\r\n", classificationSpans
				.Select (s => $"{s.Span.Span} {s.ClassificationType.Classification}"));
		}

		private IList<ClassificationSpan> Join(IEnumerable<IList<ClassificationSpan>> spanLists)
		{
			var list = new List<ClassificationSpan> ();

			foreach (var spanList in spanLists) {
				if (spanList.Count == 0) {
					continue;
				}

				int startIndex = 0;

				if (list.Count > 0) {
					var last = list[list.Count - 1];
					var firstOfCurrent = spanList[0];
					if (last.ClassificationType == firstOfCurrent.ClassificationType) {
						var joinedSpan = new SnapshotSpan (last.Span.Start, last.Span.Length + firstOfCurrent.Span.Length);
						list[list.Count - 1] = new ClassificationSpan (joinedSpan, last.ClassificationType);
						startIndex = 1;
					}
				}

				for (int i = startIndex; i < spanList.Count; i++) {
					list.Add (spanList[i]);
				}
			}

			return list;
		}

		private IEnumerable<SnapshotSpan> SplitIntoSpans(ITextSnapshot snapshot, int spanLength = 10)
		{
			int start = 0;
			while (start < snapshot.Length) {
				int length = Math.Min (spanLength, snapshot.Length - start);
				var span = new SnapshotSpan (snapshot, start, length);
				yield return span;
				start = start + length;
			}
		}
	}
}