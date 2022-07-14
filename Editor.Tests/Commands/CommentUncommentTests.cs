// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Commands;
using MonoDevelop.Xml.Editor.Completion;

using NUnit.Framework;

namespace MonoDevelop.Xml.Editor.Tests.Commands
{
	[TestFixture]
	public class CommentUncommentTests : XmlEditorTest
	{
		public const char VirtualSpaceMarker = '→';
		public const char SelectionStartMarker = '{';
		public const char SelectionEndMarker = '}';

		[Test]

		[TestCase (@"{}<x>
</x>", @"<!--<x>
</x>-->")]

		[TestCase (@"<x>{}
</x>", @"<!--<x>
</x>-->")]

		[TestCase (@"{<x>}
</x>", @"<!--<x>
</x>-->")]

		[TestCase (@"{<x>
</x>}", @"<!--<x>
</x>-->")]

		[TestCase (@"<x>
{}</x>", @"<!--<x>
</x>-->")]

		[TestCase (@"<x>
  {<a></a>}
</x>", @"<x>
  <!--<a></a>-->
</x>")]

		[TestCase (@"<x>
  <a></a>{}
</x>", @"<x>
  <!--<a></a>-->
</x>")]

		[TestCase (@"<x>
  <a></a>{}
  <!--c-->
</x>", @"<x>
  <!--<a></a>-->
  <!--c-->
</x>")]

		[TestCase (@"{}<x>
  <a></a>
  <!--c-->
</x>", @"<!--<x>
  <a></a>
  --><!--c--><!--
</x>-->", false)]

		[TestCase (@"{}<x />", @"<!--<x />-->")]

		[TestCase (@"<x />{}", @"<!--<x />-->")]

		[TestCase (@"<x
{a}=""a""/>", @"<!--<x
a=""a""/>-->")]

		[TestCase (@"{}<?xml ?>", @"<!--<?xml ?>-->")]

		[TestCase (@"{}<!--x-->", @"<!--x-->", false)]

		[TestCase (@"{<x/>
<x/>}", @"<!--<x/>
<x/>-->")]

		[TestCase (@"<{x/>
<}x/>", @"<!--<x/>
<x/>-->")]

		[TestCase (@"<x>
  {text}
<x/>", @"<x>
  <!--text-->
<x/>")]

		[TestCase (@"<x>
  {text}
  more text
<x/>", @"<x>
  <!--text-->
  more text
<x/>")]

		[TestCase (@"<x>
  text
  {<a/>}
  more text
<x/>", @"<x>
  text
  <!--<a/>-->
  more text
<x/>")]

		[TestCase (@"<x>
  {text
  <a/>}
  more text
<x/>", @"<x>
  <!--text
  <a/>-->
  more text
<x/>")]

		[TestCase (@"<x>
{  <a/>
  <a/>
}<x/>", @"<x>
<!--  <a/>
  <a/>-->
<x/>")]

		[TestCase (@"<x>
  {}
<x/>", @"<x>
  <!---->
<x/>")]

		[TestCase (@"<x>
→→{}
<x/>", @"<x>
  <!---->
<x/>")]

		[TestCase (@"<x>
  {<a/>}
  {<a/>}
<x/>", @"<x>
  <!--<a/>-->
  <!--<a/>-->
<x/>")]

		[TestCase (@"<x>
  {<a><!-- comment --></a>}
  {<a></a>}
<x/>", @"<x>
  <!--<a>--><!-- comment --><!--</a>-->
  <!--<a></a>-->
<x/>", false)]

		[TestCase (@"<x>
  {<![CDATA[bar]]>}
<x/>", @"<x>
  <!--<![CDATA[bar]]>-->
<x/>", false)]

		public void TestComment (string sourceText, string expectedText, bool toggle = true)
		{
			var (buffer, snapshotSpans, document) = GetBufferSpansAndDocument (sourceText);

			CommentUncommentCommandHandler.CommentSelection (buffer, snapshotSpans, document);

			var actualText = buffer.CurrentSnapshot.GetText ();

			Assert.AreEqual (expectedText, actualText);

			// toggle should also work in most scenarios for comment
			if (toggle) {
				TestToggle (sourceText, expectedText);
			}
		}

		[Test]

		[TestCase (@"{}<!--<x>
</x>-->", @"<x>
</x>")]

		[TestCase (@"{<!--<x>
</x>-->}", @"<x>
</x>")]

		[TestCase (@"{<x>
</x>}", @"<x>
</x>", false)]

		[TestCase (@"<x>
  {<!-- text -->}
</x>", @"<x>
   text 
</x>")]

		[TestCase (@"{}<!--<x>
  --><!-- text --><!--
</x>-->", @"<x>
  <!-- text --><!--
</x>-->", false)]

		[TestCase (@"<x>
  {<}!--<a/>-->
  <b/>
  <!--<a/>-->{}
  {<!--<a/>-->}
</x>", @"<x>
  <a/>
  <b/>
  <a/>
  <a/>
</x>", false)]

		public void TestUncomment (string sourceText, string expectedText, bool toggle = true)
		{
			var (buffer, snapshotSpans, document) = GetBufferSpansAndDocument (sourceText);

			CommentUncommentCommandHandler.UncommentSelection (buffer, snapshotSpans, document);

			var actualText = buffer.CurrentSnapshot.GetText ();

			Assert.AreEqual (expectedText, actualText);

			// toggle should also work in most scenarios for uncomment
			if (toggle) {
				TestToggle (sourceText, expectedText);
			}
		}

		void TestToggle (string sourceText, string expectedText)
		{
			var (buffer, snapshotSpans, document) = GetBufferSpansAndDocument (sourceText);

			CommentUncommentCommandHandler.ToggleCommentSelection (buffer, snapshotSpans, document);

			var actualText = buffer.CurrentSnapshot.GetText ();

			Assert.AreEqual (expectedText, actualText);
		}

		(ITextBuffer buffer, IEnumerable<VirtualSnapshotSpan> virtualSnapshotSpans, XDocument document) GetBufferSpansAndDocument (string sourceText)
		{
			var (text, spans) = GetTextAndSpans (sourceText);
			var buffer = CreateTextBuffer (text);
			var parser = XmlBackgroundParser.GetParser<XmlBackgroundParser> (buffer);

			var snapshot = buffer.CurrentSnapshot;
			var virtualSnapshotSpans = spans.Select (s => new VirtualSnapshotSpan (
				 new VirtualSnapshotPoint (new SnapshotPoint (snapshot, s.Span.Start), s.VirtualSpacesAtStart),
				 new VirtualSnapshotPoint (new SnapshotPoint (snapshot, s.Span.End), s.VirtualSpacesAtEnd)));
			var document = parser.GetOrProcessAsync (buffer.CurrentSnapshot, default).Result.XDocument;

			return (buffer, virtualSnapshotSpans, document);
		}

		private struct VirtualSpan
		{
			public Span Span;
			public int VirtualSpacesAtStart;
			public int VirtualSpacesAtEnd;
		}

		(string text, IEnumerable<VirtualSpan> spans) GetTextAndSpans(string textWithSpans)
		{
			var spans = new List<VirtualSpan> ();

			var sb = new StringBuilder ();

			int index = 0;
			int start = 0;
			int virtualSpace = 0;
			int virtualSpacesAtStart = 0;
			for (int i = 0; i < textWithSpans.Length; i++) {
				char ch = textWithSpans[i];
				if (ch == VirtualSpaceMarker) {
					virtualSpace++;
					continue;
				}

				if (ch == SelectionStartMarker) {
					start = index;
					virtualSpacesAtStart = virtualSpace;
				} else if (ch == SelectionEndMarker) {
					var span = new VirtualSpan {
						Span = new Span(start, index - start),
						VirtualSpacesAtStart = virtualSpacesAtStart,
						VirtualSpacesAtEnd = virtualSpace
					};
					spans.Add (span);
				} else {
					sb.Append (ch);
					index++;
					virtualSpace = 0;
				}
			}

			return (sb.ToString(), spans);
		}
	}
}