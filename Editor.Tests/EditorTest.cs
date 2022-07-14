// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Tests
{
	public abstract class EditorTest
	{
		public EditorTest () : this (XmlTestEnvironment.CreateEditorCatalog ()) { }

		protected EditorTest (EditorCatalog catalog) => Catalog = catalog ?? throw new ArgumentNullException (nameof (catalog));

		/// <summary>
		/// The MEF composition for the test
		/// </summary>
		public EditorCatalog Catalog { get; }

		protected virtual string ContentTypeName => XmlContentTypeNames.XmlCore;

		public virtual IContentType ContentType => Catalog.ContentTypeRegistryService.GetContentType (ContentTypeName ?? StandardContentTypeNames.Text);

		public virtual ITextView CreateTextView (string documentText, string filename = null)
		{
			var buffer = CreateTextBuffer (documentText);
			if (filename != null) {
				Catalog.TextDocumentFactoryService.CreateTextDocument (buffer, filename);
			}
			return Catalog.TextViewFactory.CreateTextView (buffer);
		}

		public virtual ITextBuffer CreateTextBuffer (string documentText)
		{
			return Catalog.BufferFactoryService.CreateTextBuffer (documentText, ContentType);
		}
	}
}
