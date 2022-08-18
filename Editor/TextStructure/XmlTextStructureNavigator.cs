// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.TextStructure
{
	class XmlTextStructureNavigator : ITextStructureNavigator
	{
		readonly ITextBuffer textBuffer;
		readonly ITextStructureNavigator codeNavigator;
		readonly XmlParserProvider parserProvider;

		public XmlTextStructureNavigator (ITextBuffer textBuffer, XmlTextStructureNavigatorProvider provider)
		{
			this.textBuffer = textBuffer;
			parserProvider = provider.ParserProvider;

			codeNavigator = provider.NavigatorService.CreateTextStructureNavigator (
				textBuffer,
				provider.ContentTypeRegistry.GetContentType (StandardContentTypeNames.Code)
			);
		}

		public IContentType ContentType => textBuffer.ContentType;

		public TextExtent GetExtentOfWord (SnapshotPoint currentPosition) => codeNavigator.GetExtentOfWord (currentPosition);

		public SnapshotSpan GetSpanOfFirstChild (SnapshotSpan activeSpan) => codeNavigator.GetSpanOfFirstChild (activeSpan);

		public SnapshotSpan GetSpanOfNextSibling (SnapshotSpan activeSpan) => codeNavigator.GetSpanOfNextSibling (activeSpan);

		public SnapshotSpan GetSpanOfPreviousSibling (SnapshotSpan activeSpan) => codeNavigator.GetSpanOfPreviousSibling (activeSpan);

		enum SelectionLevel
		{
			Self,
			Name,
			Content,
			OuterElement,
			Document,
			Attributes
		}

		public SnapshotSpan GetSpanOfEnclosing (SnapshotSpan activeSpan)
		{
			if (!parserProvider.TryGetParser (activeSpan.Snapshot.TextBuffer, out var parser)) {
				return codeNavigator.GetSpanOfEnclosing (activeSpan);
			}

			// use last parse if it's up to date, which is most likely will be
			// else use a spine from the end of the selection and update as needed
			var lastParse = parser.LastOutput;
			List<XObject> nodePath;
			XmlSpineParser? spine = null;
			if (lastParse != null && lastParse.TextSnapshot.Version.VersionNumber == activeSpan.Snapshot.Version.VersionNumber) {
				var n = lastParse.XDocument.FindAtOrBeforeOffset (activeSpan.Start.Position);
				nodePath = n.GetPath ();
			} else {
				spine = parser.GetSpineParser (activeSpan.Start);
				nodePath = spine.AdvanceToNodeEndAndGetNodePath (activeSpan.Snapshot);
			}

			// this is a little odd because it was ported from MonoDevelop, where it has to maintain its own stack of state
			// for contract selection. it describes the current semantic selection as a node path, the index of the node in that path
			// that's selected, and the kind of selection that node has.
			int selectedNodeIndex = nodePath.Count;
			SelectionLevel selectionLevel = default;

			// keep on expanding the selection until we find one that contains the current selection but is a little bigger
			while (ExpandSelection (nodePath, spine, activeSpan, ref selectedNodeIndex, ref selectionLevel)) {
				
				var selectionSpan = GetSelectionSpan (activeSpan.Snapshot, nodePath, ref selectedNodeIndex, ref selectionLevel);
				if (selectionSpan is TextSpan s && s.Start <= activeSpan.Start && s.End >= activeSpan.End && s.Length > activeSpan.Length) {
					var selectionSnapshotSpan = new SnapshotSpan (activeSpan.Snapshot, s.Start, s.Length);

					// if we're in content, the code navigator may be able to make a useful smaller expansion
					if (selectionLevel == SelectionLevel.Content) {
						var codeNavigatorSpan = codeNavigator.GetSpanOfEnclosing (activeSpan);
						if (selectionSnapshotSpan.Contains (codeNavigatorSpan)) {
							return codeNavigatorSpan;
						}
					}
					return selectionSnapshotSpan;
				}
			}

			return codeNavigator.GetSpanOfEnclosing (activeSpan);
		}

		TextSpan? GetSelectionSpan (ITextSnapshot snapshot, List<XObject> nodePath, ref int index, ref SelectionLevel level)
		{
			if (index < 0 || index >= nodePath.Count) {
				return null;
			}
			var current = nodePath[index];
			switch (level) {
			case SelectionLevel.Self:
				return current.Span;
			case SelectionLevel.OuterElement:
				var element = (XElement)current;
				return element.OuterSpan;
			case SelectionLevel.Name:
				return (current as INamedXObject)?.NameSpan;
			case SelectionLevel.Content:
				if (current is XElement el) {
					return el.InnerSpan;
				}
				if (current is XAttribute att) {
					return att.ValueSpan;
				}
				if (current is XText text) {
					return text.Span;
				}
				return null;
			case SelectionLevel.Document:
				return new TextSpan (0, snapshot.Length);
			case SelectionLevel.Attributes:
				return (current as IAttributedXObject)?.GetAttributesSpan ();
			}
			throw new InvalidOperationException ();
		}

		static bool ExpandSelection (List<XObject> nodePath, XmlSpineParser? spine, SnapshotSpan activeSpan, ref int index, ref SelectionLevel level)
		{
			if (index - 1 < 0) {
				return false;
			}

			//if an index is selected, we may need to transition level rather than transitioning index
			if (index < nodePath.Count) {
				var current = nodePath[index];
				if (current is XElement element) {
					switch (level) {
					case SelectionLevel.Self:
						if (spine is not null && !spine.AdvanceUntilClosed (element, activeSpan.Snapshot, 5000)) {
							return false;
						}
						if (!element.IsSelfClosing) {
							level = SelectionLevel.OuterElement;
							return true;
						}
						break;
					case SelectionLevel.Content:
						level = SelectionLevel.OuterElement;
						return true;
					case SelectionLevel.Name:
						level = SelectionLevel.Self;
						return true;
					case SelectionLevel.Attributes:
						level = SelectionLevel.Self;
						return true;
					}
				} else if (current is XAttribute) {
					switch (level) {
					case SelectionLevel.Name:
					case SelectionLevel.Content:
						level = SelectionLevel.Self;
						return true;
					}
				} else if (level == SelectionLevel.Name) {
					level = SelectionLevel.Self;
					return true;
				} else if (level == SelectionLevel.Document) {
					return false;
				}
			}

			//advance up the node path
			index--;
			var newNode = nodePath[index];

			//determine the starting selection level for the new node
			if (newNode is XDocument) {
				level = SelectionLevel.Document;
				return true;
			}

			if (spine != null && !spine.AdvanceUntilEnded (newNode, activeSpan.Snapshot, 5000)) {
				return false;
			}

			bool ContainsSelection (TextSpan span) => activeSpan.Start >= span.Start && activeSpan.End <= span.End;

			if (ContainsSelection (newNode.Span)) {
				if ((newNode as INamedXObject)?.NameSpan is TextSpan nr && ContainsSelection (nr)) {
					level = SelectionLevel.Name;
					return true;
				}
				if (newNode is XAttribute attribute) {
					var valRegion = attribute.ValueSpan;
					if (ContainsSelection (valRegion)) {
						level = SelectionLevel.Content;
						return true;
					}
				}
				if (newNode is XText) {
					level = SelectionLevel.Content;
					return true;
				}
				if (newNode is XElement xElement && xElement.Attributes.Count > 1) {
					if (xElement.GetAttributesSpan () is TextSpan attsSpan && ContainsSelection (attsSpan)) {
						level = SelectionLevel.Attributes;
						return true;
					}
				}
				level = SelectionLevel.Self;
				return true;
			}

			if (spine != null && !spine.AdvanceUntilClosed (newNode, activeSpan.Snapshot, 5000)) {
				return false;
			}

			if (newNode is XElement el && el.ClosingTag != null) {
				if (el.IsSelfClosing) {
					level = SelectionLevel.Self;
					return true;
				}
				if (el.InnerSpan is TextSpan innerSpan && ContainsSelection (innerSpan)) {
					level = SelectionLevel.Content;
					return true;
				}
				level = SelectionLevel.OuterElement;
				return true;
			}

			level = SelectionLevel.Self;
			return true;
		}
	}
}
