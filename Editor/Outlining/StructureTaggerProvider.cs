// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.Logging;

namespace MonoDevelop.Xml.Editor.Tagging
{
	[Export (typeof (ITaggerProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[Name (XmlContentTypeNames.Xml)]
	[TagType (typeof (IStructureTag))]
	class StructureTaggerProvider : ITaggerProvider
	{
		public IEditorLoggerService LoggerService { get; }
		public XmlParserProvider ParserProvider { get; }

		[ImportingConstructor]
		public StructureTaggerProvider (XmlParserProvider parserProvider, IEditorLoggerService loggerService)
		{
			ParserProvider = parserProvider;
			LoggerService = loggerService;
		}

		public ITagger<T> CreateTagger<T> (ITextBuffer buffer) where T : ITag =>
			(ITagger<T>)buffer.Properties.GetOrCreateSingletonProperty (() => {
				var logger = LoggerService.CreateLogger<StructureTagger> (buffer);
				return new StructureTagger (buffer, logger, this);
			});
	}
}