// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Dom
{
	public static class XmlExtensions
	{
		public static TextSpan GetSquiggleSpan (this XNode node)
		{
			return node is XElement el ? el.NameSpan : node.NextSibling.Span;
		}

		public static bool NameEquals (this INamedXObject obj, string name, bool ignoreCase)
		{
			var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			return !obj.Name.HasPrefix && string.Equals (obj.Name.Name, name, comparison);
		}

		public static bool IsTrue (this XAttributeCollection attributes, string name)
		{
			var att = attributes.Get (new XName (name), true);
			return att != null && string.Equals (att.Value, "true", StringComparison.OrdinalIgnoreCase);
		}

		public static XObject FindNodeAtOffset (this XContainer container, int offset)
		{
			while (container != null) {
				XNode lastNodeBeforeOffset = null;
				foreach (var node in container.Nodes) {
					if (node.Span.Start > offset) {
						break;
					}
					if (node.Span.Contains (offset)) {
						if (node is IAttributedXObject attContainer) {
							foreach (var att in attContainer.Attributes) {
								if (att.Span.Start > offset) {
									break;
								}
								if (att.Span.Contains (offset)) {
									return att;
								}
							}
						}
						return node;
					}
					if (node is XElement el && el.ClosingTag is XClosingTag ct && ct.Span.Contains (offset)) {
						return ct;
					}
					lastNodeBeforeOffset = node;
				}
				container = lastNodeBeforeOffset as XContainer;
			}
			return null;
		}

		public static XObject FindNodeAtOrBeforeOffset (this XContainer container, int offset)
		{
			XNode lastNodeBeforeOffset = null;
			while (container != null) {
				foreach (var node in container.Nodes) {
					if (node.Span.Start > offset) {
						break;
					}
					if (node.Span.Contains (offset)) {
						if (node is IAttributedXObject attContainer) {
							foreach (var att in attContainer.Attributes) {
								if (att.Span.Start > offset) {
									break;
								}
								if (att.Span.Contains (offset)) {
									return att;
								}
							}
						}
						return node;
					}
					if (node is XElement el && el.ClosingTag is XClosingTag ct && ct.Span.Contains (offset)) {
						return ct;
					}
					lastNodeBeforeOffset = node;
				}
				if (lastNodeBeforeOffset == container) {
					return lastNodeBeforeOffset;
				}
				container = lastNodeBeforeOffset as XContainer;
			}
			return lastNodeBeforeOffset;
		}

		public static TextSpan? GetAttributesSpan (this IAttributedXObject obj)
			=> obj.Attributes.First == null
			? (TextSpan?)null
			: TextSpan.FromBounds (obj.Attributes.First.Span.Start, obj.Attributes.Last.Span.End);
	}
}
