// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Tests;
using MonoDevelop.Xml.Parser;

using NUnit.Framework;

using TextWithMarkers = MonoDevelop.Xml.Tests.Utils.TextWithMarkers;

namespace MonoDevelop.Xml.Tests.Parser;

[TestFixture]
public class SpineParserTests : XmlEditorTest
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
	[TestCase ("", "<$>")]
	[TestCase ("<", "$>")]
	[TestCase ("<^p^>", "")] // deletion
	[TestCase ("<p><q><$>", "r", "//p/q/r")]
	public async Task IncrementalSpineParse (string documentWithMarkers, string insertionTextWithMarkers, string expectedPath = null)
	{
		var initialDocument = TextWithMarkers.Parse (documentWithMarkers, '$', '^');
		var insertion = TextWithMarkers.Parse (insertionTextWithMarkers, '$');

		// caret is optionally marked by $ and default to the insertion position, if there is one, else the end of the doc
		var initialCaretPos = initialDocument.GetMarkedPositions ('$') switch {
			{ Count: 0 } _ => initialDocument.GetMarkedPositions ('^') switch {
					{ Count: 0 } _ => initialDocument.Text.Length,
					{ } q => q[0],
				},
			{ Count: 1 } p => p[0],
			_ => throw new InvalidOperationException ()
		};

		// use one ^ to mark insertion point, or two ^ to mark replacement point, or default to inserting before caret
		var replacementSpan = initialDocument.GetMarkedPositions ('^') switch {
			{ Count: 0 } _ => new Span (initialCaretPos, 0),
			{ Count: 1 } p => new Span (p[0], 0),
			{ Count: 2 } p => new Span (p[0], p[1] - p[0]),
			_ => throw new InvalidOperationException ()
		};

		// caret after replacement may be marked by $, or be moved by the insertion
		var caretPosAfterInsertion = insertion.GetMarkedPositions ('$') switch {
			{ Count: 0 } _ => initialCaretPos < replacementSpan.Start ? initialCaretPos : initialCaretPos + insertion.Text.Length,
			{ Count: 1 } p => replacementSpan.Start + p[0],
			_ => throw new InvalidOperationException ()
		};

		var buffer = CreateTextBuffer (initialDocument.Text);
		var parser = GetParser (buffer);

		var prevSnapshot = buffer.CurrentSnapshot;
		var prevParse = await parser.GetOrProcessAsync (prevSnapshot, default);

		buffer.Replace (replacementSpan, insertion.Text);

		var currentSnapshot = buffer.CurrentSnapshot;
		var caretPoint = new SnapshotPoint (currentSnapshot, caretPosAfterInsertion);

		// use GetSpineParser with explicit prevParse to avoid race with background parse

		// new contents cancelled the initial parse but the new parse was not completed
		var spine = parser.GetSpineParser (null, caretPoint);
		var path = GetPathString ();

		// if expectedPath was provided, assert it matches, else set it to the actual path so later asserts check whether it's the same
		if (expectedPath is null) {
			expectedPath = path;
		} else {
			Assert.AreEqual (expectedPath, path);
		}

		// initial parse was complete
		spine = parser.GetSpineParser (prevParse, caretPoint);
		path = GetPathString ();
		Assert.AreEqual (expectedPath, path);

		// new parse was completed
		var currentParse = await parser.GetOrProcessAsync (prevSnapshot, default);
		spine = parser.GetSpineParser (currentParse, caretPoint);
		path = GetPathString ();
		Assert.AreEqual (expectedPath, path);

		string GetPathString ()
		{
			if (spine.TryGetNodePath (currentSnapshot, out var nodePath)) {
				return nodePath.ToPathString ();
			}
			return spine.GetPathString ();
		}
	}
}
