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
	public static void LogTaskExceptionsAndForget (this Task task, ILogger logger, [CallerMemberName] string? originMember = default)
		=> _ = task.WithTaskExceptionLogger (logger, originMember);

	/// <summary>
	/// Attaches a continution to the task that logs any exception thrown by the task, and returns the task.
	/// </summary>
	public static TTask WithTaskExceptionLogger<TTask> (this TTask task, ILogger logger, [CallerMemberName] string? originMember = default) where TTask : Task
	{
		originMember = originMember ?? throw new ArgumentNullException (nameof (originMember));
		_ = task.ContinueWith (
			t => LogTaskExceptions (logger, originMember, t),
			default,
			TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
			TaskScheduler.Default
		);
		return task;
	}

	/// <summary>
	/// Attaches a continution to the task that logs any exception thrown by the task, and returns the task.
	/// <para>
	/// If the <see cref="IEnumerable{T}"/> task result is not a collection then it will be return wrapped in an enumerator that logs any exceptions thrown during enumeration.
	/// </para>
	/// </summary>
	public static Task<IEnumerable<T>> WithTaskAndEnumeratorExceptionLogger<T> (this Task<IEnumerable<T>> task, ILogger logger, [CallerMemberName] string? originMember = default)
	{
		originMember = originMember ?? throw new ArgumentNullException (nameof (originMember));

		return task.ContinueWith (t => {
			try {
				return t.Result.WithEnumeratorExceptionLogger (logger, originMember);
			} catch (Exception ex) {
				LogInternalException (logger, ex, originMember);
				throw;
			}
		},
		TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.NotOnCanceled);
	}

	/// <summary>
	/// If the <see cref="IEnumerable{T}"/> is not a collection then it will be return wrapped in an enumerator that logs any exceptions thrown during enumeration.
	/// </summary>
	public static IEnumerable<T> WithEnumeratorExceptionLogger<T> (this IEnumerable<T> enumerable, ILogger logger, [CallerMemberName] string? originMember = default)
	{
		if (enumerable is ICollection<T> || enumerable is IReadOnlyCollection<T>) {
			return enumerable;
		}
		return new LoggedEnumerable<T> (enumerable, logger, originMember ?? throw new ArgumentNullException (nameof (originMember)));
	}

	static void LogTaskExceptions (ILogger logger, string originMember, Task t)
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
		logger.LogInternalException (ex, originMember);
	}

	/// <summary>
	/// Logs an unhandled exception.
	/// </summary>
	public static void LogInternalException (this ILogger logger, Exception ex, [CallerMemberName] string? originMember = default)
		=> logger.LogInternalExceptionInMember (ex, originMember ?? throw new ArgumentNullException (nameof (originMember)));

	[LoggerMessage (Level = LogLevel.Error, Message = "Internal exception in {originMember}")]
	static partial void LogInternalExceptionInMember (this ILogger logger, Exception ex, string originMember);

	/// <summary>
	/// Catches any exception thrown by the function, logs it, and rethrows.
	/// </summary>
	public static T InvokeAndLogExceptions<T> (this ILogger logger, Func<T> function, [CallerMemberName] string? originMember = default)
	{
		originMember = originMember ?? throw new ArgumentNullException (nameof (originMember));

		try {
			return function ();
		} catch (Exception ex) {
			logger.LogInternalException (ex, originMember);
			throw;
		}
	}

	public static Task InvokeAndLogExceptions (this ILogger logger, Func<Task> function, [CallerMemberName] string? originMember = default)
		=> logger.InvokeAndLogExceptions<Task> (function, originMember)
		   .WithTaskExceptionLogger (logger, originMember);

	public static Task<T> InvokeAndLogExceptions<T> (this ILogger logger, Func<Task<T>> function, [CallerMemberName] string? originMember = default)
		=> logger.InvokeAndLogExceptions<Task<T>> (function, originMember)
		   .WithTaskExceptionLogger (logger, originMember);

	public static IEnumerable<T> InvokeAndLogExceptions<T> (this ILogger logger, Func<IEnumerable<T>> function, [CallerMemberName] string? originMember = default)
		=> logger.InvokeAndLogExceptions<IEnumerable<T>> (function, originMember)
		   .WithEnumeratorExceptionLogger (logger, originMember);
}