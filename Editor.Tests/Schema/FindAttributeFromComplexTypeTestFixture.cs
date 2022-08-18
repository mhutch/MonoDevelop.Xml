using MonoDevelop.Xml.Editor.Completion;
using NUnit.Framework;
using System.Xml.Schema;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Element that has a single attribute.
	/// </summary>
	[TestFixture]
	public class FindAttributeFromComplexTypeFixture : SchemaTestFixtureBase
	{
		XmlSchemaAttribute attribute;
		XmlSchemaAttribute missingAttribute;
		
		public override void FixtureInit()
		{
			var path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));

			var schema = ((XmlSchemaCompletionProvider)SchemaCompletionData).Schema;
			XmlSchemaElement element = schema.FindElement(path);
			if (element is not null) {
				attribute = schema.FindAttribute (element, "name");
				missingAttribute = schema.FindAttribute(element, "missing");
			}
		}
		
		[Test]
		public void AttributeFound()
		{
			Assert.IsNotNull(attribute);
		}		
		
		[Test]
		public void CannotFindUnknownAttribute()
		{
			Assert.IsNull(missingAttribute);
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"    <xs:element name=\"note\">\r\n" +
				"        <xs:complexType>\r\n" +
				"            <xs:attribute name=\"name\"  type=\"xs:string\"/>\r\n" +
				"        </xs:complexType>\r\n" +
				"    </xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
