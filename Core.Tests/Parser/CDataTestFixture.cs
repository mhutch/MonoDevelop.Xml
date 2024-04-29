using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class CDataTestFixture
	{
		public virtual XmlRootState CreateRootState ()
		{
			return new XmlRootState ();
		}

		[TestCase ("<r><![CDATA[ <xml /> $]]></r>", " <xml /> ")]
		[TestCase ("<r><![CDATA[ ] $]]></r>", " ] ")]
		[TestCase ("<r><![CDATA[ ]] $]]></r>", " ]] ")]
		public void CData (string document, string innerText)
		{
			var parser = new XmlTreeParser (CreateRootState ());
			var result = parser.Parse (document,
				() => {
					parser.AssertStateIs<XmlCDataState> ();
				});

			var cdata = result.doc.RootElement?.FirstChild as XCData;

			Assert.IsNotNull (cdata);
			Assert.AreEqual (innerText, cdata.InnerText);
		}

		[TestCase ("<r><![CDATA[ ", 13)]
		[TestCase ("<r><![CDATA[ ]", 14)]
		[TestCase ("<r><![CDATA[ ]]", 15)]
		[TestCase ("<r><![CDATA[ ]]</r>", 19)]
		public void IncompleteCDataEof (string document, int startOffset)
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (document);

			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.IncompleteCDataEof, startOffset, 0),
				(XmlCoreDiagnostics.IncompleteTagEof, startOffset, 0)
				);
		}
	}
}
