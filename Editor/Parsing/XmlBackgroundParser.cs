// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Parsing
{
	public partial class XmlBackgroundParser : BufferParser<XmlParseResult>
	{
		private readonly ILogger logger;

		public XmlBackgroundParser (ITextBuffer2 buffer, ILogger logger, IBackgroundParseService parseService) : base (buffer, parseService)
		{
			StateMachine = CreateParserStateMachine ();
			this.logger = logger;
		}

		protected override string ContentType => XmlContentTypeNames.XmlCore;

		protected virtual XmlRootState CreateParserStateMachine () => new ();

		// the state machine does not store any state itself, so we can re-use it
		protected XmlRootState StateMachine { get; private set; }

		protected override Task<XmlParseResult> StartOperationAsync (ITextSnapshot input,
			XmlParseResult? previousOutput,
			ITextSnapshot? previousInput,
			CancellationToken token)
		{
			var parser = new XmlTreeParser (StateMachine);
			return Task.Run (() => {
				var length = input.Length;
				for (int i = 0; i < length; i++) {
					parser.Push (input[i]);
					token.ThrowIfCancellationRequested ();
				}
				var (doc, diagnostics) = parser.EndAllNodes ();
				return new XmlParseResult (doc, diagnostics, input);
			}, token);
		}

		static int MaximumCompatiblePosition (ITextSnapshot snapshotA, ITextSnapshot snapshotB)
		{
			var newVersion = snapshotA.Version;
			var oldVersion = snapshotB.Version;
			if (newVersion.VersionNumber < oldVersion.VersionNumber) {
				(oldVersion, newVersion) = (newVersion, oldVersion);
			}

			int position = Math.Min (newVersion.Length, oldVersion.Length);
			while (newVersion.VersionNumber != oldVersion.VersionNumber) {
				if (oldVersion.Changes != null) {
					foreach (var change in oldVersion.Changes) {
						if (change.OldPosition < position) {
							position = change.OldPosition;
							break;
						}
					}
				}
				oldVersion = oldVersion.Next;
			}

			return position;
		}

		public XmlSpineParser GetSpineParser (SnapshotPoint point, CancellationToken token = default)
		{
			XmlSpineParser? parser = null;

			var prevParse = LastOutput;

			if (prevParse == null) {
				prevParse = GetOrProcessAsync (point.Snapshot, CancellationToken.None).Result;
			}

			if (prevParse != null) {
				var startPos = Math.Min (point.Position, MaximumCompatiblePosition (prevParse.TextSnapshot, point.Snapshot));
				if (startPos > 0) {
					parser = XmlSpineParser.FromDocumentPosition (StateMachine, prevParse.XDocument, startPos);
				}
			}

			if (parser == null) {
				LogRecoveryFailed (logger);
				parser = new XmlSpineParser (StateMachine);
			}

			var end = Math.Min (point.Position, point.Snapshot.Length);
			for (int i = parser.Position; i < end; i++) {
				token.ThrowIfCancellationRequested ();
				parser.Push (point.Snapshot[i]);
			}

			return parser;
		}

		[LoggerMessage (EventId = 3, Level = LogLevel.Trace, Message = "XML parser recovered {recoveredPos}/{requestedPos} state'")]
		static partial void LogRecovered (ILogger logger, int recoveredPos, int requestedPos);

		[LoggerMessage (EventId = 4, Level = LogLevel.Trace, Message = "XML parser failed to recover any state'")]
		static partial void LogRecoveryFailed (ILogger logger);
	}
}
