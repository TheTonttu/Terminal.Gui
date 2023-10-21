namespace Benchmarks;

/// <summary>
/// <see cref="List{T}"/> with benchmark summary column friendly name similar to array.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class BenchmarkFormattedList<T> : List<T> {

	public BenchmarkFormattedList ()
		: base () { }

	public BenchmarkFormattedList (IEnumerable<T> collection)
		: base (collection) { }

	public override string ToString ()
	{
		return $"{typeof (T).Name}[{Count}]";
	}
}

/// <summary>
/// Extension methods related to <see cref="BenchmarkFormattedList{T}"/>.
/// </summary>
public static class BenchmarkFormattedListExtensions {
	public static BenchmarkFormattedList<TSource> ToBenchmarkList<TSource> (this IEnumerable<TSource> source)
	{
		ArgumentNullException.ThrowIfNull (source);

		return new BenchmarkFormattedList<TSource> (source);
	}
}
