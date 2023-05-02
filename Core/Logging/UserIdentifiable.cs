// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Logging;

/// <summary>
/// A value that may contain user-identifiable information and is
/// intended to be converted to a string and logged to an <see cref="ILogger"/>.
/// The logger must hash this value if PII is a concern.
/// </summary>
public struct UserIdentifiable<TValue>
{
	public TValue Value { get; }
	public UserIdentifiable (TValue value) { Value = value; }
	public static implicit operator UserIdentifiable<TValue> (TValue value) => new (value);
	public override readonly string ToString () => Value?.ToString () ?? "(null)";
}

/// <summary>
/// A value that may contain user-identifiable information and is
/// intended to be converted to a string and logged to an <see cref="ILogger"/>.
/// The logger must hash this value if PII is a concern.
/// </summary>
public struct UserIdentifiableString
{
	public string Value { get; }
	public UserIdentifiableString (string value) { Value = value; }
	public static implicit operator UserIdentifiableString (string value) => new (value);
	public override readonly string ToString () => Value.ToString ();
}

/// <summary>
/// A type to be logged to an <see cref="ILogger"/> that may originate from the user e.g. analyzer assemblies.
/// The logger must hash this value if PII is a concern, but may avoid hashing types that are known to be part of the app itself.
/// </summary>
public struct UserIdentifiableType
{
	public Type Type { get; }
	public UserIdentifiableType (Type type) { Type = type; }
	public static implicit operator UserIdentifiableType (Type type) => new (type);
	public override readonly string ToString () => Type is null? "[null]" : Type.ToString ();
}

/// <summary>
/// A filename to be logged to an <see cref="ILogger"/> that may originate from the user.
/// The logger must hash this value if PII is a concern, but may avoid hashing filenames that are known to be part of the app itself.
/// </summary>
public struct UserIdentifiableFileName
{
	public string Filename { get; }
	public UserIdentifiableFileName (string filename) { Filename = filename; }
	public static implicit operator UserIdentifiableFileName (string filename) => new (filename);
	public override readonly string ToString () => Filename;
}