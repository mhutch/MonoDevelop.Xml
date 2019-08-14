// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Editor.Completion
{
	class XmlCompletionCommitManager : IAsyncCompletionCommitManager
	{
		readonly XmlCompletionCommitManagerProvider provider;

		public XmlCompletionCommitManager (XmlCompletionCommitManagerProvider provider)
		{
			this.provider = provider;
		}

		public IEnumerable<char> PotentialCommitCharacters => Array.Empty<char> ();

		public bool ShouldCommitCompletion (IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
		{
			return false;
		}

		public CommitResult TryCommit (IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
		{
			if (!item.TryGetKind (out var kind)) {
				return CommitResult.Unhandled;
			}

			switch (kind) {
			case XmlCompletionItemKind.SelfClosingElement: {
					if (typedChar != '>') {
						string insertionText = $"{item.InsertText}/>";
						Insert (session, buffer, insertionText);
						ShiftCaret (session, 2, XmlCaretDirection.Left);
						return CommitResult.Handled;
					} else {
						goto case XmlCompletionItemKind.Element;
					}
				}
			case XmlCompletionItemKind.Element: {
					string insertionText = $"{item.InsertText}></{item.InsertText}>";
					Insert (session, buffer, insertionText);
					ShiftCaret (session, item.InsertText.Length + 3, XmlCaretDirection.Left);
					return CommitResult.Handled;
				}
			case XmlCompletionItemKind.Attribute: {
					string insertionText = $"{item.InsertText}=\"\"";
					Insert (session, buffer, insertionText);
					ShiftCaret (session, 1, XmlCaretDirection.Left);
					return CommitResult.Handled;
				}
			case XmlCompletionItemKind.AttributeValue: {
					string insertionText = $"{item.InsertText}";
					Insert (session, buffer, insertionText);
					ShiftCaret (session, 0, XmlCaretDirection.Right);
					return CommitResult.Handled;
				}
			case XmlCompletionItemKind.MultipleClosingTags:
			case XmlCompletionItemKind.ClosingTag: {
					InsertClosingTags (session, buffer, item);
					return CommitResult.Handled;
				}
			}

			LoggingService.LogWarning ($"XML commit manager did not handle unknown special completion kind {kind}");
			return CommitResult.Unhandled;
		}

		void Insert (IAsyncCompletionSession session, ITextBuffer buffer, string text)
		{
			var span = session.ApplicableToSpan.GetSpan (buffer.CurrentSnapshot);

			var bufferEdit = buffer.CreateEdit ();
			bufferEdit.Replace (span, text);
			bufferEdit.Apply ();
		}

		void InsertClosingTags (IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item)
		{
			var insertTillName = item.InsertText.Substring (1);
			var stack = item.Properties.GetProperty<List<XObject>> (typeof (List<XObject>));
			var elements = new List<XElement> ();
			for (int i = stack.Count - 1; i >= 0; i--) {
				if (stack[i] is XElement el) {
					elements.Add (el);
					if (el.Name.FullName == insertTillName) {
						break;
					}
				}
			}

			ITextSnapshot snapshot = buffer.CurrentSnapshot;
			var span = session.ApplicableToSpan.GetSpan (snapshot);

			// if this completion is the first thing on the current line, reindent the current line
			var thisLine = snapshot.GetLineFromPosition (span.Start);
			var thisLineFirstNonWhitespaceOffset = thisLine.GetFirstNonWhitespaceOffset ();
			var replaceFirstIndent = thisLineFirstNonWhitespaceOffset.HasValue && thisLineFirstNonWhitespaceOffset + thisLine.Start == span.Start;
			if (replaceFirstIndent) {
				if (thisLine.LineNumber > 0) {
					var prevLine = snapshot.GetLineFromLineNumber (thisLine.LineNumber - 1);
					span = new SnapshotSpan (prevLine.EndIncludingLineBreak, span.End);
				} else {
					span = new SnapshotSpan (thisLine.Start, span.End);
				}
			} else {
				//extend the span back a char, it doesn't include the <
				span = new SnapshotSpan (span.Start - 1, span.Length + 1);
			}

			var newLine = snapshot.GetText (thisLine.End, thisLine.EndIncludingLineBreak - thisLine.End);

			var sb = new StringBuilder ();
			foreach (var element in elements) {
				var line = snapshot.GetLineFromPosition (element.Span.Start);
				var firstNonWhitespaceOffset = line.GetFirstNonWhitespaceOffset ();
				bool isFirstOnLine = firstNonWhitespaceOffset + line.Start == element.Span.Start;
				// if the element we're closing started a line, put the closing tag on its own line with matching indentation
				// unless it's on the current line
				if (isFirstOnLine && line.LineNumber != thisLine.LineNumber) {
					var whitespaceSpan = new Span (line.Start, firstNonWhitespaceOffset.Value);
					var whitespace = snapshot.GetText (whitespaceSpan);
					sb.Append (newLine);
					sb.Append (whitespace);
				}
				sb.Append ($"</{element.Name.FullName}>");
			}

			var bufferEdit = buffer.CreateEdit ();
			bufferEdit.Replace (span, sb.ToString ());
			bufferEdit.Apply ();
		}

		private static void ShiftCaret (IAsyncCompletionSession session, int len, XmlCaretDirection caretDirection)
		{
			switch (caretDirection) {
			case XmlCaretDirection.Left:
				for (int i = 0; i < len; i++) {
					session.TextView.Caret.MoveToPreviousCaretPosition ();
				}
				return;
			case XmlCaretDirection.Right:
				for (int i = 0; i < len; i++) {
					session.TextView.Caret.MoveToNextCaretPosition ();
				}
				return;
			case XmlCaretDirection.Top:
				//TO DO
				throw new ArgumentException ($"Unsupported value '{caretDirection}'");
			case XmlCaretDirection.Down:
				//TO DO
				throw new ArgumentException ($"Unsupported value '{caretDirection}'");
			}
			return;
		}
	}
}