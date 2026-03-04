namespace BenchmarkSuite1;

using BenchmarkDotNet.Running;

using Sudoku.BenchmarkSuite1;

internal class Program
{
	static void Main(string[] args)
	{
        MinimizeLogAnalyzer.Run(@"D:\User\cp\Projects\Visual Studio\Sudoku\WebClient\AllSudokus.solutions");
        // BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
