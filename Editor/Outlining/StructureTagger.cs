// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;

namespace MonoDevelop.Xml.Editor.Tagging
{
	class StructureTagger : ITagger<IStructureTag>
	{
		private ITextBuffer buffer;
		private StructureTaggerProvider provider;
		private readonly XmlBackgroundParser parser;
		private static readonly IEnumerable<ITagSpan<IStructureTag>> emptyTagList = Array.Empty<ITagSpan<IStructureTag>> ();

		public StructureTagger (ITextBuffer buffer, StructureTaggerProvider provider)
		{
			this.buffer = buffer;
			this.provider = provider;
			parser = XmlBackgroundParser.GetParser (buffer);
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<IStructureTag>> GetTags (NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0) {
				return emptyTagList;
			}

			var snapshot = spans[0].Snapshot;

			var parseTask = parser.GetOrProcessAsync (snapshot, default);

			if (parseTask.IsCompleted) {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
				return GetTags (parseTask.Result, spans, snapshot);
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
			} else {
				parseTask.ContinueWith (t => {
					RaiseTagsChanged ();
				}, TaskScheduler.Default).Forget ();
			}

			return emptyTagList;
		}

		private void RaiseTagsChanged ()
		{
			var snapshot = buffer.CurrentSnapshot;
			var args = new SnapshotSpanEventArgs (new SnapshotSpan (snapshot, 0, snapshot.Length));
			TagsChanged?.Invoke (this, args);
		}

		private IEnumerable<ITagSpan<IStructureTag>> GetTags (XmlParseResult xmlParseResult, NormalizedSnapshotSpanCollection spans, ITextSnapshot snapshot)
		{
			var root = xmlParseResult.XDocument;

			var resultList = new List<ITagSpan<IStructureTag>> ();

			int previousLineStart = -1;
			int previousLineEnd = -1;

			foreach (var snapshotSpan in spans) {
				var nodes = GetNodesIntersectingRange (root, new TextSpan (snapshotSpan.Span.Start, snapshotSpan.Span.Length));
				foreach (var node in nodes) {
					// exclude the document itself since the root element will take care of it
					if (node is XDocument) {
						continue;
					}

					// no need to collapse text as usually collapsing the parent is good enough
					if (node is XText) {
						continue;
					}

					var nodeSpan = node.OuterSpan;
					if (nodeSpan.Start < 0 || nodeSpan.End > snapshot.Length) {
						continue;
					}

					var outliningSpan = new Span (nodeSpan.Start, nodeSpan.Length);
					var startLine = snapshot.GetLineFromPosition (outliningSpan.Start);
					var endLine = snapshot.GetLineFromPosition (outliningSpan.End);
					if (startLine.LineNumber == endLine.LineNumber) {
						// ignore single-line nodes 
						continue;
					}

					if (startLine.LineNumber == previousLineStart || endLine.LineNumber == previousLineEnd) {
						// ignore multiple nodes starting and ending on the same line
						continue;
					}

					previousLineStart = startLine.LineNumber;
					previousLineEnd = endLine.LineNumber;

					var headerSpan = new Span (outliningSpan.Start, Math.Min (startLine.End.Position - outliningSpan.Start, 100));
					string firstLine = snapshot.GetText (headerSpan);
					string collapseForm = firstLine;

					var tagSnapshotSpan = new SnapshotSpan (snapshot, outliningSpan);
					var structureTag = new StructureTag (
						snapshot,
						outliningSpan: outliningSpan,
						headerSpan: headerSpan,
						guideLineSpan: outliningSpan,
						guideLineHorizontalAnchor: outliningSpan.Start,
						type: PredefinedStructureTagTypes.Structural,
						isCollapsible: true,
						collapsedForm: collapseForm,
						collapsedHintForm: snapshot.GetText (headerSpan));
					var tagSpan = new TagSpan<IStructureTag> (tagSnapshotSpan, structureTag);
					resultList.Add (tagSpan);
				}
			}

			return resultList;
		}

		private static IEnumerable<XNode> GetNodesIntersectingRange (XContainer root, TextSpan span)
		{
			if (root.OuterSpan.Intersects (span)) {
				yield return root;
			}

			foreach (var child in root.Nodes) {
				if (child.OuterSpan.End < span.Start) {
					continue;
				}

				if (child.OuterSpan.Start >= span.End) {
					break;
				}

				if (child is XContainer childContainer) {
					foreach (var grandchild in GetNodesIntersectingRange (childContainer, span)) {
						yield return grandchild;
					}
				} else {
					yield return child;
				}
			}
		}
	}
}