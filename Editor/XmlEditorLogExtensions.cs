// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Editor
{
	static partial class XmlEditorLogExtensions
	{
		public static void CatchAndLogWarning (this Task task, ILogger logger, string origin)
		{
			_ = task.ContinueWith (
				t => {
					List<Exception>? unhandled = null;
					foreach (var inner in t.Exception!.InnerExceptions) {
						if (inner is not OperationCanceledException) {
							(unhandled ??= new List<Exception> ()).Add (inner);
						}
					}
					if (unhandled is null) {
						return;
					}
					Exception ex = unhandled.Count == 1 ? unhandled[0] : new AggregateException (unhandled);
					logger.LogInternalErrorAsWarning (ex, origin);
				},
				default,
				TaskContinuationOptions.OnlyOnFaulted,
				TaskScheduler.Default
			);
		}

		[LoggerMessage (Level = LogLevel.Warning, Message = "Internal error in {origin}")]
		public static partial void LogInternalErrorAsWarning (this ILogger logger, Exception ex, string origin);
	}
}