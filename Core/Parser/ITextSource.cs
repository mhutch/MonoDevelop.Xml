// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace MonoDevelop.Xml.Parser
{
	public interface ITextSource
	{
		int Length { get; }
		char this[int offset] { get; }
		string GetText (int begin, int length);
		TextReader CreateReader ();
	}

	public static class TextSourceExtensions
	{
		public static string GetTextBetween (this ITextSource source, int begin, int end) => source.GetText (begin, end - begin);
	}
}