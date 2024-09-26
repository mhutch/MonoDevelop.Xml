// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Options;

/// <summary>
/// Options that control XML formatting
/// </summary>
public static class XmlFormattingOptions
{
	public static readonly Option<bool> OmitXmlDeclaration = new ("xml_omit_declaration", false, true);
	public static readonly Option<bool> IndentContent = new ("xml_indent_content", true, true);

	public static readonly Option<bool> AttributesOnNewLine = new ("xml_attributes_on_new_line", false, true);
	public static readonly Option<int> MaxAttributesPerLine = new ("xml_max_attributes_per_line", 10, true);

	public static readonly Option<bool> AlignAttributes = new ("xml_align_attributes", false, true);
	public static readonly Option<bool> AlignAttributeValues = new ("xml_align_attribute_values", false, true);
	public static readonly Option<bool> WrapAttributes = new ("xml_wrap_attributes", false, true);
	public static readonly Option<int> SpacesBeforeAssignment = new ("xml_spaces_before_assignment", 0, true);
	public static readonly Option<int> SpacesAfterAssignment = new ("xml_spaces_after_assignment", 0, true);

	public static readonly Option<char> QuoteChar = new ("xml_quote_style", '"', new EditorConfigSerializer<char> (
		str => str == "single" ? '\'' : '"',
		val => val == '\'' ? "single" : "double"
		));

	public static readonly Option<int> EmptyLinesBeforeStart = new ("xml_empty_lines_before_start", 0, true);
	public static readonly Option<int> EmptyLinesAfterStart = new ("xml_empty_lines_after_start", 0, true);
	public static readonly Option<int> EmptyLinesBeforeEnd = new ("xml_empty_lines_before_end", 0, true);
	public static readonly Option<int> EmptyLinesAfterEnd = new ("xml_empty_lines_after_end", 0, true);
}