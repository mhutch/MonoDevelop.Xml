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
		ForwardParserCache<XmlParser> spineCache;

		protected override void Initialize ()
		{
			StateMachine = CreateParserStateMachine ();
			spineCache = new ForwardParserCache<XmlParser> (new XmlParser (StateMachine, false), Buffer);
			Buffer.Properties.AddProperty (typeof (IXmlBackgroundParser), this);
		}

		protected virtual XmlRootState CreateParserStateMachine () => new XmlRootState ();

		// the state machine does not store any state itself, so we can re-use it
		protected XmlRootState StateMachine { get; private set; }

		// this is intended to be called from the UI thread
		public XmlParser GetSpineParser (SnapshotPoint point)
		{
			spineCache.UpdatePosition (point.Position);
			return spineCache.Parser;
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
				return new XmlParseResult (parser.Nodes.GetRoot (), parser.Diagnostics);
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
		public XmlParseResult (XDocument xDocument, List<XmlDiagnosticInfo> diagnostics)
		{
			XDocument = xDocument;
			Diagnostics = diagnostics;
		}

        public List<XmlDiagnosticInfo> Diagnostics { get; }
        public XDocument XDocument { get; }
    }
}
