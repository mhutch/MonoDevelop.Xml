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
	public static class XmlParserExtensions
	{
		const int DEFAULT_READAHEAD_LIMIT = 5000;

		/// <summary>
		/// Gets the XML name at the parser's position.
		/// </summary>
		/// <param name="spine">A spine parser. It will not be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static XName GetCompleteName (this XmlParser spine, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			Debug.Assert (spine.CurrentState is XmlNameState);

			int end = spine.Position;
			int start = end - spine.CurrentStateLength;
			int mid = -1;

			int limit = Math.Min (snapshot.Length, end + maximumReadahead);

			//try to find the end of the name, but don't go too far
			for (; end < limit; end++) {
				char c = snapshot[end];

				if (c == ':') {
					if (mid == -1)
						mid = end;
					else
						break;
				} else if (!XmlChar.IsNameChar (c))
					break;
			}

			if (mid > 0 && end > mid + 1) {
				return new XName (snapshot.GetText (start, mid - start), snapshot.GetText (mid + 1, end - mid - 1));
			}
			return new XName (snapshot.GetText (start, end - start));
		}

		public static Dictionary<string,string> ToDictionary (this XAttributeCollection attributes, StringComparer comparer)
		{
			var dict = new Dictionary<string, string> (comparer);
			foreach (XAttribute a in attributes) {
				dict[a.Name.FullName] = a.Value ?? string.Empty;
			}
			return dict;
		}

		public static string GetAttributeOrElementValueToCaret (this XmlParser spineAtCaret, SnapshotPoint caretPosition)
		{
			int currentPosition = caretPosition.Position;
			int lineStart = caretPosition.Snapshot.GetLineFromPosition (currentPosition).Start.Position;
			int expressionStart = currentPosition - spineAtCaret.CurrentStateLength;
			if (XmlAttributeValueState.GetDelimiterChar (spineAtCaret).HasValue) {
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
		public static bool AdvanceUntilClosed (this XmlParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			if (!parser.GetContext().BuildTree) {
				throw new ArgumentException ("Parser must be in tree mode");
			}

			var el = ob as XElement;
			if (el == null) {
				return AdvanceUntilEnded (parser, ob, snapshot, maximumReadahead);
			}

			var startingDepth = parser.Nodes.Count;

			var end = Math.Min (snapshot.Length - parser.Position, maximumReadahead) + parser.Position;
			while (parser.Position < end) {
				parser.Push (snapshot[parser.Position]);
				if (el.IsClosed) {
					return true;
				}
				// just in case, bail if we pop out past the element's parent
				if (parser.Nodes.Count < startingDepth - 1) {
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
		/// <param name="snapshot"></param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilEnded (this XmlParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			var startingDepth = parser.Nodes.Count;

			var end = Math.Min (snapshot.Length - parser.Position, maximumReadahead) + parser.Position;
			while (parser.Position < end) {
				parser.Push (snapshot[parser.Position]);
				if (ob.IsEnded) {
					return true;
				}
				if (parser.Nodes.Count < startingDepth) {
					return false;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the node path at the parser position without changing the parser state, ensuring that the deepest node has a complete name.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will not be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		public static List<XObject> GetNodePath (this XmlParser spine, ITextSnapshot snapshot)
		{
			var path = spine.Nodes.ToNodePath ();

			//complete last node's name without altering the parser state
			int lastIdx = path.Count - 1;
			if (spine.CurrentState is XmlNameState && path[lastIdx] is INamedXObject) {
				XName completeName = GetCompleteName (spine, snapshot);
				var obj = path[lastIdx] = path[lastIdx].ShallowCopy ();
				((INamedXObject)obj).Name = completeName;
			}

			return path;
		}

		/// <summary>
		/// Advances the parser to end the node at the current position and gets that node's path.
		/// </summary>
		/// <param name="spine">A spine parser. Its state will be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static List<XObject> AdvanceToNodeEndAndGetNodePath (this XmlParser spine, ITextSnapshot snapshot, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			if (!spine.GetContext ().BuildTree) {
				throw new ArgumentException ("Parser must be in tree mode");
			}

			int startOffset = spine.Position;
			int startDepth = spine.Nodes.Count;

			//if in potential start of a state, advance into the next state
			var end = Math.Min (snapshot.Length - spine.Position, maximumReadahead) + spine.Position;
			if (spine.Position < end && (XmlRootState.IsNotFree (spine) || (spine.CurrentState is XmlRootState && snapshot[spine.Position] == '<'))) {
				do {
					spine.Push (snapshot[spine.Position]);
				} while (spine.Position < end && XmlRootState.IsNotFree (spine));

				//if it transitioned to another state, eat until we get a new node on the stack
				if (spine.Position < end && !(spine.CurrentState is XmlRootState) && spine.Nodes.Count <= startDepth) {
					spine.Push (snapshot[spine.Position]);
				}
			}

			var path = spine.Nodes.ToNodePath ();

			// make sure the leaf node is ended
			if (path.Count > 0) {
				var leaf = path[path.Count-1];
				if (!(leaf is XDocument)) {
					AdvanceUntilEnded (spine, leaf, snapshot, maximumReadahead - (spine.Position - startOffset));
				}
				//the leaf node might have a child that's a better match for the offset
				if (leaf is XContainer c && c.FindAtOffset (startOffset) is XObject o) {
					path.Add (o);
				}
			}

			return path;
		}

		static List<XObject> ToNodePath (this NodeStack stack)
		{
			var path = new List<XObject> (stack);
			path.Reverse ();
			return path;
		}

		public static List<XObject> GetPath (this XObject obj)
		{
			var path = new List<XObject> ();
			while (obj != null) {
				path.Add (obj);
				obj = obj.Parent;
			}
			path.Reverse ();
			return path;
		}

		public static void ConnectParents (this List<XObject> nodePath)
		{
			if (nodePath.Count > 1) {
				var parent = nodePath[nodePath.Count - 1];
				for (int i = nodePath.Count - 2; i >= 0; i--) {
					var node = nodePath[i];
					node.Parent = parent;
					parent = node;
				}
			}
		}

		public static string GetIncompleteValue (this XmlParser spineAtCaret, ITextSnapshot snapshot)
		{
			int currentPosition = spineAtCaret.Position;
			int lineStart = snapshot.GetLineFromPosition (currentPosition).Start.Position;
			int expressionStart = currentPosition - spineAtCaret.CurrentStateLength;
			if (XmlAttributeValueState.GetDelimiterChar (spineAtCaret).HasValue) {
				expressionStart += 1;
			}
			int start = Math.Max (expressionStart, lineStart);
			var expression = snapshot.GetText (start, currentPosition - start);
			return expression;
		}
	}
}
