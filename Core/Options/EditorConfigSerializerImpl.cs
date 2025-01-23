// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace MonoDevelop.Xml.Options;

abstract class EditorConfigSerializerImpl<T> : IEditorConfigSerializer<T>
{
	public abstract bool TryParse (string value, [MaybeNullWhen(false)] out T? parsedValue);
	public abstract string Serialize (T value);

	bool IEditorConfigSerializer.TryParse (string value, out object? parsedValue)
	{
		if (TryParse (value, out var parsedValueTyped)) {
			parsedValue = parsedValueTyped;
			return true;
		}

		parsedValue = null;
		return false;
	}

	string IEditorConfigSerializer.Serialize (object value) => Serialize ((T)value);
}
