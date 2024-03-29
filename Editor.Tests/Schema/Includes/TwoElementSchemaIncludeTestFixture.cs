using MonoDevelop.Xml.Editor.Completion;
using NUnit.Framework;
using MonoDevelop.Xml.Editor.Tests.Utils;

namespace MonoDevelop.Xml.Tests.Schema.Includes
{
	/// <summary>
	/// Tests that schemas referenced via xs:include elements are used when
	/// generating completion data.
	/// </summary>
	[TestFixture]
	public class TwoElementSchemaIncludeTestFixture : TwoElementSchemaTestFixture
	{
		[OneTimeTearDown]
		public void FixtureTearDown()
		{
			SchemaIncludeTestFixtureHelper.FixtureTearDown();
		}
		
		internal override IXmlSchemaCompletionProvider CreateSchemaCompletionDataObject ()
			=> SchemaIncludeTestFixtureHelper.CreateSchemaCompletionDataObject(GetMainSchema(), GetSchema());
		
		string GetMainSchema()
		{
			return "<xs:schema \r\n" +
				"targetNamespace=\"http://www.w3schools.com\" \r\n" +
				"xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" \r\n" +
				"elementFormDefault=\"qualified\">\r\n" +
				"\t<xs:include schemaLocation=\"include.xsd\"/>\r\n" +
				"</xs:schema>";
		}
	}
}
