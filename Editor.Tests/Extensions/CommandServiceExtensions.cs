// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace MonoDevelop.Xml.Editor.Tests.Extensions
{
	public static class CommandServiceExtensions
	{
		/// <summary>
		/// Enables logging of additional trace information to debug nondeterministic test failures
		/// </summary>
		public static bool EnableDebugTrace { get; set; }

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

			// There is a race here where a completion session may have triggered on the UI thread
			// but the task to compute the completion items is still running. This can cause the
			// completion to be dismissed before the items are computed.
			//
			// We mitigate this by checking if a session is open and attempting to wait for it.
			if (textView != null) {
				if (textView.Properties.TryGetProperty (typeof (IAsyncCompletionSession), out IAsyncCompletionSession session)) {
					if (EnableDebugTrace) {
						LogTrace ("Session open");
						RegisterTraceHandlers (session);
					}

					// The first time we see the session, wait for a short time to allow it to initialize,
					// otherwise completion will dismiss via TryDismissSafelyAsync if the snapshot is updated
					// before the session is initialized.
					//
					// This wait is not necessary on my local machine, but it mitigates nondeterministic
					// failures on GitHub Actions CI.
					//
					// Note that polling IAsyncCompletionSessionOperations.IsStarted does not help.
					if (IsGitHubActions && !session.Properties.TryGetProperty (HasWaitedForCompletionToInitializeKey, out bool hasWaited)) {
						session.Properties.AddProperty (HasWaitedForCompletionToInitializeKey, true);
						Thread.Sleep (500);
					}

					// Block until the computation is updated before we run more actions. This makes the
					// test reliable on my local machine.
					session.GetComputedItems (CancellationToken.None);
				}
			} else{
				LogTrace ("Session not open");
			}
		}

		const string TraceID = "CommandServiceExtensions.Trace";

		static readonly object HasWaitedForCompletionToInitializeKey = new();

		static readonly bool IsGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != null;

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
