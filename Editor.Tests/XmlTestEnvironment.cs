// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.MiniEditor;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;

using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace MonoDevelop.Xml.Editor.Tests
{
	public class XmlTestEnvironment
	{
		EditorEnvironment? editorEnvironment;

		static readonly object initLock = new ();
		static Task<XmlTestEnvironment>? initTask;

		[Export]
		public static JoinableTaskContext? MefJoinableTaskContext = null;

		public static EditorCatalog CreateEditorCatalog () => new (GetInitialized<XmlTestEnvironment> ().GetEditorHost ());

		protected static EditorEnvironment GetInitialized<T> () where T : XmlTestEnvironment, new()
			#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
			=> GetInitializedAsync<T> ().Result;
			#pragma warning restore VSTHRD002

		protected static ValueTask<EditorEnvironment> GetInitializedAsync<T> () where T : XmlTestEnvironment, new()
		{
			#pragma warning disable VSTHRD103 // Call async methods when in an async method

			if (initTask is not null && initTask.IsCompleted) {
				return new ValueTask<EditorEnvironment> (CheckResultType (initTask));
			}

			return new ValueTask<EditorEnvironment> (
				GetOrCreateInitTask<T> ()
				.ContinueWith (CheckResultType, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
			);

			static EditorEnvironment CheckResultType (Task<XmlTestEnvironment> task)
			{
				if (task.Result is not T) {
					throw new InvalidOperationException ($"Already initialized with type '{task.Result.GetType ()}'");
				}
				return task.Result.editorEnvironment!;
			}

			#pragma warning restore VSTHRD103
		}

		static Task<XmlTestEnvironment> GetOrCreateInitTask<T> () where T : XmlTestEnvironment, new()
		{
			if (initTask is not null) {
				return initTask;
			}

			lock (initLock) {
				if (initTask != null) {
					return initTask;
				}

				try {
					var mainloop = new Utils.MockMainLoop ();
					mainloop.Start ();

					MefJoinableTaskContext = mainloop.JoinableTaskContext;
					SynchronizationContext.SetSynchronizationContext (mainloop);

					var instance = new T ();
					return initTask = instance.OnInitialize ()
						.ContinueWith<XmlTestEnvironment> (_ => instance, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
				}
				catch (Exception ex) {
					return initTask = Task.FromException<XmlTestEnvironment> (ex);
				}
			}
		}

		protected virtual async Task OnInitialize ()
		{
			// Create the MEF composition
			// can be awaited instead if your framework supports it
			editorEnvironment = await EditorEnvironment.InitializeAsync (GetAssembliesToCompose ().ToArray ());

			if (editorEnvironment.CompositionErrors.Length > 0) {
				var errors = editorEnvironment.CompositionErrors.Where (e => !ShouldIgnoreCompositionError (e)).ToArray ();
				if (errors.Length > 0) {
					throw new CompositionException ($"Composition failure: {string.Join ("\n", errors)}");
				}
			}

			CustomErrorHandler.ExceptionHandled += (s, e) => HandleError (s, e.Exception);
		}

		protected virtual IEnumerable<string> GetAssembliesToCompose () => new[] {
			typeof (XmlParser).Assembly.Location,
			typeof (XmlCompletionSource).Assembly.Location,
			typeof (XmlTestEnvironment).Assembly.Location
		};

		protected virtual bool ShouldIgnoreCompositionError (string error) => false;

		protected virtual void HandleError (object? source, Exception ex)
		{
			TestExecutionContext.CurrentContext.CurrentResult.RecordAssertion (AssertionStatus.Error, ex.Message, ex.StackTrace);

			if (Debugger.IsAttached) {
				Debugger.Break ();
			}
		}
	}
}
