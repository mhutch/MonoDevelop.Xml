// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

using NUnit.Framework;

namespace MonoDevelop.Xml.Tests;

// this is in Core.Tests even though only tests in the Editor.Tests assembly currently use it
// so that MonoDevelop.MSBuild.Tests can use it w/o depending on Editor.Tests
public static class TestLoggerFactory
{
	static readonly ILoggerFactory loggerFactory = LoggerFactory.Create (builder => builder
		.AddProvider(new NUnitLoggerProvider ())
		.SetMinimumLevel (LogLevel.Debug)
	);

	public static ILogger<T> CreateLogger<T> () => loggerFactory.CreateLogger<T> ();
	public static ILogger CreateLogger (string categoryName) => loggerFactory.CreateLogger (categoryName);

	// note: LoggerFactory caches these by name and holds onto them forever, so creating one per test may be excessive
	public static ILogger CreateTestMethodLogger ()
	{
		var test = TestContext.CurrentContext.Test;
		return CreateLogger (test.FullName);
	}
}
