// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Options
{
	public static class XmlOptions
	{
		internal const string AutoInsertClosingTagName = "XmlEditor/AutoInsertClosingTag";
		internal const string AutoInsertAttributeValueName = "XmlEditor/AutoInsertAttributeValue";

		/// <summary>
		/// Automatically insert a closing tag when completing an opening tag
		/// </summary>
		public static EditorOptionKey<bool> AutoInsertClosingTag
			= new EditorOptionKey<bool> (AutoInsertClosingTagName);

		/// <summary>
		/// Automatically insert ="" when completing an attribute
		/// </summary>
		public static EditorOptionKey<bool> AutoInsertAttributeValue
			= new EditorOptionKey<bool> (AutoInsertAttributeValueName);
	}

	[Export (typeof (EditorOptionDefinition))]
	[Name(XmlOptions.AutoInsertClosingTagName)]
	sealed class AutoInsertClosingTagOption : EditorOptionDefinition<bool>
	{
		public override bool Default { get { return true; } }
		public override EditorOptionKey<bool> Key { get { return XmlOptions.AutoInsertClosingTag; } }
	}

	[Export (typeof (EditorOptionDefinition))]
	[Name(XmlOptions.AutoInsertAttributeValueName)]
	sealed class AutoInsertAttributeValueOption : EditorOptionDefinition<bool>
	{
		public override bool Default { get { return true; } }
		public override EditorOptionKey<bool> Key { get { return XmlOptions.AutoInsertAttributeValue; } }
	}

	static class XmlEditorOptionExtensions
	{
		public static bool GetAutoInsertClosingTag (this IEditorOptions options)
			=> options.GetOptionValue (XmlOptions.AutoInsertClosingTag);

		public static bool GetAutoInsertAttributeValue (this IEditorOptions options)
			=> options.GetOptionValue (XmlOptions.AutoInsertAttributeValue);
	}
}
