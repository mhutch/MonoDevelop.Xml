// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
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

	class XmlCompletionTestSource : XmlCompletionSource
	{
		public XmlCompletionTestSource (ITextView textView, ILogger logger, XmlParserProvider parserProvider) : base (textView, logger, parserProvider)
		{
		}

		protected override Task<CompletionContext> GetElementCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			bool includeBracket,
			CancellationToken token)
		{
			var item = new CompletionItem (includeBracket? "<Hello" : "Hello", this)
				.AddKind (XmlCompletionItemKind.Element);
			var items = ImmutableArray<CompletionItem>.Empty;
			items = items.Add (item).AddRange (GetMiscellaneousTags (triggerLocation, nodePath, includeBracket));
			return Task.FromResult (new CompletionContext (items));
		}

		protected override Task<CompletionContext> GetAttributeCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			IAttributedXObject attributedObject,
			Dictionary<string, string> existingAtts,
			CancellationToken token)
		{
			if (nodePath.LastOrDefault () is XElement xel && xel.NameEquals ("Hello", true)) {
				var item = new CompletionItem ("There", this)
					.AddKind (XmlCompletionItemKind.Attribute);
				var items = ImmutableArray<CompletionItem>.Empty;
				items = items.Add (item);
				return Task.FromResult (new CompletionContext (items));
			}

			return Task.FromResult (CompletionContext.Empty);
		}
	}
}
