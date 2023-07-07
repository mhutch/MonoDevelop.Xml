// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Xml.Editor.Parsing
{
	public abstract class BufferParserProvider<TParser, TParseResult> : IParserProvider<TParser, TParseResult>
		where TParser : BufferParser<TParseResult>
		where TParseResult : class
	{
		protected abstract TParser CreateParser (ITextBuffer2 buffer);

		public TParser GetParser (ITextBuffer buffer)
		{
			lock (key) {
				if (!buffer.Properties.TryGetProperty (key, out TParser parser)) {
					parser = CreateParser ((ITextBuffer2)buffer);
					parser.providerKey = key;
					buffer.Properties.AddProperty (key, parser);
				}
				return parser;
			}
		}

		public bool TryGetParser (ITextBuffer buffer, out TParser parser) => buffer.Properties.TryGetProperty (key, out parser);

		readonly object key = new();
	}
}
