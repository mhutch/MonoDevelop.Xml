// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

using MonoDevelop.Xml.Tests.Utils;

using NUnit.Framework;

namespace MonoDevelop.Xml.Editor.Tests.Extensions
{
	public static class EditorCommandExtensions
	{
		public static Task TestCommands (
			this EditorTest test,
			string beforeDocumentText, string afterDocumentText,
			Action<IEditorCommandHandlerService> command,
			string filename = default, char caretMarkerChar = '$',
			Action<ITextView> initialize = null,
			CancellationToken cancellationToken = default)
		{
			return test.TestCommands (
				beforeDocumentText,
				afterDocumentText,
				new[] { command },
				filename,
				caretMarkerChar,
				initialize,
				cancellationToken);
		}

		public static Task TestCommands (
			this EditorTest test,
			string beforeDocumentText, string afterDocumentText,
			IEnumerable<Action<IEditorCommandHandlerService>> commands,
			string filename = default, char caretMarkerChar = '$',
			Action<ITextView> initialize = null,
			CancellationToken cancellationToken = default)
		{
			(beforeDocumentText, int beforeCaretOffset) = SelectionHelper.ExtractCaret (beforeDocumentText, caretMarkerChar);
			(afterDocumentText, int afterCaretOffset) = SelectionHelper.ExtractCaret (afterDocumentText, caretMarkerChar);
			return test.TestCommands (
				beforeDocumentText, beforeCaretOffset,
				afterDocumentText, afterCaretOffset,
				commands,
				filename,
				initialize,
				cancellationToken);
		}

		public static async Task TestCommands (
			this EditorTest test,
			string beforeDocumentText, int beforeCaretOffset,
			string afterDocumentText, int afterCaretOffset,
			IEnumerable<Action<IEditorCommandHandlerService>> commands,
			string filename = default,
			Action<ITextView> initialize = null,
			CancellationToken cancellationToken = default)
		{
			await test.Catalog.JoinableTaskContext.Factory.SwitchToMainThreadAsync (cancellationToken);

			var textView = test.CreateTextView (beforeDocumentText, filename);
			textView.Caret.MoveTo (new SnapshotPoint (textView.TextBuffer.CurrentSnapshot, beforeCaretOffset));

			initialize?.Invoke (textView);

			var commandService = test.Catalog.CommandServiceFactory.GetService (textView);

			foreach (var c in commands) {
				c (commandService);
			}

			Assert.AreEqual (afterDocumentText, textView.TextBuffer.CurrentSnapshot.GetText ());
			Assert.AreEqual (afterCaretOffset, textView.Caret.Position.BufferPosition.Position);
		}
	}
}
