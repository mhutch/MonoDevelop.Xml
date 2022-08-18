// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Xml.Editor.Completion
{
	public interface IParserProvider<TParser,TParseResult> where TParser : BufferParser<TParseResult> where TParseResult : class
	{
		TParser GetParser (ITextBuffer buffer);
	}
}
