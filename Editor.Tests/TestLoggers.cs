// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.ComponentModel.Composition;

using Microsoft.Extensions.Logging;

using MonoDevelop.MSBuild.Editor.HighlightReferences;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Editor.Tagging;

namespace MonoDevelop.Xml.Editor.Tests;

static class TestLoggers
{
	static readonly ILoggerFactory loggerFactory = LoggerFactory.Create (builder => builder
		.AddConsole ()
		.SetMinimumLevel (LogLevel.Debug)
	);

	public static ILogger CreateLogger (string categoryName) => loggerFactory.CreateLogger (categoryName);
	public static ILogger<T> CreateLogger<T> () => loggerFactory.CreateLogger<T> ();

	[Export]
	public static ILogger<XmlCompletionSource> XmlCompletionSource => CreateLogger<XmlCompletionSource> ();

	[Export]
	public static ILogger<XmlBackgroundParser> XmlBackgroundParser => CreateLogger<XmlBackgroundParser> ();

	[Export]
	public static ILogger<XmlHighlightEndTagTagger> XmlHighlightEndTagTagger => CreateLogger<XmlHighlightEndTagTagger> ();

	[Export]
	public static ILogger<XmlCompletionCommitManager> XmlCompletionCommitManager => CreateLogger<XmlCompletionCommitManager> ();

	[Export]
	public static ILogger<StructureTagger> StructureTagger => CreateLogger<StructureTagger> ();

	[Export]
	public static ILogger XmlTestLogger => CreateLogger ("XML Editor Tests");
}

