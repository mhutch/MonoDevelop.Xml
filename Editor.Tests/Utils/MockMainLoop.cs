// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Threading;

namespace MonoDevelop.Xml.Editor.Tests.Utils
{
	public class MockMainLoop : SynchronizationContext
	{
		readonly AsyncQueue<Payload> pending = new();

		void Run (object? obj)
		{
			((TaskCompletionSource<bool>)obj!).SetResult (true);

			while (!pending.IsCompleted) {

				#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
				var payload = pending.DequeueAsync ().Result;
				#pragma warning restore VSTHRD002

				if (payload.Waiter != null) {
					try {
						payload.Callback (payload.State);
						payload.Waiter.TrySetResult (true);
					} catch (Exception ex) {
						payload.Waiter.TrySetException (ex);
					}
				} else {
					payload.Callback (payload.State);
				}
			}
		}

		public Thread? MainThread { get; private set; }
		public JoinableTaskContext? JoinableTaskContext { get; private set; }

		[MemberNotNull (nameof (MainThread), nameof (JoinableTaskContext))]
		public void Start ()
		{
			var readyTask = new TaskCompletionSource<bool> ();
			MainThread = new Thread (Run) { IsBackground = true };
			MainThread.SetApartmentState (ApartmentState.STA);
			MainThread.Start (readyTask);

#pragma warning disable VSSDK005 // Avoid instantiating JoinableTaskContext
			JoinableTaskContext = new JoinableTaskContext (MainThread, this);
#pragma warning restore VSSDK005 // Avoid instantiating JoinableTaskContext

#pragma warning disable VSTHRD002 // This just signals the thread was started
			readyTask.Task.Wait ();
			#pragma warning restore VSTHRD002
		}

		public void Stop () => pending.Complete ();

		public override void Send (SendOrPostCallback d, object? state)
		{
			var waiter = new TaskCompletionSource<bool> ();
			pending.Enqueue (new Payload (d, state, waiter));

			#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
			waiter.Task.Wait ();
			#pragma warning restore VSTHRD002
		}

		public override void Post (SendOrPostCallback d, object? state)
		{
			pending.Enqueue (new Payload (d, state, null));
		}

		class Payload
		{
			public SendOrPostCallback Callback { get; }
			public object? State { get; }
			public TaskCompletionSource<bool>? Waiter { get; }

			public Payload (SendOrPostCallback d, object? state, TaskCompletionSource<bool>? waiter)
			{
				Callback = d;
				State = state;
				Waiter = waiter;
			}
		}
	}
}
