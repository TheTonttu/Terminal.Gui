using BenchmarkDotNet.Attributes;
using System.Text;

namespace Benchmarks.StringExtensions {
	[MemoryDiagnoser]
	public class DecodeLastRune {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public (Rune rune, int size) RunesToArray (string str, int end = -1)
		{
			(Rune rune, int size) result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = RunesToArrayInternal (str, end);
			}
			return result;
		}

		private static (Rune rune, int size) RunesToArrayInternal (string str, int end)
		{
			var rune = str.EnumerateRunes ().ToArray () [end == -1 ? ^1 : end];
			var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
			var operationStatus = Rune.DecodeFromUtf8 (bytes, out rune, out int bytesConsumed);
			if (operationStatus == System.Buffers.OperationStatus.Done) {
				return (rune, bytesConsumed);
			}
			return (Rune.ReplacementChar, 1);
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public (Rune rune, int size) EnumerateEachRune (string str, int end = -1)
		{
			(Rune rune, int size) result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = EnumerateEachRuneInternal (str, end);
			}
			return result;
		}

		private static (Rune rune, int size) EnumerateEachRuneInternal (string str, int end)
		{
			int index = 0;
			foreach (Rune rune in str.EnumerateRunes ()) {
				if (end >= 0 && index >= end) {
					return (rune, rune.Utf8SequenceLength);
				}
				index++;
			}
			var invalid = Rune.ReplacementChar;
			return (invalid, invalid.Utf8SequenceLength);
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public (Rune rune, int size) EnumerateEachRuneMoveEndCheckOutOfLoop (string str, int end = -1)
		{
			(Rune rune, int size) result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = EnumerateEachRuneMoveEndCheckOutOfLoopInternal (str, end);
			}
			return result;
		}

		private static (Rune rune, int size) EnumerateEachRuneMoveEndCheckOutOfLoopInternal (string str, int end)
		{
			int index = 0;
			if (end >= 0) {
				foreach (Rune rune in str.EnumerateRunes ()) {
					if (index >= end) {
						return (rune, rune.Utf8SequenceLength);
					}
					index++;
				}
			} else if (!string.IsNullOrEmpty (str)) {
				// Last() causes unnecessary IEnumerator allocation
				var lastRune = str.EnumerateRunes ().Last ();
				return (lastRune, lastRune.Utf8SequenceLength);
			}

			var invalid = Rune.ReplacementChar;
			return (invalid, invalid.Utf8SequenceLength);
		}

		public IEnumerable<object []> DataSource ()
		{
			string textSource = """
				Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent quis luctus elit. Integer ut arcu eget dolor scelerisque mattis ac et diam.
				Pellentesque sed dapibus massa, vel tristique dui. Sed vitae neque eu velit ornare aliquet. Ut quis orci tempor, tempor turpis id, tempus neque.
				Praesent sapien turpis, ornare vel mauris at, varius suscipit ante. Ut pulvinar turpis massa, quis cursus arcu faucibus in.
				Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Fusce at ex blandit, convallis quam et, vulputate lacus.
				Suspendisse sit amet arcu ut arcu faucibus varius. Vivamus sit amet maximus diam. Nam ex leo, pharetra eu lobortis at, tristique ut felis.
				""";


			string[] texts = {
				textSource.Substring(0, 10),
				textSource.Substring(0, 100),
				textSource
			};

			foreach (var text in texts) {
				yield return new object [] { text, 1 };
				yield return new object [] { text, text.EnumerateRunes ().Count () / 2 };
				yield return new object [] { text, -1 };
			}
		}
	}
}
