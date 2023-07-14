using BenchmarkDotNet.Attributes;
using Terminal.Gui;

namespace Benchmarks.ConsoleDriver {
	/// <summary>
	/// Compares <see cref="Terminal.Gui.ConsoleDriver.GetColors"/> implementations.
	/// </summary>
	/// <remarks>
	/// More specifically compares impact on <see cref="WindowsDriver.GetColors"/> but the enum value caching benefits all drivers.
	/// </remarks>
	[MemoryDiagnoser]
	public class GetColors {

		[Params (1, 100, 10_000)]
		public int N { get; set; }

		[Benchmark(Baseline = true)]
		[ArgumentsSource(nameof(DataSource))]
		public bool InlineEnumGetValues (int value)
		{
			bool result = default;
			Color foreground = default;
			Color background = default;
			for (int i = 0; i < N; i++) {
				result = InlineEnumGetValuesImplementation (value, out foreground, out background);
			}
			return result;
		}

		private bool InlineEnumGetValuesImplementation (int value, out Color foreground, out Color background)
		{
			bool hasColor = false;
			foreground = default;
			background = default;
			IEnumerable<int> values = Enum.GetValues (typeof (ConsoleColor))
				.OfType<ConsoleColor> ()
				.Select (s => (int)s);
			if (values.Contains ((value >> 4) & 0xffff)) {
				hasColor = true;
				background = (Color)(ConsoleColor)((value >> 4) & 0xffff);
			}
			if (values.Contains (value - ((int)background << 4))) {
				hasColor = true;
				foreground = (Color)(ConsoleColor)(value - ((int)background << 4));
			}
			return hasColor;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool CachedEnumValuesList (int value)
		{
			bool result = default;
			Color foreground = default;
			Color background = default;
			for (int i = 0; i < N; i++) {
				result = CachedEnumValuesListImplementation (value, out foreground, out background);
			}
			return result;
		}

		private static readonly IReadOnlyList<int> List_CachedConsoleColorValues =
			Enum.GetValues<ConsoleColor> ()
				.Select (s => (int)s)
				.ToList();

		private bool CachedEnumValuesListImplementation (int value, out Color foreground, out Color background)
		{
			bool hasColor = false;
			foreground = default;
			background = default;

			if (List_CachedConsoleColorValues.Contains ((value >> 4) & 0xffff)) {
				hasColor = true;
				background = (Color)(ConsoleColor)((value >> 4) & 0xffff);
			}
			if (List_CachedConsoleColorValues.Contains (value - ((int)background << 4))) {
				hasColor = true;
				foreground = (Color)(ConsoleColor)(value - ((int)background << 4));
			}
			return hasColor;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool DeduplicateCalc (int value)
		{
			bool result = default;
			Color foreground = default;
			Color background = default;
			for (int i = 0; i < N; i++) {
				result = DeduplicateCalcImplementation (value, out foreground, out background);
			}
			return result;
		}

		private bool DeduplicateCalcImplementation (int value, out Color foreground, out Color background)
		{
			bool hasColor = false;
			foreground = default;
			background = default;

			int backgroundValue = (value >> 4) & 0xffff;
			if (List_CachedConsoleColorValues.Contains (backgroundValue)) {
				hasColor = true;
				background = (Color)(ConsoleColor)backgroundValue;
			}

			int foregroundValue = value - ((int)background << 4);
			if (List_CachedConsoleColorValues.Contains (foregroundValue)) {
				hasColor = true;
				foreground = (Color)(ConsoleColor)foregroundValue;
			}

			return hasColor;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public bool CachedEnumValuesHashSet (int value)
		{
			bool result = default;
			Color foreground = default;
			Color background = default;
			for (int i = 0; i < N; i++) {
				result = CachedEnumValuesHashSetImplementation (value, out foreground, out background);
			}
			return result;
		}

		private static readonly IReadOnlySet<int> HashSet_CachedConsoleColorValues =
			Enum.GetValues<ConsoleColor> ()
				.Select (s => (int)s)
				.ToHashSet();

		private bool CachedEnumValuesHashSetImplementation (int value, out Color foreground, out Color background)
		{
			bool hasColor = false;
			foreground = default;
			background = default;

			int backgroundValue = (value >> 4) & 0xffff;
			if (HashSet_CachedConsoleColorValues.Contains (backgroundValue)) {
				hasColor = true;
				background = (Color)(ConsoleColor)backgroundValue;
			}

			int foregroundValue = value - ((int)background << 4);
			if (HashSet_CachedConsoleColorValues.Contains (foregroundValue)) {
				hasColor = true;
				foreground = (Color)(ConsoleColor)foregroundValue;
			}

			return hasColor;
		}

		public IEnumerable<object> DataSource ()
		{
			yield return (int)ConsoleColor.Red;
			yield return (int)ConsoleColor.Green;
			yield return (int)ConsoleColor.Blue;
			yield return (int)ConsoleColor.Black;
			yield return (int)ConsoleColor.White;
		}
	}
}
