// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MonoDevelop.Xml.Options;

/// <summary>
/// Defines an option that may affect formatter, editor, analyzer or code fix behavior.
/// Some of these are read from .editorconfig, and others may be mapped to equivalent settings
/// of the host IDE.
/// </summary>
public class Option<T>
{
	public Option(string name, T defaultValue, bool isEditorConfigOption)
	{
		Name = name;
		DefaultValue = defaultValue;
		IsEditorConfigOption = isEditorConfigOption;
	}

	public Option(string name, T value, EditorConfigSerializer<T>? serializer = null) : this(name, value, true)
	{
		Serializer = serializer;
	}

	/// <summary>
	/// A unique name for the option. If this is an editorconfig option, this will be used as the name
	/// in .editorconfig.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The value to use for this option when no setting is found in EditorConfig or
	/// in the host.
	/// </summary>
	public T DefaultValue { get; }

	/// <summary>
	/// Whether this option will be read from .editorconfig.
	/// </summary>
	public bool IsEditorConfigOption { get; }

	/// <summary>
	/// Optionally override the EditorConfig serialization behavior
	/// </summary>
	public EditorConfigSerializer<T>? Serializer { get; }
}

public record EditorConfigSerializer<T> (Func<string, T> Deserialize, Func<T, string> Serialize);
