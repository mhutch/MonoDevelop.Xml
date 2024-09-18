// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MonoDevelop.Xml.Options;

// based on https://github.com/dotnet/roslyn/blob/df4ae6b81013ac45367372176b9c3135a35a7e3c/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Formatting/LineFormattingOptions.cs
/// <summary>
/// Captures common text formatting options values from an <see cref="IOptionsReader"/>
/// so that they may be accessed more efficiently.
/// </summary>
public sealed record class TextFormattingOptionValues ()
{
	public static readonly TextFormattingOptionValues Default = new ();

	public bool ConvertTabsToSpaces { get; init; } = false;
	public int TabSize { get; init; } = 4;
	public int IndentSize { get; init; } = 4;
	public string NewLine { get; init; } = Environment.NewLine;
	public bool TrimTrailingWhitespace { get; init; } = false;

	public TextFormattingOptionValues (IOptionsReader options)
	   : this ()
	{
		ConvertTabsToSpaces = options.GetOption (TextFormattingOptions.ConvertTabsToSpaces);
		TabSize = options.GetOption (TextFormattingOptions.TabSize);
		IndentSize = options.GetOption (TextFormattingOptions.IndentSize);
		NewLine = options.GetOption (TextFormattingOptions.NewLine);
	}
}