// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;

namespace MonoDevelop.Xml.Analysis
{
	public class XmlDiagnosticDescriptor
	{
		public string Id { get; }
		public string Title { get; }
		public string? Message { get; }
		public XmlDiagnosticSeverity Severity { get; }

		public XmlDiagnosticDescriptor (string id, string title, string? message, XmlDiagnosticSeverity severity)
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
			combinedMsg ??= (combinedMsg = Title + Environment.NewLine + Message);
			if (args != null && args.Length > 0) {
				return string.Format (combinedMsg, args);
			}
			return combinedMsg;
		}
	}
}