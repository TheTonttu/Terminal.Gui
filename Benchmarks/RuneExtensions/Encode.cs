using BenchmarkDotNet.Attributes;
using System.Text;

namespace Benchmarks.RuneExtensions {
	[MemoryDiagnoser]
	public class Encode {

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public int ToStringGetBytes (Rune rune, byte [] dest, int start = 0, int count = -1)
		{
			var bytes = Encoding.UTF8.GetBytes (rune.ToString ());
			var length = 0;
			for (var i = 0; i < (count == -1 ? bytes.Length : count); i++) {
				if (bytes [i] == 0) break;
				dest [start + i] = bytes [i];
				length++;
			}
			return length;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public int StackallocEncodeUtf8 (Rune rune, byte [] dest, int start = 0, int count = -1)
		{
			// Span length is 1-4
			Span<byte> bytes = stackalloc byte[rune.Utf8SequenceLength];
			int writtenBytes = rune.EncodeToUtf8 (bytes);

			int bytesToCopy = count == -1
				? writtenBytes
				: Math.Min (count, writtenBytes);
			int length = 0;
			for (int i = 0; i < bytesToCopy; i++) {
				if (bytes [i] == 0) break;
				dest [start + i] = bytes [i];
				length++;
			}
			return length;
		}

		public IEnumerable<object []> DataSource ()
		{
			var runes = new [] {
				new Rune ('a'),
				"𝔞".EnumerateRunes().Single()
			};

			foreach (var rune in runes) {
				yield return new object [] { rune, new byte [16], 0, -1 };
				yield return new object [] { rune, new byte [16], 8, -1 };
				// Do not work in original (baseline) implementation
				// yield return new object [] { rune, new byte [16], 8, 4 };
				// yield return new object [] { rune, new byte [16], 8, 8 };
			}
		}
	}
}
