// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Completion
{
	[Export (typeof (ICompletionPresenterProvider))]
	[Name (nameof (TestCompletionPresenterProvider))]
	[ContentType ("any")]
	class TestCompletionPresenterProvider : ICompletionPresenterProvider
	{
		public CompletionPresenterOptions Options { get; } = new CompletionPresenterOptions (10);

		public ICompletionPresenter GetOrCreate (ITextView textView) => presenter;

		TestCompletionPresenter presenter = new TestCompletionPresenter ();
	}

	class TestCompletionPresenter : ICompletionPresenter
	{
		#pragma warning disable 67
		public event EventHandler<CompletionFilterChangedEventArgs> FiltersChanged;
		public event EventHandler<CompletionItemSelectedEventArgs> CompletionItemSelected;
		public event EventHandler<CompletionItemEventArgs> CommitRequested;
		public event EventHandler<CompletionClosedEventArgs> CompletionClosed;
		#pragma warning restore 67

		public void Close ()
		{
		}

		public void Dispose ()
		{
			throw new NotImplementedException ();
		}

		public void Open (IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
		{
		}

		public void Update (IAsyncCompletionSession session, CompletionPresentationViewModel presentation)
		{
		}
	}
}
