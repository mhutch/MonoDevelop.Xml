// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Analysis;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Editor.Parsing
{
	public class XmlParseResult
	{
		public XmlParseResult (XDocument xDocument, IReadOnlyList<XmlDiagnostic> parseDiagnostics, ITextSnapshot textSnapshot)
		{
			XDocument = xDocument;
			ParseDiagnostics = parseDiagnostics;
			TextSnapshot = textSnapshot;
		}

		public IReadOnlyList<XmlDiagnostic> ParseDiagnostics { get; }
		public XDocument XDocument { get; }
		public ITextSnapshot TextSnapshot { get; }
	}
}
