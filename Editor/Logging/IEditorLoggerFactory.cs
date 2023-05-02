// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Logging;

/// <summary>
/// Can be imported and used to create <see cref="ILogger"/> instances.
/// <para>
/// Can create loggers scoped to a specific <see cref="ITextView"/> or <see cref="ITextBuffer"/>
/// via the extension methods on <see cref="EditorLoggerFactoryExtensions"/>
/// so that related messages can be aggregated, and so that callers so not have to explicitly
/// log the buffer/view in every message.
/// </para>
/// <para>
/// Wraps multiple <see cref="ILoggerFactory"/> instances and selects the best factory to use based on
/// the file's content type. This allows derived languages to collect log messages from XmlCore components.
/// May also create loggers that dispatch to multiple factories.
/// </para>
/// </summary>
public interface IEditorLoggerFactory
{
	ILogger<T> CreateLogger<T> (string contentTypeName);
	ILogger<T> CreateLogger<T> (IContentType contentType);
}