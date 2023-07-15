using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;
using Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class RemoveHotkeySpecifier {

		[Params (1, 100, 10_000)]
		public int N { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public string StringConcat (string text, int hotPos, Rune hotKeySpecifier)
		{
			string result = string.Empty;
			for (int i = 0; i < N; i++) {
				result = StringConcatImplementation (text, hotPos, hotKeySpecifier);
			}
			return result;
		}


		private static string StringConcatImplementation (string text, int hotPos, Rune hotKeySpecifier)
		{
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			// Scan 
			string start = string.Empty;
			int i = 0;
			foreach (Rune c in text) {
				if (c == hotKeySpecifier && i == hotPos) {
					i++;
					continue;
				}
				start += c;
				i++;
			}
			return start;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string StringBuilderAppend (string text, int hotPos, Rune hotKeySpecifier)
		{
			string result = string.Empty;
			for (int i = 0; i < N; i++) {
				result = StringBuilderAppendImplementation (text, hotPos, hotKeySpecifier);
			}
			return result;
		}

		private static string StringBuilderAppendImplementation (string text, int hotPos, Rune hotKeySpecifier)
		{
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			var stringBuilder = new StringBuilder();
			int i = 0;
			foreach (Rune c in text.EnumerateRunes ()) {
				if (c == hotKeySpecifier && i == hotPos) {
					i++;
					continue;
				}
				stringBuilder.AppendRune (c);
				i++;
			}
			return stringBuilder.ToString ();
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string SpanBuffer (string text, int hotPos, Rune hotKeySpecifier)
		{
			string result = string.Empty;
			for (int i = 0; i < N; i++) {
				result = SpanBufferImplementation (text, hotPos, hotKeySpecifier);
			}
			return result;
		}

		private static string SpanBufferImplementation (string text, int hotPos, Rune hotKeySpecifier)
		{
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			const int MaxStackallocCharBufferSize = 512; // ~1 kiB
			char[]? rentedBufferArray = null;
			try {
				Span<char> buffer = text.Length <= MaxStackallocCharBufferSize
					? stackalloc char[text.Length]
					: (rentedBufferArray = ArrayPool<char>.Shared.Rent(text.Length));

				int i = 0;
				var remainingBuffer = buffer;
				int totalCharsWritten = 0;
				foreach (Rune c in text.EnumerateRunes ()) {
					if (c == hotKeySpecifier && i == hotPos) {
						i++;
						continue;
					}
					int charsWritten = c.EncodeToUtf16 (remainingBuffer);
					totalCharsWritten += charsWritten;
					remainingBuffer = remainingBuffer [charsWritten..];
					i++;
				}

				return new string (buffer [..totalCharsWritten]);
			} finally {
				if (rentedBufferArray != null) {
					ArrayPool<char>.Shared.Return (rentedBufferArray);
				}
			}
		}

		public IEnumerable<object []> DataSource ()
		{
			string[] texts = {
				"",
				// Typical scenario.
				"_Save file (Ctrl+S)",
				// Medium text, hotkey specifier somewhere in the middle.
				"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sed euismod metus. _Phasellus lectus metus, ultricies a commodo quis, facilisis vitae nulla.",
				// Long text, hotkey specifier almost at the beginning.
				"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. _Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
				"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
				"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
				// Long text, hotkey specifier almost at the end.
				"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
				"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
				"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. _Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
			};

			var hotKeySpecifier = (Rune)'_';

			foreach (string text in texts) {
				int hotPos = text.EnumerateRunes()
					.Select((r, i) => r == hotKeySpecifier ? i : -1)
					.FirstOrDefault(i => i > -1, -1);

				yield return new object [] { text, hotPos, hotKeySpecifier };
			}
		}
	}
}
