// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace MonoDevelop.Xml.Options;

static class EditorConfigSerializer
{
	// TODO: use a TryParse func instead of deserialize (Roslyn implements this with a Func<string,Optional<T>>)
	public static IEditorConfigSerializer<T> Create<T> (Func<string, T> deserialize, Func<T, string> serialize) => new EditorConfigFuncSerializer<T> (deserialize, serialize);

	public static IEditorConfigSerializer<T> Default<T> () => typeof(T) switch {
		Type t when t == typeof(string) => (IEditorConfigSerializer<T>)(object)stringSerializer,
		Type t when t == typeof(int) => (IEditorConfigSerializer<T>)(object)intSerializer,
		Type t when t == typeof(bool) => (IEditorConfigSerializer<T>)(object)boolSerializer,
		Type t when t == typeof(bool?) => (IEditorConfigSerializer<T>)(object)nullableBoolSerializer,
		_ => throw new NotSupportedException ($"No default serializer for type {typeof(T)}")
	};

	public static IEditorConfigSerializer<T> CreateForEnum<T> () where T : struct, Enum => new EditorConfigEnumSerializer<T> ();

	static EditorConfigFuncSerializer<string> stringSerializer = new (s => s, s => s);
	static EditorConfigIntSerializer intSerializer = new EditorConfigIntSerializer ();
	static EditorConfigBoolSerializer boolSerializer = new ();
	static EditorConfigNullableBoolSerializer nullableBoolSerializer = new ();

	sealed class EditorConfigEnumSerializer<TEnum> : EditorConfigSerializerImpl<TEnum> where TEnum : struct, Enum
	{
		public override bool TryParse (string value, [MaybeNullWhen (false)] out TEnum parsedValue) => Enum.TryParse (value, ignoreCase: true, out parsedValue);
		public override string Serialize (TEnum value) => value.ToString ();
	}

	sealed class EditorConfigIntSerializer : EditorConfigSerializerImpl<int>
	{
		public override bool TryParse (string value, [MaybeNullWhen (false)] out int parsedValue) => int.TryParse (value, out parsedValue);
		public override string Serialize (int value) => value.ToString ();
	}

	sealed class EditorConfigBoolSerializer : EditorConfigSerializerImpl<bool>
	{
		public override bool TryParse (string value, [MaybeNullWhen (false)] out bool parsedValue) => bool.TryParse (value, out parsedValue);
		public override string Serialize (bool value) => value? "true" : "false";
	}

	sealed class EditorConfigNullableBoolSerializer : EditorConfigSerializerImpl<bool?>
	{
		public override bool TryParse (string value, out bool? parsedValue)
		{
			if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) {
				parsedValue = null;
				return true;
			}

			if (bool.TryParse (value, out var parsedBool)) {
				parsedValue = parsedBool;
				return true;
			}

			parsedValue = null;
			return false;
		}

		public override string Serialize (bool? value) => value switch {
			true => "true",
			false => "false",
			_ => "null"
		};
	}

	sealed class EditorConfigFuncSerializer<T> (Func<string, T> deserialize, Func<T, string> serialize) : EditorConfigSerializerImpl<T>
	{
		public override string Serialize (T value) => serialize (value);

		public override bool TryParse (string value, [MaybeNullWhen (false)] out T? parsedValue)
		{
			try {
				parsedValue = deserialize (value);
				return true;
			} catch {
				parsedValue = default;
				return false;
			}
		}
	}
}
