using BenchmarkDotNet.Attributes;
using System.Text;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter;

[MemoryDiagnoser]
public class ClipOrPad {

	[Benchmark (Baseline = true)]
	[ArgumentsSource (nameof (DataSource))]
	public string LinqEnumerate (string text, int width)
	{
		if (string.IsNullOrEmpty (text))
			return text;

		// if value is not wide enough
		if (text.EnumerateRunes ().Sum (c => Tui.RuneExtensions.GetColumns (c)) < width) {

			// pad it out with spaces to the given alignment
			int toPad = width - (text.EnumerateRunes ().Sum (c => Tui.RuneExtensions.GetColumns(c)));

			return text + new string (' ', toPad);
		}

		// value is too wide
		return new string (text.TakeWhile (c => (width -= Tui.RuneExtensions.GetColumns ((Rune)c)) >= 0).ToArray ());
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public string StringBuilderAppendRunes (string text, int width)
	{
		if (string.IsNullOrEmpty (text))
			return text;

		// Preallocate capacity as the content either clips or gets padded to that length.
		var stringBuilder = new StringBuilder (width);

		int remainingSpace = width;
		foreach (var rune in text.EnumerateRunes ()) {
			int runeWidth = Tui.RuneExtensions.GetColumns(rune);
			if (remainingSpace < runeWidth) {
				break;
			}
			Tui.StringBuilderExtensions.AppendRune (stringBuilder, rune);
			remainingSpace -= runeWidth;
		}

		// Pad any remaining space.
		stringBuilder.Append (' ', remainingSpace);

		return stringBuilder.ToString ();
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public string ReuseStackallocCharBuffer (string text, int width)
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

	public IEnumerable<object []> DataSource ()
	{
		yield return new object [] { "", 6 };
		yield return new object [] { "Hello World", 16 };
		yield return new object [] { "Hello World Hello World", 32 };
	}
}
