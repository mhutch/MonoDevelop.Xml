// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Logging;
using MonoDevelop.Xml.Editor.Options;
using MonoDevelop.Xml.Editor.Parsing;
using MonoDevelop.Xml.Logging;

namespace MonoDevelop.Xml.Editor.Commands
{
	[Name (Name)]
	[Export (typeof (ICommandHandler))]
	[Order (Before = PredefinedCompletionNames.CompletionCommandHandler)]
	[ContentType(XmlContentTypeNames.XmlCore)]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	class AutoClosingTagCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
	{
		const string Name = "Closing Tag Completion";

		[ImportingConstructor]
		public AutoClosingTagCommandHandler (XmlParserProvider parserProvider, IAsyncCompletionBroker completionBroker, IEditorLoggerFactory loggerFactory)
		{
			this.parserProvider = parserProvider;
			this.completionBroker = completionBroker;
			this.loggerFactory = loggerFactory;
		}

		readonly IAsyncCompletionBroker completionBroker;
		readonly IEditorLoggerFactory loggerFactory;
		readonly XmlParserProvider parserProvider;

		public string DisplayName => Name;

		public void ExecuteCommand (TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
		{
			// The completion handler both commits the existing selection and re-triggers,
			// however, it chains to other handlers _before_ it commits, so its undo comes
			// after them. although that's desirable for character insertion and brace completions
			// it's not what we want for end tag completion, as end tag completion is based
			// on the committed value and therefore should be undone before it.
			//
			// Hence this handler comes _before_ the completion handler, but very much like
			// the completion handler itself, it chains before making its own edits.
			nextCommandHandler ();

			if (args.TypedChar == '>' && args.TextView.Options.GetAutoInsertClosingTag ()) {
				try {
					InsertCloseTag (args, executionContext);
				} catch (Exception ex) {
					loggerFactory.GetLogger<AutoClosingTagCommandHandler> (args.TextView).LogInternalException (ex);
				}
			}
		}

		void InsertCloseTag (TypeCharCommandArgs args, CommandExecutionContext executionContext)
		{
			if (!parserProvider.TryGetParser (args.SubjectBuffer, out var parser)) {
				return;
			}

			var multiSelectionBroker = args.TextView.GetMultiSelectionBroker ();
			if (multiSelectionBroker.HasMultipleSelections) {
				return;
			}

			var position = args.TextView.Caret.Position.BufferPosition;
			var spineParser = parser.GetSpineParser (position);
			var el = spineParser.Spine.Peek () as XElement;
			if (el == null || !el.IsEnded || !el.IsNamed || el.Span.End != position.Position) {
				return;
			}

			el = (XElement) spineParser.Spine.Peek ();
			spineParser.AdvanceUntilClosed (el, position.Snapshot);

			// also check for orphaned closing tags in this element's parent
			// this is not as accurate as the tree parse we did above as it uses
			// the last parse result, which might be a little stale
			var lastParseResult = parser.LastOutput;
			if (lastParseResult != null && el.Parent != null) {
				if (lastParseResult.XDocument.FindAtOffset (el.Parent.Span.Start + 1) is XContainer parent) {
					foreach (var n in parent.Nodes) {
						if (n is XClosingTag) {
							return;
						}
					}
				}
			}

			// When the completion handler triggers a new completion session immediately after
			// committing, it created a tracking ApplicableToSpan. When we make our edit, the
			// tracking spand expands to include it, so when the new completion session is
			// committed, it erases our edit.
			//
			// Since we cannot update the ApplicableToSpan at this point, we explicitly dismiss and
			// re-trigger completion instead.

			var completionSession = completionBroker.GetSession (args.TextView);
			if (completionSession != null) {
				completionSession.Dismiss ();
			}

			var bufferEdit = args.SubjectBuffer.CreateEdit ();

			// if there's an extra > after committing due to VS losing track of overtype, delete it
			ITextSnapshot snapshot = args.SubjectBuffer.CurrentSnapshot;
			if (snapshot.Length > position && snapshot[position] == '>') {
				bufferEdit.Delete (new Span (position, 1));
			}

			bufferEdit.Insert (position, $"</{el.Name.FullName}>");

			bufferEdit.Apply ();
			snapshot = args.SubjectBuffer.CurrentSnapshot;

			args.TextView.Caret.MoveTo (new SnapshotPoint (snapshot, position));

			if (completionSession != null) {
				var trigger = new CompletionTrigger (
					CompletionTriggerReason.Insertion, args.SubjectBuffer.CurrentSnapshot, '>');
				var location = args.TextView.Caret.Position.BufferPosition;
				var token = executionContext.OperationContext.UserCancellationToken;

				completionSession = completionBroker.GetSession (args.TextView);
				if (completionSession == null) {
					completionSession = completionBroker.TriggerCompletion (args.TextView, trigger, location, token);
				}

				completionSession?.OpenOrUpdate (trigger, location, token);
			}

			return;
		}

		public CommandState GetCommandState (TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
			=> nextCommandHandler ();
	}
}
