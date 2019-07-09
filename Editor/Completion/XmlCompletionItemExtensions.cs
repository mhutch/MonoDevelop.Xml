// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Editor.Completion
{
	/// <summary>
	/// Helpers for attaching data to <see cref="CompletionItem"/> instances.
	/// </summary>
	public static class XmlCompletionItemExtensions
	{
		static readonly Type DocsKey = typeof (ICompletionDocumentationProvider);
		static readonly Type KindKey = typeof (XmlCompletionItemKind);

		static readonly AnnotationDocumentationProvider annotationDocsProvider = new AnnotationDocumentationProvider ();
		static readonly StringXmlDocumentationProvider stringDocsProvider = new StringXmlDocumentationProvider ();
		static readonly ClosingElementDocumentationProvider closingElementDocProvider = new ClosingElementDocumentationProvider ();
		static readonly EntityDocumentationProvider entityDocsProvider = new EntityDocumentationProvider ();

		public static CompletionItem AddDocumentationProvider (this CompletionItem item, ICompletionDocumentationProvider docsProvider)
		{
			item.Properties.AddProperty (DocsKey, docsProvider);
			return item;
		}

		public static CompletionItem AddDocumentation (this CompletionItem item, string documentation)
		{
			item.Properties.AddProperty (DocsKey, stringDocsProvider);
			item.Properties.AddProperty (stringDocsProvider, documentation);
			return item;
		}

		public static CompletionItem AddDocumentation (this CompletionItem item, XmlSchemaAnnotation annotation)
		{
			item.Properties.AddProperty (DocsKey, annotationDocsProvider);
			item.Properties.AddProperty (annotationDocsProvider, annotation);
			return item;
		}

		internal static CompletionItem AddClosingElementDocumentation (this CompletionItem item, XElement closingFor, bool isMultiple = false)
		{
			item.Properties.AddProperty (DocsKey, closingElementDocProvider);
			item.Properties.AddProperty (closingElementDocProvider, (closingFor, isMultiple));
			return item;
		}

		public static CompletionItem AddEntityDocumentation (this CompletionItem item, string value)
		{
			item.Properties.AddProperty (DocsKey, entityDocsProvider);
			item.Properties.AddProperty (entityDocsProvider, value);
			return item;
		}

		/// <summary>
		/// Marks the item so it can be handled appropriately when committing it.
		/// </summary>
		public static CompletionItem AddKind (this CompletionItem item, XmlCompletionItemKind kind)
		{
			item.Properties.AddProperty (KindKey, kind);
			return item;
		}

		public static XmlCompletionItemKind? GetKind (this CompletionItem item)
		{
			if (item.Properties.TryGetProperty (KindKey, out XmlCompletionItemKind kind)) {
				return kind;
			}
			return null;
		}

		public static Task<object> GetDocumentationAsync (this CompletionItem item, IAsyncCompletionSession session, CancellationToken token)
		{
			if (item.Properties.TryGetProperty<ICompletionDocumentationProvider> (DocsKey, out var provider)) {
				return provider.GetDocumentationAsync (session, item, token);
			}
			return Task.FromResult ((object)null);
		}

		class StringXmlDocumentationProvider : ICompletionDocumentationProvider
		{
			public Task<object> GetDocumentationAsync (IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
			{
				var desc = item.Properties.GetProperty<string> (this);
				var content = new ClassifiedTextElement (
					new ClassifiedTextRun (PredefinedClassificationTypeNames.NaturalLanguage, desc)
				);
				return Task.FromResult<object> (content);
			}
		}

		class AnnotationDocumentationProvider : ICompletionDocumentationProvider
		{
			public Task<object> GetDocumentationAsync (IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
			{
				var annotation = item.Properties.GetProperty<XmlSchemaAnnotation> (this);

				var documentationBuilder = new StringBuilder ();
				foreach (XmlSchemaObject schemaObject in annotation.Items) {
					var schemaDocumentation = schemaObject as XmlSchemaDocumentation;
					if (schemaDocumentation != null && schemaDocumentation.Markup != null) {
						foreach (XmlNode node in schemaDocumentation.Markup) {
							var textNode = node as XmlText;
							if (textNode != null && !string.IsNullOrEmpty (textNode.Data))
								documentationBuilder.Append (textNode.Data);
						}
					}
				}

				var desc = documentationBuilder.ToString ();
				var content = new ClassifiedTextElement (
					new ClassifiedTextRun (PredefinedClassificationTypeNames.NaturalLanguage, desc)
				);

				return Task.FromResult<object> (content);
			}
		}

		class ClosingElementDocumentationProvider : ICompletionDocumentationProvider
		{
			public Task<object> GetDocumentationAsync (IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
			{
				var closingFor = item.Properties.GetProperty<(XElement element, bool isMultiple)> (this);

				ClassifiedTextElement content;

				if (closingFor.isMultiple) {
					content =  new ClassifiedTextElement (
						new ClassifiedTextRun (PredefinedClassificationTypeNames.NaturalLanguage, "Closing tag for element "),
						new ClassifiedTextRun (PredefinedClassificationTypeNames.Type, $"<{closingFor.element.Name}>"),
						new ClassifiedTextRun (PredefinedClassificationTypeNames.NaturalLanguage, ", closing all intermediate elements")
					);
				} else {
					content = new ClassifiedTextElement (
						new ClassifiedTextRun (PredefinedClassificationTypeNames.NaturalLanguage, "Closing tag for element "),
						new ClassifiedTextRun (PredefinedClassificationTypeNames.Type, $"<{closingFor.element.Name}>")
					);
				}


				return Task.FromResult<object> (content);
			}
		}

		class EntityDocumentationProvider : ICompletionDocumentationProvider
		{
			public Task<object> GetDocumentationAsync (IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
			{
				var value = item.Properties.GetProperty<string> (this);

				var content = new ClassifiedTextElement (
					new ClassifiedTextRun (PredefinedClassificationTypeNames.NaturalLanguage, "Escaped '"),
					new ClassifiedTextRun (PredefinedClassificationTypeNames.Literal, value),
					new ClassifiedTextRun (PredefinedClassificationTypeNames.NaturalLanguage, "'")
				);

				return Task.FromResult<object> (content);
			}
		}
	}

	/// <summary>
	/// If an instance of this is attached to a <see cref="CompletionItem"/> instance using <see cref="XmlCompletionItemExtensions.AddDocumentationProvider(CompletionItem, ICompletionDocumentationProvider)"/>,
	/// the completion source will use it to lazily look up documentation.
	/// </summary>
	public interface ICompletionDocumentationProvider
	{
		Task<object> GetDocumentationAsync (IAsyncCompletionSession session, CompletionItem item, CancellationToken token);
	}

	public enum XmlCompletionItemKind
	{
		Element,
		Attribute,
		AttributeValue,
		CData,
		Comment,
		Prolog,
		Entity,
		ClosingTag,
		MultipleClosingTags
	}
}
