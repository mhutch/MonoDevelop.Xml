// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Xml.Editor.Tests.Extensions
{
	public static class QuickInfoTestExtensions
	{
		public static async Task<QuickInfoItemsCollection> GetQuickInfoItems (
			this EditorTest test,
			string documentText,
			char caretMarker = '$')
		{
			var caretOffset = documentText.IndexOf (caretMarker);
			if (caretOffset < 0) {
				throw new ArgumentException ("Document does not contain a caret marker", nameof (documentText));
			}
			documentText = documentText.Substring (0, caretOffset) + documentText.Substring (caretOffset + 1);

			await test.Catalog.JoinableTaskContext.Factory.SwitchToMainThreadAsync ();
			var textView = test.CreateTextView(documentText);
			return await test.GetQuickInfoItems (textView, caretOffset);
		}

		public static async Task<QuickInfoItemsCollection> GetQuickInfoItems (
			this EditorTest test,
			ITextView textView,
			int caretPosition,
			CancellationToken cancellationToken = default)
		{
			var broker = test.Catalog.AsyncQuickInfoBroker;
			var snapshot = textView.TextBuffer.CurrentSnapshot;

			var items = await broker.GetQuickInfoItemsAsync (
				textView,
				snapshot.CreateTrackingPoint (caretPosition, PointTrackingMode.Positive),
				cancellationToken
			);

			return items;
		}
	}
}
