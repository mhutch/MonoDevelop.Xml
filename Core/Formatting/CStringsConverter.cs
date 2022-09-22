// 
// XmlFormattingPolicy.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Text;
using System.ComponentModel;
using System.Globalization;

namespace MonoDevelop.Xml.Formatting
{
	class CStringsConverter : TypeConverter
	{
		public override bool CanConvertFrom (ITypeDescriptorContext? context, Type? sourceType) => sourceType == typeof (string);
	
		public override bool CanConvertTo (ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof (string);
	
		public override object? ConvertFrom (ITypeDescriptorContext? context, CultureInfo? culture, object? value) => value is null? null : UnescapeString ((string)value);
	
		public override object? ConvertTo (ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) => value is null ? null : EscapeString ((string)value);
	
		public static string EscapeString (string text)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				string txt;
				switch (c) {
					case '"': txt = "\\\""; break;
					case '\0': txt = @"\0"; break;
					case '\\': txt = @"\\"; break;
					case '\a': txt = @"\a"; break;
					case '\b': txt = @"\b"; break;
					case '\f': txt = @"\f"; break;
					case '\v': txt = @"\v"; break;
					case '\n': txt = @"\n"; break;
					case '\r': txt = @"\r"; break;
					case '\t': txt = @"\t"; break;
					default:
						sb.Append (c);
						continue;
				}
				sb.Append (txt);
			}
			return sb.ToString ();
		}
		
		public static string UnescapeString (string text)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				if (c == '\\') {
					if (++i >= text.Length)
						break;
					c = text [i];
					char txt;
					switch (c) {
						case '"': txt = '"'; break;
						case '0': txt = '\0'; break;
						case '\\': txt = '\\'; break;
						case 'a': txt = '\a'; break;
						case 'b': txt = '\b'; break;
						case 'f': txt = '\f'; break;
						case 'v': txt = '\v'; break;
						case 'n': txt = '\n'; break;
						case 'r': txt = '\r'; break;
						case 't': txt = '\t'; break;
						default: txt = c; break;
					}
					sb.Append (txt);
				} else
					sb.Append (c);
			}
			return sb.ToString ();
		}
	}
}
