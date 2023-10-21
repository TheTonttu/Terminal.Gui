using BenchmarkDotNet.Attributes;
using Terminal.Gui;

namespace Benchmarks.TextFormatter;

[MemoryDiagnoser]
public class GetMaxColsForWidth {

	[Benchmark (Baseline = true)]
	[ArgumentsSource (nameof (DataSource))]
	public int RuneListPerLine (List<string> lines, int width)
	{
		var runesLength = 0;
		var lineIdx = 0;
		for (; lineIdx < lines.Count; lineIdx++) {
			var runes = lines [lineIdx].ToRuneList ();
			var maxRuneWidth = runes.Count > 0
				? runes.Max (r => Math.Max (r.GetColumns (), 1)) : 1;
			if (runesLength + maxRuneWidth > width) {
				break;
			}
			runesLength += maxRuneWidth;
		}
		return lineIdx;
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public int EnumerateRunesPerLine (List<string> lines, int width)
	{
		int runesLength = 0;
		int lineIdx = 0;
		for (; lineIdx < lines.Count; lineIdx++) {
			string line = lines [lineIdx];
			int maxRuneWidth = 1;
			foreach (var rune in line.EnumerateRunes ()) {
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

	public IEnumerable<object []> DataSource ()
	{
		BenchmarkFormattedList<string> lines = new() {
			"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.",
			"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.",
			"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
		};

		int[] widths = { 1, 2, 4 };

		foreach (int width in widths) {
			yield return new object [] { lines, width };
		}
	}
}
