using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices;
using System.Text;

namespace Benchmarks.StringExtensions {
	[MemoryDiagnoser]
	public class BytesToString {

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (ArrayDataSource))]
		public string IEnumerableToArray (IEnumerable<byte> bytes, Encoding? encoding = null)
		{
			if (encoding == null) {
				encoding = Encoding.UTF8;
			}
			return encoding.GetString (bytes.ToArray ());
		}

		[Benchmark]
		[ArgumentsSource (nameof (ArrayDataSource))]
		public string Array_ReadOnlySpan (byte [] bytes, Encoding? encoding = null)
		{
			return ReadOnlySpanInternal (bytes.AsSpan (), encoding);
		}

		[Benchmark]
		[ArgumentsSource (nameof (ListDataSource))]
		public string List_ReadOnlySpan (List<byte> bytes, Encoding? encoding = null)
		{
			return ReadOnlySpanInternal (CollectionsMarshal.AsSpan (bytes), encoding);
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

		public IEnumerable<object? []> ListDataSource ()
		{
			yield return new object? [] { new List<byte> (), null };
			yield return new object? [] { Enumerable.Range (0, 10).Select (i => (byte)i).ToList (), null };
			yield return new object? [] { Enumerable.Range (0, 100).Select (i => (byte)i).ToList (), null };
			yield return new object? [] { Enumerable.Range (0, 1000).Select (i => (byte)i).ToList (), null };
		}
	}
}
