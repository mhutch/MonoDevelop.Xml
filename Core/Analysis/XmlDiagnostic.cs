// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Analysis
{
	public class XmlDiagnostic
	{
		public XmlDiagnosticDescriptor Descriptor { get; }
		public ImmutableDictionary<string, object> Properties { get; }
		public TextSpan Span { get; }

		readonly object []? messageArgs;

		public XmlDiagnostic (XmlDiagnosticDescriptor descriptor, TextSpan span, ImmutableDictionary<string, object>? properties = null, object[]? messageArgs = null)
		{
			Descriptor = descriptor;
			Span = span;
			Properties = properties ?? ImmutableDictionary<string, object>.Empty;
			this.messageArgs = messageArgs;
		}

		public XmlDiagnostic (XmlDiagnosticDescriptor descriptor, TextSpan span, params object [] messageArgs)
			: this (descriptor, span, null, messageArgs)
		{
		}

		public string GetFormattedMessageWithTitle () => Descriptor.GetFormattedMessageWithTitle (messageArgs);
	}
}