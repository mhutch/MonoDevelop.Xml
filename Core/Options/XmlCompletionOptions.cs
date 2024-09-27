// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Options;

public static class XmlCompletionOptions
{
	/// <summary>
	/// Whether completion should insert ="" when completing an attribute
	/// </summary>
	public static readonly Option<bool> InsertEmptyAttributeValue = new ("xml_insert_empty_attribute_value", true, false);

	/// <summary>
	/// Whether completion should insert a closing tag when completing an element
	/// </summary>
	public static readonly Option<bool> InsertClosingTag = new ("xml_insert_closing_tag", true, false);
}
