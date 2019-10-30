// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Xml.Editor.Completion
{
	/// <summary>
	/// Base class for parsers that parse a text buffer on every change.
	/// </summary>
	public abstract class BufferParser<TParseResult> : BackgroundProcessor<ITextSnapshot,TParseResult> where TParseResult : class
	{
		public static TParser GetParser<TParser> (ITextBuffer buffer) where TParser : BufferParser<TParseResult>, new()
		{
			var parser = buffer.Properties.GetOrCreateSingletonProperty (typeof (TParser), () => new TParser ());

			//avoid capturing by calling this outside the lambda
			if (parser.Buffer == null) {
				if (!buffer.ContentType.IsOfType (parser.ContentType)) {
					parser.Dispose ();
					throw new ArgumentException (
						$"Buffer content type is {buffer.ContentType.TypeName}, expecting {parser.ContentType}",
						nameof (buffer));
				}
				parser.Initialize ((ITextBuffer2)buffer);
			}
			return parser;
		}

		void Initialize (ITextBuffer2 buffer)
		{
			Buffer = buffer;

			// it's not super-important to unsubscribe this, as it has the same lifetime as the buffer.
			buffer.ChangedOnBackground += BufferChangedOnBackground;

			// if the content type changes, discard the parser. it will be recreated if needed anyway.
			buffer.ContentTypeChanged += BufferContentTypeChanged;

			Initialize ();
		}

		protected abstract string ContentType { get; }

		protected ITextBuffer2 Buffer { get; private set; }

		protected virtual void Initialize ()
		{
		}

		void BufferChangedOnBackground (object sender, TextContentChangedEventArgs e)
		{
			StartProcessing ((ITextSnapshot2)e.After);
		}

		protected override void OnOperationCompleted (ITextSnapshot input, TParseResult output)
		{
			ParseCompleted?.Invoke (this, new ParseCompletedEventArgs<TParseResult> (output, input));
		}

		public event EventHandler<ParseCompletedEventArgs<TParseResult>> ParseCompleted;

		protected override int CompareInputs (ITextSnapshot a, ITextSnapshot b)
			=> a.Version.VersionNumber.CompareTo (b.Version.VersionNumber);

		void BufferContentTypeChanged (object sender, ContentTypeChangedEventArgs e)
		{
			if (!e.AfterContentType.IsOfType (ContentType)) {
				Dispose ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && Buffer != null) {
				Buffer.ChangedOnBackground -= BufferChangedOnBackground;
				Buffer.ContentTypeChanged -= BufferContentTypeChanged;
				Buffer.Properties.RemoveProperty (GetType ());
				Buffer = null;
			}
		}
	}

	public class ParseCompletedEventArgs<T> : EventArgs
	{
		public ParseCompletedEventArgs (T parseResult, ITextSnapshot snapshot)
		{
			ParseResult = parseResult;
			Snapshot = snapshot;
		}

		public T ParseResult { get; }
		public ITextSnapshot Snapshot { get; }
	}
}
