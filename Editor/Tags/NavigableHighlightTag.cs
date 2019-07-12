// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// ROSLYN IMPORT: from Microsoft.CodeAnalysis.Editor.Shared.Tagging

using Microsoft.VisualStudio.Text.Tagging;

namespace MonoDevelop.Xml.Editor.Tags
{
	/// <summary>
	/// The base type of any text marker tags that can be navigated with Ctrl+Shift+Up and Ctrl+Shift+Down.
	/// </summary>
	/// <remarks>
	/// Unless you are writing code relating to reference or keyword highlighting, you should not be using
	/// this type.</remarks>
	abstract class NavigableHighlightTag : TextMarkerTag
	{
		protected NavigableHighlightTag (string type) : base (type)
		{
		}
	}

	class WrittenReferenceHighlightTag : NavigableHighlightTag
	{
		internal const string TagId = "MarkerFormatDefinition/HighlightedWrittenReference";

		public static readonly WrittenReferenceHighlightTag Instance = new WrittenReferenceHighlightTag ();

		private WrittenReferenceHighlightTag ()
			: base (TagId)
		{
		}
	}

	class DefinitionHighlightTag : NavigableHighlightTag
	{
		internal const string TagId = "MarkerFormatDefinition/HighlightedDefinition";

		public static readonly DefinitionHighlightTag Instance = new DefinitionHighlightTag ();

		private DefinitionHighlightTag ()
			: base (TagId)
		{
		}
	}

	class ReferenceHighlightTag : NavigableHighlightTag
	{
		internal const string TagId = "MarkerFormatDefinition/HighlightedReference";

		public static readonly ReferenceHighlightTag Instance = new ReferenceHighlightTag ();

		private ReferenceHighlightTag ()
			: base (TagId)
		{
		}
	}
}
