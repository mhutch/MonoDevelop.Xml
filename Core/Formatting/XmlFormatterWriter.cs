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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Formatting
{
	public partial class XmlFormatterWriter : XmlWriter
	{
		// Static/constant members.

		const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
		const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

		static readonly Encoding unmarked_utf8encoding = new UTF8Encoding (false, false);

		static readonly char[] escaped_attr_chars = new [] { '"', '&', '<', '>', '\r', '\n' };
		static readonly char[] escaped_text_chars_with_newlines = new[] { '&', '<', '>', '\r', '\n' };
		static readonly char[] escaped_text_chars_without_newlines = new[] { '&', '<', '>' };

		// Internal classes

		class XmlNodeInfo
		{
			public string? Prefix;
			public string? LocalName;
			public string? NS;
			public bool HasSimple;
			public bool HasElements;
			public string? XmlLang;
			public XmlSpace XmlSpace;
		}

		internal class StringUtil
		{
			static readonly CultureInfo cul = CultureInfo.InvariantCulture;
			static readonly CompareInfo cmp = CultureInfo.InvariantCulture.CompareInfo;

			public static int IndexOf (string src, string target) => cmp.IndexOf (src, target);

			public static int Compare (string s1, string s2) => cmp.Compare (s1, s2);

			public static string Format (string format, params object[] args) => string.Format (cul, format, args);
		}

		enum XmlDeclState {
			Allow,
			Ignore,
			Auto,
			Prohibit,
		}

		// Instance fields

		readonly TextWriter source; // the input TextWriter to .ctor().
		TextWriterWrapper writer;

		// It is used for storing xml:space, xml:lang and xmlns values.
		StringWriter? preserver;
		string preserved_name = "";
		bool is_preserved_xmlns;

		readonly bool allow_doc_fragment = true;
		readonly bool close_output_stream = true;
		readonly bool ignore_encoding;

		bool namespaces = true;
		XmlDeclState xmldecl_state = XmlDeclState.Allow;
		readonly bool check_character_validity = false;
		readonly NewLineHandling newline_handling = NewLineHandling.None;

		bool is_document_entity;
		WriteState state = WriteState.Start;
		XmlNodeType node_state = XmlNodeType.None;
		readonly XmlNamespaceManager nsmanager = new (new NameTable ());
		int open_count;
		XmlNodeInfo [] elements = new XmlNodeInfo [10];
		readonly Stack<string> new_local_namespaces = new ();
		readonly List<string> explicit_nsdecls = new ();

		string? newline;
		readonly bool v2;
		int lastEmptyLineCount;
		
		XmlFormattingSettings formatSettings = new ();
		XmlFormattingSettings defaultFormatSettings = new ();
		TextStylePolicy textPolicy = new ();

		// Constructors

		public XmlFormatterWriter (string filename, Encoding encoding)
			: this (new FileStream (filename, FileMode.Create, FileAccess.Write, FileShare.None), encoding)
		{
		}

		public XmlFormatterWriter (Stream stream, Encoding? encoding)
			: this (new StreamWriter (stream, encoding == null ? unmarked_utf8encoding : encoding))
		{
			ignore_encoding = (encoding == null);
		}

		public XmlFormatterWriter (TextWriter writer)
		{
			if (writer == null)
				throw new ArgumentNullException (nameof (writer));
			ignore_encoding = (writer.Encoding == null);

			if (writer is StreamWriter streamWriter)
				BaseStream = streamWriter.BaseStream;
			source = writer;

			this.writer = new TextWriterWrapper (writer, this);

			xmldecl_state = formatSettings.OmitXmlDeclaration ? XmlDeclState.Ignore : XmlDeclState.Allow;
			check_character_validity = false;
			v2 = true;
		}
		
		readonly Dictionary<XmlNode,XmlFormattingSettings> formatMap = new Dictionary<XmlNode, XmlFormattingSettings> ();


		public void WriteNode (XmlNode node, XmlFormattingPolicy formattingPolicy, TextStylePolicy textPolicy)
		{
			this.textPolicy = textPolicy;
			newline = this.textPolicy.GetEolMarker ();
			formatMap.Clear ();
			defaultFormatSettings = formattingPolicy.DefaultFormat;
			foreach (XmlFormattingSettings format in formattingPolicy.Formats) {
				foreach (string xpath in format.ScopeXPath) {
					if (node.SelectNodes (xpath) is XmlNodeList match) {
						foreach (XmlNode n in match)
							formatMap[n] = format;
					}
				}
			}
			WriteNode (node);
		}
		
		void WriteNode (XmlNode node)
		{
			XmlFormattingSettings oldFormat = formatSettings;
			SetFormat (node);
			
			switch (node.NodeType) {
				case XmlNodeType.Document: {
					if (!defaultFormatSettings.OmitXmlDeclaration)
						WriteDeclarationIfMissing ((XmlDocument)node);
					WriteContent (node);
					break;
				}
				case XmlNodeType.Attribute: {
					XmlAttribute at = (XmlAttribute) node;
					if (at.Specified) {
						WriteStartAttribute (at.NamespaceURI.Length > 0 ? at.Prefix : string.Empty, at.LocalName, at.NamespaceURI);
						WriteContent (node);
						WriteEndAttribute ();
					}
					break;
				}
				case XmlNodeType.CDATA: {
					WriteCData (((XmlCDataSection)node).Data);
					break;
				}
				case XmlNodeType.Comment: {
					WriteComment (((XmlComment)node).Data);
					break;
				}
				case XmlNodeType.DocumentFragment: {
					foreach (XmlNode child in node.ChildNodes)
						WriteNode (child);
					break;
				}
				case XmlNodeType.DocumentType: {
					XmlDocumentType dt = (XmlDocumentType) node;
					WriteDocType (dt.Name, dt.PublicId, dt.SystemId, dt.InternalSubset);
					break;
				}
				case XmlNodeType.Element: {
					XmlElement elem = (XmlElement) node;
					writer.AttributesIndent = -1;
					WriteStartElement (
						elem.NamespaceURI == null || elem.NamespaceURI.Length == 0 ? string.Empty : elem.Prefix,
						elem.LocalName,
						elem.NamespaceURI);
		
					if (elem.HasAttributes) {
						int oldBeforeSp = formatSettings.SpacesBeforeAssignment;
						int maxLen = 0;
						if (formatSettings.AlignAttributeValues) {
							foreach (XmlAttribute at in elem.Attributes) {
								string name = XmlFormatterWriter.GetAttributeName (at);
								if (name.Length > maxLen)
									maxLen = name.Length;
							}
						}
						foreach (XmlAttribute at in elem.Attributes) {
							if (formatSettings.AlignAttributeValues) {
								string name = XmlFormatterWriter.GetAttributeName (at);
								formatSettings.SpacesBeforeAssignment = (maxLen - name.Length) + oldBeforeSp;
							}
							WriteNode (at);
						}
						formatSettings.SpacesBeforeAssignment = oldBeforeSp;
					}
					
					if (!elem.IsEmpty) {
						CloseStartElement ();
						WriteContent (elem);
						WriteFullEndElement ();
					}
					else
						WriteEndElement ();
					break;
				}
				case XmlNodeType.EntityReference: {
					XmlEntityReference eref = (XmlEntityReference) node;
					WriteRaw ("&");
					WriteName (eref.Name);
					WriteRaw (";");
					break;
				}
				case XmlNodeType.ProcessingInstruction: {
					XmlProcessingInstruction pi = (XmlProcessingInstruction) node;
					WriteProcessingInstruction (pi.Target, pi.Data);
					break;
				}
				case XmlNodeType.SignificantWhitespace: {
					XmlSignificantWhitespace wn = (XmlSignificantWhitespace) node;
					WriteWhitespace (wn.Data);
					break;
				}
				case XmlNodeType.Text: {
					XmlText t = (XmlText) node;
					WriteString (t.Data);
					break;
				}
				case XmlNodeType.Whitespace: {
					XmlWhitespace wn = (XmlWhitespace) node;
					WriteWhitespace (wn.Data);
					break;
				}
				case XmlNodeType.XmlDeclaration: {
					if (!defaultFormatSettings.OmitXmlDeclaration) {
						XmlDeclaration dec = (XmlDeclaration) node;
						WriteRaw (string.Format ("<?xml {0}?>", dec.Value));
					}
					break;
				}
			}
			formatSettings = oldFormat;
		}

		static string GetAttributeName (XmlAttribute at)
		{
			if (at.NamespaceURI.Length > 0)
				return at.Prefix + ":" + at.LocalName;
			else
				return at.LocalName;
		}
		
		void WriteContent (XmlNode node)
		{
			for (XmlNode? n = node.FirstChild; n != null; n = n.NextSibling)
				WriteNode (n);
		}

		void WriteDeclarationIfMissing (XmlDocument doc)
		{
			var declaration = doc.FirstChild as XmlDeclaration;
			if (declaration == null) {
				declaration = doc.CreateXmlDeclaration ("1.0", "UTF-8", null);
				WriteNode (declaration);
			}
		}
		
		void SetFormat (XmlNode node)
		{
			if (formatMap.TryGetValue (node, out var s)) {
				formatSettings = s;
			}
			else if (node is XmlElement) {
				formatSettings = defaultFormatSettings;
			}
		}

		// 2.0 XmlWriterSettings support

		// As for ConformanceLevel, MS.NET is inconsistent with
		// MSDN documentation. For example, even if ConformanceLevel
		// is set as .Auto, multiple WriteStartDocument() calls
		// result in an error.
		// ms-help://MS.NETFramework.v20.en/wd_xml/html/7db8802b-53d8-4735-a637-4d2d2158d643.htm

		// Context Retriever

		public override string? XmlLang => open_count == 0 ? null : elements [open_count - 1].XmlLang;

		public override XmlSpace XmlSpace => open_count == 0 ? XmlSpace.None : elements [open_count - 1].XmlSpace;

		public override WriteState WriteState => state;

		public override string? LookupPrefix (string namespaceUri)
		{
			if (namespaceUri == null || namespaceUri == string.Empty)
				throw ArgumentError ("The Namespace cannot be empty.");

			if (namespaceUri == nsmanager.DefaultNamespace)
				return string.Empty;

			string? prefix = nsmanager.LookupPrefixExclusive (namespaceUri, false);

			// XmlNamespaceManager has changed to return null
			// when NSURI not found.
			// (Contradiction to the ECMA documentation.)
			return prefix;
		}

		// Stream Control

		public Stream? BaseStream { get; }

		public override void Close ()
		{
			if (state != WriteState.Error) {
				if (state == WriteState.Attribute)
					WriteEndAttribute ();
				while (open_count > 0)
					WriteEndElement ();
			}

			if (close_output_stream)
				writer.Close ();
			else
				writer.Flush ();
			state = WriteState.Closed;
		}

		public override void Flush ()
		{
			writer.Flush ();
		}

		// Misc Control
		public bool Namespaces {
			get { return namespaces; }
			set {
				if (state != WriteState.Start)
					throw InvalidOperation ("This property must be set before writing output.");
				namespaces = value;
			}
		}

		// XML Declaration

		public override void WriteStartDocument ()
		{
			WriteStartDocumentCore (false, false);
			is_document_entity = true;
		}

		public override void WriteStartDocument (bool standalone)
		{
			WriteStartDocumentCore (true, standalone);
			is_document_entity = true;
		}

		void WriteStartDocumentCore (bool outputStd, bool standalone)
		{
			if (state != WriteState.Start)
				throw StateError ("XmlDeclaration");

			switch (xmldecl_state) {
			case XmlDeclState.Ignore:
				return;
			case XmlDeclState.Prohibit:
				throw InvalidOperation ("WriteStartDocument cannot be called when ConformanceLevel is Fragment.");
			}

			state = WriteState.Prolog;

			writer.Write ("<?xml version=");
			writer.Write (formatSettings.QuoteChar);
			writer.Write ("1.0");
			writer.Write (formatSettings.QuoteChar);
			if (!ignore_encoding) {
				writer.Write (" encoding=");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (writer.Encoding.WebName);
				writer.Write (formatSettings.QuoteChar);
			}
			if (outputStd) {
				writer.Write (" standalone=");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (standalone ? "yes" : "no");
				writer.Write (formatSettings.QuoteChar);
			}
			writer.Write ("?>");

			xmldecl_state = XmlDeclState.Ignore;
		}

		public override void WriteEndDocument ()
		{
			switch (state) {
			case WriteState.Error:
			case WriteState.Closed:
			case WriteState.Start:
				throw StateError ("EndDocument");
			}

			if (state == WriteState.Attribute)
				WriteEndAttribute ();
			while (open_count > 0)
				WriteEndElement ();

			state = WriteState.Start;
			is_document_entity = false;
		}

		// DocType Declaration

		public override void WriteDocType (string name,
			string? pubid, string? sysid, string? subset)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (!XmlChar.IsName (name))
				throw ArgumentError ("name");

			if (node_state != XmlNodeType.None)
				throw StateError ("DocType");
			node_state = XmlNodeType.DocumentType;

			if (xmldecl_state == XmlDeclState.Auto)
				OutputAutoStartDocument ();

			WriteIndent ();

			writer.Write ("<!DOCTYPE ");
			writer.Write (name);
			if (pubid != null) {
				writer.Write (" PUBLIC ");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (pubid);
				writer.Write (formatSettings.QuoteChar);
				writer.Write (' ');
				writer.Write (formatSettings.QuoteChar);
				if (sysid != null)
					writer.Write (sysid);
				writer.Write (formatSettings.QuoteChar);
			}
			else if (sysid != null) {
				writer.Write (" SYSTEM ");
				writer.Write (formatSettings.QuoteChar);
				writer.Write (sysid);
				writer.Write (formatSettings.QuoteChar);
			}

			if (subset != null) {
				writer.Write ("[");
				// LAMESPEC: see the top of this source.
				writer.Write (subset);
				writer.Write ("]");
			}
			writer.Write ('>');

			state = WriteState.Prolog;
		}

		// StartElement

		public override void WriteStartElement (
			string? prefix, string localName, string? namespaceUri)
		{
			if (state == WriteState.Error || state == WriteState.Closed)
				throw StateError ("StartTag");
			node_state = XmlNodeType.Element;

			bool anonPrefix = (prefix == null);

			// Crazy namespace check goes here.
			//
			// 1. if Namespaces is false, then any significant 
			//    namespace indication is not allowed.
			// 2. if Prefix is non-empty and NamespaceURI is
			//    empty, it is an error in 1.x, or it is reset to
			//    an empty string in 2.0.
			// 3. null NamespaceURI indicates that namespace is
			//    not considered.
			// 4. prefix must not be equivalent to "XML" in
			//    case-insensitive comparison.
			if (!namespaces && !string.IsNullOrEmpty (namespaceUri))
				throw ArgumentError ("Namespace is disabled in this XmlTextWriter.");
			if (!namespaces && !string.IsNullOrEmpty (prefix))
				throw ArgumentError ("Namespace prefix is disabled in this XmlTextWriter.");

			// If namespace URI is empty, then either prefix
			// must be empty as well, or there is an
			// existing namespace mapping for the prefix.
			if (!string.IsNullOrEmpty (prefix) && namespaceUri == null) {
				namespaceUri = nsmanager.LookupNamespace (prefix, false);
				if (namespaceUri == null || namespaceUri.Length == 0)
					throw ArgumentError ("Namespace URI must not be null when prefix is not an empty string.");
			}
			// Considering the fact that WriteStartAttribute()
			// automatically changes argument namespaceURI, this
			// is kind of silly implementation. See bug #77094.
			if (namespaces &&
			    prefix != null && prefix.Length == 3 &&
			    namespaceUri != XmlNamespace &&
			    (prefix [0] == 'x' || prefix [0] == 'X') &&
			    (prefix [1] == 'm' || prefix [1] == 'M') &&
			    (prefix [2] == 'l' || prefix [2] == 'L'))
				throw new ArgumentException ("A prefix cannot be equivalent to \"xml\" in case-insensitive match.");


			if (xmldecl_state == XmlDeclState.Auto)
				OutputAutoStartDocument ();
			if (state == WriteState.Element)
				CloseStartElement ();
			if (open_count > 0)
				elements [open_count - 1].HasElements = true;

			nsmanager.PushScope ();

			if (namespaces && namespaceUri != null) {
				// If namespace URI is empty, then prefix must 
				// be empty as well.
				if (anonPrefix && namespaceUri.Length > 0)
					prefix = LookupPrefix (namespaceUri);
				if (prefix == null || namespaceUri.Length == 0)
					prefix = null;
			}
			
			WriteEmptyLines (formatSettings.EmptyLinesBeforeStart);
			ResetEmptyLineCount ();
			WriteIndent ();

			writer.Write ("<");

			if (!string.IsNullOrEmpty (prefix)) {
				writer.Write (prefix);
				writer.Write (':');
			}
			writer.Write (localName);

			if (elements.Length == open_count) {
				var tmp = new XmlNodeInfo [open_count << 1];
				Array.Copy (elements, tmp, open_count);
				elements = tmp;
			}
			if (elements [open_count] == null)
				elements [open_count] =
					new XmlNodeInfo ();
			XmlNodeInfo info = elements [open_count];
			info.Prefix = prefix;
			info.LocalName = localName;
			info.NS = namespaceUri;
			info.HasSimple = false;
			info.HasElements = false;
			info.XmlLang = XmlLang;
			info.XmlSpace = XmlSpace;
			open_count++;

			if (namespaces && namespaceUri != null) {
				string? oldns = nsmanager.LookupNamespace (prefix, false);
				if (oldns != namespaceUri && prefix is not null) {
					nsmanager.AddNamespace (prefix, namespaceUri);
					new_local_namespaces.Push (prefix);
				}
			}

			state = WriteState.Element;
		}
		
		void WriteEmptyLines (int count)
		{
			if (count > lastEmptyLineCount) {
				for (int n=0; n<count - lastEmptyLineCount; n++)
					writer.Write (newline);
				lastEmptyLineCount = count;
			}
		}
		
		void ResetEmptyLineCount ()
		{
			lastEmptyLineCount = 0;
		}
		
		void WriteAssignment ()
		{
			for (int n=0; n < formatSettings.SpacesBeforeAssignment; n++)
				writer.Write (' ');
			writer.Write ('=');
			for (int n=0; n < formatSettings.SpacesAfterAssignment; n++)
				writer.Write (' ');
		}

		void CloseStartElement ()
		{
			CloseStartElementCore ();

			if (state == WriteState.Element) {
				writer.Write ('>');
				WriteEmptyLines (formatSettings.EmptyLinesAfterStart);
			}
			state = WriteState.Content;
		}

		void CloseStartElementCore ()
		{
			ResetEmptyLineCount ();
			if (state == WriteState.Attribute)
				WriteEndAttribute ();

			if (new_local_namespaces.Count == 0) {
				if (explicit_nsdecls.Count > 0)
					explicit_nsdecls.Clear ();
				return;
			}

			// Missing xmlns attributes are added to 
			// explicit_nsdecls (it is cleared but this way
			// I save another array creation).
			int idx = explicit_nsdecls.Count;
			while (new_local_namespaces.Count > 0) {
				string p = (string) new_local_namespaces.Pop ();
				bool match = false;
				for (int i = 0; i < explicit_nsdecls.Count; i++) {
					if ((string) explicit_nsdecls [i] == p) {
						match = true;
						break;
					}
				}
				if (match)
					continue;
				explicit_nsdecls.Add (p);
			}

			for (int i = idx; i < explicit_nsdecls.Count; i++) {
				string prefix = (string) explicit_nsdecls [i];
				string? ns = nsmanager.LookupNamespace (prefix, false);
				if (ns == null)
					continue; // superceded
				if (prefix.Length > 0) {
					writer.Write (" xmlns:");
					writer.Write (prefix);
				} else {
					writer.Write (" xmlns");
				}
				WriteAssignment ();
				writer.Write (formatSettings.QuoteChar);
				WriteEscapedString (ns, true);
				writer.Write (formatSettings.QuoteChar);
			}
			explicit_nsdecls.Clear ();
		}

		// EndElement

		public override void WriteEndElement ()
		{
			WriteEndElementCore (false);
		}

		public override void WriteFullEndElement ()
		{
			WriteEndElementCore (true);
		}

		void WriteEndElementCore (bool full)
		{
			if (state == WriteState.Error || state == WriteState.Closed)
				throw StateError ("EndElement");
			if (open_count == 0)
				throw InvalidOperation ("There is no more open element.");

			// bool isEmpty = state != WriteState.Content;

			CloseStartElementCore ();

			nsmanager.PopScope ();

			if (state == WriteState.Element) {
				if (full) {
					writer.Write ('>');
					WriteEmptyLines (formatSettings.EmptyLinesAfterStart);
					WriteEmptyLines (formatSettings.EmptyLinesBeforeEnd);
				}
				else {
					writer.Write (" />");
					WriteEmptyLines (formatSettings.EmptyLinesAfterEnd);
				}
			}

			if (full || state == WriteState.Content) {
				WriteEmptyLines (formatSettings.EmptyLinesBeforeEnd);
				WriteIndentEndElement ();
			}

			XmlNodeInfo info = elements [--open_count];

			if (full || state == WriteState.Content) {
				writer.Write ("</");
				if (!string.IsNullOrEmpty (info.Prefix)) {
					writer.Write (info.Prefix);
					writer.Write (':');
				}
				writer.Write (info.LocalName);
				writer.Write ('>');
				ResetEmptyLineCount ();
				WriteEmptyLines (formatSettings.EmptyLinesAfterEnd);
			}

			state = WriteState.Content;
			if (open_count == 0)
				node_state = XmlNodeType.EndElement;
		}

		// Attribute

		public override void WriteStartAttribute (
			string? prefix, string localName, string? namespaceUri)
		{
			// LAMESPEC: this violates the expected behavior of
			// this method, as it incorrectly allows unbalanced
			// output of attributes. Microfot changes description
			// on its behavior at their will, regardless of
			// ECMA description.
			if (state == WriteState.Attribute)
				WriteEndAttribute ();

			if (state != WriteState.Element && state != WriteState.Start)
				throw StateError ("Attribute");

			if (prefix is null)
				prefix = string.Empty;

			// For xmlns URI, prefix is forced to be "xmlns"
			bool isNSDecl = false;
			if (namespaceUri == XmlnsNamespace) {
				isNSDecl = true;
				if (prefix.Length == 0 && localName != "xmlns")
					prefix = "xmlns";
			}
			else
				isNSDecl = (prefix == "xmlns" ||
					localName == "xmlns" && prefix.Length == 0);

			if (namespaces) {
				// MS implementation is pretty hacky here. 
				// Regardless of namespace URI it is regarded
				// as NS URI for "xml".
				if (prefix == "xml")
					namespaceUri = XmlNamespace;
				// infer namespace URI.
				else if (namespaceUri is null) {
					if (isNSDecl)
						namespaceUri = XmlnsNamespace;
					else
						namespaceUri = string.Empty;
				}

				// It is silly design - null namespace with
				// "xmlns" are allowed (for namespace-less
				// output; while there is Namespaces property)
				// On the other hand, namespace "" is not 
				// allowed.
				if (isNSDecl && namespaceUri != XmlnsNamespace)
					throw ArgumentError (string.Format ("The 'xmlns' attribute is bound to the reserved namespace '{0}'", XmlnsNamespace));

				// If namespace URI is empty, then either prefix
				// must be empty as well, or there is an
				// existing namespace mapping for the prefix.
				if (prefix.Length > 0 && namespaceUri.Length == 0) {
					namespaceUri = nsmanager.LookupNamespace (prefix, false);
					if (namespaceUri == null || namespaceUri.Length == 0)
						throw ArgumentError ("Namespace URI must not be null when prefix is not an empty string.");
				}

				// Dive into extremely complex procedure.
				if (!isNSDecl && namespaceUri.Length > 0)
					prefix = DetermineAttributePrefix (
						prefix, localName, namespaceUri);
			}

			writer.AttributesPerLine++;
			if (formatSettings.WrapAttributes && writer.AttributesPerLine > 1)
				writer.MarkBlockStart ();
			
			if (formatSettings.AttributesInNewLine || writer.AttributesPerLine > formatSettings.MaxAttributesPerLine) {
				writer.MarkBlockEnd ();
				WriteIndentAttribute ();
				writer.AttributesPerLine = 1;
			}
			else if (state != WriteState.Start)
				writer.Write (' ');

			if (writer.AttributesIndent == -1)
				writer.AttributesIndent = writer.Column;

			if (prefix.Length > 0) {
				writer.Write (prefix);
				writer.Write (':');
			}
			writer.Write (localName);
			WriteAssignment ();
			writer.Write (formatSettings.QuoteChar);

			if (isNSDecl || prefix == "xml") {
				if (preserver == null)
					preserver = new StringWriter ();
				else
					preserver.GetStringBuilder ().Length = 0;
				writer = new TextWriterWrapper (preserver, this, writer);

				if (!isNSDecl) {
					is_preserved_xmlns = false;
					preserved_name = localName;
				} else {
					is_preserved_xmlns = true;
					preserved_name = localName == "xmlns" ? string.Empty : localName;
				}
			}

			state = WriteState.Attribute;
		}

		// See also:
		// "DetermineAttributePrefix(): local mapping overwrite"
		string DetermineAttributePrefix (string prefix, string local, string ns)
		{
			bool mockup = false;
			if (prefix.Length == 0) {
				if (LookupPrefix (ns) is string foundPrefix && foundPrefix.Length > 0) {
					return foundPrefix;
				}
				mockup = true;
			}
			else {
				prefix = nsmanager.NameTable.Add (prefix);
				string? existing = nsmanager.LookupNamespace (prefix, true);
				if (existing == ns)
					return prefix;
				if (existing is not null) {
					// See code comment on the head of
					// this source file.
					nsmanager.RemoveNamespace (prefix, existing);
					if (nsmanager.LookupNamespace (prefix, true) != existing) {
						nsmanager.AddNamespace (prefix, existing);
						mockup = true;
					}
				}
			}
			if (mockup) {
				prefix = MockupPrefix (ns, true);
			}
			new_local_namespaces.Push (prefix);
			nsmanager.AddNamespace (prefix, ns);

			return prefix;
		}

		string MockupPrefix (string ns, bool skipLookup)
		{
			string? prefix = skipLookup ? null : LookupPrefix (ns);
			if (prefix != null && prefix.Length > 0)
				return prefix;
			for (int p = 1; ; p++) {
				prefix = StringUtil.Format ("d{0}p{1}", open_count, p);
				if (new_local_namespaces.Contains (prefix))
					continue;
				var existingNS = nsmanager.NameTable.Get (prefix);
				if (existingNS is not null && nsmanager.LookupNamespace (existingNS) is not null)
					continue;
				nsmanager.AddNamespace (prefix, ns);
				new_local_namespaces.Push (prefix);
				return prefix;
			}
		}

		public override void WriteEndAttribute ()
		{
			if (state != WriteState.Attribute)
				throw StateError ("End of attribute");

			if (writer.Wrapped == preserver) {
				writer = writer.PreviousWrapper ?? new TextWriterWrapper (source, this);
				string value = preserver.ToString ();
				if (is_preserved_xmlns) {
					if (preserved_name.Length > 0 &&
					    value.Length == 0)
						throw ArgumentError ("Non-empty prefix must be mapped to non-empty namespace URI.");
					string? existing = nsmanager.LookupNamespace (preserved_name, false);
					explicit_nsdecls.Add (preserved_name);
					if (open_count > 0) {

						if (v2 &&
						    elements [open_count - 1].Prefix == preserved_name &&
						    elements [open_count - 1].NS != value)
							throw new XmlException (string.Format ("Cannot redefine the namespace for prefix '{0}' used at current element", preserved_name));

						if (elements [open_count - 1].NS != string.Empty ||
						    elements [open_count - 1].Prefix != preserved_name) {
							if (existing != value)
								nsmanager.AddNamespace (preserved_name, value);
						}
					}
				} else {
					switch (preserved_name) {
					case "lang":
						if (open_count > 0)
							elements [open_count - 1].XmlLang = value;
						break;
					case "space":
						switch (value) {
						case "default":
							if (open_count > 0)
								elements [open_count - 1].XmlSpace = XmlSpace.Default;
							break;
						case "preserve":
							if (open_count > 0)
								elements [open_count - 1].XmlSpace = XmlSpace.Preserve;
							break;
						default:
							throw ArgumentError ("Invalid value for xml:space.");
						}
						break;
					}
				}
				writer.Write (value);
			}

			writer.Write (formatSettings.QuoteChar);
			
			if (writer.InBlock) {
				writer.MarkBlockEnd ();
				if (writer.Column > textPolicy.FileWidth) {
					WriteIndentAttribute ();
					writer.WriteBlock (true);
					writer.AttributesPerLine++;
				} else {
					writer.WriteBlock (false);
				}
			}
			
			state = WriteState.Element;
		}

		// Non-Text Content

		public override void WriteComment (string? text)
		{
			if (text == null)
				throw ArgumentError ("text");

			if (text.Length > 0 && text [text.Length - 1] == '-')
				throw ArgumentError ("An input string to WriteComment method must not end with '-'. Escape it with '&#2D;'.");
			if (StringUtil.IndexOf (text, "--") > 0)
				throw ArgumentError ("An XML comment cannot end with \"-\".");

			if (state == WriteState.Attribute || state == WriteState.Element)
				CloseStartElement ();

			WriteIndent ();

			ShiftStateTopLevel ("Comment", false, false, false);

			writer.Write ("<!--");
			writer.Write (text);
			writer.Write ("-->");
			ResetEmptyLineCount ();
		}

		// LAMESPEC: see comments on the top of this source.
		public override void WriteProcessingInstruction (string? name, string? text)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (text == null)
				throw ArgumentError ("text");

			WriteIndent ();

			if (!XmlChar.IsName (name))
				throw ArgumentError ("A processing instruction name must be a valid XML name.");

			if (StringUtil.IndexOf (text, "?>") > 0)
				throw ArgumentError ("Processing instruction cannot contain \"?>\" as its value.");

			ShiftStateTopLevel ("ProcessingInstruction", false, name == "xml", false);

			writer.Write ("<?");
			writer.Write (name);
			writer.Write (' ');
			writer.Write (text);
			writer.Write ("?>");

			if (state == WriteState.Start)
				state = WriteState.Prolog;
			ResetEmptyLineCount ();
		}

		// Text Content

		public override void WriteWhitespace (string? text)
		{
			if (text == null)
				throw ArgumentError ("text");

			// huh? Shouldn't it accept an empty string???
			if (text.Length == 0 ||
			    XmlChar.IndexOfNonWhitespace (text) >= 0)
				throw ArgumentError ("WriteWhitespace method accepts only whitespaces.");

			ShiftStateTopLevel ("Whitespace", true, false, true);

			writer.Write (text);
			ResetEmptyLineCount ();
		}

		public override void WriteCData (string? text)
		{
			if (text == null)
				text = string.Empty;
			ShiftStateContent ("CData", false);

			if (StringUtil.IndexOf (text, "]]>") >= 0)
				throw ArgumentError ("CDATA section must not contain ']]>'.");
			writer.Write ("<![CDATA[");
			WriteCheckedString (text);
			writer.Write ("]]>");
			ResetEmptyLineCount ();
		}

		public override void WriteString (string? text)
		{
			if (text == null || text.Length == 0)
				return; // do nothing, including state transition.
			ShiftStateContent ("Text", true);

			WriteEscapedString (text, state == WriteState.Attribute);
		}

		public override void WriteRaw (string raw)
		{
			if (raw == null)
				return; // do nothing, including state transition.

			//WriteIndent ();

			// LAMESPEC: It rejects XMLDecl while it allows
			// DocType which could consist of non well-formed XML.
			ShiftStateTopLevel ("Raw string", true, true, true);

			writer.Write (raw);
		}

		public override void WriteCharEntity (char ch)
		{
			WriteCharacterEntity (ch, '\0', false);
		}

		public override void WriteSurrogateCharEntity (char low, char high)
		{
			WriteCharacterEntity (low, high, true);
		}

		void WriteCharacterEntity (char ch, char high, bool surrogate)
		{
			if (surrogate &&
			    ('\uD800' > high || high > '\uDC00' ||
			     '\uDC00' > ch || ch > '\uDFFF'))
				throw ArgumentError (string.Format ("Invalid surrogate pair was found. Low: &#x{0:X}; High: &#x{1:X};", (int) ch, (int) high));
			else if (check_character_validity && XmlChar.IsInvalid (ch))
				throw ArgumentError ($"Invalid character &#x{(int)ch:X};");

			ShiftStateContent ("Character", true);

			int v = surrogate ? (high - 0xD800) * 0x400 + ch - 0xDC00 + 0x10000 : (int) ch;
			writer.Write ("&#x");
			writer.Write (v.ToString ("X", CultureInfo.InvariantCulture));
			writer.Write (';');
		}

		public override void WriteEntityRef (string name)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (!XmlChar.IsName (name))
				throw ArgumentError ("Argument name must be a valid XML name.");

			ShiftStateContent ("Entity reference", true);

			writer.Write ('&');
			writer.Write (name);
			writer.Write (';');
		}

		// Applied methods

		public override void WriteName (string name)
		{
			if (name == null)
				throw ArgumentError ("name");
			if (!XmlChar.IsName (name))
				throw ArgumentError ("Not a valid name string.");
			WriteString (name);
		}

		public override void WriteNmToken (string nmtoken)
		{
			if (nmtoken == null)
				throw ArgumentError ("nmtoken");
			if (!XmlChar.IsNmToken (nmtoken))
				throw ArgumentError ("Not a valid NMTOKEN string.");
			WriteString (nmtoken);
		}

		public override void WriteQualifiedName (
			string localName, string? ns)
		{
			if (localName == null)
				throw ArgumentError ("localName");
			if (ns == null)
				ns = string.Empty;

			if (ns == XmlnsNamespace)
				throw ArgumentError ("Prefix 'xmlns' is reserved and cannot be overriden.");
			if (!XmlChar.IsNCName (localName))
				throw ArgumentError ("localName must be a valid NCName.");

			ShiftStateContent ("QName", true);

			string? prefix = string.IsNullOrEmpty (ns)? "" : LookupPrefix (ns);
			if (prefix is null) {
				if (state == WriteState.Attribute)
					prefix = MockupPrefix (ns, false);
				else
					throw ArgumentError ($"Namespace '{ns}' is not declared.");
			}

			if (prefix != "") {
				writer.Write (prefix);
				writer.Write (":");
			}
			writer.Write (localName);
		}

		// Chunk data

		void CheckChunkRange (Array buffer, int index, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (index < 0 || buffer.Length < index)
				throw ArgumentOutOfRangeError ("index");
			if (count < 0 || buffer.Length < index + count)
				throw ArgumentOutOfRangeError ("count");
		}

		public override void WriteBase64 (byte [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			WriteString (Convert.ToBase64String (buffer, index, count));
		}

		public override void WriteBinHex (byte [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			ShiftStateContent ("BinHex", true);

			WriteBinHex (buffer, index, count, writer);
		}

		internal static void WriteBinHex (byte [] buffer, int index, int count, TextWriter w)
		{
			if (buffer == null)
				throw new ArgumentNullException (nameof (buffer));
			if (index < 0) {
				throw new ArgumentOutOfRangeException (
					nameof (index), index,
					"index must be non negative integer.");
			}
			if (count < 0) {
				throw new ArgumentOutOfRangeException (
					nameof (count), count,
					"count must be non negative integer.");
			}
			if (buffer.Length < index + count)
				throw new ArgumentOutOfRangeException ("index and count must be smaller than the length of the buffer.");

			// Copied from XmlTextWriter.WriteBinHex ()
			int end = index + count;
			for (int i = index; i < end; i++) {
				int val = buffer [i];
				int high = val >> 4;
				int low = val & 15;
				if (high > 9)
					w.Write ((char) (high + 55));
				else
					w.Write ((char) (high + 0x30));
				if (low > 9)
					w.Write ((char) (low + 55));
				else
					w.Write ((char) (low + 0x30));
			}
		}

		public override void WriteChars (char [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			ShiftStateContent ("Chars", true);

			WriteEscapedBuffer (buffer, index, count,
				state == WriteState.Attribute);
		}

		public override void WriteRaw (char [] buffer, int index, int count)
		{
			CheckChunkRange (buffer, index, count);

			ShiftStateContent ("Raw text", false);

			writer.Write (buffer, index, count);
		}

		// Utilities

		void WriteIndent ()
		{
			WriteIndentCore (0, false);
		}

		void WriteIndentEndElement ()
		{
			WriteIndentCore (-1, false);
		}

		void WriteIndentAttribute ()
		{
			if (formatSettings.AlignAttributes && writer.AttributesIndent != -1) {
				if (state != WriteState.Start)
					writer.Write (newline);
				if (textPolicy.TabsToSpaces)
					writer.Write (new string (' ', writer.AttributesIndent));
				else
					writer.Write (new string ('\t', writer.AttributesIndent / textPolicy.TabWidth) + new string (' ', writer.AttributesIndent % textPolicy.TabWidth));
			} else {
				if (!WriteIndentCore (0, true))
					writer.Write (' '); // space is required instead.
			}
		}

		bool WriteIndentCore (int nestFix, bool attribute)
		{
			if (!formatSettings.IndentContent)
				return false;
			for (int i = open_count - 1; i >= 0; i--)
				if (!attribute && elements [i].HasSimple)
					return false;

			if (state != WriteState.Start)
				writer.Write (newline);
			writer.Write (textPolicy.TabsToSpaces ? new string (' ', (open_count + nestFix) * textPolicy.TabWidth) : new string ('\t', open_count + nestFix));
			return true;
		}

		void OutputAutoStartDocument ()
		{
			if (state != WriteState.Start)
				return;
			WriteStartDocumentCore (false, false);
		}

		void ShiftStateTopLevel (string occurred, bool allowAttribute, bool dontCheckXmlDecl, bool isCharacter)
		{
			switch (state) {
			case WriteState.Error:
			case WriteState.Closed:
				throw StateError (occurred);
			case WriteState.Start:
				if (isCharacter)
					CheckMixedContentState ();
				if (xmldecl_state == XmlDeclState.Auto && !dontCheckXmlDecl)
					OutputAutoStartDocument ();
				state = WriteState.Prolog;
				break;
			case WriteState.Attribute:
				if (allowAttribute)
					break;
				goto case WriteState.Closed;
			case WriteState.Element:
				if (isCharacter)
					CheckMixedContentState ();
				CloseStartElement ();
				break;
			case WriteState.Content:
				if (isCharacter)
					CheckMixedContentState ();
				break;
			}

		}

		void CheckMixedContentState ()
		{
//			if (open_count > 0 &&
//			    state != WriteState.Attribute)
//				elements [open_count - 1].HasSimple = true;
			if (open_count > 0)
				elements [open_count - 1].HasSimple = true;
		}

		void ShiftStateContent (string occurred, bool allowAttribute)
		{
			switch (state) {
			case WriteState.Error:
			case WriteState.Closed:
					throw StateError (occurred);
			case WriteState.Prolog:
			case WriteState.Start:
				if (!allow_doc_fragment || is_document_entity)
					goto case WriteState.Closed;
				if (xmldecl_state == XmlDeclState.Auto)
					OutputAutoStartDocument ();
				CheckMixedContentState ();
				state = WriteState.Content;
				break;
			case WriteState.Attribute:
				if (allowAttribute)
					break;
				goto case WriteState.Closed;
			case WriteState.Element:
				CloseStartElement ();
				CheckMixedContentState ();
				break;
			case WriteState.Content:
				CheckMixedContentState ();
				break;
			}
		}

		void WriteEscapedString (string text, bool isAttribute)
		{
			escaped_attr_chars [0] = formatSettings.QuoteChar;
			char [] escaped = isAttribute ?
				escaped_attr_chars :
				(newline_handling != NewLineHandling.None
					? escaped_text_chars_with_newlines
					: escaped_text_chars_without_newlines);

			int idx = text.IndexOfAny (escaped);
			if (idx >= 0) {
				char [] arr = text.ToCharArray ();
				WriteCheckedBuffer (arr, 0, idx);
				WriteEscapedBuffer (
					arr, idx, arr.Length - idx, isAttribute);
			} else {
				WriteCheckedString (text);
			}
		}

		void WriteCheckedString (string s)
		{
			int i = XmlChar.IndexOfInvalid (s, true);
			if (i >= 0) {
				char [] arr = s.ToCharArray ();
				writer.Write (arr, 0, i);
				WriteCheckedBuffer (arr, i, arr.Length - i);
			} else {
				// no invalid character.
				writer.Write (s);
			}
		}

		void WriteCheckedBuffer (char [] text, int idx, int length)
		{
			int start = idx;
			int end = idx + length;
			while ((idx = XmlChar.IndexOfInvalid (text, start, length, true)) >= 0) {
				if (check_character_validity) // actually this is one time pass.
					throw ArgumentError ($"Input contains invalid character at {idx} : &#x{(int)text[idx]:X};");
				if (start < idx)
					writer.Write (text, start, idx - start);
				writer.Write ("&#x");
				writer.Write (((int) text [idx]).ToString (
					"X",
					CultureInfo.InvariantCulture));
				writer.Write (';');
				length -= idx - start + 1;
				start = idx + 1;
			}
			if (start < end)
				writer.Write (text, start, end - start);
		}

		void WriteEscapedBuffer (char [] text, int index, int length,
			bool isAttribute)
		{
			int start = index;
			int end = index + length;
			for (int i = start; i < end; i++) {
				switch (text [i]) {
				default:
					continue;
				case '&':
				case '<':
				case '>':
					if (start < i)
						WriteCheckedBuffer (text, start, i - start);
					writer.Write ('&');
					switch (text [i]) {
					case '&': writer.Write ("amp;"); break;
					case '<': writer.Write ("lt;"); break;
					case '>': writer.Write ("gt;"); break;
					case '\'': writer.Write ("apos;"); break;
					case '"': writer.Write ("quot;"); break;
					}
					break;
				case '"':
				case '\'':
					if (isAttribute && text [i] == formatSettings.QuoteChar)
						goto case '&';
					continue;
				case '\r':
					if (i + 1 < end && text [i] == '\n')
						i++; // CRLF
					goto case '\n';
				case '\n':
					if (start < i)
						WriteCheckedBuffer (text, start, i - start);
					if (isAttribute) {
						writer.Write (text [i] == '\r' ?
							"&#xD;" : "&#xA;");
						break;
					}
					switch (newline_handling) {
					case NewLineHandling.Entitize:
						writer.Write (text [i] == '\r' ?
							"&#xD;" : "&#xA;");
						break;
					case NewLineHandling.Replace:
						writer.Write (newline);
						break;
					default:
						writer.Write (text [i]);
						break;
					}
					break;
				}
				start = i + 1;
			}
			if (start < end)
				WriteCheckedBuffer (text, start, end - start);
		}

		// Exceptions

		Exception ArgumentOutOfRangeError (string name)
		{
			state = WriteState.Error;
			return new ArgumentOutOfRangeException (name);
		}

		Exception ArgumentError (string msg)
		{
			state = WriteState.Error;
			return new ArgumentException (msg);
		}

		Exception InvalidOperation (string msg)
		{
			state = WriteState.Error;
			return new InvalidOperationException (msg);
		}

		Exception StateError (string occurred)
		{
			return InvalidOperation ($"This XmlWriter does not accept {occurred} at this state {state}.");
		}
	}
}
