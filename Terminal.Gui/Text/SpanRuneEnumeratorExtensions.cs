using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Extensions to <see cref="SpanRuneEnumeratorExtensions"/> to support TUI text manipulation.
	/// </summary>
	public static class SpanRuneEnumeratorExtensions {

		/// <summary>
		/// Gets the number of columns the enumerated runes occupy in the terminal.
		/// </summary>
		/// <remarks>
		/// This is a Terminal.Gui extension method to <see cref="SpanRuneEnumerator"/> to support TUI text manipulation.
		/// </remarks>
		/// <param name="runes">The runes to measure.</param>
		/// <returns></returns>
		public static int GetColumns (this SpanRuneEnumerator runes)
		{
			int sum = 0;
			foreach (var rune in runes) {
				sum += Math.Max (rune.GetColumns (), 0);
			}
			return sum;
		}

		/// <summary>
		/// Gets the number of runes in the enumerator.
		/// </summary>
		/// <remarks>
		/// This is a Terminal.Gui extension method to <see cref="SpanRuneEnumerator"/> to support TUI text manipulation.
		/// </remarks>
		/// <param name="runes">The string to count.</param>
		/// <returns></returns>
		public static int GetRuneCount (this SpanRuneEnumerator runes)
		{
			int count = 0;
			while (runes.MoveNext ()) {
				count++;
			}
			return count;
		}
	}
}
