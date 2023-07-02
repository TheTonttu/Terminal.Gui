using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Various StringBuilder extension methods.
	/// </summary>
	public static class StringBuilderExtensions {

		const int MaxRuneChars = 2;

		/// <summary>
		/// Appends rune to the StringBuilder via stack allocated char array buffer.
		/// </summary>
		/// <param name="stringBuilder"></param>
		/// <param name="rune"></param>
		/// <returns>The string builder to allow additional call chaining.</returns>
		public static StringBuilder AppendRune (this StringBuilder stringBuilder, Rune rune)
		{
			Span<char> buffer = stackalloc char[MaxRuneChars];
			int charsWritten = rune.EncodeToUtf16 (buffer);
			stringBuilder.Append (buffer [..charsWritten]);

			return stringBuilder;
		}
	}
}
