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
using MonoDevelop.Xml.Editor.Parsing;
using MonoDevelop.Xml.Logging;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Completion
{
	public abstract partial class XmlCompletionSource<TCompletionTriggerContext> : IAsyncCompletionSource where TCompletionTriggerContext : XmlCompletionTriggerContext
	{
		protected XmlParserProvider XmlParserProvider { get; }

		protected ITextView TextView { get; }

		protected ILogger Logger { get; }

		protected XmlCompletionSource (ITextView textView, ILogger logger, XmlParserProvider parserProvider)
		{
			XmlParserProvider = parserProvider;
			TextView = textView;
			Logger = logger;
			InitializeBuiltInItems ();
		}

		protected XmlBackgroundParser GetParser (ITextBuffer textBuffer) => XmlParserProvider.GetParser (textBuffer);

		protected XmlSpineParser GetSpineParser (SnapshotPoint snapshotPoint)
		{
			var backgroundParser = GetParser (snapshotPoint.Snapshot.TextBuffer);
			var spineParser = backgroundParser.GetSpineParser (snapshotPoint);
			return spineParser;
		}

		public Task<CompletionContext> GetCompletionContextAsync (IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
			=> Logger.InvokeAndLogExceptions (() => GetCompletionContextAsyncInternal (session, trigger, triggerLocation, applicableToSpan, token));

		async Task<CompletionContext> GetCompletionContextAsyncInternal (IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
		{
			var spineParser = GetSpineParser (triggerLocation);
			var triggerContext = CreateTriggerContext (session, trigger, spineParser, triggerLocation, applicableToSpan);

			if (!triggerContext.IsSupportedTriggerReason) {
				return CompletionContext.Empty;
			}

			await triggerContext.InitializeNodePath (Logger, token).ConfigureAwait (false);

			var tasks = GetCompletionTasks (triggerContext, token).ToList ();

			await Task.WhenAll (tasks).ConfigureAwait (false);

			var allItems = ImmutableArray<CompletionItem>.Empty;
			foreach (var task in tasks) {
#pragma warning disable VSTHRD103 // Call async methods when in an async method
				if (task.Result is IList<CompletionItem> taskItems && taskItems.Count > 0) {
					allItems = allItems.AddRange (taskItems);
				}
#pragma warning restore VSTHRD103 // Call async methods when in an async method
			}

			if (allItems.IsEmpty) {
				return CompletionContext.Empty;
			}

			return new CompletionContext (allItems, null, InitialSelectionHint.SoftSelection);
		}

		/// <summary>
		/// Construct a context that gathers computed information about the current completion trigger point.
		/// </summary>
		protected abstract TCompletionTriggerContext CreateTriggerContext (IAsyncCompletionSession session, CompletionTrigger trigger, XmlSpineParser spineParser, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan);

		IEnumerable<Task<IList<CompletionItem>?>> GetCompletionTasks (TCompletionTriggerContext triggerContext, CancellationToken cancellationToken)
		{
			yield return GetAdditionalCompletionsAsync (triggerContext, cancellationToken);

			if (triggerContext.XmlTriggerKind == XmlCompletionTrigger.None) {
				yield break;
			}

			// this is used by XmlCompletionCommitManager.ShouldCommitCompletion to determine whether XmlCompletionSource participated in the session and how the completion should be committed
			triggerContext.Session.Properties[typeof (XmlCompletionTrigger)] = triggerContext.XmlTriggerKind;

			switch (triggerContext.XmlTriggerKind) {
			case XmlCompletionTrigger.ElementValue:
				yield return GetElementValueCompletionsAsync (triggerContext, cancellationToken);
				goto case XmlCompletionTrigger.Tag;

			case XmlCompletionTrigger.Tag:
			case XmlCompletionTrigger.ElementName:
				//TODO: if it's on the first or second line and there's no DTD declaration, add the DTDs, or at least <!DOCTYPE
				//TODO: add snippets // MonoDevelop.Ide.CodeTemplates.CodeTemplateService.AddCompletionDataForFileName (DocumentContext.Name, list);
				yield return GetElementCompletionsAsync (triggerContext, triggerContext.XmlTriggerKind != XmlCompletionTrigger.ElementName, cancellationToken);
				break;

			case XmlCompletionTrigger.AttributeName:
				if (triggerContext.SpineParser.Spine.TryFind<IAttributedXObject> (maxDepth: 1) is not IAttributedXObject attributedOb) {
					throw new InvalidOperationException ("Did not find IAttributedXObject in stack for XmlCompletionTrigger.Attribute");
				}
				triggerContext.SpineParser.Clone ().AdvanceUntilEnded ((XObject)attributedOb, triggerContext.TriggerLocation.Snapshot, 1000);
				var attributes = attributedOb.Attributes.ToDictionary (StringComparer.OrdinalIgnoreCase);
				yield return GetAttributeCompletionsAsync (triggerContext, attributedOb, attributes, cancellationToken);
				break;

			case XmlCompletionTrigger.AttributeValue:
				if (triggerContext.SpineParser.Spine.TryPeek (out XAttribute? att) && triggerContext.SpineParser.Spine.TryPeek (1, out IAttributedXObject? attributedObject)) {
					yield return GetAttributeValueCompletionsAsync (triggerContext, attributedObject, att, cancellationToken);
				}
				break;

			case XmlCompletionTrigger.Entity:
				yield return GetEntityCompletionsAsync (triggerContext, cancellationToken);
				break;

			case XmlCompletionTrigger.DocType:
			case XmlCompletionTrigger.DeclarationOrCDataOrComment:
				yield return GetDeclarationCompletionsAsync (triggerContext, cancellationToken);
				break;
			}
		}

		public Task<object> GetDescriptionAsync (IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
			=> Logger.InvokeAndLogExceptions (() => item.GetDocumentationAsync (session, token));

		public CompletionStartData InitializeCompletion (CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
			=> Logger.InvokeAndLogExceptions (
				() => {
					var spine = GetSpineParser (triggerLocation);
					return InitializeCompletion (trigger, triggerLocation, spine, token);
				});

		/// <summary>
		/// Determine whether the current location is a completion trigger point, and what its span is. Runs on the UI thread so must be fast.
		/// </summary>
		protected virtual CompletionStartData InitializeCompletion (CompletionTrigger trigger, SnapshotPoint triggerLocation, XmlSpineParser spineParser, CancellationToken token)
		{
			var reason = XmlCompletionTriggerContext.ConvertReason (trigger.Reason, trigger.Character);
			if (reason == XmlTriggerReason.Unknown) {
				return CompletionStartData.DoesNotParticipateInCompletion;
			}

			LogAttemptingCompletion (Logger, spineParser.CurrentState, spineParser.CurrentStateLength, trigger.Character, trigger.Reason);

			var (kind, spanStart, spanLength) = XmlCompletionTriggering.GetTriggerAndSpan (spineParser, reason, trigger.Character, new SnapshotTextSource (triggerLocation.Snapshot));

			if (kind != XmlCompletionTrigger.None) {
				return new CompletionStartData (CompletionParticipation.ProvidesItems, new SnapshotSpan (triggerLocation.Snapshot, spanStart, spanLength));
			}

			//TODO: closing tag completion after typing >

			return CompletionStartData.DoesNotParticipateInCompletion;
		}

		[LoggerMessage (EventId = 2, Level = LogLevel.Trace, Message = "Attempting completion for state '{state}'x{currentSpineLength}, character='{triggerChar}', trigger='{triggerReason}'")]
		static partial void LogAttemptingCompletion (ILogger logger, XmlParserState state, int currentSpineLength, char triggerChar, CompletionTriggerReason triggerReason);

		protected virtual Task<IList<CompletionItem>?> GetElementCompletionsAsync (TCompletionTriggerContext context, bool includeBracket, CancellationToken token)
			=> TaskCompleted (null);

		protected virtual Task<IList<CompletionItem>?> GetAttributeCompletionsAsync (TCompletionTriggerContext context, IAttributedXObject attributedObject, Dictionary<string, string> existingAttributes, CancellationToken token)
			=> TaskCompleted (null);

		protected virtual Task<IList<CompletionItem>?> GetAttributeValueCompletionsAsync (TCompletionTriggerContext context, IAttributedXObject attributedObject, XAttribute attribute, CancellationToken token)
			=> TaskCompleted (null);

		protected virtual Task<IList<CompletionItem>?> GetEntityCompletionsAsync (TCompletionTriggerContext context, CancellationToken token)
			=> TaskCompleted (null);

		protected virtual Task<IList<CompletionItem>?> GetDeclarationCompletionsAsync (TCompletionTriggerContext context, CancellationToken token)
			=> TaskCompleted (
				context.NodePath is not null && context.NodePath.Any (n => n is XElement)
					? new [] { cdataItemWithBracket, commentItemWithBracket }
					: new [] { commentItemWithBracket }
				);

		protected virtual Task<IList<CompletionItem>?> GetElementValueCompletionsAsync (TCompletionTriggerContext context, CancellationToken token)
			=> TaskCompleted (null);

		/// <summary>
		/// Get additional completions that are not handled by the XmlCompletionSource.
		protected virtual Task<IList<CompletionItem>?> GetAdditionalCompletionsAsync (TCompletionTriggerContext context, CancellationToken token)
			=> TaskCompleted (null);

		static Task<IList<CompletionItem>?> TaskCompleted (IList<CompletionItem>? items) => Task.FromResult (items);

		CompletionItem cdataItem, commentItem, prologItem;
		CompletionItem cdataItemWithBracket, commentItemWithBracket, prologItemWithBracket;
		CompletionItem[] entityItems;

		[MemberNotNull (
			nameof (cdataItem), nameof (commentItem), nameof (prologItem),
			nameof (cdataItemWithBracket), nameof (commentItemWithBracket), nameof (prologItemWithBracket),
			nameof (entityItems))]
		void InitializeBuiltInItems ()
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
			CompletionItem EntityItem (string name, string character)
			{
				name = $"&{name};";
				return new CompletionItem (name, this, XmlImages.Entity, ImmutableArray<CompletionFilter>.Empty, string.Empty, name, name, character, ImmutableArray<ImageElement>.Empty)
					.AddEntityDocumentation (character)
					.AddKind (XmlCompletionItemKind.Entity);
			}
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

		protected IList<CompletionItem> GetBuiltInEntityItems () => entityItems;

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

				string insertText = prefix + name;

				// force these to sort last, they're not very interesting values to browse as these tags are most likely already closed
				string sortText = "ZZZZZZ" + insertText;

				var item = new CompletionItem (insertText, this, XmlImages.ClosingTag, ImmutableArray<CompletionFilter>.Empty, "", insertText, sortText, insertText, ImmutableArray<ImageElement>.Empty)
					.AddClosingElementDocumentation (el, dedup.Count > 1)
					.AddKind (dedup.Count == 1? XmlCompletionItemKind.ClosingTag : XmlCompletionItemKind.MultipleClosingTags);
				item.Properties.AddProperty (typeof (List<XObject>), nodePath);
				yield return item;
			}
		}
	}
}
