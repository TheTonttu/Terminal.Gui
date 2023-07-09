using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;

namespace Benchmarks.StringExtensions {
	[MemoryDiagnoser]
	public class RunesToString {

		private static readonly StringBuilder CachedStringBuilder = new StringBuilder();

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public string StringConcat (IEnumerable<Rune> runes, int size)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = StringConcatImplementation (runes);
			}
			return str;
		}

		private static string StringConcatImplementation (IEnumerable<Rune> runes)
		{
			var str = string.Empty;

			foreach (var rune in runes) {
				str += rune.ToString ();
			}

			return str;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string EncodeCharsStringBuilder (IEnumerable<Rune> runes, int size)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = EncodeCharsStringBuilderImplementation (runes);
			}
			return str;
		}

		private static string EncodeCharsStringBuilderImplementation (IEnumerable<Rune> runes)
		{
			var stringBuilder = new StringBuilder ();
			const int maxUtf16CharsPerRune = 2;
			Span<char> chars = stackalloc char[maxUtf16CharsPerRune];
			foreach (var rune in runes) {
				int charsWritten = rune.EncodeToUtf16 (chars);
				stringBuilder.Append (chars [..charsWritten]);
			}
			return stringBuilder.ToString ();
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string EncodeCharsCachedStringBuilder (IEnumerable<Rune> runes, int size)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = EncodeCharsCachedStringBuilderImplementation (runes);
			}
			return str;
		}

		private static string EncodeCharsCachedStringBuilderImplementation (IEnumerable<Rune> runes)
		{
			lock (CachedStringBuilder) {
				const int maxUtf16CharsPerRune = 2;
				Span<char> chars = stackalloc char[maxUtf16CharsPerRune];
				foreach (var rune in runes) {
					int charsWritten = rune.EncodeToUtf16 (chars);
					CachedStringBuilder.Append (chars [..charsWritten]);
				}
				string str = CachedStringBuilder.ToString();
				CachedStringBuilder.Clear ();
				return str;
			}
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string RuneSpanStringBuilderAppend (Rune [] runes, int size)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = RuneSpanStringBuilderAppendImplementation (runes);
			}
			return str;
		}

		private static string RuneSpanStringBuilderAppendImplementation (in ReadOnlySpan<Rune> runes)
		{
			lock (CachedStringBuilder) {
				const int maxUtf16CharsPerRune = 2;
				Span<char> chars = stackalloc char[maxUtf16CharsPerRune];
				foreach (var rune in runes) {
					int charsWritten = rune.EncodeToUtf16 (chars);
					CachedStringBuilder.Append (chars [..charsWritten]);
				}
				string str = CachedStringBuilder.ToString();
				CachedStringBuilder.Clear ();
				return str;
			}
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string RuneSpanArrayBuffer (Rune [] runes, int size)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = RuneSpanArrayBufferImplementation (runes);
			}
			return str;
		}

		private static string RuneSpanArrayBufferImplementation (in ReadOnlySpan<Rune> runes)
		{
			const int MaxUtf16CharsPerRune = 2;
			const int MaxStackallocBufferSize = 512; // ~1 kiB

			char[]? rentedArray = null;
			try {
				int bufferSize = runes.Length * MaxUtf16CharsPerRune;
				Span<char> buffer = bufferSize <= MaxStackallocBufferSize
				? stackalloc char[MaxStackallocBufferSize]
				: (rentedArray = ArrayPool<char>.Shared.Rent(bufferSize));

				var remainingBuffer = buffer;
				foreach (var rune in runes) {
					int charsWritten = rune.EncodeToUtf16 (remainingBuffer);
					remainingBuffer = remainingBuffer [charsWritten..];
				}

				return new string (buffer [..^remainingBuffer.Length]);
			} finally {
				if (rentedArray != null) {
					ArrayPool<char>.Shared.Return (rentedArray);
				}
			}
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string RuneSpanArrayBufferExactStackallocSize (Rune [] runes, int size)
		{
			string str = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				str = RuneSpanArrayBufferExactStackallocSizeImplementation (runes);
			}
			return str;
		}

		private static string RuneSpanArrayBufferExactStackallocSizeImplementation (in ReadOnlySpan<Rune> runes)
		{
			const int MaxUtf16CharsPerRune = 2;
			const int MaxStackallocBufferSize = 512; // ~1 kiB

			char[]? rentedArray = null;
			try {
				int bufferSize = runes.Length * MaxUtf16CharsPerRune;
				Span<char> buffer = bufferSize <= MaxStackallocBufferSize
				? stackalloc char[bufferSize]
				: (rentedArray = ArrayPool<char>.Shared.Rent(bufferSize));

				var remainingBuffer = buffer;
				foreach (var rune in runes) {
					int charsWritten = rune.EncodeToUtf16 (remainingBuffer);
					remainingBuffer = remainingBuffer [charsWritten..];
				}

				return new string (buffer [..^remainingBuffer.Length]);
			} finally {
				if (rentedArray != null) {
					ArrayPool<char>.Shared.Return (rentedArray);
				}
			}
		}

		public IEnumerable<object []> DataSource ()
		{
			string textSource =
				"""
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
				Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
				Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
				""";

			// Extra parameter as workaround to single parameter grouping different length arrays to same baseline making comparison difficult.
			int[] sizes = {
				1, 10, 100, textSource.Length / 2, textSource.Length
			};

			foreach (int size in sizes) {
				yield return new object [] { textSource.EnumerateRunes ().Take (size).ToArray (), size };
			}
		}
	}
}
