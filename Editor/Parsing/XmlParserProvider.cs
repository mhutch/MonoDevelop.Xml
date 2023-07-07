// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using MonoDevelop.Xml.Editor.Logging;

namespace MonoDevelop.Xml.Editor.Parsing
{
	[Export]
	public class XmlParserProvider : BufferParserProvider<XmlBackgroundParser, XmlParseResult>
	{
		readonly IEditorLoggerFactory loggerService;
		readonly BackgroundParseServiceProvider parseServiceProvider;

		[ImportingConstructor]
		public XmlParserProvider (IEditorLoggerFactory loggerService, BackgroundParseServiceProvider parseServiceProvider)
		{
			this.loggerService = loggerService;
			this.parseServiceProvider = parseServiceProvider;
		}

		protected override XmlBackgroundParser CreateParser (ITextBuffer2 buffer)
		{
			var logger = loggerService.CreateLogger<XmlBackgroundParser> (buffer);
			var parseService = parseServiceProvider.GetParseServiceForContentType (buffer.ContentType.TypeName);
			return new (buffer, logger, parseService);
		}
	}
}
