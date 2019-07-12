// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.Tags;

namespace MonoDevelop.Xml.Editor.HighlightReferences
{
	public abstract class HighlightReferencesTagger : ITagger<ITag>, IDisposable
	{
		const int triggerDelayMilliseconds = 1000;

		readonly ITextView textView;
		readonly JoinableTaskContext joinableTaskContext;
		readonly Timer timer;

		public HighlightReferencesTagger (ITextView textView, JoinableTaskContext joinableTaskContext)
		{
			this.textView = textView;
			this.joinableTaskContext = joinableTaskContext;
			textView.Caret.PositionChanged += CaretPositionChanged;
			textView.TextBuffer.ChangedLowPriority += BufferChanged;
			timer = new Timer (TimerFired);
		}

		void CaretPositionChanged (object sender, CaretPositionChangedEventArgs e)
		{
			//if the caret moves within a highlight, we don't need to update anything
			//as the highlight is still current
			if (IsCaretInHighlight (e.NewPosition.BufferPosition)) {
				return;
			}

			ClearHighlightsAndRescheduleTimer (e.NewPosition.BufferPosition.Snapshot);
		}

		void BufferChanged (object sender, TextContentChangedEventArgs e)
		{
			ClearHighlightsAndRescheduleTimer (e.After);
		}

		void TimerFired (object state)
		{
			//only one timer is running and can assign this, so we don't have to lock
			cancelSource?.Cancel ();
			cancelSource = new CancellationTokenSource ();
			var token = cancelSource.Token;

			Task.Run (async () => {
				try {
					var position = textView.Caret.Position.BufferPosition;
					var newHighlights = await GetReferencesAsync (position, token);

					ImmutableArray<(ReferenceUsage type, SnapshotSpan location)> oldHighlights;
					lock (highlightsLocker) {
						if (token.IsCancellationRequested) {
							return;
						}
						oldHighlights = highlights;
						highlights = newHighlights;
						highlightedSnapshot = position.Snapshot;
					}

					var oldSpan = GetHighlightedRange (highlights);
					var newSpan = GetHighlightedRange (newHighlights);
					var updateSpan = UnionNonEmpty (MapToCurrentIfNonEmpty (position.Snapshot, oldSpan), newSpan);
					if (updateSpan.IsEmpty) {
						return;
					}

					await joinableTaskContext.Factory.SwitchToMainThreadAsync ();
					TagsChanged?.Invoke (this, new SnapshotSpanEventArgs (updateSpan));
				} catch (Exception ex) {
					LogInternalError (ex);
				}
			}, token);
		}

		void LogInternalError (Exception ex)
		{
			LoggingService.LogWarning ("Internal error in highlight references: {ex}", ex);
		}

		readonly object highlightsLocker = new object ();
		ImmutableArray<(ReferenceUsage type, SnapshotSpan location)> highlights = ImmutableArray<(ReferenceUsage type, SnapshotSpan location)>.Empty;
		ITextSnapshot highlightedSnapshot;

		CancellationTokenSource cancelSource;

		void ClearHighlights (ITextSnapshot forSnapshot)
		{
			SnapshotSpan span;

			lock (highlightsLocker) {
				if (highlights.Length == 0 || highlightedSnapshot.Version.VersionNumber > forSnapshot.Version.VersionNumber) {
					return;
				}
				span = GetHighlightedRange (highlights);
				highlights = ImmutableArray<(ReferenceUsage type, SnapshotSpan location)>.Empty;
			}

			joinableTaskContext.Factory.Run (async delegate {
				await joinableTaskContext.Factory.SwitchToMainThreadAsync ();
				TagsChanged?.Invoke (this, new SnapshotSpanEventArgs (span));
			});
		}

		SnapshotSpan GetHighlightedRange (ImmutableArray<(ReferenceUsage type, SnapshotSpan location)> spans)
			=> new SnapshotSpan (
				spans[0].location.Start,
				spans[spans.Length - 1].location.End
			);

		SnapshotSpan UnionNonEmpty (SnapshotSpan a, SnapshotSpan b)
			=> a.IsEmpty? b : b.IsEmpty? a :
			new SnapshotSpan (
				//using both snapshots here validates that they're the same
				new SnapshotPoint (a.Snapshot, Math.Min (a.Start, b.Start)),
				new SnapshotPoint (b.Snapshot, Math.Max (a.End, b.End))
			);

		SnapshotSpan MapToCurrentIfNonEmpty (ITextSnapshot newSnapshot, SnapshotSpan oldSpan)
			=> oldSpan.IsEmpty
			? new SnapshotSpan (newSnapshot, 0, 0)
			: oldSpan.Snapshot.CreateTrackingSpan (oldSpan, SpanTrackingMode.EdgeInclusive).GetSpan (newSnapshot);

		void ClearHighlightsAndRescheduleTimer (ITextSnapshot forSnapshot)
		{
			cancelSource?.Cancel ();
			ClearHighlights (forSnapshot);
			timer.Change (triggerDelayMilliseconds, Timeout.Infinite);
		}

		bool IsCaretInHighlight (SnapshotPoint caretPosition)
		{
			var h = highlights;
			if (h.Length == 0 || h[0].location.Snapshot != caretPosition.Snapshot) {
				return false;
			}
			return h.BinarySearch ((ReferenceUsage.Other, new SnapshotSpan (caretPosition, 0)), IntersectionComparer.Instance) > -1;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<ITag>> GetTags (NormalizedSnapshotSpanCollection spans)
		{
			//this may be assigned from another thread so capture a consistent value
			var h = highlights;

			//verify it matches the snapshot we're being asked to tag
			//if it doesn't, then an updated one will be coming along soon
			if (h.Length == 0 || h[0].location.Snapshot != spans[0].Snapshot) {
				yield break;
			}

			//FIXME is this correct handling of errors that span multiple spans?
			foreach (var taggingSpan in spans) {
				foreach (var (type, location) in highlights) {
					if (location.IntersectsWith (taggingSpan)) {
						yield return new TagSpan<ITag> (location, GetTag (type));
					}
				}
			}
		}

		protected abstract Task<ImmutableArray<(ReferenceUsage type, SnapshotSpan location)>> GetReferencesAsync (SnapshotPoint caretLocation, CancellationToken token);

		static NavigableHighlightTag GetTag (ReferenceUsage type)
		{
			switch (type) {
			default:
			case ReferenceUsage.Other:
			case ReferenceUsage.Read:
				return ReferenceHighlightTag.Instance;
			case ReferenceUsage.Definition:
				return DefinitionHighlightTag.Instance;
			case ReferenceUsage.Write:
				return WrittenReferenceHighlightTag.Instance;
			}
		}

		bool disposed = false;

		public void Dispose ()
		{
			if (disposed) {
				return;
			}
			disposed = true;

			textView.Caret.PositionChanged -= CaretPositionChanged;
			textView.TextBuffer.ChangedLowPriority -= BufferChanged;
			timer.Dispose ();
		}

		/// <summary>
		/// Considers items to be indentical if they intersect
		/// </summary>
		class IntersectionComparer : IComparer<(ReferenceUsage type, SnapshotSpan location)>
		{
			public static IntersectionComparer Instance { get; } = new IntersectionComparer ();

			public int Compare ((ReferenceUsage type, SnapshotSpan location) x, (ReferenceUsage type, SnapshotSpan location) y)
			{
				if (x.location.IntersectsWith (y.location)) {
					return 0;
				}
				return x.location.Start.CompareTo (y.location.Start);
			}
		}
	}

	public enum ReferenceUsage
	{
		Other,
		Write,
		Definition,
		Read
	}
}
