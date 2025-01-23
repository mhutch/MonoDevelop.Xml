// based on https://raw.githubusercontent.com/dotnet/roslyn/f20e4d3e49b4e2d0b615e57c64fac5c356b25079/src/Workspaces/Core/Portable/Options/IGlobalOptionService.cs
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MonoDevelop.Xml.Options;

/// <summary>
/// Provides services for reading and writing global client (in-proc) options
/// shared across all workspaces.
/// </summary>
public interface IGlobalOptionService : IOptionsReader
{
    /// <summary>
    /// Gets the current value of the specific option.
    /// </summary>
    T GetOption<T>(Option<T> option);

    /// <summary>
    /// Gets the current values of specified options.
    /// All options are read atomically.
    /// </summary>
    ImmutableArray<object?> GetOptions(ImmutableArray<IOption> optionKeys);

    void SetGlobalOption<T>(Option<T> option, T value);

    /// <summary>
    /// Sets and persists the value of a global option.
    /// Sets the value of a global option.
    /// Invokes registered option persisters.
    /// Triggers option changed event for handlers registered with <see cref="AddOptionChangedHandler"/>.
    /// </summary>
    void SetGlobalOption(IOption optionKey, object? value);

    /// <summary>
    /// Atomically sets the values of specified global options. The option values are persisted.
    /// Triggers option changed event for handlers registered with <see cref="AddOptionChangedHandler"/>.
    /// </summary>
    /// <remarks>
    /// Returns true if any option changed its value stored in the global options.
    /// </remarks>
    bool SetGlobalOptions(ImmutableArray<KeyValuePair<IOption, object?>> options);

    /// <summary>
    /// Refreshes the stored value of an option. This should only be called from persisters.
    /// Does not persist the new option value.
    /// </summary>
    /// <remarks>
    /// Returns true if the option changed its value stored in the global options.
    /// </remarks>
    bool RefreshOption(IOption optionKey, object? newValue);

    void AddOptionChangedHandler(object target, WeakEventHandler<OptionChangedEventArgs> handler);

    void RemoveOptionChangedHandler(object target, WeakEventHandler<OptionChangedEventArgs> handler);
}

public delegate void WeakEventHandler<TEventArgs>(object sender, object target, TEventArgs args);

public sealed class OptionChangedEventArgs(ImmutableArray<(IOption key, object? newValue)> changedOptions) : EventArgs
{
    public ImmutableArray<(IOption key, object? newValue)> ChangedOptions => changedOptions;

    public bool HasOption(Func<IOption, bool> predicate)
        => changedOptions.Any(option => predicate(option.key));
}
