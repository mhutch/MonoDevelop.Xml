// 
// XmlNamespaceMap.cs
//  
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Novell, Inc.
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

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MonoDevelop.Xml.Editor.Completion
{
	class XmlNamespacePrefixMap
	{
		readonly Dictionary<string, string> pfNsMap = new ();
		readonly Dictionary<string, string> nsPfMap = new ();

		/// <summary>Gets the prefix registered for the namespace, empty if it's 
		/// the default namespace, or null if it's not registered.</summary>
		public bool TryGetPrefix (string ns, [NotNullWhen(true)] out string? prefix) => nsPfMap.TryGetValue (ns, out prefix);
		
		/// <summary>Gets the namespace registered for prefix, or default namespace if prefix is empty.</summary>
		public bool TryGetNamespace (string prefix, [NotNullWhen (true)] out string? ns) => pfNsMap.TryGetValue (prefix, out ns);
		
		/// <summary>Registers a namespace for a prefix, or the default namespace if the prefix is empty.</summary>
		public void AddPrefix (string ns, string prefix)
		{
			nsPfMap[ns] = prefix;
			pfNsMap[prefix] = ns;
		}
	}
}
