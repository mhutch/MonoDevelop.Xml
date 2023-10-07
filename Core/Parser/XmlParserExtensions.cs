// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Parser
{
	public static class XmlParserExtensions
	{
		public static bool IsRootFree (this XmlSpineParser parser) => XmlRootState.IsFree(parser.GetContext ());
		public static bool MaybeTag (this XmlSpineParser parser) => XmlRootState.MaybeTag(parser.GetContext ());
		internal static bool MaybeCData (this XmlSpineParser parser) => XmlRootState.MaybeCData(parser.GetContext ());
		internal static bool MaybeDocType (this XmlSpineParser parser) => XmlRootState.MaybeDocType(parser.GetContext ());
		internal static bool MaybeComment (this XmlSpineParser parser) => XmlRootState.MaybeComment(parser.GetContext ());
		internal static bool MaybeCDataOrCommentOrDocType (this XmlSpineParser parser) => XmlRootState.IsNotFree (parser.GetContext ());
		public static bool IsRootNotFree (this XmlSpineParser parser) => XmlRootState.IsNotFree (parser.GetContext ());


		public static bool IsTagFree (this XmlSpineParser parser) => XmlTagState.IsFree (parser.GetContext ());


		public static bool IsExpectingAttributeQuote (this XmlSpineParser parser) => XmlAttributeState.IsExpectingQuote (parser.GetContext ());


		public static char? GetAttributeValueDelimiter (this XmlSpineParser parser) => XmlAttributeValueState.GetDelimiterChar (parser.GetContext ());

		public static bool IsInAttributeValue (this XmlSpineParser parser) => parser.GetContext ().CurrentState is XmlAttributeValueState;

		public static bool IsInText (this XmlSpineParser parser) => parser.GetContext ().CurrentState is XmlTextState;

		public static List<XObject> GetPath (this XmlParser parser) => parser.GetContext().Nodes.ToPath ();

		public static List<XObject> GetPath (this XObject obj)
		{
			XObject? current = obj;
			var path = new List<XObject> ();
			while (current is not null) {
				path.Add (current);
				current = current.Parent;
			}
			path.Reverse ();
			return path;
		}

		static List<XObject> ToPath (this NodeStack stack)
		{
			var path = new List<XObject> (stack);
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
