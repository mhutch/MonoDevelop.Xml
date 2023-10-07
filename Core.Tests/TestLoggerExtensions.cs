// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Tests;

public record struct LogMessage (LogLevel Level, EventId EventId, string Message, Exception? Exception);

public static class TestLoggerExtensions
{
	public static ILogger CollectMessages (this ILogger logger, out List<LogMessage> messages, LogLevel collectionLevel = LogLevel.Error)
	{
		var collectingLogger = new MessageCollectingLogger (logger, collectionLevel);
		messages = collectingLogger.Messages;
		return collectingLogger;
	}

	public static ILogger RethrowExceptions (this ILogger logger) => new ExceptionRethrowingLogger (logger);

	class MessageCollectingLogger : ILogger
	{
		readonly ILogger innerLogger;
		readonly LogLevel collectionLevel;

		public MessageCollectingLogger (ILogger innerLogger, LogLevel collectionLevel = LogLevel.Error)
		{
			this.innerLogger = innerLogger;
			this.collectionLevel = collectionLevel;
		}

		public List<LogMessage> Messages { get; } = new ();

		public IDisposable? BeginScope<TState> (TState state) where TState : notnull => innerLogger.BeginScope (state);

		public bool IsEnabled (LogLevel logLevel) => (logLevel >= collectionLevel) || innerLogger.IsEnabled (logLevel);

		public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			innerLogger.Log (logLevel, eventId, state, exception, formatter);
			if (logLevel >= collectionLevel) {
				Messages.Add (new (logLevel, eventId, formatter (state, exception), exception));
			}
			if (innerLogger.IsEnabled (logLevel)) {
				innerLogger.Log (logLevel, eventId, state, exception, formatter);
			}
		}
	}

	class ExceptionRethrowingLogger : ILogger
	{
		ILogger innerLogger;

		public ExceptionRethrowingLogger (ILogger innerLogger)
		{
			this.innerLogger = innerLogger;
		}

		public IDisposable? BeginScope<TState> (TState state) where TState : notnull => innerLogger.BeginScope (state);

		public bool IsEnabled (LogLevel logLevel) => innerLogger.IsEnabled (logLevel);

		public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			innerLogger.Log (logLevel, eventId, state, exception, formatter);
			if (exception is not null) {
				throw exception;
			}
		}
	}
}