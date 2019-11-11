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
			var openingPoint = args.TextView.Caret.Position.BufferPosition;

			// short circuit on the easy checks
			ITextSnapshot snapshot = openingPoint.Snapshot;
			if (!IsQuoteChar (args.TypedChar)
				|| !IsBraceCompletionEnabled (args.TextView)
				|| !args.TextView.Selection.IsEmpty
				|| !XmlBackgroundParser.TryGetParser (snapshot.TextBuffer, out var parser)) {
				nextCommandHandler ();
				return;
			}

			// overtype
			if (snapshot.Length > openingPoint.Position && snapshot[openingPoint] == args.TypedChar) {
				var spine = parser.GetSpineParser (openingPoint);
				bool isOverType =
					// in a quoted value typing its quote char over the end quote
					spine.GetAttributeValueDelimiter () == args.TypedChar
					// typing a quote after after the attribute's equals sign
					|| spine.IsExpectingAttributeQuote ();
				if (isOverType) {
					using (var edit = args.SubjectBuffer.CreateEdit ()) {
						edit.Replace (openingPoint.Position, 1, args.TypedChar.ToString ());
						edit.Apply ();
					}
					args.TextView.Caret.MoveTo (new SnapshotPoint (args.SubjectBuffer.CurrentSnapshot, openingPoint.Position + 1));
					return;
				}
			}

			// auto insertion of matching quote
			if (snapshot.Length == openingPoint.Position || !IsQuoteChar (snapshot[openingPoint])) {
				var spine = parser.GetSpineParser (openingPoint);
				// if we're in a state where we expect an attribute value quote
				// and we're able to walk the parser to the end of of the line without completing the attribute
				// and without ending in an incomplete attribute value, then it's reasonable to auto insert a matching quote
				if (spine.IsExpectingAttributeQuote ()) {
					var att = (XAttribute)spine.Spine.Peek ();
					if (AdvanceParserUntilConditionOrEol (spine, snapshot, p => att.Value != null, 1000) && att.Value == null && !(spine.CurrentState is XmlAttributeValueState)) {
						using (var edit = args.SubjectBuffer.CreateEdit ()) {
							//TODO create an undo transition between the two chars
							edit.Insert (openingPoint.Position, new string (args.TypedChar, 2));
							edit.Apply ();
						}
						args.TextView.Caret.MoveTo (new SnapshotPoint (args.SubjectBuffer.CurrentSnapshot, openingPoint.Position + 1));
						return;
					}
				}
			}

			nextCommandHandler ();
			return;
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
			return false;
		}

		static System.Reflection.PropertyInfo braceManagerEnabledProp;

		static bool IsQuoteChar (char ch) => ch == '"' || ch == '\'';

		static bool AdvanceParserUntilConditionOrEol (XmlSpineParser parser,
			ITextSnapshot snapshot,
			Func<XmlParser, bool> condition,
			int maxChars)
		{
			if (parser.Position == snapshot.Length) {
				return true;
			}
			int maxPos = Math.Min (snapshot.Length, Math.Max (parser.Position + maxChars, maxChars));
			while (parser.Position < maxPos) {
				char c = snapshot[parser.Position];
				if (c == '\r' || c == '\n') {
					return true;
				}
				parser.Push (c);
				if (condition (parser)) {
					return true;
				}
			}
			return false;
		}
	}
}
