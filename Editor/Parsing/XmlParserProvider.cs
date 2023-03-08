// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using MonoDevelop.Xml.Editor.Logging;

namespace MonoDevelop.Xml.Editor.Completion
{
	[Export]
	public class XmlParserProvider : BufferParserProvider<XmlBackgroundParser, XmlParseResult>
	{
		readonly IEditorLoggerService loggerService;

		[ImportingConstructor]
		public XmlParserProvider (IEditorLoggerService loggerService)
		{
			this.loggerService = loggerService;
		}

		protected override XmlBackgroundParser CreateParser (ITextBuffer2 buffer)
		{
			var logger = loggerService.CreateLogger<XmlBackgroundParser> (buffer);
			return new (buffer, logger);
		}
	}
}
