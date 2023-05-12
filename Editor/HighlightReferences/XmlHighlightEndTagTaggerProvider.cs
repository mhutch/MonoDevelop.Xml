// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.Logging;
using MonoDevelop.Xml.Editor.Tagging;

namespace MonoDevelop.MSBuild.Editor.HighlightReferences
{
	[Export(typeof(IViewTaggerProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[TagType (typeof (NavigableHighlightTag))]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	class XmlHighlightEndTagTaggerProvider : IViewTaggerProvider
	{
		public JoinableTaskContext JoinableTaskContext { get; }
		public IEditorLoggerFactory LoggerService { get; }
		public XmlParserProvider ParserProvider { get; }

		[ImportingConstructor]
		public XmlHighlightEndTagTaggerProvider (JoinableTaskContext joinableTaskContext, XmlParserProvider parserProvider, IEditorLoggerFactory loggerService)
		{
			JoinableTaskContext = joinableTaskContext;
			LoggerService = loggerService;
			ParserProvider = parserProvider;
		}

		public ITagger<T> CreateTagger<T> (ITextView textView, ITextBuffer buffer) where T : ITag
			=> (ITagger<T>)buffer.Properties.GetOrCreateSingletonProperty (() => {
				var logger = LoggerService.CreateLogger<XmlHighlightEndTagTagger> (textView);
				return new XmlHighlightEndTagTagger (textView, this, logger);
			});
	}
}
