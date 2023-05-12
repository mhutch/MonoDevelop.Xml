// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Logging;

class NullLoggerFactory : ILoggerFactory
{
	static readonly NullLogger nullLogger = new NullLogger ();
	static readonly NullScope nullScope = new NullScope ();

	public static NullLoggerFactory Instance { get; } = new ();

	public void AddProvider (ILoggerProvider provider) => throw new NotSupportedException ();

	public ILogger CreateLogger (string categoryName) => nullLogger;

	public void Dispose () { }

	class NullLogger : ILogger
	{
		public bool IsEnabled (LogLevel logLevel) => false;

		public IDisposable BeginScope<TState> (TState state) => nullScope;
		public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => throw new InvalidOperationException ();
	}

	class NullScope : IDisposable
	{
		public void Dispose () { }
	}
}