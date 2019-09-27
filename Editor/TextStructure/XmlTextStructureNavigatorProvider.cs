using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.TextStructure
{
	[Export (typeof (ITextStructureNavigatorProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	class XmlTextStructureNavigatorProvider : ITextStructureNavigatorProvider
	{
		readonly ITextStructureNavigatorSelectorService navigatorService;
		readonly IContentTypeRegistryService contentTypeRegistry;

		[ImportingConstructor]
		public XmlTextStructureNavigatorProvider (
			ITextStructureNavigatorSelectorService navigatorService,
			IContentTypeRegistryService contentTypeRegistry)
		{
			this.navigatorService = navigatorService;
			this.contentTypeRegistry = contentTypeRegistry;
		}

		public ITextStructureNavigator CreateTextStructureNavigator (ITextBuffer textBuffer)
		{
			var codeNavigator = navigatorService.CreateTextStructureNavigator (
				textBuffer,
				contentTypeRegistry.GetContentType (StandardContentTypeNames.Code)
			);
			return new XmlTextStructureNavigator (textBuffer, codeNavigator);
		}
	}
}
