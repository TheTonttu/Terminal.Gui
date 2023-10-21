using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class Justify {

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public string StringSplit (string text, int width, char spaceChar = ' ', TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			var words = text.Split (' ');
			int textCount;
			if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
				textCount = words.Sum (arg => arg.GetColumns ());
			} else {
				textCount = words.Sum (arg => arg.GetRuneCount ());
			}
			var spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
			var extras = words.Length > 1 ? (width - textCount) % (words.Length - 1) : 0;

			var s = new System.Text.StringBuilder ();
			for (int w = 0; w < words.Length; w++) {
				var x = words [w];
				s.Append (x);
				if (w + 1 < words.Length)
					for (int i = 0; i < spaces; i++)
						s.Append (spaceChar);
				if (extras > 0) {
					for (int i = 0; i < 1; i++)
						s.Append (spaceChar);
					extras--;
				}
				if (w + 1 == words.Length - 1) {
					for (int i = 0; i < extras; i++)
						s.Append (spaceChar);
				}
			}
			return s.ToString ();
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string SpanRangeSplit (string text, int width, char spaceChar = ' ', TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int WordSearchBufferStackallocLimit = 256; // Size of Range is ~8 bytes, so the stack allocated buffer size is ~4 kiB.

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			Range[]? rentedWordBuffer = null;
			try {
				int firstSpaceIdx = text.IndexOf (' ');
				if (firstSpaceIdx == -1) {
					// Text has no spaces so nothing to justify because spaces will not be added to the end.
					return text;
				}

				// Use 1/2 of text length as potential word count for deciding between stackalloc and rent.
				// Potentially the whole text could be spaces so we don't want to abuse the stack too much.
				Span<Range> wordSearchBuffer = (text.Length * 0.50) <= WordSearchBufferStackallocLimit
					? stackalloc Range [WordSearchBufferStackallocLimit]
					: (rentedWordBuffer = ArrayPool<Range>.Shared.Rent(text.Length));

				int searchIdx = firstSpaceIdx + 1;

				int freeBufferIdx = 0;
				wordSearchBuffer [freeBufferIdx] = (0..firstSpaceIdx);
				freeBufferIdx++;

				while (searchIdx < text.Length) {
					int spaceIdx = text.IndexOf (' ', searchIdx);
					if (spaceIdx == -1) {
						break;
					}

					int startIdx = searchIdx;
					int wordLength = (spaceIdx - searchIdx);
					int endIdx = searchIdx + wordLength;
					wordSearchBuffer [freeBufferIdx] = (startIdx..endIdx);
					freeBufferIdx++;

					searchIdx = spaceIdx + 1;
				}

				if (searchIdx < text.Length) {
					int lastWordLength = text.Length - searchIdx;
					wordSearchBuffer [freeBufferIdx] = (searchIdx..(searchIdx + lastWordLength));
					freeBufferIdx++;
				}

				int wordCount = freeBufferIdx;
				var words = wordSearchBuffer[..wordCount];

				// Calculate text count based on found words.
				int textCount = 0;
				var textChars = text.AsSpan();
				if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
					for (int i = 0; i < words.Length; i++) {
						var word = textChars[words [i]];
						textCount += word.GetColumns ();
					}
				} else {
					for (int i = 0; i < words.Length; i++) {
						var word = textChars[words [i]];
						textCount += word.GetRuneCount ();
					}
				}

				int spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
				int extras = words.Length > 1 ? (width - textCount) % (words.Length - 1) : 0;
				// Clamp minimums to 0
				spaces = Math.Max (spaces, 0);
				extras = Math.Max (extras, 0);

				var s = new StringBuilder();
				for (int w = 0; w < words.Length; w++) {
					var word = textChars[words [w]];
					s.Append (word);

					int nextWordIdx = w + 1;
					if (nextWordIdx < words.Length) {
						s.Append (spaceChar, spaces);
					}
					if (extras > 0) {
						// Dump all remaining extras if this is the second to last word.
						if (nextWordIdx == words.Length - 1) {
							s.Append (spaceChar, extras);
							extras = 0;
						} else {
							s.Append (spaceChar);
							extras--;
						}
					}
				}
				return s.ToString ();
			} finally {
				if (rentedWordBuffer != null) {
					ArrayPool<Range>.Shared.Return (rentedWordBuffer, clearArray: false);
				}
			}
		}

		public IEnumerable<object []> DataSource ()
		{
			var directions = new [] {
				TextDirection.LeftRight_TopBottom,
				TextDirection.TopBottom_LeftRight,
			};

			foreach (var direction in directions) {
				// Empty = nothing to justify
				yield return new object [] { "", 8, ' ', direction };
				// No spaces = nothing to justify
				yield return new object [] { "HelloWorld", 16, ' ', direction };
				// Width shorter than text length
				yield return new object [] { "Hello World", 8, ' ', direction };
				//yield return new object [] { "Hello World Hello World", 32, ' ', direction };
				yield return new object [] { "Ð ÑÐ Ð²Ð Ñ Ð ÑÐ Ð²Ð Ñ", 64, ' ', direction };
				// Extreme scenario
				yield return new object [] {
					"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
					"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
					"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
					1000, ' ', direction
				};
			}
		}
	}
}
