// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests;

/// <summary>
/// Logger that logs to the TestContext.Out so the messages are associated with the test
/// </summary>
class NUnitLoggerProvider : ILoggerProvider
{
	public ILogger CreateLogger (string categoryName) => new NUnitLogger (this, categoryName);

	public void Dispose ()
	{
	}

	class NUnitLogger : ILogger
	{
		// use a list because we can't efficiently enumerate Stack<T> backwards
		readonly List<NUnitLogScope> scopeStack = new ();
		readonly NUnitLoggerProvider provider;
		readonly string categoryName;

		public NUnitLogger (NUnitLoggerProvider provider, string categoryName)
		{
			this.provider = provider;
			this.categoryName = categoryName;
		}

		public IDisposable? BeginScope<TState> (TState state) where TState : notnull
		{
			var scope = new NUnitLogScope (this, state ?? throw new ArgumentNullException (nameof (state)));
			lock (scopeStack) {
				scopeStack.Add (scope);
			}
			return scope;
		}

		public bool IsEnabled (LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled (logLevel)) {
				return;
			}

			string message = formatter (state, exception);
			if (message is null && exception is null) {
				return;
			}

			// TODO: indentation for multiline messages
			var writer = TestContext.Out;

			writer.WriteLine ("{0}: {1} #{2}", GetLevelPrefix (logLevel), categoryName, eventId);
			writer.WriteLine (message);

			WriteScopes (writer);

			if (exception is not null) {
				writer.WriteLine (exception.ToString ());
			}
		}

		void WriteScopes (TextWriter writer)
		{
			lock (scopeStack) {
				// enumerating list forwards is equiv to enumerating stack backwards
				foreach (var scope  in scopeStack) {
					writer.Write (" => ");
					writer.WriteLine (scope.State);
				}
			}
		}

		static string GetLevelPrefix (LogLevel logLevel) => logLevel switch {
			// same prefixes as console logger
			LogLevel.Error => "fail",
			LogLevel.Debug => "dbug",
			LogLevel.Information => "info",
			LogLevel.Trace => "trce",
			LogLevel.Critical => "crit",
			LogLevel.Warning => "warn",
			_ => throw new ArgumentOutOfRangeException (nameof (logLevel))
		};

		class NUnitLogScope : IDisposable
		{
			int disposed = 0;
			readonly NUnitLogger parent;

			public NUnitLogScope (NUnitLogger parent, object state)
			{
				this.parent = parent;
				State = state;
			}

			public object State { get; }

			public void Dispose ()
			{
				if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1) {
					return;
				}
				// we're not allowed to throw from Dispose so let's be super cautious
				var scopes = parent.scopeStack;
				lock(scopes) {
					// if the stack is somehow empty, just bail
					if (scopes.Count == 0) {
						return;
					}
					// If the top item is `this` then everything's in order, pop it and we're done. Ideally this is the only thing that ever happens.
					if (scopes[scopes.Count-1] == this) {
						scopes.RemoveAt(scopes.Count-1);
						return;
					}
					// We found something unexpected, thing have not been disposed in sequence.
					// If `this` is not on the stack, it was disposed late and another instance has tidied up.
					// Pretend nothing happened and back away slowly.
					int thisIdx = scopes.IndexOf (this);
					if (thisIdx < 0) {
						return;
					}
					// If `this` is on the stack, then one or more things have not been disposed that should have been disposed before it.
					// So pop everything up to and including `this`.
					scopes.RemoveRange (thisIdx, scopes.Count - thisIdx);
				}
			}
		}
	}
}