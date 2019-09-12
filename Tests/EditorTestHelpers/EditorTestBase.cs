// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
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
			var buffer = Catalog.BufferFactoryService.CreateTextBuffer (documentText, ContentType);
			if (filename != null) {
				Catalog.TextDocumentFactoryService.CreateTextDocument (buffer, filename);
			}
			return Catalog.TextViewFactory.CreateTextView (buffer);
		}

		protected (string document, int caretOffset) ExtractCaret (string document, char caretMarkerChar)
		{
			var caretOffset = document.IndexOf (caretMarkerChar);
			if (caretOffset < 0) {
				throw new ArgumentException ("Document does not contain a caret marker");
			}
			return (document.Substring (0, caretOffset) + document.Substring (caretOffset + 1), caretOffset);
		}

		public void TestCommands (
			string beforeDocumentText, string afterDocumentText,
			Action<IEditorCommandHandlerService> command,
			string filename = default, char caretMarkerChar = '$')
		{
			TestCommands (beforeDocumentText,
				afterDocumentText,
				new[] { command },
				filename,
				caretMarkerChar);
		}

		public void TestCommands (
			string beforeDocumentText, string afterDocumentText,
			IEnumerable<Action<IEditorCommandHandlerService>> commands,
			string filename = default, char caretMarkerChar = '$')
		{
			int beforeCaretOffset, afterCaretOffset;
			(beforeDocumentText, beforeCaretOffset) = ExtractCaret (beforeDocumentText, caretMarkerChar);
			(afterDocumentText, afterCaretOffset) = ExtractCaret (afterDocumentText, caretMarkerChar);
			TestCommands (
				beforeDocumentText, beforeCaretOffset,
				afterDocumentText, afterCaretOffset,
				commands,
				filename);
		}

		public void TestCommands (
			string beforeDocumentText, int beforeCaretOffset,
			string afterDocumentText, int afterCaretOffset,
			IEnumerable<Action<IEditorCommandHandlerService>> commands,
			string filename = default)
		{
			var textView = CreateTextView (beforeDocumentText, filename);
			textView.Caret.MoveTo (new SnapshotPoint (textView.TextBuffer.CurrentSnapshot, beforeCaretOffset));

			var commandService = Catalog.CommandServiceFactory.GetService (textView);

			foreach (var c in commands) {
				c (commandService);

				//ensure the computation is completed before we continue typing
				if (textView.Properties.TryGetProperty (typeof (IAsyncCompletionSession), out IAsyncCompletionSession session)) {
					session.GetComputedItems (CancellationToken.None);
				}
			}

			Assert.AreEqual (afterDocumentText, textView.TextBuffer.CurrentSnapshot.GetText ());
			Assert.AreEqual (afterCaretOffset, textView.Caret.Position.BufferPosition.Position);
		}
	}
}
