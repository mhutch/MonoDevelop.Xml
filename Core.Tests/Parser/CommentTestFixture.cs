using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class CommentTestFixture
	{
		public virtual XmlRootState CreateRootState ()
		{
			return new XmlRootState ();
		}

		[TestCase ("<r><!-- text$ --></r>", " text ")]
		[TestCase ("<r><!-- < $--></r>", " < ")]
		[TestCase ("<r><!-- <! $--></r>", " <! ")]
		[TestCase ("<r><!-- <!- $--></r>", " <!- ")]
		public void Comment (string document, string innerText)
		{
			var parser = new XmlTreeParser (CreateRootState ());
			var result = parser.Parse (document,
				() => {
					parser.AssertStateIs<XmlCommentState> ();
				});

			var comment = result.doc.RootElement?.FirstChild as XComment;

			Assert.IsNotNull (comment);
			Assert.AreEqual (innerText, comment.InnerText);
		}

		[TestCase ("<r><!-- ", 8)]
		public void IncompleteCommentEof (string document, int startOffset)
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (document);

			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.IncompleteCommentEof, startOffset, 0),
				(XmlCoreDiagnostics.IncompleteTagEof, startOffset, 0)
				);
		}

		[Test]
		public void IncompleteEndCommentEof ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse ("<r><!-- -- --></r>");

			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.IncompleteEndComment, 10, 0)
				);
		}
	}
}
