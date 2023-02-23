// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

#if NETFRAMEWORK
#nullable disable warnings
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Completion
{
	public abstract partial class XmlCompletionSource : IAsyncCompletionSource
	{
		protected XmlBackgroundParser XmlParser { get; }

		protected ITextView TextView { get; }

		readonly ILogger logger;

		protected XmlCompletionSource (ITextView textView, ILogger logger, XmlParserProvider parserProvider)
		{
			XmlParser = parserProvider.GetParser (textView.TextBuffer);
			TextView = textView;
			this.logger = logger;
			InitializeBuiltinItems ();
		}

		public async virtual Task<CompletionContext> GetCompletionContextAsync (
			IAsyncCompletionSession session,
			CompletionTrigger trigger,
			SnapshotPoint triggerLocation,
			SnapshotSpan applicableToSpan,
			CancellationToken token)
		{
			var reason = ConvertReason (trigger.Reason, trigger.Character);
			if (reason == null) {
				return CompletionContext.Empty;
			}

			var parser = XmlParser.GetSpineParser (triggerLocation);

			// FIXME: cache the value from InitializeCompletion somewhere?
			var (kind, _) = XmlCompletionTriggering.GetTrigger (parser, reason.Value, trigger.Character);

			if (kind != XmlCompletionTrigger.None) {
				List<XObject> nodePath = parser.GetNodePath (triggerLocation.Snapshot);

				session.Properties.AddProperty (typeof (XmlCompletionTrigger), kind);

				switch (kind) {
				case XmlCompletionTrigger.Element:
				case XmlCompletionTrigger.ElementWithBracket:
					// if we're completing an existing element, remove it from the path
					// so we don't get completions for its children instead
					if (nodePath.Count > 0) {
						if (nodePath[nodePath.Count-1] is XElement leaf && leaf.Name.Length == applicableToSpan.Length) {
							nodePath.RemoveAt (nodePath.Count - 1);
						}
					}
					//TODO: if it's on the first or second line and there's no DTD declaration, add the DTDs, or at least <!DOCTYPE
					//TODO: add snippets // MonoDevelop.Ide.CodeTemplates.CodeTemplateService.AddCompletionDataForFileName (DocumentContext.Name, list);
					return await GetElementCompletionsAsync (session, triggerLocation, nodePath, kind == XmlCompletionTrigger.ElementWithBracket, token);

				case XmlCompletionTrigger.Attribute:
					if (parser.Spine.TryFind<IAttributedXObject> (maxDepth: 1) is not IAttributedXObject attributedOb) {
						throw new InvalidOperationException ("Did not find IAttributedXObject in stack for XmlCompletionTrigger.Attribute");
					}
					parser.Clone ().AdvanceUntilEnded ((XObject)attributedOb, triggerLocation.Snapshot, 1000);
					var attributes = attributedOb.Attributes.ToDictionary (StringComparer.OrdinalIgnoreCase);
					return await GetAttributeCompletionsAsync (session, triggerLocation, nodePath, attributedOb, attributes, token);

				case XmlCompletionTrigger.AttributeValue:
					if (parser.Spine.Peek () is XAttribute att && parser.Spine.Peek (1) is IAttributedXObject attributedObject) {
						return await GetAttributeValueCompletionsAsync (session, triggerLocation, nodePath, attributedObject, att, token);
					}
					break;

				case XmlCompletionTrigger.Entity:
					return await GetEntityCompletionsAsync (session, triggerLocation, nodePath, token);

				case XmlCompletionTrigger.DocType:
				case XmlCompletionTrigger.DeclarationOrCDataOrComment:
					return await GetDeclarationCompletionsAsync (session, triggerLocation, nodePath, token);
				}
			}

			return CompletionContext.Empty;
		}

		static XmlTriggerReason? ConvertReason (CompletionTriggerReason reason, char typedChar)
		{
			switch (reason) {
			case CompletionTriggerReason.Insertion:
				if (typedChar != '\0')
					return XmlTriggerReason.TypedChar;
				break;
			case CompletionTriggerReason.Backspace:
				return XmlTriggerReason.Backspace;
			case CompletionTriggerReason.Invoke:
			case CompletionTriggerReason.InvokeAndCommitIfUnique:
				return XmlTriggerReason.Invocation;
			}
			return null;
		}

		public virtual Task<object> GetDescriptionAsync (
			IAsyncCompletionSession session,
			CompletionItem item,
			CancellationToken token)
		{
			return item.GetDocumentationAsync (session, token);
		}

		public virtual CompletionStartData InitializeCompletion (CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
		{
			var reason = ConvertReason (trigger.Reason, trigger.Character);
			if (reason == null) {
				return CompletionStartData.DoesNotParticipateInCompletion;
			}

			var spine = XmlParser.GetSpineParser (triggerLocation);

			LogAttemptingCompletion (logger, spine.CurrentState, spine.CurrentStateLength, trigger.Character, trigger.Reason);

			var (kind, length) = XmlCompletionTriggering.GetTrigger (spine, reason.Value, trigger.Character);
			if (kind != XmlCompletionTrigger.None) {
				return new CompletionStartData (CompletionParticipation.ProvidesItems, new SnapshotSpan (triggerLocation.Snapshot, triggerLocation.Position - length, length));
			}

			//TODO: closing tag completion after typing >

			return CompletionStartData.DoesNotParticipateInCompletion;
		}

		[LoggerMessage (EventId = 2, Level = LogLevel.Trace, Message = "Attempting completion for state '{state}'x{currentSpineLength}, character='{triggerChar}', trigger='{triggerReason}'")]
		static partial void LogAttemptingCompletion (ILogger logger, XmlParserState state, int currentSpineLength, char triggerChar, CompletionTriggerReason triggerReason);


		protected virtual Task<CompletionContext> GetElementCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			bool includeBracket,
			CancellationToken token
			)
			=> Task.FromResult (CompletionContext.Empty);

		protected virtual Task<CompletionContext> GetAttributeCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			IAttributedXObject attributedObject,
			Dictionary<string, string> existingAtts,
			CancellationToken token
			)
			=> Task.FromResult (CompletionContext.Empty);

		protected virtual Task<CompletionContext> GetAttributeValueCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			IAttributedXObject attributedObject,
			XAttribute attribute,
			CancellationToken token
			)
			=> Task.FromResult (CompletionContext.Empty);

		protected virtual Task<CompletionContext> GetEntityCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			CancellationToken token
			)
			=> Task.FromResult (CreateCompletionContext (GetBuiltInEntityItems ()));

		protected virtual Task<CompletionContext> GetDeclarationCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			CancellationToken token
			)
			=> Task.FromResult (
				CreateCompletionContext (
					nodePath.Any (n => n is XElement)
						? new [] { cdataItemWithBracket, commentItemWithBracket }
						: new [] { commentItemWithBracket }
					)
				);

		static CompletionContext CreateCompletionContext (IEnumerable<CompletionItem> items)
			=> new (ImmutableArray<CompletionItem>.Empty.AddRange (items), null, InitialSelectionHint.SoftSelection);

		CompletionItem cdataItem, commentItem, prologItem;
		CompletionItem cdataItemWithBracket, commentItemWithBracket, prologItemWithBracket;
		CompletionItem[] entityItems;

		[MemberNotNull(
			nameof(cdataItem), nameof (commentItem), nameof (prologItem),
			nameof(cdataItemWithBracket), nameof (commentItemWithBracket), nameof (prologItemWithBracket),
			nameof(entityItems))]
		void InitializeBuiltinItems ()
		{
			cdataItem = new CompletionItem ("![CDATA[", this, XmlImages.CData)
					.AddDocumentation ("XML character data")
					.AddKind (XmlCompletionItemKind.CData);

			commentItem = new CompletionItem ("!--", this, XmlImages.Comment)
				.AddDocumentation ("XML comment")
				.AddKind (XmlCompletionItemKind.Comment);

			//TODO: commit $"?xml version=\"1.0\" encoding=\"{encoding}\" ?>"
			prologItem = new CompletionItem ("?xml", this, XmlImages.Prolog)
				.AddDocumentation ("XML prolog")
				.AddKind (XmlCompletionItemKind.Prolog);

			cdataItemWithBracket = new CompletionItem ("<![CDATA[", this, XmlImages.CData)
					.AddDocumentation ("XML character data")
					.AddKind (XmlCompletionItemKind.CData);

			commentItemWithBracket = new CompletionItem ("<!--", this, XmlImages.Comment)
				.AddDocumentation ("XML comment")
				.AddKind (XmlCompletionItemKind.Comment);

			//TODO: commit $"?xml version=\"1.0\" encoding=\"{encoding}\" ?>"
			prologItemWithBracket = new CompletionItem ("<?xml", this, XmlImages.Prolog)
				.AddDocumentation ("XML prolog")
				.AddKind (XmlCompletionItemKind.Prolog);

			entityItems = new CompletionItem[] {
				EntityItem ("apos", "'"),
				EntityItem ("quot", "\""),
				EntityItem ("lt", "<"),
				EntityItem ("gt", ">"),
				EntityItem ("amp", "&"),
			};

			//TODO: need to tweak semicolon insertion dor XmlCompletionItemKind.Entity
			CompletionItem EntityItem (string name, string character) =>
				new CompletionItem (name, this, XmlImages.Entity, ImmutableArray<CompletionFilter>.Empty, string.Empty, name, name, character, ImmutableArray<ImageElement>.Empty)
				.AddEntityDocumentation (character)
				.AddKind (XmlCompletionItemKind.Entity);
		}

		/// <summary>
		/// Gets completion items for closing tags, comments, CDATA etc.
		/// </summary>
		protected IEnumerable<CompletionItem> GetMiscellaneousTags (SnapshotPoint triggerLocation, List<XObject> nodePath, bool includeBracket, bool allowCData = false)
		{
			if (nodePath.Count == 0 & triggerLocation.GetContainingLine().LineNumber == 0) {
				yield return includeBracket? prologItemWithBracket : prologItem;
			}

			if (allowCData) {
				yield return includeBracket ? cdataItemWithBracket : cdataItem;
			}

			yield return includeBracket ? commentItemWithBracket : commentItem;

			foreach (var closingTag in GetClosingTags (nodePath, includeBracket)) {
				yield return closingTag;
			}
		}

		protected IEnumerable<CompletionItem> GetBuiltInEntityItems () => entityItems;

		IEnumerable<CompletionItem> GetClosingTags (List<XObject> nodePath, bool includeBracket)
		{
			var dedup = new HashSet<string> ();

			var prefix = includeBracket ? "</" : "/";

			//FIXME: search forward to see if tag's closed already
			for (int i = nodePath.Count - 1; i >= 0; i--) {
				var ob = nodePath[i];
				if (!(ob is XElement el))
					continue;
				if (!el.IsNamed || el.IsClosed)
					yield break;

				string name = el.Name.FullName!;
				if (!dedup.Add (name)) {
					continue;
				}

				var item = new CompletionItem (prefix + name, this, XmlImages.ClosingTag)
					.AddClosingElementDocumentation (el, dedup.Count > 1)
					.AddKind (dedup.Count == 1? XmlCompletionItemKind.ClosingTag : XmlCompletionItemKind.MultipleClosingTags);
				item.Properties.AddProperty (typeof (List<XObject>), nodePath);
				yield return item;
			}
		}
	}
}
