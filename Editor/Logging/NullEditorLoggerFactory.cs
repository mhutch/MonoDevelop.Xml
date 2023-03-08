// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Logging;

class NullEditorLoggerFactory : IEditorLoggerFactory
{
	public static NullEditorLoggerFactory Instance { get; } = new ();

	public ILogger<T> CreateLogger<T> () => new NullLogger<T> ();

	class NullLogger<T> : ILogger<T>
	{
		public bool IsEnabled (LogLevel logLevel) => false;

		public IDisposable BeginScope<TState> (TState state) => new NullLoggerScope ();
		public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => throw new InvalidOperationException ();
	}

	class NullLoggerScope : IDisposable
	{
		public void Dispose () { }
	}
}