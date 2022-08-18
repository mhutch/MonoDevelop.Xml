// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.Tests;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class ParserExtensionTests : XmlEditorTest
	{
		[Test]
		[TestCase ("<a><b>", "")]
		[TestCase ("<a><b>foo", "foo")]
		[TestCase ("<a><b>    foo", "foo")]
		[TestCase ("<a><b bar='", "")]
		[TestCase ("<a><b bar='xyz", "xyz")]
		[TestCase ("<a><b bar='  xyz", "  xyz")]
		public void TestGetIncompleteValue (string doc, string expected)
		{
			var buffer = CreateTextBuffer (doc);
			var snapshot = buffer.CurrentSnapshot;
			var caretPoint = new SnapshotPoint (snapshot, snapshot.Length);

			var spine = GetParser (buffer).GetSpineParser (caretPoint);
			var actual = spine.GetIncompleteValue (snapshot);

			Assert.AreEqual (expected, actual);
		}

		[Test]
		public async Task TestIncremental ()
		{
			var buffer = CreateTextBuffer (" ");
			var parser = GetParser (buffer);
			await parser.GetOrProcessAsync (buffer.CurrentSnapshot, default);

			buffer.Insert (0, " ");
			var snapshot = buffer.CurrentSnapshot;
			var caretPoint = new SnapshotPoint (snapshot, 1);

			var spine = parser.GetSpineParser (caretPoint);
		}
	}
}
