//
// ExpandSelectionTests.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2018 Microsoft Corp
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Editor.Completion;

using NUnit.Framework;

namespace MonoDevelop.Xml.Editor.Tests.Commands
{
	[TestFixture]
	public class ExpandSelectionTests : XmlEditorTest
	{
		public ExpandSelectionTests () { }

		const string Document = @"<!-- this is
a comment-->
<foo hello=""hi"" goodbye=""bye"">
	this is some text
	<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>
</foo>
";
		const string CommentDoc = @"<!-- this is
a comment-->";

		const string ElementFoo = @"<foo hello=""hi"" goodbye=""bye"">";

		const string ElementWithBodyFoo = @"<foo hello=""hi"" goodbye=""bye"">
	this is some text
	<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>
</foo>";

		const string TextNode = "this is some text";

		const string TextNodeWithWhitespace = @"	this is some text
";

		const string AttributesFoo = @"hello=""hi"" goodbye=""bye""";

		const string AttributeHello = @"hello=""hi""";

		const string AttributeGoodbye = @"goodbye=""bye""";

		const string BodyFoo = @"
	this is some text
	<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>
";
		const string ElementBar = @"<bar>";

		const string ElementWithBodyBar = @"<bar><baz thing=""done"" />
		<!--another comment-->
	</bar>";

		const string BodyBar = @"<baz thing=""done"" />
		<!--another comment-->
	";

		const string ElementBaz= @"<baz thing=""done"" />";

		const string AttributeThing = @"thing=""done""";

		const string CommentBar = @"<!--another comment-->";

		const string CommentBarWithWhitespace = @"		<!--another comment-->
";

		//args are document, line, col, then the expected sequence of expansions
		[Test]
		[TestCase (Document, 1, 2, CommentDoc)]
		[TestCase (Document, 3, 2, "foo", ElementFoo, ElementWithBodyFoo)]
		[TestCase (Document, 3, 3, "foo", ElementFoo, ElementWithBodyFoo)]
		[TestCase (Document, 3, 15, "hi", AttributeHello, AttributesFoo, ElementFoo, ElementWithBodyFoo)]
		[TestCase (Document, 3, 7, "hello", AttributeHello, AttributesFoo, ElementFoo, ElementWithBodyFoo)]
		[TestCase (Document, 4, 7, "is", TextNode, TextNodeWithWhitespace, BodyFoo, ElementWithBodyFoo)]
		[TestCase (Document, 5, 22, "done", AttributeThing, ElementBaz, BodyBar, ElementWithBodyBar, BodyFoo, ElementWithBodyFoo)]
		[TestCase (Document, 6, 12, CommentBar, CommentBarWithWhitespace, BodyBar, ElementWithBodyBar, BodyFoo, ElementWithBodyFoo)]
		public async Task TestExpandShrink (object[] args)
		{
			var buffer = CreateTextBuffer ((string)args[0]);
			var parser = GetParser (buffer);
			var snapshot = buffer.CurrentSnapshot;
			var navigator = Catalog.TextStructureNavigatorSelectorService.GetTextStructureNavigator (buffer);

			var line = snapshot.GetLineFromLineNumber ((int)args[1] - 1);
			var offset = line.Start + (int)args[2] - 1;

			// it's extremely unlikely the parser will hve an up to date parse result yet
			// so this should use the spine parser codepath

			SnapshotSpan Span(int s, int l) => new SnapshotSpan (snapshot, s, l);

			var span = Span (offset, 0);

			//check expanding causes correct selections
			for (int i = 3; i < args.Length; i++) {
				span = navigator.GetSpanOfEnclosing (span);
				var text = snapshot.GetText (span);
				Assert.AreEqual (args[i], text);
			}

			//check entire doc is selected
			span = navigator.GetSpanOfEnclosing (span);
			Assert.AreEqual (0, span.Start.Position);
			Assert.AreEqual (snapshot.Length, span.Length);

			// now repeat the tests with an up to date parse result
			await parser.GetOrProcessAsync (snapshot, CancellationToken.None);

			span = Span (offset, 0);

			//check expanding causes correct selections
			for (int i = 3; i < args.Length; i++) {
				span = navigator.GetSpanOfEnclosing (span);
				var text = snapshot.GetText (span);
				Assert.AreEqual (args[i], text);
			}

			//check entire doc is selected
			span = navigator.GetSpanOfEnclosing (span);
			Assert.AreEqual (0, span.Start.Position);
			Assert.AreEqual (snapshot.Length, span.Length);
		}
	}
}
