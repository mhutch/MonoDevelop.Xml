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
	public abstract class XmlBackgroundParser<TResult>
		: BackgroundParser<TResult>, IXmlBackgroundParser
		where TResult : XmlParseResult
	{
		protected override void Initialize ()
		{
			StateMachine = CreateParserStateMachine ();
			Buffer.Properties.AddProperty (typeof (IXmlBackgroundParser), this);
		}

		protected virtual XmlRootState CreateParserStateMachine () => new XmlRootState ();

		// the state machine does not store any state itself, so we can re-use it
		protected XmlRootState StateMachine { get; private set; }

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

		public XmlParser GetSpineParser (SnapshotPoint point)
		{
			XmlParser parser = null;

			var prevParse = LastParseResult;
			if (prevParse != null) {
				var startPos = Math.Min (point.Position, MaximumCompatiblePosition (prevParse.TextSnapshot, point.Snapshot));
				if (startPos > 0) {
					var obj = prevParse.XDocument.FindNodeAtOrBeforeOffset (startPos);
					var state = StateMachine.TryRecreateState (obj, startPos);
					if (state != null) {
						LoggingService.LogDebug ($"XML parser recovered {state.Position}/{point.Position} state");
						parser = new XmlParser (state, StateMachine);
					}
				}
			}

			if (parser == null) {
				LoggingService.LogDebug ($"XML parser failed to recover any state");
				parser = new XmlParser (StateMachine, false);
			}

			var end = Math.Min (point.Position, point.Snapshot.Length);
			for (int i = parser.Position; i < end; i++) {
				parser.Push (point.Snapshot[i]);
			}

			return parser;
		}

		Task<XmlParseResult> IXmlBackgroundParser.GetOrParseAsync (ITextSnapshot snapshot, CancellationToken token)
			=> GetOrParseAsync (snapshot, token)
			.ContinueWith (
				t => (XmlParseResult)t.Result,
				token,
				TaskContinuationOptions.ExecuteSynchronously,
				TaskScheduler.Default);

		XmlParseResult IXmlBackgroundParser.LastParseResult => LastParseResult;
		EventHandler<ParseCompletedEventArgs<XmlParseResult>> parseCompleted;
		object parseEventLock = new object ();

		event EventHandler<ParseCompletedEventArgs<XmlParseResult>> IXmlBackgroundParser.ParseCompleted {
			add {
				lock (parseEventLock) {
					if (parseCompleted == null) {
						ParseCompleted += XmlParseCompleted;
					}
					parseCompleted += value;
				}
			}
			remove {
				lock (parseEventLock) {
					parseCompleted -= value;
					if (parseCompleted == null) {
						ParseCompleted -= XmlParseCompleted;
					}
				}
			}
		}

		void XmlParseCompleted (object sender, ParseCompletedEventArgs<TResult> e)
			=> parseCompleted?.Invoke (this, new ParseCompletedEventArgs<XmlParseResult> (e.ParseResult, e.Snapshot));
	}

	interface IXmlBackgroundParser
	{
		XmlParser GetSpineParser (SnapshotPoint point);
		XmlParseResult LastParseResult { get; }
		Task<XmlParseResult> GetOrParseAsync (ITextSnapshot snapshot, CancellationToken token);
		event EventHandler<ParseCompletedEventArgs<XmlParseResult>> ParseCompleted;
	}

	public sealed class XmlBackgroundParser : XmlBackgroundParser<XmlParseResult>
	{
		protected override Task<XmlParseResult> StartParseAsync (ITextSnapshot2 snapshot, XmlParseResult previousParse, ITextSnapshot2 previousSnapshot, CancellationToken token)
		{
			var parser = new XmlParser (StateMachine, true);
			return Task.Run (() => {
				var length = snapshot.Length;
				for (int i = 0; i < length; i++) {
					parser.Push (snapshot[i]);
				}
				return new XmlParseResult (parser.Nodes.GetRoot (), parser.Diagnostics, snapshot);
			});
		}

		/// <summary>
		/// Gets a <see cref="XmlBackgroundParser{TResult}"/>-derived parser for the buffer if one is available.
		/// </summary>
		internal static bool TryGetParser (ITextBuffer buffer, out IXmlBackgroundParser parser)
			=> buffer.Properties.TryGetProperty (typeof (IXmlBackgroundParser), out parser);
	}

	public class XmlParseResult
	{
		public XmlParseResult (XDocument xDocument, List<XmlDiagnosticInfo> diagnostics, ITextSnapshot textSnapshot)
		{
			XDocument = xDocument;
			Diagnostics = diagnostics;
			TextSnapshot = textSnapshot;
		}

		public List<XmlDiagnosticInfo> Diagnostics { get; }
		public XDocument XDocument { get; }
		public ITextSnapshot TextSnapshot { get; }
	}
}
