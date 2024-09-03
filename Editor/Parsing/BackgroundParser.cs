// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Xml.Editor.Parsing
{
	public abstract partial class BackgroundProcessor<TInput, TOutput> : IDisposable
		where TInput : class
		where TOutput : class
	{
		readonly IBackgroundParseService parseService;

		protected BackgroundProcessor (IBackgroundParseService parseService)
		{
			this.parseService = parseService;
		}

		Operation CreateOperation (TInput input)
		{
			var tokenSource = new CancellationTokenSource ();

			Task<TOutput> task;
			try {
				task = StartOperationAsync (input, tokenSource.Token);
			} catch (Exception ex) {
				OnUnhandledParseError (ex);
				return new Operation (this, Task.FromException<TOutput> (ex), input, new CancellationTokenSource ());
			}

			var operation = new Operation (this, task, input, tokenSource);

			parseService.RegisterBackgroundOperation (task);

			#pragma warning disable VSTHRD110, VSTHRD105 // Observe result of async calls, Avoid method overloads that assume TaskScheduler.Current

			//capture successful parses
			task.ContinueWith ((t, state) => {
				Operation op = (Operation)state!;
				try {
					// op.Output accesses the task.Result, throwing any exceptions
					op.Processor.OnOperationCompleted (op.Input, op.Output);
					op.Processor.lastSuccessfulOperation = op;
				} catch (Exception eventException) {
					op.Processor.OnUnhandledParseError (eventException);
				}
			}, operation, tokenSource.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			#pragma warning restore VSTHRD110, VSTHRD105

			return operation;
		}

		protected void StartProcessing (TInput input)
		{
			currentOperation?.Cancel ();
			currentOperation = CreateOperation (input);
		}

		protected virtual void OnOperationCompleted (TInput input, TOutput output)
		{
		}

		static void LastDitchLog (Exception ex)
		{
			if (System.Diagnostics.Debugger.IsAttached) {
				System.Diagnostics.Debugger.Break ();
			} else {
				Console.WriteLine (ex);
			}
		}

		/// <summary>
		/// Subclasses should override this to log exceptions into their framework of choice.
		/// If they do not, it will simply break into any attached debugger or be printed to the console.
		/// </summary>
		protected virtual void OnUnhandledParseError (Exception ex)
		{
			BackgroundProcessor<TInput, TOutput>.LastDitchLog (ex);
		}

		Operation? currentOperation;
		Operation? lastSuccessfulOperation;

		protected abstract Task<TOutput> StartOperationAsync (TInput input, TOutput? previousOutput, TInput? previousInput, CancellationToken token);

		protected abstract int CompareInputs (TInput a, TInput b);

		Task<TOutput> StartOperationAsync (TInput input, CancellationToken token)
		{
			var lastSuccessful = lastSuccessfulOperation;
			if (lastSuccessful != null && CompareInputs (lastSuccessful.Input, input) < 0) {
				return StartOperationAsync (input, lastSuccessful.Output, lastSuccessful.Input, token);
			}
			return StartOperationAsync (input, default, default, token);
		}

		public TOutput? LastOutput => lastSuccessfulOperation?.Output;

		/// <summary>
		/// Get an existing completed or running processor task for the provided input if available, or creates a new processor task.
		/// </summary>
		public Task<TOutput> GetOrProcessAsync (TInput input, CancellationToken token)
		{
			var current = currentOperation;
			if (current != null && CompareInputs (current.Input, input) == 0 && current.RegisterAdditionalCancellationOwner (token)) {
				return current.Task;
			}

			currentOperation = current = CreateOperation (input);
			return current.Task;
		}

		protected virtual void Dispose (bool disposing)
		{
		}

		~BackgroundProcessor () => Dispose (false);

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
	}
}
