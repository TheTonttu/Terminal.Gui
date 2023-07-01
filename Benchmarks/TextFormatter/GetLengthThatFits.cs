using BenchmarkDotNet.Attributes;
using System.Text;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class GetLengthThatFits {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public int ToRuneList (string str, int columns)
		{
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = ToRuneListImplementation (str, columns);
			}
			return result;
		}

		public static int ToRuneListImplementation (string text, int columns) =>
			LengthFromRuneList (
				text == null
					? null
					: Tui.StringExtensions.ToRuneList (text)
				, columns);

		/// <summary>
		/// <see cref="Tui.TextFormatter.GetLengthThatFits(List{Rune}, int)"/>
		/// </summary>
		private static int LengthFromRuneList (List<Rune>? runes, int columns)
		{
			if (runes == null || runes.Count == 0) {
				return 0;
			}

			var runesLength = 0;
			var runeIdx = 0;
			for (; runeIdx < runes.Count; runeIdx++) {
				var runeWidth = Math.Max (Tui.RuneExtensions.GetColumns(runes [runeIdx]), 1);
				if (runesLength + runeWidth > columns) {
					break;
				}
				runesLength += runeWidth;
			}
			return runeIdx;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public int EnumerateStringRunes (string str, int columns)
		{
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = EnumerateStringRunesImplementation (str.EnumerateRunes(), columns);
			}
			return result;
		}

		public static int EnumerateStringRunesImplementation (StringRuneEnumerator runes, int columns)
		{
			int runesLength = 0;
			int runeIdx = 0;
			foreach (var rune in runes) {
				int runeWidth = Math.Max (Tui.RuneExtensions.GetColumns(rune), 1);
				if (runesLength + runeWidth > columns) {
					break;
				}
				runesLength += runeWidth;
				runeIdx++;
			}
			return runeIdx;
		}

		public IEnumerable<object []> DataSource ()
		{
			string[] texts = {
				"Hello World",
				"こんにちは 世界"
			};

			foreach (var text in texts) {
				int runeCount = text.EnumerateRunes ().Count ();
				int[] columns = {
					1, runeCount / 2, runeCount
				};
				foreach (var column in columns) {
					yield return new object [] { text, column };
				}

			}
		}
	}
}
