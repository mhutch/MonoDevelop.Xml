// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using MonoDevelop.MSBuild.Editor.HighlightReferences;
using MonoDevelop.Xml.Editor.Parsing;
using MonoDevelop.Xml.Editor.Tagging;
using MonoDevelop.Xml.Tests;
using MonoDevelop.Xml.Tests.Utils;

using NUnit.Framework;
using NUnit.Framework.Internal;

namespace MonoDevelop.Xml.Editor.Tests.Tagging;

[TestFixture]
public class HighlightEndTagTaggerTests : HighlightTaggerTest<NavigableHighlightTag,NavigableHighlightTag>
{
	[Test]
	public async Task TestHighlightEndTagger ()
	{
		var text = TextWithMarkers.Parse ("<^foo^><></^foo^>", '^');
		var spans = text.GetMarkedSpans ('^');

		await Catalog.JoinableTaskContext.Factory.SwitchToMainThreadAsync ();
		var textView = CreateTextView(text.Text);

		var logger = TestLoggerFactory.CreateTestMethodLogger ().RethrowExceptions ();

		var parserProvider = Catalog.GetService<XmlParserProvider> ();
		var parser = parserProvider.GetParser (textView.TextBuffer);
		Assert.NotNull (parser);

		var tagger = new XmlHighlightEndTagTagger (textView, parserProvider, Catalog.JoinableTaskContext, logger);

		var allHighlights = await GetAllHighlights (textView, tagger);

		AssertHighlights (
			allHighlights,
			new Highlight (spans[0], (spans[1], MatchingTagHighlightTag.Instance)),
			new Highlight (spans[1], (spans[0], MatchingTagHighlightTag.Instance))
		);
	}
}
