using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter;

[MemoryDiagnoser]
public class ReplaceHotKeyWithTag {

	[Benchmark (Baseline = true)]
	[ArgumentsSource (nameof (DataSource))]
	public string RuneListToString (string text, int hotPos)
	{
		// Set the high bit
		var runes = Tui.StringExtensions.ToRuneList(text);
		if (Rune.IsLetterOrDigit (runes [hotPos])) {
			runes [hotPos] = new Rune ((uint)runes [hotPos].Value);
		}
		return Tui.StringExtensions.ToString (runes);
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public string SpanBuffer (string text, int hotPos)
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

	public IEnumerable<object []> DataSource ()
	{
		string[] texts = {
			"",
			"Save file (Ctrl+S)",
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sed euismod metus. Phasellus lectus metus, ultricies a commodo quis, facilisis vitae nulla.",
			"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
			"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
			"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
		};

		foreach (string text in texts) {
			int runeCount = text.EnumerateRunes ().Count();
			int[] positions = {
				0, runeCount / 2, runeCount - 1, runeCount
			};

			foreach (int hotPos in positions) {
				yield return new object [] { text, hotPos };
			}
		}
	}
}
