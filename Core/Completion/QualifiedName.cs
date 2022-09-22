//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
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

#if NETFRAMEWORK
#nullable disable warnings
#endif

using System;
using System.Xml;

namespace MonoDevelop.Xml.Editor.Completion
{
	/// <summary>
	/// An <see cref="XmlQualifiedName"/> with the namespace prefix.
	/// </summary>
	/// <remarks>
	/// The namespace prefix active for a namespace is 
	/// needed when an element is inserted via autocompletion. This
	/// class just adds this extra information alongside the 
	/// <see cref="XmlQualifiedName"/>.
	/// </remarks>
	public class QualifiedName
	{
		XmlQualifiedName xmlQualifiedName = XmlQualifiedName.Empty;
		
		public QualifiedName(string? name, string? namespaceUri)
			: this(name, namespaceUri, string.Empty)
		{
		}
		
		public QualifiedName(string? name, string? namespaceUri, string prefix)
		{
			xmlQualifiedName = new XmlQualifiedName(name, namespaceUri);
			Prefix = prefix;
		}
		
		public static bool operator ==(QualifiedName lhs, QualifiedName rhs)
		{
			if (lhs is not null && rhs is not null) {
				return lhs.Equals(rhs);
			}
			if (lhs is null && rhs is null) {
				return true;
			}
			return false;
		}
		
		public static bool operator !=(QualifiedName lhs, QualifiedName rhs) => !(lhs == rhs);

		/// <summary>
		/// A qualified name is considered equal if the namespace and 
		/// name are the same.  The prefix is ignored.
		/// </summary>
		public override bool Equals (object? obj)
			=> obj switch {
				QualifiedName qualifiedName => xmlQualifiedName.Equals (qualifiedName.xmlQualifiedName),
				XmlQualifiedName name => xmlQualifiedName.Equals (name),
				_ => false
			};
		
		public override int GetHashCode() => xmlQualifiedName.GetHashCode();
		
		/// <summary>
		/// Gets the namespace of the qualified name.
		/// </summary>
		public string Namespace {
			get => xmlQualifiedName.Namespace;
			set => xmlQualifiedName = new XmlQualifiedName(xmlQualifiedName.Name, value);
		}
		
		/// <summary>
		/// Gets the name of the element.
		/// </summary>
		public string Name {
			get => xmlQualifiedName.Name;
			set => xmlQualifiedName = new XmlQualifiedName(value, xmlQualifiedName.Namespace);
		}

		/// <summary>
		/// Gets the namespace prefix used.
		/// </summary>
		public string Prefix { get; set; } = string.Empty;
	}
}
