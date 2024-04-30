// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.Logging;
using MonoDevelop.Xml.Editor.Parsing;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Tests.Completion
{

	[Export (typeof (IAsyncCompletionSourceProvider))]
	[Name ("Xml Completion Test Source Provider")]
	[ContentType (XmlEditorTestContentType.Name)]
	class XmlCompletionTestSourceProvider : IAsyncCompletionSourceProvider
	{
		readonly IEditorLoggerFactory loggerService;
		readonly XmlParserProvider parserProvider;

		[ImportingConstructor]
		public XmlCompletionTestSourceProvider (IEditorLoggerFactory loggerService, XmlParserProvider parserProvider)
		{
			this.loggerService = loggerService;
			this.parserProvider = parserProvider;
		}

		public IAsyncCompletionSource GetOrCreate (ITextView textView)
		{
			var logger = loggerService.CreateLogger<XmlCompletionTestSource> (textView);
			return new XmlCompletionTestSource (textView, logger, parserProvider);
		}
	}

	class XmlCompletionTestSource : XmlCompletionSource<XmlCompletionTriggerContext>
	{
		public XmlCompletionTestSource (ITextView textView, ILogger logger, XmlParserProvider parserProvider) : base (textView, logger, parserProvider)
		{
		}

		protected override Task<IList<CompletionItem>?> GetElementCompletionsAsync (XmlCompletionTriggerContext context, bool includeBracket, CancellationToken token)
		{
			var item = new CompletionItem (includeBracket? "<Hello" : "Hello", this)
				.AddKind (XmlCompletionItemKind.Element);
			var items = new List<CompletionItem> () { item };
			if (context.NodePath is not null) {
				items.AddRange (GetMiscellaneousTags (context.TriggerLocation, context.NodePath, includeBracket));
			}
			return Task.FromResult<IList<CompletionItem>?> (items);
		}

		protected override Task<IList<CompletionItem>?> GetAttributeCompletionsAsync (XmlCompletionTriggerContext context, IAttributedXObject attributedObject, Dictionary<string, string> existingAtts, CancellationToken token)
		{
			if (context.NodePath?.LastOrDefault () is XElement xel && xel.NameEquals ("Hello", true)) {
				var item = new CompletionItem ("There", this)
					.AddKind (XmlCompletionItemKind.Attribute);
				var items = new List<CompletionItem> () {  item };
				return Task.FromResult<IList<CompletionItem>?> (items);
			}

			return Task.FromResult<IList<CompletionItem>?> (null);
		}

		protected override XmlCompletionTriggerContext CreateTriggerContext (IAsyncCompletionSession session, CompletionTrigger trigger, XmlSpineParser spineParser, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan)
			=> new (session, triggerLocation, spineParser, trigger, applicableToSpan);
	}
}
