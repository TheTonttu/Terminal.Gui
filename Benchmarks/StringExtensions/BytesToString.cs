using BenchmarkDotNet.Attributes;
using System.Runtime.InteropServices;
using System.Text;

namespace Benchmarks.StringExtensions;

[MemoryDiagnoser]
public class BytesToString {

	[Benchmark (Baseline = true)]
	[ArgumentsSource (nameof (ArrayDataSource))]
	public string IEnumerableToArray_Array (IEnumerable<byte> bytes, int size, BenchmarkFormattedEncoding encoding)
	{
		var actualEncoding = encoding?.Encoding;
		return IEnumerableToArrayImplementation (bytes, actualEncoding);
	}

	[Benchmark]
	[ArgumentsSource (nameof (ListDataSource))]
	public string IEnumerableToArray_List (IEnumerable<byte> bytes, int size, BenchmarkFormattedEncoding encoding)
	{
		var actualEncoding = encoding?.Encoding;
		return IEnumerableToArrayImplementation (bytes, actualEncoding);
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
	public string ReadOnlySpan_Array (byte [] bytes, int size, BenchmarkFormattedEncoding encoding)
	{
		var actualEncoding = encoding?.Encoding;
		return ReadOnlySpanImplementation (bytes.AsSpan (), actualEncoding);
	}

	[Benchmark]
	[ArgumentsSource (nameof (ListDataSource))]
	public string ReadOnlySpan_List (List<byte> bytes, int size, BenchmarkFormattedEncoding encoding)
	{
		var actualEncoding = encoding?.Encoding;
		return ReadOnlySpanImplementation (CollectionsMarshal.AsSpan (bytes), actualEncoding);
	}

	private static string ReadOnlySpanImplementation (in ReadOnlySpan<byte> bytes, Encoding? encoding)
	{
		encoding ??= Encoding.UTF8;
		return encoding.GetString (bytes);
	}

	public IEnumerable<object? []> ArrayDataSource ()
	{
		var encoding = new BenchmarkFormattedEncoding(Encoding.UTF8);

		// Extra argument as workaround for grouping different length collections to same baseline making comparison difficult.
		int[] sizes = {
			10, 100, 1000
		};

		yield return new object? [] { Array.Empty<byte> (), 0, null };
		foreach (int size in sizes) {
			yield return new object? [] { Enumerable.Range (0, size).Select (i => (byte)i).ToArray (), size, encoding };
		}
	}

	public IEnumerable<object? []> ListDataSource ()
	{
		var encoding = new BenchmarkFormattedEncoding(Encoding.UTF8);

		// Extra argument as workaround for grouping different length collections to same baseline making comparison difficult.
		int[] sizes = {
			10, 100, 1000
		};

		yield return new object? [] { new BenchmarkFormattedList<byte> (), 0, null };
		foreach (int size in sizes) {
			yield return new object? [] { Enumerable.Range (0, size).Select (i => (byte)i).ToBenchmarkList (), size, encoding };
		}
	}
}
