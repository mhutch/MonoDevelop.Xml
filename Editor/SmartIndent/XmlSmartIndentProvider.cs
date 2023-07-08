// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor.Logging;
using MonoDevelop.Xml.Editor.Parsing;

namespace MonoDevelop.Xml.Editor.SmartIndent
{
	[Export (typeof (ISmartIndentProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	class XmlSmartIndentProvider : ISmartIndentProvider
	{
		readonly XmlParserProvider parserProvider;
		readonly IEditorLoggerFactory loggerFactory;

		[ImportingConstructor]
		public XmlSmartIndentProvider (XmlParserProvider parserProvider, IEditorLoggerFactory loggerFactory)
		{
			this.parserProvider = parserProvider;
			this.loggerFactory = loggerFactory;
		}

		public ISmartIndent CreateSmartIndent (ITextView textView)
		{
			return textView.Properties.GetOrCreateSingletonProperty (() => {
				var logger = loggerFactory.CreateLogger<XmlSmartIndent> (textView);
				return new XmlSmartIndent (textView, parserProvider, logger);
			});
		}
	}
}
