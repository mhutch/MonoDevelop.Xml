// 
// TestParser.cs
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
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	public static class TestXmlParser
	{
		public static void Parse (string doc, params Action<XmlParser>[] asserts)
		{
			var p = new XmlTreeParser (new XmlRootState ());
			p.Parse (doc, Array.ConvertAll (asserts, a => (Action)(() => a (p))));
		}

		public static void Parse (string txt, params Action<XNode>[] asserts)
		{
			var p = new XmlTreeParser (new XmlRootState ());
			var context = p.GetContext ();

			//parse and capture span info
			var list = new List<int> ();
			p.Parse (txt, Array.ConvertAll (asserts, a => (Action)(() => list.Add (context.Position))));

			var doc = (XDocument)context.Nodes.Last ();

			for (int i = 0; i < asserts.Length; i++) {
				asserts[i] (doc.AllDescendentNodes.FirstOrDefault (n => n.Span.Contains (list[i])));
			}
		}

		public static void Parse (this XmlParser parser, string doc, params Action[] asserts) => parser.Parse (doc, '$', false, asserts);

		public static void Parse (this XmlParser parser, string doc, char trigger, params Action[] asserts) => parser.Parse (doc, trigger, false, asserts);

		public static void Parse (this XmlParser parser, string doc, char trigger = '$', bool preserveWindowsNewlines = false, params Action[] asserts)
		{
			var context = parser.GetContext ();
			Assert.AreEqual (0, context.Position);
			int assertNo = 0;
			for (int i = 0; i < doc.Length; i++) {
				char c = doc[i];
				if (c == '\r' && !preserveWindowsNewlines) {
					continue;
				}
				if (c == trigger) {
					if (i + 1 < doc.Length && doc[i + 1] == trigger) {
						parser.Push (c);
						i++;
						continue;
					}
					asserts[assertNo] ();
					assertNo++;
				} else {
					parser.Push (c);
				}
			}
			Assert.AreEqual (asserts.Length, assertNo);

			var diagnostics = context.Diagnostics;
			if (diagnostics != null) {
				foreach (var diagnostic in diagnostics) {
					Assert.GreaterOrEqual (diagnostic.Span.Start, 0);
					Assert.GreaterOrEqual (diagnostic.Span.Length, 0);
				}
			}
		}

		public static XObject PeekSpine (this XmlParser parser) => parser.GetContext ().Nodes.Peek ();

		public static XObject PeekSpine (this XmlParser parser, int down) => parser.GetContext ().Nodes.Peek (down);

		public static string GetPath (this XmlParser parser)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			foreach (XObject obj in parser.GetContext().Nodes) {
				if (obj is XDocument) {
					sb.Insert (0, '/');
					break;
				}
				sb.Insert (0, obj.FriendlyPathRepresentation);
				sb.Insert (0, '/');
			}
			return sb.ToString ();
		}
		
		public static void AssertPath (this XmlParser parser, string path)
		{
			Assert.AreEqual (path, parser.GetPath ());
		}
		
		public static Action PathAssertion (this XmlParser parser, string path)
		{
			return delegate {
				Assert.AreEqual (path, parser.GetPath ());
			};
		}
		
		public static void AssertStateIs<T> (this XmlParser parser) where T : XmlParserState
		{
			var context = parser.GetContext ();
			Assert.IsTrue (context.CurrentState is T, "Current state is {0} not {1}", context.CurrentState.GetType ().Name, typeof (T).Name);
		}

		public static void AssertStateIsNot<T> (this XmlParser parser) where T : XmlParserState
		{
			var context = parser.GetContext ();
			Assert.IsFalse (context.CurrentState is T, "Current state is {0}", typeof (T).Name);
		}
		
		public static void AssertNodeDepth (this XmlParser parser, int depth)
		{
			var nodes = parser.GetContext ().Nodes;
			Assert.AreEqual (depth, nodes.Count, "Node depth is {0} not {1}", nodes.Count, depth);
		}
		
		public static void AssertNodeIs<T> (this XmlParser parser, int down)
		{
			XObject n = parser.GetContext ().Nodes.Peek (down);
			AssertNodeDepth (parser, down);
			Assert.IsTrue (n is T, "Node down {0} is {1}, not {2}", down, n.GetType ().Name, typeof (T).Name);
		}
		
		public static void AssertNodeIs<T> (this XmlParser parser)
		{
			var context = parser.GetContext ();
			XObject n = context.Nodes.Peek ();
			Assert.IsTrue (n is T, "Node is {0}, not {1}", n.GetType ().Name, typeof (T).Name);
		}
		
		public static void AssertErrorCount (this XmlParser parser, int count)
		{
			AssertErrorCount (parser, count, x => true);
		}
		
		public static void AssertErrorCount (this XmlParser parser, int count, Func<XmlDiagnosticInfo,bool> filter)
		{
			string msg = null;
			var diagnostics = parser.GetContext ().Diagnostics;
			var actualCount = diagnostics.Count (filter);
			if (actualCount != count) {
				var sb = new System.Text.StringBuilder ();
				foreach (var err in diagnostics)
					if (filter (err))
						sb.AppendFormat ("{0}@{1}: {2}\n", err.Severity, err.Span, err.Message);
				msg = sb.ToString ();
			}
			Assert.AreEqual (count, actualCount, msg);
		}

		public static void AssertAttributes (this XmlParser parser, params string[] nameValuePairs)
		{
			parser.AssertNodeIs<IAttributedXObject> ();
			IAttributedXObject obj = (IAttributedXObject) parser.GetContext ().Nodes.Peek ();
			parser.AssertAttributes (obj, nameValuePairs);
		}

		public static void AssertAttributes (this XmlParser parser, IAttributedXObject obj, params string[] nameValuePairs)
		{
			if ((nameValuePairs.Length % 2) != 0)
				throw new ArgumentException ("nameValuePairs");

			int i = 0;
			foreach (XAttribute att in obj.Attributes) {
				Assert.IsTrue (i < nameValuePairs.Length);
				Assert.AreEqual (nameValuePairs[i], att.Name.FullName);
				Assert.AreEqual (nameValuePairs[i + 1], att.Value);
				i += 2;
			}
			Assert.AreEqual (nameValuePairs.Length, i);
		}
		
		public static void AssertNoErrors (this XmlParser parser)
		{
			parser.AssertErrorCount (0);
		}
		
		public static void AssertEmpty (this XmlParser parser)
		{
			parser.AssertNodeDepth (1);
			parser.AssertNodeIs<XDocument> (); 
		}
		
		public static void AssertName (this XmlParser parser, string name)
		{
			parser.AssertName (0, name);
		}
		
		public static void AssertName (this XmlParser parser, int down, string name)
		{
			XObject node = parser.GetContext ().Nodes.Peek (down);
			Assert.IsTrue (node is INamedXObject);
			Assert.AreEqual (name, ((INamedXObject)node).Name.FullName);
		}

		public static void AssertPath (this XNode node, params QualifiedName[] qualifiedNames)
		{
			var path = new List<XNode> ();
			while (node != null) {
				path.Add (node);
				node = node.Parent as XNode;
			}
			path.Reverse ();

			Assert.AreEqual (
				new XmlElementPath (qualifiedNames),
				XmlElementPath.Resolve (path.ToArray ())
			);
		}
	}
}
