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
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace MonoDevelop.Xml.Formatting;

partial class XmlFormatterWriter
{
	internal class XmlNamespaceManager : IXmlNamespaceResolver, IEnumerable
	{
		#region Data
		struct NsDecl {
			public string Prefix;
			public string? Uri;
		}
		
		struct NsScope {
			public int DeclCount;
			public string? DefaultNamespace;
		}
		
		NsDecl [] decls;
		int declPos = -1;
		
		NsScope [] scopes;
		int scopePos = -1;
		
		string? defaultNamespace;
		int count;
		
		// precondition declPos == nsDecl.Length
		void GrowDecls ()
		{
			NsDecl [] old = decls;
			decls = new NsDecl [declPos * 2 + 1];
			if (declPos > 0)
				Array.Copy (old, 0, decls, 0, declPos);
		}
		
		// precondition scopePos == scopes.Length
		void GrowScopes ()
		{
			NsScope [] old = scopes;
			scopes = new NsScope [scopePos * 2 + 1];
			if (scopePos > 0)
				Array.Copy (old, 0, scopes, 0, scopePos);
		}
		
		#endregion
		
		#region Fields

		readonly XmlNameTable nameTable;
		internal const string XmlnsXml = "http://www.w3.org/XML/1998/namespace";
		internal const string XmlnsXmlns = "http://www.w3.org/2000/xmlns/";
		internal const string PrefixXml = "xml";
		internal const string PrefixXmlns = "xmlns";

		#endregion

		#region Constructor

		public XmlNamespaceManager (XmlNameTable nameTable)
		{
			if (nameTable == null)
				throw new ArgumentNullException (nameof (nameTable));
			this.nameTable = nameTable;

			nameTable.Add (PrefixXmlns);
			nameTable.Add (PrefixXml);
			nameTable.Add (string.Empty);
			nameTable.Add (XmlnsXmlns);
			nameTable.Add (XmlnsXml);

			decls = new NsDecl[10];
			scopes = new NsScope[40];
		}

		#endregion

		#region Properties

		public virtual string DefaultNamespace => defaultNamespace == null ? string.Empty : defaultNamespace;

		public virtual XmlNameTable NameTable => nameTable;

		#endregion

		#region Methods

		public virtual void AddNamespace (string prefix, string uri) => AddNamespace (prefix, uri, false);

		internal virtual void AddNamespace (string prefix, string uri, bool atomizedNames)
		{
			if (prefix == null)
				throw new ArgumentNullException (nameof (prefix), "Value cannot be null.");

			if (uri == null)
				throw new ArgumentNullException (nameof (uri), "Value cannot be null.");
			if (!atomizedNames) {
				prefix = nameTable.Add (prefix);
				uri = nameTable.Add (uri);
			}

			if (prefix == PrefixXml && uri == XmlnsXml)
				return;

			IsValidDeclaration (prefix, uri, true);

			if (prefix.Length == 0)
				defaultNamespace = uri;
			
			for (int i = declPos; i > declPos - count; i--) {
				if (ReferenceEquals (decls [i].Prefix, prefix)) {
					decls [i].Uri = uri;
					return;
				}
			}
			
			declPos ++;
			count ++;
			
			if (declPos == decls.Length)
				GrowDecls ();
			decls [declPos].Prefix = prefix;
			decls [declPos].Uri = uri;
		}

		static string? IsValidDeclaration (string prefix, string uri, bool throwException)
		{
			string? message = null;
			// It is funky, but it does not check whether prefix
			// is equivalent to "xml" in case-insensitive means.
			if (prefix == PrefixXml && uri != XmlnsXml)
				message = $"Prefix \"xml\" can only be bound to the fixed namespace URI \"{XmlnsXml}\". \"{uri}\" is invalid.";
			else if (prefix == "xmlns")
				message = "Declaring prefix named \"xmlns\" is not allowed to any namespace.";
			else if (uri == XmlnsXmlns)
				message = $"Namespace URI \"{XmlnsXmlns}\" cannot be declared with any namespace.";

			if (message != null && throwException)
				throw new ArgumentException (message);
			else
				return message;
		}

		public virtual IEnumerator GetEnumerator ()
		{
			// In fact it returns such table's enumerator that contains all the namespaces.
			// while HasNamespace() ignores pushed namespaces.
			
			Hashtable ht = new Hashtable ();
			for (int i = 0; i <= declPos; i++) {
				if (decls [i].Prefix != string.Empty && decls [i].Uri != null) {
					ht [decls [i].Prefix] = decls [i].Uri;
				}
			}
			
			ht [string.Empty] = DefaultNamespace;
			ht [PrefixXml] = XmlnsXml;
			ht [PrefixXmlns] = XmlnsXmlns;
			
			return ht.Keys.GetEnumerator ();
		}

		public virtual IDictionary<string, string> GetNamespacesInScope (XmlNamespaceScope scope)
		{
			var table = new Dictionary<string, string> ();

			if (scope == XmlNamespaceScope.Local) {
				for (int i = 0; i < count; i++)
					if (decls [declPos - i].Prefix == string.Empty && decls [declPos - i].Uri == string.Empty) {
						if (table.ContainsKey (string.Empty))
							table.Remove (string.Empty);
					}
					else if (decls [declPos - i].Uri is string uri)
						table.Add (decls [declPos - i].Prefix, uri);
			} else {
				for (int i = 0; i <= declPos; i++) {
					if (decls [i].Prefix == string.Empty && decls [i].Uri == string.Empty) {
						// removal of default namespace
						if (table.ContainsKey (string.Empty))
							table.Remove (string.Empty);
					}
					else if (decls [i].Uri is string uri)
						table [decls [i].Prefix] = uri;
				}

				if (scope == XmlNamespaceScope.All)
					table.Add ("xml", XmlNamespaceManager.XmlnsXml);
			}

			return table;
		}

		public virtual bool HasNamespace (string prefix) => HasNamespace (prefix, false);

		internal virtual bool HasNamespace (string prefix, bool atomizedNames)
		{
			if (prefix == null || count == 0)
				return false;

			for (int i = declPos; i > declPos - count; i--) {
				if (decls [i].Prefix == prefix)
					return true;
			}
			
			return false;
		}

		public virtual string? LookupNamespace (string prefix) => LookupNamespace (prefix, false);

		internal virtual string? LookupNamespace (string? prefix, bool atomizedNames)
		{
			switch (prefix) {
			case PrefixXmlns:
				return nameTable.Get (XmlnsXmlns);
			case PrefixXml:
				return nameTable.Get (XmlnsXml);
			case "":
				return DefaultNamespace;
			case null:
				return null;
			}

			for (int i = declPos; i >= 0; i--) {
				if (XmlNamespaceManager.CompareString (decls [i].Prefix, prefix, atomizedNames) && decls [i].Uri != null /* null == flag for removed */)
					return decls [i].Uri;
			}
			
			return null;
		}

		public virtual string? LookupPrefix (string uri) => LookupPrefix (uri, false);

		static bool CompareString (string? s1, string? s2, bool atomizedNames) => atomizedNames ? ReferenceEquals (s1, s2) : s1 == s2;

		internal string? LookupPrefix (string uri, bool atomizedName) => LookupPrefixCore (uri, atomizedName, false);

		internal string? LookupPrefixExclusive (string uri, bool atomizedName) => LookupPrefixCore (uri, atomizedName, true);

		string? LookupPrefixCore (string uri, bool atomizedName, bool excludeOverriden)
		{
			if (uri == null)
				return null;

			if (XmlNamespaceManager.CompareString (uri, DefaultNamespace, atomizedName))
				return string.Empty;

			if (XmlNamespaceManager.CompareString (uri, XmlnsXml, atomizedName))
				return PrefixXml;
			
			if (XmlNamespaceManager.CompareString (uri, XmlnsXmlns, atomizedName))
				return PrefixXmlns;

			for (int i = declPos; i >= 0; i--) {
				if (XmlNamespaceManager.CompareString (decls [i].Uri, uri, atomizedName) && decls [i].Prefix.Length > 0) // we already looked for ""
					if (!excludeOverriden || !IsOverriden (i))
						return decls [i].Prefix;
			}

			// ECMA specifies that this method returns String.Empty
			// in case of no match. But actually MS.NET returns null.
			// For more information,see
			//  http://lists.ximian.com/archives/public/mono-list/2003-January/005071.html
			//return String.Empty;
			return null;
		}

		bool IsOverriden (int idx)
		{
			if (idx == declPos)
				return false;
			string prefix = decls [idx + 1].Prefix;
			for (int i = idx + 1; i <= declPos; i++)
				if ((object) decls [idx].Prefix == (object) prefix)
					return true;
			return false;
		}

		public virtual bool PopScope ()
		{
			if (scopePos == -1)
				return false;

			declPos -= count;
			defaultNamespace = scopes [scopePos].DefaultNamespace;
			count = scopes [scopePos].DeclCount;
			scopePos --;
			return true;
		}

		public virtual void PushScope ()
		{
			scopePos ++;
			if (scopePos == scopes.Length)
				GrowScopes ();
			
			scopes [scopePos].DefaultNamespace = defaultNamespace;
			scopes [scopePos].DeclCount = count;
			count = 0;
		}

		// It is rarely used, so we don't need NameTable optimization on it.
		public virtual void RemoveNamespace (string prefix, string uri)
		{
			RemoveNamespace (prefix, uri, false);
		}

		internal virtual void RemoveNamespace (string prefix, string uri, bool atomizedNames)
		{
			if (prefix == null)
				throw new ArgumentNullException (nameof (prefix));

			if (uri == null)
				throw new ArgumentNullException (nameof (uri));
			
			if (count == 0)
				return;

			for (int i = declPos; i > declPos - count; i--) {
				if (XmlNamespaceManager.CompareString (decls [i].Prefix, prefix, atomizedNames) && XmlNamespaceManager.CompareString (decls [i].Uri, uri, atomizedNames))
					decls [i].Uri = null;
			}
		}

		#endregion
	}
}
