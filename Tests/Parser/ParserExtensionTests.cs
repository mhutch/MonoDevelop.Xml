// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Tests.Completion;
using MonoDevelop.Xml.Tests.EditorTestHelpers;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Parser
{
	[TestFixture]
	public class ParserExtensionTests : EditorTestBase
	{
		protected override string ContentTypeName => XmlContentTypeNames.XmlCore;
		protected override (EditorEnvironment, EditorCatalog) InitializeEnvironment () => TestEnvironment.EnsureInitialized ();

		[Test]
		[TestCase ("<a><b>", "")]
		[TestCase ("<a><b>foo", "foo")]
		[TestCase ("<a><b>    foo", "foo")]
		[TestCase ("<a><b bar='", "")]
		[TestCase ("<a><b bar='xyz", "xyz")]
		[TestCase ("<a><b bar='  xyz", "  xyz")]
		public void TestGetIncompleteValue (string doc, string expected)
		{
			var buffer = Catalog.BufferFactoryService.CreateTextBuffer (doc, ContentType);
			var snapshot = buffer.CurrentSnapshot;
			var caretPoint = new SnapshotPoint (snapshot, snapshot.Length);

			var spine = XmlBackgroundParser.GetParser (buffer).GetSpineParser (caretPoint);
			var actual = spine.GetIncompleteValue (snapshot);

			Assert.AreEqual (expected, actual);
		}
	}
}
