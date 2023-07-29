using System.Text;

namespace Benchmarks {

	/// <summary>
	/// Encoding with benchmark summary column friendly name.
	/// </summary>
	public class BenchmarkFormattedEncoding {

		public Encoding Encoding { get; }

		public BenchmarkFormattedEncoding (Encoding encoding)
		{
			Encoding = encoding;
		}

		public override string ToString () => Encoding.EncodingName;
	}
}
