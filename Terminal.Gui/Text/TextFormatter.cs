using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum TextAlignment {
		/// <summary>
		/// The text will be left-aligned.
		/// </summary>
		Left,
		/// <summary>
		/// The text will be right-aligned.
		/// </summary>
		Right,
		/// <summary>
		/// The text will be centered horizontally.
		/// </summary>
		Centered,
		/// <summary>
		/// The text will be justified (spaces will be added to existing spaces such that
		/// the text fills the container horizontally).
		/// </summary>
		Justified
	}

	/// <summary>
	/// Vertical text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum VerticalTextAlignment {
		/// <summary>
		/// The text will be top-aligned.
		/// </summary>
		Top,
		/// <summary>
		/// The text will be bottom-aligned.
		/// </summary>
		Bottom,
		/// <summary>
		/// The text will centered vertically.
		/// </summary>
		Middle,
		/// <summary>
		/// The text will be justified (spaces will be added to existing spaces such that
		/// the text fills the container vertically).
		/// </summary>
		Justified
	}

	/// TextDirection  [H] = Horizontal  [V] = Vertical
	/// =============
	/// LeftRight_TopBottom [H] Normal
	/// TopBottom_LeftRight [V] Normal
	/// 
	/// RightLeft_TopBottom [H] Invert Text
	/// TopBottom_RightLeft [V] Invert Lines
	/// 
	/// LeftRight_BottomTop [H] Invert Lines
	/// BottomTop_LeftRight [V] Invert Text
	/// 
	/// RightLeft_BottomTop [H] Invert Text + Invert Lines
	/// BottomTop_RightLeft [V] Invert Text + Invert Lines
	///
	/// <summary>
	/// Text direction enumeration, controls how text is displayed.
	/// </summary>
	public enum TextDirection {
		/// <summary>
		/// Normal horizontal direction.
		/// <code>HELLO<br/>WORLD</code>
		/// </summary>
		LeftRight_TopBottom,
		/// <summary>
		/// Normal vertical direction.
		/// <code>H W<br/>E O<br/>L R<br/>L L<br/>O D</code>
		/// </summary>
		TopBottom_LeftRight,
		/// <summary>
		/// This is a horizontal direction. <br/> RTL
		/// <code>OLLEH<br/>DLROW</code>
		/// </summary>
		RightLeft_TopBottom,
		/// <summary>
		/// This is a vertical direction.
		/// <code>W H<br/>O E<br/>R L<br/>L L<br/>D O</code>
		/// </summary>
		TopBottom_RightLeft,
		/// <summary>
		/// This is a horizontal direction.
		/// <code>WORLD<br/>HELLO</code>
		/// </summary>
		LeftRight_BottomTop,
		/// <summary>
		/// This is a vertical direction.
		/// <code>O D<br/>L L<br/>L R<br/>E O<br/>H W</code>
		/// </summary>
		BottomTop_LeftRight,
		/// <summary>
		/// This is a horizontal direction.
		/// <code>DLROW<br/>OLLEH</code>
		/// </summary>
		RightLeft_BottomTop,
		/// <summary>
		/// This is a vertical direction.
		/// <code>D O<br/>L L<br/>R L<br/>O E<br/>W H</code>
		/// </summary>
		BottomTop_RightLeft
	}

	/// <summary>
	/// Provides text formatting. Supports <see cref="View.HotKey"/>s, horizontal alignment, vertical alignment, multiple lines, and word-based line wrap.
	/// </summary>
	public class TextFormatter {

		#region Static Members

		internal static string StripCRLF (string str, bool keepNewLine = false)
		{
			const string newlineChars = "\r\n";

			var remaining = str.AsSpan ();
			int firstNewlineCharIndex = remaining.IndexOfAny (newlineChars);
			// Early exit to avoid StringBuilder allocation if there are no newline characters.
			if (firstNewlineCharIndex < 0) {
				return str;
			}

			var stringBuilder = new StringBuilder();
			var firstSegment = remaining[..firstNewlineCharIndex];
			stringBuilder.Append (firstSegment);

			// The first newline is not skipped at this point because the "keepNewLine" condition has not been evaluated.
			// This means there will be 1 extra iteration because the same newline index is checked again in the loop.
			remaining = remaining [firstNewlineCharIndex..];

			while (remaining.Length > 0) {
				int newlineCharIndex = remaining.IndexOfAny (newlineChars);
				if (newlineCharIndex < 0) {
					break;
				}

				var segment = remaining[..newlineCharIndex];
				stringBuilder.Append (segment);

				int stride = segment.Length;
				// Evaluate how many newline characters to preserve.
				char newlineChar = remaining [newlineCharIndex];
				if (newlineChar == '\n') {
					stride++;
					if (keepNewLine) {
						stringBuilder.Append ('\n');
					}
				} else /* '\r' */ {
					int nextCharIndex = newlineCharIndex + 1;
					bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
					if (crlf) {
						stride += 2;
						if (keepNewLine) {
							stringBuilder.Append ('\n');
						}
					} else {
						stride++;
						if (keepNewLine) {
							stringBuilder.Append ('\r');
						}
					}
				}
				remaining = remaining [stride..];
			}
			stringBuilder.Append (remaining);
			return stringBuilder.ToString ();
		}

		/// <summary>
		/// Span buffer variant of <see cref="StripCRLF(string, bool)"/>.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="buffer"></param>
		/// <param name="keepNewLine"></param>
		/// <returns>Number of chars written to the buffer.</returns>
		internal static int StripCRLF (in ReadOnlySpan<char> str, in Span<char> buffer, bool keepNewLine = false)
		{
			const string newlineChars = "\r\n";

			var remaining = str;
			int firstNewlineCharIndex = remaining.IndexOfAny (newlineChars);
			// Early exit if there are no newline characters.
			if (firstNewlineCharIndex < 0) {
				str.CopyTo (buffer);
				return str.Length;
			}

			var remainingBuffer = buffer;
			var firstSegment = remaining[..firstNewlineCharIndex];
			firstSegment.CopyTo (remainingBuffer);
			remainingBuffer = remainingBuffer [firstSegment.Length..];

			// The first newline is not skipped at this point because the "keepNewLine" condition has not been evaluated.
			// This means there will be 1 extra iteration because the same newline index is checked again in the loop.
			remaining = remaining [firstNewlineCharIndex..];

			while (remaining.Length > 0) {
				int newlineCharIndex = remaining.IndexOfAny (newlineChars);
				if (newlineCharIndex < 0) {
					break;
				}

				var segment = remaining[..newlineCharIndex];
				segment.CopyTo (remainingBuffer);
				remainingBuffer = remainingBuffer [segment.Length..];

				int stride = segment.Length;
				// Evaluate how many newline characters to preserve.
				char newlineChar = remaining [newlineCharIndex];
				if (newlineChar == '\n') {
					stride++;
					if (keepNewLine) {
						remainingBuffer [0] = '\n';
						remainingBuffer = remainingBuffer [1..];
					}
				} else /* '\r' */ {
					int nextCharIndex = newlineCharIndex + 1;
					bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
					if (crlf) {
						stride += 2;
						if (keepNewLine) {
							remainingBuffer [0] = '\n';
							remainingBuffer = remainingBuffer [1..];
						}
					} else {
						stride++;
						if (keepNewLine) {
							remainingBuffer [0] = '\r';
							remainingBuffer = remainingBuffer [1..];
						}
					}
				}
				remaining = remaining [stride..];
			}
			remaining.CopyTo (remainingBuffer);
			remainingBuffer = remainingBuffer [remaining.Length..];

			return buffer.Length - remainingBuffer.Length;
		}

		internal static string ReplaceCRLFWithSpace (string str)
		{
			const string newlineChars = "\r\n";

			var remaining = str.AsSpan ();
			int firstNewlineCharIndex = remaining.IndexOfAny (newlineChars);
			// Early exit to avoid StringBuilder allocation if there are no newline characters.
			if (firstNewlineCharIndex < 0) {
				return str;
			}

			var stringBuilder = new StringBuilder();
			var firstSegment = remaining[..firstNewlineCharIndex];
			stringBuilder.Append (firstSegment);

			// The first newline is not skipped at this point because the condition has not been evaluated.
			// This means there will be 1 extra iteration because the same newline index is checked again in the loop.
			remaining = remaining [firstNewlineCharIndex..];

			while (remaining.Length > 0) {
				int newlineCharIndex = remaining.IndexOfAny (newlineChars);
				if (newlineCharIndex < 0) {
					break;
				}

				var segment = remaining[..newlineCharIndex];
				stringBuilder.Append (segment);

				int stride = segment.Length;
				// Replace newlines
				char newlineChar = remaining [newlineCharIndex];
				if (newlineChar == '\n') {
					stride++;
					stringBuilder.Append (' ');
				} else /* '\r' */ {
					int nextCharIndex = newlineCharIndex + 1;
					bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
					if (crlf) {
						stride += 2;
						stringBuilder.Append (' ');
					} else {
						stride++;
						stringBuilder.Append (' ');
					}
				}
				remaining = remaining [stride..];
			}
			stringBuilder.Append (remaining);
			return stringBuilder.ToString ();
		}

		internal static int ReplaceCRLFWithSpace (in ReadOnlySpan<char> str, in Span<char> buffer)
		{
			const string newlineChars = "\r\n";

			var remaining = str;
			int firstNewlineCharIndex = remaining.IndexOfAny (newlineChars);
			// Early exit if there are no newline characters.
			if (firstNewlineCharIndex < 0) {
				str.CopyTo (buffer);
				return str.Length;
			}

			var remainingBuffer = buffer;

			var firstSegment = remaining[..firstNewlineCharIndex];
			firstSegment.CopyTo (remainingBuffer);
			remainingBuffer = remainingBuffer [firstSegment.Length..];

			// The first newline is not skipped at this point because the condition has not been evaluated.
			// This means there will be 1 extra iteration because the same newline index is checked again in the loop.
			remaining = remaining [firstNewlineCharIndex..];

			while (remaining.Length > 0) {
				int newlineCharIndex = remaining.IndexOfAny (newlineChars);
				if (newlineCharIndex < 0) {
					break;
				}

				var segment = remaining[..newlineCharIndex];
				segment.CopyTo (remainingBuffer);
				remainingBuffer = remainingBuffer [segment.Length..];

				int stride = segment.Length;
				// Replace newlines
				char newlineChar = remaining [newlineCharIndex];
				if (newlineChar == '\n') {
					stride++;
					remainingBuffer [0] = ' ';
					remainingBuffer = remainingBuffer [1..];
				} else /* '\r' */ {
					int nextCharIndex = newlineCharIndex + 1;
					bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
					if (crlf) {
						stride += 2;
						remainingBuffer [0] = ' ';
						remainingBuffer = remainingBuffer [1..];
					} else {
						stride++;
						remainingBuffer [0] = ' ';
						remainingBuffer = remainingBuffer [1..];
					}
				}
				remaining = remaining [stride..];
			}
			remaining.CopyTo (remainingBuffer);
			remainingBuffer = remainingBuffer [remaining.Length..];
			return buffer.Length - remainingBuffer.Length;
		}

		/// <summary>
		/// Splits all newlines in the <paramref name="text"/> into a list
		/// and supports both CRLF and LF, preserving the ending newline.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>A list of text without the newline characters.</returns>
		public static List<string> SplitNewLine (string text)
		{
			if (string.IsNullOrEmpty (text)) {
				return new () { string.Empty };
			}

			var lines = new List<string>();

			const string newlineChars = "\r\n";
			var remaining = text.AsSpan();
			while (remaining.Length > 0) {
				int newlineCharIndex = remaining.IndexOfAny (newlineChars);
				if (newlineCharIndex == -1) {
					break;
				}

				var line = remaining[..newlineCharIndex].ToString();
				lines.Add (line);

				int stride = line.Length;
				char newlineChar = remaining [newlineCharIndex];
				if (newlineChar == '\n') {
					stride++;
				} else /* 'r' */ {
					int nextCharIndex = newlineCharIndex + 1;
					bool crlf = nextCharIndex < remaining.Length && remaining[nextCharIndex] == '\n';
					stride += crlf ? 2 : 1;
				}
				remaining = remaining [stride..];

				// Ended with line break so there should be an empty line.
				if (remaining.Length == 0) {
					lines.Add (string.Empty);
				}
			}

			if (remaining.Length > 0) {
				string remainingLine = remaining.ToString();
				lines.Add (remainingLine);
			}

			return lines;
		}

		/// <summary>
		/// Adds trailing whitespace or truncates <paramref name="text"/>
		/// so that it fits exactly <paramref name="width"/> console units.
		/// Note that some unicode characters take 2+ columns
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public static string ClipOrPad (string text, int width)
		{
			if (string.IsNullOrEmpty (text))
				return text;

			// TODO: Reuse StringBuilder
			// Preallocate capacity as the content either clips or gets padded to that length.
			var stringBuilder = new StringBuilder (width);

			Span<char> buffer = stackalloc char[2];
			int remainingSpace = width;
			foreach (var rune in text.EnumerateRunes ()) {
				int runeWidth = rune.GetColumns();
				if (remainingSpace < runeWidth) {
					break;
				}

				int charsWritten = rune.EncodeToUtf16 (buffer);
				stringBuilder.Append (buffer [..charsWritten]);
				remainingSpace -= runeWidth;
			}

			// Pad any remaining space.
			stringBuilder.Append (' ', remainingSpace);

			return stringBuilder.ToString ();
		}

		/// <summary>
		/// Formats the provided text to fit within the width provided using word wrapping.
		/// </summary>
		/// <param name="text">The text to word wrap</param>
		/// <param name="width">The number of columns to constrain the text to</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// This method does not do any justification.
		/// </para>
		/// <para>
		/// This method strips Newline ('\n' and '\r\n') sequences before processing.
		/// </para>
		/// <para>
		/// If <paramref name="preserveTrailingSpaces"/> is <see langword="false"/> at most one space will be preserved at the end of the last line.
		/// </para>
		/// </remarks>
		public static List<string> WordWrapText (
			string text, int width, bool preserveTrailingSpaces = false, int tabWidth = 0,
			TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}

			if (string.IsNullOrEmpty (text)) {
				return new List<string> ();
			}

			// This duplication avoids extra string allocation compared to early exit in the span variant.
			int maxTextWidth = IsHorizontalDirection(textDirection)
				? text.Length * 2
				: text.Length;
			if (maxTextWidth <= width) {
				// Early exit when the simplest worst case length fits the single line.
				if (preserveTrailingSpaces && !text.Contains ('\t')) {
					return new () { text };
				}
			}

			return WordWrapText (text.AsSpan (), width, preserveTrailingSpaces, tabWidth, textDirection);
		}

		/// <summary>
		/// Formats the provided text (read-only char span) to fit within the width provided using word wrapping.
		/// </summary>
		/// <inheritdoc cref="WordWrapText(string, int, bool, int, TextDirection)"/>
		public static List<string> WordWrapText (
			in ReadOnlySpan<char> text, int width, bool preserveTrailingSpaces = false, int tabWidth = 0,
			TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int MaxStackallocStripBufferSize = 512; // ~1 kiB
			const int MaxStackallocRuneBufferSize = 256; // ~1 kiB

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}

			if (text.IsEmpty) {
				return new List<string> ();
			}

			int maxTextWidth = IsHorizontalDirection(textDirection)
				? text.Length * 2
				: text.Length;
			if (maxTextWidth <= width) {
				// Early exit when the simplest worst case length fits the single line.
				if (preserveTrailingSpaces && !text.Contains ('\t')) {
					return new () { text.ToString () };
				}
			}

			int start = 0, end;
			var lines = new List<string> ();

			char[]? stripRentedArray = null;
			Rune[]? runeRentedArray = null;
			try {
				Span<char> stripBuffer = text.Length <= MaxStackallocStripBufferSize
					? stackalloc char[text.Length]
					: (stripRentedArray = ArrayPool<char>.Shared.Rent (text.Length));

				Span<Rune> runeBuffer = text.Length <= MaxStackallocRuneBufferSize
					? stackalloc Rune[text.Length]
					: (runeRentedArray = ArrayPool<Rune>.Shared.Rent(text.Length));

				int crlfStrippedCharsWritten = StripCRLF (text, stripBuffer);
				var crlfStrippedChars = stripBuffer[..crlfStrippedCharsWritten];

				int runeIdx = 0;
				foreach (var rune in crlfStrippedChars.EnumerateRunes ()) {
					runeBuffer [runeIdx] = rune;
					runeIdx++;
				}
				var runes = runeBuffer[..runeIdx];

				if (preserveTrailingSpaces) {
					while ((end = start) < runes.Length) {
						end = GetNextWhiteSpace (runes, start, width, out bool incomplete);
						if (end == 0 && incomplete) {
							start = runes.Length;
							break;
						}
						lines.Add (StringExtensions.ToString (runes [start..end]));
						start = end;
						if (incomplete) {
							start = runes.Length;
							break;
						}
					}
				} else {
					if (IsHorizontalDirection (textDirection)) {
						while ((end = start + Math.Max (GetLengthThatFits (runes [start..], width), 1)) < runes.Length) {
							while (runes [end].Value != ' ' && end > start)
								end--;
							if (end == start)
								end = start + GetLengthThatFits (runes [end..], width);
							var str = StringExtensions.ToString (runes[start..end]);
							if (end > start && str.GetColumns () <= width) {
								lines.Add (str);
								start = end;
								if (runes [end].Value == ' ') {
									start++;
								}
							} else {
								end++;
								start = end;
							}
						}
					} else {
						while ((end = start + width) < runes.Length) {
							while (runes [end].Value != ' ' && end > start) {
								end--;
							}
							if (end == start) {
								end = start + width;
							}
							lines.Add (StringExtensions.ToString (runes [start..end]));
							start = end;
							if (runes [end].Value == ' ') {
								start++;
							}
						}
					}
				}

				if (start < runes.Length) {
					var str = StringExtensions.ToString (runes[start..]);
					if (IsVerticalDirection (textDirection) || preserveTrailingSpaces || (!preserveTrailingSpaces && str.GetColumns () <= width)) {
						lines.Add (str);
					}
				}

				return lines;
			} finally {
				if (stripRentedArray != null) {
					ArrayPool<char>.Shared.Return (stripRentedArray);
				}
				if (runeRentedArray != null) {
					ArrayPool<Rune>.Shared.Return (runeRentedArray);
				}
			}

			int GetNextWhiteSpace (in ReadOnlySpan<Rune> runes, int from, int cWidth, out bool incomplete, int cLength = 0)
			{
				var lastFrom = from;
				var to = from;
				var length = cLength;
				incomplete = false;

				while (length < cWidth && to < runes.Length) {
					var rune = runes [to];
					if (IsHorizontalDirection (textDirection)) {
						length += rune.GetColumns ();
					} else {
						length++;
					}
					if (length > cWidth) {
						if (to >= runes.Length || (length > 1 && cWidth <= 1)) {
							incomplete = true;
						}
						return to;
					}
					if (rune.Value == ' ') {
						if (length == cWidth) {
							return to + 1;
						} else if (length > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (runes, to + 1, cWidth, out incomplete, length);
						}
					} else if (rune.Value == '\t') {
						length += tabWidth + 1;
						if (length == tabWidth && tabWidth > cWidth) {
							return to + 1;
						} else if (length > cWidth && tabWidth > cWidth
							// HACK: Prevent infinite loop when tabWidth > cWidth
							&& from != to) {
							return to;
						} else {
							return GetNextWhiteSpace (runes, to + 1, cWidth, out incomplete, length);
						}
					}
					to++;
				}
				if (cLength > 0 && to < runes.Length && runes [to].Value != ' ' && runes [to].Value != '\t') {
					return from;
				} else if (cLength > 0 && to < runes.Length && (runes [to].Value == ' ' || runes [to].Value == '\t')) {
					return lastFrom;
				} else {
					return to;
				}
			}
		}

		/// <summary>
		/// Justifies text within a specified width. 
		/// </summary>
		/// <param name="text">The text to justify.</param>
		/// <param name="width">The number of columns to clip the text to. Text longer than <paramref name="width"/> will be clipped.</param>
		/// <param name="talign">Alignment.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>Justified and clipped text.</returns>
		public static string ClipAndJustify (string text, int width, TextAlignment talign, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			return ClipAndJustify (text, width, talign == TextAlignment.Justified, textDirection);
		}

		/// <summary>
		/// Justifies text within a specified width. 
		/// </summary>
		/// <param name="text">The text to justify.</param>
		/// <param name="width">The number of columns to clip the text to. Text longer than <paramref name="width"/> will be clipped.</param>
		/// <param name="justify">Justify.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>Justified and clipped text.</returns>
		public static string ClipAndJustify (string text, int width, bool justify, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int MaxStackallocRuneBufferSize = 512; // Size of Rune is ~4 bytes, so the stack allocated buffer size is ~2 kiB.

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			int maxTextWidth = IsHorizontalDirection(textDirection)
				? text.Length * 2
				: text.Length;
			if (maxTextWidth <= width) {
				// Early exit when the worst case fits the width.
				return justify
					? Justify (text, width, ' ', textDirection)
					: text;
			}

			int maxRuneCount = text.Length;
			Rune[]? rentedRuneArray = null;
			Span<Rune> runeBuffer = maxRuneCount <= MaxStackallocRuneBufferSize
				? stackalloc Rune[maxRuneCount]
				: (rentedRuneArray = ArrayPool<Rune>.Shared.Rent(maxRuneCount));
			try {
				int freeBufferIdx = 0;
				if (IsHorizontalDirection (textDirection)) {
					int maxColumns = width;
					int sumColumns = 0;
					foreach (var rune in text.EnumerateRunes ()) {
						int runeColumns = Math.Max(rune.GetColumns(), 1);
						if (sumColumns + runeColumns > maxColumns) {
							int finalLength = freeBufferIdx;
							return StringExtensions.ToString (runeBuffer [..finalLength]);
						}
						runeBuffer [freeBufferIdx] = rune;
						freeBufferIdx++;
						sumColumns += runeColumns;
					}

					if (sumColumns < maxColumns && justify) {
						return Justify (text, maxColumns, ' ', textDirection);
					}
				} else {
					int maxLength = width;
					int sumLength = 0;
					foreach (var rune in text.EnumerateRunes ()) {
						if (sumLength + 1 > maxLength) {
							int finalLength = freeBufferIdx;
							return StringExtensions.ToString (runeBuffer [..finalLength]);
						}
						runeBuffer [freeBufferIdx] = rune;
						freeBufferIdx++;
						sumLength++;
					}
				}
				return text;
			} finally {
				if (rentedRuneArray != null) {
					ArrayPool<Rune>.Shared.Return (rentedRuneArray);
				}
			}
		}

		/// <summary>
		/// Justifies the text to fill the width provided. Space will be added between words (demarked by spaces and tabs) to
		/// make the text just fit <c>width</c>. Spaces will not be added to the ends.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width"></param>
		/// <param name="spaceChar">Character to replace whitespace and pad with. For debugging purposes.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>The justified text.</returns>
		public static string Justify (string text, int width, char spaceChar = ' ', TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int WordSearchBufferStackallocLimit = 256; // Size of Range is ~8 bytes, so the stack allocated buffer size is ~2 kiB.

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			Range[]? rentedWordBuffer = null;
			try {
				int firstSpaceIdx = text.IndexOf (' ');
				if (firstSpaceIdx == -1) {
					// Text has no spaces so nothing to justify because spaces will not be added to the end.
					return text;
				}

				// Use 1/2 of text length as potential word count for deciding between stackalloc and rent.
				// Potentially the whole text could be spaces so we don't want to abuse the stack too much.
				Span<Range> wordSearchBuffer = (text.Length * 0.50) <= WordSearchBufferStackallocLimit
					? stackalloc Range [WordSearchBufferStackallocLimit]
					: (rentedWordBuffer = ArrayPool<Range>.Shared.Rent(text.Length));

				int searchIdx = firstSpaceIdx + 1;

				int freeBufferIdx = 0;
				wordSearchBuffer [freeBufferIdx] = (0..firstSpaceIdx);
				freeBufferIdx++;

				while (searchIdx < text.Length) {
					int spaceIdx = text.IndexOf (' ', searchIdx);
					if (spaceIdx == -1) {
						break;
					}

					int startIdx = searchIdx;
					int wordLength = (spaceIdx - searchIdx);
					int endIdx = searchIdx + wordLength;
					wordSearchBuffer [freeBufferIdx] = (startIdx..endIdx);
					freeBufferIdx++;

					searchIdx = spaceIdx + 1;
				}

				if (searchIdx < text.Length) {
					int lastWordLength = text.Length - searchIdx;
					wordSearchBuffer [freeBufferIdx] = (searchIdx..(searchIdx + lastWordLength));
					freeBufferIdx++;
				}

				int wordCount = freeBufferIdx;
				var words = wordSearchBuffer[..wordCount];

				// Calculate text count based on found words.
				int textCount = 0;
				var textChars = text.AsSpan();
				if (IsHorizontalDirection (textDirection)) {
					for (int i = 0; i < words.Length; i++) {
						var word = textChars[words [i]];
						textCount += word.GetColumns ();
					}
				} else {
					for (int i = 0; i < words.Length; i++) {
						var word = textChars[words [i]];
						textCount += word.GetRuneCount ();
					}
				}

				// TODO: Could optimize by returning original text if spaces + extras is <= 0 but that would break original behavior.
				int spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
				int extras = words.Length > 1 ? (width - textCount) % (words.Length - 1) : 0;
				// Clamp minimums to 0
				spaces = Math.Max (spaces, 0);
				extras = Math.Max (extras, 0);

				// TODO: Precalculate StringBuilder capacity
				var s = new StringBuilder();
				for (int w = 0; w < words.Length; w++) {
					var word = textChars[words [w]];
					s.Append (word);

					int nextWordIdx = w + 1;
					if (nextWordIdx < words.Length) {
						s.Append (spaceChar, spaces);
					}
					if (extras > 0) {
						// Dump all remaining extras if this is the second to last word.
						if (nextWordIdx == words.Length - 1) {
							s.Append (spaceChar, extras);
							extras = 0;
						} else {
							s.Append (spaceChar);
							extras--;
						}
					}
				}
				return s.ToString ();
			} finally {
				if (rentedWordBuffer != null) {
					ArrayPool<Range>.Shared.Return (rentedWordBuffer, clearArray: false);
				}
			}
		}

		static char [] whitespace = new char [] { ' ', '\t' };

		/// <summary>
		/// Reformats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
		/// <param name="talign">Specifies how the text will be aligned horizontally.</param>
		/// <param name="wordWrap">If <see langword="true"/>, the text will be wrapped to new lines no longer than <paramref name="width"/>.	
		/// If <see langword="false"/>, forces text to fit a single line. Line breaks are converted to spaces. The text will be clipped to <paramref name="width"/>.</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// An empty <paramref name="text"/> string will result in one empty line.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is 0, a single, empty line will be returned.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible. 
		/// </para>
		/// </remarks>
		public static List<string> Format (string text, int width, TextAlignment talign, bool wordWrap, bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			return Format (text, width, talign == TextAlignment.Justified, wordWrap, preserveTrailingSpaces, tabWidth, textDirection);
		}

		/// <summary>
		/// Reformats text into lines, applying text alignment and optionally wrapping text to new lines on word boundaries.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="width">The number of columns to constrain the text to for word wrapping and clipping.</param>
		/// <param name="justify">Specifies whether the text should be justified.</param>
		/// <param name="wordWrap">If <see langword="true"/>, the text will be wrapped to new lines no longer than <paramref name="width"/>.	
		/// If <see langword="false"/>, forces text to fit a single line. Line breaks are converted to spaces. The text will be clipped to <paramref name="width"/>.</param>
		/// <param name="preserveTrailingSpaces">If <see langword="true"/> trailing spaces at the end of wrapped lines will be preserved.
		///  If <see langword="false"/>, trailing spaces at the end of wrapped lines will be trimmed.</param>
		/// <param name="tabWidth">The number of columns used for a tab.</param>
		/// <param name="textDirection">The text direction.</param>
		/// <returns>A list of word wrapped lines.</returns>
		/// <remarks>
		/// <para>
		/// An empty <paramref name="text"/> string will result in one empty line.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is 0, a single, empty line will be returned.
		/// </para>
		/// <para>
		/// If <paramref name="width"/> is int.MaxValue, the text will be formatted to the maximum width possible. 
		/// </para>
		/// </remarks>
		public static List<string> Format (
			string text, int width, bool justify, bool wordWrap,
			bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int MaxStackallocCharBufferSize = 512; // ~1 kiB

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "width cannot be negative");
			}
			List<string> lineResult = new List<string> ();

			if (string.IsNullOrEmpty (text) || width == 0) {
				lineResult.Add (string.Empty);
				return lineResult;
			}

			char[]? charRentedArray = null;
			try {
				Span<char> charBuffer = text.Length <= MaxStackallocCharBufferSize
					? stackalloc char[text.Length]
					: (charRentedArray = ArrayPool<char>.Shared.Rent (text.Length));

				if (wordWrap == false) {
					int replaceCharsWritten = ReplaceCRLFWithSpace (text, charBuffer);
					lineResult.Add (ClipAndJustify (new string (charBuffer [..replaceCharsWritten]), width, justify, textDirection));
					return lineResult;
				}

				int stripCharsWritten = StripCRLF (text, charBuffer, keepNewLine: true);
				var strippedText = charBuffer[..stripCharsWritten];

				var remaining = strippedText;
				while (remaining.Length > 0) {
					int newlineIdx = remaining.IndexOf('\n');
					if (newlineIdx == -1) {
						break;
					}

					var lineSegment = remaining[..newlineIdx];
					var wrappedLines = WordWrapText (lineSegment, width, preserveTrailingSpaces, tabWidth, textDirection);
					foreach (var line in wrappedLines) {
						lineResult.Add (ClipAndJustify (line, width, justify, textDirection));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (string.Empty);
					}
					remaining = remaining [(lineSegment.Length + 1)..];
				}

				foreach (var line in WordWrapText (remaining, width, preserveTrailingSpaces, tabWidth, textDirection)) {
					lineResult.Add (ClipAndJustify (line, width, justify, textDirection));
				}

				return lineResult;
			} finally {
				if (charRentedArray != null) {
					ArrayPool<char>.Shared.Return (charRentedArray);
				}
			}
		}

		/// <summary>
		/// Computes the number of lines needed to render the specified text given the width.
		/// </summary>
		/// <returns>Number of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The minimum width for the text.</param>
		public static int MaxLines (string text, int width)
		{
			var result = TextFormatter.Format (text, width, false, true);
			return result.Count;
		}

		/// <summary>
		/// Computes the maximum width needed to render the text (single line or multiple lines, word wrapped) given 
		/// a number of columns to constrain the text to.
		/// </summary>
		/// <returns>Width of the longest line after formatting the text constrained by <paramref name="maxColumns"/>.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="maxColumns">The number of columns to constrain the text to for formatting.</param>
		public static int MaxWidth (string text, int maxColumns)
		{
			var lines = TextFormatter.Format (text: text, width: maxColumns, justify: false, wordWrap: true);
			int maxWidth = 0;
			foreach (string line in lines) {
				int lineWidth = 0;
				foreach (var rune in line.EnumerateRunes ()) {
					lineWidth += Math.Max (rune.GetColumns (), 1);
				}
				if (lineWidth > maxWidth) {
					maxWidth = lineWidth;
				}
			}
			return maxWidth;
		}

		/// <summary>
		/// Returns the width of the widest line in the text, accounting for wide-glyphs (uses <see cref="StringExtensions.GetColumns(string)"/>).
		/// <paramref name="text"/> if it contains newlines.
		/// </summary>
		/// <param name="text">Text, may contain newlines.</param>
		/// <returns>The length of the longest line.</returns>
		public static int MaxWidthLine (string text)
		{
			var result = TextFormatter.SplitNewLine (text);
			return result.Max (x => x.GetColumns ());
		}

		/// <summary>
		/// Gets the maximum characters width from the list based on the <paramref name="startIndex"/>
		/// and the <paramref name="length"/>.
		/// </summary>
		/// <param name="lines">The lines.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="length">The length.</param>
		/// <returns>The maximum characters width.</returns>
		public static int GetSumMaxCharWidth (List<string> lines, int startIndex = -1, int length = -1)
		{
			if (length == 0 || lines.Count == 0 || startIndex >= lines.Count) {
				return 0;
			}

			var max = 0;
			for (int i = (startIndex == -1 ? 0 : startIndex); i < (length == -1 ? lines.Count : startIndex + length); i++) {
				string line = lines [i];
				if (line.Length == 0) {
					continue;
				}

				int lineMax = 0;
				foreach (var rune in line.EnumerateRunes ()) {
					int runeWidth = Math.Max (rune.GetColumns (), 1);
					if (runeWidth > lineMax) {
						lineMax = runeWidth;
					}
				}
				max += lineMax;
			}
			return max;
		}

		/// <summary>
		/// Gets the maximum characters width from the text based on the <paramref name="startIndex"/>
		/// and the <paramref name="length"/>.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="length">The length.</param>
		/// <returns>The maximum characters width.</returns>
		public static int GetSumMaxCharWidth (string text, int startIndex = -1, int length = -1)
		{
			if (length == 0 || string.IsNullOrEmpty (text)) {
				return 0;
			}

			var enumerator = text.EnumerateRunes ();
			int index = 0;
			if (startIndex > -1) {
				// Fast forward to the start index.
				while (index < startIndex) {
					if (!enumerator.MoveNext ()) {
						return 0;
					}
					index++;
				}
			}

			int max = 0;
			if (length > -1) {
				int currentLength = 0;
				while (currentLength++ < length && enumerator.MoveNext ()) {
					Rune rune = enumerator.Current;
					max += Math.Max (rune.GetColumns (), 1);
					index++;
				}
			} else {
				while (enumerator.MoveNext ()) {
					Rune rune = enumerator.Current;
					max += Math.Max (rune.GetColumns (), 1);
					index++;
				}
			}

			return max;
		}

		/// <summary>
		/// Gets the number of the Runes in a <see cref="string"/> that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="columns">The width.</param>
		/// <returns>The index of the text that fit the width.</returns>
		public static int GetLengthThatFits (string text, int columns)
		{
			if (string.IsNullOrEmpty (text)) {
				return 0;
			}
			return GetLengthThatFits (text.EnumerateRunes (), columns);
		}

		/// <summary>
		/// Gets the number of the Runes in an enumerator of Runes that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="runes">The enumerator of runes.</param>
		/// <param name="columns">The width.</param>
		/// <returns>The index of the last Rune in <paramref name="runes"/> that fit in <paramref name="columns"/>.</returns>
		private static int GetLengthThatFits (StringRuneEnumerator runes, int columns)
		{
			int runesLength = 0;
			int runeIdx = 0;
			foreach (var rune in runes) {
				int runeWidth = Math.Max (rune.GetColumns (), 1);
				if (runesLength + runeWidth > columns) {
					break;
				}
				runesLength += runeWidth;
				runeIdx++;
			}
			return runeIdx;
		}

		/// <summary>
		/// Gets the number of Runes in a span that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="runes">The enumerator of runes.</param>
		/// <param name="columns">The width.</param>
		/// <returns>The index of the last Rune in <paramref name="runes"/> that fit in <paramref name="columns"/>.</returns>
		public static int GetLengthThatFits (in ReadOnlySpan<Rune> runes, int columns)
		{
			int runesLength = 0;
			int runeIdx = 0;
			foreach (var rune in runes) {
				int runeWidth = Math.Max (rune.GetColumns (), 1);
				if (runesLength + runeWidth > columns) {
					break;
				}
				runesLength += runeWidth;
				runeIdx++;
			}
			return runeIdx;
		}

		/// <summary>
		/// Gets the number of the Runes in a list of Runes that will fit in <paramref name="columns"/>.
		/// </summary>
		/// <param name="runes">The list of runes.</param>
		/// <param name="columns">The width.</param>
		/// <returns>The index of the last Rune in <paramref name="runes"/> that fit in <paramref name="columns"/>.</returns>
		public static int GetLengthThatFits (List<Rune> runes, int columns)
		{
			if (runes == null || runes.Count == 0) {
				return 0;
			}

			var runesLength = 0;
			var runeIdx = 0;
			for (; runeIdx < runes.Count; runeIdx++) {
				var runeWidth = Math.Max (runes [runeIdx].GetColumns (), 1);
				if (runesLength + runeWidth > columns) {
					break;
				}
				runesLength += runeWidth;
			}
			return runeIdx;
		}

		/// <summary>
		/// Gets the index position from the list based on the <paramref name="width"/>.
		/// </summary>
		/// <param name="lines">The lines.</param>
		/// <param name="width">The width.</param>
		/// <returns>The index of the list that fit the width.</returns>
		public static int GetMaxColsForWidth (List<string> lines, int width)
		{
			int runesLength = 0;
			int lineIdx = 0;
			for (; lineIdx < lines.Count; lineIdx++) {
				string line = lines [lineIdx];
				int maxRuneWidth = 1;
				foreach (var rune in line.EnumerateRunes()) {
					int runeWidth = Math.Max (rune.GetColumns (), 1);
					if (runeWidth > maxRuneWidth) {
						maxRuneWidth = runeWidth;
					}
				}

				if (runesLength + maxRuneWidth > width) {
					break;
				}
				runesLength += maxRuneWidth;
			}
			return lineIdx;
		}

		/// <summary>
		///  Calculates the rectangle required to hold text, assuming no word wrapping or justification.
		/// </summary>
		/// <param name="x">The x location of the rectangle</param>
		/// <param name="y">The y location of the rectangle</param>
		/// <param name="text">The text to measure</param>
		/// <param name="direction">The text direction.</param>
		/// <returns></returns>
		public static Rect CalcRect (int x, int y, string text, TextDirection direction = TextDirection.LeftRight_TopBottom)
		{
			if (string.IsNullOrEmpty (text)) {
				return new Rect (new Point (x, y), Size.Empty);
			}

			int w, h;

			if (IsHorizontalDirection (direction)) {
				int mw = 0;
				int ml = 1;

				int cols = 0;
				foreach (var rune in text.EnumerateRunes ()) {
					if (rune.Value == '\n') {
						ml++;
						if (cols > mw) {
							mw = cols;
						}
						cols = 0;
					} else if (rune.Value != '\r') {
						cols++;
						var rw = ((Rune)rune).GetColumns ();
						if (rw > 0) {
							rw--;
						}
						cols += rw;
					}
				}
				if (cols > mw) {
					mw = cols;
				}
				w = mw;
				h = ml;
			} else {
				int vw = 1, cw = 1;
				int vh = 0;

				int rows = 0;
				foreach (var rune in text.EnumerateRunes ()) {
					if (rune.Value == '\n') {
						vw++;
						if (rows > vh) {
							vh = rows;
						}
						rows = 0;
						cw = 1;
					} else if (rune.Value != '\r') {
						rows++;
						var rw = ((Rune)rune).GetColumns ();
						if (cw < rw) {
							cw = rw;
							vw++;
						}
					}
				}
				if (rows > vh) {
					vh = rows;
				}
				w = vw;
				h = vh;
			}

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Finds the hotkey and its location in text. 
		/// </summary>
		/// <param name="text">The text to look in.</param>
		/// <param name="hotKeySpecifier">The hotkey specifier (e.g. '_') to look for.</param>
		/// <param name="firstUpperCase">If <c>true</c> the legacy behavior of identifying the first upper case character as the hotkey will be enabled.
		/// Regardless of the value of this parameter, <c>hotKeySpecifier</c> takes precedence.</param>
		/// <param name="hotPos">Outputs the Rune index into <c>text</c>.</param>
		/// <param name="hotKey">Outputs the hotKey.</param>
		/// <returns><c>true</c> if a hotkey was found; <c>false</c> otherwise.</returns>
		public static bool FindHotKey (string text, Rune hotKeySpecifier, bool firstUpperCase, out int hotPos, out Key hotKey)
		{
			if (string.IsNullOrEmpty (text) || hotKeySpecifier == (Rune)0xFFFF) {
				hotPos = -1;
				hotKey = Key.Unknown;
				return false;
			}

			Rune hot_key = (Rune)0;
			int hot_pos = -1;

			// Use first hot_key char passed into 'hotKey'.
			// TODO: Ignore hot_key of two are provided
			// TODO: Do not support non-alphanumeric chars that can't be typed
			int i = 0;
			foreach (Rune c in text.EnumerateRunes ()) {
				if ((char)c.Value != 0xFFFD) {
					if (c == hotKeySpecifier) {
						hot_pos = i;
					} else if (hot_pos > -1) {
						hot_key = c;
						break;
					}
				}
				i++;
			}

			// Legacy support - use first upper case char if the specifier was not found
			if (hot_pos == -1 && firstUpperCase) {
				i = 0;
				foreach (Rune c in text.EnumerateRunes ()) {
					if ((char)c.Value != 0xFFFD) {
						if (Rune.IsUpper (c)) {
							hot_key = c;
							hot_pos = i;
							break;
						}
					}
					i++;
				}
			}

			if (hot_key != (Rune)0 && hot_pos != -1) {
				hotPos = hot_pos;

				if (Rune.IsValid (hot_key.Value) && char.IsLetterOrDigit ((char)hot_key.Value)) {
					hotKey = (Key)char.ToUpperInvariant ((char)hot_key.Value);
					return true;
				}
			}

			hotPos = -1;
			hotKey = Key.Unknown;
			return false;
		}

		/// <summary>
		/// Replaces the Rune at the index specified by the <c>hotPos</c> parameter with a tag identifying 
		/// it as the hotkey.
		/// </summary>
		/// <param name="text">The text to tag the hotkey in.</param>
		/// <param name="hotPos">The Rune index of the hotkey in <c>text</c>.</param>
		/// <returns>The text with the hotkey tagged.</returns>
		/// <remarks>
		/// The returned string will not render correctly without first un-doing the tag. To undo the tag, search for 
		/// </remarks>
		public string ReplaceHotKeyWithTag (string text, int hotPos)
		{
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			const int MaxStackallocCharBufferSize = 512; // ~1 kiB
			char[]? rentedBufferArray = null;
			try {
				Span<char> buffer = text.Length <= MaxStackallocCharBufferSize
					? stackalloc char[text.Length]
					: (rentedBufferArray = ArrayPool<char>.Shared.Rent(text.Length));

				var remainingBuffer = buffer;
				bool modified = false;
				int index = 0;
				int totalCharsWritten = 0;
				foreach (var rune in text.EnumerateRunes ()) {
					var outputRune = rune;
					if (index == hotPos && Rune.IsLetterOrDigit (rune)) {
						outputRune = new Rune ((uint)rune.Value);
						modified = true;
					}

					int charsWritten = outputRune.EncodeToUtf16 (remainingBuffer);
					totalCharsWritten += charsWritten;
					remainingBuffer = remainingBuffer [charsWritten..];
					index++;
				}

				if (modified) {
					return new string (buffer [..totalCharsWritten]);
				}

				return text;
			} finally {
				if (rentedBufferArray != null) {
					ArrayPool<char>.Shared.Return (rentedBufferArray);
				}
			}
		}

		/// <summary>
		/// Removes the hotkey specifier from text.
		/// </summary>
		/// <param name="text">The text to manipulate.</param>
		/// <param name="hotKeySpecifier">The hot-key specifier (e.g. '_') to look for.</param>
		/// <param name="hotPos">Returns the position of the hot-key in the text. -1 if not found.</param>
		/// <returns>The input text with the hotkey specifier ('_') removed.</returns>
		public static string RemoveHotKeySpecifier (string text, int hotPos, Rune hotKeySpecifier)
		{
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			const int MaxStackallocCharBufferSize = 512; // ~1 kiB
			char[]? rentedBufferArray = null;
			try {
				Span<char> buffer = text.Length <= MaxStackallocCharBufferSize
					? stackalloc char[text.Length]
					: (rentedBufferArray = ArrayPool<char>.Shared.Rent(text.Length));

				int i = 0;
				var remainingBuffer = buffer;
				int totalCharsWritten = 0;
				foreach (Rune c in text.EnumerateRunes ()) {
					if (c == hotKeySpecifier && i == hotPos) {
						i++;
						continue;
					}
					int charsWritten = c.EncodeToUtf16 (remainingBuffer);
					totalCharsWritten += charsWritten;
					remainingBuffer = remainingBuffer [charsWritten..];
					i++;
				}

				return new string (buffer [..totalCharsWritten]);
			} finally {
				if (rentedBufferArray != null) {
					ArrayPool<char>.Shared.Return (rentedBufferArray);
				}
			}
		}

		#endregion // Static Members

		List<string> _lines = new List<string> ();
		string _text;
		TextAlignment _textAlignment;
		VerticalTextAlignment _textVerticalAlignment;
		TextDirection _textDirection;
		Key _hotKey;
		int _hotKeyPos = -1;
		Size _size;

		/// <summary>
		/// Event invoked when the <see cref="HotKey"/> is changed.
		/// </summary>
		public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

		/// <summary>
		///   The text to be displayed. This string is never modified.
		/// </summary>
		public virtual string Text {
			get => _text;
			set {
				_text = value;

				if (_text != null && _text.GetRuneCount () > 0 && (Size.Width == 0 || Size.Height == 0 || Size.Width != _text.GetColumns ())) {
					// Provide a default size (width = length of longest line, height = 1)
					// TODO: It might makes more sense for the default to be width = length of first line?
					Size = new Size (TextFormatter.MaxWidth (Text, int.MaxValue), 1);
				}

				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Used by <see cref="Text"/> to resize the view's <see cref="View.Bounds"/> with the <see cref="Size"/>.
		/// Setting <see cref="AutoSize"/> to true only work if the <see cref="View.Width"/> and <see cref="View.Height"/> are null or
		///   <see cref="LayoutStyle.Absolute"/> values and doesn't work with <see cref="LayoutStyle.Computed"/> layout,
		///   to avoid breaking the <see cref="Pos"/> and <see cref="Dim"/> settings.
		/// </summary>
		public bool AutoSize { get; set; }

		/// <summary>
		/// Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
		/// or not when <see cref="TextFormatter.WordWrap"/> is enabled. 
		/// If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when 
		/// <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
		/// </summary>
		public bool PreserveTrailingSpaces { get; set; }

		/// <summary>
		/// Controls the horizontal text-alignment property.
		/// </summary>
		/// <value>The text alignment.</value>
		public TextAlignment Alignment {
			get => _textAlignment;
			set {
				_textAlignment = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Controls the vertical text-alignment property. 
		/// </summary>
		/// <value>The text vertical alignment.</value>
		public VerticalTextAlignment VerticalAlignment {
			get => _textVerticalAlignment;
			set {
				_textVerticalAlignment = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Controls the text-direction property. 
		/// </summary>
		/// <value>The text vertical alignment.</value>
		public TextDirection Direction {
			get => _textDirection;
			set {
				_textDirection = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// Check if it is a horizontal direction
		/// </summary>
		public static bool IsHorizontalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_TopBottom:
			case TextDirection.RightLeft_BottomTop:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is a vertical direction
		/// </summary>
		public static bool IsVerticalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.BottomTop_LeftRight:
			case TextDirection.BottomTop_RightLeft:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is Left to Right direction
		/// </summary>
		public static bool IsLeftToRight (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
				return true;
			default:
				return false;
			}
		}

		/// <summary>
		/// Check if it is Top to Bottom direction
		/// </summary>
		public static bool IsTopToBottom (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
				return true;
			default:
				return false;
			}
		}

		// TODO: This is not implemented!
		/// <summary>
		/// 
		/// </summary>
		public bool WordWrap { get; set; } = false;

		/// <summary>
		/// Gets or sets the size of the area the text will be constrained to when formatted.
		/// </summary>
		/// <remarks>
		/// Does not return the size the formatted text; just the value that was set.
		/// </remarks>
		public Size Size {
			get {
				return _size;
			}
			set {
				_size = value;
				NeedsFormat = true;
			}
		}

		/// <summary>
		/// The specifier character for the hotkey (e.g. '_'). Set to '\xffff' to disable hotkey support for this View instance. The default is '\xffff'.
		/// </summary>
		public Rune HotKeySpecifier { get; set; } = (Rune)0xFFFF;

		/// <summary>
		/// The position in the text of the hotkey. The hotkey will be rendered using the hot color.
		/// </summary>
		public int HotKeyPos { get => _hotKeyPos; set => _hotKeyPos = value; }

		/// <summary>
		/// Gets the hotkey. Will be an upper case letter or digit.
		/// </summary>
		public Key HotKey {
			get => _hotKey;
			internal set {
				if (_hotKey != value) {
					var oldKey = _hotKey;
					_hotKey = value;
					HotKeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, value));
				}
			}
		}

		/// <summary>
		/// Gets the cursor position from <see cref="HotKey"/>. If the <see cref="HotKey"/> is defined, the cursor will be positioned over it.
		/// </summary>
		public int CursorPosition { get; set; }

		/// <summary>
		/// Gets the size required to hold the formatted text, given the constraints placed by <see cref="Size"/>.
		/// </summary>
		/// <remarks>
		/// Causes a format, resetting <see cref="NeedsFormat"/>.
		/// </remarks>
		/// <returns></returns>
		public Size GetFormattedSize ()
		{
			var lines = Lines;
			var width = Lines.Max (line => line.GetColumns ());
			var height = Lines.Count;
			return new Size (width, height);
		}

		/// <summary>
		/// Gets the formatted lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Upon a 'get' of this property, if the text needs to be formatted (if <see cref="NeedsFormat"/> is <c>true</c>)
		/// <see cref="Format(string, int, bool, bool, bool, int, TextDirection)"/> will be called internally. 
		/// </para>
		/// </remarks>
		public List<string> Lines {
			get {
				// With this check, we protect against subclasses with overrides of Text
				if (string.IsNullOrEmpty (Text) || Size.IsEmpty) {
					_lines = new List<string> {
						string.Empty
					};
					NeedsFormat = false;
					return _lines;
				}

				if (NeedsFormat) {
					var shown_text = _text;
					if (FindHotKey (_text, HotKeySpecifier, true, out _hotKeyPos, out Key newHotKey)) {
						HotKey = newHotKey;
						shown_text = RemoveHotKeySpecifier (Text, _hotKeyPos, HotKeySpecifier);
						shown_text = ReplaceHotKeyWithTag (shown_text, _hotKeyPos);
					}

					if (IsVerticalDirection (_textDirection)) {
						var colsWidth = GetSumMaxCharWidth (shown_text, 0, 1);
						_lines = Format (shown_text, Size.Height, _textVerticalAlignment == VerticalTextAlignment.Justified, Size.Width > colsWidth,
							PreserveTrailingSpaces, 0, _textDirection);
						if (!AutoSize) {
							colsWidth = GetMaxColsForWidth (_lines, Size.Width);
							if (_lines.Count > colsWidth) {
								_lines.RemoveRange (colsWidth, _lines.Count - colsWidth);
							}
						}
					} else {
						_lines = Format (shown_text, Size.Width, _textAlignment == TextAlignment.Justified, Size.Height > 1,
							PreserveTrailingSpaces, 0, _textDirection);
						if (!AutoSize && _lines.Count > Size.Height) {
							_lines.RemoveRange (Size.Height, _lines.Count - Size.Height);
						}
					}

					NeedsFormat = false;
				}
				return _lines;
			}
		}

		/// <summary>
		/// Gets or sets whether the <see cref="TextFormatter"/> needs to format the text when <see cref="Draw(Rect, Attribute, Attribute, Rect, bool)"/> is called.
		/// If it is <c>false</c> when Draw is called, the Draw call will be faster.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is set to true when the properties of <see cref="TextFormatter"/> are set.
		/// </para>
		/// </remarks>
		public bool NeedsFormat { get; set; }

		/// <summary>
		/// Causes the <see cref="TextFormatter"/> to reformat the text. 
		/// </summary>
		/// <returns>The formatted text.</returns>
		public string Format ()
		{
			var sb = new StringBuilder ();
			// Lines_get causes a Format
			foreach (var line in Lines) {
				sb.AppendLine (line.ToString ());
			}
			return sb.ToString ();
		}

		/// <summary>
		/// Draws the text held by <see cref="TextFormatter"/> to <see cref="Application.Driver"/> using the colors specified.
		/// </summary>
		/// <param name="bounds">Specifies the screen-relative location and maximum size for drawing the text.</param>
		/// <param name="normalColor">The color to use for all text except the hotkey</param>
		/// <param name="hotColor">The color to use to draw the hotkey</param>
		/// <param name="containerBounds">Specifies the screen-relative location and maximum container size.</param>
		/// <param name="fillRemaining">Determines if the bounds width will be used (default) or only the text width will be used.</param>
		public void Draw (Rect bounds, Attribute normalColor, Attribute hotColor, Rect containerBounds = default, bool fillRemaining = true)
		{
			// With this check, we protect against subclasses with overrides of Text (like Button)
			if (string.IsNullOrEmpty (_text)) {
				return;
			}

			Application.Driver?.SetAttribute (normalColor);

			// Use "Lines" to ensure a Format (don't use "lines"))

			var linesFormatted = Lines;
			switch (_textDirection) {
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_BottomTop:
			case TextDirection.BottomTop_RightLeft:
				linesFormatted.Reverse ();
				break;
			}

			bool isVertical = IsVerticalDirection (_textDirection);
			var maxBounds = bounds;
			if (Application.Driver != null) {
				maxBounds = containerBounds == default
					? bounds
					: new Rect (Math.Max (containerBounds.X, bounds.X),
					Math.Max (containerBounds.Y, bounds.Y),
					Math.Max (Math.Min (containerBounds.Width, containerBounds.Right - bounds.Left), 0),
					Math.Max (Math.Min (containerBounds.Height, containerBounds.Bottom - bounds.Top), 0));
			}
			if (maxBounds.Width == 0 || maxBounds.Height == 0) {
				return;
			}

			// BUGBUG: v2 - TextFormatter should not change the clip region. If a caller wants to break out of the clip region it should do
			// so explicitly.
			//var savedClip = Application.Driver?.Clip;
			//if (Application.Driver != null) {
			//	Application.Driver.Clip = maxBounds;
			//}
			int lineOffset = !isVertical && bounds.Y < 0 ? Math.Abs (bounds.Y) : 0;

			Rune[]? rentedRuneBufferArray = null;
			try {
				for (int lineIdx = lineOffset; lineIdx < linesFormatted.Count; lineIdx++) {
					if ((isVertical && lineIdx > bounds.Width) || (!isVertical && lineIdx > bounds.Height))
						continue;
					if ((isVertical && lineIdx >= maxBounds.Left + maxBounds.Width)
						|| (!isVertical && lineIdx >= maxBounds.Top + maxBounds.Height + lineOffset))

						break;

					string line = _lines [lineIdx];
					// Make sure the rented array fits the worst case, i.e. rune per char.
					int maxLineRuneCount = line.Length;
					if (rentedRuneBufferArray == null) {
						rentedRuneBufferArray = ArrayPool<Rune>.Shared.Rent (maxLineRuneCount);
					} else if (rentedRuneBufferArray.Length < maxLineRuneCount) {
						// Resize if previously rented array is potentially too small.
						ArrayPool<Rune>.Shared.Return (rentedRuneBufferArray);
						rentedRuneBufferArray = ArrayPool<Rune>.Shared.Rent (maxLineRuneCount);
					}

					int bufferIdx = 0;
					int runeCount = 0;
					foreach (var rune in line.EnumerateRunes ()) {
						rentedRuneBufferArray [bufferIdx] = rune;
						runeCount++;
						bufferIdx++;
					}

					var runes = rentedRuneBufferArray.AsSpan(0, runeCount);

					switch (_textDirection) {
					case TextDirection.RightLeft_BottomTop:
					case TextDirection.RightLeft_TopBottom:
					case TextDirection.BottomTop_LeftRight:
					case TextDirection.BottomTop_RightLeft:
						runes.Reverse ();
						break;
					}

					// When text is justified, we lost left or right, so we use the direction to align. 

					int x, y;
					// Horizontal Alignment
					if (_textAlignment == TextAlignment.Right || (_textAlignment == TextAlignment.Justified && !IsLeftToRight (_textDirection))) {
						if (isVertical) {
							int runesWidth = GetSumMaxCharWidth (Lines, lineIdx);
							x = bounds.Right - runesWidth;
							CursorPosition = bounds.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
						} else {
							int runesWidth = runes.GetColumns();
							x = bounds.Right - runesWidth;
							CursorPosition = bounds.Width - runesWidth + (_hotKeyPos > -1 ? _hotKeyPos : 0);
						}
					} else if (_textAlignment == TextAlignment.Left || _textAlignment == TextAlignment.Justified) {
						if (isVertical) {
							int runesWidth = lineIdx > 0 ? GetSumMaxCharWidth (Lines, 0, lineIdx) : 0;
							x = bounds.Left + runesWidth;
						} else {
							x = bounds.Left;
						}
						CursorPosition = _hotKeyPos > -1 ? _hotKeyPos : 0;
					} else if (_textAlignment == TextAlignment.Centered) {
						if (isVertical) {
							int runesWidth = GetSumMaxCharWidth (Lines, lineIdx);
							x = bounds.Left + lineIdx + ((bounds.Width - runesWidth) / 2);
							CursorPosition = (bounds.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
						} else {
							int runesWidth = runes.GetColumns ();
							x = bounds.Left + (bounds.Width - runesWidth) / 2;
							CursorPosition = (bounds.Width - runesWidth) / 2 + (_hotKeyPos > -1 ? _hotKeyPos : 0);
						}
					} else {
						throw new ArgumentOutOfRangeException ();
					}

					// Vertical Alignment
					if (_textVerticalAlignment == VerticalTextAlignment.Bottom || (_textVerticalAlignment == VerticalTextAlignment.Justified && !IsTopToBottom (_textDirection))) {
						if (isVertical) {
							y = bounds.Bottom - runes.Length;
						} else {
							y = bounds.Bottom - Lines.Count + lineIdx;
						}
					} else if (_textVerticalAlignment == VerticalTextAlignment.Top || _textVerticalAlignment == VerticalTextAlignment.Justified) {
						if (isVertical) {
							y = bounds.Top;
						} else {
							y = bounds.Top + lineIdx;
						}
					} else if (_textVerticalAlignment == VerticalTextAlignment.Middle) {
						if (isVertical) {
							int s = (bounds.Height - runes.Length) / 2;
							y = bounds.Top + s;
						} else {
							int s = (bounds.Height - Lines.Count) / 2;
							y = bounds.Top + lineIdx + s;
						}
					} else {
						throw new ArgumentOutOfRangeException ();
					}

					int colOffset = bounds.X < 0 ? Math.Abs (bounds.X) : 0;
					int start = isVertical ? bounds.Top : bounds.Left;
					int size = isVertical ? bounds.Height : bounds.Width;
					int current = start + colOffset;

					for (int idx = (isVertical ? start - y : start - x) + colOffset; current < start + size; idx++) {
						if (idx < 0 || x + current + colOffset < 0) {
							current++;
							continue;
						} else if (!fillRemaining && idx > runes.Length - 1) {
							break;
						}
						if ((!isVertical && idx > maxBounds.Left + maxBounds.Width - bounds.X + colOffset)
							|| (isVertical && idx > maxBounds.Top + maxBounds.Height - bounds.Y))

							break;

						var rune = (Rune)' ';
						if (isVertical) {
							Application.Driver?.Move (x, current);
							if (idx >= 0 && idx < runes.Length) {
								rune = runes [idx];
							}
						} else {
							Application.Driver?.Move (current, y);
							if (idx >= 0 && idx < runes.Length) {
								rune = runes [idx];
							}
						}
						if (HotKeyPos > -1 && idx == HotKeyPos) {
							if ((isVertical && _textVerticalAlignment == VerticalTextAlignment.Justified) ||
							(!isVertical && _textAlignment == TextAlignment.Justified)) {
								CursorPosition = idx - start;
							}
							Application.Driver?.SetAttribute (hotColor);
							Application.Driver?.AddRune (rune);
							Application.Driver?.SetAttribute (normalColor);
						} else {
							Application.Driver?.AddRune (rune);
						}
						int runeWidth = Math.Max (rune.GetColumns (), 1);
						if (isVertical) {
							current++;
						} else {
							current += runeWidth;
						}
						int nextRuneWidth = idx + 1 > -1 && idx + 1 < runes.Length ? runes [idx + 1].GetColumns () : 0;
						if (!isVertical && idx + 1 < runes.Length && current + nextRuneWidth > start + size) {
							break;
						}
					}
				}
			} finally {
				if (rentedRuneBufferArray != null) {
					ArrayPool<Rune>.Shared.Return (rentedRuneBufferArray);
				}
			}

			//if (Application.Driver != null) {
			//	Application.Driver.Clip = (Rect)savedClip;
			//}
		}
	}
}
