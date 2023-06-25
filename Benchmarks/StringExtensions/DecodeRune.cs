using BenchmarkDotNet.Attributes;
using System.Text;
using Tui = Terminal.Gui;

namespace Benchmarks.StringExtensions {
	[MemoryDiagnoser]
	public class DecodeRune {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public (Rune rune, int size) RunesToArray (string str, int start = 0, int count = -1)
		{
			(Rune rune, int size) result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = RunesToArrayInternal (str, start, count);
			}
			return result;
		}

		private static (Rune Rune, int Size) RunesToArrayInternal (string str, int start = 0, int count = -1)
		{
			var rune = str.EnumerateRunes ().ToArray () [start];
			var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
			if (count == -1) {
				count = bytes.Length;
			}
			var operationStatus = Rune.DecodeFromUtf8 (bytes, out rune, out int bytesConsumed);
			if (operationStatus == System.Buffers.OperationStatus.Done && bytesConsumed >= count) {
				return (rune, bytesConsumed);
			}
			return (Rune.ReplacementChar, 1);
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public (Rune rune, int size) EnumerateEachRunes (string str, int start = 0, int count = -1)
		{
			(Rune rune, int size) result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = EnumerateEachRunesInternal (str, start, count);
			}
			return result;
		}

		private static (Rune Rune, int Size) EnumerateEachRunesInternal (string str, int start = 0, int count = -1)
		{
			int index = 0;
			foreach (Rune rune in str.EnumerateRunes ()) {
				if (index < start) {
					index++;
					continue;
				}

				if (count >= 0 && rune.Utf8SequenceLength >= count) {
					break;
				}

				return (rune, rune.Utf8SequenceLength);
			}
			var invalid = Rune.ReplacementChar;
			return (invalid, invalid.Utf8SequenceLength);
		}

		public IEnumerable<object []> DataSource ()
		{
			string textSource =
				"""
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
				Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
				Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
				""";


			string[] texts = {
				Tui.StringExtensions.ToString(textSource.EnumerateRunes().Take(1)),
				Tui.StringExtensions.ToString(textSource.EnumerateRunes().Take(10)),
				Tui.StringExtensions.ToString(textSource.EnumerateRunes().Take(100)),
				textSource
			};

			foreach (var text in texts) {
				int midPoint = text.EnumerateRunes ().Count () / 2;

				yield return new object [] { text, 0, midPoint };
				yield return new object [] { text, midPoint, -1 };
				yield return new object [] { text, 0, -1 };
			}
		}
	}
}
