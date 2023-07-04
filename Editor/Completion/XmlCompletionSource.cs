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
		protected XmlParserProvider XmlParserProvider { get; }

		protected ITextView TextView { get; }

		protected ILogger Logger { get; }

		protected XmlCompletionSource (ITextView textView, ILogger logger, XmlParserProvider parserProvider)
		{
			XmlParserProvider = parserProvider;
			TextView = textView;
			Logger = logger;
			InitializeBuiltinItems ();
		}

		protected XmlBackgroundParser GetParser (ITextBuffer textBuffer) => XmlParserProvider.GetParser (textBuffer);

		protected XmlSpineParser GetSpineParser (SnapshotPoint snapshotPoint)
		{
			var backgroundParser = GetParser (snapshotPoint.Snapshot.TextBuffer);
			var spineParser = backgroundParser.GetSpineParser (snapshotPoint);
			return spineParser;
		}

		public async virtual Task<CompletionContext> GetCompletionContextAsync (IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
		{
			var tasks = GetCompletionTasks (session, trigger, triggerLocation, applicableToSpan, token).ToList ();

			await Task.WhenAll (tasks);

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

		IEnumerable<Task<IList<CompletionItem>?>> GetCompletionTasks (IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
		{
			yield return GetAdditionalCompletionsAsync (session, trigger, triggerLocation, applicableToSpan, token);

			var reason = ConvertReason (trigger.Reason, trigger.Character);
			if (reason == null) {
				yield break;
			}

			var parser = GetSpineParser (triggerLocation);

			// FIXME: cache the value from InitializeCompletion somewhere?
			var (kind, _) = XmlCompletionTriggering.GetTrigger (parser, reason.Value, trigger.Character);

			if (kind == XmlCompletionTrigger.None) {
				yield break;
			}

			List<XObject> nodePath = parser.GetNodePath (triggerLocation.Snapshot);
			session.Properties.AddProperty (typeof (XmlCompletionTrigger), kind);

			switch (kind) {
			case XmlCompletionTrigger.ElementValue:
				yield return GetElementValueCompletionsAsync (session, triggerLocation, nodePath, token);
				goto case XmlCompletionTrigger.Element;

			case XmlCompletionTrigger.Element:
				// if we're completing an existing element, remove it from the path
				// so we don't get completions for its children instead
				if (nodePath.Count > 0) {
					if (nodePath[nodePath.Count - 1] is XElement leaf && leaf.Name.Length == applicableToSpan.Length) {
						nodePath.RemoveAt (nodePath.Count - 1);
					}
				}
				//TODO: if it's on the first or second line and there's no DTD declaration, add the DTDs, or at least <!DOCTYPE
				//TODO: add snippets // MonoDevelop.Ide.CodeTemplates.CodeTemplateService.AddCompletionDataForFileName (DocumentContext.Name, list);
				yield return GetElementCompletionsAsync (session, triggerLocation, nodePath, reason == XmlTriggerReason.Invocation, token);
				break;

			case XmlCompletionTrigger.Attribute:
				if (parser.Spine.TryFind<IAttributedXObject> (maxDepth: 1) is not IAttributedXObject attributedOb) {
					throw new InvalidOperationException ("Did not find IAttributedXObject in stack for XmlCompletionTrigger.Attribute");
				}
				parser.Clone ().AdvanceUntilEnded ((XObject)attributedOb, triggerLocation.Snapshot, 1000);
				var attributes = attributedOb.Attributes.ToDictionary (StringComparer.OrdinalIgnoreCase);
				yield return GetAttributeCompletionsAsync (session, triggerLocation, nodePath, attributedOb, attributes, token);
				break;

			case XmlCompletionTrigger.AttributeValue:
				if (parser.Spine.TryPeek (out XAttribute? att) && parser.Spine.TryPeek (1, out IAttributedXObject? attributedObject)) {
					yield return GetAttributeValueCompletionsAsync (session, triggerLocation, nodePath, attributedObject, att, token);
				}
				break;

			case XmlCompletionTrigger.Entity:
				yield return GetEntityCompletionsAsync (session, triggerLocation, nodePath, token);
				break;

			case XmlCompletionTrigger.DocType:
			case XmlCompletionTrigger.DeclarationOrCDataOrComment:
				yield return GetDeclarationCompletionsAsync (session, triggerLocation, nodePath, token);
				break;
			}
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

			var spine = GetSpineParser (triggerLocation);

			LogAttemptingCompletion (Logger, spine.CurrentState, spine.CurrentStateLength, trigger.Character, trigger.Reason);

			var (kind, length) = XmlCompletionTriggering.GetTrigger (spine, reason.Value, trigger.Character);
			if (kind != XmlCompletionTrigger.None) {
				return new CompletionStartData (CompletionParticipation.ProvidesItems, new SnapshotSpan (triggerLocation.Snapshot, triggerLocation.Position - length, length));
			}

			//TODO: closing tag completion after typing >

			return CompletionStartData.DoesNotParticipateInCompletion;
		}

		[LoggerMessage (EventId = 2, Level = LogLevel.Trace, Message = "Attempting completion for state '{state}'x{currentSpineLength}, character='{triggerChar}', trigger='{triggerReason}'")]
		static partial void LogAttemptingCompletion (ILogger logger, XmlParserState state, int currentSpineLength, char triggerChar, CompletionTriggerReason triggerReason);

		protected virtual Task<IList<CompletionItem>?> GetElementCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			bool includeBracket,
			CancellationToken token
			)
			=> Task.FromResult<IList<CompletionItem>?> (null);

		protected virtual Task<IList<CompletionItem>?> GetAttributeCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			IAttributedXObject attributedObject,
			Dictionary<string, string> existingAtts,
			CancellationToken token
			)
			=> Task.FromResult<IList<CompletionItem>?> (null);

		protected virtual Task<IList<CompletionItem>?> GetAttributeValueCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			IAttributedXObject attributedObject,
			XAttribute attribute,
			CancellationToken token
			)
			=> Task.FromResult<IList<CompletionItem>?> (null);

		protected virtual Task<IList<CompletionItem>?> GetEntityCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			CancellationToken token
			)
			=> Task.FromResult<IList<CompletionItem>?> (null);

		protected virtual Task<IList<CompletionItem>?> GetDeclarationCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			CancellationToken token
			)
			=> Task.FromResult<IList<CompletionItem>?> (
				nodePath.Any (n => n is XElement)
					? new [] { cdataItemWithBracket, commentItemWithBracket }
					: new [] { commentItemWithBracket }
				);

		protected virtual Task<IList<CompletionItem>?> GetElementValueCompletionsAsync (
			IAsyncCompletionSession session,
			SnapshotPoint triggerLocation,
			List<XObject> nodePath,
			CancellationToken token) => Task.FromResult<IList<CompletionItem>?> (null);

		protected virtual Task<IList<CompletionItem>?> GetAdditionalCompletionsAsync (
			IAsyncCompletionSession session,
			CompletionTrigger trigger,
			SnapshotPoint triggerLocation,
			SnapshotSpan applicableToSpan,
			CancellationToken token) => Task.FromResult<IList<CompletionItem>?> (null);

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
