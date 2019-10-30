// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Completion
{
	public class XmlBackgroundParser : BufferParser<XmlParseResult>
	{
		protected override void Initialize ()
		{
			StateMachine = CreateParserStateMachine ();
		}

		protected override string ContentType => XmlContentTypeNames.XmlCore;

		protected virtual XmlRootState CreateParserStateMachine () => new XmlRootState ();

		// the state machine does not store any state itself, so we can re-use it
		protected XmlRootState StateMachine { get; private set; }

		protected override Task<XmlParseResult> StartOperationAsync (ITextSnapshot input,
			XmlParseResult previousOutput,
			ITextSnapshot previousInput,
			CancellationToken token)
		{
			var parser = new XmlTreeParser (StateMachine);
			return Task.Run (() => {
				var length = input.Length;
				for (int i = 0; i < length; i++) {
					parser.Push (input[i]);
				}
				var (doc, diagnostics) = parser.FinalizeDocument ();
				return new XmlParseResult (doc, diagnostics, input);
			}, token);
		}

		static int MaximumCompatiblePosition (ITextSnapshot snapshotA, ITextSnapshot snapshotB)
		{
			var newVersion = snapshotA.Version;
			var oldVersion = snapshotB.Version;
			if (newVersion.VersionNumber < oldVersion.VersionNumber) {
				var v = newVersion;
				newVersion = oldVersion;
				oldVersion = v;
			}

			int position = Math.Min (newVersion.Length, oldVersion.Length);
			while (newVersion.VersionNumber != oldVersion.VersionNumber) {
				foreach (var change in oldVersion.Changes) {
					if (change.OldPosition > position) {
						position = change.OldPosition;
						break;
					}
				}
				oldVersion = oldVersion.Next;
			}

			return position;
		}

		public XmlSpineParser GetSpineParser (SnapshotPoint point)
		{
			XmlSpineParser parser = null;

			var prevParse = LastOutput;
			if (prevParse != null) {
				var startPos = Math.Min (point.Position, MaximumCompatiblePosition (prevParse.TextSnapshot, point.Snapshot));
				if (startPos > 0) {
					var obj = prevParse.XDocument.FindAtOrBeforeOffset (startPos);
					var state = StateMachine.TryRecreateState (obj, startPos);
					if (state != null) {
						LoggingService.LogDebug ($"XML parser recovered {state.Position}/{point.Position} state");
						parser = new XmlSpineParser (state, StateMachine);
					}
				}
			}

			if (parser == null) {
				LoggingService.LogDebug ($"XML parser failed to recover any state");
				parser = new XmlSpineParser (StateMachine);
			}

			var end = Math.Min (point.Position, point.Snapshot.Length);
			for (int i = parser.Position; i < end; i++) {
				parser.Push (point.Snapshot[i]);
			}

			return parser;
		}

		public static bool TryGetParser (ITextBuffer buffer, out XmlBackgroundParser parser)
			=> buffer.Properties.TryGetProperty (typeof (XmlBackgroundParser), out parser);

		public static XmlBackgroundParser GetParser (ITextBuffer buffer) => GetParser<XmlBackgroundParser> (buffer);
	}

	public class XmlParseResult
	{
		public XmlParseResult (XDocument xDocument, List<XmlDiagnosticInfo> parseDiagnostics, ITextSnapshot textSnapshot)
		{
			XDocument = xDocument;
			ParseDiagnostics = parseDiagnostics;
			TextSnapshot = textSnapshot;
		}

		public List<XmlDiagnosticInfo> ParseDiagnostics { get; }
		public XDocument XDocument { get; }
		public ITextSnapshot TextSnapshot { get; }
	}
}
