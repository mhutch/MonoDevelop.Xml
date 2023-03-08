// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Logging;

/// <summary>
/// A factory that can create ILogger instances for a given content type. Must be exported with a ContentType attribute.
/// May optionally use Name/Order attributes to control precedence relative to other loggers.
/// </summary>
public interface IEditorLoggerFactory
{
	ILogger<T> CreateLogger<T> ();
}
