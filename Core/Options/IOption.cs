// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MonoDevelop.Xml.Options;

public interface IOption
{
	/// <summary>
	/// A unique name for the option. If this is an editorconfig option, this will be used as the name
	/// in .editorconfig.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Whether this option will be read from .editorconfig.
	/// </summary>
	bool IsEditorConfigOption { get; }

	/// <summary>
	/// Deserialize the option value to/from an editorconfig string
	/// </summary>
	IEditorConfigSerializer Serializer {get; }

	/// <summary>
	/// The type of the option value
	/// </summary>
	Type Type { get; }
}
