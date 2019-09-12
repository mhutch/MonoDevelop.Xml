// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Options;

namespace MonoDevelop.Xml.Editor.Completion
{
	class XmlCompletionCommitManager : IAsyncCompletionCommitManager
	{
		static readonly char[] allCommitChars = { '>', '/', '=', ' ', ';', '"', '\'' };
		static readonly char[] attributeCommitChars = { '>', '/', '=' };
		static readonly char[] tagCommitChars = { '>', '/', ' ' };
		static readonly char[] entityCommitChars = { ';' };
		static readonly char[] attributeValueCommitChars = { '"', '\'' };

		readonly XmlCompletionCommitManagerProvider provider;

		public XmlCompletionCommitManager (XmlCompletionCommitManagerProvider provider)
		{
			this.provider = provider;
		}

		public IEnumerable<char> PotentialCommitCharacters => allCommitChars;

		public bool ShouldCommitCompletion (IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
		{
			if (Array.IndexOf (allCommitChars, typedChar) < 0) {
				return false;
			}

			// only handle sessions that XML completion participated in
			// although we aren't told what exact item we might be committing yet, the trigger tells us enough
			// about the kind of item to allow us to specialize the commit chars
			if (!session.Properties.TryGetProperty (typeof (XmlCompletionTrigger), out XmlCompletionTrigger kind)) {
				return false;
			};

			switch (kind) {
			case XmlCompletionTrigger.Element:
			case XmlCompletionTrigger.ElementWithBracket:
				// allow using / as a commit char for elements as self-closing elements, but special case disallowing it
				// in the cases where that could conflict with typing the / at the start of a closing tag
				if (typedChar == '/') {
					var span = session.ApplicableToSpan.GetSpan (location.Snapshot);
					if (span.Length == (kind == XmlCompletionTrigger.Element ? 0 : 1)) {
						return false;
					}
				}
				return Array.IndexOf (tagCommitChars, typedChar) > -1;

			case XmlCompletionTrigger.Attribute:
				return Array.IndexOf (attributeCommitChars, typedChar) > -1;

			case XmlCompletionTrigger.AttributeValue:
				return Array.IndexOf (attributeValueCommitChars, typedChar) > -1;

			case XmlCompletionTrigger.DocType:
				return Array.IndexOf (tagCommitChars, typedChar) > -1;

			case XmlCompletionTrigger.Entity:
				return Array.IndexOf (entityCommitChars, typedChar) > -1;
			}

			return false;
		}

		static readonly CommitResult CommitSwallowChar = new CommitResult (true, CommitBehavior.SuppressFurtherTypeCharCommandHandlers);

		static readonly CommitResult CommitCancel = new CommitResult (true, CommitBehavior.CancelCommit);

		public CommitResult TryCommit (IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
		{
			if (!item.TryGetKind (out var kind)) {
				return CommitResult.Unhandled;
			}

			var span = session.ApplicableToSpan.GetSpan (buffer.CurrentSnapshot);
			bool wasTypedInFull = span.Length == item.InsertText.Length;

			switch (kind) {
			case XmlCompletionItemKind.SelfClosingElement: {
					//comitting self-closing element with > makes it non-self-closing
					if (typedChar == '>') {
						goto case XmlCompletionItemKind.Element;
					}

					ConsumeTrailingChar (ref span, '>');

					string insertionText = $"{item.InsertText}/>";
					Insert (session, buffer, insertionText, span);
					SetCaretSpanOffset (item.InsertText.Length);

					// don't insert double /
					if (typedChar == '/' && !wasTypedInFull) {
						return CommitSwallowChar;
					}

					return CommitResult.Handled;
				}
			case XmlCompletionItemKind.Attribute: {
					//completion shouldn't interfere with typing out in full
					//TODO insert overtypeable quotes after the =
					if (typedChar == '=') {
						Insert (session, buffer, item.InsertText, span);
						return CommitResult.Handled;
					}

					string insertionText = session.TextView.Options.GetAutoInsertAttributeValue ()
						? $"{item.InsertText}=\"\""
						: item.InsertText;

					Insert (session, buffer, insertionText, span);
					SetCaretSpanOffset (insertionText.Length - 1);
					return CommitResult.Handled;
				}
			case XmlCompletionItemKind.Element:
			case XmlCompletionItemKind.AttributeValue: {
					Insert (session, buffer, item.InsertText, span);
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

			void SetCaretSpanOffset (int spanOffset)
				=> session.TextView.Caret.MoveTo (
					new SnapshotPoint (buffer.CurrentSnapshot, span.Start.Position + spanOffset));
		}



		static void ConsumeTrailingChar (ref SnapshotSpan span, char charToConsume)
		{
			var snapshot = span.Snapshot;
			if (snapshot.Length > span.End && snapshot[span.End] == charToConsume) {
				span = new SnapshotSpan (snapshot, span.Start, span.Length + 1);
			}
		}

		static void Insert (IAsyncCompletionSession session, ITextBuffer buffer, string text, SnapshotSpan span)
		{
			var bufferEdit = buffer.CreateEdit ();
			bufferEdit.Replace (span, text);
			bufferEdit.Apply ();
		}

		static void InsertClosingTags (IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item)
		{
			// completion may or may not include it depending how it was triggered
			bool includesBracket = item.InsertText[0] == '<';

			var insertTillName = item.InsertText.Substring (includesBracket? 2 : 1);
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

			// extend the span back to include the <, this logic assumes it's included
			if (!includesBracket) {
				span = new SnapshotSpan (span.Start - 1, span.Length + 1);
			}

			// if this completion is the first thing on the current line, reindent the current line
			var thisLine = snapshot.GetLineFromPosition (span.Start);
			var thisLineFirstNonWhitespaceOffset = thisLine.GetFirstNonWhitespaceOffset ();
			var replaceFirstIndent = thisLineFirstNonWhitespaceOffset.HasValue && thisLineFirstNonWhitespaceOffset + thisLine.Start == span.Start;
			if (replaceFirstIndent) {
				if (thisLine.LineNumber > 0) {
					var prevLine = snapshot.GetLineFromLineNumber (thisLine.LineNumber - 1);
					span = new SnapshotSpan (prevLine.End, span.End);
				} else {
					span = new SnapshotSpan (thisLine.Start, span.End);
				}
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

			ConsumeTrailingChar (ref span, '>');

			var bufferEdit = buffer.CreateEdit ();
			bufferEdit.Replace (span, sb.ToString ());
			bufferEdit.Apply ();
		}
	}
}