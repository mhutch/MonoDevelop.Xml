// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor.Logging;
using MonoDevelop.Xml.Tests;

namespace MonoDevelop.Xml.Editor.Tests;

[Export (typeof (IEditorLoggerProvider))]
[ContentType (XmlEditorTestContentType.Name)]
class XmlTestEditorLoggerProvider : IEditorLoggerProvider
{
	public ILogger CreateLogger (string categoryName) => TestLoggerFactory.CreateLogger (categoryName);

	public void Dispose () { }
}