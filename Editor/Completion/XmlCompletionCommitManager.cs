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
using MonoDevelop.Xml.Parser;

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
			case XmlCompletionItemKind.MultipleClosingTags:
			case XmlCompletionItemKind.ClosingTag:
				InsertClosingTags (session, buffer, item);
				return CommitResult.Handled;
			}

			LoggingService.LogWarning ($"MSBuild commit manager did not handle unknown special completion kind {kind}");
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
			var stack = item.Properties.GetProperty<NodeStack> (typeof (NodeStack));
			var elements = stack.OfType<XElement> ().TakeWhile (t => {
				if (insertTillName == null) {
					return false;
				}
				if (t.Name.FullName == insertTillName) {
					insertTillName = null;
				}
				return true;
			}).ToList ();

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
	}
}