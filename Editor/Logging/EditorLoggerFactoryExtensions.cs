// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Xml.Editor.Logging;

// TODO: these could return proxies that recreates the internal logger if the buffer's content type changes
public static class EditorLoggerFactoryExtensions
{
	/// <summary>
	/// Get a logger for the <paramref name="buffer"/> if it exists, reusing a previously-created <see cref="ITextBuffer"/>-specific logger if possible.
	/// <returns>
	/// A wrapper <see cref="ILogger{TCategoryName}"> that defers creating/fetching the <see cref="ITextBuffer"/>-specific logger until it's used.
	/// </returns>
	/// <remarks>
	/// If this will only be called once for this <paramref name="buffer"/> and <typeparamref name="TCategoryName"/>, use <see cref="CreateLogger{TCategoryName}(IEditorLoggerFactory, ITextBuffer)"/> instead and store the result.
	/// </remarks>
	public static ILogger<TCategoryName> GetLogger<TCategoryName> (this IEditorLoggerFactory factory, ITextBuffer buffer) => new LazyTextBufferLogger<TCategoryName> (factory, buffer);

	/// <summary>
	/// Get a logger for the <paramref name="textView"/>, reusing a previously-created <see cref="ITextView"/>-specific if possible.
	/// </summary>
	/// <returns>
	/// A wrapper <see cref="ILogger{TCategoryName}"> that defers creating/fetching the <see cref="ITextView"/>-specific logger until it's used.
	/// </returns>
	/// <remarks>
	/// If this will only be called once for this <paramref name="textView"/> and <typeparamref name="TCategoryName"/>, use <see cref="CreateLogger{TCategoryName}(IEditorLoggerFactory, ITextView)"/> instead and store the result.
	/// </remarks>
	public static ILogger<TCategoryName> GetLogger<TCategoryName> (this IEditorLoggerFactory factory, ITextView textView) => new LazyTextViewLogger<TCategoryName> (factory, textView);

	/// <summary>
	/// Create a logger for the <paramref name="buffer"/>.
	/// <para>
	/// If this will be called multiple times for this <paramref name="textView"/> and <typeparamref name="TCategoryName"/>, use <see cref="GetLogger{T}(IEditorLoggerFactory, ITextBuffer)"/> instead, which reuses a previously-created logger if possible.
	/// </para>
	/// </summary>
	public static ILogger<TCategoryName> CreateLogger<TCategoryName> (this IEditorLoggerFactory factory, ITextBuffer buffer)
	{
		var logger = factory.CreateLogger<TCategoryName> (buffer.ContentType);
		logger.BeginScope (new TextBufferId (buffer));
		return logger;
	}

	/// <summary>
	/// Create a logger for the <paramref name="textView"/>.
	/// <para>
	/// If this will be called multiple times for this <paramref name="textView"/> and <typeparamref name="TCategoryName"/>, use <see cref="GetLogger{T}(IEditorLoggerFactory, ITextView)"/> instead, which reuses a previously-created logger if possible.
	/// </para>
	/// </summary>
	public static ILogger<TCategoryName> CreateLogger<TCategoryName> (this IEditorLoggerFactory factory, ITextView textView)
	{
		var logger = factory.CreateLogger<TCategoryName> (textView.TextBuffer.ContentType);
		logger.BeginScope (new TextViewId (textView));
		logger.BeginScope (new TextBufferId (textView.TextBuffer));
		return logger;
	}

	readonly struct TextBufferId
	{
		readonly int id;

		public TextBufferId (ITextBuffer buffer)
		{
			//TODO better id?
			id = buffer.GetHashCode ();
		}

		public override readonly string ToString () => $"TextBuffer #{id}";
	}

	readonly struct TextViewId
	{
		readonly int id;

		public TextViewId (ITextView textView) : this ()
		{
			//TODO better id?
			id = textView.GetHashCode ();
		}

		public override readonly string ToString () => $"TextView #{id}";
	}

	class LazyTextBufferLogger<TCategoryName> : ILogger<TCategoryName>
	{
		readonly IEditorLoggerFactory factory;
		readonly ITextBuffer buffer;

		ILogger<TCategoryName>? logger;
		ILogger<TCategoryName> GetLogger () => logger ??= buffer.Properties.GetOrCreateSingletonProperty (() => factory.CreateLogger<TCategoryName> (buffer));

		public LazyTextBufferLogger (IEditorLoggerFactory factory, ITextBuffer buffer)
		{
			this.factory = factory;
			this.buffer = buffer;
		}

		public IDisposable? BeginScope<TState> (TState state) where TState : notnull => GetLogger ().BeginScope (state);

		public bool IsEnabled (LogLevel logLevel) => GetLogger ().IsEnabled (logLevel);

		public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => GetLogger ().Log (logLevel, eventId, state, exception, formatter);
	}

	class LazyTextViewLogger<TCategoryName> : ILogger<TCategoryName>
	{
		readonly IEditorLoggerFactory factory;
		readonly ITextView textView;

		ILogger<TCategoryName>? logger;
		ILogger<TCategoryName> GetLogger () => logger ??= textView.Properties.GetOrCreateSingletonProperty (() => factory.CreateLogger<TCategoryName> (textView));

		public LazyTextViewLogger (IEditorLoggerFactory factory, ITextView textView)
		{
			this.factory = factory;
			this.textView = textView;
		}

		public IDisposable? BeginScope<TState> (TState state) where TState : notnull => GetLogger ().BeginScope (state);

		public bool IsEnabled (LogLevel logLevel) => GetLogger ().IsEnabled (logLevel);

		public void Log<TState> (LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => GetLogger ().Log (logLevel, eventId, state, exception, formatter);
	}
}
