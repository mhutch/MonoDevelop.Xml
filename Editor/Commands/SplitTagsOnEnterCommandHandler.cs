// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MonoDevelop.Xml.Editor.Commands
{
	[Name (Name)]
	[Export (typeof (ICommandHandler))]
	[Order (Before = PredefinedCompletionNames.CompletionCommandHandler)]
	[ContentType(XmlContentTypeNames.XmlCore)]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	class SplitTagsOnEnterCommandHandler : IChainedCommandHandler<ReturnKeyCommandArgs>
	{
		[Import]
		ISmartIndentationService SmartIndentService { get; set; }

		[Import]
		ITextBufferUndoManagerProvider UndoManagerProvider { get; set; }


		[Import]
		IAsyncCompletionBroker CompletionBroker{ get; set; }

		const string Name = nameof (SplitTagsOnEnterCommandHandler);

		public string DisplayName => Name;

		public void ExecuteCommand (ReturnKeyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
		{
			if (SmartIndentService == null || CompletionBroker.IsCompletionActive (args.TextView)) {
				nextCommandHandler ();
				return;
			}

			var caretPos = args.TextView.Caret.Position.BufferPosition;
			int p = caretPos.Position;
			var s = caretPos.Snapshot;

			// we could be smarter about actually analyzing the XML and creating good undo transactions.
			// right now it just looks for the caret being between "></" and moves the rest of the line
			// onto an indented new line before letting the normal handler run
			bool betweenTags = p > 0 && (p + 3) < s.Length && s[p - 1] == '>' && s[p] == '<' && s[p + 1] == '/';
			if (!betweenTags) {
				nextCommandHandler ();
				return;
			}

			var undoManager = UndoManagerProvider.GetTextBufferUndoManager (args.SubjectBuffer);
			using (var transaction = undoManager.TextBufferUndoHistory.CreateTransaction ("Split Tags")) {
				string lineBreakText = null;
				var currentLine = s.GetLineFromPosition (p);
				if (args.TextView.Options.GetReplicateNewLineCharacter ()) {
					lineBreakText = currentLine.GetLineBreakText ();
				}
				if (string.IsNullOrEmpty (lineBreakText)) {
					lineBreakText = args.TextView.Options.GetNewLineCharacter ();
				}

				var edit = args.SubjectBuffer.CreateEdit ();
				edit.Insert (p, lineBreakText);
				s = edit.Apply ();

				var nextLine = s.GetLineFromLineNumber (currentLine.LineNumber + 1);
				var indent = SmartIndentService.GetDesiredIndentation (args.TextView, nextLine);
				if (indent != null) {
					edit = args.SubjectBuffer.CreateEdit ();
					edit.Insert (nextLine.Start.Position, GetIndentString (indent.Value, args.TextView.Options));
					s = edit.Apply ();
				}
				transaction.Complete ();
			}

			// move the caret back to the original location before letting the rest of the handlers run
			args.TextView.Caret.MoveTo (new SnapshotPoint (s, p));

			nextCommandHandler ();
		}

		static string GetIndentString (int indent, IEditorOptions options)
		{
			if (options.IsConvertTabsToSpacesEnabled ()) {
				return new string (' ', indent);
			}
			var tabSize = options.GetTabSize ();
			int tabs = indent / tabSize;
			int spaces = indent - tabs * tabSize;
			var indentStr = new string ('\t', tabs);
			if (spaces > 0) {
				indentStr += new string (' ', spaces);
			}
			return indentStr;
		}

		public CommandState GetCommandState (ReturnKeyCommandArgs args, Func<CommandState> nextCommandHandler)
			=> nextCommandHandler ();
	}
}
