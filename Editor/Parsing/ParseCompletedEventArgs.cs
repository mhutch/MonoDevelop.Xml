// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Text;

namespace MonoDevelop.Xml.Editor.Completion
{
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
