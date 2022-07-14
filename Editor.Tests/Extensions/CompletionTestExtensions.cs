// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.Xml.Tests.Utils;

namespace MonoDevelop.Xml.Editor.Tests.Extensions
{
	public static class CompletionTestExtensions
	{
		public static Task<CompletionContext> GetCompletionContext (
			this EditorTest test,
			string documentText, CompletionTriggerReason reason = default, char triggerChar = '\0', char caretMarker = '$', string filename = default, CancellationToken cancellationToken = default)
		{
			int caretOffset;
			(documentText, caretOffset) = SelectionHelper.ExtractCaret (documentText, caretMarker);

			var textView = test.CreateTextView (documentText, filename);

			return test.GetCompletionContext (textView, caretOffset, reason, triggerChar, cancellationToken);
		}

		public static async Task<CompletionContext> GetCompletionContext (
			this EditorTest test,
			ITextView textView, int caretPosition, CompletionTriggerReason reason, char triggerChar, CancellationToken cancellationToken = default)
		{
			var broker = test.Catalog.AsyncCompletionBroker;
			var snapshot = textView.TextBuffer.CurrentSnapshot;

			var trigger = new CompletionTrigger (reason, snapshot, triggerChar);
			if (triggerChar != '\0') {
				snapshot = textView.TextBuffer.Insert (caretPosition, triggerChar.ToString ());
				caretPosition++;
			}

			var context = await broker.GetAggregatedCompletionContextAsync (
				textView,
				trigger,
				new SnapshotPoint (snapshot, caretPosition),
				cancellationToken
			);

			return context.CompletionContext;
		}
	}
}
