// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Editor.Logging;
using MonoDevelop.Xml.Logging;

namespace MonoDevelop.Xml.Editor.Parsing;

/// <summary>
/// Import to obtain <see cref="IBackgroundParseService"/> instances.
/// </summary>
[Export (typeof (BackgroundParseServiceProvider))]
public sealed class BackgroundParseServiceProvider
{
	readonly object lockerObject = new ();
	readonly IEditorLoggerFactory loggerFactory;

	ImmutableDictionary<string, BackgroundParserService> parseServices = ImmutableDictionary<string, BackgroundParserService>.Empty;

	[ImportingConstructor]
	public BackgroundParseServiceProvider (IEditorLoggerFactory loggerFactory)
	{
		this.loggerFactory = loggerFactory;
	}

	public IBackgroundParseService GetParseServiceForBuffer (ITextBuffer buffer) => GetParseServiceForContentType (buffer.ContentType.TypeName);
	public IBackgroundParseService GetParseServiceForContentType (string contentTypeName)
	{
		if (parseServices.TryGetValue (contentTypeName, out var service)) {
			return service;
		}
		lock (lockerObject) {
			if (parseServices.TryGetValue (contentTypeName, out service)) {
				return service;
			}
			var logger = loggerFactory.CreateLogger<BackgroundParserService> (contentTypeName);
			service = new BackgroundParserService (logger);
			parseServices = parseServices.Add (contentTypeName, service);
		}
		return service;
	}

	class BackgroundParserService : IBackgroundParseService
	{
		readonly ILogger logger;

		[ImportingConstructor]
		public BackgroundParserService (ILogger logger)
		{
			this.logger = logger;
		}

		int runningTasks = 0;

		public bool IsRunning => runningTasks > 0;

		public void RegisterBackgroundOperation (Task task)
		{
			int taskCount = Interlocked.Increment (ref runningTasks);
			task.ContinueWith (OperationCompleted, TaskScheduler.Default).LogTaskExceptionsAndForget (logger);

			if (taskCount == 1 || taskCount == 0) {
				RunningStateChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		void OperationCompleted (Task t)
		{
			int taskCount = Interlocked.Decrement (ref runningTasks);

			if (taskCount == 1 || taskCount == 0) {
				RunningStateChanged?.Invoke (this, EventArgs.Empty);
			}
		}

		public event EventHandler? RunningStateChanged;
	}
}