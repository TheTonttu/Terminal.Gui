using System;

namespace Terminal.Gui {
	/// <summary>
	/// Extensions to <see cref="ReadOnlySpan{Char}"/> to support TUI text manipulation.
	/// </summary>
	public static class ReadOnlySpanCharExtensions {
		/// <summary>
		/// Gets the number of columns the characters occupy in the terminal.
		/// </summary>
		/// <remarks>
		/// This is a Terminal.Gui extension method to <see cref="ReadOnlySpan{Char}"/> to support TUI text manipulation.
		/// </remarks>
		/// <param name="chars">The chars to measure.</param>
		/// <returns></returns>
		public static int GetColumns (this ReadOnlySpan<char> chars)
		{
			return chars.EnumerateRunes ().GetColumns ();
		}

		/// <summary>
		/// Gets the number of runes in the chars.
		/// </summary>
		/// <remarks>
		/// This is a Terminal.Gui extension method to <see cref="ReadOnlySpan{Char}"/> to support TUI text manipulation.
		/// </remarks>
		/// <param name="chars">The chars to count.</param>
		/// <returns></returns>
		public static int GetRuneCount (this ReadOnlySpan<char> chars)
		{
			return chars.EnumerateRunes ().GetRuneCount ();
		}
	}
}
