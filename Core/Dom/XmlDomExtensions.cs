// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MonoDevelop.Xml.Dom
{
	public static class XmlDomExtensions
	{
		public static TextSpan GetSquiggleSpan (this XNode node)
		{
			return node is XElement el ? el.NameSpan : node.NextSibling?.Span ?? node.Span;
		}

		public static bool NameEquals (this INamedXObject obj, string name, bool ignoreCase)
		{
			var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			return !obj.Name.HasPrefix && string.Equals (obj.Name.Name, name, comparison);
		}

		public static bool IsTrue (this XAttributeCollection attributes, string name)
		{
			var att = attributes.Get (name, true);
			return att != null && string.Equals (att.Value, "true", StringComparison.OrdinalIgnoreCase);
		}

		static XAttribute? FindAttribute (this IAttributedXObject attContainer, int offset)
		{
			foreach (var att in attContainer.Attributes) {
				if (att.Span.Start > offset) {
					break;
				}
				if (att.Span.Contains (offset)) {
					return att;
				}
			}
			return null;
		}

		public static XObject? FindAtOffset (this XContainer container, int offset)
		{
			if (container.Span.Contains (offset) && container is IAttributedXObject attObj && attObj.FindAttribute (offset) is XAttribute attribute) {
				return attribute;
			}

			XContainer? current = container;

			while (current is not null) {
				XNode? lastNodeBeforeOffset = null;
				foreach (var node in current.Nodes) {
					if (node.Span.Start > offset) {
						break;
					}
					if (node.Span.Contains (offset)) {
						if (node is IAttributedXObject attContainer && attContainer.FindAttribute (offset) is XAttribute att) {
							return att;
						}
						return node;
					}
					if (node is XElement el && el.ClosingTag is XClosingTag ct && ct.Span.Contains (offset)) {
						return ct;
					}
					lastNodeBeforeOffset = node;
				}
				current = lastNodeBeforeOffset as XContainer;
			}
			return null;
		}

		public static XObject? FindAtOrBeforeOffset (this XContainer container, int offset)
		{
			if (container.Span.Contains (offset) && container is IAttributedXObject attObj && attObj.FindAttribute (offset) is XAttribute attribute) {
				return attribute;
			}

			XNode? lastNodeBeforeOffset = null;
			XContainer? current = container;

			while (current is not null) {
				foreach (var node in current.Nodes) {
					if (node.Span.Start > offset) {
						break;
					}
					if (node.Span.Contains (offset)) {
						if (node is IAttributedXObject attContainer && attContainer.FindAttribute (offset) is XAttribute att) {
							return att;
						}
						return node;
					}
					if (node is XElement el && el.ClosingTag is XClosingTag ct && ct.Span.Contains (offset)) {
						return ct;
					}
					lastNodeBeforeOffset = node;
				}
				if (lastNodeBeforeOffset == current) {
					return lastNodeBeforeOffset;
				}
				current = lastNodeBeforeOffset as XContainer;
			}
			return lastNodeBeforeOffset;
		}

		public static TextSpan? GetAttributesSpan (this IAttributedXObject obj)
			=> obj.Attributes.IsEmpty
			? null
			: TextSpan.FromBounds (obj.Attributes.First.Span.Start, obj.Attributes.Last.Span.End);

		public static Dictionary<string, string> ToDictionary (this XAttributeCollection attributes, StringComparer comparer)
		{
			var dict = new Dictionary<string, string> (comparer);
			foreach (XAttribute a in attributes) {
				if (a.Name.IsValid) {
					dict[a.Name.FullName] = a.Value ?? string.Empty;
				}
			}
			return dict;
		}

		public static XNode? FindPreviousNode (this XNode node) => node.FindPreviousSibling () ?? node.Parent as XNode;

		public static XNode? FindPreviousSibling (this XNode node)
		{
			if (node.Parent is XContainer container) {
				var n = container.FirstChild;
				while (n != null) {
					if (n.NextSibling == node) {
						return n;
					}
					n = n.NextSibling;
				}
			}
			return null;
		}

		public static XElement? GetPreviousSiblingElement (this XElement element)
		{
			if (element.Parent is XElement parentEl) {
				var node = parentEl.FirstChild;
				XElement? prevElement = null;
				while (node != null) {
					if (node is XElement nextElement) {
						if (nextElement == element) {
							return prevElement;
						}
						prevElement = nextElement;
					}
					node = node.NextSibling;
				}
			}
			return null;
		}

		public static XAttribute? FindPreviousSibling (this XAttribute att)
		{
			if (att.Parent is IAttributedXObject p && p.Attributes is XAttributeCollection atts) {
				var a = atts.First;
				while (a != null) {
					if (a.NextSibling == att) {
						return a;
					}
					a = a.NextSibling;
				}
			}
			return null;
		}

		/// <summary>
		/// Get the nodes from the document that intersect a particular range.
		/// </summary>
		public static IEnumerable<XNode> GetNodesIntersectingRange (this XDocument document, TextSpan span)
		{
			if (document.FindAtOrBeforeOffset (span.Start) is not XObject startObj) {
				yield break;
			}

			var node = startObj as XNode ?? startObj.Parents.OfType<XNode> ().First ();

			foreach (var n in node.FollowingNodes ()) {
				if (n.Span.Start > span.End) {
					yield break;
				}
				if (n.Span.Intersects (span)) {
					yield return n;
				}
			}
		}

		/// <summary>
		/// Gets subsequent nodes in the document in the order they appears in the text.
		/// </summary>
		public static IEnumerable<XNode> FollowingNodes (this XNode node)
		{
			XNode? current = node;

			while (current is not null) {
				yield return current;
				if (current is XContainer c) {
					foreach (var n in c.AllDescendentNodes) {
						yield return n;
					}
				}
				while (current is not null && current.NextSibling == null) {
					current = current.Parent as XNode;
				}
				current = current?.NextSibling;
			}
		}

		public static IEnumerable<T> SelfAndParentsOfType<T> (this XObject obj)
		{
			XObject? current = obj;

			while (current is not null) {
				if (current is T t) {
					yield return t;
				}
				current = current.Parent;
			}
		}

		public static XNode? GetNodeContainingRange (this XNode node, TextSpan span)
		{
			if (node is XContainer container) {
				foreach (var child in container.Nodes) {
					var found = child.GetNodeContainingRange (span);
					if (found != null) {
						return found;
					}
				}
			}

			if (node.Span.Contains (span)) {
				return node;
			}

			return null;
		}

		public static void VisitSelfAndDescendents (this XNode node, Action<XNode> action)
		{
			action (node);
			if (node is XContainer container) {
				foreach (var child in container.Nodes) {
					VisitSelfAndDescendents (child, action);
				}
			}
		}
	}
}
