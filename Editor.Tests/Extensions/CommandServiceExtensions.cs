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
		/// <summary>
		/// Enables logging of additional trace information to debug nondeterministic test failures
		/// </summary>
		public static bool EnableDebugTrace { get; set; }

		public static void Type (this IEditorCommandHandlerService commandService, string text)
		{
			foreach (var c in text) {
				switch (c) {
				case '\n':
					Enter (commandService);
					break;
				default:
					if (EnableDebugTrace) {
						LogTrace ($"Typing '{c}'");
					}
					commandService.CheckAndExecute ((v, b) => new TypeCharCommandArgs (v, b, c));
					break;
				}
			}
		}

		public static void Enter (this IEditorCommandHandlerService commandService)
		{
			if (EnableDebugTrace) {
				LogTrace ("Invoking return key");
			}
			commandService.CheckAndExecute ((v, b) => new ReturnKeyCommandArgs (v, b));
		}

		public static void InvokeCompletion (this IEditorCommandHandlerService commandService)
		{
			if (EnableDebugTrace) {
				LogTrace ("Invoking completion");
			}
			commandService.CheckAndExecute ((v, b) => new InvokeCompletionListCommandArgs (v, b));
		}

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

			if (textView != null) {
				if (textView.Properties.TryGetProperty (typeof (IAsyncCompletionSession), out IAsyncCompletionSession session)) {
					if (EnableDebugTrace) {
						LogTrace ("Session open");
						RegisterTraceHandlers (session);
					}
					//ensure the computation is completed before we continue typing
					session.GetComputedItems (CancellationToken.None);
					LogTrace ("Session open");
				}
			} else{
				LogTrace ("Session not open");
			}
		}

		const string TraceID = "CommandServiceExtensions.Trace";

		static void LogTrace(string message) => Console.WriteLine ($"{TraceID}: {message}");

		static void RegisterTraceHandlers (IAsyncCompletionSession session)
		{
			if (session.Properties.TryGetProperty (TraceID, out bool hasHandler)) {
				return;
			}

			session.Properties.AddProperty (TraceID, true);
			session.Dismissed += (s, e) => {
				LogTrace ($"Session dismissed:\n{Environment.StackTrace}");
				LogTrace (Environment.StackTrace);
			};
			session.ItemCommitted += (s, e) => {
				LogTrace ($"Session committed '{e.Item.InsertText}':\n{Environment.StackTrace}");
			};
			session.ItemsUpdated += (s, e) => {
				LogTrace ($"Session updated");
			};
		}

		static Action Noop { get; } = new Action (() => { });
		static Func<CommandState> Unspecified { get; } = () => CommandState.Unspecified;
	}
}
