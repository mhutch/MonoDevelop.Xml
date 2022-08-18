// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Completion
{
	public class XmlParseResult
	{
		public XmlParseResult (XDocument xDocument, List<XmlDiagnosticInfo> parseDiagnostics, ITextSnapshot textSnapshot)
		{
			XDocument = xDocument;
			ParseDiagnostics = parseDiagnostics;
			TextSnapshot = textSnapshot;
		}

		public List<XmlDiagnosticInfo> ParseDiagnostics { get; }
		public XDocument XDocument { get; }
		public ITextSnapshot TextSnapshot { get; }
	}
}
