using BenchmarkDotNet.Attributes;
using System.Text;

namespace Benchmarks.RuneExtensions {
	[MemoryDiagnoser]
	public class IsSurrogatePair {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public bool ToString (Rune rune)
		{
			bool result = default;
			for (int i = 0; i < Repetitions; i++) {
				ToStringImplementation (rune);
			}
			return result;
		}

		private static bool ToStringImplementation (Rune rune)
		{
			return char.IsSurrogatePair (rune.ToString (), 0);
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool StackallocChars (Rune rune)
		{
			bool result = default;
			for (int i = 0; i < Repetitions; i++) {
				StackallocCharsImplementation (rune);
			}
			return result;
		}

		public static bool StackallocCharsImplementation (Rune rune)
		{
			Span<char> charBuffer = stackalloc char[2];
			int charsWritten = rune.EncodeToUtf16 (charBuffer);
			return charsWritten >= 2 && char.IsSurrogatePair (charBuffer [0], charBuffer [1]);
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool EarlyExitBmp (Rune rune)
		{
			bool result = default;
			for (int i = 0; i < Repetitions; i++) {
				EarlyExitBmpImplementation (rune);
			}
			return result;
		}

		public static bool EarlyExitBmpImplementation (Rune rune)
		{
			if (rune.IsBmp) {
				return false;
			}

			Span<char> charBuffer = stackalloc char[2];
			int charsWritten = rune.EncodeToUtf16 (charBuffer);
			return charsWritten >= 2 && char.IsSurrogatePair (charBuffer [0], charBuffer [1]);
		}

		public IEnumerable<object> DataSource ()
		{
			yield return new Rune ('a');
			yield return "𝔹".EnumerateRunes ().Single ();
		}
	}
}
