// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor.Tests;

static class TestLoggers
{
	static readonly ILoggerFactory loggerFactory = LoggerFactory.Create (builder => builder
		.AddConsole ()
		.SetMinimumLevel (LogLevel.Debug)
	);

	public static ILogger CreateLogger (string categoryName) => loggerFactory.CreateLogger (categoryName);
	public static ILogger<T> CreateLogger<T> () => loggerFactory.CreateLogger<T> ();
}

