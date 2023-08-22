// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// originally imported from https://raw.githubusercontent.com/microsoft/vs-editor-api/master/src/Editor/Language/Impl/Language/AsyncCompletion/DefaultCompletionItemManager.cs
// at commit cc54ccf435221210660b51f0f9ca49c0c99407fa

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.PatternMatching;
using Microsoft.VisualStudio.Utilities;

using MonoDevelop.Xml.Editor.Logging;
using MonoDevelop.Xml.Logging;

namespace MonoDevelop.Xml.Editor.Completion
{
	[Export (typeof (IAsyncCompletionItemManagerProvider))]
	[Name (XmlCompletionItemManager.ProviderName)]
	[ContentType (XmlContentTypeNames.XmlCore)]
	[TextViewRole (PredefinedTextViewRoles.Editable)]
	[Order(Before = PredefinedCompletionNames.DefaultCompletionItemManager)]
	internal sealed class XmlCompletionItemManagerProvider : IAsyncCompletionItemManagerProvider
	{
		readonly IPatternMatcherFactory patternMatcherFactory;
		readonly IEditorLoggerFactory loggerFactory;

		[ImportingConstructor]
		public XmlCompletionItemManagerProvider (IPatternMatcherFactory patternMatcherFactory, IEditorLoggerFactory loggerFactory)
		{
			this.patternMatcherFactory = patternMatcherFactory;
			this.loggerFactory = loggerFactory;
		}

		public IAsyncCompletionItemManager GetOrCreate (ITextView textView)
		{
			return textView.Properties.GetOrCreateSingletonProperty (() =>
				new XmlCompletionItemManager (patternMatcherFactory, loggerFactory.CreateLogger<XmlCompletionItemManager> (textView))
			);
		}
	}

	public class XmlCompletionItemManager : IAsyncCompletionItemManager
	{
		/// <summary>
		/// Allows derived classes to insert their <see cref="IAsyncCompletionItemManagerProvider"/> before this manager's provider.
		/// </summary>
		public const string ProviderName = nameof (XmlCompletionItemManager);

		protected IPatternMatcherFactory PatternMatcherFactory;
		protected ILogger Logger { get; }

		public XmlCompletionItemManager (IPatternMatcherFactory patternMatcherFactory, ILogger logger)
		{
			PatternMatcherFactory = patternMatcherFactory;
			Logger = logger;
		}

		Task<FilteredCompletionModel> IAsyncCompletionItemManager.UpdateCompletionListAsync (IAsyncCompletionSession session, AsyncCompletionSessionDataSnapshot data, CancellationToken token)
			=> UpdateCompletionListAsync (session, data, token).WithTaskExceptionLogger (Logger);

		protected virtual Task<FilteredCompletionModel> UpdateCompletionListAsync (IAsyncCompletionSession session, AsyncCompletionSessionDataSnapshot data, CancellationToken token)
			=> Task.Run (() => UpdateCompletionList (session, data, token), token);

		protected virtual FilteredCompletionModel UpdateCompletionList (IAsyncCompletionSession session, AsyncCompletionSessionDataSnapshot data, CancellationToken token)
		{
			// Filter by text
			var filterText = session.ApplicableToSpan.GetText (data.Snapshot);
			if (string.IsNullOrWhiteSpace (filterText)) {
				// There is no text filtering. Just apply user filters, sort alphabetically and return.
				IEnumerable<CompletionItem> listFiltered = data.InitialSortedList;
				if (data.SelectedFilters.Any (n => n.IsSelected)) {
					listFiltered = listFiltered.Where (n => ShouldBeInCompletionList (n, data.SelectedFilters));
				}
				var listSorted = listFiltered.OrderBy (n => n.SortText);
				var listHighlighted = listSorted.Select (n => new CompletionItemWithHighlight (n)).ToImmutableArray ();
				return new FilteredCompletionModel (listHighlighted, 0, data.SelectedFilters);
			}

			token.ThrowIfCancellationRequested ();

			// Pattern matcher not only filters, but also provides a way to order the results by their match quality.
			// The relevant CompletionItem is match.Item1, its PatternMatch is match.Item2
			var patternMatcher = PatternMatcherFactory.CreatePatternMatcher (
				filterText,
				new PatternMatcherCreationOptions (System.Globalization.CultureInfo.CurrentCulture, PatternMatcherCreationFlags.IncludeMatchedSpans));

			var matches = data.InitialSortedList
				// Perform pattern matching
				.Select (completionItem => (completionItem, patternMatcher.TryMatch (completionItem.FilterText)))
				// Pick only items that were matched, unless length of filter text is 1
				.Where (n => (filterText.Length == 1 || patternMatcher.HasInvalidPattern || n.Item2.HasValue));

			// See which filters might be enabled based on the typed code
			var textFilteredFilters = matches.SelectMany (n => n.completionItem.Filters).Distinct ();

			token.ThrowIfCancellationRequested ();

			// When no items are available for a given filter, it becomes unavailable. Expanders always appear available.
			var updatedFilters = ImmutableArray.CreateRange (data.SelectedFilters.Select (n => n.WithAvailability (
				  n.Filter is CompletionExpander ? true : textFilteredFilters.Contains (n.Filter))));

			// Filter by user-selected filters. The value on availableFiltersWithSelectionState conveys whether the filter is selected.
			var filterFilteredList = matches;
			if (data.SelectedFilters.Any (n => (n.Filter is CompletionExpander))) {
				filterFilteredList = matches.Where (n => ShouldBeInExpandedCompletionList (n.completionItem, data.SelectedFilters));
			}
			if (data.SelectedFilters.Any (n => !(n.Filter is CompletionExpander) && n.IsSelected)) {
				filterFilteredList = filterFilteredList.Where (n => ShouldBeInCompletionList (n.completionItem, data.SelectedFilters));
			}


			(CompletionItem completionItem, PatternMatch? patternMatch) bestMatch;
			if (patternMatcher.HasInvalidPattern) {
				// In a rare edge case where the pattern is invalid (e.g. it is just punctuation), see if any items contain what user typed.
				bestMatch = filterFilteredList.FirstOrDefault (n => n.completionItem.FilterText.IndexOf (filterText, 0, StringComparison.OrdinalIgnoreCase) > -1);
			} else {
				// 99.% cases fall here
				bestMatch = filterFilteredList.OrderByDescending (n => n.Item2.HasValue).ThenBy (n => n.Item2).FirstOrDefault ();
			}

			token.ThrowIfCancellationRequested ();

			var listWithHighlights = filterFilteredList.Select (n => {
				ImmutableArray<Span> safeMatchedSpans = ImmutableArray<Span>.Empty;
				if (n.completionItem.DisplayText.Equals (n.completionItem.FilterText, StringComparison.Ordinal)) {
					if (n.Item2.HasValue) {
						safeMatchedSpans = n.Item2.Value.MatchedSpans;
					}
				} else {
					// Matches were made against FilterText. We are displaying DisplayText. To avoid issues, re-apply matches for these items
					var newMatchedSpans = patternMatcher.TryMatch (n.completionItem.DisplayText);
					if (newMatchedSpans.HasValue) {
						safeMatchedSpans = newMatchedSpans.Value.MatchedSpans;
					}
				}

				if (safeMatchedSpans.IsDefaultOrEmpty) {
					return new CompletionItemWithHighlight (n.completionItem);
				} else {
					return new CompletionItemWithHighlight (n.completionItem, safeMatchedSpans);
				}
			}).ToImmutableArray ();

			token.ThrowIfCancellationRequested ();

			int selectedItemIndex = 0;
			var selectionHint = UpdateSelectionHint.NoChange;
			if (data.DisplaySuggestionItem) {
				selectedItemIndex = -1;
			} else {
				for (int i = 0; i < listWithHighlights.Length; i++) {
					if (listWithHighlights[i].CompletionItem == bestMatch.completionItem) {
						selectedItemIndex = i;
						selectionHint = UpdateSelectionHint.Selected;
						break;
					}
				}
			}

			return new FilteredCompletionModel (listWithHighlights, selectedItemIndex, updatedFilters, selectionHint, centerSelection: true, uniqueItem: null);
		}

		Task<ImmutableArray<CompletionItem>> IAsyncCompletionItemManager.SortCompletionListAsync (IAsyncCompletionSession session, AsyncCompletionSessionInitialDataSnapshot data, CancellationToken token)
			=> SortCompletionListAsync (session, data, token).WithTaskExceptionLogger (Logger);

		/// <summary>
		/// Asynchronously sorts the completion list. The default implementation starts a task to run <see cref="SortCompletionList"/> on a background thread.
		/// </summary>
		/// <remarks>Exceptions from this method are caught and logged to <see cref="Logger"/></remarks>
		protected virtual Task<ImmutableArray<CompletionItem>> SortCompletionListAsync (IAsyncCompletionSession session, AsyncCompletionSessionInitialDataSnapshot data, CancellationToken token)
			=> Task.Run (() => SortCompletionList (session, data, token), token);

		/// <summary>
		/// Synchronously sorts the completion list. This is called by the default implementation of <see cref="SortCompletionListAsync"/> and otherwise ignored.
		/// </summary>
		/// <remarks>Exceptions from this method are caught and logged to <see cref="Logger"/></remarks>
		protected virtual ImmutableArray<CompletionItem> SortCompletionList (IAsyncCompletionSession session, AsyncCompletionSessionInitialDataSnapshot data, CancellationToken token)
		{
			var sortComparer = GetSortComparer (token);
			return data.InitialList.Sort (sortComparer);
		}

		Comparison<CompletionItem> GetSortComparer (CancellationToken token) => (x, y) => {
			int cancelCount = 0;
			if (++cancelCount == 1000) {
				token.ThrowIfCancellationRequested ();
				cancelCount = 0;
			}

			return string.Compare (x.SortText, y.SortText, StringComparison.Ordinal);
		};

		#region Filtering

		static bool ShouldBeInCompletionList (CompletionItem item, ImmutableArray<CompletionFilterWithState> filtersWithState)
		{
			// Filter out items which don't have a filter which matches selected Filter Button
			foreach (var filterWithState in filtersWithState.Where (n => !(n.Filter is CompletionExpander) && n.IsSelected)) {
				if (item.Filters.Any (n => n == filterWithState.Filter)) {
					return true;
				}
			}
			return false;
		}

		static bool ShouldBeInExpandedCompletionList (CompletionItem item, ImmutableArray<CompletionFilterWithState> filtersWithState)
		{
			// Remove items which have a filter which matches deselected Expander Button
			foreach (var filterWithState in filtersWithState.Where (n => n.Filter is CompletionExpander && !(n.IsSelected))) {
				if (item.Filters.Any (n => n is CompletionExpander && n == filterWithState.Filter)) {
					return false;
				}
			}
			return true;
		}

		#endregion
	}
}