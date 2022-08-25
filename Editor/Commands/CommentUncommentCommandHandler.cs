// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

using JoinableTaskContext = Microsoft.VisualStudio.Threading.JoinableTaskContext;

namespace MonoDevelop.Xml.Editor.Commands
{
	[Export (typeof (ICommandHandler))]
	[Name (Name)]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	class CommentUncommentCommandHandler :
		ICommandHandler<CommentSelectionCommandArgs>,
		ICommandHandler<UncommentSelectionCommandArgs>,
		ICommandHandler<ToggleBlockCommentCommandArgs>,
		ICommandHandler<ToggleLineCommentCommandArgs>
	{
		const string Name = nameof (CommentUncommentCommandHandler);
		const string OpenComment = "<!--";
		const string CloseComment = "-->";

		[ImportingConstructor]
		public CommentUncommentCommandHandler (
			XmlParserProvider parserProvider,
			ITextUndoHistoryRegistry undoHistoryRegistry,
			IEditorOperationsFactoryService editorOperationsFactoryService,
			JoinableTaskContext joinableTaskContext)
		{
			this.parserProvider = parserProvider;
			this.undoHistoryRegistry = undoHistoryRegistry;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
			this.joinableTaskContext = joinableTaskContext;
		}

		readonly XmlParserProvider parserProvider;
		readonly ITextUndoHistoryRegistry undoHistoryRegistry;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;
		readonly JoinableTaskContext joinableTaskContext;

		public string DisplayName => Name;

		public CommandState GetCommandState (CommentSelectionCommandArgs args) => CommandState.Available;

		public CommandState GetCommandState (UncommentSelectionCommandArgs args) => CommandState.Available;

		public CommandState GetCommandState (ToggleBlockCommentCommandArgs args) => CommandState.Available;

		public CommandState GetCommandState (ToggleLineCommentCommandArgs args) => CommandState.Available;

		enum Operation
		{
			Comment,
			Uncomment,
			Toggle
		}

		public bool ExecuteCommand (CommentSelectionCommandArgs args, CommandExecutionContext executionContext)
			=> ExecuteCommandCore (args, executionContext, Operation.Comment);

		public bool ExecuteCommand (UncommentSelectionCommandArgs args, CommandExecutionContext executionContext)
			=> ExecuteCommandCore (args, executionContext, Operation.Uncomment);

		public bool ExecuteCommand (ToggleBlockCommentCommandArgs args, CommandExecutionContext executionContext)
			=> ExecuteCommandCore (args, executionContext, Operation.Toggle);

		public bool ExecuteCommand (ToggleLineCommentCommandArgs args, CommandExecutionContext executionContext)
			=> ExecuteCommandCore (args, executionContext, Operation.Toggle);

		bool ExecuteCommandCore (EditorCommandArgs args, CommandExecutionContext context, Operation operation)
		{
			ITextView textView = args.TextView;
			ITextBuffer textBuffer = args.SubjectBuffer;

			if (!parserProvider.TryGetParser (textBuffer, out var parser)) {
				return false;
			}

			// the rest of this method needs to run on the the main thread anyways
			#pragma warning disable VSTHRD102 // Implement internal logic asynchronously

			var xmlParseResult = joinableTaskContext.Factory.Run (() => parser.GetOrProcessAsync (textBuffer.CurrentSnapshot, context.OperationContext.UserCancellationToken));

			#pragma warning restore VSTHRD102

			if (xmlParseResult?.XDocument is not XDocument xmlDocumentSyntax || context.OperationContext.UserCancellationToken.IsCancellationRequested) {
				return false;
			}

			string description = operation.ToString ();

			var editorOperations = editorOperationsFactoryService.GetEditorOperations (textView);
			var multiSelectionBroker = textView.GetMultiSelectionBroker ();
			var selectedSpans = multiSelectionBroker.AllSelections.Select (selection => selection.Extent);

			using (context.OperationContext.AddScope (allowCancellation: false, description: description)) {
				ITextUndoHistory undoHistory = undoHistoryRegistry.RegisterHistory (textBuffer);

				using (ITextUndoTransaction undoTransaction = undoHistory.CreateTransaction (description)) {
					switch (operation) {
					case Operation.Comment:
						CommentSelection (textBuffer, selectedSpans, xmlDocumentSyntax, editorOperations, multiSelectionBroker);
						break;
					case Operation.Uncomment:
						UncommentSelection (textBuffer, selectedSpans, xmlDocumentSyntax);
						break;
					case Operation.Toggle:
						ToggleCommentSelection (textBuffer, selectedSpans, xmlDocumentSyntax, editorOperations, multiSelectionBroker);
						break;
					}

					undoTransaction.Complete ();
				}
			}

			return true;
		}

		public static void CommentSelection (
			ITextBuffer textBuffer,
			IEnumerable<VirtualSnapshotSpan> selectedSpans,
			XDocument xmlDocumentSyntax,
			IEditorOperations? editorOperations = null,
			IMultiSelectionBroker? multiSelectionBroker = null)
		{
			var snapshot = textBuffer.CurrentSnapshot;

			var spansToExpandIntoComments = new List<SnapshotSpan> ();
			var newCommentInsertionPoints = new List<VirtualSnapshotPoint> ();

			foreach (var selectedSpan in selectedSpans) {
				// empty selection on an empty line results in inserting a new comment
				if (selectedSpan.IsEmpty && string.IsNullOrWhiteSpace (snapshot.GetLineFromPosition (selectedSpan.Start.Position).GetText ())) {
					newCommentInsertionPoints.Add (selectedSpan.Start);
				} else {
					spansToExpandIntoComments.Add (selectedSpan.SnapshotSpan);
				}
			}

			NormalizedSnapshotSpanCollection commentSpans = NormalizedSnapshotSpanCollection.Empty;
			if (spansToExpandIntoComments.Any ()) {
				commentSpans = GetCommentableSpansInSelection (xmlDocumentSyntax, spansToExpandIntoComments);
			}

			using (var edit = textBuffer.CreateEdit ()) {
				if (commentSpans.Any ()) {
					CommentSpans (edit, commentSpans);
				}

				if (newCommentInsertionPoints.Any ()) {
					CommentEmptySpans (edit, newCommentInsertionPoints, editorOperations);
				}

				edit.Apply ();
			}

			var newSnapshot = textBuffer.CurrentSnapshot;

			// Now fix up the selections after the edit.
			var translatedInsertionPoints = newCommentInsertionPoints.Select (p => p.TranslateTo (newSnapshot)).ToHashSet ();
			var fixupSelectionStarts = selectedSpans.Where (s => !s.IsEmpty).ToDictionary (
				c => c.Start.TranslateTo (newSnapshot, PointTrackingMode.Positive),
				c => c.Start.TranslateTo (newSnapshot, PointTrackingMode.Negative));

			if (multiSelectionBroker != null) {
				multiSelectionBroker.PerformActionOnAllSelections (transformer => {
					// for newly inserted comments position the caret inside the comment
					if (translatedInsertionPoints.Contains (transformer.Selection.ActivePoint)) {
						transformer.MoveTo (
							new VirtualSnapshotPoint (transformer.Selection.End.Position - CloseComment.Length),
							select: false,
							insertionPointAffinity: PositionAffinity.Successor);
						// for commented code make sure the new selection includes the opening <!--
					} else if (fixupSelectionStarts.TryGetValue (transformer.Selection.Start, out var newStart)) {
						var end = transformer.Selection.End;
						transformer.MoveTo (newStart, select: false, PositionAffinity.Successor);
						transformer.MoveTo (end, select: true, PositionAffinity.Successor);
					}
				});
			}
		}

		public static void UncommentSelection (
			ITextBuffer textBuffer,
			IEnumerable<VirtualSnapshotSpan> selectedSpans,
			XDocument xmlDocumentSyntax)
		{
			var commentedSpans = GetCommentedSpansInSelection (xmlDocumentSyntax, selectedSpans);
			if (commentedSpans == null || !commentedSpans.Any ()) {
				return;
			}

			using (var edit = textBuffer.CreateEdit ()) {
				UncommentSpans (edit, commentedSpans);
				edit.Apply ();
			}
		}

		public static void ToggleCommentSelection (
			ITextBuffer textBuffer,
			IEnumerable<VirtualSnapshotSpan> selectedSpans,
			XDocument xmlDocumentSyntax,
			IEditorOperations? editorOperations = null,
			IMultiSelectionBroker? multiSelectionBroker = null)
		{
			var commentedSpans = GetCommentedSpansInSelection (xmlDocumentSyntax, selectedSpans);
			if (!commentedSpans.Any ()) {
				CommentSelection (
					textBuffer,
					selectedSpans,
					xmlDocumentSyntax,
					editorOperations,
					multiSelectionBroker);
				return;
			}

			UncommentSelection (textBuffer, selectedSpans, xmlDocumentSyntax);
		}

		/// <summary>
		/// Performs the actual insertions of comment markers around the <see cref="commentSpans"/>
		/// </summary>
		static void CommentSpans (ITextEdit edit, NormalizedSnapshotSpanCollection commentSpans)
		{
			foreach (var commentSpan in commentSpans) {
				edit.Insert (commentSpan.Start, OpenComment);
				edit.Insert (commentSpan.End, CloseComment);
			}
		}

		/// <summary>
		/// Inserts a new comment at each virtual point, materializing the virtual space if necessary
		/// </summary>
		static void CommentEmptySpans (ITextEdit edit, IEnumerable<VirtualSnapshotPoint> virtualPoints, IEditorOperations? editorOperations)
		{
			foreach (var virtualPoint in virtualPoints) {
				if (virtualPoint.IsInVirtualSpace) {
					string leadingWhitespace;
					if (editorOperations is not null) {
						leadingWhitespace = editorOperations.GetWhitespaceForVirtualSpace (virtualPoint);
					} else {
						leadingWhitespace = new string (' ', virtualPoint.VirtualSpaces);
					}

					if (leadingWhitespace.Length > 0) {
						edit.Insert (virtualPoint.Position, leadingWhitespace);
					}
				}

				edit.Insert (virtualPoint.Position, OpenComment);
				edit.Insert (virtualPoint.Position, CloseComment);
			}
		}

		/// <summary>
		/// Removes the comment markers from a set of spans
		/// </summary>
		static void UncommentSpans (ITextEdit edit, IEnumerable<SnapshotSpan> commentedSpans)
		{
			int beginCommentLength = OpenComment.Length;
			int endCommentLength = CloseComment.Length;

			foreach (var commentSpan in commentedSpans) {
				edit.Delete (commentSpan.Start, beginCommentLength);
				edit.Delete (commentSpan.End - endCommentLength, endCommentLength);
			}
		}

		/// <summary>
		/// Calculates the syntactically valid non-commented portions of a set of spans
		/// excluding the already commented portions.
		/// </summary>
		static NormalizedSnapshotSpanCollection GetCommentableSpansInSelection (XDocument xmlDocumentSyntax, IEnumerable<SnapshotSpan> selectedSpans)
		{
			var commentSpans = new List<SnapshotSpan> ();
			var snapshot = selectedSpans.First ().Snapshot;

			var validSpans = xmlDocumentSyntax.GetValidCommentSpans (selectedSpans.Select (s => GetDesiredCommentSpan (s)));

			foreach (var singleValidSpan in validSpans) {
				var snapshotSpan = new SnapshotSpan (snapshot, new Span (singleValidSpan.Start, singleValidSpan.Length));
				commentSpans.Add (snapshotSpan);
			}

			return new NormalizedSnapshotSpanCollection (commentSpans);
		}

		/// <summary>
		/// Returns the already commented portions intersecting a set of spans.
		/// </summary>
		static IEnumerable<SnapshotSpan> GetCommentedSpansInSelection (XDocument xmlDocumentSyntax, IEnumerable<VirtualSnapshotSpan> selectedSpans)
		{
			var commentedSpans = new List<SnapshotSpan> ();
			var snapshot = selectedSpans.First ().Snapshot;

			foreach (var selectedSpan in selectedSpans) {
				bool allowLineUncomment = true;

				if (selectedSpan.IsEmpty) {
					// For point selection, first see which comments are returned for the point span
					// If the strictly inside a commented node, just uncommented that node
					// otherwise, allow line uncomment
					var start = selectedSpan.Start.Position.Position;
					var selectionCommentedSpans =
						xmlDocumentSyntax.GetCommentedSpans (new[] { new TextSpan (start, 0) }).ToList ();
					foreach (var selectionCommentedSpan in selectionCommentedSpans) {
						if (selectionCommentedSpan.Contains (start) &&
							selectionCommentedSpan.Start != start &&
							selectionCommentedSpan.End != start) {
							var snapshotSpan = new SnapshotSpan (snapshot, selectionCommentedSpan.Start, selectionCommentedSpan.Length);
							commentedSpans.Add (snapshotSpan);
							allowLineUncomment = false;
							break;
						}
					}
				}

				if (allowLineUncomment) {
					var desiredCommentSpan = GetDesiredCommentSpan (selectedSpan.SnapshotSpan);
					var commentedSpans2 = xmlDocumentSyntax.GetCommentedSpans (new[] { desiredCommentSpan });
					foreach (var commentedSpan2 in commentedSpans2) {
						var snapshotSpan = new SnapshotSpan (snapshot, commentedSpan2.Start, commentedSpan2.Length);
						commentedSpans.Add (snapshotSpan);
					}
				}
			}

			return commentedSpans;
		}

		/// <summary>
		/// Expands the selection to the non-whitespace portion of the current line
		/// in case the caret is in whitespace to the left from the XML to comment/uncomment
		/// </summary>
		static TextSpan GetDesiredCommentSpan (SnapshotSpan selectedSpan)
		{
			ITextSnapshot snapshot = selectedSpan.Snapshot;
			if (!selectedSpan.IsEmpty) {
				int selectionLength = selectedSpan.Length;

				// tweak the selection end to not include the last line break
				while (selectionLength > 0 && IsLineBreak (snapshot[selectedSpan.Start + selectionLength - 1])) {
					selectionLength--;
				}

				return new TextSpan (selectedSpan.Start, selectionLength);
			}

			// Comment line for empty selections (first to last non-whitespace character)
			var line = snapshot.GetLineFromPosition (selectedSpan.Start);

			int? start = null;
			for (int i = line.Start; i < line.End.Position; i++) {
				if (!XmlChar.IsWhitespace (snapshot[i])) {
					start = i;
					break;
				}
			}

			if (start == null) {
				return new TextSpan (selectedSpan.Start, 0);
			}

			int end = start.Value;
			for (int i = line.End.Position - 1; i >= end; i--) {
				if (!XmlChar.IsWhitespace (snapshot[i])) {
					// need to add 1 since end is exclusive
					end = i + 1;
					break;
				}
			}

			return TextSpan.FromBounds (start.Value, end);

			bool IsLineBreak (char c)
			{
				return c == '\n' || c == '\r';
			}
		}
	}

	static class CommentUtilities
	{
		public static IEnumerable<TextSpan> GetValidCommentSpans (this XContainer node, IEnumerable<TextSpan> selectedSpans)
			=> GetCommentSpans (node, selectedSpans, returnComments: false);

		public static IEnumerable<TextSpan> GetCommentedSpans (this XContainer node, IEnumerable<TextSpan> selectedSpans)
			=> GetCommentSpans (node, selectedSpans, returnComments: true);

		static IEnumerable<TextSpan> GetCommentSpans (this XContainer node, IEnumerable<TextSpan> selectedSpans, bool returnComments)
		{
			var commentSpans = new List<TextSpan> ();

			// First unify, normalize and deduplicate syntactic spans since multiple selections can result
			// in a single syntactic span to be commented
			var regions = new List<Span> ();
			foreach (var selectedSpan in selectedSpans) {
				var region = node.GetValidCommentRegion (selectedSpan);
				regions.Add (region.ToSpan ());
			}

			var normalizedRegions = new NormalizedSpanCollection (regions);

			// Then for each region cut out the existing comments that may be inside
			foreach (var currentRegion in normalizedRegions) {
				int currentStart = currentRegion.Start;

				// Creates comments such that current comments are excluded
				foreach (var comment in node.GetNodesIntersectingRange (currentRegion.ToTextSpan ()).OfType<XComment> ()) {
					var commentNodeSpan = comment.Span;
					if (returnComments)
						commentSpans.Add (commentNodeSpan);
					else {
						var validCommentSpan = TextSpan.FromBounds (currentStart, commentNodeSpan.Start);
						if (validCommentSpan.Length != 0) {
							commentSpans.Add (validCommentSpan);
						}

						currentStart = commentNodeSpan.End;
					}
				}

				if (!returnComments) {
					if (currentStart <= currentRegion.End) {
						var remainingCommentSpan = TextSpan.FromBounds (currentStart, currentRegion.End);
						if (remainingCommentSpan.Equals (currentRegion) || remainingCommentSpan.Length != 0) {
							// Comment any remaining uncommented area
							commentSpans.Add (remainingCommentSpan);
						}
					}
				}
			}

			return commentSpans.Distinct ();
		}

		static TextSpan ToTextSpan (this Span span)
		{
			return new TextSpan (span.Start, span.Length);
		}

		static Span ToSpan (this TextSpan textSpan)
		{
			return new Span (textSpan.Start, textSpan.Length);
		}

		static TextSpan GetValidCommentRegion (this XContainer node, TextSpan commentSpan)
		{
			var commentSpanStart = GetCommentRegion (node, commentSpan.Start, commentSpan);

			if (commentSpan.Length == 0) {
				return commentSpanStart;
			}

			var commentSpanEnd = GetCommentRegion (node, commentSpan.End - 1, commentSpan);

			return TextSpan.FromBounds (
				start: Math.Min (commentSpanStart.Start, commentSpanEnd.Start),
				end: Math.Max (Math.Max (commentSpanStart.End, commentSpanEnd.End), commentSpan.End));
		}

		static TextSpan GetCommentRegion (this XContainer node, int position, TextSpan span)
		{
			var nodeAtPosition = node.FindAtOffset (position);

			// if the selection starts or ends in text, we want to preserve the 
			// exact span the user has selected and split the text at that boundary
			if (nodeAtPosition is XText || nodeAtPosition is null) {
				return span;
			}

			if (nodeAtPosition is XComment ||
				nodeAtPosition is XProcessingInstruction ||
				nodeAtPosition is XCData) {
				return nodeAtPosition.Span;
			}

			var nearestParentElement = nodeAtPosition.SelfAndParentsOfType<XElement> ().FirstOrDefault ();
			if (nearestParentElement == null) {
				return new TextSpan (position, 0);
			}

			var endSpan = nearestParentElement.ClosingTag;
			if (endSpan == null) {
				return nodeAtPosition.Span;
			}

			int start = nearestParentElement.Span.Start;
			return new TextSpan (start, endSpan.Span.End - start);
		}
	}
}