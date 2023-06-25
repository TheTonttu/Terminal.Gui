using BenchmarkDotNet.Attributes;
using System.Text;

namespace Benchmarks.RuneExtensions {
	[MemoryDiagnoser]
	public class Encode {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public int ToStringGetBytes (Rune rune, byte [] dest, int start = 0, int count = -1)
		{
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = ToStringGetBytesInternal (rune, dest, start, count);
			}
			return result;
		}

		private static int ToStringGetBytesInternal (Rune rune, byte [] dest, int start, int count)
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
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = StackallocUtf8EncodeInternal (rune, dest, start, count);
			}
			return result;
		}

		private static int StackallocUtf8EncodeInternal (Rune rune, byte [] dest, int start, int count)
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
				// Does not work in original (baseline) implementation
				//yield return new object [] { rune, new byte [16], 8, 8 };
			}
		}
	}
}
