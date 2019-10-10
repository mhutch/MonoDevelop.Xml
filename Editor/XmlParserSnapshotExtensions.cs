// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor
{
	public static class XmlParserSnapshotExtensions
	{
		const int DEFAULT_READAHEAD_LIMIT = 5000;

		/// <summary>
		/// Gets the XML name at the parser's position.
		/// </summary>
		/// <param name="spine">A spine parser. It will not be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static XName GetCompleteName (this XmlSpineParser parser, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
			=> parser.GetCompleteName (new SnapshotTextSource (snapshot), maximumReadahead);

		public static string GetAttributeOrElementValueToCaret (this XmlSpineParser spineAtCaret, SnapshotPoint caretPosition)
		{
			int currentPosition = caretPosition.Position;
			int lineStart = caretPosition.Snapshot.GetLineFromPosition (currentPosition).Start.Position;
			int expressionStart = currentPosition - spineAtCaret.CurrentStateLength;
			if (spineAtCaret.GetAttributeValueDelimiter ().HasValue) {
				expressionStart += 1;
			}
			int start = Math.Max (expressionStart, lineStart);
			var expression = caretPosition.Snapshot.GetText (start, currentPosition - start);
			return expression;
		}

		/// <summary>
		/// Advances the parser until the specified object is closed i.e. has a closing tag.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="ob">The object to complete</param>
		/// <param name="snapshot"></param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilClosed (this XmlSpineParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
			=> parser.AdvanceUntilClosed (ob, new SnapshotTextSource (snapshot), maximumReadahead);

		/// <summary>
		/// Advances the parser until the specified object is ended.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="ob">The object to complete</param>
		/// <param name="snapshot"></param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilEnded (this XmlSpineParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
			=> parser.AdvanceUntilEnded (ob, new SnapshotTextSource (snapshot), maximumReadahead);


		/// <summary>
		/// Gets the node path at the parser position without changing the parser state, ensuring that the deepest node has a complete name.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will not be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		public static List<XObject> GetNodePath (this XmlSpineParser parser, ITextSnapshot snapshot)
			=> parser.GetNodePath (new SnapshotTextSource (snapshot));


		/// <summary>
		/// Advances the parser to end the node at the current position and gets that node's path.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static List<XObject> AdvanceToNodeEndAndGetNodePath (this XmlSpineParser parser, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
			=> parser.AdvanceToNodeEndAndGetNodePath (new SnapshotTextSource (snapshot), maximumReadahead);


		public static string GetIncompleteValue (this XmlSpineParser spineAtCaret, ITextSnapshot snapshot)
		{
			int currentPosition = spineAtCaret.Position;
			int lineStart = snapshot.GetLineFromPosition (currentPosition).Start.Position;
			int expressionStart = currentPosition - spineAtCaret.CurrentStateLength;
			if (spineAtCaret.GetAttributeValueDelimiter ().HasValue) {
				expressionStart += 1;
			}
			int start = Math.Max (expressionStart, lineStart);
			var expression = snapshot.GetText (start, currentPosition - start);
			return expression;
		}
	}
}
