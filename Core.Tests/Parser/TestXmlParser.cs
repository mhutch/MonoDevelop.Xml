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

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	public static class TestXmlParser
	{
		public static (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) Parse (string doc, params Action<XmlParser>[] asserts)
		{
			var p = new XmlTreeParser (new XmlRootState ());
			return p.Parse (doc, Array.ConvertAll (asserts, a => (Action)(() => a (p))));
		}

		public static (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) Parse (string txt, params Action<XNode?>[] asserts)
		{
			var p = new XmlTreeParser (new XmlRootState ());
			var context = p.GetContext ();

			//parse and capture span info
			var list = new List<int> ();
			var result = p.Parse (txt, Array.ConvertAll (asserts, a => (Action)(() => list.Add (context.Position))));


			for (int i = 0; i < asserts.Length; i++) {
				asserts[i] (result.doc.AllDescendentNodes.FirstOrDefault (n => n.Span.Contains (list[i])));
			}

			return result;
		}

		public static (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) Parse (this XmlParser parser, string doc, params Action[] asserts) => parser.Parse (doc, '$', false, asserts);

		public static (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) Parse (this XmlParser parser, string doc, char trigger, params Action[] asserts) => parser.Parse (doc, trigger, false, asserts);

		public static (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) Parse (this XmlParser parser, string doc, char trigger = '$', bool preserveWindowsNewlines = false, params Action[] asserts)
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

			return parser.EndAllNodes ();
		}

		public static T AssertCast<T> (this object? o) where T : class
		{
			Assert.IsInstanceOf<T> (o);
			return (T)o!;
		}

		public static T AssertNotNull<T> (this T? o) where T : notnull
		{
			Assert.NotNull (o);
			return (T)o;
		}

		public static T AssertName<T> (this T o, string? expectedName) where T : XObject, INamedXObject => AssertName (o, null, expectedName);

		public static T AssertName<T> (this T o, string? expectedPrefix, string? expectedName) where T : XObject, INamedXObject
		{
			Assert.AreEqual (expectedPrefix, o.Name.Prefix);
			Assert.AreEqual (expectedName, o.Name.Name);
			return o;
		}
		public static T AssertComplete<T> (this T o) where T : XObject
		{
			Assert.IsTrue (o.IsComplete);
			return o;
		}

		public static T AssertIncomplete<T> (this T o) where T : XObject
		{
			Assert.IsFalse (o.IsComplete);
			return o;
		}

		public static string GetPathString (this XmlParser parser) => parser.GetPath ().ToPathString ();

		public static string ToPathString (this List<XObject> nodePath)
		{
			Assert.Greater (nodePath.Count, 0, "Node path is empty");


			if (nodePath[0] is not XDocument) {
				Assert.Fail ("First node in path must be a document");
			}

			var sb = new System.Text.StringBuilder ("/");

			for (int i = 1; i< nodePath.Count; i++) {
				sb.Append ('/');
				sb.Append (nodePath[i].FriendlyPathRepresentation);
			}

			return sb.ToString ();
		}
		
		public static void AssertPath (this XmlParser parser, string path)
		{
			Assert.AreEqual (path, parser.GetPathString ());
		}
		
		public static Action PathAssertion (this XmlParser parser, string path)
		{
			return delegate {
				Assert.AreEqual (path, parser.GetPathString ());
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

		static void AssertDepthAtLeast (this NodeStack nodes, int depth)
		{
			Assert.GreaterOrEqual (nodes.Count, depth, "Node depth was {0}, expected at least {1}", nodes.Count, depth);
		}

		public static XObject AssertPeek (this XmlParser parser, int down = 0)
		{
			var nodes = parser.GetContext ().Nodes;
			nodes.AssertDepthAtLeast (down);
			return nodes.Peek (down);
		}

		public static T AssertNodeIs<T> (this XmlParser parser, int down = 0) where T : class
			=> parser.AssertPeek(down).AssertCast<T> ();

		public static void AssertNoDiagnostics (this (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) parseResult, Func<XmlDiagnostic, bool>? filter = null) => AssertDiagnosticCount (parseResult.diagnostics, 0, filter);

		public static void AssertDiagnosticCount (this (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) parseResult, int count, Func<XmlDiagnostic, bool>? filter = null)
			=> AssertDiagnosticCount (parseResult.diagnostics, count, filter);
		public static void AssertDiagnosticCount (this XmlParser parser, int count, Func<XmlDiagnostic, bool>? filter = null)
			=> AssertDiagnosticCount (parser.GetContext ().Diagnostics, count, filter);

		public static void AssertDiagnosticCount (this IReadOnlyList<XmlDiagnostic>? diagnostics, int count, Func<XmlDiagnostic, bool>? filter = null)
		{
			if (diagnostics is null) {
				#pragma warning disable NUnit2007 // The actual value should not be a constant
				Assert.AreEqual (count, 0);
				#pragma warning restore NUnit2007
				return;
			}

			int actualCount = filter is not null? diagnostics.Count(filter) : diagnostics.Count;

			if (actualCount != count) {
				var sb = new System.Text.StringBuilder ();
				sb.AppendLine ($"Expected {count} diagnostics, got {actualCount}:");
				foreach (var err in filter is null? diagnostics : diagnostics.Where (filter)) {
					sb.AppendLine ($"{err.Descriptor.Severity}@{err.Span}: {err.GetFormattedMessageWithTitle ()}");
				}
				Assert.AreEqual (count, actualCount, sb.ToString ());
			}
		}

		public static IReadOnlyList<XmlDiagnostic> AssertDiagnostics (this (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) parseResult, int count, Func<XmlDiagnostic, bool>? filter = null)
		{
			var diagnostics = parseResult.diagnostics ?? new List<XmlDiagnostic> ();

			if (filter is not null) {
				diagnostics = diagnostics.Where (filter).ToList ();
			}

			AssertDiagnosticCount (diagnostics, count);

			return diagnostics;
		}

		public static void AssertDiagnostics (this (XDocument doc, IReadOnlyList<XmlDiagnostic>? diagnostics) parseResult, params (XmlDiagnosticDescriptor desc, int start, int length)[] expectedDiagnostics)
			=> AssertDiagnostics (parseResult.diagnostics, expectedDiagnostics);

		public static void AssertDiagnostics (this XmlParser parser, params (XmlDiagnosticDescriptor desc, int start, int length)[] expectedDiagnostics)
			=> AssertDiagnostics (parser.GetContext ().Diagnostics, expectedDiagnostics);

		public static void AssertDiagnostics (this IReadOnlyList<XmlDiagnostic>? diagnostics, params (XmlDiagnosticDescriptor desc, int start, int length)[] expectedDiagnostics)
		{
			diagnostics ??= new List<XmlDiagnostic> ();

			var expectedSet = new HashSet<(XmlDiagnosticDescriptor desc, TextSpan span)> (expectedDiagnostics.Select (d => (d.desc, new TextSpan (d.start, d.length))));
			var actualSet = new HashSet<(XmlDiagnosticDescriptor desc, TextSpan span)> (diagnostics.Select (d => (d.Descriptor, d.Span)));

			var missing = expectedSet.Except (actualSet);
			var unexpected = actualSet.Except (expectedSet);

			bool hasMissing = missing.Any ();
			bool hasUnexpected = unexpected.Any ();

			if (hasMissing || hasUnexpected) {
				var sb = new System.Text.StringBuilder ();
				if (hasMissing) {
					sb.AppendLine ($"Missing expected diagnostics:");
					foreach (var err in missing) {
						sb.AppendLine ($"    {err.desc.Id}@{err.span.Start}+{err.span.Length}");
					}
				}
				if (hasUnexpected) {
					sb.AppendLine ($"Unexpected diagnostics:");
					foreach (var err in unexpected) {
						sb.AppendLine ($"    {err.desc.Id}@{err.span.Start}+{err.span.Length}");
					}
				}
				Assert.Fail (sb.ToString ());
			}
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

		public static void AssertNodeName (this XmlParser parser, string name) => parser.AssertNodeName (null, name);

		public static void AssertNodeName (this XmlParser parser, string? prefix, string name)
		{
			var actual = parser.GetNodeName ();
			Assert.AreEqual (actual.Prefix, prefix);
			Assert.AreEqual (actual.Name, name);
		}

		static XName GetNodeName (this XmlParser parser)
		{
			var namedObject = parser.AssertNodeIs<INamedXObject> ();
			if (namedObject.IsNamed)
				return namedObject.Name;

			var context = parser.GetContext ();
			parser.AssertStateIs<XmlNameState> ();
			return context.KeywordBuilder.ToString ();
		}

		public static void AssertPath (this XNode? node, params QualifiedName[] qualifiedNames)
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
