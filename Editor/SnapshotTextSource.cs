// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.VisualStudio.Text;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor
{
	public class SnapshotTextSource : ITextSource
	{
		public SnapshotTextSource (ITextSnapshot snapshot)
		{
			Snapshot = snapshot;
		}

		public ITextSnapshot Snapshot { get; }

		public int Length => Snapshot.Length;

		public TextReader CreateReader () => new SnapshotTextReader (Snapshot);

		public char this [int offset] => Snapshot[offset];

		public string GetText (int begin, int length) => Snapshot.GetText (new Span (begin, length));

		class SnapshotTextReader : TextReader
		{
			readonly ITextSnapshot snapshot;
			int position;

			public SnapshotTextReader (ITextSnapshot snapshot)
			{
				this.snapshot = snapshot;
			}

			public override int Peek ()
			{
				if (position + 1 < snapshot.Length) {
					return snapshot[position + 1];
				}
				return -1;
			}

			public override int Read ()
			{
				if (position < snapshot.Length) {
					return snapshot[position++];
				}
				return -1;
			}

			public override string ReadToEnd ()
			{
				return snapshot.GetText (new Span (position, snapshot.Length - position));
			}
		}
	}

	public static class TextSourceSnapshotExtensions
	{
		public static SnapshotTextSource GetTextSource (this ITextSnapshot snapshot) => new SnapshotTextSource (snapshot);
	}
}