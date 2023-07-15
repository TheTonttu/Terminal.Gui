using BenchmarkDotNet.Attributes;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {

	[MemoryDiagnoser]
	public class GetSumMaxCharWidth_Lines {

		[Params (1, 100, 10_000)]
		public int N { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public int EnumerateRunesLinq (List<string> lines, int startIndex, int length)
		{
			int result = default;
			for (int i = 0; i < N; i++) {
				result = EnumerateRunesLinqImplementation (lines, startIndex, length);
			}
			return result;
		}

		private static int EnumerateRunesLinqImplementation (List<string> lines, int startIndex = -1, int length = -1)
		{
			var max = 0;
			for (int i = (startIndex == -1 ? 0 : startIndex); i < (length == -1 ? lines.Count : startIndex + length); i++) {
				var runes = lines [i];
				if (runes.Length > 0)
					max += runes.EnumerateRunes ().Max (r => Math.Max (r.GetColumns (), 1));
			}
			return max;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public int NestedLoop (List<string> lines, int startIndex, int length)
		{
			int result = default;
			for (int i = 0; i < N; i++) {
				result = NestedLoopImplementation (lines, startIndex, length);
			}
			return result;
		}

		public static int NestedLoopImplementation (List<string> lines, int startIndex = -1, int length = -1)
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

		public IEnumerable<object []> DataSource ()
		{
			List<string> lines = new() {
				"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.",
				"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.",
				"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
			};

			yield return new object [] { lines, -1, -1 };
			yield return new object [] { lines, 0, 0 };
			yield return new object [] { lines, 1, 2 };
			yield return new object [] { lines, 3, 1 };
		}
	}
}
