using BenchmarkDotNet.Attributes;
using System.Text;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[ShortRunJob]
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
		public string StringBuilderCharSpanSlice (string str, bool keepNewLine = false)
		{
			string result = string.Empty;
			for (int i = 0; i < Repetitions; i++) {
				result = StringBuilderCharSpanSliceImplementation (str, keepNewLine);
			}
			return result;
		}

		// The re-implementations have a bit different outcome compared to baseline. Baseline implementation preserves CR when the string has LF+CR which seems like off-by-one error.
		private static string StringBuilderCharSpanSliceImplementation (string str, bool keepNewLine = false)
		{
			var stringBuilder = new StringBuilder();

			var remaining = str.AsSpan ();
			while (remaining.Length > 0) {
				int nextLineBreakIndex = remaining.IndexOfAny ('\r', '\n');
				if (nextLineBreakIndex == -1) {
					if (str.Length == remaining.Length) {
						return str;
					}
					stringBuilder.Append (remaining);
					break;
				}

				var slice = remaining.Slice (0, nextLineBreakIndex);
				stringBuilder.Append (slice);

				// Evaluate how many line break characters to preserve.
				int stride;
				char lineBreakChar = remaining [nextLineBreakIndex];
				if (lineBreakChar == '\n') {
					stride = 1;
					if (keepNewLine) {
						stringBuilder.Append ('\n');
					}
				} else /* '\r' */ {
					bool crlf = (nextLineBreakIndex + 1) < remaining.Length && remaining [nextLineBreakIndex + 1] == '\n';
					if (crlf) {
						stride = 2;
						if (keepNewLine) {
							stringBuilder.Append ('\n');
						}
					} else {
						stride = 1;
						if (keepNewLine) {
							stringBuilder.Append ('\r');
						}
					}
				}
				remaining = remaining.Slice (slice.Length + stride);
			}
			return stringBuilder.ToString ();
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string EarlyExitStringBuilderCharSpanSlice(string str, bool keepNewLine = false)
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
				// Evaluate how many line break characters to preserve.
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

		public IEnumerable<object []> DataSource ()
		{
			// TODO: Better source texts. The 100 length does not have any newline characters which skews the numbers in favour of early exit optimization.
			string textSource =
				"""
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
				Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
				Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
				""";
			// Consistent line endings between systems keeps performance evaluation more consistent.
			textSource = textSource.ReplaceLineEndings ("\r\n");

			bool[] permutations = { true, false };
			foreach (bool keepNewLine in permutations) {
				yield return new object [] { textSource [..1], keepNewLine };
				yield return new object [] { textSource [..10], keepNewLine };
				yield return new object [] { textSource [..100], keepNewLine };
				yield return new object [] { textSource, keepNewLine };
			}
		}
	}
}
