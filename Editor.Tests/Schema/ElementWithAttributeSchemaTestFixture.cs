using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using MonoDevelop.Xml.Editor.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Element that has a single attribute.
	/// </summary>
	[TestFixture]
	public class ElementWithAttributeSchemaTestFixture : SchemaTestFixtureBase
	{
		CompletionContext attributeCompletionData;
		string attributeName;
		
		async Task Init ()
		{
			if (attributeCompletionData != null)
				return;
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));
						
			attributeCompletionData = await SchemaCompletionData.GetAttributeCompletionDataAsync (DummyCompletionSource.Instance, path, CancellationToken.None);
			attributeName = attributeCompletionData.ItemList[0].DisplayText;
		}

		[Test]
		public async Task AttributeCount()
		{
			await Init ();
			Assert.AreEqual(1, attributeCompletionData.ItemList.Count, "Should be one attribute.");
		}
		
		[Test]
		public async Task AttributeName()
		{
			await Init ();
			Assert.AreEqual("name", attributeName, "Attribute name is incorrect.");
		}
		
		[Test]
		public async Task NoAttributesForUnknownElement()
		{
			await Init ();
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("foobar", "http://www.w3schools.com"));
			var attributes = await SchemaCompletionData.GetAttributeCompletionDataAsync (DummyCompletionSource.Instance, path, CancellationToken.None);
			
			Assert.AreEqual(0, attributes.ItemList.Count, "Should not find attributes for unknown element.");
		}
		
		protected override string GetSchema()
		{
			return "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"http://www.w3schools.com\" xmlns=\"http://www.w3schools.com\" elementFormDefault=\"qualified\">\r\n" +
				"    <xs:element name=\"note\">\r\n" +
				"        <xs:complexType>\r\n" +
				"\t<xs:attribute name=\"name\"  type=\"xs:string\"/>\r\n" +
				"        </xs:complexType>\r\n" +
				"    </xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
