// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Xml.Editor.Logging;

/// <summary>
/// Can be imported and used to create <see cref="ILogger"/> instances.
/// Similar to <see cref="ILoggerFactory"/> but determines the most appropriate
/// logger to use for a given <see cref="ITextView"/> or <see cref="ITextBuffer"/>.
/// This allows derived languages to collect log messages from XmlCore components
/// when used in the context of their content type.
/// </summary>
public interface IEditorLoggerService
{
	ILogger<T> CreateLogger<T> (ITextBuffer buffer);
	ILogger<T> CreateLogger<T> (ITextView textView);
}
