// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Analysis
{
	public static class XmlDiagnosticExtensions
	{
		public static void Add (this ICollection<XmlDiagnostic> list, XmlDiagnosticDescriptor descriptor, TextSpan span)
			=> list.Add (new XmlDiagnostic (descriptor, span));

		public static void Add (this ICollection<XmlDiagnostic> list, XmlDiagnosticDescriptor descriptor, TextSpan span, params object[] messageArgs)
			=> list.Add (new XmlDiagnostic (descriptor, span, messageArgs));

		public static void Add (this ICollection<XmlDiagnostic> list, XmlDiagnosticDescriptor descriptor, TextSpan span, ImmutableDictionary<string, object> properties, params object[] messageArgs)
			=> list.Add (new XmlDiagnostic (descriptor, span, properties, messageArgs));

		public static void Add (this ICollection<XmlDiagnostic> list, XmlDiagnosticDescriptor descriptor, int position)
			=> list.Add (descriptor, (TextSpan)position);

		public static void Add (this ICollection<XmlDiagnostic> list, XmlDiagnosticDescriptor descriptor, int position, params object[] messageArgs)
			=> list.Add (descriptor, (TextSpan)position, messageArgs);

		public static void Add (this ICollection<XmlDiagnostic> list, XmlDiagnosticDescriptor descriptor, int position, ImmutableDictionary<string, object> properties, params object[] messageArgs)
			=> list.Add (descriptor, (TextSpan) position, properties, messageArgs);
	}
}