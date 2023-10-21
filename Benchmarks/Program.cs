using BenchmarkDotNet.Running;

namespace Benchmarks;

class Program {
	static void Main (string [] args)
	{
		var switcher = BenchmarkSwitcher.FromAssembly (typeof (Program).Assembly);
		if (args.Length > 0) {
			switcher.Run (args);
		} else {
			switcher.RunAll ();
			// Run single benchmark:
			// BenchmarkRunner.Run<BenchmarkClass> ();
		}
	}
}