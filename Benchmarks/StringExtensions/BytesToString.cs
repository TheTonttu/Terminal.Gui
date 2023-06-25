using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices;
using System.Text;

namespace Benchmarks.StringExtensions {
	[MemoryDiagnoser]
	public class BytesToString {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (ArrayDataSource))]
		public string IEnumerableToArray_Array (IEnumerable<byte> bytes, Encoding? encoding = null)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = IEnumerableToArrayInternal (bytes, ref encoding);
			}
			return str;
		}

		[Benchmark]
		[ArgumentsSource (nameof (ListDataSource))]
		public string IEnumerableToArray_List (IEnumerable<byte> bytes, Encoding? encoding = null)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = IEnumerableToArrayInternal (bytes, ref encoding);
			}
			return str;
		}

		private static string IEnumerableToArrayInternal (IEnumerable<byte> bytes, ref Encoding? encoding)
		{
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			return encoding.GetString (bytes.ToArray ());
		}

		[Benchmark]
		[ArgumentsSource (nameof (ArrayDataSource))]
		public string ReadOnlySpan_Array (byte [] bytes, Encoding? encoding = null)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = ReadOnlySpanInternal (bytes.AsSpan (), encoding);
			}
			return str;
		}

		[Benchmark]
		[ArgumentsSource (nameof (ListDataSource))]
		public string ReadOnlySpan_List (List<byte> bytes, Encoding? encoding = null)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = ReadOnlySpanInternal (CollectionsMarshal.AsSpan (bytes), encoding);
			}
			return str;
		}

		private static string ReadOnlySpanInternal (in ReadOnlySpan<byte> bytes, Encoding? encoding)
		{
			encoding ??= Encoding.UTF8;
			return encoding.GetString (bytes);
		}

		public IEnumerable<object? []> ArrayDataSource ()
		{
			yield return new object? [] { Array.Empty<byte> (), null };
			yield return new object? [] { Enumerable.Range (0, 10).Select (i => (byte)i).ToArray (), null };
			yield return new object? [] { Enumerable.Range (0, 100).Select (i => (byte)i).ToArray (), null };
			yield return new object? [] { Enumerable.Range (0, 1000).Select (i => (byte)i).ToArray (), null };
		}

		// TODO: Custom IParam for List<T> to display it properly in the benchmark summary.
		public IEnumerable<object? []> ListDataSource ()
		{
			yield return new object? [] { new List<byte> (), null };
			yield return new object? [] { Enumerable.Range (0, 10).Select (i => (byte)i).ToList (), null };
			yield return new object? [] { Enumerable.Range (0, 100).Select (i => (byte)i).ToList (), null };
			yield return new object? [] { Enumerable.Range (0, 1000).Select (i => (byte)i).ToList (), null };
		}
	}
}
