// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor.Completion;

namespace MonoDevelop.Xml.Editor.Tests
{
	public abstract class XmlEditorTest : EditorTest
	{
		protected override EditorCatalog CreateCatalog () => XmlTestEnvironment.CreateEditorCatalog ();

		protected override string ContentTypeName => XmlEditorTestContentType.Name;

		protected XmlBackgroundParser GetParser (ITextBuffer buffer) => Catalog.GetService<XmlParserProvider> ().GetParser (buffer);
	}

	/// <summary>
	/// Content type for editor extensions used only in the context of testing
	/// </summary>
	static class XmlEditorTestContentType
	{
		public const string Name = "XmlEditorTest";

		[Export]
		[Name (Name)]
		[BaseDefinition (XmlContentTypeNames.XmlCore)]
		public static readonly ContentTypeDefinition? XmlCompletionTestContentTypeDefinition = null;
	}
}
