// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Editor.Parsing;

/// <summary>
/// Allows observing the status of background parsing operations.
/// May be extended in future to provide custom scheduling.
/// </summary>
public interface IBackgroundParseService
{
	void RegisterBackgroundOperation (Task task);
	event EventHandler RunningStateChanged;
	public bool IsRunning { get; }
}