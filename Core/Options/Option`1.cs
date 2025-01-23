// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace MonoDevelop.Xml.Options;

/// <summary>
/// Defines an option that may affect formatter, editor, analyzer or code fix behavior.
/// Some of these are read from .editorconfig, and others may be mapped to equivalent settings
/// of the host IDE.
/// </summary>
public class Option<T> : IOption
{
	public Option(string name, T value, bool isEditorConfigOption, IEditorConfigSerializer<T>? serializer = null)
	{
		Name = name;
		DefaultValue = value;
		IsEditorConfigOption = isEditorConfigOption;
		Serializer = serializer ?? EditorConfigSerializer.Default<T>();
	}

	/// <<inheritdoc/>
	public string Name { get; }

	/// <summary>
	/// The value to use for this option when no setting is found in EditorConfig or
	/// in the host.
	/// </summary>
	public T DefaultValue { get; }

	/// <inheritdoc/>
	public bool IsEditorConfigOption { get; }

	/// <summary>
	/// Deserialize the option value to/from an editorconfig string
	/// </summary>
	public IEditorConfigSerializer<T> Serializer { get; }

	public Type Type => typeof(T);

	IEditorConfigSerializer IOption.Serializer => Serializer;
}
