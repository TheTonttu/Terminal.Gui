using BenchmarkDotNet.Attributes;
using System.Text;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class MaxWidth {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public int NestedLinq (string text, int maxColumns)
		{
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = NestedLinqImplementation (text, maxColumns);
			}
			return result;
		}

		private static int NestedLinqImplementation (string text, int maxColumns)
		{
			var result = Tui.TextFormatter.Format (text: text, width: maxColumns, justify: false, wordWrap: true);
			var max = 0;
			result.ForEach (s => {
				var m = 0;
				s.ToRuneList ().ForEach (r => m += Math.Max (r.GetColumns (), 1));
				if (m > max) {
					max = m;
				}
			});
			return max;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public int NestedForeach (string text, int maxColumns)
		{
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = NestedForeachImplementation (text, maxColumns);
			}
			return result;
		}

		public static int NestedForeachImplementation (string text, int maxColumns)
		{
			var lines = Tui.TextFormatter.Format (text: text, width: maxColumns, justify: false, wordWrap: true);
			int maxWidth = 0;
			foreach (string line in lines) {
				int lineWidth = 0;
				foreach (var rune in line.EnumerateRunes ()) {
					lineWidth += Math.Max (rune.GetColumns (), 1);
				}
				if (lineWidth > maxWidth) {
					maxWidth = lineWidth;
				}
			}
			return maxWidth;
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
				int[] maxColumns = {
					1,
					(int)(text.EnumerateRunes().Count() * 0.25),
					(int)(text.EnumerateRunes().Count() * 0.75),
					int.MaxValue,
				};

				foreach (int width in maxColumns) {
					yield return new object [] { text, width };
				}
			}
		}
	}
}
