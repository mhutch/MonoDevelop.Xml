// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Options;

public interface IOptionsReader
{
	bool TryGetOption<T> (Option<T> option, out T value);
}
