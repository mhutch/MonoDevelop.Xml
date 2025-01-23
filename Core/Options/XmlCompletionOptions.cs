// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace MonoDevelop.Xml.Options;

public static class XmlCompletionOptions
{
	/// <summary>
	/// Whether completion should insert ="" when completing an attribute
	/// </summary>
	public static readonly Option<bool> InsertEmptyAttributeValue = new ("xml_insert_empty_attribute_value", false, false);

	/// <summary>
	/// Whether completion should insert a closing tag when completing an element
	/// </summary>
	public static readonly Option<bool> InsertClosingTag = new ("xml_insert_closing_tag", true, false);

	public static IEnumerable<IOption> GetAll ()
	{
		yield return InsertEmptyAttributeValue;
		yield return InsertClosingTag;
	}
}
