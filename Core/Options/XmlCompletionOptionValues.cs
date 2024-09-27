// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Options;

/// <summary>
/// Provides completion handlers with with pre-resolved information about the XML completion settings
/// </summary>
public class XmlCompletionOptionValues (IOptionsReader options)
{
	public bool InsertEmptyAttributeValue { get; } = options.GetOption (XmlCompletionOptions.InsertEmptyAttributeValue);
	public bool InsertClosingTag { get; } = options.GetOption (XmlCompletionOptions.InsertClosingTag);
	public char QuoteChar { get; } = options.GetOption (XmlFormattingOptions.QuoteChar);
}
