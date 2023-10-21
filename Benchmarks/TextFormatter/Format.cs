using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class Format {

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public List<string> Original (
			string text, int width, bool justify, bool wordWrap,
			bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "width cannot be negative");
			}
			List<string> lineResult = new List<string> ();

			if (string.IsNullOrEmpty (text) || width == 0) {
				lineResult.Add (string.Empty);
				return lineResult;
			}

			if (wordWrap == false) {
				text = Tui.TextFormatter.ReplaceCRLFWithSpace (text);
				lineResult.Add (Tui.TextFormatter.ClipAndJustify (text, width, justify, textDirection));
				return lineResult;
			}

			var runes = Tui.TextFormatter.StripCRLF (text, true).ToRuneList ();
			int runeCount = runes.Count;
			int lp = 0;
			for (int i = 0; i < runeCount; i++) {
				Rune c = runes [i];
				if (c.Value == '\n') {
					var wrappedLines = Tui.TextFormatter.WordWrapText (Tui.StringExtensions.ToString (runes.GetRange (lp, i - lp)), width, preserveTrailingSpaces, tabWidth, textDirection);
					foreach (var line in wrappedLines) {
						lineResult.Add (Tui.TextFormatter.ClipAndJustify (line, width, justify, textDirection));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (string.Empty);
					}
					lp = i + 1;
				}
			}
			foreach (var line in Tui.TextFormatter.WordWrapText (Tui.StringExtensions.ToString (runes.GetRange (lp, runeCount - lp)), width, preserveTrailingSpaces, tabWidth, textDirection)) {
				lineResult.Add (Tui.TextFormatter.ClipAndJustify (line, width, justify, textDirection));
			}

			return lineResult;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public List<string> ArrayBuffer (
			string text, int width, bool justify, bool wordWrap,
			bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int MaxStackallocCharBufferSize = 512; // ~1 kiB

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "width cannot be negative");
			}
			List<string> lineResult = new List<string> ();

			if (string.IsNullOrEmpty (text) || width == 0) {
				lineResult.Add (string.Empty);
				return lineResult;
			}

			char[]? charRentedArray = null;
			try {
				Span<char> charBuffer = text.Length <= MaxStackallocCharBufferSize
					? stackalloc char[MaxStackallocCharBufferSize]
					: (charRentedArray = ArrayPool<char>.Shared.Rent (text.Length));

				if (wordWrap == false) {
					int replaceCharsWritten = Tui.TextFormatter.ReplaceCRLFWithSpace (text, charBuffer);
					lineResult.Add (Tui.TextFormatter.ClipAndJustify (new string (charBuffer [..replaceCharsWritten]), width, justify, textDirection));
					return lineResult;
				}

				int stripCharsWritten = Tui.TextFormatter.StripCRLF (text, charBuffer, keepNewLine: true);
				var strippedText = charBuffer[..stripCharsWritten];

				var remaining = strippedText;
				while (remaining.Length > 0) {
					int newlineIdx = remaining.IndexOf('\n');
					if (newlineIdx == -1) {
						break;
					}

					var lineSegment = remaining[..newlineIdx];
					var wrappedLines = Tui.TextFormatter.WordWrapText (lineSegment, width, preserveTrailingSpaces, tabWidth, textDirection);
					foreach (var line in wrappedLines) {
						lineResult.Add (Tui.TextFormatter.ClipAndJustify (line, width, justify, textDirection));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (string.Empty);
					}
					remaining = remaining [(lineSegment.Length + 1)..];
				}

				foreach (var line in Tui.TextFormatter.WordWrapText (remaining, width, preserveTrailingSpaces, tabWidth, textDirection)) {
					lineResult.Add (Tui.TextFormatter.ClipAndJustify (line, width, justify, textDirection));
				}

				return lineResult;
			} finally {
				if (charRentedArray != null) {
					ArrayPool<char>.Shared.Return (charRentedArray);
				}
			}
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public List<string> ExactStackallocSize (
			string text, int width, bool justify, bool wordWrap,
			bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int MaxStackallocCharBufferSize = 512; // ~1 kiB

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "width cannot be negative");
			}
			List<string> lineResult = new List<string> ();

			if (string.IsNullOrEmpty (text) || width == 0) {
				lineResult.Add (string.Empty);
				return lineResult;
			}

			char[]? charRentedArray = null;
			try {
				Span<char> charBuffer = text.Length <= MaxStackallocCharBufferSize
					? stackalloc char[text.Length]
					: (charRentedArray = ArrayPool<char>.Shared.Rent (text.Length));

				if (wordWrap == false) {
					int replaceCharsWritten = Tui.TextFormatter.ReplaceCRLFWithSpace (text, charBuffer);
					lineResult.Add (Tui.TextFormatter.ClipAndJustify (new string (charBuffer [..replaceCharsWritten]), width, justify, textDirection));
					return lineResult;
				}

				int stripCharsWritten = Tui.TextFormatter.StripCRLF (text, charBuffer, keepNewLine: true);
				var strippedText = charBuffer[..stripCharsWritten];

				var remaining = strippedText;
				while (remaining.Length > 0) {
					int newlineIdx = remaining.IndexOf('\n');
					if (newlineIdx == -1) {
						break;
					}

					var lineSegment = remaining[..newlineIdx];
					var wrappedLines = Tui.TextFormatter.WordWrapText (lineSegment, width, preserveTrailingSpaces, tabWidth, textDirection);
					foreach (var line in wrappedLines) {
						lineResult.Add (Tui.TextFormatter.ClipAndJustify (line, width, justify, textDirection));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (string.Empty);
					}
					remaining = remaining [(lineSegment.Length + 1)..];
				}

				foreach (var line in Tui.TextFormatter.WordWrapText (remaining, width, preserveTrailingSpaces, tabWidth, textDirection)) {
					lineResult.Add (Tui.TextFormatter.ClipAndJustify (line, width, justify, textDirection));
				}

				return lineResult;
			} finally {
				if (charRentedArray != null) {
					ArrayPool<char>.Shared.Return (charRentedArray);
				}
			}
		}

		public IEnumerable<object []> DataSource ()
		{
			// TODO: Too many permutations to run. Select permutations that cover the most common scenarios.

			string[] texts = {
				// Extreme newline scenario
				"E\r\nx\r\nt\r\nr\r\ne\r\nm\r\ne\r\nn\r\ne\r\nw\r\nl\r\ni\r\nn\r\ne\r\ns\r\nc\r\ne\r\nn\r\na\r\nr\r\ni\r\no\r\n",
				"Hello World\tHello world",
				// Single line
				"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
				"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
				"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
				// Multiline
				"""
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń."					
				"""
				// Consistent line endings between systems for more consistent performance evaluation.
				.ReplaceLineEndings("\r\n"),
			};

			bool[] justification = { true/*, false*/ };
			bool[] wordWrapping = { true /*, false*/ };
			bool[] spacePreservation = { true/*, false*/ };
			int[] tabWidths = { /*0, 1,*/ 3 };
			var textDirections = new [] {
				TextDirection.LeftRight_TopBottom,
				TextDirection.TopBottom_LeftRight
			};

			foreach (string text in texts) {
				var maxColumns = new List<int>() {
					1,
					//Math.Max((int)(text.EnumerateRunes().Count() * 0.25), 1),
					Math.Max((int)(text.EnumerateRunes().Count() * 0.50), 1),
					//Math.Max((int)(text.EnumerateRunes().Count() * 0.75), 1),
					Math.Max((int)(text.EnumerateRunes().Count() * 2.0), 1),
				};

				while (maxColumns.Count >= 2 && maxColumns [1] <= 1) {
					maxColumns.RemoveAt (1);
				}

				foreach (int width in maxColumns)
				foreach (bool justify in justification)
				foreach (bool wordWrap in wordWrapping)
				foreach (bool preserveTrailingWhiteSpaces in spacePreservation)
				foreach (int tabWidth in tabWidths)
				foreach (var direction in textDirections) {
					yield return new object [] { text, width, justify, wordWrap, preserveTrailingWhiteSpaces, tabWidth, direction };
				}
			}
		}
	}
}