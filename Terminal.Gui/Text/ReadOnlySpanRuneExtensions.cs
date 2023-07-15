using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Extensions to <see cref="ReadOnlySpan{Rune}"/> to support TUI text manipulation.
	/// </summary>
	public static class ReadOnlySpanRuneExtensions {
		/// <summary>
		/// Gets the number of columns the runes occupy in the terminal.
		/// </summary>
		/// <remarks>
		/// This is a Terminal.Gui extension method to <see cref="ReadOnlySpan{Rune}"/> to support TUI text manipulation.
		/// </remarks>
		/// <param name="runes">The runes to measure.</param>
		/// <returns></returns>
		public static int GetColumns (this ReadOnlySpan<Rune> runes)
		{
			int sum = 0;
			foreach (var rune in runes) {
				sum += Math.Max (rune.GetColumns (), 0);
			}
			return sum;
		}

		/// <summary>
		/// Gets the number of columns the runes occupy in the terminal.
		/// </summary>
		/// <remarks>
		/// This is a Terminal.Gui extension method to <see cref="Span{Rune}"/> to support TUI text manipulation.
		/// </remarks>
		/// <param name="runes">The runes to measure.</param>
		/// <returns></returns>
		public static int GetColumns (this Span<Rune> runes)
		{
			// This method exists just to avoid need to explicitly cast to ReadOnlySpan<Rune>.
			return GetColumns ((ReadOnlySpan<Rune>)runes);
		}
	}
}
