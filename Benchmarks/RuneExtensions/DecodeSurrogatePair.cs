using BenchmarkDotNet.Attributes;
using System.Text;
using Tui = Terminal.Gui;

namespace Benchmarks.RuneExtensions {
	[MemoryDiagnoser]
	public class DecodeSurrogatePair {

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public (bool, char []?) ToStringToCharArray (Rune rune)
		{
			bool result = ToStringToCharArrayImplementation (rune, out char []? chars);
			return (result, chars);
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
		public (bool, char []?) EncodeToCharArray (Rune rune)
		{
			bool result = EncodeToCharArrayImplementation (rune, out char []? chars);
			return (result, chars);
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
		public (bool, char []?) InlineSurrogatePairCheck (Rune rune)
		{
			bool result = InlineSurrogatePairCheckImplementation (rune, out char []? chars);
			return (result, chars);
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
		public (bool, char[]?) EarlyExitBmp (Rune rune)
		{
			bool result = EarlyExitBmpImplementation (rune, out char []? chars);
			return (result, chars);
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
