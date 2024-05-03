// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;

namespace MonoDevelop.Xml.Dom;

public class XNameComparer : IComparer<XName>, IEqualityComparer<XName>
{
	readonly bool ignoreCase;

	XNameComparer (bool ignoreCase)
	{
		this.ignoreCase = ignoreCase;
	}

	public static XNameComparer Ordinal { get; } = new XNameComparer (false);

	public static XNameComparer OrdinalIgnoreCase { get; } = new XNameComparer (true);

	public int Compare (XName x, XName y) => x.CompareTo (y, ignoreCase);

	public bool Equals (XName x, XName y) => x.Equals (y, ignoreCase);

	public int GetHashCode (XName obj) => obj.GetHashCode (ignoreCase);
}
