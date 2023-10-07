// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.Xml.Logging;

namespace MonoDevelop.Xml.Editor.HighlightReferences
{
	/// <summary>
	/// Base class for taggers that highlight based on caret location. Subclasses need only implement
	/// GetHighlightsAsync and GetTagsAsync, and this will take care of triggering, clearing etc.
	/// </summary>
	public abstract class HighlightTagger<TTag, TKind>
		: ITagger<TTag>, IDisposable
		where TTag : ITag
	{
		const int triggerDelayMilliseconds = 200;

		protected internal ITextView TextView { get;  }
		protected JoinableTaskContext JoinableTaskContext { get; }
		readonly Timer timer;
		protected ILogger Logger { get; }

		protected HighlightTagger (ITextView textView, JoinableTaskContext joinableTaskContext, ILogger logger)
		{
			TextView = textView;
			JoinableTaskContext = joinableTaskContext;
			Logger = logger;
			textView.Caret.PositionChanged += CaretPositionChanged;
			textView.TextBuffer.ChangedLowPriority += BufferChanged;
			timer = new Timer (TimerFired);
		}

		void CaretPositionChanged (object sender, CaretPositionChangedEventArgs e)
		{
			//if the caret moves within a highlight, we don't need to update anything
			//as the highlight is still current
			if (IsStillValid (e.NewPosition.BufferPosition)) {
				return;
			}

			ClearHighlightsAndRescheduleTimer (e.NewPosition.BufferPosition.Snapshot);
		}

		void BufferChanged (object sender, TextContentChangedEventArgs e)
		{
			ClearHighlightsAndRescheduleTimer (e.After);
		}

		// internal for testing
		internal async Task<(SnapshotSpan? sourceSpan, SnapshotSpan tagsChangedSpan)?> UpdateHighlightsAsync (CancellationToken cancellationToken = default)
		{
			var position = TextView.Caret.Position.BufferPosition;
			var newHighlights = await GetHighlightsAsync (position, cancellationToken).ConfigureAwait (false);
			sourceSpan = newHighlights.highlights.Length == 0 ? (SnapshotSpan?)null : newHighlights.sourceSpan;

			ImmutableArray<(TKind kind, SnapshotSpan location)> oldHighlights;
			lock (highlightsLocker) {
				oldHighlights = highlights;
				highlights = newHighlights.highlights;
				highlightedSnapshot = position.Snapshot;
				if (cancellationToken.IsCancellationRequested) {
					return null;
				}
			}

			var oldSpan = GetHighlightedRange (highlights);
			var newSpan = GetHighlightedRange (newHighlights.highlights);
			var tagsChangedSpan = UnionNonEmpty (MapToCurrentIfNonEmpty (position.Snapshot, oldSpan), newSpan);
			if (tagsChangedSpan.IsEmpty) {
				return null;
			}
			return (sourceSpan, tagsChangedSpan);
		}

		void TimerFired (object state)
		{
			//only one timer is running and can assign this, so we don't have to lock
			cancelSource?.Cancel ();
			cancelSource = new CancellationTokenSource ();
			var token = cancelSource.Token;

			Task.Run (async () => {
				var updateResult = await UpdateHighlightsAsync ().ConfigureAwait (false);
				if (updateResult.HasValue) {
					await JoinableTaskContext.Factory.SwitchToMainThreadAsync (token);
					TagsChanged?.Invoke (this, new SnapshotSpanEventArgs (updateResult.Value.tagsChangedSpan));
				}
			}, token)
			.LogTaskExceptionsAndForget (Logger, "HighlightTagger.TimerFired");
		}

		readonly object highlightsLocker = new ();

		ImmutableArray<(TKind kind, SnapshotSpan location)> highlights = ImmutableArray<(TKind kind, SnapshotSpan location)>.Empty;
		ITextSnapshot highlightedSnapshot;
		SnapshotSpan? sourceSpan;

		CancellationTokenSource cancelSource;

		void ClearHighlights (ITextSnapshot forSnapshot)
		{
			SnapshotSpan span;

			lock (highlightsLocker) {
				if (highlights.Length == 0 || highlightedSnapshot.Version.VersionNumber > forSnapshot.Version.VersionNumber) {
					return;
				}
				span = GetHighlightedRange (highlights);
				highlights = ImmutableArray<(TKind kind, SnapshotSpan location)>.Empty;
			}

			JoinableTaskContext.Factory.Run (async delegate {
				await JoinableTaskContext.Factory.SwitchToMainThreadAsync ();
				TagsChanged?.Invoke (this, new SnapshotSpanEventArgs (span));
			});
		}

		SnapshotSpan GetHighlightedRange (ImmutableArray<(TKind kind, SnapshotSpan location)> spans)
			=> spans.Length == 0
				? new SnapshotSpan ()
				: new SnapshotSpan (
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

		bool IsStillValid (SnapshotPoint caretPosition)
		{
			var sourceSpan = this.sourceSpan;
			if (sourceSpan == null) {
				return false;
			}
			if (sourceSpan.Value.Snapshot == caretPosition.Snapshot && ContainsInclusive (sourceSpan.Value, caretPosition)) {
				return true;
			}

			if (!RemainsValidIfCaretMovesBetweenHighlights) {
				return false;
			}

			var h = highlights;
			if (h.Length == 0 || h[0].location.Snapshot != caretPosition.Snapshot) {
				return false;
			}
			var loc = (default (TKind), new SnapshotSpan (caretPosition, 0));
			return h.BinarySearch (loc, IntersectionComparer.Instance) > -1;
		}

		static bool ContainsInclusive (SnapshotSpan span, SnapshotPoint point)
			=> span.Start <= point.Position && point.Position <= span.End;

		protected virtual bool RemainsValidIfCaretMovesBetweenHighlights => false;

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public IEnumerable<ITagSpan<TTag>> GetTags (NormalizedSnapshotSpanCollection spans)
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
						yield return new TagSpan<TTag> (location, GetTag (type));
					}
				}
			}
		}

		protected abstract
			Task<(SnapshotSpan sourceSpan, ImmutableArray<(TKind kind, SnapshotSpan location)> highlights)>
			GetHighlightsAsync (SnapshotPoint caretLocation, CancellationToken token);

		protected abstract TTag GetTag (TKind kind);

		protected
			(SnapshotSpan sourceSpan, ImmutableArray<(TKind kind, SnapshotSpan location)> highlights)
			Empty
			=> (default, ImmutableArray<(TKind kind, SnapshotSpan location)>.Empty);

		bool disposed;

		public void Dispose ()
		{
			if (disposed) {
				return;
			}
			disposed = true;
			GC.SuppressFinalize (this);

			Dispose (true);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing) {
				cancelSource?.Cancel ();
				TextView.Caret.PositionChanged -= CaretPositionChanged;
				TextView.TextBuffer.ChangedLowPriority -= BufferChanged;
				timer.Dispose ();
			}
		}

		~HighlightTagger ()
		{
			Dispose (false);
		}

		/// <summary>
		/// Considers items to be identical if they intersect
		/// </summary>
		class IntersectionComparer : IComparer<(TKind kind, SnapshotSpan location)>
		{
			public static IntersectionComparer Instance { get; } = new IntersectionComparer ();

			public int Compare ((TKind kind, SnapshotSpan location) x, (TKind kind, SnapshotSpan location) y)
			{
				if (x.location.IntersectsWith (y.location)) {
					return 0;
				}
				return x.location.Start.CompareTo (y.location.Start);
			}
		}
	}
}
