using BenchmarkDotNet.Attributes;
using System.Text;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class StripCRLF {

		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public string ToRuneListEdit (string str, bool keepNewLine = false)
		{
			string result = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				result = ToRuneListEditImplementation (str, keepNewLine);
			}
			return result;
		}

		private static string ToRuneListEditImplementation (string str, bool keepNewLine)
		{
			var runes = Tui.StringExtensions.ToRuneList(str);
			for (int i = 0; i < runes.Count; i++) {
				switch ((char)runes [i].Value) {
				case '\n':
					if (!keepNewLine) {
						runes.RemoveAt (i);
					}
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1].Value == '\n') {
						runes.RemoveAt (i);
						if (!keepNewLine) {
							runes.RemoveAt (i);
						}
						i++;
					} else {
						if (!keepNewLine) {
							runes.RemoveAt (i);
						}
					}
					break;
				}
			}
			return Tui.StringExtensions.ToString (runes);
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string EarlyExitStringBuilderCharSpanSlice (string str, bool keepNewLine = false)
		{
			string result = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				result = EarlyExitStringBuilderCharSpanSliceImplementation (str, keepNewLine);
			}
			return result;
		}

		internal static string EarlyExitStringBuilderCharSpanSliceImplementation (string str, bool keepNewLine = false)
		{
			const string newlineChars = "\r\n";

			var remaining = str.AsSpan ();
			int firstNewlineCharIndex = remaining.IndexOfAny (newlineChars);
			// Early exit to avoid StringBuilder allocation if there are no newline characters.
			if (firstNewlineCharIndex < 0) {
				return str;
			}

			var stringBuilder = new StringBuilder();
			var firstSegment = remaining[..firstNewlineCharIndex];
			stringBuilder.Append (firstSegment);

			// The first newline is not skipped at this point because the "keepNewLine" condition has not been evaluated.
			// This means there will be 1 extra iteration because the same newline index is checked again in the loop.
			remaining = remaining [firstNewlineCharIndex..];

			while (remaining.Length > 0) {
				int newlineCharIndex = remaining.IndexOfAny (newlineChars);
				if (newlineCharIndex < 0) {
					break;
				}

				var segment = remaining[..newlineCharIndex];
				stringBuilder.Append (segment);

				int stride = segment.Length;
				// Evaluate how many newline characters to preserve.
				char newlineChar = remaining [newlineCharIndex];
				if (newlineChar == '\n') {
					stride++;
					if (keepNewLine) {
						stringBuilder.Append ('\n');
					}
				} else /* '\r' */ {
					int nextCharIndex = newlineCharIndex + 1;
					bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
					if (crlf) {
						stride += 2;
						if (keepNewLine) {
							stringBuilder.Append ('\n');
						}
					} else {
						stride++;
						if (keepNewLine) {
							stringBuilder.Append ('\r');
						}
					}
				}
				remaining = remaining [stride..];
			}
			stringBuilder.Append (remaining);
			return stringBuilder.ToString ();
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string SpanBuffer (string str, bool keepNewLine = false)
		{
			char[] buffer = new char[str.Length];
			string result = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				int charsWritten = SpanBufferImplementation (str, buffer, keepNewLine);
				result = new string (buffer, 0, charsWritten);
			}
			return result;
		}

		internal static int SpanBufferImplementation (in ReadOnlySpan<char> str, in Span<char> buffer, bool keepNewLine = false)
		{
			const string newlineChars = "\r\n";

			var remaining = str;
			var remainingBuffer = buffer;
			int firstNewlineCharIndex = remaining.IndexOfAny (newlineChars);
			// Early exit if there are no newline characters.
			if (firstNewlineCharIndex < 0) {
				str.CopyTo (buffer);
				return str.Length;
			}

			var firstSegment = remaining[..firstNewlineCharIndex];
			firstSegment.CopyTo (remainingBuffer);
			remainingBuffer = remainingBuffer [firstSegment.Length..];

			// The first newline is not skipped at this point because the "keepNewLine" condition has not been evaluated.
			// This means there will be 1 extra iteration because the same newline index is checked again in the loop.
			remaining = remaining [firstNewlineCharIndex..];

			while (remaining.Length > 0) {
				int newlineCharIndex = remaining.IndexOfAny (newlineChars);
				if (newlineCharIndex < 0) {
					break;
				}

				var segment = remaining[..newlineCharIndex];
				segment.CopyTo (remainingBuffer);
				remainingBuffer = remainingBuffer [segment.Length..];

				int stride = segment.Length;
				// Evaluate how many newline characters to preserve.
				char newlineChar = remaining [newlineCharIndex];
				if (newlineChar == '\n') {
					stride++;
					if (keepNewLine) {
						remainingBuffer [0] = '\n';
						remainingBuffer = remainingBuffer [1..];
					}
				} else /* '\r' */ {
					int nextCharIndex = newlineCharIndex + 1;
					bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
					if (crlf) {
						stride += 2;
						if (keepNewLine) {
							remainingBuffer [0] = '\n';
							remainingBuffer = remainingBuffer [1..];
						}
					} else {
						stride++;
						if (keepNewLine) {
							remainingBuffer [0] = '\r';
							remainingBuffer = remainingBuffer [1..];
						}
					}
				}
				remaining = remaining [stride..];
			}
			remaining.CopyTo (remainingBuffer);
			remainingBuffer = remainingBuffer [remaining.Length..];

			return buffer.Length - remainingBuffer.Length;
		}

		public IEnumerable<object []> DataSource ()
		{
			string[] textPermutations = {
				// Extreme newline scenario
				"E\r\nx\r\nt\r\nr\r\ne\r\nm\r\ne\r\nn\r\ne\r\nw\r\nl\r\ni\r\nn\r\ne\r\ns\r\nc\r\ne\r\nn\r\na\r\nr\r\ni\r\no\r\n",
				// Long text with few line endings
				"""
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
				Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
				Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
				"""
				// Consistent line endings between systems for more consistent performance evaluation.
				.ReplaceLineEndings ("\r\n"),
				// Long text without line endings
				"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sed euismod metus. Phasellus lectus metus, ultricies a commodo quis, facilisis vitae nulla. " +
				"Curabitur mollis ex nisl, vitae mattis nisl consequat at. Aliquam dolor lectus, tincidunt ac nunc eu, elementum molestie lectus. Donec lacinia eget dolor a scelerisque. " +
				"Aenean elementum molestie rhoncus. Duis id ornare lorem. Nam eget porta sapien. Etiam rhoncus dignissim leo, ac suscipit magna finibus eu. Curabitur hendrerit elit erat, sit amet suscipit felis condimentum ut. " +
				"Nullam semper tempor mi, nec semper quam fringilla eu. Aenean sit amet pretium augue, in posuere ante. Aenean convallis porttitor purus, et posuere velit dictum eu."
			};

			bool[] newLinePermutations = { true, false };

			foreach (var text in textPermutations)
			foreach (bool keepNewLine in newLinePermutations) {
				yield return new object [] { text, keepNewLine };
			}
		}
	}
}
