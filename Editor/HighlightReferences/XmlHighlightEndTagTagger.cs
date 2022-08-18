// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.HighlightReferences;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.MSBuild.Editor.HighlightReferences
{
	class XmlHighlightEndTagTagger : HighlightTagger<ITextMarkerTag, ITextMarkerTag>
	{
		XmlParserProvider parserProvider;

		public XmlHighlightEndTagTagger (
			ITextView textView, XmlHighlightEndTagTaggerProvider provider
			)
			: base (textView, provider.JoinableTaskContext)
		{
			parserProvider = provider.ParserProvider;
		}

		protected async override
			Task<(SnapshotSpan sourceSpan, ImmutableArray<(ITextMarkerTag kind, SnapshotSpan location)> highlights)>
			GetHighlightsAsync (SnapshotPoint caretLocation, CancellationToken token)
		{
			if (!parserProvider.TryGetParser (TextView.TextBuffer, out var parser)) {
				return Empty;
			}

			var spine = parser.GetSpineParser (caretLocation);
			if (!(spine.CurrentState is XmlNameState) || !(spine.CurrentState.Parent is XmlTagState || spine.CurrentState.Parent is XmlClosingTagState)) {
				return Empty;
			}

			var parseResult = await parser.GetOrProcessAsync (caretLocation.Snapshot, token).ConfigureAwait (false);

			var node = parseResult.XDocument.RootElement.FindAtOffset (caretLocation.Position);

			if (node is XElement element) {
				if (element.ClosingTag is XClosingTag closingTag && closingTag.IsNamed) {
					return CreateResult (element.NameSpan, closingTag.NameSpan);
				}
			} else if (node is XClosingTag closingTag) {
				var matchingElement = parseResult.XDocument.AllDescendentNodes
					.OfType<XElement> ()
					.FirstOrDefault (t => t.ClosingTag == closingTag);
				if (matchingElement != null && matchingElement.IsNamed) {
					return CreateResult (closingTag.NameSpan, matchingElement.NameSpan);
				}
			}
			return Empty;

			(SnapshotSpan sourceSpan, ImmutableArray<(ITextMarkerTag kind, SnapshotSpan location)> highlights)
				CreateResult (TextSpan source, TextSpan target)
				=> (
					new SnapshotSpan (caretLocation.Snapshot, source.Start, source.Length),
					ImmutableArray<(ITextMarkerTag kind, SnapshotSpan location)>.Empty
					.Add ((
						MatchingTagHighlightTag.Instance,
						new SnapshotSpan (caretLocation.Snapshot, target.Start, target.Length)))
				);
		}

		protected override ITextMarkerTag GetTag (ITextMarkerTag kind) => kind;
	}

	sealed class MatchingTagHighlightTag : TextMarkerTag
	{
		internal const string TagId = "brace matching";

		public static readonly MatchingTagHighlightTag Instance = new MatchingTagHighlightTag ();

		private MatchingTagHighlightTag () : base (TagId) { }
	}
}
