using System;
using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System.Windows.Forms;

namespace Sudoku.Benchmarks;

[CPUUsageDiagnoser]
public class SolveSudokuFileBenchmark
{
    private const int ProblemCount = 1000;
    private const string DataFileName = "NormalSudokus.sudoku";
    private static readonly char[] EmptyElapsedTime = new string('0', 16).ToCharArray();

    private WinFormsSettings settings = null!;
    private SudokuController controller = null!;
    private SudokuFileService fileService = null!;
    private CancellationToken token;
    private BenchmarkUserInteraction ui = null!;
    private char[][] problems = Array.Empty<char[]>();
    private string dataFilePath = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        ui = new BenchmarkUserInteraction();
        settings = new WinFormsSettings
        {
            GenerateNormalSudoku = true,
            GenerateXSudoku = false,
            UsePrecalculatedProblems = false,
            GenerateMinimalProblems = false,
            MinValues = 25
        };

        controller = new SudokuController(settings, ui);
        controller.CreateNewProblem(settings.GenerateXSudoku, notify: false);
        fileService = new SudokuFileService(controller.CurrentProblem, settings, ui);
        token = CancellationToken.None;

        dataFilePath = ResolveSudokuFilePath();
        problems = File.ReadLines(dataFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .Where(line => line.Length >= WinFormsSettings.TotalCellCount)
            .Take(ProblemCount)
            .Select(line => line.ToCharArray(0, WinFormsSettings.TotalCellCount))
            .ToArray();

        if(problems.Length < ProblemCount)
            throw new InvalidOperationException($"Datei {dataFilePath} liefert nur {problems.Length} gültige Rätsel (benötigt: {ProblemCount}).");
    }

    [Benchmark]
    public int SolveFirst1000Problems()
    {
        int solved = 0;

        for(int i = 0; i < problems.Length; i++)
        {
            controller.CreateNewProblem(settings.GenerateXSudoku, notify: false);
            fileService.Sudoku = controller.CurrentProblem;
            fileService.InitProblem(problems[i], EmptyElapsedTime, string.Empty);

            controller.Solve(findAllSolutions: false, progress: null, token).GetAwaiter().GetResult();

            if(controller.CurrentProblem.NumberOfSolutions == 0)
                throw new InvalidOperationException($"Puzzle #{i + 1} aus {dataFilePath} konnte nicht gelöst werden.");

            solved++;
        }

        return solved;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        controller?.Dispose();
    }

    private static string ResolveSudokuFilePath()
    {
        string? directory = AppContext.BaseDirectory;

        while(!string.IsNullOrEmpty(directory))
        {
            string candidate = Path.Combine(directory, "WebClient", DataFileName);
            if(File.Exists(candidate))
                return candidate;

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new FileNotFoundException($"Datei {DataFileName} wurde nicht gefunden.");
    }

    private sealed class BenchmarkUserInteraction : IUserInteraction
    {
        public void ShowError(string message) { }
        public void ShowInfo(string message) { }
        public DialogResult Confirm(string message, MessageBoxButtons buttons = MessageBoxButtons.YesNo) => DialogResult.Yes;
        public int GetSeverity() => 1;
        public string AskForFilename(string defaultExt) => string.Empty;
    }
}