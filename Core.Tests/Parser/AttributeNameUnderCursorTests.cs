using System.Linq;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class AttributeNameUnderCursorTests
	{
		[Test]
		public void SuccessTest1()
		{
			AssertAttributeName ("<a foo$", "foo");
		}
		
		[Test]
		public void SuccessTest2()
		{
			AssertAttributeName ("<a foo=$", "foo");
		}
		
		[Test]
		public void SuccessTest3()
		{
			AssertAttributeName ("<a foo='$", "foo");
		}
		
		[Test]
		public void SuccessTest4()
		{
			AssertAttributeName ("<a type='a$", "type");
		}

		public void AssertAttributeName (string doc, string name)
		{
			TestXmlParser.Parse (doc, p => {
				p.AssertNodeIs<XAttribute>();
				p.AssertNodeName (name);
			});
		}
	}
}
