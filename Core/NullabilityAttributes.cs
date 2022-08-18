// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#if !NETCOREAPP

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage (AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
sealed class MemberNotNullAttribute : Attribute
{
	public MemberNotNullAttribute (params string[] members) { }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class MemberNotNullWhenAttribute : Attribute
{
    public MemberNotNullWhenAttribute(bool when, params string[] members) { }
}

#endif