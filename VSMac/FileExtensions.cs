using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor
{
	static class XmlFileExtensions
	{
		[Export]
		[FileExtension(".xml")]
		[ContentType(XmlContentTypeNames.Xml)]
		internal static FileExtensionToContentTypeDefinition XmlFileExtensionDefinition = null;

		[Export]
		[FileExtension(".xsl")]
		[ContentType(XmlContentTypeNames.Xslt)]
		internal static FileExtensionToContentTypeDefinition XslFileExtensionDefinition = null;

		[Export]
		[FileExtension(".xslt")]
		[ContentType(XmlContentTypeNames.Xslt)]
		internal static FileExtensionToContentTypeDefinition XsltFileExtensionDefinition = null;

		[Export]
		[FileExtension(".xsd")]
		[ContentType(XmlContentTypeNames.Xsd)]
		internal static FileExtensionToContentTypeDefinition XsdFileExtensionDefinition = null;
	}
}