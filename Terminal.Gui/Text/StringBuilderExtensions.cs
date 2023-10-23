using System;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Extensions to <see cref="StringBuilder"/> to support TUI text manipulation.
	/// </summary>>
	public static class StringBuilderExtensions {

		/// <summary>
		/// Appends rune to the StringBuilder via stack allocated char array buffer.
		/// </summary>
		/// <param name="stringBuilder"></param>
		/// <param name="rune"></param>
		/// <returns>The string builder to allow additional call chaining.</returns>
		public static StringBuilder AppendRune (this StringBuilder stringBuilder, Rune rune)
		{
			const int maxUtf16CharsPerRune = 2;
			Span<char> buffer = stackalloc char[maxUtf16CharsPerRune];
			int charsWritten = rune.EncodeToUtf16 (buffer);
			stringBuilder.Append (buffer [..charsWritten]);

			return stringBuilder;
		}
	}
}
