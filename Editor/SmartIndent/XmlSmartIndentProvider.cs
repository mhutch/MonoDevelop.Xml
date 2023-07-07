// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor.Parsing;

namespace MonoDevelop.Xml.Editor.SmartIndent
{
	[Export (typeof (ISmartIndentProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	class XmlSmartIndentProvider : ISmartIndentProvider
	{
		readonly XmlParserProvider parserProvider;

		[ImportingConstructor]
		public XmlSmartIndentProvider (XmlParserProvider parserProvider)
		{
			this.parserProvider = parserProvider;
		}

		public ISmartIndent CreateSmartIndent (ITextView textView)
		{
			return textView.Properties.GetOrCreateSingletonProperty (() => new XmlSmartIndent (textView, parserProvider));
		}
	}
}
