#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui;
/// <summary>
/// Extensions to <see cref="string"/> to support TUI text manipulation.
/// </summary>
public static class StringExtensions {

	private static readonly StringBuilder CachedStringBuilder = new StringBuilder ();

	/// <summary>
	/// Repeats the string <paramref name="n"/> times.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The text to repeat.</param>
	/// <param name="n">Number of times to repeat the text.</param>
	/// <returns>
	///  The text repeated if <paramref name="n"/> is greater than zero, 
	///  otherwise <see langword="null"/>.
	/// </returns>
	public static string? Repeat (this string str, int n)
	{
		if (n <= 0) {
			return null;
		}

		if (string.IsNullOrEmpty (str) || n == 1) {
			return str;
		}
		return new StringBuilder (str.Length * n)
			.Insert (0, str, n)
			.ToString ();
	}

	/// <summary>
	/// Gets the number of columns the string occupies in the terminal.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to measure.</param>
	/// <returns></returns>
	public static int GetColumns (this string? str)
	{
		if (str == null) {
			return 0;
		}

		return str.EnumerateRunes ().GetColumns ();
	}

	/// <summary>
	/// Gets the number of columns the string runes occupy in the terminal.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="StringRuneEnumerator"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="runes">The runes to measure.</param>
	/// <returns></returns>
	public static int GetColumns (this StringRuneEnumerator runes)
	{
		int sum = 0;
		foreach (var rune in runes) {
			sum += Math.Max (rune.GetColumns (), 0);
		}
		return sum;
	}

	/// <summary>
	/// Gets the number of runes in the string.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to count.</param>
	/// <returns></returns>
	public static int GetRuneCount (this string str) => str.EnumerateRunes ().Count ();

	/// <summary>
	/// Converts the string into a <see cref="Rune"/> array.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to convert.</param>
	/// <returns></returns>
	public static Rune [] ToRunes (this string str) => str.EnumerateRunes ().ToArray ();

	/// <summary>
	/// Converts the string into a <see cref="List{Rune}"/>.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to convert.</param>
	/// <returns></returns>
	public static List<Rune> ToRuneList (this string str) => str.EnumerateRunes ().ToList ();

	/// <summary>
	/// Unpacks the first UTF-8 encoding in the string and returns the rune and its width in bytes.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to decode.</param>
	/// <param name="start">Starting offset.</param>
	/// <param name="count">Number of bytes in the buffer, or -1 to make it the length of the buffer.</param>
	/// <returns></returns>
	public static (Rune Rune, int Size) DecodeRune (this string str, int start = 0, int count = -1)
	{
		int index = 0;
		foreach (Rune rune in str.EnumerateRunes ()) {
			if (index < start) {
				index++;
				continue;
			}

			if (count >= 0 && rune.Utf8SequenceLength >= count) {
				break;
			}

			return (rune, rune.Utf8SequenceLength);
		}
		var invalid = Rune.ReplacementChar;
		return (invalid, invalid.Utf8SequenceLength);
	}

	/// <summary>
	/// Unpacks the last UTF-8 encoding in the string.
	/// </summary>
	/// <remarks>
	/// This is a Terminal.Gui extension method to <see cref="string"/> to support TUI text manipulation.
	/// </remarks>
	/// <param name="str">The string to decode.</param>
	/// <param name="end">Rune index in string to stop at; if -1, use the buffer length.</param>
	/// <returns></returns>
	public static (Rune rune, int size) DecodeLastRune (this string str, int end = -1)
	{
		if (end <= -1) {
			var lastRune = Rune.ReplacementChar;
			foreach (Rune rune in str.EnumerateRunes ()) {
				lastRune = rune;
			}
			return (lastRune, lastRune.Utf8SequenceLength);
		}

		int index = 0;
		foreach (Rune rune in str.EnumerateRunes ()) {
			if (index == end) {
				return (rune, rune.Utf8SequenceLength);
			}
			index++;
		}
		var invalid = Rune.ReplacementChar;
		return (invalid, invalid.Utf8SequenceLength);
	}

	/// <summary>
	/// Converts a <see cref="Rune"/> generic collection into a string.
	/// </summary>
	/// <param name="runes">The enumerable rune to convert.</param>
	/// <returns></returns>
	public static string ToString (IEnumerable<Rune> runes)
	{
		// TODO: Use Microsoft.Extensions.ObjectPool to rent out StringBuilder.
		lock (CachedStringBuilder) {
			const int maxUtf16CharsPerRune = 2;
			Span<char> chars = stackalloc char[maxUtf16CharsPerRune];
			foreach (var rune in runes) {
				int charsWritten = rune.EncodeToUtf16 (chars);
				CachedStringBuilder.Append (chars [..charsWritten]);
			}
			string str = CachedStringBuilder.ToString();
			CachedStringBuilder.Clear ();
			return str;
		}
	}

	/// <summary>
	/// Converts read-only span of runes into a string.
	/// </summary>
	/// <param name="runes">The runes to convert.</param>
	/// <returns></returns>
	public static string ToString (in ReadOnlySpan<Rune> runes)
	{
		const int MaxUtf16CharsPerRune = 2;
		const int MaxStackallocBufferSize = 512; // ~1 kiB

		char[]? rentedArray = null;
		try {
			int bufferSize = runes.Length * MaxUtf16CharsPerRune;
			Span<char> buffer = bufferSize <= MaxStackallocBufferSize
				? stackalloc char[bufferSize]
				: (rentedArray = ArrayPool<char>.Shared.Rent(bufferSize));

			var remainingBuffer = buffer;
			foreach (var rune in runes) {
				int charsWritten = rune.EncodeToUtf16 (remainingBuffer);
				remainingBuffer = remainingBuffer [charsWritten..];
			}

			return new string (buffer [..^remainingBuffer.Length]);
		} finally {
			if (rentedArray != null) {
				ArrayPool<char>.Shared.Return (rentedArray);
			}
		}
	}

	/// <summary>
	/// Converts a byte generic collection into a string in the provided encoding (default is UTF8)
	/// </summary>
	/// <param name="bytes">The enumerable byte to convert.</param>
	/// <param name="encoding">The encoding to be used.</param>
	/// <returns></returns>
	public static string ToString (IEnumerable<byte> bytes, Encoding? encoding = null)
	{
		if (encoding == null) {
			encoding = Encoding.UTF8;
		}
		return encoding.GetString (bytes.ToArray ());
	}

	/// <summary>
	/// Converts read-only span of bytes into a string in the provided encoding (default is UTF8).
	/// </summary>
	/// <param name="bytes">The enumerable byte to convert.</param>
	/// <param name="encoding">The encoding to be used.</param>
	/// <returns></returns>
	public static string ToString (in ReadOnlySpan<byte> bytes, Encoding? encoding = null)
	{
		encoding ??= Encoding.UTF8;
		return encoding.GetString (bytes);
	}
}
