//
// XDocType.cs
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

#nullable enable

namespace MonoDevelop.Xml.Dom
{
	public class XDocType : XNode, INamedXObject
	{
		public XDocType (int startOffset) : base (startOffset) {}
		public XDocType (TextSpan span) : base (span) {}

		protected XDocType () {}
		protected override XObject NewInstance () { return new XDocType (); }

		public XName RootElement { get; set; }
		public string? PublicFpi { get; set; }
		public bool IsPublic => PublicFpi is not null;
		public TextSpan InternalDeclarationRegion { get; set; }
		public string? Uri { get; set; }

		public override string FriendlyPathRepresentation {
			get { return "<!DOCTYPE>"; }
		}

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			var copyFromDT = (XDocType) copyFrom;
			//immutable types
			RootElement = copyFromDT.RootElement;
			PublicFpi = copyFromDT.PublicFpi;
			InternalDeclarationRegion = copyFromDT.InternalDeclarationRegion;
			Uri = copyFromDT.Uri;
		}

		XName INamedXObject.Name {
			get { return RootElement; }
			set { RootElement = value; }
		}

		bool INamedXObject.IsNamed => RootElement.IsValid;

		TextSpan INamedXObject.NameSpan => new (Span.Start + 10, RootElement.Length);

		public override string ToString ()
			=> $"[DocType: RootElement='{RootElement.FullName}', PublicFpi='{PublicFpi}',  InternalDeclarationRegion='{InternalDeclarationRegion}', Uri='{Uri}']";
	}
}
