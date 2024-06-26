// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using MonoDevelop.Xml.Dom;

namespace MonoDevelop.Xml.Tests.Utils;

/// <summary>
/// Represents text with marked spans and/or positions
/// </summary>
public class TextWithMarkers
{
	TextWithMarkers (string text, char[] markerChars, List<int>[] markedPositionsById)
	{
		Text = text;
		this.markerChars = markerChars;
		this.markedPositionsById = markedPositionsById;
	}

	readonly char[] markerChars;
	readonly List<int>[] markedPositionsById;

	/// <summary>
	/// The text with the marker characters removed
	/// </summary>
	public string Text { get; }

	int GetMarkerId (char? markerChar)
	{
		int markerId;
		if (markerChar is null) {
			if (markedPositionsById.Length != 1) {
				throw new ArgumentException ("More than one marker char was used in this document, you must specify which one", nameof (markerChar));
			}
			markerId = 0;
		} else {
			markerId = Array.IndexOf (markerChars, markerChar);
			if (markerId < 0) {
				throw new ArgumentException ($"The character '{markerChar}' was not used as a marker", nameof (markerChar));
			}
		}
		return markerId;
	}

	/// <summary>
	/// Gets all the marked positions for the specified <paramref name="markerChar"/>
	/// </summary>
	/// <param name="markerChar">Which marker character to use. May be null if only one marker character was specified when creating the <see cref="TextWithMarkers"/></param>
	public IList<int> GetMarkedPositions (char? markerChar = null) => markedPositionsById[GetMarkerId (markerChar)];

	/// <summary>
	/// Gets the single position marked with the specified <paramref name="markerChar"/>
	/// </summary>
	/// <param name="markerChar">Which marker character to use. May be null if only one marker character was specified when creating the <see cref="TextWithMarkers"/></param>
	/// <returns>The position</returns>
	/// <exception cref="TextWithMarkersMismatchException">Did not find exactly one marker for <paramref name="markerChar"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was null but multiple markers were specified when creating the <see cref="TextWithMarkers"/></exception>

	public int GetMarkedPosition (char? markerChar = null)
	{
		if (!TryGetMarkedPosition (out int position, markerChar)) {
			ThrowExactMismatchException (markerChar, 1, "position");
		}
		return position;
	}

	/// <summary>
	/// Tries to get the single position marked with the specified <paramref name="markerChar"/>.
	/// </summary>
	/// <param name="markerChar">Which marker character to use. May be null if only one marker character was specified when creating the <see cref="TextWithMarkers"/></param>
	/// <param name="span">The position, if this method returned <c>true</c>, otherwise <c>default</c></param>
	/// <returns>Whether a single position was found for <paramref name="markerChar"/>. More than one marker will cause an exception to be thrown</returns>
	/// <remarks>The presence of more than one marker will cause an exception to be thrown</remarks>
	/// <exception cref="TextWithMarkersMismatchException">More than one marker was found for <paramref name="markerChar"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was null but multiple markers were specified when creating the <see cref="TextWithMarkers"/></exception>

	public bool TryGetMarkedPosition (out int position, char? markerChar = null)
	{
		var id = GetMarkerId (markerChar);
		var positions = markedPositionsById[id];

		if (positions.Count == 1) {
			position = positions[0];
			return true;
		}

		if (positions.Count > 1) {
			ThrowZeroOrNMismatchException (markerChar, positions.Count, "position");
		}

		position = default;
		return false;
	}

	/// <summary>
	/// Gets the single span marked with the specified <paramref name="markerChar"/>.
	/// </summary>
	/// <param name="markerChar">Which marker character to use. May be null if only one marker character was specified when creating the <see cref="TextWithMarkers"/></param>
	/// <param name="allowZeroWidthSingleMarker">Whether to allow use of a single marker character when the span is zero-width</param>
	/// <returns>The <see cref="TextSpan"/> representing the marked span</returns>
	/// <exception cref="TextWithMarkersMismatchException">Did not find exactly one span (two markers) for <paramref name="markerChar"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was null but multiple markers were specified when creating the <see cref="TextWithMarkers"/></exception>
	public TextSpan GetMarkedSpan (char? markerChar = null, bool allowZeroWidthSingleMarker = false)
	{
		if (!TryGetMarkedSpan (out var span, markerChar, allowZeroWidthSingleMarker)) {
			ThrowExactMismatchException (markerChar, 2, "span");
		}
		return span;
	}

	/// <summary>
	/// Tries to get the span marked with the specified <paramref name="markerChar"/>.
	/// </summary>
	/// <param name="markerChar">Which marker character to use. May be null if only one marker character was specified when creating the <see cref="TextWithMarkers"/></param>
	/// <param name="span">The <see cref="TextSpan"/> representing the marked span, if this method returned <c>true</c>, otherwise <c>default</c></param>
	/// <param name="allowZeroWidthSingleMarker">Whether to allow use of a single marker character when the span is zero-width</param>
	/// <returns>Whether a single span was found for <paramref name="markerChar"/></returns>
	/// <exception cref="TextWithMarkersMismatchException">Did not find exactly zero or one spans (zero or two markers) for <paramref name="markerChar"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was null but multiple markers were specified when creating the <see cref="TextWithMarkers"/></exception>
	public bool TryGetMarkedSpan (out TextSpan span, char? markerChar = null, bool allowZeroWidthSingleMarker = false)
	{
		var id = GetMarkerId (markerChar);
		var positions = markedPositionsById[id];

		if (allowZeroWidthSingleMarker && positions.Count == 1) {
			span = new TextSpan (positions[0], 0);
			return true;
		}

		if (positions.Count == 2) {
			int start = positions[0];
			int end = positions[1];
			span = TextSpan.FromBounds (start, end);
			return true;
		} else if (positions.Count > 2) {
			ThrowZeroOrNMismatchException(markerChar, 2, "span");
		}

		span = default;
		return false;
	}

	static void CheckStartEndMarkersDifferent(char spanStartMarker, char spanEndMarker)
	{
		if (spanStartMarker == spanEndMarker) {
			throw new ArgumentException ("Cannot use same character as both start and end markers with this overload");
		}
	}

	/// <summary>
	/// Gets the single span marked with the specified <paramref name="markerChar"/>.
	/// </summary>
	/// <param name="spanStartMarker">The marker character that indicates the start of the span</param>
	/// <param name="spanEndMarker">The marker character that indicates the end of the span</param>
	/// <returns>The <see cref="TextSpan"/> representing the marked span</returns>
	/// <exception cref="TextWithMarkersMismatchException">Did not find exactly one span</exception>
	/// <exception cref="TextWithMarkersMismatchException">The number of <paramref name="spanStartMarker"/> characters did not match the number of <paramref name="spanEndMarker"/> characters</exception>
	/// <exception cref="TextWithMarkersMismatchException">The span end marker was found before the start marker</exception>
	/// <exception cref="ArgumentException">The <paramref name="spanStartMarker"/> or <paramref name="spanEndMarker"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	/// <exception cref="ArgumentException">Cannot use same character as both start and end markers with this overload</exception>
	public TextSpan GetMarkedSpan (char spanStartMarker, char spanEndMarker)
	{
		if (!TryGetMarkedSpan (out var span, spanStartMarker, spanEndMarker)) {
			ThrowExactSpanMismatchException (spanStartMarker, spanEndMarker, 1);
		}
		return span;
	}

	/// <summary>
	/// Tries to get the span marked with the specified <paramref name="spanStartMarker"/> and <paramref name="spanEndMarker"/>
	/// </summary>
	/// <param name="spanStartMarker">The marker character that indicates the start of the span</param>
	/// <param name="spanEndMarker">The marker character that indicates the end of the span</param>
	/// <returns>Whether a single span was found for the <paramref name="spanStartMarker"/> and <paramref name="spanEndMarker"/></returns>
	/// <exception cref="TextWithMarkersMismatchException">Multiple spans were found, must be zero or one</exception>
	/// <exception cref="TextWithMarkersMismatchException">The number of <paramref name="spanStartMarker"/> characters did not match the number of <paramref name="spanEndMarker"/> characters</exception>
	/// <exception cref="TextWithMarkersMismatchException">A span end marker was found before the corresponding start marker</exception>
	/// <exception cref="ArgumentException">The <paramref name="spanStartMarker"/> or <paramref name="spanEndMarker"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	/// <exception cref="ArgumentException">Cannot use same character as both start and end markers with this overload</exception>
	public bool TryGetMarkedSpan (out TextSpan span, char spanStartMarker, char spanEndMarker)
	{
		CheckStartEndMarkersDifferent (spanStartMarker, spanEndMarker);

		var startPositions = GetMarkedPositions (spanStartMarker);
		var endPositions = GetMarkedPositions (spanEndMarker);

		if (startPositions.Count == 0 && endPositions.Count == 0) {
			span = default;
			return false;
		}

		if (startPositions.Count != endPositions.Count) {
			ThrowNonEqualMismatchException (spanStartMarker, startPositions, spanEndMarker, endPositions);
		} else if (startPositions.Count > 1) {
			ThrowZeroOrOneSpanMismatchException (spanStartMarker, spanEndMarker, startPositions);
		}

		int start = startPositions[0];
		int end = endPositions[0];
		if (end < start) {
			ThrowEndBeforeStartMismatchException (spanStartMarker, start, spanEndMarker, end);
		}
		span = TextSpan.FromBounds (start, end);
		return true;
	}

	/// <summary>
	/// Gets all spans marked with the specified <paramref name="markerChar"/>
	/// </summary>
	/// <param name="markerChar">Which marker character to use. May be null if only one marker character was specified when creating the <see cref="TextWithMarkers"/></param>
	/// <returns>An array of <see cref="TextSpan"/> representing the marker spans. May be empty if the text did not contain any <paramref name="markerChar"/> characters</returns>
	/// <exception cref="TextWithMarkersMismatchException">An odd number of markers were found for <paramref name="markerChar"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChar"/> was null but multiple markers were specified when creating the <see cref="TextWithMarkers"/></exception>
	public TextSpan[] GetMarkedSpans (char? markerChar = null)
	{
		var markers = GetMarkedPositions (markerChar);

		if (markers.Count % 2 != 0) {
			ThrowEvenMismatchException (markerChar, "spans");
		}

		var spans = new TextSpan [markers.Count / 2];

		for (int i = 0; i < spans.Length; i++) {
			int j = i * 2;
			int start = markers[j];
			int end = markers[j + 1];
			spans[i] = TextSpan.FromBounds (start, end);
		}

		return spans;
	}

	/// <summary>
	/// Gets all spans marked with the specified <paramref name="spanStartMarker"/> and <paramref name="spanEndMarker"/>
	/// </summary>
	/// <param name="spanStartMarker">Which marker character to use at the start each span</param>
	/// <param name="spanEndMarker">Which marker character to use at the end each span</param>
	/// <returns>An array of <see cref="TextSpan"/> representing the marker spans. May be empty if the text did not contain any <paramref name="spanStartMarker"/> and <paramref name="spanEndMarker"/> characters</returns>
	/// <exception cref="TextWithMarkersMismatchException">The number of <paramref name="spanStartMarker"/> characters did not match the number of <paramref name="spanEndMarker"/> characters</exception>
	/// <exception cref="TextWithMarkersMismatchException">A span end marker was found before the corresponding start marker</exception>
	/// <exception cref="ArgumentException">The <paramref name="spanStartMarker"/> or <paramref name="spanEndMarker"/> was not specified when creating the <see cref="TextWithMarkers"/></exception>
	public TextSpan[] GetMarkedSpans (char spanStartMarker, char spanEndMarker)
	{
		var startPositions = GetMarkedPositions (spanStartMarker);
		var endPositions = GetMarkedPositions (spanEndMarker);

		if (startPositions.Count != endPositions.Count) {
			ThrowNonEqualMismatchException (spanStartMarker, startPositions, spanEndMarker, endPositions);
		}

		var spans = new TextSpan [startPositions.Count];

		for (int i = 0; i > spans.Length; i++) {
			int start = startPositions[i];
			int end = endPositions[i];
			if (end < start) {
				ThrowEndBeforeStartMismatchException (spanStartMarker, start, spanEndMarker, end);
			}
			spans[i] = TextSpan.FromBounds (start, end);
		}

		return spans;
	}

	/// <summary>
	/// Parse a string with marker characters into a <see cref="TextWithMarkers"/>
	/// </summary>
	/// <param name="textWithMarkers">The text with markers</param>
	/// <param name="markerChars">The marker characters</param>
	/// <returns>A <see cref="TextWithMarkers"/> representing the marker-free text and the marked positions and/or spans</returns>
	/// <exception cref="ArgumentNullException">The <paramref name="textWithMarkers"/> was null</exception>
	/// <exception cref="ArgumentNullException">The <paramref name="markerChars"/> was null</exception>
	/// <exception cref="ArgumentException">The <paramref name="markerChars"/> array was empty</exception>
	/// <exception cref="ArgumentException">A character was included multiple times in the <paramref name="markerChars"/> array</exception>
	public static TextWithMarkers Parse (string textWithMarkers, params char[] markerChars)
	{
		if (textWithMarkers is null) {
			throw new ArgumentNullException (nameof (textWithMarkers));
		}

		if (markerChars is null) {
			throw new ArgumentNullException (nameof (markerChars));
		}

		if (markerChars.Length == 0) {
			throw new ArgumentException ("Array of marker characters is empty", nameof (markerChars));
		}

		for (int i = 0; i < markerChars.Length; i++) {
			char c = markerChars [i];
			for (int j = i + 1; j < markerChars.Length; j++) {
				if (c == markerChars[j]) {
					throw new ArgumentException ($"The character '{c}' was included multiple times in {nameof (markerChars)}", nameof (markerChars));
				}
			}
		}

		var markerIndices = Array.ConvertAll (markerChars, c => new List<int> ());

		var sb = new StringBuilder (textWithMarkers.Length);

		for (int i = 0; i < textWithMarkers.Length; i++) {
			var c = textWithMarkers[i];
			int markerId = Array.IndexOf (markerChars, c);
			if (markerId > -1) {
				markerIndices[markerId].Add (sb.Length);
			} else {
				sb.Append (c);
			}
		}

		return new (sb.ToString (), markerChars, markerIndices);
	}

	/// <summary>
	/// Parse a string with marker characters into a <see cref="TextWithMarkers"/>
	/// </summary>
	/// <param name="textWithMarkers">The text with markers</param>
	/// <param name="markerChar">The marker character</param>
	/// <returns>A <see cref="TextWithMarkers"/> representing the marker-free text and the marked positions and/or spans</returns>
	/// <exception cref="ArgumentNullException">The <paramref name="textWithMarkers"/> was null</exception>

	public static TextWithMarkers Parse (string textWithMarkers, char markerChar)
	{
		if (textWithMarkers is null) {
			throw new ArgumentNullException (nameof (textWithMarkers));
		}

		var markerIndices = new List<int> ();

		var sb = new StringBuilder (textWithMarkers.Length);

		for (int i = 0; i < textWithMarkers.Length; i++) {
			var c = textWithMarkers[i];
			if (c == markerChar) {
				markerIndices.Add (sb.Length);
			} else {
				sb.Append (c);
			}
		}

		return new (sb.ToString (), [markerChar], [markerIndices]);
	}

	/// <summary>
	/// Extract a single marked position and marker-free text from a string with a single marker character
	/// </summary>
	/// <param name="textWithMarkers">The text with a marker character</param>
	/// <param name="markerChar">The marker character</param>
	/// <returns>A tuple with the marker-free text and the marked position</returns>
	/// <exception cref="TextWithMarkersMismatchException">Did not find exactly one marker for <paramref name="markerChar"/></exception>
	/// <exception cref="ArgumentNullException">The <paramref name="textWithMarker"/> was null</exception>
	public static (string text, int position) ExtractSinglePosition (string textWithMarker, char markerChar = '|')
	{
		var parsed = Parse (textWithMarker, markerChar);
		var caret = parsed.GetMarkedPosition (markerChar);
		return (parsed.Text, caret);
	}

	/// <summary>
	/// Extract a single marked span and marker-free text from a string with exactly two marker characters
	/// </summary>
	/// <param name="textWithMarkers">The text with marker characters</param>
	/// <param name="markerChar">The marker character</param>
	/// <param name="allowZeroWidthSingleMarker">Whether to allow use of a single marker character when the span is zero-width</param>
	/// <returns>A tuple with the marker-free text and the marked span</returns>
	/// <exception cref="TextWithMarkersMismatchException">Did not find exactly one span (two markers) for <paramref name="markerChar"/></exception>
	/// <exception cref="ArgumentNullException">The <paramref name="textWithMarkers"/> was null</exception>
	public static (string text, TextSpan span) ExtractSingleSpan (string textWithMarkers, char markerChar = '|', bool allowZeroWidthSingleMarker = false)
	{
		var parsed = Parse (textWithMarkers, markerChar);
		var span = parsed.GetMarkedSpan (markerChar, allowZeroWidthSingleMarker);
		return (parsed.Text, span);
	}

	/// <summary>
	/// Extract a single marked span and marker-free text from a string with exactly two marker characters
	/// </summary>
	/// <param name="textWithMarkers">The text with marker characters</param>
	/// <param name="spanStartMarker">The marker character that indicates the start of the span</param>
	/// <param name="spanEndMarker">The marker character that indicates the end of the span</param>
	/// <returns>A tuple with the marker-free text and the marked span</returns>
	/// <exception cref="TextWithMarkersMismatchException">Did not find exactly one span</exception>
	/// <exception cref="TextWithMarkersMismatchException">The number of <paramref name="spanStartMarker"/> characters did not match the number of <paramref name="spanEndMarker"/> characters</exception>
	/// <exception cref="TextWithMarkersMismatchException">The span end marker was found before the start marker</exception>
	/// <exception cref="ArgumentNullException">The <paramref name="textWithMarkers"/> was null</exception>
	/// <exception cref="ArgumentException">Cannot use same character as both start and end markers with this overload</exception>
	public static (string text, TextSpan span) ExtractSingleSpan (string textWithMarkers, char spanStartMarker = '^', char spanEndMarker = '$')
	{
		CheckStartEndMarkersDifferent (spanStartMarker, spanEndMarker);
		var parsed = Parse (textWithMarkers, [ spanStartMarker, spanEndMarker ]);
		var span = parsed.GetMarkedSpan (spanStartMarker, spanEndMarker);
		return (parsed.Text, span);
	}

	[DoesNotReturn]
	void ThrowExactMismatchException (char? markerChar, int expected, string kind)
	{
		var id = GetMarkerId (markerChar);
		var positions = markedPositionsById[id];
		throw new TextWithMarkersMismatchException ($"Expected {expected} '{markerChars[id]}' characters for {kind}, found {positions.Count}");
	}

	[DoesNotReturn]
	void ThrowExactSpanMismatchException (char startMarker, char endMarker, int expected)
	{
		var positions = GetMarkedPositions (startMarker);
		throw new TextWithMarkersMismatchException ($"Expected {expected} '{startMarker}' start marker and '{endMarker}' end marker characters for span, found {positions.Count}");
	}

	[DoesNotReturn]
	void ThrowZeroOrNMismatchException (char? markerChar, int expected, string kind)
	{
		var id = GetMarkerId (markerChar);
		var positions = markedPositionsById[id];
		throw new TextWithMarkersMismatchException ($"Expected zero or {expected} '{markerChars[id]}' characters for {kind}, found {positions.Count}");
	}

	[DoesNotReturn]
	void ThrowZeroOrOneSpanMismatchException (char startMarker, char endMarker, IList<int> startPositions)
	{
		throw new TextWithMarkersMismatchException ($"Expected zerone or one '{startMarker}' start marker and '{endMarker}' end marker characters for span, found {startPositions.Count}");
	}

	[DoesNotReturn]
	void ThrowEvenMismatchException (char? markerChar, string kind)
	{
		var id = GetMarkerId (markerChar);
		var positions = markedPositionsById[id];
		throw new TextWithMarkersMismatchException ($"Expected even number of '{markerChars[id]}' characters for {kind}, found {positions.Count}");
	}

	[DoesNotReturn]
	void ThrowEndBeforeStartMismatchException (char spanStartMarker, int startMarkerPosition, char spanEndMarker, int endMarkerPosition)
	{
		throw new TextWithMarkersMismatchException ($"Span end character '{spanEndMarker}' position {endMarkerPosition} is before span start character '{spanStartMarker}' position {startMarkerPosition}");
	}

	[DoesNotReturn]
	void ThrowNonEqualMismatchException (char spanStartMarker, IList<int> startPositions, char spanEndMarker, IList<int> endPositions)
	{
		throw new TextWithMarkersMismatchException ($"Expected number of '{spanStartMarker}' span start markers to equal number of '{spanEndMarker}' span end markers, found {startPositions.Count} != {endPositions.Count}");
	}
}

class TextWithMarkersMismatchException : Exception
{
	public TextWithMarkersMismatchException (string message) : base (message)
	{
	}
}