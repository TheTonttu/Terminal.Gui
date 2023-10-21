using BenchmarkDotNet.Attributes;
using System.Text;
using Terminal.Gui;
using Tui = Terminal.Gui;

namespace Benchmarks.TextFormatter;

[MemoryDiagnoser]
public class ReplaceCRLFWithSpace {

	private char [] _buffer = default!;

	[GlobalSetup]
	public void GlobalSetup ()
	{
		// Buffer for span buffer implementation.
		// The idea is that application would reuse a buffer and resize it whenever needed throughout the application lifetime.
		_buffer = new char [1000];
	}

	[Benchmark (Baseline = true)]
	[ArgumentsSource (nameof (DataSource))]
	public string ToRuneListReplace (string str)
	{
		var runes = str.ToRuneList ();
		for (int i = 0; i < runes.Count; i++) {
			switch (runes [i].Value) {
			case '\n':
				runes [i] = (Rune)' ';
				break;

			case '\r':
				if ((i + 1) < runes.Count && runes [i + 1].Value == '\n') {
					runes [i] = (Rune)' ';
					runes.RemoveAt (i + 1);
					i++;
				} else {
					runes [i] = (Rune)' ';
				}
				break;
			}
		}
		return Tui.StringExtensions.ToString (runes);
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public string EarlyExitStringBuilderCharSpanSlice (string str)
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

		// The first newline is not skipped at this point because the condition has not been evaluated.
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
			// Replace newlines
			char newlineChar = remaining [newlineCharIndex];
			if (newlineChar == '\n') {
				stride++;
				stringBuilder.Append (' ');
			} else /* '\r' */ {
				int nextCharIndex = newlineCharIndex + 1;
				bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
				if (crlf) {
					stride += 2;
					stringBuilder.Append (' ');
				} else {
					stride++;
					stringBuilder.Append (' ');
				}
			}
			remaining = remaining [stride..];
		}
		stringBuilder.Append (remaining);
		return stringBuilder.ToString ();
	}

	[Benchmark]
	[ArgumentsSource (nameof (DataSource))]
	public string SpanBuffer (string str)
	{

		int charsWritten = SpanBufferImplementation (str, _buffer);
		return new string (_buffer, 0, charsWritten);
	}

	private static int SpanBufferImplementation (in ReadOnlySpan<char> str, in Span<char> buffer)
	{
		const string newlineChars = "\r\n";

		var remaining = str;
		int firstNewlineCharIndex = remaining.IndexOfAny (newlineChars);
		// Early exit if there are no newline characters.
		if (firstNewlineCharIndex < 0) {
			str.CopyTo (buffer);
			return str.Length;
		}

		var remainingBuffer = buffer;

		var firstSegment = remaining[..firstNewlineCharIndex];
		firstSegment.CopyTo (remainingBuffer);
		remainingBuffer = remainingBuffer [firstSegment.Length..];

		// The first newline is not skipped at this point because the condition has not been evaluated.
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
			// Replace newlines
			char newlineChar = remaining [newlineCharIndex];
			if (newlineChar == '\n') {
				stride++;
				remainingBuffer [0] = ' ';
				remainingBuffer = remainingBuffer [1..];
			} else /* '\r' */ {
				int nextCharIndex = newlineCharIndex + 1;
				bool crlf = nextCharIndex < remaining.Length && remaining [nextCharIndex] == '\n';
				if (crlf) {
					stride += 2;
					remainingBuffer [0] = ' ';
					remainingBuffer = remainingBuffer [1..];
				} else {
					stride++;
					remainingBuffer [0] = ' ';
					remainingBuffer = remainingBuffer [1..];
				}
			}
			remaining = remaining [stride..];
		}
		remaining.CopyTo (remainingBuffer);
		remainingBuffer = remainingBuffer [remaining.Length..];
		return buffer.Length - remainingBuffer.Length;
	}

	public IEnumerable<object> DataSource ()
	{
		// Extreme newline scenario
		yield return "E\r\nx\r\nt\r\nr\r\ne\r\nm\r\ne\r\nn\r\ne\r\nw\r\nl\r\ni\r\nn\r\ne\r\ns\r\nc\r\ne\r\nn\r\na\r\nr\r\ni\r\no\r\n";
		// Long text with few line endings
		yield return
			"""
				Ĺόŕéḿ íṕśúḿ d́όĺόŕ śít́ áḿét́, ćόńśéćt́ét́úŕ ád́íṕíśćíńǵ éĺít́. Ṕŕáéśéńt́ q́úíś ĺúćt́úś éĺít́. Íńt́éǵéŕ út́ áŕćú éǵét́ d́όĺόŕ śćéĺéŕíśq́úé ḿát́t́íś áć ét́ d́íáḿ.
				Ṕéĺĺéńt́éśq́úé śéd́ d́áṕíb́úś ḿáśśá, v́éĺ t́ŕíśt́íq́úé d́úí. Śéd́ v́ít́áé ńéq́úé éú v́éĺít́ όŕńáŕé áĺíq́úét́. Út́ q́úíś όŕćí t́éḿṕόŕ, t́éḿṕόŕ t́úŕṕíś íd́, t́éḿṕúś ńéq́úé.
				Ṕŕáéśéńt́ śáṕíéń t́úŕṕíś, όŕńáŕé v́éĺ ḿáúŕíś át́, v́áŕíúś śúśćíṕít́ áńt́é. Út́ ṕúĺv́íńáŕ t́úŕṕíś ḿáśśá, q́úíś ćúŕśúś áŕćú f́áúćíb́úś íń.
				Óŕćí v́áŕíúś ńát́όq́úé ṕéńát́íb́úś ét́ ḿáǵńíś d́íś ṕáŕt́úŕíéńt́ ḿόńt́éś, ńáśćét́úŕ ŕíd́íćúĺúś ḿúś. F́úśćé át́ éx́ b́ĺáńd́ít́, ćόńv́áĺĺíś q́úáḿ ét́, v́úĺṕút́át́é ĺáćúś.
				Śúśṕéńd́íśśé śít́ áḿét́ áŕćú út́ áŕćú f́áúćíb́úś v́áŕíúś. V́ív́áḿúś śít́ áḿét́ ḿáx́íḿúś d́íáḿ. Ńáḿ éx́ ĺéό, ṕh́áŕét́ŕá éú ĺόb́όŕt́íś át́, t́ŕíśt́íq́úé út́ f́éĺíś.
				"""
			// Consistent line endings between systems for more consistent performance evaluation.
			.ReplaceLineEndings ("\r\n");
		// Long text without line endings
		yield return
			"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla sed euismod metus. Phasellus lectus metus, ultricies a commodo quis, facilisis vitae nulla. " +
			"Curabitur mollis ex nisl, vitae mattis nisl consequat at. Aliquam dolor lectus, tincidunt ac nunc eu, elementum molestie lectus. Donec lacinia eget dolor a scelerisque. " +
			"Aenean elementum molestie rhoncus. Duis id ornare lorem. Nam eget porta sapien. Etiam rhoncus dignissim leo, ac suscipit magna finibus eu. Curabitur hendrerit elit erat, sit amet suscipit felis condimentum ut. " +
			"Nullam semper tempor mi, nec semper quam fringilla eu. Aenean sit amet pretium augue, in posuere ante. Aenean convallis porttitor purus, et posuere velit dictum eu.";
	}
}
