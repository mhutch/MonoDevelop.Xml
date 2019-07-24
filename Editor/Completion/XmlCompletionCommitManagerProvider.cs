// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Completion
{
	[Export (typeof (IAsyncCompletionCommitManagerProvider))]
	[Name ("XML Completion Commit Manager Provider")]
	[ContentType (XmlContentTypeNames.XmlCore)]
	class XmlCompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
	{
		[Import]
		public JoinableTaskContext JoinableTaskContext { get; set; }

		[Import]
		public ISmartIndentationService SmartIndentationService { get; set; }

		[Import]
		public IEditorCommandHandlerServiceFactory CommandServiceFactory { get; set; }

		public IAsyncCompletionCommitManager GetOrCreate (ITextView textView) =>
			textView.Properties.GetOrCreateSingletonProperty (
				typeof (XmlCompletionCommitManager), () => new XmlCompletionCommitManager (this)
			);
	}
}