// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Parser
{
	public static class XmlParserTextSourceExtensions
	{
		const int DEFAULT_READAHEAD_LIMIT = 5000;

		/// <summary>
		/// Gets the XML name at the parser's position.
		/// </summary>
		/// <param name="spine">A spine parser. It will not be modified.</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static XName GetCompleteName (this XmlSpineParser spine, ITextSource text, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			Debug.Assert (spine.CurrentState is XmlNameState);

			int end = spine.Position;
			int start = end - spine.CurrentStateLength;
			int mid = -1;

			int limit = Math.Min (text.Length, end + maximumReadahead);

			//try to find the end of the name, but don't go too far
			for (; end < limit; end++) {
				char c = text[end];

				if (c == ':') {
					if (mid == -1)
						mid = end;
					else
						break;
				} else if (!XmlChar.IsNameChar (c))
					break;
			}

			if (mid > 0 && end > mid + 1) {
				return new XName (text.GetText (start, mid - start), text.GetText (mid + 1, end - mid - 1));
			}
			return new XName (text.GetText (start, end - start));
		}

		/// <summary>
		/// Advances the parser until the specified object is closed i.e. has a closing tag.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="ob">The object to complete</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <param name="maximumReadahead">Maximum number of characters to advance before giving up.</param>
		/// <returns>Whether the object was successfully completed</returns>
		public static bool AdvanceUntilClosed (this XmlSpineParser parser, XObject ob, ITextSource text, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			var el = ob as XElement;
			if (el == null) {
				return AdvanceUntilEnded (parser, ob, text, maximumReadahead);
			}

			var startingDepth = parser.Spine.Count;

			var end = Math.Min (text.Length - parser.Position, maximumReadahead) + parser.Position;
			while (parser.Position < end) {
				parser.Push (text[parser.Position]);
				if (el.IsClosed) {
					return true;
				}
				// just in case, bail if we pop out past the element's parent
				if (parser.Spine.Count < startingDepth - 1) {
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
		public static bool AdvanceUntilEnded (this XmlSpineParser parser, XObject ob, ITextSource text, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			var startingDepth = parser.Spine.Count;

			var end = Math.Min (text.Length - parser.Position, maximumReadahead) + parser.Position;
			while (parser.Position < end) {
				parser.Push (text[parser.Position]);
				if (ob.IsEnded) {
					return true;
				}
				if (parser.Spine.Count < startingDepth) {
					return false;
				}
			}
			// if at end of document, consider text nodes to be ended anyways
			if (parser.Position == text.Length && ob is XText xt) {
				xt.End (text.GetTextBetween (xt.Span.Start, text.Length));
			}
			return false;
		}

		/// <summary>
		/// Gets the node path at the parser position without changing the parser state, ensuring that the deepest node has a complete name.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will not be modified.</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		public static List<XObject> GetNodePath (this XmlSpineParser parser, ITextSource text)
		{
			var path = parser.Spine.ToNodePath ();

			//complete last node's name without altering the parser state
			int lastIdx = path.Count - 1;
			if (parser.CurrentState is XmlNameState && path[lastIdx] is INamedXObject) {
				XName completeName = GetCompleteName (parser, text);
				var obj = path[lastIdx] = path[lastIdx].ShallowCopy ();
				((INamedXObject)obj).Name = completeName;
			}

			return path;
		}

		/// <summary>
		/// Advances the parser to end the node at the current position and gets that node's path.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="text">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static List<XObject> AdvanceToNodeEndAndGetNodePath (this XmlSpineParser parser, ITextSource text, int maximumReadahead = DEFAULT_READAHEAD_LIMIT)
		{
			var context = parser.GetContext ();

			int startOffset = parser.Position;
			int startDepth = parser.Spine.Count;

			//if in potential start of a state, advance into the next state
			var end = Math.Min (text.Length - context.Position, maximumReadahead) + context.Position;
			if (parser.Position < end && (XmlRootState.IsNotFree (context) || (context.CurrentState is XmlRootState && text[parser.Position] == '<'))) {
				do {
					parser.Push (text[parser.Position]);
				} while (parser.Position < end && XmlRootState.IsNotFree (context));

				//if it transitioned to another state, eat until we get a new node on the stack
				if (parser.Position < end && !(context.CurrentState is XmlRootState) && context.Nodes.Count <= startDepth) {
					parser.Push (text[parser.Position]);
				}
			}

			var path = parser.Spine.ToNodePath ();

			// make sure the leaf node is ended
			if (path.Count > 0) {
				var leaf = path[path.Count-1];
				if (!(leaf is XDocument)) {
					AdvanceUntilEnded (parser, leaf, text, maximumReadahead - (parser.Position - startOffset));
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
				var parent = nodePath[0];
				for (int i = 1; i < nodePath.Count; i++) {
					var node = nodePath[i];
					if (node.Parent == null) {
						if (parent is XContainer c && node is XNode n) {
							c.AddChildNode (n);
						} else {
							node.Parent = parent;
						}
					}
					parent = node;
				}
			}
		}
	}
}
