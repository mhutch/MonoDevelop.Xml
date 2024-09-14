// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Options;

public static class OptionReaderExtensions
{
	public static T GetOption<T>(this IOptionsReader options, Option<T> option)
	{
		if (options.TryGetOption<T> (option, out T value)) {
			return value;
		}
		return option.DefaultValue;
	}
}