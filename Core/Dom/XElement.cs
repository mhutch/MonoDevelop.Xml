//
// XElement.cs
//
// Author:
//   Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonoDevelop.Xml.Dom
{
	public class XElement : XContainer, IAttributedXObject
	{
		public XElement (int startOffset) : base (startOffset)
		{
			Attributes = new XAttributeCollection (this);
		}

		public XElement (int startOffset, XName name) : this (startOffset)
		{
			this.Name = name;
		}

		public XNode ClosingTag { get; private set; }
		public bool IsClosed { get { return ClosingTag != null; } }
		public bool IsSelfClosing { get { return ClosingTag == this; } }

		public void Close (XNode closingTag)
		{
			this.ClosingTag = closingTag;
			if (closingTag is XClosingTag)
				closingTag.Parent = this;
		}

		public XName Name { get; set; }

		internal bool HasEndBracket { get; set; }

		public override bool IsComplete => HasEndBracket && IsNamed;
		public bool IsNamed { get { return Name.IsValid; } }

		public XAttributeCollection Attributes { get; }

		protected XElement ()
		{
			Attributes = new XAttributeCollection (this);
		}

		protected override XObject NewInstance () { return new XElement (); }

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			var copyFromEl = (XElement) copyFrom;
			Name = copyFromEl.Name; //XName is immutable value type
			//include attributes
			foreach (var a in copyFromEl.Attributes)
				Attributes.AddAttribute ((XAttribute) a.ShallowCopy ());
		}

		public override string ToString ()
		{
			return string.Format ("[XElement Name='{0}' Location='{1}'", Name.FullName, Span);
		}

		public override void BuildTreeString (StringBuilder builder, int indentLevel)
		{
			builder.Append (' ', indentLevel * 2);
			builder.AppendFormat ("[XElement Name='{0}' Location='{1}' Children=", Name.FullName, Span);
			builder.AppendLine ();

			foreach (XNode child in Nodes)
				child.BuildTreeString (builder, indentLevel + 1);

			builder.Append (' ', indentLevel * 2);
			builder.Append ("Attributes=");
			builder.AppendLine ();

			foreach (XAttribute att in Attributes)
				att.BuildTreeString (builder, indentLevel + 1);

			if (ClosingTag is XClosingTag) {
				builder.AppendLine ("ClosingTag=");
				ClosingTag.BuildTreeString (builder, indentLevel + 1);
			} else if (ClosingTag == null)
				builder.AppendLine ("ClosingTag=(null)");
			else
				builder.AppendLine ("ClosingTag=(Self)");

			builder.Append (' ', indentLevel * 2);
			builder.AppendLine ("]");
		}

		public override string FriendlyPathRepresentation {
			get { return Name.FullName; }
		}

		public IEnumerable<XElement> Elements {
			get {
				XElement el;
				foreach (XNode node in Nodes) {
					el = node as XElement;
					if (el != null)
						yield return el;
				}
			}
		}

		public IEnumerable<XElement> AllDescendentElements {
			get {
				foreach (XElement el in Elements) {
					yield return el;
					foreach (XElement el2 in el.AllDescendentElements)
						yield return el2;
				}
			}
		}

		public int NameOffset => Span.Start + 1;

		public TextSpan NameSpan => new TextSpan (Span.Start + 1, Name.Length);

		public XElement GetNextSiblingElement ()
		{
			var node = NextSibling;
			while (node != null) {
				if (node is XElement nextElement) {
					return nextElement;
				}
				node = node.NextSibling;
			}
			return null;
		}

		public IEnumerable<XElement> FollowingElements {
			get {
				var node = NextSibling;
				while (node != null) {
					if (node is XElement nextElement) {
						yield return nextElement;
					}
					node = node.NextSibling;
				}
			}
		}

		public XElement ParentElement => Parent as XElement;

		IEnumerable<XNode> WithClosingTag (IEnumerable<XNode> nodes)
		{
			foreach (var n in nodes) {
				yield return n;
			}
			yield return ClosingTag;
		}

		public override IEnumerable<XNode> AllDescendentNodes {
			get {
				if (ClosingTag != null && ClosingTag != this) {
					return WithClosingTag (base.AllDescendentNodes);
				}
				return base.AllDescendentNodes;
			}
		}

		/// <summary>
		/// The span of the content between this tag and the closing tag. Null if there is no closing tag or it is self closing.
		/// </summary>
		public TextSpan? InnerSpan => (ClosingTag == null || IsSelfClosing)
			? (TextSpan?) null
			: TextSpan.FromBounds (Span.End, ClosingTag.Span.Start);

		/// <summary>
		/// The span from the start of this element to the end of its closing tag.
		/// </summary>
		public override TextSpan OuterSpan => ClosingTag == null? Span : TextSpan.FromBounds (Span.Start, ClosingTag.Span.End);
	}
}
