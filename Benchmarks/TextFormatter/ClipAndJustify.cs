using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter {
	[MemoryDiagnoser]
	public class ClipAndJustify {

		[Params (1, 100, 10_000)]
		public int N { get; set; }

		[Benchmark (Baseline = true)]
		[ArgumentsSource (nameof (DataSource))]
		public string RuneListPassthrough (string text, int width, bool justify, TextDirection textDirection)
		{
			string result = string.Empty;
			for (int i = 0; i < N; i++) {
				result = RuneListPassthroughImplementation (text, width, justify, textDirection);
			}
			return result;
		}

		private static string RuneListPassthroughImplementation (string text, int width, bool justify, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			var runes = text.ToRuneList ();
			int slen = runes.Count;
			if (slen > width) {
				if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
					return Tui.StringExtensions.ToString (runes.GetRange (0, Tui.TextFormatter.GetLengthThatFits (text, width)));
				} else {
					return Tui.StringExtensions.ToString (runes.GetRange (0, width));
				}
			} else {
				if (justify) {
					return Tui.TextFormatter.Justify (text, width, ' ', textDirection);
				} else if (Tui.TextFormatter.IsHorizontalDirection (textDirection) && text.GetColumns () > width) {
					return Tui.StringExtensions.ToString (runes.GetRange (0, Tui.TextFormatter.GetLengthThatFits (text, width)));
				}
				return text;
			}
		}

		[Benchmark]
		[ArgumentsSource (nameof (DataSource))]
		public string RentedRuneArray (string text, int width, bool justify, TextDirection textDirection)
		{
			string result = string.Empty;
			for (int i = 0; i < N; i++) {
				result = RentedRuneArrayImplementation (text, width, justify, textDirection);
			}
			return result;
		}

		private static string RentedRuneArrayImplementation (string text, int width, bool justify, TextDirection textDirection = TextDirection.LeftRight_TopBottom)
		{
			const int MaxStackallocRuneBufferSize = 512; // Size of Rune is ~4 bytes, so the stack allocated buffer size is ~2 kiB.

			if (width < 0) {
				throw new ArgumentOutOfRangeException (nameof (width), "Width cannot be negative.");
			}
			if (string.IsNullOrEmpty (text)) {
				return text;
			}

			int maxTextWidth = Tui.TextFormatter.IsHorizontalDirection(textDirection)
				? text.Length * 2
				: text.Length;
			if (maxTextWidth <= width) {
				// Early exit when the worst case fits the width.
				return justify
					? Tui.TextFormatter.Justify (text, width, ' ', textDirection)
					: text;
			}

			Rune[]? rentedRuneArray = null;
			try {
				int maxRuneCount = text.Length;
				Span<Rune> runeBuffer = maxRuneCount <= MaxStackallocRuneBufferSize
					? stackalloc Rune[maxRuneCount]
					: (rentedRuneArray = ArrayPool<Rune>.Shared.Rent(maxRuneCount));

				int freeBufferIdx = 0;
				if (Tui.TextFormatter.IsHorizontalDirection (textDirection)) {
					int maxColumns = width;
					int sumColumns = 0;
					foreach (var rune in text.EnumerateRunes ()) {
						int runeColumns = Math.Max(rune.GetColumns(), 1);
						if (sumColumns + runeColumns > maxColumns) {
							int finalLength = freeBufferIdx;
							return Tui.StringExtensions.ToString (runeBuffer [..finalLength]);
						}
						runeBuffer [freeBufferIdx] = rune;
						freeBufferIdx++;
						sumColumns += runeColumns;
					}

					if (sumColumns < maxColumns && justify) {
						return Tui.TextFormatter.Justify (text, maxColumns, ' ', textDirection);
					}
				} else {
					int maxLength = width;
					int sumLength = 0;
					foreach (var rune in text.EnumerateRunes ()) {
						if (sumLength + 1 > maxLength) {
							int finalLength = freeBufferIdx;
							return Tui.StringExtensions.ToString (runeBuffer [..finalLength]);
						}
						runeBuffer [freeBufferIdx] = rune;
						freeBufferIdx++;
						sumLength++;
					}
				}
				return text;
			} finally {
				if (rentedRuneArray != null) {
					ArrayPool<Rune>.Shared.Return (rentedRuneArray);
				}
			}
		}

		public IEnumerable<object []> DataSource ()
		{
			var directions = new [] {
				TextDirection.LeftRight_TopBottom,
				TextDirection.TopBottom_LeftRight,
			};

			bool[] justification = { true, false };

			string[] texts = {
				"",
				"Hello World",
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

				foreach (int width in maxColumns)
				foreach (bool justify in justification)
				foreach (var direction in directions) {
					yield return new object [] { text, width, justify, direction };
				}
			}
		}
	}
}
