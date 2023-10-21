using BenchmarkDotNet.Attributes;
using System.Text;
using Terminal.Gui;

namespace Benchmarks.TextFormatter;

[MemoryDiagnoser]
public class GetSumMaxCharWidth_String {

	[Benchmark (Baseline = true)]
	[ArgumentsSource (nameof (DataSource))]
	public int IterateRuneList (string text, int startIndex = -1, int length = -1)
	{
		var max = 0;
		var runes = text.ToRunes ();
		for (int i = (startIndex == -1 ? 0 : startIndex); i < (length == -1 ? runes.Length : startIndex + length); i++) {
			max += Math.Max (runes [i].GetColumns (), 1);
		}
		return max;
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public int EnumerateRunes (string text, int startIndex = -1, int length = -1)
	{
		if (length == 0 || string.IsNullOrEmpty (text)) {
			return 0;
		}

		var enumerator = text.EnumerateRunes ();
		if (startIndex > -1) {
			int index = 0;
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
			}
		} else {
			while (enumerator.MoveNext ()) {
				Rune rune = enumerator.Current;
				max += Math.Max (rune.GetColumns (), 1);
			}
		}

		return max;
	}

	public IEnumerable<object []> DataSource ()
	{
		string[] texts = {
			"Hello World",
			// Single line
			"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
			"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
			"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
			// Multiline
			"""
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń."					
				"""
			// Consistent line endings between systems for more consistent performance evaluation.
			.ReplaceLineEndings("\r\n"),
		};

		foreach (string text in texts) {
			int[] indexes = {
				0,
				text.Length / 2
				-1
			};

			int[] lengths = {
				0,
				text.Length / 2,
				-1,
			};

			foreach (int startIndex in indexes)
			foreach (int length in lengths) {
				yield return new object [] { text, startIndex, length };
			}
		}
	}
}
