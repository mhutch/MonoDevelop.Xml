// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Xml.Editor.Completion;

namespace MonoDevelop.Xml.Editor.TextStructure
{
	[Export (typeof (ITextStructureNavigatorProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	class XmlTextStructureNavigatorProvider : ITextStructureNavigatorProvider
	{
		public ITextStructureNavigatorSelectorService NavigatorService { get; }
		public IContentTypeRegistryService ContentTypeRegistry { get; }
		public XmlParserProvider ParserProvider { get; }

		[ImportingConstructor]
		public XmlTextStructureNavigatorProvider (
			ITextStructureNavigatorSelectorService navigatorService,
			IContentTypeRegistryService contentTypeRegistry,
			XmlParserProvider parserProvider)
		{
			NavigatorService = navigatorService;
			ContentTypeRegistry = contentTypeRegistry;
			ParserProvider = parserProvider;
		}

		public ITextStructureNavigator CreateTextStructureNavigator (ITextBuffer textBuffer)
		{
			return new XmlTextStructureNavigator (textBuffer, this);
		}
	}
}
