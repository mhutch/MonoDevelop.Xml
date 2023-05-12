// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace MonoDevelop.Xml.Logging;

public static partial class LoggerExtensions
{
	public static void CatchAndLogWarning (this Task task, ILogger logger, [CallerMemberName] string? originMember = default)
	{
		if (originMember is null) {
			throw new ArgumentNullException (nameof (originMember));
		}

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
				logger.LogInternalErrorAsWarning (ex, originMember);
			},
			default,
			TaskContinuationOptions.OnlyOnFaulted,
			TaskScheduler.Default
		);
	}

	[LoggerMessage (Level = LogLevel.Warning, Message = "Internal error in {originMember}")]
	public static partial void LogInternalErrorAsWarning (this ILogger logger, Exception ex, string originMember);
}