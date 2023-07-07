// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Threading;
using Microsoft.VisualStudio.Text;


namespace MonoDevelop.Xml.Editor.Parsing
{
	/// <summary>
	/// Base class for parsers that parse a text buffer on every change.
	/// </summary>
	public abstract class BufferParser<TParseResult> : BackgroundProcessor<ITextSnapshot,TParseResult> where TParseResult : class
	{
		internal object? providerKey;

		public BufferParser (ITextBuffer2 buffer)
		{
			Buffer = buffer;

			if (!buffer.ContentType.IsOfType (ContentType)) {
				throw new ArgumentException (
					$"Buffer content type is {buffer.ContentType.TypeName}, expecting {ContentType}",
					nameof (buffer));
			}

			// it's not super-important to unsubscribe this, as it has the same lifetime as the buffer.
			buffer.ChangedOnBackground += BufferChangedOnBackground;

			// if the content type changes, discard the parser. it will be recreated if needed anyway.
			buffer.ContentTypeChanged += BufferContentTypeChanged;
		}

		protected abstract string ContentType { get; }

		protected ITextBuffer2 Buffer { get; }

		void BufferChangedOnBackground (object? sender, TextContentChangedEventArgs e)
		{
			StartProcessing ((ITextSnapshot2)e.After);
		}

		protected override void OnOperationCompleted (ITextSnapshot input, TParseResult output)
		{
			ParseCompleted?.Invoke (this, new ParseCompletedEventArgs<TParseResult> (output, input));
		}

		public event EventHandler<ParseCompletedEventArgs<TParseResult>>? ParseCompleted;

		protected override int CompareInputs (ITextSnapshot a, ITextSnapshot b)
			=> a.Version.VersionNumber.CompareTo (b.Version.VersionNumber);

		void BufferContentTypeChanged (object? sender, ContentTypeChangedEventArgs e)
		{
			if (!e.AfterContentType.IsOfType (ContentType)) {
				Dispose ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				var takeKey = Interlocked.Exchange (ref providerKey, null);
				if (takeKey != null) {
					Buffer.ChangedOnBackground -= BufferChangedOnBackground;
					Buffer.ContentTypeChanged -= BufferContentTypeChanged;
					Buffer.Properties.RemoveProperty (takeKey);
				}
			}
		}
	}
}
