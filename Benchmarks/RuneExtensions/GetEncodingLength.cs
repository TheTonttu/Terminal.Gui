using BenchmarkDotNet.Attributes;
using System.Text;

namespace Benchmarks.RuneExtensions {
	[MemoryDiagnoser]
	public class GetEncodingLength {
		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public int ToStringToCharArrayGetBytes (Rune rune, Encoding encoding)
		{
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = ToStringToCharArrayGetBytesImplementation (rune, encoding);
			}
			return result;
		}

		private static int ToStringToCharArrayGetBytesImplementation (Rune rune, Encoding? encoding)
		{
			encoding ??= Encoding.UTF8;
			var bytes = encoding.GetBytes (rune.ToString ().ToCharArray ());
			var offset = 0;
			if (bytes [^1] == 0) {
				offset++;
			}
			return bytes.Length - offset;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public int StackallocEncodeUtf16ToByteBuffer (Rune rune, Encoding encoding)
		{
			int result = default;
			for (int i = 0; i < Repetitions; i++) {
				result = SpanSliceEncodeUtf16ToByteBufferImplementation (rune, encoding);
			}
			return result;
		}

		private static int SpanSliceEncodeUtf16ToByteBufferImplementation (Rune rune, Encoding encoding = null)
		{
			encoding ??= Encoding.UTF8;

			// Get characters with UTF16 to keep that part independent of selected encoding.
			Span<char> charBuffer = stackalloc char[2];
			int charsWritten = rune.EncodeToUtf16(charBuffer);
			Span<char> chars = charBuffer[..charsWritten];

			int maxEncodedLength = encoding.GetMaxByteCount (charsWritten);
			Span<byte> byteBuffer = stackalloc byte[maxEncodedLength];
			int bytesEncoded = encoding.GetBytes (chars, byteBuffer);
			Span<byte> encodedBytes = byteBuffer[..bytesEncoded];

			int offset = 0;
			if (encodedBytes [^1] == 0) {
				offset++;
			}

			return encodedBytes.Length - offset;
		}

		public IEnumerable<object []> DataSource ()
		{
			var encodings = new[] { Encoding.UTF8, Encoding.UTF32 };
			foreach (var encoding in encodings) {
				yield return new object [] { new Rune ('a'), encoding };
				yield return new object [] { "𝔹".EnumerateRunes().Single(), encoding };
			}
		}
	}
}
