using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using MonoDevelop.Xml.Editor.Completion;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Schema
{
	/// <summary>
	/// Retrieve completion data for an xml schema that specifies only one 
	/// element.
	/// </summary>
	[TestFixture]
	public class SingleElementSchemaTestFixture : SchemaTestFixtureBase
	{
		CompletionContext childElementCompletionData;
		CompletionContext attributeCompletionData;
		
		async Task Init ()
		{
			if (attributeCompletionData != null)
				return;
			
			XmlElementPath path = new XmlElementPath();
			path.Elements.Add(new QualifiedName("note", "http://www.w3schools.com"));

			attributeCompletionData = 
				await SchemaCompletionData.GetAttributeCompletionDataAsync (DummyCompletionSource.Instance, path, CancellationToken.None);

			childElementCompletionData = 
				await SchemaCompletionData.GetChildElementCompletionDataAsync (DummyCompletionSource.Instance, path, CancellationToken.None);
		}
		
		[Test]
		public async Task NamespaceUri()
		{
			await Init ();
			Assert.AreEqual("http://www.w3schools.com", SchemaCompletionData.NamespaceUri, "Unexpected namespace.");
		}
		
		[Test]
		public async Task NoteElementHasNoAttributes()
		{
			await Init ();
			Assert.AreEqual(0, attributeCompletionData.ItemList.Count, "Not expecting any attributes.");
		}
		
		[Test]
		public async Task NoteElementHasNoChildElements()
		{
			await Init ();
			Assert.AreEqual(0, childElementCompletionData.ItemList.Count, "Not expecting any child elements.");
		}
		
		protected override string GetSchema()
		{
			return "<?xml version=\"1.0\"?>\r\n" +
				"<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"\r\n" +
				"targetNamespace=\"http://www.w3schools.com\"\r\n" +
				"xmlns=\"http://www.w3schools.com\"\r\n" +
				"elementFormDefault=\"qualified\">\r\n" +
				"<xs:element name=\"note\">\r\n" +
				"</xs:element>\r\n" +
				"</xs:schema>";
		}
	}
}
