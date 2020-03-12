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

using System.Linq;
using NUnit.Framework;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using System.Xml;

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
			parser.AssertErrorCount (0);
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
			parser.AssertErrorCount (0);
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
					parser.AssertErrorCount (3);
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (4);
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
					Assert.IsFalse (parser.PeekSpine (1).IsComplete);
					parser.AssertNodeDepth (6);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.c/tag.d");
				},
				delegate {
					parser.AssertStateIs<XmlAttributeValueState> ();
					parser.AssertNodeDepth (9);
					Assert.IsTrue (parser.PeekSpine (3) is XElement eld && eld.Name.Name == "tag.d" && eld.IsComplete);
					Assert.IsTrue (parser.PeekSpine (2) is XElement ele && ele.Name.Name == "tag.e" && !ele.IsComplete);
					Assert.IsTrue (parser.PeekSpine (1) is XElement elf && elf.Name.Name == "tag.f" && !elf.IsComplete);
					Assert.IsTrue (parser.PeekSpine () is XAttribute att && !att.IsComplete);
					parser.AssertPath ("//doc/tag.a/tag.b/tag.c/tag.d/tag.e/tag.f/@id");
				}
			);
			parser.AssertEmpty ();
			parser.AssertErrorCount (5, x => x.Severity == DiagnosticSeverity.Error);
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
			parser.AssertErrorCount (2);
		}



		[Test]
		public void ClosingTagWithWhitespace ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"<doc><a></ a></doc >");
			parser.AssertEmpty ();
			parser.AssertErrorCount (0);
		}


		[Test]
		public void BadClosingTag ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"<doc><x><abc></ab c><cd></cd></x></doc>");
			parser.AssertEmpty ();
			parser.AssertErrorCount (2);
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
			parser.AssertErrorCount (0);
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
			XDocument doc = (XDocument)parser.PeekSpine ();
			Assert.IsTrue (doc.FirstChild is XDocType);
			XDocType dt = (XDocType)doc.FirstChild;
			Assert.AreEqual ("html", dt.RootElement.FullName);
			Assert.AreEqual ("-//W3C//DTD XHTML 1.0 Strict//EN", dt.PublicFpi);
			Assert.AreEqual ("DTD/xhtml1-strict.dtd", dt.Uri);
			var expectedInternalDecl = @"
<!-- foo -->
<!bar #baz>
".Replace ("\r\n", "\n");
			var actualInternalDecl = docText.Substring (dt.InternalDeclarationRegion.Start, dt.InternalDeclarationRegion.Length);
			Assert.AreEqual (expectedInternalDecl, actualInternalDecl);
			parser.AssertNoErrors ();
		}

		[Test]
		public void NamespacedAttributes ()
		{
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (@"<tag foo:bar='1' foo:bar:baz='2' foo='3' />");
			parser.AssertEmpty ();
			var doc = (XDocument)parser.PeekSpine ();
			var el = (XElement)doc.FirstChild;
			Assert.AreEqual (3, el.Attributes.Count ());
			Assert.AreEqual ("foo", el.Attributes.ElementAt (0).Name.Prefix);
			Assert.AreEqual ("bar", el.Attributes.ElementAt (0).Name.Name);
			Assert.AreEqual ("foo", el.Attributes.ElementAt (1).Name.Prefix);
			Assert.AreEqual ("bar:baz", el.Attributes.ElementAt (1).Name.Name);
			Assert.IsNull (el.Attributes.ElementAt (2).Name.Prefix);
			Assert.AreEqual ("foo", el.Attributes.ElementAt (2).Name.Name);
			Assert.AreEqual (3, el.Attributes.Count ());
			parser.AssertErrorCount (1);
			Assert.AreEqual (26, parser.GetContext().Diagnostics[0].Span.Start, 26);
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
			parser.AssertErrorCount (0);

			var doc = ((XDocument)parser.PeekSpine ()).RootElement;
			Assert.NotNull (doc);
			Assert.AreEqual ("doc", doc.Name.Name);
			Assert.True (doc.IsEnded);

			var a = (XElement)doc.FirstChild;
			Assert.NotNull (a);
			Assert.AreEqual ("a", a.Name.Name);
			Assert.True (a.IsEnded);
			Assert.False (a.IsSelfClosing);
			Assert.IsNull (a.NextSibling);

			var b = (XElement)a.FirstChild;
			Assert.NotNull (b);
			Assert.AreEqual ("b", b.Name.Name);
			Assert.True (b.IsEnded);
			Assert.False (b.IsSelfClosing);
			Assert.IsNull (b.NextSibling);

			var c = (XElement)b.FirstChild;
			Assert.NotNull (c);
			Assert.AreEqual ("c", c.Name.Name);
			Assert.True (c.IsEnded);
			Assert.True (c.IsSelfClosing);
			Assert.IsNull (c.FirstChild);

			var d = (XElement)c.NextSibling;
			Assert.True (d.IsEnded);
			Assert.False (d.IsSelfClosing);
			Assert.AreEqual ("d", d.Name.Name);

			var e = (XElement)d.FirstChild;
			Assert.NotNull (e);
			Assert.True (e.IsEnded);
			Assert.True (e.IsSelfClosing);
			Assert.AreEqual ("e", e.Name.Name);

			var f = (XElement)d.NextSibling;
			Assert.AreEqual (f, b.LastChild);
			Assert.True (f.IsEnded);
			Assert.False (f.IsSelfClosing);
			Assert.AreEqual ("f", f.Name.Name);

			var g = (XElement)f.FirstChild;
			Assert.NotNull (g);
			Assert.True (g.IsEnded);
			Assert.True (g.IsSelfClosing);
			Assert.AreEqual ("g", g.Name.Name);
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
			parser.AssertErrorCount (0);

			var el = (((XDocument)parser.PeekSpine ()).RootElement)?.FirstChild as XElement;
			Assert.NotNull (el);
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
			var docTxt = @"<foo someAtt=""SomeVal"">
<!-- blah -->
  some text
<![CDATA[ dfdfdf ]]>
</foo>
";
			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt, preserveWindowsNewlines: true);
			parser.AssertEmpty ();
			var doc = (XDocument)parser.PeekSpine ();

			string Substring (XObject obj) => docTxt.Substring (obj.Span.Start, obj.Span.Length);

			Assert.AreEqual (@"<foo someAtt=""SomeVal"">", Substring (doc.RootElement));
			Assert.AreEqual (@"someAtt=""SomeVal""", Substring (doc.RootElement.Attributes.First));
			Assert.AreEqual (@"<!-- blah -->", Substring (doc.RootElement.Nodes.OfType<XComment> ().First ()));
			Assert.AreEqual (@"<![CDATA[ dfdfdf ]]>", Substring (doc.RootElement.Nodes.OfType<XCData> ().First ()));
			Assert.AreEqual (@"</foo>", Substring (doc.RootElement.ClosingTag));
		}

		[Test]
		public void ProcessingInstruction()
		{
			var docTxt = @"<?x?>";

			var parser = new XmlTreeParser (CreateRootState ());
			parser.Parse (docTxt, preserveWindowsNewlines: true);
			parser.AssertEmpty ();
			var doc = (XDocument)parser.PeekSpine ();
			var processingInstruction = doc.FirstChild;

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
			parser.AssertErrorCount (4);
		}
	}
}
