// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

#if NETFRAMEWORK
#nullable disable warnings
#endif

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;

using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;

namespace MonoDevelop.Xml.Editor.Completion;

/// <summary>
/// Encapsulates the context of completion triggering in <see cref="XmlCompletionSource{TTriggerContext}"/> so that
/// subclassed completion sources can augment it with additional information.
/// </summary>
public class XmlCompletionTriggerContext
{
	public XmlCompletionTriggerContext (IAsyncCompletionSession session, SnapshotPoint triggerLocation, XmlSpineParser spineParser, CompletionTrigger trigger, SnapshotSpan applicableToSpan)
	{
		Session = session;
		TriggerLocation = triggerLocation;
		SpineParser = spineParser;
		Trigger = trigger;
		ApplicableToSpan = applicableToSpan;

		XmlTriggerReason = ConvertReason (trigger.Reason, trigger.Character);

		// FIXME: cache the value from InitializeCompletion somewhere?
		XmlTriggerKind = XmlCompletionTriggering.GetTrigger (spineParser, XmlTriggerReason, trigger.Character);
	}

	/// <summary>
	/// Initializes the node path. Only called if <see cref="IsSupportedTriggerReason"/> is true.
	/// </summary>
	public virtual Task InitializeNodePath (ILogger logger, CancellationToken cancellationToken)
	{
		SpineParser.TryGetNodePath (TriggerLocation.Snapshot, out List<XObject>? nodePath, cancellationToken: cancellationToken);
		NodePath = nodePath;

		// if we're completing an existing element, remove it from the path
		// so we don't get completions for its children instead
		if ((XmlTriggerKind == XmlCompletionTrigger.ElementName || XmlTriggerKind == XmlCompletionTrigger.Tag) && nodePath?.Count > 0) {
			if (nodePath[nodePath.Count - 1] is XElement leaf && leaf.Name.Length == ApplicableToSpan.Length) {
				nodePath.RemoveAt (nodePath.Count - 1);
			}
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Whether the <see cref="CompletionTriggerReason"/> is supported by this completion source.
	/// </summary>
	public virtual bool IsSupportedTriggerReason => XmlTriggerKind != XmlCompletionTrigger.None;

	public IAsyncCompletionSession Session { get; }
	public SnapshotPoint TriggerLocation { get; }
	public XmlSpineParser SpineParser { get; }
	public CompletionTrigger Trigger { get; }
	public SnapshotSpan ApplicableToSpan { get; }

	internal XmlCompletionTrigger XmlTriggerKind { get; }
	internal XmlTriggerReason XmlTriggerReason { get; }

	public List<XObject>? NodePath { get; private set; }

	internal static XmlTriggerReason ConvertReason (CompletionTriggerReason reason, char typedChar)
	{
		switch (reason) {
		case CompletionTriggerReason.Insertion:
			if (typedChar != '\0')
				return XmlTriggerReason.TypedChar;
			break;
		case CompletionTriggerReason.Backspace:
			return XmlTriggerReason.Backspace;
		case CompletionTriggerReason.Invoke:
		case CompletionTriggerReason.InvokeAndCommitIfUnique:
			return XmlTriggerReason.Invocation;
		}
		return XmlTriggerReason.Unknown;
	}
}
