//
// XmlTextWriter.cs
//
// Author:
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2006 Novell, Inc.

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using System.Text;

namespace MonoDevelop.Xml.Formatting;

partial class XmlFormatterWriter
{
	class TextWriterWrapper : TextWriter
	{
		public TextWriter Wrapped { get; }
		public readonly TextWriterWrapper? PreviousWrapper;
		readonly XmlFormatterWriter formatter;
		readonly StringBuilder sb = new ();

		public int Column;
		public int AttributesPerLine;
		public int AttributesIndent;

		public TextWriterWrapper (TextWriter wrapped, XmlFormatterWriter formatter)
		{
			Wrapped = wrapped;
			this.formatter = formatter;
		}

		public TextWriterWrapper (TextWriter wrapped, XmlFormatterWriter formatter, TextWriterWrapper currentWriter)
			: this (wrapped, formatter)
		{
			PreviousWrapper = currentWriter;
		}

		public void MarkBlockStart ()
		{
			InBlock = true;
		}
		
		public void MarkBlockEnd ()
		{
			InBlock = false;
		}
		
		public void WriteBlock (bool wrappedLine)
		{
			if (wrappedLine)
				Write (sb.ToString ());
			else
				Wrapped.Write (sb.ToString ());
			sb.Length = 0;
		}
		
		public bool InBlock { get; private set; }
		
		public override Encoding Encoding => Wrapped.Encoding;
		
		public override void Write (char c)
		{
			if (InBlock)
				sb.Append (c);
			else
				Wrapped.Write (c);
			
			if (c == '\n') {
				AttributesPerLine = 0;
				Column = 0;
			}
			else {
				if (c == '\t')
					Column += formatter.textPolicy.TabWidth;
				else
					Column++;
			}
		}
	}
}
