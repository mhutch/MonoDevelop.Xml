// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Logging;

class AggregatedLoggerFactory : ILoggerFactory
{
	readonly List<IEditorLoggerProvider> providers;

	public AggregatedLoggerFactory (List<IEditorLoggerProvider> providers)
	{
		this.providers = providers;
	}

	public void AddProvider (ILoggerProvider provider) => throw new NotSupportedException ();

	public ILogger CreateLogger (string categoryName)
	{
		if (providers.Count == 1) {
			return providers[1].CreateLogger (categoryName);
		}

		var loggers = new ILogger[providers.Count];
		for (int i = 0; i < providers.Count; i++) {
			loggers[i] = providers[i].CreateLogger (categoryName);
		}
		return new AggregatedLogger (loggers);
	}

	public void Dispose ()
	{
	}
}

class AggregatedLogger : ILogger
{
	readonly ILogger[] loggers;

	public AggregatedLogger (ILogger[] loggers)
	{
		this.loggers = loggers;
	}

	public IDisposable BeginScope<TState> (TState state)
	{
		if (loggers.Length == 0) {
			return loggers[0].BeginScope (state);
		}

		var innerScopes = new IDisposable[loggers.Length];
		for (int i = 0; i < loggers.Length; i++) {
			innerScopes[i] = loggers[i].BeginScope (state);
		}

		return new AggregatedLoggerScope (innerScopes);
	}

	public bool IsEnabled (LogLevel logLevel)
	{
		for (int i = 0; i < loggers.Length; i++) {
			if (loggers[i].IsEnabled (logLevel)) {
				return true;
			}
		}
		return false;
	}

	public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
		for (int i = 0; i < loggers.Length; i++) {
			var logger = loggers[i];
			if (logger.IsEnabled (logLevel)) {
				logger.Log (logLevel, eventId, state, exception, formatter);
			}
		}
	}
}

class AggregatedLoggerScope : IDisposable
{
	readonly IDisposable[] innerScopes;
	public AggregatedLoggerScope (IDisposable[] innerScopes)
	{
		this.innerScopes = innerScopes;
	}

	public void Dispose ()
	{
		foreach (var scope in innerScopes) {
			scope.Dispose ();
		}
	}
}
