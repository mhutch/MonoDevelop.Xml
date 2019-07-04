// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//ROSLYN IMPORT: Microsoft.CodeAnalysis.Editor.ReferenceHighlighting.NavigateToHighlightReferenceCommandHandler

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Xml.Editor.Tags;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace MonoDevelop.Xml.Editor.Commands
{
	[Export (typeof (VSCommanding.ICommandHandler))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[Name ("Xml Navigate to Highlighted Reference Command Handler")]
	internal partial class NavigateToHighlightReferenceCommandHandler :
		VSCommanding.ICommandHandler<NavigateToNextHighlightedReferenceCommandArgs>,
		VSCommanding.ICommandHandler<NavigateToPreviousHighlightedReferenceCommandArgs>
	{
		private readonly IOutliningManagerService _outliningManagerService;
		private readonly IViewTagAggregatorFactoryService _tagAggregatorFactory;

		public string DisplayName => "Navigate To Highlighted Reference";

		[ImportingConstructor]
		public NavigateToHighlightReferenceCommandHandler (
			IOutliningManagerService outliningManagerService,
			IViewTagAggregatorFactoryService tagAggregatorFactory)
		{
			_outliningManagerService = outliningManagerService ?? throw new ArgumentNullException (nameof (outliningManagerService));
			_tagAggregatorFactory = tagAggregatorFactory ?? throw new ArgumentNullException (nameof (tagAggregatorFactory));
		}

		public CommandState GetCommandState (NavigateToNextHighlightedReferenceCommandArgs args)
		{
			return GetCommandStateImpl (args);
		}

		public CommandState GetCommandState (NavigateToPreviousHighlightedReferenceCommandArgs args)
		{
			return GetCommandStateImpl (args);
		}

		private CommandState GetCommandStateImpl (EditorCommandArgs args)
		{
			using (var tagAggregator = _tagAggregatorFactory.CreateTagAggregator<NavigableHighlightTag> (args.TextView)) {
				var tagUnderCursor = FindTagUnderCaret (tagAggregator, args.TextView);
				return tagUnderCursor == null ? CommandState.Unavailable : CommandState.Available;
			}
		}

		public bool ExecuteCommand (NavigateToNextHighlightedReferenceCommandArgs args, CommandExecutionContext context)
		{
			return ExecuteCommandImpl (args, navigateToNext: true, context);
		}

		public bool ExecuteCommand (NavigateToPreviousHighlightedReferenceCommandArgs args, CommandExecutionContext context)
		{
			return ExecuteCommandImpl (args, navigateToNext: false, context);
		}

		private bool ExecuteCommandImpl (EditorCommandArgs args, bool navigateToNext, CommandExecutionContext context)
		{
			using (var tagAggregator = _tagAggregatorFactory.CreateTagAggregator<NavigableHighlightTag> (args.TextView)) {
				var tagUnderCursor = FindTagUnderCaret (tagAggregator, args.TextView);

				if (tagUnderCursor == null) {
					return false;
				}

				var spans = GetTags (tagAggregator, args.TextView.TextSnapshot.GetFullSpan()).ToList ();

				var destTag = GetDestinationTag (tagUnderCursor.Value, spans, navigateToNext);

				if (args.TextView.TryMoveCaretToAndEnsureVisible (destTag.Start, _outliningManagerService)) {
					args.TextView.SetSelection (destTag);
				}
			}

			return true;
		}

		private static IEnumerable<SnapshotSpan> GetTags (
			ITagAggregator<NavigableHighlightTag> tagAggregator,
			SnapshotSpan span)
		{
			return tagAggregator.GetTags (span)
								.SelectMany (tag => tag.Span.GetSpans (span.Snapshot.TextBuffer))
								.OrderBy (tag => tag.Start);
		}

		private static SnapshotSpan GetDestinationTag (
			SnapshotSpan tagUnderCursor,
			List<SnapshotSpan> orderedTagSpans,
			bool navigateToNext)
		{
			var destIndex = orderedTagSpans.BinarySearch (tagUnderCursor, new StartComparer ());

			destIndex += navigateToNext ? 1 : -1;
			if (destIndex < 0) {
				destIndex = orderedTagSpans.Count - 1;
			} else if (destIndex == orderedTagSpans.Count) {
				destIndex = 0;
			}

			return orderedTagSpans[destIndex];
		}

		private SnapshotSpan? FindTagUnderCaret (
			ITagAggregator<NavigableHighlightTag> tagAggregator,
			ITextView textView)
		{
			// We always want to be working with the surface buffer here, so this line is correct
			var caretPosition = textView.Caret.Position.BufferPosition.Position;

			var tags = GetTags (tagAggregator, new SnapshotSpan (textView.TextSnapshot, new Span (caretPosition, 0)));
			return tags.Any ()
				? tags.First ()
				: (SnapshotSpan?)null;
		}

		private class StartComparer : IComparer<SnapshotSpan>
		{
			public int Compare (SnapshotSpan x, SnapshotSpan y)
			{
				return x.Start.CompareTo (y.Start);
			}
		}
	}
}