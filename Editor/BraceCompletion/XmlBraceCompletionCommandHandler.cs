// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.BraceCompletion;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

using BF = System.Reflection.BindingFlags;

namespace MonoDevelop.Xml.Editor.BraceCompletion
{
	// The VS editor's brace completion infrastructure has limitations that we cannot work around, so instead of
	// implementing a brace completion context/session provider, we implement custom XML brace insertions and overtype behaviors
	//
	// the main limitation is that brace completion can only happen at the end of a line, or when the only remaining characters
	// on the line are part of active brace completion sessions. this means that you cannot get quote completion when typing an
	// attribute into an existing element.
	//
	// another limitation is that brace completion handlers from base content types are ignored. content types derived from
	// XmlCore (such as MSBuild) cannot implement their own brace completion behaviors while still inheriting XmlCore behaviors
	//
	// it's also difficult for commit handlers to create completion sessions. for example, when an attribute is committed
	// the commit handler may insert ="" quotes and move the caret between the quotes. it's not possible to create a brace
	// completion session so that the second quote becomes overtypeable.
	//
	// the one big downside to this is that we lose the little bit of editor UI that indicates that the character is
	// overtypeable. reimplementing is is nontrivial, especially across multiple platforms.
	//
	[Name (Name)]
	[Export (typeof (ICommandHandler))]
	[Order (After = PredefinedCompletionNames.CompletionCommandHandler, Before = "BraceCompletionCommandHandler")]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[TextViewRole (PredefinedTextViewRoles.Interactive)]
	class XmlBraceCompletionCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
	{
		const string Name = nameof (XmlBraceCompletionCommandHandler);

		public string DisplayName => Name;

		public CommandState GetCommandState (TypeCharCommandArgs args, Func<CommandState> nextCommandHandler) => nextCommandHandler ();

		public void ExecuteCommand (TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
		{
			var view = args.TextView;
			var buffer = args.SubjectBuffer;
			var openingPoint = view.Caret.Position.BufferPosition;
			char typedChar = args.TypedChar;

			// short circuit on the easy checks
			ITextSnapshot snapshot = openingPoint.Snapshot;
			if (!IsTriggerChar (typedChar)
				|| !IsBraceCompletionEnabled (view)
				|| !view.Selection.IsEmpty
				|| !XmlBackgroundParser.TryGetParser (snapshot.TextBuffer, out var parser)) {
				nextCommandHandler ();
				return;
			}

			// overtype
			if (IsQuoteChar(typedChar) && openingPoint > 0 && snapshot.Length > openingPoint && snapshot[openingPoint] == typedChar) {
				var spine = parser.GetSpineParser (openingPoint);
				if (spine.GetAttributeValueDelimiter () == typedChar) {
					if (snapshot[openingPoint - 1] != typedChar) {
						// in a quoted value typing its quote char over the end quote, and not immediately after start quote
						view.Caret.MoveTo (new SnapshotPoint (buffer.CurrentSnapshot, openingPoint + 1));
					}
					return;
				}
			}

			nextCommandHandler ();

			if (typedChar == '=') {
				var position = view.Caret.Position.BufferPosition;
				snapshot = position.Snapshot;
				if (position > 0 &&
					snapshot[position - 1] == '=' &&
					(position == snapshot.Length ||
						(position < snapshot.Length &&
						snapshot[position] is char next &&
						(next == ' ' || next == '>' || next == '/' || next == '\r' || next == '\n')))) {
					InsertDoubleQuotes ("\"\"", position);
				}
			}

			return;

			void InsertDoubleQuotes(string doubleQuotes, SnapshotPoint openingPoint)
			{
				var spine = parser.GetSpineParser (openingPoint);
				if (spine.IsExpectingAttributeQuote ()) {
					//TODO create an undo transition between the two chars
					buffer.Insert (openingPoint, doubleQuotes);
					view.Caret.MoveTo (new SnapshotPoint (buffer.CurrentSnapshot, openingPoint.Position + 1));
					return;
				}
			}
		}

		static bool IsBraceCompletionEnabled (ITextView textView)
		//=> textView.Properties.TryGetProperty ("BraceCompletionManager", out IBraceCompletionManager manager) && manager.Enabled;
		//HACK: VSMac as of 16.4 doesn't have IBraceCompletionManager in the assembly that the 16.4 nugets
		// say it's in, so we can't use it even when depending on 16.4. use reflection instead.
		{
			if (textView.Properties.TryGetProperty ("BraceCompletionManager", out object manager)) {
				var prop = braceManagerEnabledProp
					?? ( braceManagerEnabledProp =
						manager.GetType ().GetProperty ("Enabled", BF.Instance | BF.NonPublic | BF.Public)
					);
				return prop != null && prop.GetValue (manager) is bool b && b;
			}
			return true; // default to true if can't find BraceCompletionManager
		}

		static System.Reflection.PropertyInfo braceManagerEnabledProp;

		static bool IsQuoteChar (char ch) => ch == '"' || ch == '\'';
		static bool IsTriggerChar (char ch) => IsQuoteChar (ch) || ch == '=';
	}
}
