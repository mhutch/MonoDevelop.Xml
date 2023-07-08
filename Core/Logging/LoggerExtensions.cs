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
	/// <summary>
	/// Logs any exceptions thrown by the task, and observes the task so it doesn't throw unobserved exceptions.
	/// </summary>
	public static void LogExceptionsAndForget (this Task task, ILogger logger, [CallerMemberName] string? originMember = default)
	{
		task.CatchAndLogIfFaulted (logger, originMember);
	}

	public static Task<T> WithExceptionLogger<T> (this Task<T> task, ILogger logger, [CallerMemberName] string? originMember = default)
	{
		task.CatchAndLogIfFaulted (logger, originMember);
		return task;
	}

	/// <summary>
	/// Attaches a continution to the task that logs any exception thrown by the task, and returns the task.
	/// </summary>
	public static Task WithExceptionLogger (this Task task, ILogger logger, [CallerMemberName] string? originMember = default)
	{
		task.CatchAndLogIfFaulted (logger, originMember);
		return task;
	}

	static Task CatchAndLogIfFaulted (this Task task, ILogger logger, string? originMember)
	{
		if (originMember is null) {
			throw new ArgumentNullException (nameof (originMember));
		}

		_ = task.ContinueWith (
			t => LogExceptions (logger, originMember, t),
			default,
			TaskContinuationOptions.OnlyOnFaulted,
			TaskScheduler.Default
		);

		return task;
	}

	static void LogExceptions (ILogger logger, string originMember, Task t)
	{
		if (t.Exception is null) {
			return;
		}
		List<Exception>? unhandled = null;
		foreach (var inner in t.Exception.InnerExceptions) {
			if (inner is not OperationCanceledException) {
				(unhandled ??= new List<Exception> ()).Add (inner);
			}
		}
		if (unhandled is null) {
			return;
		}
		Exception ex = unhandled.Count == 1 ? unhandled[0] : new AggregateException (unhandled);
		logger.LogInternalError (ex, originMember);
	}

	/// <summary>
	/// Logs an unhandled exception.
	/// </summary>
	public static void LogInternalError (this ILogger logger, Exception ex, [CallerMemberName] string? originMember = default)
		=> logger.LogInternalErrorInMember (ex, originMember ?? throw new ArgumentNullException (nameof (originMember)));

	[LoggerMessage (Level = LogLevel.Error, Message = "Internal error in {originMember}")]
	static partial void LogInternalErrorInMember (this ILogger logger, Exception ex, string originMember);

	/// <summary>
	/// Catches any exception thrown by the function, logs it, and rethrows. If the function returns a task that faults, its exception will also be logged.
	/// </summary>
	public static T InvokeAndLogErrors<T> (this ILogger logger, Func<T> function, [CallerMemberName] string? originMember = default)
	{
		try {
			var result = function ();
			if (result is Task task) {
				task.CatchAndLogIfFaulted (logger, originMember);
			}
			return result;
		} catch (Exception ex) {
			logger.LogInternalError (ex, originMember);
			throw;
		}
	}
}