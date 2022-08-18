// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;

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
		[ImportingConstructor]
		public XmlCompletionCommitManagerProvider (JoinableTaskContext joinableTaskContext, ISmartIndentationService smartIndentationService, IEditorCommandHandlerServiceFactory commandServiceFactory, ILogger<XmlCompletionCommitManager> logger)
		{
			JoinableTaskContext = joinableTaskContext;
			SmartIndentationService = smartIndentationService;
			CommandServiceFactory = commandServiceFactory;
			Logger = logger;
		}

		public JoinableTaskContext JoinableTaskContext { get; }
		public ISmartIndentationService SmartIndentationService { get; }
		public IEditorCommandHandlerServiceFactory CommandServiceFactory { get; }
		public ILogger Logger { get; }

		public IAsyncCompletionCommitManager GetOrCreate (ITextView textView) =>
			textView.Properties.GetOrCreateSingletonProperty (
				typeof (XmlCompletionCommitManager), () => new XmlCompletionCommitManager (this)
			);
	}
}