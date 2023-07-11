// 
// ParsingTests.cs
// 
// Author:
//   Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Linq;
using System.Text;

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using MonoDevelop.Xml.Tests.Utils;

using NUnit.Framework;
using NUnit.Framework.Internal;

using XmlParserContext = MonoDevelop.Xml.Parser.XmlParserContext;

namespace MonoDevelop.Xml.Tests.Parser
{

	[TestFixture]
	public class ParsingTests
	{
		public virtual XmlRootState CreateRootState ()
		{
			return new XmlRootState ();
		}

		[Test]
		public void AttributeName ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b id=""$foo"" />
	</tag.a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlAttributeValueState> ();
					parser.AssertPath ("//doc/tag.a/tag.b/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertNoDiagnostics ();
		}

		[Test]
		public void Attributes ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a name=""foo"" arg=5 wibble = 6 bar.baz = 'y.ff7]' $ />
</doc>
",
				delegate {
					parser.AssertStateIs<XmlTagState> ();
					parser.AssertAttributes ("name", "foo", "arg", "5", "wibble", "6", "bar.baz", "y.ff7]");
				}
			);
			parser.AssertEmpty ();
			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.UnquotedAttributeValue, 30, 1),
				(XmlCoreDiagnostics.UnquotedAttributeValue, 41, 1)
			);
		}

		[Test]
		public void AttributeRecovery ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b arg='fff' sdd = sdsds= 'foo' ff = 5 $ />
	</tag.a>
<a><b valid/></a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlTagState> ();
					parser.AssertAttributes ("arg", "fff", "sdd", "sdsds", "ff", "5");
					parser.AssertDiagnostics (
						(XmlCoreDiagnostics.UnquotedAttributeValue, 41, 5),
						(XmlCoreDiagnostics.InvalidNameCharacter, 52, 0),
						(XmlCoreDiagnostics.UnquotedAttributeValue, 59, 1)
					);
				}
			);
			parser.AssertEmpty ();

			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.UnquotedAttributeValue, 41, 5),
				(XmlCoreDiagnostics.InvalidNameCharacter, 52, 0),
				(XmlCoreDiagnostics.UnquotedAttributeValue, 59, 1),
				(XmlCoreDiagnostics.IncompleteAttribute, 86, 0)
			);
		}

		[Test]
		public void IncompleteTags ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a att1 >
		<tag.b att2="" >
			<tag.c att3 = ' 
				<tag.d $ att4 = >
					<tag.e att5='' att6=' att7 = >
						<tag.f id='$foo' />
					</tag.e>
				</tag.d>
			</tag.c>
		</tag.b>
	</tag.a>
</doc>
",
				delegate {
					parser.AssertStateIs<XmlTagState> ();
					parser.AssertNodeIs<XObject> (1).AssertIncomplete ();
					parser.AssertNodeDepth (6);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.c/tag.d");
				},
				delegate {
					parser.AssertStateIs<XmlAttributeValueState> ();
					parser.AssertNodeDepth (9);
					parser.AssertNodeIs<XElement> (3).AssertName ("tag.d").AssertComplete ();
					parser.AssertNodeIs<XElement> (2).AssertName ("tag.e").AssertIncomplete ();
					parser.AssertNodeIs<XElement> (1).AssertName ("tag.f").AssertIncomplete ();
					parser.AssertNodeIs<XAttribute> ().AssertIncomplete ();
					parser.AssertPath ("//doc/tag.a/tag.b/tag.c/tag.d/tag.e/tag.f/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.IncompleteAttribute, 20, 0),
				(XmlCoreDiagnostics.MalformedNamedTag, 43, 0),
				(XmlCoreDiagnostics.MalformedNamedTag, 64, 0),
				(XmlCoreDiagnostics.IncompleteAttributeValue, 79, 0),
				(XmlCoreDiagnostics.MalformedNamedTag, 123, 0)
			);
		}

		[Test]
		public void Unclosed ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<tag.a>
		<tag.b><tag.b>$
	</tag.a>$
</doc>
",
				delegate {
					parser.AssertStateIs<XmlRootState> ();
					parser.AssertNodeDepth (5);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.b");
				},
				delegate {
					parser.AssertStateIs<XmlRootState> ();
					parser.AssertNodeDepth (2);
					parser.AssertPath ("//doc");
				}
			);
			parser.AssertEmpty ();
			parser.AssertDiagnosticCount (2);
		}



		[Test]
		public void ClosingTagWithWhitespace ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"<doc><a></ a></doc >");
			parser.AssertEmpty ();
			parser.AssertNoDiagnostics ();
		}


		[Test]
		public void BadClosingTag ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"<doc><x><abc></ab c><cd></cd></x></doc>");
			parser.AssertEmpty ();
			parser.AssertDiagnosticCount (2);
		}

		[Test]
		public void MismatchedElementNameWithNamespace ()
		{
			var docTxt = "<X><n:></a><b></X>";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt);
			parser.AssertEmpty ();
			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.ZeroLengthNameWithNamespace, 4, 2),
				(XmlCoreDiagnostics.UnmatchedClosingTag, 7, 4),
				(XmlCoreDiagnostics.UnclosedTag, 11, 3),
				(XmlCoreDiagnostics.UnclosedTag, 3, 4)
			);
		}

		[Test]
		public void MismatchedElementNameWithWhitespaceInName ()
		{
			var docTxt = "<X><n:\na></a><b></X>";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt);
			parser.AssertEmpty ();
			parser.AssertDiagnostics (
				(XmlCoreDiagnostics.ZeroLengthNameWithNamespace, 4, 2),
				(XmlCoreDiagnostics.IncompleteAttribute, 8, 0),
				(XmlCoreDiagnostics.UnmatchedClosingTag, 9, 4),
				(XmlCoreDiagnostics.UnclosedTag, 13, 3),
				(XmlCoreDiagnostics.UnclosedTag, 3, 6)
			);
		}

		[Test]
		public void Misc ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<!DOCTYPE $  >
	<![CDATA[ ]  $ ]  ]]>
	<!--   <foo> <bar arg=""> $  -->
</doc>
",
				delegate {
					parser.AssertStateIs<XmlDocTypeState> ();
					parser.AssertNodeDepth (3);
					parser.AssertPath ("//doc/<!DOCTYPE>");
				},
				delegate {
					parser.AssertStateIs<XmlCDataState> ();
					parser.AssertNodeDepth (3);
					parser.AssertPath ("//doc/<![CDATA[ ]]>");
				},
				delegate {
					parser.AssertStateIs<XmlCommentState> ();
					parser.AssertNodeDepth (3);
					parser.AssertPath ("//doc/<!-- -->");
				}
			);
			parser.AssertEmpty ();
			parser.AssertNoDiagnostics ();
		}

		[Test]
		public void DocTypeCapture ()
		{
			var docText = @"
		<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN""
""DTD/xhtml1-strict.dtd""
[
<!-- foo -->
<!bar #baz>
]>
<doc><foo/></doc>".Replace ("\r\n", "\n");
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docText);
			parser.AssertEmpty ();
			var doc = parser.AssertNodeIs<XDocument> ();
			Assert.IsInstanceOf<XDocType> (doc.FirstChild);
			XDocType dt = doc.FirstChild.AssertCast<XDocType> ();
			Assert.AreEqual ("html", dt.RootElement.FullName);
			Assert.AreEqual ("-//W3C//DTD XHTML 1.0 Strict//EN", dt.PublicFpi);
			Assert.AreEqual ("DTD/xhtml1-strict.dtd", dt.Uri);
			var expectedInternalDecl = @"
<!-- foo -->
<!bar #baz>
".Replace ("\r\n", "\n");
			var actualInternalDecl = docText.Substring (dt.InternalDeclarationRegion.Start, dt.InternalDeclarationRegion.Length);
			Assert.AreEqual (expectedInternalDecl, actualInternalDecl);

			parser.AssertDiagnostics ();
		}

		[Test]
		public void NamespacedAttributes ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"<tag foo:bar='1' foo:bar:baz='2' foo='3' />");
			parser.AssertEmpty ();
			var doc = parser.AssertNodeIs<XDocument> ();
			var el = doc.FirstChild.AssertCast<XElement> ();
			Assert.AreEqual (3, el.Attributes.Count);
			el.Attributes.ElementAt (0).AssertName ("foo", "bar");
			el.Attributes.ElementAt (1).AssertName ("foo", "bar:baz");
			el.Attributes.ElementAt (2).AssertName ("foo");
			var diags = parser.AssertDiagnostics (1);
			Assert.AreEqual (26, diags[0].Span.Start, 26);
		}

		[Test]
		public void SimpleTree ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<a>
		<b>
			<c/>
			<d>
				<e/>
			</d>
			<f>
				<g/>
			</f>
		</b>
	</a>
</doc>");
			parser.AssertNoDiagnostics ();

			var doc = parser.AssertNodeIs<XDocument> ().RootElement
				.AssertNotNull ()
				.AssertName ("doc");
			Assert.True (doc.IsEnded);

			var a = doc.FirstChild.AssertCast<XElement> ().AssertName ("a");
			Assert.True (a.IsEnded);
			Assert.False (a.IsSelfClosing);
			Assert.IsNull (a.NextSibling);

			var b = a.FirstChild.AssertCast<XElement> ().AssertName ("b");
			Assert.True (b.IsEnded);
			Assert.False (b.IsSelfClosing);
			Assert.IsNull (b.NextSibling);

			var c = b.FirstChild.AssertCast<XElement> ().AssertName ("c");
			Assert.AreEqual ("c", c.Name.Name);
			Assert.True (c.IsEnded);
			Assert.True (c.IsSelfClosing);

			var d = c.NextSibling.AssertCast<XElement> ().AssertName ("d");
			Assert.True (d.IsEnded);
			Assert.False (d.IsSelfClosing);

			var e = d.FirstChild.AssertCast<XElement> ().AssertName ("e");
			Assert.True (e.IsEnded);
			Assert.True (e.IsSelfClosing);

			var f = d.NextSibling.AssertCast<XElement> ().AssertName ("f");
			Assert.AreEqual (f, b.LastChild);
			Assert.True (f.IsEnded);
			Assert.False (f.IsSelfClosing);

			var g = f.FirstChild.AssertCast<XElement> ().AssertName ("g");
			Assert.True (g.IsEnded);
			Assert.True (g.IsSelfClosing);
		}

		[Test]
		public void UnnamedElement ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"
<doc>
	<a/>
	<
	<c/>
</doc>");
		}

		[Test]
		public void TextNode ()
		{
			var docTxt = @"
<doc>
	<a>
		<b />
		abc defg
	</a>
</doc>";

			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt, preserveWindowsNewlines: true);
			parser.AssertNoDiagnostics ();

			var el = parser.AssertNodeIs<XDocument> ()
				.RootElement.AssertNotNull()
				.FirstChild.AssertCast<XElement>();
			Assert.AreEqual (2, el.Nodes.Count ());
			var b = el.FirstChild as XElement;
			Assert.NotNull (b);
			var t = el.LastChild as XText;
			Assert.NotNull (t);
			Assert.AreEqual ("abc defg", docTxt.Substring (t.Span.Start, t.Span.Length));
			Assert.AreEqual ("abc defg", t.Text);
		}

		[Test]
		public void Positions ()
		{
			const string docTxt = @"<foo someAtt=""SomeVal"">
<!-- blah -->
  some text
<![CDATA[ dfdfdf ]]>
</foo>
";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt, preserveWindowsNewlines: true);
			parser.AssertEmpty ();
			var doc = parser.AssertNodeIs<XDocument> ()
				.RootElement.AssertNotNull ();

			static void AssertSubstring (string expected, XObject obj) => Assert.AreEqual (expected, docTxt.Substring (obj.Span.Start, obj.Span.Length));

			AssertSubstring (@"<foo someAtt=""SomeVal"">", doc);
			AssertSubstring (@"someAtt=""SomeVal""", doc.Attributes.First.AssertNotNull ());
			AssertSubstring (@"<!-- blah -->", doc.Nodes.OfType<XComment>().First ());
			AssertSubstring (@"<![CDATA[ dfdfdf ]]>", doc.Nodes.OfType<XCData>().First ());
			AssertSubstring (@"</foo>", doc.ClosingTag.AssertNotNull ());
		}

		[Test]
		public void ProcessingInstruction()
		{
			var docTxt = @"<?x?>";

			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt, preserveWindowsNewlines: true);
			parser.AssertEmpty ();
			var doc = parser.AssertNodeIs<XDocument>();
			var processingInstruction = doc.FirstChild.AssertNotNull ();

			Assert.AreEqual (0, processingInstruction.Span.Start);
			Assert.AreEqual (5, processingInstruction.Span.Length);
		}

		[Test]
		public void NameStartsWithWhitespaceAndColon ()
		{
			var docTxt = "< :";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt);
		}

		[Test]
		public void MismatchedElementNameWithWhitespaceInName2 ()
		{
			var docTxt = "<X><n\n:a></a><b></X>";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt);
			parser.AssertEmpty ();
			parser.AssertDiagnosticCount (4);
		}

		[Test]
		public void InvalidNameState ()
		{
			var docTxt = "<a:<x";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt);
			parser.AssertDiagnosticCount (2);
		}

		[Test]
		public void TwoOpenAngles ()
		{
			var docTxt = "<<";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt);
			var diagnostic = parser.AssertDiagnostics (1)[0];
			Assert.AreEqual (1, diagnostic.Span.Start);
			Assert.AreEqual (1, diagnostic.Span.Length);
		}

		// This walks over the XHTML 1.0 Strict Schema and verifies at every character location
		// that a recovered spine parser is equivalent to one created from scratch.
		// It also calculates the recovery rate and verifies that it does not regress from the
		// last recorded value.
		[Test]
		public void SpineParserRecovery ()
		{
			using var sr = new StreamReader (ResourceManager.GetXhtmlStrictSchema ());
			var docTxt = sr.ReadToEnd ();

			var rootState = CreateRootState ();

			var treeParser = new XmlTreeParser (rootState);
			foreach (char c in docTxt) {
				treeParser.Push (c);
			}
			(var doc, var diag) = treeParser.FinalizeDocument ();

			Assert.AreEqual (0, diag.Count);

			var spineParser = new XmlSpineParser (rootState);

			int totalNotRecovered = 0;

			for (int i = 0; i < docTxt.Length; i++) {
				char c = docTxt[i];
				spineParser.Push (c);

				var recoveredParser = XmlSpineParser.FromDocumentPosition (rootState, doc, i).AssertNotNull ();
				var delta = i - recoveredParser.Position;
				totalNotRecovered += delta;

				var end = Math.Min (i + 1, docTxt.Length);
				for (int j = recoveredParser.Position; j < end; j++) {
					recoveredParser.Push (docTxt[j]);
				}

				AssertEqual (spineParser.GetContext (), recoveredParser.GetContext ());
			}

			int total = docTxt.Length * docTxt.Length / 2;
			float recoveryRate = 1f - totalNotRecovered / (float)total;
			TestContext.WriteLine ($"Recovered {(recoveryRate * 100f):F2}%");

			// check it never regresses
			Assert.LessOrEqual (totalNotRecovered, 1118088);
		}

		void AssertEqual (XmlParserContext a, XmlParserContext b)
		{
			Assert.AreEqual (a.Position, b.Position);

			Assert.AreEqual (a.CurrentState, b.CurrentState);

			// NOTE: CurrentStateLength is not recovered for the root or tag states
			// as it's not needed to resume those states
			if (a.CurrentState is not XmlRootState && a.CurrentState is not XmlTagState) {
				Assert.AreEqual (a.CurrentStateLength, b.CurrentStateLength);
			}
			Assert.AreEqual (a.StateTag, b.StateTag);

			// NOTE: PreviousState is not reliably recovered _but_ the parser only recovers at positions where that doesn't matter

			AssertEqual (a.KeywordBuilder, b.KeywordBuilder);

			Assert.AreEqual (a.Nodes.Count, b.Nodes.Count);
			Assert.AreEqual (a.Nodes.Peek().GetType(), b.Nodes.Peek().GetType());
		}

		// avoid allocating strings unless they're not equal
		// makes the test marginally faster ( ~5%?)
		void AssertEqual (StringBuilder a, StringBuilder b)
		{
			if (a.Length != b.Length) {
				Assert.AreEqual (a.ToString (), b.ToString ());
			}
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i]) {
					Assert.AreEqual (a.ToString (), b.ToString ());
				}
			}
		}
	}
}
