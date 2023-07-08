// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor
{
	public static class XmlParserSnapshotExtensions
	{
		const int DEFAULT_READAHEAD_LIMIT = XmlParserTextSourceExtensions.DEFAULT_READAHEAD_LIMIT;

		/// <summary>
		/// Gets the XML name at the parser's position.
		/// </summary>
		/// <param name="spine">A spine parser. It will not be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static bool TryGetCompleteName (this XmlSpineParser parser, ITextSnapshot snapshot, out XName completeName, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
			=> parser.TryGetCompleteName (new SnapshotTextSource (snapshot), out completeName, maximumReadahead, cancellationToken);

		/// <summary>
		/// Advances the parser until the specified object is closed i.e. has a closing tag.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="ob">The object to complete</param>
		/// <param name="snapshot"></param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilClosed (this XmlSpineParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
			=> parser.AdvanceUntilClosed (ob, new SnapshotTextSource (snapshot), maximumReadahead, cancellationToken);

		/// <summary>
		/// Advances the parser until the specified object is ended.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="ob">The object to complete</param>
		/// <param name="snapshot"></param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilEnded (this XmlSpineParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
			=> parser.AdvanceUntilEnded (ob, new SnapshotTextSource (snapshot), maximumReadahead, cancellationToken);


		/// <summary>
		/// Advances the parser until the specified condition is met or the end of the line is reached.
		/// </summary>
		public static bool AdvanceParserUntilConditionOrEol (this XmlSpineParser parser, ITextSnapshot snapshot, Func<XmlParser, bool> condition, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
			=> parser.AdvanceParserUntilConditionOrEol (new SnapshotTextSource (snapshot), condition, maximumReadahead, cancellationToken);

		/// <summary>
		/// Gets the node path at the parser position without changing the parser state, ensuring that the deepest node has a complete name.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will not be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		public static bool TryGetNodePath (this XmlSpineParser parser, ITextSnapshot snapshot, [NotNullWhen (true)] out List<XObject>? nodePath, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
			=> parser.TryGetNodePath (new SnapshotTextSource (snapshot), out nodePath, maximumReadahead, cancellationToken);

		/// <summary>
		/// Advances the parser to end the node at the current position and gets that node's path.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static bool TryAdvanceToNodeEndAndGetNodePath (this XmlSpineParser parser, ITextSnapshot snapshot, [NotNullWhen (true)] out List<XObject>? nodePath, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
			=> parser.TryAdvanceToNodeEndAndGetNodePath (new SnapshotTextSource (snapshot), out nodePath, maximumReadahead, cancellationToken);


		public static string GetIncompleteValue (this XmlSpineParser spineAtCaret, ITextSnapshot snapshot)
		{
			int caretPosition = spineAtCaret.Position;
			var node = spineAtCaret.Spine.Peek ();

			int valueStart;
			if (node is XText t) {
				valueStart = t.Span.Start;
			} else if (node is XElement el && el.IsEnded) {
				valueStart = el.Span.End;
			} else {
				int lineStart = snapshot.GetLineFromPosition (caretPosition).Start.Position;
				valueStart = spineAtCaret.Position - spineAtCaret.CurrentStateLength;
				if (spineAtCaret.GetAttributeValueDelimiter ().HasValue) {
					valueStart += 1;
				}
				valueStart = Math.Min (Math.Max (valueStart, lineStart), caretPosition);
			}

			return snapshot.GetText (valueStart, caretPosition - valueStart);
		}

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
	}
}
