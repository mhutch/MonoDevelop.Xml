// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MonoDevelop.Xml.Analysis
{
	class XmlCoreDiagnostics
	{
		public static XmlDiagnosticDescriptor IncompleteAttributeValue = new (
			nameof (IncompleteAttributeValue),
			"Incomplete attribute value",
			"The value of attribute '{0}' ended unexpectedly due to character '{1}'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor UnquotedAttributeValue = new (
			nameof (UnquotedAttributeValue),
			"Unquoted attribute value",
			"The value of attribute '{0}' is not contained within quote markers.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor DuplicateAttributeName = new (
			nameof (DuplicateAttributeName),
			"Duplicate attribute name",
			"Element has more than one attribute named '{0}'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteAttribute = new (
			nameof (IncompleteAttribute),
			"Incomplete attribute",
			"Attribute is incomplete due to unexpected character '{0}'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor UnclosedTag = new (
			nameof (UnclosedTag),
			"Unclosed tag",
			"The tag '{0}' has no matching closing tag",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor UnmatchedClosingTag = new (
			nameof (UnmatchedClosingTag),
			"Unmatched closing tag",
			"The closing tag '{0}' does not match any open tag",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteClosingTag = new (
			nameof (IncompleteClosingTag),
			"Incomplete closing tag",
			"Closing tag is incomplete due to unexpected character '{0}'",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor UnnamedClosingTag = new (
			nameof (UnnamedClosingTag),
			"Unnamed closing tag",
			"The closing tag ended without a name",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor MalformedTagOpening = new (
			nameof (MalformedTagOpening),
			"Malformed tag",
			"Tag is malformed due to unexpected character '{0}'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor MalformedTag = new (
			nameof (MalformedTag),
			"Malformed tag",
			"Tag is malformed due to unexpected character '{0}'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor MalformedNamedTag = new (
			nameof (MalformedNamedTag),
			"Malformed tag",
			"Tag '{0}' is malformed due to unexpected character '{1}'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor MalformedSelfClosingTag = new (
			nameof (MalformedSelfClosingTag),
			"Malformed tag",
			"Self-closing tag is malformed due to unexpected character '{0}' after the forward slash.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor MalformedNamedSelfClosingTag = new (
			nameof (MalformedSelfClosingTag),
			"Malformed tag",
			"Self-closing tag '{0}' is malformed due to unexpected character '{1}' after the forward slash.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor UnnamedTag = new (
			nameof (UnnamedTag),
			"Unnamed tag",
			"The tag ended without a name.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor UnexpectedEndOfFile = new (
			nameof (UnexpectedEndOfFile),
			"Unexpected end of file",
			"Incomplete node due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteDocType = new (
			nameof (IncompleteDocType),
			"Incomplete doctype",
			"Doctype is incomplete due to unexpected character '{0}'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteEndComment = new (
			nameof (IncompleteEndComment),
			"Incomplete end comment",
			"The string '--' must not appear in comments except when ending the comment with '-->'.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor ZeroLengthNamespace = new (
			nameof (ZeroLengthNamespace),
			"Zero-length namespace",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor ZeroLengthNameWithNamespace = new (
			nameof (ZeroLengthNameWithNamespace),
			"Zero-length name with non-empty namespace.",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor InvalidNameCharacter = new (
			nameof (InvalidNameCharacter),
			"Name has invalid character",
			"Name was ended by invalid name character '{0}'",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor MultipleNamespaceSeparators = new (
			nameof (MultipleNamespaceSeparators),
			"Name has multiple namespace separators",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteAttributeEof = new (
			nameof (IncompleteAttributeEof),
			"Incomplete attribute",
			"Incomplete attribute due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteClosingTagEof = new (
			nameof (IncompleteClosingTagEof),
			"Incomplete closing tag",
			"Incomplete closing tag due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteDocTypeEof = new (
			nameof (IncompleteDocTypeEof),
			"Incomplete doctype",
			"Incomplete doctype due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteTagEof = new (
			nameof (IncompleteTagEof),
			"Incomplete tag",
			"Incomplete tag due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteCDataEof = new (
			nameof (IncompleteCDataEof),
			"Incomplete CDATA",
			"Incomplete CDATA due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteCommentEof = new (
			nameof (IncompleteCommentEof),
			"Incomplete comment",
			"Incomplete comment due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);

		public static XmlDiagnosticDescriptor IncompleteProcessingInstructionEof = new (
			nameof (IncompleteProcessingInstructionEof),
			"Incomplete processing instruction",
			"Incomplete processing instruction due to unexpected end of file",
			XmlDiagnosticSeverity.Error
		);
	}
}
