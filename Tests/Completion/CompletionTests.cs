// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Tests.EditorTestHelpers;
using NUnit.Framework;

namespace MonoDevelop.Xml.Tests.Completion
{
	static class CompletionTestContentType
	{
		public const string Name = "XmlCompletionTest";

		[Export]
		[Name (Name)]
		[BaseDefinition (XmlContentTypeNames.XmlCore)]
		public static readonly ContentTypeDefinition XmlCompletionTestContentTypeDefinition = null;
	}

	[Export (typeof (IAsyncCompletionSourceProvider))]
	[Name ("Xml Completion Test Source Provider")]
	[ContentType (CompletionTestContentType.Name)]
	class XmlCompletionTestSourceProvider : IAsyncCompletionSourceProvider
	{
		public IAsyncCompletionSource GetOrCreate (ITextView textView) => new XmlCompletionTestSource (textView);
	}

	class XmlCompletionTestSource : XmlCompletionSource<XmlBackgroundParser,XmlParseResult>
	{
		public XmlCompletionTestSource (ITextView textView) : base (textView)
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

	[TestFixture]
	public class CompletionTests : CompletionTestBase
	{
		protected override string ContentTypeName => CompletionTestContentType.Name;

		[Test]
		public async Task TestElementStartCompletion ()
		{
			var result = await GetCompletionContext ("<$");
			result.AssertNonEmpty ();
			result.AssertContains ("Hello");
			result.AssertContains ("!--");
		}

		[Test]
		public async Task TestRootCompletion ()
		{
			var result = await GetCompletionContext ("$");
			result.AssertNonEmpty ();
			result.AssertContains ("<Hello");
			result.AssertContains ("<!--");
		}

		[Test]
		public async Task TestElementNameCompletionInvocation ()
		{
			var result = await GetCompletionContext ("<foo$");
			result.AssertNonEmpty ();
			result.AssertContains ("Hello");
			result.AssertContains ("!--");
		}

		protected override (EditorEnvironment, EditorCatalog) InitializeEnvironment () => TestEnvironment.EnsureInitialized ();
	}
}
