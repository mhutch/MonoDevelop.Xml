// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace MonoDevelop.Xml.Tests.EditorTestHelpers
{
	public static class CommandServiceExtensions
	{
		public static void Type (this IEditorCommandHandlerService commandService, string text)
		{
			foreach (var c in text) {
				commandService.CheckAndExecute ((v, b) => new TypeCharCommandArgs (v, b, c));
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
			if (commandService.GetCommandState (argsFactory, Unspecified).IsAvailable) {
				commandService.Execute (argsFactory, Noop);
			} else {
				throw new InvalidOperationException ($"No handler available for `{typeof (T)}`");
			}
		}

		static Action Noop { get; } = new Action (() => { });
		static Func<CommandState> Unspecified { get; } = () => CommandState.Unspecified;
	}
}
