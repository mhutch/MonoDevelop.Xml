// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Completion
{
	public abstract class XmlCompletionSource<TParser, TResult> : IAsyncCompletionSource where TResult : XmlParseResult where TParser : XmlBackgroundParser<TResult>, new()
	{
		protected ITextView TextView { get; }

		protected XmlCompletionSource (ITextView textView)
		{
			TextView = textView;
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

			var parser = BackgroundParser<TResult>.GetParser<TParser> ((ITextBuffer2)triggerLocation.Snapshot.TextBuffer);
			var spine = parser.GetSpineParser (triggerLocation);

			// FIXME: cache the value from InitializeCompletion somewhere?
			var (kind, _) = XmlCompletionTriggering.GetTrigger (spine, reason.Value, trigger.Character);

			if (kind != XmlCompletionTrigger.None) {
				List<XObject> nodePath = GetNodePath (spine, triggerLocation.Snapshot);

				session.Properties.AddProperty (typeof (XmlCompletionTrigger), kind);

				switch (kind) {
				case XmlCompletionTrigger.Element:
				case XmlCompletionTrigger.ElementWithBracket:
					// if we're completing an existing element, remove it from the path
					// so we don't get completions for its children instead
					if (nodePath.Count > 0) {
						var lastNode = nodePath[nodePath.Count - 1] as XElement;
						if (lastNode != null && lastNode.Name.Length == applicableToSpan.Length) {
							nodePath.RemoveAt (nodePath.Count - 1);
						}
					}
					//TODO: if it's on the first or second line and there's no DTD declaration, add the DTDs, or at least <!DOCTYPE
					//TODO: add snippets // MonoDevelop.Ide.CodeTemplates.CodeTemplateService.AddCompletionDataForFileName (DocumentContext.Name, list);
					return await GetElementCompletionsAsync (session, triggerLocation, nodePath, kind == XmlCompletionTrigger.ElementWithBracket, token);

				case XmlCompletionTrigger.Attribute:
					IAttributedXObject attributedOb = (spine.Nodes.Peek () as IAttributedXObject) ?? spine.Nodes.Peek (1) as IAttributedXObject;
					return await GetAttributeCompletionsAsync (session, triggerLocation, nodePath, attributedOb, GetExistingAttributes (spine, triggerLocation.Snapshot, attributedOb), token);

				case XmlCompletionTrigger.AttributeValue:
					if (spine.Nodes.Peek () is XAttribute att && spine.Nodes.Peek (1) is IAttributedXObject attributedObject) {
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

			var parser = BackgroundParser<TResult>.GetParser<TParser> ((ITextBuffer2)triggerLocation.Snapshot.TextBuffer);
			var spine = parser.GetSpineParser (triggerLocation);

			LoggingService.LogDebug (
				"Attempting completion for state '{0}'x{1}, character='{2}', trigger='{3}'",
				spine.CurrentState, spine.CurrentStateLength, trigger.Character, trigger
			);

			var (kind, length) = XmlCompletionTriggering.GetTrigger (spine, reason.Value, trigger.Character);
			if (kind != XmlCompletionTrigger.None) {
				return new CompletionStartData (CompletionParticipation.ProvidesItems, new SnapshotSpan (triggerLocation.Snapshot, triggerLocation.Position - length, length));
			}

			//TODO: closing tag completion after typing >

			return CompletionStartData.DoesNotParticipateInCompletion;
		}

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

		CompletionContext CreateCompletionContext (IEnumerable<CompletionItem> items)
			=> new CompletionContext (ImmutableArray<CompletionItem>.Empty.AddRange (items), null, InitialSelectionHint.SoftSelection);

		protected List<XObject> GetNodePath (XmlParser spine, ITextSnapshot snapshot)
		{
			var path = new List<XObject> (spine.Nodes);

			//remove the root XDocument
			path.RemoveAt (path.Count - 1);

			//complete incomplete XName if present
			if (spine.CurrentState is XmlNameState && path[0] is INamedXObject) {
				path[0] = path[0].ShallowCopy ();
				XName completeName = GetCompleteName (spine, snapshot);
				((INamedXObject)path[0]).Name = completeName;
			}
			path.Reverse ();
			return path;
		}

		protected XName GetCompleteName (XmlParser spine, ITextSnapshot snapshot)
		{
			Debug.Assert (spine.CurrentState is XmlNameState);

			int end = spine.Position;
			int start = end - spine.CurrentStateLength;
			int mid = -1;

			int limit = Math.Min (snapshot.Length, end + 35);

			//try to find the end of the name, but don't go too far
			for (; end < limit; end++) {
				char c = snapshot[end];

				if (c == ':') {
					if (mid == -1)
						mid = end;
					else
						break;
				} else if (!XmlChar.IsNameChar (c))
					break;
			}

			if (mid > 0 && end > mid + 1) {
				return new XName (snapshot.GetText (start, mid - start), snapshot.GetText (mid + 1, end - mid - 1));
			}
			return new XName (snapshot.GetText (start, end - start));
		}

		static Dictionary<string, string> GetExistingAttributes (XmlParser spineParser, ITextSnapshot snapshot, IAttributedXObject attributedOb)
		{
			// clone parser to avoid modifying state
			spineParser = (XmlParser)((ICloneable)spineParser).Clone ();

			// parse rest of element to get all attributes
			for (int i = spineParser.Position; i < snapshot.Length; i++) {
				spineParser.Push (snapshot[i]);

				var currentState = spineParser.CurrentState;
				switch (spineParser.CurrentState) {
				case XmlAttributeState _:
				case XmlAttributeValueState _:
				case XmlTagState _:
					continue;
				case XmlNameState _:
					if (currentState.Parent is XmlAttributeState) {
						continue;
					}
					break;
				}
				break;
			}

			var existingAtts = new Dictionary<string, string> (StringComparer.OrdinalIgnoreCase);
			foreach (XAttribute a in attributedOb.Attributes) {
				existingAtts[a.Name.FullName] = a.Value ?? string.Empty;
			}

			return existingAtts;
		}

		protected TParser GetParser () => BackgroundParser<TResult>.GetParser<TParser> ((ITextBuffer2)TextView.TextBuffer);

		protected XmlParser GetSpineParser (SnapshotPoint point) => GetParser ().GetSpineParser (point);

		protected string GetAttributeOrElementValueToCaret (XmlParser spineAtCaret, SnapshotPoint caretPosition)
		{
			int currentPosition = caretPosition.Position;
			int lineStart = caretPosition.Snapshot.GetLineFromPosition (currentPosition).Start.Position;
			int expressionStart = currentPosition - spineAtCaret.CurrentStateLength;
			if (XmlAttributeValueState.GetDelimiterChar (spineAtCaret).HasValue) {
				expressionStart += 1;
			}
			int start = Math.Max (expressionStart, lineStart);
			var expression = caretPosition.Snapshot.GetText (start, currentPosition - start);
			return expression;
		}

		CompletionItem cdataItem, commentItem, prologItem;
		CompletionItem cdataItemWithBracket, commentItemWithBracket, prologItemWithBracket;
		CompletionItem[] entityItems;

		void InitializeBuiltinItems ()
		{
			cdataItem = new CompletionItem ("![CDATA[", this, XmlImages.Declaration)
					.AddDocumentation ("XML character data")
					.AddKind (XmlCompletionItemKind.CData);

			commentItem = new CompletionItem ("!--", this, XmlImages.Declaration)
				.AddDocumentation ("XML comment")
				.AddKind (XmlCompletionItemKind.Comment);

			//TODO: commit $"?xml version=\"1.0\" encoding=\"{encoding}\" ?>"
			prologItem = new CompletionItem ("?xml", this, XmlImages.Declaration)
				.AddDocumentation ("XML prolog")
				.AddKind (XmlCompletionItemKind.Prolog);

			cdataItemWithBracket = new CompletionItem ("<![CDATA[", this, XmlImages.Declaration)
					.AddDocumentation ("XML character data")
					.AddKind (XmlCompletionItemKind.CData);

			commentItemWithBracket = new CompletionItem ("<!--", this, XmlImages.Declaration)
				.AddDocumentation ("XML comment")
				.AddKind (XmlCompletionItemKind.Comment);

			//TODO: commit $"?xml version=\"1.0\" encoding=\"{encoding}\" ?>"
			prologItemWithBracket = new CompletionItem ("<?xml", this, XmlImages.Declaration)
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

				string name = el.Name.FullName;
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
