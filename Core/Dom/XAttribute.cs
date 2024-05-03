//
// XAttribute.cs
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

using System.Diagnostics.CodeAnalysis;

namespace MonoDevelop.Xml.Dom
{
	public class XAttribute : XObject, INamedXObject
	{
		public XAttribute (int startOffset, XName name, string value, int valueOffset) : base (startOffset)
		{
			Name = name;
			Value = value;
			ValueOffset = valueOffset;
		}

		public XAttribute (int startOffset) : base (startOffset) { }

		public XName Name { get; set; }

		[MemberNotNullWhen (true, nameof (Value), nameof (ValueOffset), nameof (ValueSpan))]
		public override bool IsComplete => base.IsComplete && IsNamed && HasValue;

		public bool IsNamed => Name.IsValid;

		public string? Value { get; private set; }

		public int? ValueOffset { get; private set; }

		[MemberNotNull(nameof(Value), nameof(ValueOffset))]
		internal void SetValue (int offset, string value)
		{
			ValueOffset = offset;
			Value = value;
		}

		public XAttribute? NextSibling { get; internal protected set; }

		protected XAttribute () {}
		protected override XObject NewInstance () { return new XAttribute (); }

		protected override void ShallowCopyFrom (XObject copyFrom)
		{
			base.ShallowCopyFrom (copyFrom);
			var copyFromAtt = (XAttribute) copyFrom;
			//immutable types
			Name = copyFromAtt.Name;
			Value = copyFromAtt.Value;
			ValueOffset = copyFromAtt.ValueOffset;
		}

		public override string ToString () => $"[XAttribute Name='{Name.FullName}' Location='{Span}' Value='{Value}']";

		public override string FriendlyPathRepresentation => "@" + Name.FullName;

		public TextSpan NameSpan => new (Span.Start, Name.Length);

		public TextSpan? ValueSpan => HasValue? new (ValueOffset.Value, Value.Length) : null;

		// value nullability helpers

		[MemberNotNullWhen (true, nameof (Value), nameof (ValueOffset), nameof (ValueSpan))]
		public bool HasValue => Value is not null;

		[MemberNotNullWhen (true, nameof (Value), nameof (ValueOffset), nameof (ValueSpan))]
		public bool HasNonEmptyValue => !string.IsNullOrEmpty (Value);

		[MemberNotNullWhen (true, nameof (Value), nameof (ValueOffset), nameof (ValueSpan))]
		public bool TryGetValue ([NotNullWhen (true)] out string? value)
		{
			value = Value;
			return HasValue;
		}

		[MemberNotNullWhen (true, nameof (Value), nameof (ValueOffset), nameof (ValueSpan))]
		public bool TryGetNonEmptyValue ([NotNullWhen (true)] out string? value)
		{
			value = Value;
			return HasNonEmptyValue;
		}
	}
}
