using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class WordWrapText {
		[Params (1, 100, 10_000)]
		public int Repetitions { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public List<string> Original (string text, int width, bool preserveTrailingSpaces, int tabWidth, TextDirection textDirection)
		{
			List<string> result = new();
			for (int i = 0; i < Repetitions; i++) {
				result = OriginalImplementation (text, width, preserveTrailingSpaces, tabWidth, textDirection);
			}
			return result;
		}

		private static List<string> OriginalImplementation (string text, int width, bool preserveTrailingSpaces = false, int tabWidth = 0, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}

			int start = 0, end;
			var lines = new List<string> ();

			if (string.IsNullOrEmpty (text)) {
				return lines;
			}

			var runes = Tui.TextFormatter.StripCRLF (text).ToRuneList ();
			if (preserveTrailingSpaces) {
				while ((end = start) < runes.Count) {
					end = GetNextWhiteSpace (start, width, out bool incomplete);
					if (end == 0 && incomplete) {
						start = text.GetRuneCount ();
						break;
					}
					lines.Add (Tui.StringExtensions.ToString (runes.GetRange (start, end - start)));
					start = end;
					if (incomplete) {
						start = text.GetRuneCount ();
						break;
					}
				}
			} else {
				if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
					while ((end = start + Math.Max (Tui.TextFormatter.GetLengthThatFits (runes.GetRange (start, runes.Count - start), width), 1)) < runes.Count) {
						while (runes [end].Value != ' ' && end > start)
							end--;
						if (end == start)
							end = start + Tui.TextFormatter.GetLengthThatFits (runes.GetRange (end, runes.Count - end), width);
						var str = Tui.StringExtensions.ToString (runes.GetRange (start, end - start));
						if (end > start && str.GetColumns () <= width) {
							lines.Add (str);
							start = end;
							if (runes [end].Value == ' ') {
								start++;
							}
						} else {
							end++;
							start = end;
						}
					}
				} else {
					while ((end = start + width) < runes.Count) {
						while (runes [end].Value != ' ' && end > start) {
							end--;
						}
						if (end == start) {
							end = start + width;
						}
						lines.Add (Tui.StringExtensions.ToString (runes.GetRange (start, end - start)));
						start = end;
						if (runes [end].Value == ' ') {
							start++;
						}
					}
				}
			}

			int GetNextWhiteSpace (int from, int cWidth, out bool incomplete, int cLength = 0)
			{
				var lastFrom = from;
				var to = from;
				var length = cLength;
				incomplete = false;

				while (length < cWidth && to < runes.Count) {
					var rune = runes [to];
					if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
						length += rune.GetColumns ();
					} else {
						length++;
					}
					if (length > cWidth) {
						if (to >= runes.Count || (length > 1 && cWidth <= 1)) {
							incomplete = true;
						}
						return to;
					}
					if (rune.Value == ' ') {
						if (length == cWidth) {
							return to + 1;
						} else if (length > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
						}
					} else if (rune.Value == '\t') {
						length += tabWidth + 1;
						if (length == tabWidth && tabWidth > cWidth) {
							return to + 1;
						} else if (length > cWidth && tabWidth > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, out incomplete, length);
						}
					}
					to++;
				}
				if (cLength > 0 && to < runes.Count && runes [to].Value != ' ' && runes [to].Value != '\t') {
					return from;
				} else if (cLength > 0 && to < runes.Count && (runes [to].Value == ' ' || runes [to].Value == '\t')) {
					return lastFrom;
				} else {
					return to;
				}
			}

			if (start < text.GetRuneCount ()) {
				var str = Tui.StringExtensions.ToString (runes.GetRange (start, runes.Count - start));
				if (Tui.TextFormatter.IsVerticalDirection (textDirection) || preserveTrailingSpaces || (!preserveTrailingSpaces && str.GetColumns () <= width)) {
					lines.Add (str);
				}
			}

			return lines;
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public List<string> ArrayBuffers (string text, int width, bool preserveTrailingSpaces, int tabWidth, TextDirection textDirection)
		{
			List<string> result = new();
			for (int i = 0; i < Repetitions; i++) {
				result = ArrayBuffersImplementation (text, width, preserveTrailingSpaces, tabWidth, textDirection);
			}
			return result;
		}

		public static List<string> ArrayBuffersImplementation (
			string text, int width, bool preserveTrailingSpaces = false, int tabWidth = 0,
			TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int MaxStackallocStripBufferSize = 512; // ~1 kiB
			const int MaxStackallocRuneBufferSize = 256; // ~1 kiB

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}

			if (string.IsNullOrEmpty (text)) {
				return new List<string> ();
			}

			int maxTextWidth = Tui.TextFormatter.IsHorizontalDirection(textDirection)
				? text.Length * 2
				: text.Length;
			if (maxTextWidth <= width) {
				// Early exit when the simplest worst case length fits the single line.
				if (preserveTrailingSpaces && !text.Contains ('\t')) {
					return new () { text };
				}
			}

			int start = 0, end;
			var lines = new List<string> ();

			char[]? stripRentedArray = null;
			Rune[]? runeRentedArray = null;
			try {
				Span<char> stripBuffer = text.Length <= MaxStackallocStripBufferSize
					? stackalloc char[MaxStackallocStripBufferSize]
					: (stripRentedArray = ArrayPool<char>.Shared.Rent (text.Length));

				Span<Rune> runeBuffer = text.Length <= MaxStackallocRuneBufferSize
					? stackalloc Rune[MaxStackallocRuneBufferSize]
					: (runeRentedArray = ArrayPool<Rune>.Shared.Rent(text.Length));

				int crlfStrippedCharsWritten = Tui.TextFormatter.StripCRLF (text, stripBuffer);
				var crlfStrippedChars = stripBuffer[..crlfStrippedCharsWritten];

				int runeIdx = 0;
				foreach (var rune in crlfStrippedChars.EnumerateRunes ()) {
					runeBuffer [runeIdx] = rune;
					runeIdx++;
				}
				var runes = runeBuffer[..runeIdx];

				if (preserveTrailingSpaces) {
					while ((end = start) < runes.Length) {
						end = GetNextWhiteSpace (runes, start, width, out bool incomplete);
						if (end == 0 && incomplete) {
							start = text.GetRuneCount ();
							break;
						}
						lines.Add (Tui.StringExtensions.ToString (runes [start..end]));
						start = end;
						if (incomplete) {
							start = text.GetRuneCount ();
							break;
						}
					}
				} else {
					if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
						while ((end = start + Math.Max (Tui.TextFormatter.GetLengthThatFits (runes [start..], width), 1)) < runes.Length) {
							while (runes [end].Value != ' ' && end > start)
								end--;
							if (end == start)
								end = start + Tui.TextFormatter.GetLengthThatFits (runes [end..], width);
							var str = Tui.StringExtensions.ToString (runes[start..end ]);
							if (end > start && str.GetColumns () <= width) {
								lines.Add (str);
								start = end;
								if (runes [end].Value == ' ') {
									start++;
								}
							} else {
								end++;
								start = end;
							}
						}
					} else {
						while ((end = start + width) < runes.Length) {
							while (runes [end].Value != ' ' && end > start) {
								end--;
							}
							if (end == start) {
								end = start + width;
							}
							lines.Add (Tui.StringExtensions.ToString (runes [start..end]));
							start = end;
							if (runes [end].Value == ' ') {
								start++;
							}
						}
					}
				}

				if (start < text.GetRuneCount ()) {
					var str = Tui.StringExtensions.ToString (runes[start..]);
					if (Tui.TextFormatter.IsVerticalDirection (textDirection) || preserveTrailingSpaces || (!preserveTrailingSpaces && str.GetColumns () <= width)) {
						lines.Add (str);
					}
				}

				return lines;
			} finally {
				if (stripRentedArray != null) {
					ArrayPool<char>.Shared.Return (stripRentedArray);
				}
				if (runeRentedArray != null) {
					ArrayPool<Rune>.Shared.Return (runeRentedArray);
				}
			}

			int GetNextWhiteSpace (in ReadOnlySpan<Rune> runes, int from, int cWidth, out bool incomplete, int cLength = 0)
			{
				var lastFrom = from;
				var to = from;
				var length = cLength;
				incomplete = false;

				while (length < cWidth && to < runes.Length) {
					var rune = runes [to];
					if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
						length += rune.GetColumns ();
					} else {
						length++;
					}
					if (length > cWidth) {
						if (to >= runes.Length || (length > 1 && cWidth <= 1)) {
							incomplete = true;
						}
						return to;
					}
					if (rune.Value == ' ') {
						if (length == cWidth) {
							return to + 1;
						} else if (length > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (runes, to + 1, cWidth, out incomplete, length);
						}
					} else if (rune.Value == '\t') {
						length += tabWidth + 1;
						if (length == tabWidth && tabWidth > cWidth) {
							return to + 1;
						} else if (length > cWidth && tabWidth > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (runes, to + 1, cWidth, out incomplete, length);
						}
					}
					to++;
				}
				if (cLength > 0 && to < runes.Length && runes [to].Value != ' ' && runes [to].Value != '\t') {
					return from;
				} else if (cLength > 0 && to < runes.Length && (runes [to].Value == ' ' || runes [to].Value == '\t')) {
					return lastFrom;
				} else {
					return to;
				}
			}
		}

		public IEnumerable<object []> DataSource ()
		{
			//string text, int width, bool preserveTrailingSpaces, int tabWidth, TextDirection textDirection

			var directions = new [] {
				TextDirection.LeftRight_TopBottom,
				TextDirection.TopBottom_LeftRight,
			};

			bool[] trailingSpace = { true, false };

			int[] tabWidths = { 0, 1, 3 };

			string[] texts = {
				"",
				"Hello World\tHello World",
				"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sed euismod metus. Phasellus lectus metus, ultricies a commodo quis, facilisis vitae nulla.",
				"Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ. " +
				"Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé. " +
				"Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.",
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

				foreach (int width in maxColumns) {
					foreach (bool preserveTrailingSpaces in trailingSpace) {
						foreach (var tabWidth in tabWidths) {
							foreach (var direction in directions) {
								yield return new object [] { text, width, preserveTrailingSpaces, tabWidth, direction };
							}
						}
					}
				}
			}
		}
	}
}
