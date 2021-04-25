// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Xml.Editor.Completion;
using MonoDevelop.Xml.Parser;
using static MonoDevelop.Xml.Editor.Classification.XmlClassificationTypes;

namespace MonoDevelop.Xml.Editor.Classification
{
	[Export (typeof (IClassifierProvider))]
	[ContentType (XmlContentTypeNames.XmlCore)]
	sealed class XmlClassifierProvider : IClassifierProvider
	{
		internal IClassificationType[] Types;

		[ImportMany]
		private IEnumerable<Lazy<IXmlClassifierExtension, IOrderable>> xmlClassifierExtensions = null;

		private IEnumerable<IXmlClassifierExtension> orderedXmlClassifierExtensions;

		public IEnumerable<IXmlClassifierExtension> XmlClassifierExtensions {
			get {
				if (orderedXmlClassifierExtensions == null) {
					orderedXmlClassifierExtensions = Orderer.Order (xmlClassifierExtensions).Select (e => e.Value).ToArray ();
				}

				return orderedXmlClassifierExtensions;
			}
		}

		[ImportingConstructor]
		public XmlClassifierProvider (IClassificationTypeRegistryService classificationTypeRegistryService)
		{
			Types = new IClassificationType[]
			{
				classificationTypeRegistryService.GetClassificationType("text"),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlAttributeName),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlAttributeQuotes),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlAttributeValue),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlCDataSection),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlComment),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlDelimiter),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlEntityReference),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlName),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlProcessingInstruction),
				classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlText),
			};
		}

		public IClassifier GetClassifier (ITextBuffer textBuffer)
		{
			foreach (var extension in XmlClassifierExtensions) {
				if (!extension.ShouldClassify (textBuffer)) {
					return null;
				}
			}

			return new XmlClassifier (textBuffer, Types, CallExtensions);
		}

		internal void CallExtensions (ClassificationSpan classificationSpan, Action<ClassificationSpan> sink)
		{
			foreach (var extension in XmlClassifierExtensions) {
				if (extension.TryReplace (classificationSpan, sink)) {
					return;
				}
			}

			sink (classificationSpan);
		}
	}

	public sealed class XmlClassifier : IClassifier
	{
		private readonly IClassificationType[] types;
		private readonly Action<ClassificationSpan, Action<ClassificationSpan>> classificationReplacer;
		private readonly XmlBackgroundParser parser;

		public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

		public XmlClassifier (ITextBuffer buffer, IClassificationType[] types = null, Action<ClassificationSpan, Action<ClassificationSpan>> classificationReplacer = null)
		{
			this.types = types;
			this.classificationReplacer = classificationReplacer;
			this.parser = XmlBackgroundParser.GetParser (buffer);
		}

		private XmlSpineParser spineParser;
		private ITextSnapshot lastSnapshot;

		public IList<ClassificationSpan> GetClassificationSpans (SnapshotSpan span)
		{
			var snapshot = span.Snapshot;

			// don't reuse the previous span parser if it's not available, is from a different snapshot, is ahead
			// or is too far behind (it's cheaper to re-retrieve it from scratch than to fast-forward it too far)
			if (spineParser == null || lastSnapshot != snapshot || spineParser.Position > span.Start || spineParser.Position < span.Start.Position - 10000) {
				spineParser = parser.GetSpineParser (span.Start);
			} else {
				for (int i = spineParser.Position; i < span.Start; i++) {
					spineParser.Push (snapshot[i]);
				}
			}

			lastSnapshot = snapshot;

			IClassificationType previousClassification = null;
			int start = span.Start.Position;
			int end = span.End.Position;
			int previousSpanStart = start;

			var result = new List<ClassificationSpan> ();

#if DumpStates
			var sb = new StringBuilder ();
#endif

			for (int i = start; i < end; i++) {
				var previousState = spineParser.CurrentState;
				char ch = snapshot[i];
				spineParser.Push (ch);
				var currentState = spineParser.CurrentState;
				int currentStateTag = spineParser.GetContext ().StateTag;

#if DumpStates
				int previousStateTag = spineParser.GetContext ().StateTag;
				var previousName = GetStateName (previousState, previousStateTag);
				var currentName = GetStateName (currentState, currentStateTag);

				if (currentName != previousName) {
					previousName += " -> " + currentName;
				}

				sb.AppendLine ($"{ch} {previousName}");
#endif

				var currentClassificationType = GetClassification (previousState, ch, currentState, currentStateTag);
				var currentClassification = types[(int)currentClassificationType];
				if (currentClassification != previousClassification) {
					if (i > previousSpanStart) {
						var previousSpan = new SnapshotSpan (snapshot, previousSpanStart, i - previousSpanStart);
						var previousTag = new ClassificationSpan (previousSpan, previousClassification);
						AddSpan (previousTag, result.Add);
					}

					previousSpanStart = i;
					previousClassification = currentClassification;
				}
			}

#if DumpStates
			var dump = sb.ToString ();
			Console.Out.Write (dump);
#endif

			if (previousSpanStart < end) {
				var lastSpan = new SnapshotSpan (snapshot, previousSpanStart, end - previousSpanStart);
				var lastTag = new ClassificationSpan (lastSpan, previousClassification);
				AddSpan (lastTag, result.Add);
			}

			return result;
		}

		private void AddSpan (ClassificationSpan classificationSpan, Action<ClassificationSpan> callback)
		{
			if (classificationReplacer != null) {
				classificationReplacer (classificationSpan, callback);
			} else {
				callback (classificationSpan);
			}
		}

		private string GetStateName (XmlParserState state, int stateTag)
		{
			string result = null;

			if (state.Parent is XmlParserState parent) {
				result = GetStateName (parent, 0) + ".";
			}

			result += state.ToString ()
				.Replace ("MonoDevelop.Xml.Parser.", "")
				.Replace ("Xml", "")
				.Replace ("State", "");

			if (stateTag > 0) {
				result += stateTag.ToString ();
			}

			return result;
		}

		private XmlClassificationTypes GetClassification (
			XmlParserState previousState,
			char ch,
			XmlParserState currentState,
			int currentStateTag)
		{
			if (currentState is XmlTextState) {
				switch (ch) {
				case '&':
				case '#':
				case ';':
					return XmlEntityReference;
				}

				return XmlText;
			} else if (currentState is XmlCDataState) {
				if (ch == ']' && currentStateTag == 2) {
					return XmlDelimiter;
				}

				if (ch == '[' && previousState is XmlRootState) {
					return XmlDelimiter;
				}

				return XmlCDataSection;
			} else if (currentState is XmlProcessingInstructionState) {
				return XmlProcessingInstruction;
			} else if (currentState is XmlCommentState) {
				return XmlComment;
			} else if (currentState is XmlAttributeValueState) {
				if (ch == '"' || ch == '\'') {
					return XmlAttributeQuotes;
				}

				return XmlAttributeValue;
			}

			switch (ch) {
			case ' ':
			case '\n':
			case '\r':
			case '\t':
				return None;
			case '<':
			case '/':
			case '>':
			case '?':
			case '=':
			case '!':
			case '[':
			case ']':
			case '-':
				if (currentState is XmlRootState && currentStateTag == 3) {
					return XmlComment;
				}

				return XmlDelimiter;
			case '\'':
				return XmlText;
			case '"':
				if (currentState is XmlAttributeState) {
					return XmlAttributeQuotes;
				}

				return XmlText;
			case ':':
				if (currentState is XmlNameState) {
					return XmlName;
				}

				break;
			default:
				break;
			}

			if (currentState is XmlNameState) {
				var parent = currentState.Parent;
				if (parent is XmlAttributeState) {
					return XmlAttributeName;
				}

				return XmlName;
			} else if (currentState is XmlRootState) {
				if (currentStateTag == 4) {
					return XmlDelimiter;
				}
			}

			return None;
		}

		private void RaiseClassificationChanged (SnapshotSpan span)
		{
			ClassificationChanged?.Invoke (this, new ClassificationChangedEventArgs (span));
		}
	}
}
