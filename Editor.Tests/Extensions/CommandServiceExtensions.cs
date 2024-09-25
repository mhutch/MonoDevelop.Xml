// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace MonoDevelop.Xml.Editor.Tests.Extensions
{
	public static class CommandServiceExtensions
	{
		public static void Type (this IEditorCommandHandlerService commandService, string text)
		{
			foreach (var c in text) {
				switch (c) {
				case '\n':
					Enter (commandService);
					break;
				default:
					System.Console.WriteLine($"Typing '{c}'");
					commandService.CheckAndExecute ((v, b) => new TypeCharCommandArgs (v, b, c));
					break;
				}
			}
		}

		public static void Enter (this IEditorCommandHandlerService commandService)
			=> commandService.CheckAndExecute ((v, b) => new ReturnKeyCommandArgs (v, b));

		public static void InvokeCompletion (this IEditorCommandHandlerService commandService)
			=> commandService.CheckAndExecute ((v, b) => new InvokeCompletionListCommandArgs (v, b));

		public static void CheckAndExecute<T> (
			this IEditorCommandHandlerService commandService,
			Func<ITextView, ITextBuffer, T> argsFactory) where T : EditorCommandArgs
		{
			ITextView textView = null;
			Func<ITextView, ITextBuffer, T> capturingArgsFactory = (v, b) => { textView = v; return argsFactory (v, b); };
			if (commandService.GetCommandState (argsFactory, Unspecified).IsAvailable) {
				commandService.Execute (capturingArgsFactory, Noop);
			} else {
				throw new InvalidOperationException ($"No handler available for `{typeof (T)}`");
			}

			//ensure the computation is completed before we continue typing
			if (textView != null) {
				if (textView.Properties.TryGetProperty (typeof (IAsyncCompletionSession), out IAsyncCompletionSession session)) {
					session.GetComputedItems (CancellationToken.None);
					Console.WriteLine("Session open");
				}
			} else{
				Console.WriteLine("Session not open");
			}
		}

		static Action Noop { get; } = new Action (() => { });
		static Func<CommandState> Unspecified { get; } = () => CommandState.Unspecified;
	}
}
