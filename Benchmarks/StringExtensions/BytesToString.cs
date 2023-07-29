using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices;
using System.Text;

namespace Benchmarks.StringExtensions {
	[MemoryDiagnoser]
	public class BytesToString {

		[Params (1, 100, 10_000)]
		public int N { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (ArrayDataSource))]
		public string IEnumerableToArray_Array (IEnumerable<byte> bytes, BenchmarkFormattedEncoding encoding)
		{
			var actualEncoding = encoding.Encoding;
			string str = string.Empty;
			for (int i = 0; i < N; i++) {
				str = IEnumerableToArrayImplementation (bytes, actualEncoding);
			}
			return str;
		}

		[Benchmark]
		[ArgumentsSource (nameof (ListDataSource))]
		public string IEnumerableToArray_List (IEnumerable<byte> bytes, BenchmarkFormattedEncoding encoding)
		{
			var actualEncoding = encoding.Encoding;
			string str = string.Empty;
			for (int i = 0; i < N; i++) {
				str = IEnumerableToArrayImplementation (bytes, actualEncoding);
			}
			return str;
		}

		private static string IEnumerableToArrayImplementation (IEnumerable<byte> bytes, Encoding? encoding)
		{
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			return encoding.GetString (bytes.ToArray ());
		}

		[Benchmark]
		[ArgumentsSource (nameof (ArrayDataSource))]
		public string ReadOnlySpan_Array (byte [] bytes, BenchmarkFormattedEncoding encoding)
		{
			var actualEncoding = encoding.Encoding;
			string str = string.Empty;
			for (int i = 0; i < N; i++) {
				str = ReadOnlySpanImplementation (bytes.AsSpan (), actualEncoding);
			}
			return str;
		}

		[Benchmark]
		[ArgumentsSource (nameof (ListDataSource))]
		public string ReadOnlySpan_List (List<byte> bytes, BenchmarkFormattedEncoding encoding)
		{
			var actualEncoding = encoding.Encoding;
			string str = string.Empty;
			for (int i = 0; i < N; i++) {
				str = ReadOnlySpanImplementation (CollectionsMarshal.AsSpan (bytes), actualEncoding);
			}
			return str;
		}

		private static string ReadOnlySpanImplementation (in ReadOnlySpan<byte> bytes, Encoding? encoding)
		{
			encoding ??= Encoding.UTF8;
			return encoding.GetString (bytes);
		}

		public IEnumerable<object? []> ArrayDataSource ()
		{
			var encoding = new BenchmarkFormattedEncoding(Encoding.UTF8);

			yield return new object? [] { Array.Empty<byte> (), null };
			yield return new object? [] { Enumerable.Range (0, 10).Select (i => (byte)i).ToArray (), encoding };
			yield return new object? [] { Enumerable.Range (0, 100).Select (i => (byte)i).ToArray (), encoding };
			yield return new object? [] { Enumerable.Range (0, 1000).Select (i => (byte)i).ToArray (), encoding };
		}

		// TODO: Custom IParam for List<T> to display it properly in the benchmark summary.
		public IEnumerable<object? []> ListDataSource ()
		{
			var encoding = new BenchmarkFormattedEncoding(Encoding.UTF8);

			yield return new object? [] { new List<byte> (), null };
			yield return new object? [] { Enumerable.Range (0, 10).Select (i => (byte)i).ToList (), encoding };
			yield return new object? [] { Enumerable.Range (0, 100).Select (i => (byte)i).ToList (), encoding };
			yield return new object? [] { Enumerable.Range (0, 1000).Select (i => (byte)i).ToList (), encoding };
		}
	}
}
