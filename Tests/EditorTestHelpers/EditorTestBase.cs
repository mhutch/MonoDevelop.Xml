// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Tests.Completion;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.EditorTestHelpers
{
	public abstract class EditorTestBase
	{
		[OneTimeSetUp]
		public void InitializeEditorEnvironment ()
		{
			(Environment, Catalog) = InitializeEnvironment ();
		}

		protected abstract (EditorEnvironment, EditorCatalog) InitializeEnvironment ();

		public virtual EditorEnvironment Environment { get; private set; }
		public virtual EditorCatalog Catalog { get; private set; }

		protected abstract string ContentTypeName { get; }

		public virtual IContentType ContentType => Catalog.ContentTypeRegistryService.GetContentType (ContentTypeName ?? StandardContentTypeNames.Text);

		public virtual ITextView CreateTextView (string documentText)
		{
			var buffer = Catalog.BufferFactoryService.CreateTextBuffer (documentText, ContentType);
			return Catalog.TextViewFactory.CreateTextView (buffer);
		}
	}
}
