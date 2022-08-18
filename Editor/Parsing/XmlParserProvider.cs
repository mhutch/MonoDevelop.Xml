// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Xml.Editor.Completion
{
	[Export]
	public class XmlParserProvider : BufferParserProvider<XmlBackgroundParser, XmlParseResult>
	{
		readonly ILogger<XmlBackgroundParser> logger;

		[ImportingConstructor]
		public XmlParserProvider (ILogger<XmlBackgroundParser> logger)
		{
			this.logger = logger;
		}

		protected override XmlBackgroundParser CreateParser (ITextBuffer2 buffer) => new (buffer, logger);
	}
}
