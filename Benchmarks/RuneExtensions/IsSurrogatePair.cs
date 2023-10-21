using BenchmarkDotNet.Attributes;
using System.Text;

namespace Benchmarks.RuneExtensions;

[MemoryDiagnoser]
public class IsSurrogatePair {

	[Benchmark (Baseline = true)]
	[ArgumentsSource (nameof (DataSource))]
	public bool ToString (Rune rune)
	{
		return char.IsSurrogatePair (rune.ToString (), 0);
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public bool StackallocChars (Rune rune)
	{
		Span<char> charBuffer = stackalloc char[2];
		int charsWritten = rune.EncodeToUtf16 (charBuffer);
		return charsWritten >= 2 && char.IsSurrogatePair (charBuffer [0], charBuffer [1]);
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public bool EarlyExitBmp (Rune rune)
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
