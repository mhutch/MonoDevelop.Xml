// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Options;

/// <summary>
/// Options that control the behavior of the XML editor
/// </summary>
public static class XmlEditorOptions
{
	public static Option<bool> AutoInsertClosingTag = new ("xml_auto_insert_closing_tag", true, false);
	public static Option<bool> AutoInsertAttributeValue = new ("xml_auto_insert_attribute_value", true, false);
}
