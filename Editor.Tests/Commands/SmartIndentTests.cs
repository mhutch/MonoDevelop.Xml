// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.Xml.Editor.Parsing;
using MonoDevelop.Xml.Editor.SmartIndent;
using MonoDevelop.Xml.Tests;

using NUnit.Framework;

namespace MonoDevelop.Xml.Editor.Tests.Commands
{
	class SmartIndentTests : XmlEditorTest
	{
		[Test]
		[TestCase ("<a>|", 4)] // single indent inside one element
		[TestCase ("<a b=''|", 8)] // double indent in element attributes
		[TestCase ("<a>\n    <b>|", 8)] // double indent when 2 elements deep
		[TestCase ("<a>\n<b>|", 4)] // respect user correction on previous line
		[TestCase ("<a>\n  <b>|", 6)] // respect user correction on previous line
		[TestCase ("<a>\n    <b>|</b>\n</a>", 4)] // tag close on current line deindents
		[TestCase ("<a>\n    <b><c>|</c></b>\n</a>", 4)] // double tag close
		[TestCase ("<a>w|</a>", 0)] // no indent for closing tag
		[TestCase ("<a>w| </a>", 0)] // no indent for closing tag preceded by whitespace
		[TestCase ("<a>|w</a>", 4)] // indent when content is present
		public async Task TestSmartIndent (string doc, int expectedIndent)
		{
			var caretPos = doc.IndexOf ('|');
			if (caretPos > -1) {
				doc = doc.Replace ("|", "\n");
				caretPos++;
			} else {
				caretPos = doc.Length;
				doc += "\n";
			}

			await Catalog.JoinableTaskContext.Factory.SwitchToMainThreadAsync ();
			var textView = CreateTextView (doc);

			var line = textView.TextBuffer.CurrentSnapshot.GetLineFromPosition (caretPos);
			GetParser (textView.TextBuffer);

			textView.Options.SetOptionValue (DefaultOptions.ConvertTabsToSpacesOptionId, true);
			textView.Options.SetOptionValue (DefaultOptions.IndentSizeOptionId, 4);
			textView.Options.SetOptionValue (DefaultOptions.TabSizeOptionId, 4);

			var smartIndent = new XmlSmartIndent (textView, Catalog.GetService<XmlParserProvider> (), TestLoggerFactory.CreateTestMethodLogger ());
			var indent = smartIndent.GetDesiredIndentation (line);
			Assert.AreEqual (expectedIndent, indent);
		}
	}
}