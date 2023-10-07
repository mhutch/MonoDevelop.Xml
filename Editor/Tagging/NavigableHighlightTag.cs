// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// ROSLYN IMPORT: from Microsoft.CodeAnalysis.Editor.Shared.Tagging

using Microsoft.VisualStudio.Text.Tagging;

namespace MonoDevelop.Xml.Editor.Tagging
{
	/// <summary>
	/// The base type of any text marker tags that can be navigated with Ctrl+Shift+Up and Ctrl+Shift+Down.
	/// </summary>
	/// <remarks>
	/// Unless you are writing code relating to reference or keyword highlighting, you should not be using
	/// this type.
	/// Taggers that subclass HighlightReferencesTagger should use NavigableHighlightTag as their TagType.
	/// </remarks>
	public abstract class NavigableHighlightTag : TextMarkerTag
	{
		protected NavigableHighlightTag (string type) : base (type)
		{
		}
	}

	public sealed class WrittenReferenceHighlightTag : NavigableHighlightTag
	{
		//NOTE: This re-uses the format defined by Roslyn. The TextMate occurrences tagger does the same.
		public const string TagId = "MarkerFormatDefinition/HighlightedWrittenReference";

		public static readonly WrittenReferenceHighlightTag Instance = new ();

		private WrittenReferenceHighlightTag ()
			: base (TagId)
		{
		}
	}

	public sealed class DefinitionHighlightTag : NavigableHighlightTag
	{
		//NOTE: This re-uses the format defined by Roslyn. The TextMate occurrences tagger does the same.
		public const string TagId = "MarkerFormatDefinition/HighlightedDefinition";

		public static readonly DefinitionHighlightTag Instance = new ();

		private DefinitionHighlightTag ()
			: base (TagId)
		{
		}
	}

	public sealed class ReferenceHighlightTag : NavigableHighlightTag
	{
		//NOTE: This re-uses the format defined by Roslyn. The TextMate occurrences tagger does the same.
		public const string TagId = "MarkerFormatDefinition/HighlightedReference";

		public static readonly ReferenceHighlightTag Instance = new ();

		private ReferenceHighlightTag ()
			: base (TagId)
		{
		}
	}
}
