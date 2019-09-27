using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor
{
	public static class XmlParserExtensions
	{
		/// <summary>
		/// Gets the XML name at the parser's position.
		/// </summary>
		/// <param name="spine">A spine parser. It will not be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static XName GetCompleteName (this XmlParser spine, ITextSnapshot snapshot, int maximumReadahead = 50)
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
		public static bool AdvanceUntilClosed (this XmlParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = 500)
		{
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
				if (parser.Nodes.Count < startingDepth) {
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
		public static bool AdvanceUntilEnded (this XmlParser parser, XObject ob, ITextSnapshot snapshot, int maximumReadahead = 500)
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
		/// Gets the node path at the parser condition. Reads ahead to complete names, but does not complete the nodes.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		public static List<XObject> GetNodePath (this XmlParser spine, ITextSnapshot snapshot)
		{
			var path = new List<XObject> (spine.Nodes);

			//remove the root XDocument
			path.RemoveAt (path.Count - 1);

			//complete incomplete XName if present
			if (spine.CurrentState is XmlNameState && path[0] is INamedXObject) {
				path[0] = path[0].ShallowCopy ();
				XName completeName = GetCompleteName (spine, snapshot);
				((INamedXObject)path[0]).Name = completeName;
			}
			path.Reverse ();
			return path;
		}

		/// <summary>
		/// Gets the node path at the parser condition, ensuring that the deepest element is closed.
		/// </summary>
		/// <param name="parser">A spine parser. Its state will be modified.</param>
		/// <param name="snapshot">The text snapshot corresponding to the parser.</param>
		/// <returns></returns>
		public static List<XObject> GetNodePathWithCompleteLeafElement (this XmlParser parser, ITextSnapshot snapshot)
		{
			int offset = parser.Position;
			var length = snapshot.Length;
			int i = offset;

			var nodePath = parser.Nodes.ToList ();

			//if inside body of unclosed element, capture whole body
			if (parser.CurrentState is XmlRootState && parser.Nodes.Peek () is XElement unclosedEl) {
				while (i < length && InRootOrClosingTagState () && !unclosedEl.IsClosed) {
					parser.Push (snapshot[i++]);
				}
			}

			//if in potential start of a state, capture it
			else if (parser.CurrentState is XmlRootState && GetStateTag () > 0) {
				//eat until we figure out whether it's a state transition 
				while (i < length && GetStateTag () > 0) {
					parser.Push (snapshot[i++]);
				}
				//if it transitioned to another state, eat until we get a new node on the stack
				if (NotInRootState ()) {
					var newState = parser.CurrentState;
					while (i < length && NotInRootState () && parser.Nodes.Count <= nodePath.Count) {
						parser.Push (snapshot[i++]);
					}
					if (parser.Nodes.Count > nodePath.Count) {
						nodePath.Insert (0, parser.Nodes.Peek ());
					}
				}
			}

			//ensure any unfinished names are captured
			while (i < length && InNameOrAttributeState ()) {
				parser.Push (snapshot[i++]);
			}

			//if nodes are incomplete, they won't get connected
			if (nodePath.Count > 1) {
				for (int idx = 0; idx < nodePath.Count - 1; idx++) {
					var node = nodePath[idx];
					if (node.Parent == null) {
						var parent = nodePath[idx + 1];
						node.Parent = parent;
					}
				}
			}

			return nodePath;

			bool InNameOrAttributeState () =>
				parser.CurrentState is XmlNameState
					|| parser.CurrentState is XmlAttributeState
					  || parser.CurrentState is XmlAttributeValueState;

			bool InRootOrClosingTagState () =>
				parser.CurrentState is XmlRootState
				  || parser.CurrentState is XmlNameState
				  || parser.CurrentState is XmlClosingTagState;

			int GetStateTag () => ((IXmlParserContext)parser).StateTag;

			bool NotInRootState () => !(parser.CurrentState is XmlRootState);
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
