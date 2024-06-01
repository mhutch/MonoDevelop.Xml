// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Diagnostics.CodeAnalysis;

namespace MonoDevelop.Xml.Analysis
{
	public class XmlDiagnosticDescriptor
	{
		public string Id { get; }
		public string Title { get; }

		[StringSyntax (StringSyntaxAttribute.CompositeFormat)]
		public string? MessageFormat { get; }
		public XmlDiagnosticSeverity Severity { get; }

		public XmlDiagnosticDescriptor (string id, string title, [StringSyntax (StringSyntaxAttribute.CompositeFormat)] string? messageFormat, XmlDiagnosticSeverity severity)
		{
			Title = title ?? throw new ArgumentNullException (nameof (title));
			Id = id ?? throw new ArgumentNullException (nameof (id));
			MessageFormat = messageFormat;
			Severity = severity;
		}

		public XmlDiagnosticDescriptor (string id, string title, XmlDiagnosticSeverity severity)
			: this (id, title, null, severity) { }

		internal string GetFormattedMessageWithTitle (object[]? messageArgs)
		{
			try {
				string? message = messageArgs?.Length > 0 && MessageFormat is string format
					? string.Format (MessageFormat, messageArgs)
					: MessageFormat;
				return string.IsNullOrEmpty (message)
					? Title
					: Title + Environment.NewLine + message;
			} catch (FormatException ex) {
				// this is likely to be called from somewhere other than where the diagnostic was constructed
				// so ensure the error has enough info to track it down
				throw new FormatException ($"Error formatting message for diagnostic {Id}", ex);
			}
		}
	}
}