// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MonoDevelop.Xml.Options;

// based on https://github.com/dotnet/roslyn/blob/199c241cef61d94e25fcfd0f6bcaa91faa35d515/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Formatting/FormattingOptions2.cs#L23
/// <summary>
/// Options that control text formatting. Accessing these multiple times may be done more efficiently using <see cref="TextFormattingOptionValues"/>.
/// </summary>
public class TextFormattingOptions
{
	public static readonly Option<bool> UseTabs = new ("indent_style", false,
		new EditorConfigSerializer<bool> (str => str == "tab", value => value ? "tab" : "space")
	);

	public static readonly Option<int> TabSize = new ("tab_size", 4, true);

	public static readonly Option<int> IndentSize = new ("indent_size", 4, true);

	public static readonly Option<string> NewLine = new ("end_of_line", Environment.NewLine, new EditorConfigSerializer<string> (
		str => str switch {
			"lf" => "\n",
			"cr" => "\r",
			"crlf" => "\r\n",
			_ => Environment.NewLine
		},
		value => value switch {
			"\n" => "lf",
			"\r" => "cr",
			"\r\n" => "crlf",
			_ => "unset"
		}));


	public static readonly Option<bool> InsertFinalNewline = new ("insert_final_newline", true, true);

	public static readonly Option<bool> TrimTrailingWhitespace = new ("trim_trailing_whitespace", true, true);

	public static readonly Option<int?> MaxLineLength = new ("max_line_length", null, new EditorConfigSerializer<int?> (
		str => str != "off" && int.TryParse (str, out var val) && val > 0 ? val : null,
		val => val.HasValue && val.Value > 0 ? val.Value.ToString () : "off"
		));
}
