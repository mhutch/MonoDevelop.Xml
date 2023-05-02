// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MonoDevelop.Xml.Editor.Logging;

// TODO: these could return proxies that recreates the internal logger if the buffer's content type changes
public static class EditorLoggerFactoryExtensions
{
	/// <summary>
	/// Get a logger for the <paramref name="buffer"/> if it exists, reusing the existing logger if possible, and creating one if necessary.
	/// </summary>
	public static ILogger<T> GetLogger<T> (this IEditorLoggerFactory factory, ITextBuffer buffer)
		=> buffer.Properties.GetOrCreateSingletonProperty (typeof (T), () => factory.CreateLogger<T> (buffer));

	/// <summary>
	/// Get a logger for the <paramref name="textView"/>, reusing the existing logger if possible, and creating one if necessary.
	/// </summary>
	public static ILogger<T> GetLogger<T> (this IEditorLoggerFactory factory, ITextView textView)
		=> textView.Properties.GetOrCreateSingletonProperty (typeof (T), () => factory.CreateLogger<T> (textView));

	/// <summary>
	/// Create a logger for the <paramref name="buffer"/>.
	/// <para>
	/// It is generally preferable to use <see cref="GetLogger{T}(IEditorLoggerFactory, ITextBuffer)"/>, which reuses an existing logger if possible, and only creates one if necessary.
	/// </para>
	/// </summary>
	public static ILogger<T> CreateLogger<T> (this IEditorLoggerFactory factory, ITextBuffer buffer)
	{
		var logger = factory.CreateLogger<T> (buffer.ContentType);
		logger.BeginScope (new TextBufferId (buffer));
		return logger;
	}

	/// <summary>
	/// Create a logger for the <paramref name="textView"/>.
	/// <para>
	/// It is generally preferable to use <see cref="GetLogger{T}(IEditorLoggerFactory, ITextView)"/>, which reuses an existing logger if possible, and only creates one if necessary.
	/// </para>
	/// </summary>
	public static ILogger<T> CreateLogger<T> (this IEditorLoggerFactory factory, ITextView textView)
	{
		var logger = factory.CreateLogger<T> (textView.TextBuffer.ContentType);
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
}
