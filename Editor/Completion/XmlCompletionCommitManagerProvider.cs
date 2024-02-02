// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Xml.Editor.Logging;

namespace MonoDevelop.Xml.Editor.Completion;

[Export (typeof (IAsyncCompletionCommitManagerProvider))]
[Name ("XML Completion Commit Manager Provider")]
[ContentType (XmlContentTypeNames.XmlCore)]
[method: ImportingConstructor]
class XmlCompletionCommitManagerProvider (
		JoinableTaskContext joinableTaskContext, ISmartIndentationService smartIndentationService,
		IEditorCommandHandlerServiceFactory commandServiceFactory, IEditorLoggerFactory loggerService
	) : IAsyncCompletionCommitManagerProvider
{
	public JoinableTaskContext JoinableTaskContext { get; } = joinableTaskContext;
	public ISmartIndentationService SmartIndentationService { get; } = smartIndentationService;
	public IEditorCommandHandlerServiceFactory CommandServiceFactory { get; } = commandServiceFactory;
	public IEditorLoggerFactory LoggerService { get; } = loggerService;

	public IAsyncCompletionCommitManager GetOrCreate (ITextView textView)
	{
		return textView.Properties.GetOrCreateSingletonProperty (() => {
			var logger = LoggerService.CreateLogger<XmlCompletionCommitManager> (textView);
			return new XmlCompletionCommitManager (logger, JoinableTaskContext, CommandServiceFactory);
		});
	}
}