using System;
using System.Threading;
using System.Windows.Forms;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace Sudoku.Benchmarks;
[CPUUsageDiagnoser]
public class MinimizeBenchmark
{
	private WinFormsSettings settings = null!;
	private SudokuController controller = null!;
    private CancellationToken token;
	private BaseProblem seedProblem = null!;
    private int targetSeverity;
    [GlobalSetup]
    public void Setup()
    {
        settings = new WinFormsSettings
        {
            GenerateNormalSudoku = true,
            GenerateXSudoku = false,
            UsePrecalculatedProblems = false,
            GenerateMinimalProblems = true,
            MinValues = 25
        };
        controller = new SudokuController(settings, new BenchmarkUserInteraction());
        controller.CreateNewProblem(settings.GenerateXSudoku, notify: false);
        token = CancellationToken.None;
        var generationParameters = new GenerationParameters(settings);
        controller.GenerateBaseProblem(generationParameters, usePrecalculated: false, progress: null, token: token).GetAwaiter().GetResult();
        controller.CurrentProblem.FindSolutions(2, token);
        controller.CurrentProblem.SolverTask?.Wait();
        seedProblem = controller.CurrentProblem.Clone();
        targetSeverity = settings.SeverityLevel;
        if (targetSeverity == 0)
            targetSeverity = 1;
    }

	[Benchmark]
	public bool MinimizeProblem()
    {
        Console.WriteLine("MinimizeProblem iteration started.");
        try
        {
            var workingProblem = seedProblem.Clone();
            controller.UpdateProblem(workingProblem);
			var minimized = controller.Minimize(targetSeverity, progress: null, token: token).GetAwaiter().GetResult();
			Console.WriteLine($"MinimizeProblem iteration finished successfully (result: {(minimized != null ? "OK" : "null")}).");
			return minimized != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MinimizeProblem iteration failed: {ex}");
            throw;
        }
    }

    private sealed class BenchmarkUserInteraction : IUserInteraction
    {
        public void ShowError(string message)
        {
        }

        public void ShowInfo(string message)
        {
        }

        public DialogResult Confirm(string message, MessageBoxButtons buttons = MessageBoxButtons.YesNo) => DialogResult.Yes;
        public int GetSeverity() => 1;
        public string AskForFilename(string defaultExt) => string.Empty;
    }
}