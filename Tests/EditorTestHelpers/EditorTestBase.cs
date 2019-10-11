// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Tests.Completion;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.EditorTestHelpers
{
	public abstract class EditorTestBase
	{
		[OneTimeSetUp]
		public void InitializeEditorEnvironment ()
		{
			(Environment, Catalog) = InitializeEnvironment ();
		}

		protected abstract (EditorEnvironment, EditorCatalog) InitializeEnvironment ();

		public virtual EditorEnvironment Environment { get; private set; }
		public virtual EditorCatalog Catalog { get; private set; }

		protected abstract string ContentTypeName { get; }

		public virtual IContentType ContentType => Catalog.ContentTypeRegistryService.GetContentType (ContentTypeName ?? StandardContentTypeNames.Text);

		public virtual ITextView CreateTextView (string documentText, string filename = null)
		{
			var buffer = CreateTextBuffer (documentText);
			if (filename != null) {
				Catalog.TextDocumentFactoryService.CreateTextDocument (buffer, filename);
			}
			return Catalog.TextViewFactory.CreateTextView (buffer);
		}

		public virtual ITextBuffer CreateTextBuffer (string documentText)
		{
			return Catalog.BufferFactoryService.CreateTextBuffer (documentText, ContentType);
		}

		protected (string document, int caretOffset) ExtractCaret (string document, char caretMarkerChar)
		{
			var caretOffset = document.IndexOf (caretMarkerChar);
			if (caretOffset < 0) {
				throw new ArgumentException ("Document does not contain a caret marker");
			}
			return (document.Substring (0, caretOffset) + document.Substring (caretOffset + 1), caretOffset);
		}

		public Task TestCommands (
			string beforeDocumentText, string afterDocumentText,
			Action<IEditorCommandHandlerService> command,
			string filename = default, char caretMarkerChar = '$',
			Action<ITextView> initialize = null)
		{
			return TestCommands (beforeDocumentText,
				afterDocumentText,
				new[] { command },
				filename,
				caretMarkerChar,
				initialize);
		}

		public Task TestCommands (
			string beforeDocumentText, string afterDocumentText,
			IEnumerable<Action<IEditorCommandHandlerService>> commands,
			string filename = default, char caretMarkerChar = '$',
			Action<ITextView> initialize = null)
		{
			int beforeCaretOffset, afterCaretOffset;
			(beforeDocumentText, beforeCaretOffset) = ExtractCaret (beforeDocumentText, caretMarkerChar);
			(afterDocumentText, afterCaretOffset) = ExtractCaret (afterDocumentText, caretMarkerChar);
			return TestCommands (
				beforeDocumentText, beforeCaretOffset,
				afterDocumentText, afterCaretOffset,
				commands,
				filename,
				initialize);
		}

		public async Task TestCommands (
			string beforeDocumentText, int beforeCaretOffset,
			string afterDocumentText, int afterCaretOffset,
			IEnumerable<Action<IEditorCommandHandlerService>> commands,
			string filename = default,
			Action<ITextView> initialize = null)
		{
			await Catalog.JoinableTaskContext.Factory.SwitchToMainThreadAsync ();

			var textView = CreateTextView (beforeDocumentText, filename);
			textView.Caret.MoveTo (new SnapshotPoint (textView.TextBuffer.CurrentSnapshot, beforeCaretOffset));

			initialize?.Invoke (textView);

			var commandService = Catalog.CommandServiceFactory.GetService (textView);

			foreach (var c in commands) {
				c (commandService);
			}

			Assert.AreEqual (afterDocumentText, textView.TextBuffer.CurrentSnapshot.GetText ());
			Assert.AreEqual (afterCaretOffset, textView.Caret.Position.BufferPosition.Position);
		}
	}
}
