// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Logging;

/// <summary>
/// A value that may contain user-identifiable information and is intended to be converted
/// to a string and logged to an <see cref="ILogger"/>.
/// Loggers that strip PII must hash this value unless it can be reliably determined
/// to be a non-identifiable value such a type or filename that is part of the app itself.
/// </summary>
public interface IUserIdentifiableValue
{
	public object? Value { get; }
}

/// </inheritdoc>
public interface IUserIdentifiable<TValue> : IUserIdentifiableValue
{
	public new TValue Value { get; }
}

/// </inheritdoc>
public readonly struct UserIdentifiable<TValue> : IUserIdentifiable<TValue>
{
	public TValue Value { get; }
	public UserIdentifiable (TValue value) { Value = value; }
	public static implicit operator UserIdentifiable<TValue> (TValue value) => new (value);
	public override readonly string ToString () => Value?.ToString () ?? "[null]";

	object? IUserIdentifiableValue.Value => Value;
}

/// <summary>
/// A filename to be logged to an <see cref="ILogger"/> that may originate from the user.
/// Loggers that strip PII must hash this value unless it can be reliably determined
/// to be a non-identifiable value such a filename that is part of the app itself.
/// </summary>
public readonly struct UserIdentifiableFileName : IUserIdentifiable<string>
{
	public string Value { get; }
	public UserIdentifiableFileName (string filename) { Value = filename; }
	public static implicit operator UserIdentifiableFileName (string filename) => new (filename);
	public override readonly string ToString () => Value;

	object? IUserIdentifiableValue.Value => Value;
}