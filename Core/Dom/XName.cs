//
// XName.cs
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

using System;
using System.Diagnostics.CodeAnalysis;

namespace MonoDevelop.Xml.Dom
{
	public struct XName : IEquatable<XName>
	{
		public XName (string prefix, string name)
		{
			Prefix = prefix;
			Name = name;
		}

		public XName (string name)
		{
			Prefix = null;
			Name = name;
		}

		public static XName Empty => default;

		public string? Prefix { get; }

		public string? Name { get; }

		public string? FullName => Prefix == null ? Name : Prefix + ':' + Name;

		[MemberNotNullWhen(true, nameof(Name), nameof (FullName))]
		public bool IsValid { get { return !string.IsNullOrEmpty (Name); } }

		[MemberNotNullWhen(true, nameof(Prefix))]
		public bool HasPrefix { get { return !string.IsNullOrEmpty (Prefix); } }

		public int Length => (Name?.Length ?? 0) + (Prefix != null ? Prefix.Length + 1 : 0);

		#region Equality

		public static bool operator == (XName x, XName y) => x.Equals (y);

		public static bool operator != (XName x, XName y) => !x.Equals (y);

		public bool Equals (XName other) => Prefix == other.Prefix && Name == other.Name;

		public override bool Equals (object? obj) => (obj is not XName other) || Equals (other);

		public override int GetHashCode ()
		{
			int hash = 0;
			if (Prefix != null) hash += Prefix.GetHashCode ();
			if (Name != null) hash += Name.GetHashCode ();
			return hash;
		}

		#endregion

		public static bool Equals (XName a, XName b, bool ignoreCase)
		{
			var comp = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			return string.Equals (a.Prefix, b.Prefix, comp) && string.Equals (a.Name, b.Name, comp);
		}

		public override string ToString ()
		{
			if (!HasPrefix)
				return Name ?? "[Empty Name]";
			return Prefix + ":" + Name;
		}

		public static implicit operator XName (string name) => new(name);
	}
}
