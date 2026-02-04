using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using System.Windows.Forms;
using Microsoft.VSDiagnostics;

namespace Sudoku.Benchmarks;
[CPUUsageDiagnoser]
public class GenerateBaseProblemBenchmark
{
    private WinFormsSettings settings;
    private SudokuController controller;
    private CancellationToken token;
    [GlobalSetup]
    public void Setup()
    {
        settings = new WinFormsSettings
        {
            GenerateNormalSudoku = true,
            GenerateXSudoku = false,
            UsePrecalculatedProblems = false,
            GenerateMinimalProblems = false,
            MinValues = 25
        };
        controller = new SudokuController(settings, new BenchmarkUserInteraction());
        controller.CreateNewProblem(settings.GenerateXSudoku, notify: false);
        token = CancellationToken.None;
    }

    [Benchmark]
    public bool GenerateBaseProblem()
    {
        Console.WriteLine("GenerateBaseProblem iteration started.");
        var parameters = new GenerationParameters(settings);
        try
        {
            var result = controller.GenerateBaseProblem(parameters, usePrecalculated: false, progress: null, token: token).GetAwaiter().GetResult();
            Console.WriteLine($"GenerateBaseProblem iteration finished successfully (result: {result}).");
            return result;
        }
        catch(Exception ex)
        {
            Console.WriteLine($"GenerateBaseProblem iteration failed: {ex}");
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