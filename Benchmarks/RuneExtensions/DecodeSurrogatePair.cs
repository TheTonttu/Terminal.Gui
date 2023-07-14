using BenchmarkDotNet.Attributes;
using System.Text;
using Tui = Terminal.Gui;

namespace Benchmarks.RuneExtensions {
	[MemoryDiagnoser]
	public class DecodeSurrogatePair {

		[Params (1, 100, 10_000)]
		public int N { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public bool ToStringToCharArray (Rune rune)
		{
			bool result = default;
			char[]? chars;
			for (int i = 0; i < N; i++) {
				ToStringToCharArrayImplementation (rune, out chars);
			}
			return result;
		}

		public static bool ToStringToCharArrayImplementation (Rune rune, out char []? chars)
		{
			if (Tui.RuneExtensions.IsSurrogatePair (rune)) {
				chars = rune.ToString ().ToCharArray ();
				return true;
			}
			chars = null;
			return false;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool EncodeToCharArray (Rune rune)
		{
			bool result = default;
			char[]? chars;
			for (int i = 0; i < N; i++) {
				EncodeToCharArrayImplementation (rune, out chars);
			}
			return result;
		}

		private static bool EncodeToCharArrayImplementation (Rune rune, out char []? chars)
		{
			if (Tui.RuneExtensions.IsSurrogatePair (rune)) {
				chars = new char [rune.Utf16SequenceLength];
				rune.EncodeToUtf16 (chars);
				return true;
			}
			chars = null;
			return false;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool InlineSurrogatePairCheck (Rune rune)
		{
			bool result = default;
			char[]? chars;
			for (int i = 0; i < N; i++) {
				InlineSurrogatePairCheckImplementation (rune, out chars);
			}
			return result;
		}

		private static bool InlineSurrogatePairCheckImplementation (Rune rune, out char []? chars)
		{
			Span<char> charBuffer = stackalloc char[2];
			int charsWritten = rune.EncodeToUtf16 (charBuffer);
			if (charsWritten >= 2 && char.IsSurrogatePair (charBuffer [0], charBuffer [1])) {
				chars = charBuffer [..charsWritten].ToArray ();
				return true;
			}
			chars = null;
			return false;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool EarlyExitBmp (Rune rune)
		{
			bool result = default;
			char[]? chars;
			for (int i = 0; i < N; i++) {
				EarlyExitBmpImplementation (rune, out chars);
			}
			return result;
		}

		private static bool EarlyExitBmpImplementation (Rune rune, out char []? chars)
		{
			if (rune.IsBmp) {
				chars = null;
				return false;
			}

			Span<char> charBuffer = stackalloc char[2];
			int charsWritten = rune.EncodeToUtf16 (charBuffer);
			if (charsWritten >= 2 && char.IsSurrogatePair (charBuffer [0], charBuffer [1])) {
				chars = charBuffer [..charsWritten].ToArray ();
				return true;
			}

			chars = null;
			return false;
		}

		public IEnumerable<object> DataSource ()
		{
			yield return new Rune ('a');
			yield return "𝔹".EnumerateRunes ().Single ();
		}
	}
}
