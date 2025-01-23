// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace MonoDevelop.Xml.Options;

class EditorConfigCharSetSerializer : EditorConfigSerializerImpl<Encoding>
{
	public override string Serialize (Encoding value)
	{
		if (TrySerialize (value, out var name) && name is not null) {
			return name;
		}
		throw new InvalidOperationException ($"Encoding '{value}' cannot be serialized to editorconfig charset");
	}

	public override bool TryParse (string value, out Encoding? encoding)
	{
		switch (value) {
			case "latin1": encoding = Latin1; return true;
			case "utf-8": encoding = Utf8NoBom; return true;
			case "utf-8-bom": encoding = Utf8WithBom; return true;
			case "utf-16be": encoding = Utf16BigEndian; return true;
			case "utf-16le": encoding = Utf16LittleEndian; return true;
			default: encoding = null; return false;
		}
	}

	static bool TrySerialize (Encoding encoding, out string? name)
	{
		switch(encoding.CodePage) {
			case 28591:
				name = "latin1";
				return true;
			case 65001:
				name = encoding.GetPreamble().Length > 0? "utf-8-bom" : "utf-8";
				return true;
			case 1200:
				name = "utf-16le";
				return true;
			case 1201:
				name = "utf-16be";
				return true;
			default:
				name = null;
				return false;
		}
	}

	readonly static Lazy<Encoding> latin1 = new(() => Encoding.GetEncoding (28591));
	readonly static Lazy<Encoding> utf8NoBom = new(() => new UTF8Encoding (false));

	public static Encoding Latin1 => latin1.Value;
	public static Encoding Utf8NoBom => utf8NoBom.Value;
	public static Encoding Utf8WithBom => Encoding.UTF8;

	public static Encoding Utf16BigEndian => Encoding.BigEndianUnicode;
	public static Encoding Utf16LittleEndian => Encoding.Unicode;
}
