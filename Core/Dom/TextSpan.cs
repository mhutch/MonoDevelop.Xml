// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace MonoDevelop.Xml.Dom
{
	public struct TextSpan : IEquatable<TextSpan>
	{
		public TextSpan (int start, int length)
		{
			Start = start;
			Length = length;
		}

        public int Start { get; }
        public int Length { get; }
		public int End => Start + Length;

		public bool Contains (int offset) => offset >= Start && offset < End;

		public bool ContainsOuter (int offset) => offset >= Start && offset <= End;
		public bool Contains (TextSpan other) => Start <= other.Start && (End > other.End || (End == other.End && other.Length > 0));
		public bool ContainsOuter (TextSpan other) => Start <= other.Start && End >= other.End;
		public bool Intersects (TextSpan other) => other.Start <= End && other.End >= Start;

		public static TextSpan FromBounds (int start, int end) => new (start, end - start);

		public override string ToString () => $"({Start}-{End})";

		public bool Equals (TextSpan other) => other.Start == Start && other.Length == Length;

		public override bool Equals (object? obj) => obj is TextSpan t && t.Equals (this);

		public override int GetHashCode () => (Start << 16) ^ (Start >> 16) ^ Length; //try to distribute bits over the range a bit better

		public static explicit operator TextSpan (int position) => new (position, 0);
	}
}
