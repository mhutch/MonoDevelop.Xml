// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Threading;

using MonoDevelop.Xml.Logging;

namespace MonoDevelop.Xml.Editor.Completion;

/// <summary>
/// Abstract implementation of <see cref="IAsyncCompletionCommitManager"/> that codifies the pattern of handling sessions and items annotated
/// with <see cref="TSessionTriggerKind"/> and <see cref="TItemKind"/> values on the session and item `Properties`, keyed by that type.
/// </summary>
/// <typeparam name="TSessionTriggerKind">Sessions annotated with this type will be handled.</typeparam>
/// <typeparam name="TItemKind">Within sessions annotated by <see cref="TSessionTriggerKind"/>, items annotated this type will receive special handling.</typeparam>
public abstract class AbstractCompletionCommitManager<TSessionTriggerKind, TItemKind> (
		ILogger logger, JoinableTaskContext joinableTaskContext, IEditorCommandHandlerServiceFactory commandServiceFactory
	) : IAsyncCompletionCommitManager
{
	readonly ILogger logger = logger;
	readonly JoinableTaskContext joinableTaskContext = joinableTaskContext;
	readonly IEditorCommandHandlerServiceFactory commandServiceFactory = commandServiceFactory;

	public abstract IEnumerable<char> PotentialCommitCharacters { get; }

	/// <summary>
	/// For any session with a <see cref="TSessionTriggerKind"/> value, completion will be cancelled if this returns false for that value.
	/// </summary>
	protected abstract bool IsCommitCharForTriggerKind (TSessionTriggerKind trigger, IAsyncCompletionSession session, ITextSnapshot snapshot, char typedChar);

	/// <summary>
	/// When committing an item with a <see cref="TSessionTriggerKind"/> value on a session with a <see cref="TSessionTriggerKind"/> value,
	/// this will be called to commit that item.
	/// </summary>
	protected abstract CommitResult TryCommitItemKind (TItemKind itemKind, IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token);

	public bool ShouldCommitCompletion (IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
		=> logger.InvokeAndLogExceptions (() => ShouldCommitCompletionInternal (session, location, typedChar, token));

	bool ShouldCommitCompletionInternal (IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
	{
		// Only handle sessions that are annotated with TTriggerKind.
		//
		// Although we aren't told what exact item we might be committing yet, the trigger tells us enough
		// about the kind of item to allow us to specialize the commit chars.
		//
		// NOTE: Returning false will not actually prevent the item from getting committed as another commit manager might handle it.
		// We must also explicitly cancel the commit in TryCommit.
		if (session.Properties.TryGetProperty (typeof (TSessionTriggerKind), out TSessionTriggerKind trigger)) {
			return IsCommitCharForTriggerKind (trigger, session, location.Snapshot, typedChar);
		};

		return false;
	}

	protected static readonly CommitResult CommitSwallowChar = new (true, CommitBehavior.SuppressFurtherTypeCharCommandHandlers);

	protected static readonly CommitResult CommitCancel = new (true, CommitBehavior.CancelCommit);

	public CommitResult TryCommit (IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
		=> logger.InvokeAndLogExceptions (() => TryCommitInternal (session, buffer, item, typedChar, token));

	CommitResult TryCommitInternal (IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
	{
		// This seems to get called if some other commit manager returned true from ShouldCommitCompletion even if we returned false.
		// So, we have to check again whether this is a session that we participated in and whether it is a valid commit char.

		// If we didn't participate in this session, let it fall through to other commit managers.
		if (!session.Properties.TryGetProperty (typeof (TSessionTriggerKind), out TSessionTriggerKind trigger)) {
			return CommitResult.Unhandled;
		};

		// If we did participate in the session and we don't consider the typed char to be a commit char for this trigger, cancel the commit.
		// This prevents lower-priority generic commit manager from committing items with chars that could have matched our items.
		if (typedChar != '\n' && typedChar != '\t') {
			// per-item CommitCharacters overrides the default commit chars
			if (item.CommitCharacters.IsDefaultOrEmpty) {
				if (!IsCommitCharForTriggerKind (trigger, session, buffer.CurrentSnapshot, typedChar)) {
					return CommitCancel;
				}
			} else if (item.CommitCharacters.Contains (typedChar)) {
				return CommitCancel;
			}
		}

		// If we didn't set an item kind, let it fall through to the default commit manager.
		// Note that this means that if we add any items and don't set an item kind, they will be handled by the
		// default manager, which may not commit on all of our commit chars.
		if (!item.Properties.TryGetProperty (typeof (TItemKind), out TItemKind itemKind)) {
			return CommitResult.Unhandled;
		}

		return TryCommitItemKind (itemKind, session, buffer, item, typedChar, token);
	}

	/// <summary>
	/// Re-invoke completion after the commit is complete.
	/// </summary>
	protected void RetriggerCompletion (ITextView textView)
	{
		var task = Task.Run (async () => {
			await joinableTaskContext.Factory.SwitchToMainThreadAsync ();
			commandServiceFactory.GetService (textView).Execute ((v, b) => new Microsoft.VisualStudio.Text.Editor.Commanding.Commands.InvokeCompletionListCommandArgs (v, b), null);
		});
		task.LogTaskExceptionsAndForget (logger);
	}

	/// <summary>
	/// Extend the span forwards to also include the specified character, if present.
	/// </summary>
	protected static void ExtendSpanToConsume (ref SnapshotSpan span, char charToConsume)
	{
		var snapshot = span.Snapshot;
		if (snapshot.Length > span.End && snapshot[span.End] == charToConsume) {
			span = new SnapshotSpan (snapshot, span.Start, span.Length + 1);
		}
	}

	/// <summary>
	/// Replace the span with the provided text and move the caret to the specified offset within that text.
	/// </summary>
	protected static void ReplaceSpanAndMoveCaret (IAsyncCompletionSession session, ITextBuffer buffer, SnapshotSpan spanToReplace, string insertionText, int caretOffsetWithinInsertedText)
	{
		ReplaceSpan (buffer, spanToReplace, insertionText);
		session.TextView.Caret.MoveTo (new SnapshotPoint (buffer.CurrentSnapshot, spanToReplace.Start.Position + caretOffsetWithinInsertedText));
	}

	/// <summary>
	/// Replace the span with the provided text.
	/// </summary>
	/// <param name="session"></param>
	protected static void ReplaceSpan (ITextBuffer buffer, SnapshotSpan spanToReplace, string insertionText)
	{
		var bufferEdit = buffer.CreateEdit ();
		bufferEdit.Replace (spanToReplace, insertionText);
		bufferEdit.Apply ();
	}

	/// <summary>
	/// Replace the session's ApplicableToSpan with the provided text.
	/// </summary>
	protected static void ReplaceApplicableToSpan (IAsyncCompletionSession session, ITextBuffer buffer, string text)
	{
		var span = session.ApplicableToSpan.GetSpan (buffer.CurrentSnapshot);
		ReplaceSpan (buffer, span, text);
	}
}