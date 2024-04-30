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
			Func<ITextView, Task> initialize = null,
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
			Func<ITextView, Task> initialize = null,
			CancellationToken cancellationToken = default)
		{
			(beforeDocumentText, int beforeCaretOffset) = TextWithMarkers.ExtractSinglePosition (beforeDocumentText, caretMarkerChar);
			(afterDocumentText, int afterCaretOffset) = TextWithMarkers.ExtractSinglePosition (afterDocumentText, caretMarkerChar);

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
			Func<ITextView,Task> initialize = null,
			CancellationToken cancellationToken = default)
		{
			await test.Catalog.JoinableTaskContext.Factory.SwitchToMainThreadAsync (cancellationToken);

			var textView = test.CreateTextView (beforeDocumentText, filename);
			textView.Caret.MoveTo (new SnapshotPoint (textView.TextBuffer.CurrentSnapshot, beforeCaretOffset));

			if (initialize is not null) {
				await initialize (textView);
			}

			var commandService = test.Catalog.CommandServiceFactory.GetService (textView);

			foreach (var c in commands) {
				c (commandService);
			}

			Assert.AreEqual (afterDocumentText, textView.TextBuffer.CurrentSnapshot.GetText ());
			Assert.AreEqual (afterCaretOffset, textView.Caret.Position.BufferPosition.Position);
		}
	}
}
