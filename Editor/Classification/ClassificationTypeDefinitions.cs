// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Classification
{
	public class ClassificationTypeNames
	{
		public const string XmlAttributeName = "xml - attribute name";
		public const string XmlAttributeQuotes = "xml - attribute quotes";
		public const string XmlAttributeValue = "xml - attribute value";
		public const string XmlCDataSection = "xml - cdata section";
		public const string XmlComment = "xml - comment";
		public const string XmlDelimiter = "xml - delimiter";
		public const string XmlEntityReference = "xml - entity reference";
		public const string XmlName = "xml - name";
		public const string XmlProcessingInstruction = "xml - processing instruction";
		public const string XmlText = "xml - text";
	}

	public enum XmlClassificationTypes : byte
	{
		None,
		XmlAttributeName,
		XmlAttributeQuotes,
		XmlAttributeValue,
		XmlCDataSection,
		XmlComment,
		XmlDelimiter,
		XmlEntityReference,
		XmlName,
		XmlProcessingInstruction,
		XmlText,
		Count,
	}

	class ClassificationTypeDefinitions
	{
		[Export]
		[Name (ClassificationTypeNames.XmlAttributeName)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralAttributeNameTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlAttributeQuotes)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralAttributeQuotesTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlAttributeValue)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralAttributeValueTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlCDataSection)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralCDataSectionTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlComment)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralCommentTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlDelimiter)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralDelimiterTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlEntityReference)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralEntityReferenceTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlName)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralNameTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlProcessingInstruction)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralProcessingInstructionTypeDefinition = null;

		[Export]
		[Name (ClassificationTypeNames.XmlText)]
		[BaseDefinition (PredefinedClassificationTypeNames.FormalLanguage)]
		internal readonly ClassificationTypeDefinition XmlLiteralTextTypeDefinition = null;
	}
}
