// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.HighlightReferences;

using NUnit.Framework;

namespace MonoDevelop.Xml.Editor.Tests.Tagging;

public abstract class HighlightTaggerTest<TTag, TKind> : XmlEditorTest where TTag : ITag
{
	protected ITagAggregator<TTag> CreateAggregator (ITextBuffer textBuffer) => Catalog.BufferTagAggregatorFactoryService.CreateTagAggregator<TTag> (textBuffer);
	protected Task<(SnapshotSpan? sourceSpan, SnapshotSpan tagsChangedSpan)?> UpdateHighlightsAsync (HighlightTagger<TTag, TKind> tagger) => tagger.UpdateHighlightsAsync (CancellationToken.None);

	protected async Task<List<Highlight>> GetAllHighlights (ITextView textView, HighlightTagger<TTag, TKind> tagger)
	{
		var highlights = await GetAllHighlightsInner (textView, tagger).ConfigureAwait (false);

		return highlights.Select (kv => new Highlight (
				new TextSpan (kv.Key.Start, kv.Key.Length),
				kv.Value.Select (t => (new TextSpan (t.Span.Start, t.Span.Length), t.Tag)).OrderBy (t => t.Item1.Start).ToList ()
			))
			.OrderBy (h => h.Source.Start)
			.ToList ();
	}

	async Task<Dictionary<SnapshotSpan, List<ITagSpan<TTag>>>> GetAllHighlightsInner (ITextView textView, HighlightTagger<TTag, TKind> tagger)
	{
		Dictionary<SnapshotSpan, List<ITagSpan<TTag>>> highlights = new ();

		for (int i = 0; i <= textView.TextBuffer.CurrentSnapshot.Length; i++) {
			textView.Caret.MoveTo (new SnapshotPoint (textView.TextBuffer.CurrentSnapshot, i));

			// return null if it was cancelled or nothing changed
			var updateResult = await UpdateHighlightsAsync (tagger).ConfigureAwait (false);
			if (updateResult is null) {
				continue;
			}

			if (updateResult.Value.sourceSpan is SnapshotSpan sourceSpan) {
				var tags = tagger.GetTags (new NormalizedSnapshotSpanCollection (new SnapshotSpan (textView.TextBuffer.CurrentSnapshot, 0, textView.TextBuffer.CurrentSnapshot.Length))).ToList ();
				if (highlights.TryGetValue (sourceSpan, out var existingTags)) {
					Assert.That (existingTags, Is.EquivalentTo (tags).Using (new TagSpanComparer ()));
				} else {
					highlights.Add (sourceSpan, tags);
				}
			}
		}

		return highlights;
	}

	protected void AssertHighlights (List<Highlight> actualHighlights, params Highlight[] expectedHighlights)
	{
		Assert.AreEqual (expectedHighlights.Length, actualHighlights.Count);

		for (int i = 0; i < expectedHighlights.Length; i++) {
			var actual = actualHighlights[i];
			var expected = expectedHighlights[i];

			Assert.AreEqual (expected.Source, actual.Source);
			Assert.AreEqual (expected.Tags.Count, actual.Tags.Count);

			for (int j = 0; j < expected.Tags.Count; j++) {
				var actualTag = actual.Tags[j];
				var expectedTag = expected.Tags[j];

				Assert.AreEqual (expectedTag.markedSpan, actualTag.markedSpan);
			}
		}
	}

	class TagSpanComparer : IComparer<ITagSpan<TTag>>
	{
		public int Compare (ITagSpan<TTag> x, ITagSpan<TTag> y)
		{
			if (x.Span.Snapshot != y.Span.Snapshot) {
				return -1;
			}
			if (x.Span.Span != y.Span.Span) {
				return -1;
			}
			if (Equals (x.Tag, y.Tag)) {
				return 0;
			}
			return 1;
		}
	}

	protected struct Highlight
	{
		public TextSpan Source { get; set; }
		public List<(TextSpan markedSpan, TTag tag)> Tags { get; private set; }

		public Highlight (TextSpan source, List<(TextSpan markedSpan, TTag tag)> tags)
		{
			Source = source;
			Tags = tags;
		}

		public Highlight (TextSpan source, params (TextSpan markedSpan, TTag tag)[] tags)
		{
			Source = source;
			Tags = tags.ToList ();
		}
	}
}