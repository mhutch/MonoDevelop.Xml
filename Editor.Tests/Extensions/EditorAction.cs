// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace MonoDevelop.Xml.Editor.Tests.Extensions
{
	public static class EditorAction
	{
		public static IEnumerable<Action<IEditorCommandHandlerService>> Type (string text)
		{
			foreach (var c in text) {
				switch (c) {
				case '\n':

					yield return Enter;
					break;
				default:
					if (EnableDebugTrace) {
						LogTrace ($"Typing '{c}'");
					}
					yield return (commandService) => commandService.CheckAndExecute ((v, b) => new TypeCharCommandArgs (v, b, c));
					break;
				}
			}
		}

		public static void Enter (IEditorCommandHandlerService commandService)
		{
			if (EnableDebugTrace) {
				LogTrace ("Invoking return key");
			}
			commandService.CheckAndExecute ((v, b) => new ReturnKeyCommandArgs (v, b));
		}

		public static void InvokeCompletion (IEditorCommandHandlerService commandService)
		{
			if (EnableDebugTrace) {
				LogTrace ("Invoking completion");
			}
			commandService.CheckAndExecute ((v, b) => new InvokeCompletionListCommandArgs (v, b));
		}

		const string TraceID = "EditorAction.Trace";
		static bool EnableDebugTrace => CommandServiceExtensions.EnableDebugTrace;
		static void LogTrace (string message) => Console.WriteLine ($"{TraceID}: {message}");
	}
}
