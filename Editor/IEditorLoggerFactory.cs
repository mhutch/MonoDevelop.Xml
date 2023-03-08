// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Xml.Editor;

public interface IEditorLoggerFactory
{
	ILogger<T> CreateLogger<T> (ITextBuffer buffer);
	ILogger<T> CreateLogger<T> (ITextView textView);
	ILogger<T> CreateLogger<T> ();
}