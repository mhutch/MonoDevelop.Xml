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
		public string? Message { get; }
		public XmlDiagnosticSeverity Severity { get; }

		public XmlDiagnosticDescriptor (string id, string title, [StringSyntax (StringSyntaxAttribute.CompositeFormat)] string? message, XmlDiagnosticSeverity severity)
		{
			Title = title ?? throw new ArgumentNullException (nameof (title));
			Id = id ?? throw new ArgumentNullException (nameof (id));
			Message = message;
			Severity = severity;
		}

		public XmlDiagnosticDescriptor (string id, string title, XmlDiagnosticSeverity severity)
			: this (id, title, null, severity) { }

		string? combinedMsg;

		internal string GetFormattedMessage (object[]? args)
		{
			try {
				combinedMsg ??= (combinedMsg = Title + Environment.NewLine + Message);
				if (args != null && args.Length > 0) {
					return string.Format (combinedMsg, args);
				}
			} catch (FormatException ex) {
				// this is likely to be called from somewhere other than where the diagnostic was constructed
				// so ensure the error has enough info to track it down
				throw new FormatException ($"Error formatting message for diagnostic {Id}", ex);
			}
			return combinedMsg;
		}
	}
}