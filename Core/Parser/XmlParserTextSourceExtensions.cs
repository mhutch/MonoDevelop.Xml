// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public static class XmlParserTextSourceExtensions
	{
		internal const int DEFAULT_READAHEAD_LIMIT = 5000;

		/// <summary>
		/// Gets the XML name at the parser's position.
		/// </summary>
		/// <param name="spine">A spine parser. It will not be modified.</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static bool TryGetCompleteName (this XmlSpineParser spine, ITextSource text, out XName xname, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			Debug.Assert (spine.CurrentState is XmlNameState);

			int start = spine.Position - spine.CurrentStateLength;
			if (!text.TryGetXNameLengthAtPosition (start, spine.Position, out int length, maximumReadahead, cancellationToken)) {
				xname = XName.Empty;
				return false;
			}

			string name = text.GetText (start, length);
			xname = XNameFromString (name);
			return true;
		}

		static XName XNameFromString (string name)
		{
			int i = name.IndexOf (':');
			if (i < 0) {
				return new XName (name);
			} else {
				return new XName (name.Substring (0, i), name.Substring (i + 1));
			}
		}

		public static bool TryGetXNameLengthAtPosition (this ITextSource text, int nameStartPosition, int currentPosition, out int xnameLength, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			int readaheadLimit = currentPosition + maximumReadahead;
			int limit = Math.Min (text.Length, readaheadLimit);

			//try to find the end of the name, but don't go too far
			for (; currentPosition < limit; currentPosition++) {
				char c = text[currentPosition];
				if (!XmlChar.IsNameChar (c)) {
					break;
				}
				if (cancellationToken.IsCancellationRequested) {
					xnameLength = 0;
					return false;
				}
			}

			if (currentPosition + 1 == readaheadLimit) {
				xnameLength = 0;
				return false;
			}

			xnameLength = currentPosition - nameStartPosition;
			return true;
		}

		public static bool TryGetAttributeValueLengthAtPosition (this ITextSource text, char delimiter, int attributeStartPosition, int currentPosition, out int attributeValueLength, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			int readaheadLimit = currentPosition + maximumReadahead;
			int limit = Math.Min (text.Length, readaheadLimit);

			//try to find the end of the name, but don't go too far
			for (; currentPosition < limit; currentPosition++) {
				char c = text[currentPosition];
				if (XmlChar.IsInvalid (c) || c == '<') {
					attributeValueLength = currentPosition - attributeStartPosition - 1;
					return true;
				}
				switch (delimiter) {
				case '\'':
				case '"':
					if (c == delimiter) {
						attributeValueLength = currentPosition - attributeStartPosition;
						return true;
					}
					break;
				default:
					if (XmlChar.IsWhitespace (c)) {
						attributeValueLength = currentPosition - attributeStartPosition;
						return true;
					}
					break;
				}
				if (cancellationToken.IsCancellationRequested) {
					attributeValueLength = 0;
					return false;
				}
			}

			if (currentPosition + 1 == readaheadLimit) {
				attributeValueLength = 0;
				return false;
			}

			attributeValueLength = currentPosition - attributeStartPosition;
			return true;
		}

		/// <summary>
		/// Advances the parser until the specified object is closed i.e. has a closing tag.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="ob">The object to complete</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilClosed (this XmlSpineParser parser, XObject ob, ITextSource text, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			var el = ob as XElement;
			if (el == null) {
				return AdvanceUntilEnded (parser, ob, text, maximumReadahead, cancellationToken);
			}

			int readaheadLimit = parser.Position + maximumReadahead;
			int limit = Math.Min (text.Length, readaheadLimit);

			var startingDepth = parser.Spine.Count;

			while (parser.Position < limit) {
				parser.Push (text[parser.Position]);
				if (el.IsClosed) {
					return true;
				}
				// just in case, bail if we pop out past the element's parent
				if (parser.Spine.Count < startingDepth - 1) {
					return false;
				}
				if (cancellationToken.IsCancellationRequested) {
					return false;
				}
			}

			return false;
		}

		/// <summary>
		/// Advances the parser until the specified object is ended.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="ob">The object to complete</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilEnded (this XmlSpineParser parser, XObject ob, ITextSource text, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			var startingDepth = parser.Spine.Count;

			int readaheadLimit = parser.Position + maximumReadahead;
			int limit = Math.Min (text.Length, readaheadLimit);

			while (parser.Position < limit) {
				parser.Push (text[parser.Position]);
				if (ob.IsEnded) {
					return true;
				}
				if (parser.Spine.Count < startingDepth) {
					return false;
				}
				if (cancellationToken.IsCancellationRequested) {
					return false;
				}
			}

			// if at end of document, forcefully ends all nodes
			if (parser.Position == text.Length) {
				parser.EndAllNodes ();
				return true;
			}

			if (parser.Position + 1 >= readaheadLimit) {
				return false;
			}

			return false;
		}

		/// <summary>
		/// Advances the parser until the specified condition is met or the end of the line is reached.
		/// </summary>
		public static bool AdvanceParserUntilConditionOrEol (this XmlSpineParser parser, ITextSource text, Func<XmlParser, bool> condition, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			if (parser.Position == text.Length) {
				return true;
			}

			int readaheadLimit = parser.Position + maximumReadahead;
			int limit = Math.Min (text.Length, readaheadLimit);

			while (parser.Position < limit) {
				char c = text[parser.Position];
				if (c == '\r' || c == '\n') {
					return true;
				}
				parser.Push (c);
				if (condition (parser)) {
					return true;
				}
				if (cancellationToken.IsCancellationRequested) {
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the node path at the parser position without changing the parser state, ensuring that the deepest node has a complete name.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will not be modified.</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		public static bool TryGetNodePath (this XmlSpineParser parser, ITextSource text, [NotNullWhen (true)] out List<XObject>? nodePath, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			var path = parser.GetPath ();

			//complete last node's name without altering the parser state
			int lastIdx = path.Count - 1;
			if (parser.CurrentState is XmlNameState && path[lastIdx] is INamedXObject) {
				if (!TryGetCompleteName (parser, text, out XName completeName, maximumReadahead, cancellationToken)) {
					nodePath = null;
					return false;
				}
				var obj = path[lastIdx] = path[lastIdx].ShallowCopy ();
				((INamedXObject)obj).Name = completeName;
			}

			nodePath = path;
			return true;
		}

		/// <summary>
		/// Advances the parser to end the node at the current position and gets that node's path.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static bool TryAdvanceToNodeEndAndGetNodePath (this XmlSpineParser parser, ITextSource text, [NotNullWhen (true)] out List<XObject>? nodePath, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			var context = parser.GetContext ();

			int startOffset = parser.Position;
			int startDepth = parser.Spine.Count;

			int readaheadLimit = parser.Position + maximumReadahead;
			int limit = Math.Min (text.Length, readaheadLimit);

			//if in potential start of a state, advance into the next state
			if (parser.Position < limit && (XmlRootState.IsNotFree (context) || (context.CurrentState is XmlRootState && text[parser.Position] == '<'))) {
				do {
					parser.Push (text[parser.Position]);
					if (cancellationToken.IsCancellationRequested) {
						nodePath = null;
						return false;
					}
				} while (parser.Position < limit && XmlRootState.IsNotFree (context));

				//if it transitioned to another state, eat until we get a new node on the stack
				if (parser.Position < limit && !(context.CurrentState is XmlRootState) && context.Nodes.Count <= startDepth) {
					parser.Push (text[parser.Position]);
				}
				if (cancellationToken.IsCancellationRequested) {
					nodePath = null;
					return false;
				}
			}

			var path = parser.GetPath ();

			// make sure the leaf node is ended
			if (path.Count > 0) {
				var leaf = path[path.Count-1];
				if (!(leaf is XDocument)) {
					if (!AdvanceUntilEnded (parser, leaf, text, maximumReadahead - (parser.Position - startOffset), cancellationToken)) {
						nodePath = null;
						return false;
					}
				}
				//the leaf node might have a child that's a better match for the offset
				if (leaf is XContainer c && c.FindAtOffset (startOffset) is XObject o) {
					path.Add (o);
				}
			}

			nodePath = path;
			return true;
		}

		/// <summary>
		/// If the parser is within an attribute value or XText node, determine the full value and span of that value.
		/// </summary>
		/// <param name="parser">A spine parser. Its position will not be modified.</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <param name="value">The full value of the attribute or text, or as much as could be recovered before hitting the readahead limit.</param>
		/// <param name="valueSpan">The span of the <paramref name="value"/>.</param>
		/// <param name="stopAtLineBreak">Whether to truncate the value upon reaching a line break.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the parser is in a value state and the full value could be read. If <c>false</c>, and the parser was in a value state, the <paramref name="value"/> may be non-null but incomplete.</returns>
		public static bool TryGetIncompleteValue (
			this XmlSpineParser parser, ITextSource text, [NotNullWhen (true)] out string? value, [NotNullWhen (true)] out TextSpan? valueSpan,
			bool stopAtLineBreak = false, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			if (parser.IsInAttributeValue ()) {
				int length = parser.CurrentStateLength;
				var cloneParser = parser.Clone ();

				bool success = cloneParser.TryAdvanceUntilStateChange (text, stopAtLineBreak, out var deepestNode, maximumReadahead, cancellationToken);

				if (cancellationToken.IsCancellationRequested) {
					value = null;
					valueSpan = null;
					return false;
				}

				// this should always succeed, as a value state can only begin when an attribute state is on the stack already
				var attributeNode = (XAttribute)deepestNode;

				if (success) {
					valueSpan = attributeNode.ValueSpan!.Value;
					value = attributeNode.Value ?? "";
				} else {
					value = cloneParser.GetContext().KeywordBuilder.ToString ();
					valueSpan = new TextSpan (cloneParser.Position - value.Length, value.Length);
				}
				return success;
			}

			if (parser.IsInText ()) {
				int length = parser.CurrentStateLength;
				var cloneParser = parser.Clone ();
				bool success = cloneParser.TryAdvanceUntilStateChange (text, stopAtLineBreak, out var deepestNode, maximumReadahead, cancellationToken);

				// deepestNode may not be XText if it was at the start of the state and no chars were pushed due to cancellation
				if (cancellationToken.IsCancellationRequested || deepestNode is not XText textNode) {
					value = null;
					valueSpan = null;
					return false;
				}

				// if we did not reach the end of the node, force it to end anyways
				if (!success) {
					cloneParser.Push ('<');
				}

				valueSpan = textNode.Span;
				value = textNode.Text;

				return success;
			}

			value = null;
			valueSpan = null;
			return false;
		}

		static bool TryAdvanceUntilStateChange (this XmlSpineParser parser, ITextSource text, bool stopAtLineBreak, out XObject captureDeepestNode, int maximumReadahead = DEFAULT_READAHEAD_LIMIT, CancellationToken cancellationToken = default)
		{
			var nodeStack = parser.GetContext ().Nodes;
			captureDeepestNode = nodeStack.Peek ();
			int deepestStack = nodeStack.Count;

			int readaheadLimit = parser.Position + maximumReadahead;
			int limit = Math.Min (text.Length, readaheadLimit);
			var startingState = parser.CurrentState;

			while (parser.Position < limit && parser.CurrentState == startingState && !cancellationToken.IsCancellationRequested) {
				char c = text[parser.Position];
				if (stopAtLineBreak && (c == '\n' || c == '\r')) {
					break;
				}
				parser.Push (c);
				if (nodeStack.Count > deepestStack) {
					captureDeepestNode = nodeStack.Peek ();
				}
			}

			return parser.CurrentState != startingState;
		}
	}
}
