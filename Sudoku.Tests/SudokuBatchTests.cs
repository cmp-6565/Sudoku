using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sudoku.Sudoku.Tests;

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public class LiveDebugListener: TraceListener
{
    public override void Write(string message) { Debugger.Log(0, null, message); }

    public override void WriteLine(string message) { Debugger.Log(0, null, message + Environment.NewLine); }
}

[TestClass]
public sealed class SudokuBatchTests
{
    private const int SudokuBatchSize = 500;
    private const byte ReadOnlyEncodingOffset = 64;
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory!, "..", "..", "..", ".."));
    private static readonly string NormalSudokusFilePath = Path.Combine(RepoRoot, "WebClient", "NormalSudokus.sudoku");
    private static readonly string NormalSudokusSolutionsFilePath = Path.Combine(RepoRoot, "WebClient", "NormalSudokus.solutions");
    private static readonly string XSudokusFilePath = Path.Combine(RepoRoot, "WebClient", "XSudokus.sudoku");
    private static readonly string XSudokusSolutionsFilePath = Path.Combine(RepoRoot, "WebClient", "XSudokus.solutions");
    Random rand = new Random(/* unchecked((int)DateTime.Now.Ticks)*/ 1);

    private record struct TestResult(string Puzzle, string MinimalProblem, int Diff, string Solution, double Seconds);

    private WinFormsSettings settings;
    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public void Setup()
    {
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new LiveDebugListener());
    }

    [TestMethod]
    public async Task SolveAndMinimizeNormalSudokus()
    {
        settings = CreateSettings(false);
        string[] puzzles = LoadPuzzles(NormalSudokusFilePath);
        List<TestResult> referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, NormalSudokusSolutionsFilePath, out bool referenceExists);
        await SolveAndMinimizeSudokus(puzzles, referenceSolutions, referenceExists, NormalSudokusSolutionsFilePath);
    }

    [TestMethod]
    public async Task SolveAndMinimizeXSudokus()
    {
        settings = CreateSettings(true);
        string[] puzzles = LoadPuzzles(XSudokusFilePath);
        List<TestResult> referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, XSudokusSolutionsFilePath, out bool referenceExists);
        await SolveAndMinimizeSudokus(puzzles, referenceSolutions, referenceExists, XSudokusSolutionsFilePath);
    }

    [TestMethod]
    public async Task GenerateAndSolveNormalSudokus()
    {
        settings = CreateSettings(false);
        await GenerateAndSolveSudokus();
    }
    [TestMethod]
    public async Task GenerateAndSolveXSudokus()
    {
        settings = CreateSettings(true);
        await GenerateAndSolveSudokus();
    }

    [TestMethod]
    public async Task CheckMinimizeXSudoku()
    {
        settings = CreateSettings(true);
        string minimalXSudoku = "000000000000000000000001000000000100000000000020340000100000053600007000000008020";
        await CheckMinimizeSudoku(minimalXSudoku);
    }
    [TestMethod]
    public async Task CheckMinimizeNormalSudoku()
    {
        settings = CreateSettings(false);
        string minimalNormalSudoku = "000200001940000000500000000601300000000090850000000040750040000000600200000000000";
        await CheckMinimizeSudoku(minimalNormalSudoku);
    }
    private async Task CheckMinimizeSudoku(string minimalSudoku)
    {
        int randomCells = settings.GenerateXSudoku? 8: 15;
        int passes = 100;
        string computedSolution;
        int originalCount = CountValues(minimalSudoku.ToCharArray());

        TestContext?.WriteLine($"Löse ein minimales Problem ({minimalSudoku}) und setze {randomCells} zufällige Werte, minimiere und vergleiche mit der Originalllösung, es werden Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\" betrachtet.");
        TestContext?.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Diff.",-6} {"Lösung",-81} {"Total",10} {"Minimierung",10}");

        Trace.WriteLine($"Löse ein minimales Problem ({minimalSudoku}) und setze {randomCells} zufällige Werte, minimiere und vergleiche mit der Originalllösung, es werden Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\" betrachtet.");
        Trace.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Diff.",-6} {"Lösung",-81} {"Total",10} {"Minimierung",10}");

        var sudoku = CreateProblem(minimalSudoku);
        await FindSolution(sudoku, settings.FindAllSolutions ? settings.MaxSolutions : 1);
        computedSolution = SerializeSolution(sudoku);

        for(int i=0; i < passes; i++)
        {
            string solution;
            string problem=minimalSudoku;

            char[] minimalSudokuChars = problem.ToCharArray();
            for(int j=0; j < randomCells; j++)
            {
                int cell = rand.Next(0, 81);
                minimalSudokuChars[cell] = computedSolution[cell];
            }
            problem = new string(minimalSudokuChars);
            sudoku = CreateProblem(problem);

            Stopwatch stopwatch = Stopwatch.StartNew();

            await FindSolution(sudoku, settings.FindAllSolutions ? settings.MaxSolutions : 1);
            solution = SerializeSolution(sudoku);
            Assert.AreEqual(computedSolution, solution, $"Lösung für Problem {problem} stimmt nicht mit Originallösung überein");

            Stopwatch minimize = Stopwatch.StartNew();
            var minimizedProblem = await sudoku.Minimize(settings.SeverityLevel, CancellationToken.None);
            minimize.Stop();
            minimizedProblem.ResetMatrix();

            Assert.IsNotNull(minimizedProblem, $"Minimalproblem konnte nicht ermittelt werden.");
            string minimizedSerialized = SerializeProblem(minimizedProblem);
            Assert.AreEqual(originalCount, CountValues(minimizedSerialized.ToCharArray()), $"Das ermittelte Minimalproblem für Sudoku #{i + 1} ist nicht minimal.");

            Assert.AreEqual(minimalSudoku, minimizedSerialized, $"Minimiertes Problem stimmt nicht mit der Original-Problem überein.");

            await FindSolution(minimizedProblem!, settings.FindAllSolutions ? settings.MaxSolutions : 1);
            string minimizedSolution = SerializeSolution(minimizedProblem!);
            Assert.AreEqual(computedSolution, minimizedSolution, $"Minimierte Lösung stimmt nicht mit der Original-Lösung überein.");

            stopwatch.Stop();
            TestContext?.WriteLine($"{i + 1,4:D4} {problem,-81} {minimizedSerialized,-81} {CountValues(problem.ToCharArray())- CountValues(minimizedSerialized.ToCharArray()),6:d2} {minimizedSolution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2} {minimize.Elapsed.TotalSeconds,10:F2}");
            Trace.WriteLine($"{i + 1,4:D4} {problem,-81} {minimizedSerialized,-81} {CountValues(problem.ToCharArray()) - CountValues(minimizedSerialized.ToCharArray()),6:d2} {minimizedSolution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2} {minimize.Elapsed.TotalSeconds,10:F2}");
        }
    }

    private int CountValues(char[] values)
    {
        int count = 0;
        for(int i = 0; i < values.Length; i++)
            if(values[i] != '0') count++;
        return count; 
    }

    private async Task GenerateAndSolveSudokus()
    {
        TestContext?.WriteLine($"Generiere und löse {SudokuBatchSize} Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\".");
        TestContext?.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Lösung",-81} {"Sekunden",10}");

        for(int index = 0; index < SudokuBatchSize; index++)
        {
            var controller = new SudokuController(settings, null);
            controller.CreateNewProblem(settings.GenerateXSudoku, false);

            Stopwatch stopwatch = Stopwatch.StartNew();

            await controller.GenerateBatch(settings.SeverityLevel, false, new Action<object, string>(GenerationFinished), null, null, CancellationToken.None);
            string problem = SerializeProblem(controller.CurrentProblem);
            await FindSolution(controller.CurrentProblem, settings.FindAllSolutions ? settings.MaxSolutions : 1);
            string solution = SerializeSolution(controller.CurrentProblem);

            stopwatch.Stop();
            TestContext?.WriteLine($"{index + 1,4:D4} {problem,-81} {solution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2}");
            Trace.WriteLine($"{index + 1,4:D4} {problem,-81} {solution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2}");
        }
    }
    public void GenerationFinished(Object o, string s) {  }

    private async Task SolveAndMinimizeSudokus(string[] puzzles, List<TestResult> referenceSolutions, bool referenceExists, string solutionFilename)
    {
        TestContext?.WriteLine($"Löse und minimiere {puzzles.Length} Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\". Referenzlösungen {(referenceExists ? "werden verglichen" : "werden erstellt")}.");
        TestContext?.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Diff.",-6} {"Lösung",-81} {"Sekunden",10}");

        Trace.WriteLine($"Löse und minimiere {puzzles.Length} Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\". Referenzlösungen {(referenceExists ? "werden verglichen" : "werden erstellt")}.");
        Trace.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Diff.",-6} {"Lösung",-81} {"Sekunden",10}");
        for(int index = 0; index < puzzles.Length; index++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string serializedPuzzle = puzzles[index];

            var sudoku = CreateProblem(serializedPuzzle);
            serializedPuzzle = SerializeProblem(sudoku); // remove read-only encoding for display
            int diff = sudoku.nValues;

            await FindSolution(sudoku, settings.FindAllSolutions ? settings.MaxSolutions : 1);
            string computedSolution = SerializeSolution(sudoku);

            var minimizedProblem = await sudoku.Minimize(settings.SeverityLevel, CancellationToken.None);
            minimizedProblem.ResetMatrix();
            diff -= minimizedProblem.nValues;

            Assert.IsNotNull(minimizedProblem, $"Minimalproblem für Sudoku #{index + 1} konnte nicht ermittelt werden.");
            Assert.IsTrue(diff >= 0, $"Das ermittelte Minimalproblem für Sudoku #{index + 1} ist nicht minimal.");

            string minimizedSerialized = SerializeProblem(minimizedProblem);

            await FindSolution(minimizedProblem!, settings.FindAllSolutions? settings.MaxSolutions : 1);
            string minimizedSolution = SerializeSolution(minimizedProblem!);

            Assert.AreEqual(computedSolution, minimizedSolution, $"Minimierte Lösung für Sudoku #{index + 1} stimmt nicht mit der Original-Lösung überein.");

            stopwatch.Stop();

            if(referenceExists)
            {
                Assert.AreEqual(referenceSolutions[index].Solution, computedSolution, $"Lösung #{index + 1} weicht von der Referenz in \"{solutionFilename}\" ab.");
                Assert.AreEqual(referenceSolutions[index].MinimalProblem, minimizedSerialized, $"Minimales Problem #{index + 1} weicht von der Referenz in \"{solutionFilename}\" ab.");
                Assert.AreEqual(referenceSolutions[index].Diff, diff, $"Diff #{index + 1} weicht von der Referenz in \"{solutionFilename}\" ab.");
            }
            else
            {
                referenceSolutions.Add(new TestResult { Puzzle = serializedPuzzle, Solution = computedSolution, MinimalProblem = minimizedSerialized, Diff = diff, Seconds = stopwatch.Elapsed.TotalSeconds });
            }

            TestContext?.WriteLine($"{index + 1,4:D4} {serializedPuzzle,-81} {minimizedSerialized,-81} {diff,6:d2} {minimizedSolution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2}");
            Trace.WriteLine($"{index + 1,4:D4} {serializedPuzzle,-81} {minimizedSerialized,-81} {diff,6:d2} {minimizedSolution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2}");
        }

        if(!referenceExists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(solutionFilename) ?? RepoRoot);
            File.WriteAllText(solutionFilename, JsonSerializer.Serialize(referenceSolutions));
        }
    }

    private static WinFormsSettings CreateSettings(bool xSudoku)
    {
        var configuration = new WinFormsSettings
        {
            GenerateNormalSudoku = !xSudoku,
            GenerateXSudoku = xSudoku,
            UsePrecalculatedProblems = false,
            GenerateMinimalProblems = false,
            FindAllSolutions = true,
            TraceMode = false,
            AutoCheck = false,
            SeverityLevel = int.MaxValue,
            MaxSolutions = 2
        };
        configuration.DisplayLanguage = CultureInfo.GetCultureInfo("de-DE").Name;
        return configuration;
    }

    private static string[] LoadPuzzles(string filename)
    {
        Assert.IsTrue(File.Exists(filename), $"Die Datei \"{filename}\" wurde nicht gefunden.");

        string[] puzzles = File.ReadLines(filename)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .Take(SudokuBatchSize)
            .ToArray();

        Assert.AreEqual(SudokuBatchSize, puzzles.Length, $"Es konnten nur {puzzles.Length} von {SudokuBatchSize} Sudokus geladen werden.");

        foreach(string puzzle in puzzles)
            Assert.AreEqual(WinFormsSettings.TotalCellCount, puzzle.Length, "Eine Sudoku-Zeile besitzt nicht exakt 81 Zeichen.");

        return puzzles;
    }

    private static List<TestResult> LoadOrCreateReferenceSolutions(int expectedCount, string filename, out bool referenceExists)
    {
        referenceExists = File.Exists(filename);
        if(!referenceExists)
            return new List<TestResult>(expectedCount);

        var testResults = new List<TestResult>(expectedCount);
        string fileContent = File.ReadAllText(filename);
        try
        {
            testResults = JsonSerializer.Deserialize<List<TestResult>>(fileContent);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Fehler beim Lesen der Datei \"{filename}\": {ex.Message}");
        }

        Assert.AreEqual(expectedCount, testResults.Count, $"Die Datei \"{filename}\" enthält nicht genügend Referenzlösungen.");
        return testResults;
    }

    private BaseProblem CreateProblem(string serializedPuzzle)
    {
        BaseProblem problem = settings.GenerateXSudoku? (BaseProblem)new XSudokuProblem(settings) : new SudokuProblem(settings);
        LoadSerializedPuzzle(problem, serializedPuzzle);
        return problem;
    }

    private static void LoadSerializedPuzzle(BaseProblem problem, string serializedPuzzle)
    {
        Assert.AreEqual(WinFormsSettings.TotalCellCount, serializedPuzzle.Length, "Ungültige Sudoku-Länge.");

        problem.ResetSolutions();

        for(int index = 0; index < serializedPuzzle.Length; index++)
        {
            byte encodedValue = (byte)(serializedPuzzle[index] - '0');
            bool readOnly = encodedValue > ReadOnlyEncodingOffset;
            if(encodedValue >= ReadOnlyEncodingOffset)
                encodedValue -= ReadOnlyEncodingOffset;

            byte cellValue = encodedValue;
            int row = index / WinFormsSettings.SudokuSize;
            int col = index % WinFormsSettings.SudokuSize;

            problem.SetValue(row, col, cellValue, cellValue != Values.Undefined);
            problem.SetReadOnly(row, col, readOnly && cellValue != Values.Undefined);
        }
    }

    private static async Task FindSolution(BaseProblem problem, int numberOfSolutions)
    {
        problem.ResetMatrix();
        problem.ResetSolutions();
        await problem.FindSolutions(numberOfSolutions, CancellationToken.None);
        if(!problem.SolverTask.IsCompleted)
            await problem.SolverTask;

        Assert.IsTrue(problem.NumberOfSolutions > 0, "Es wurde keine Lösung gefunden.");
        Assert.IsTrue(problem.NumberOfSolutions == 1, "Keine eindeutige Lösung gefunden.");
    }
    private static string SerializeSolution(BaseProblem problem)
    {
        var builder = new StringBuilder(WinFormsSettings.TotalCellCount);

        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                byte value = problem.Solutions[0].GetValue(row, col);
                Assert.AreNotEqual(Values.Undefined, value, "Die berechnete Lösung ist unvollständig.");
                builder.Append((char)('0' + value));
            }

        return builder.ToString();
    }
    private static string SerializeProblem(BaseProblem problem)
    {
        var builder = new StringBuilder(WinFormsSettings.TotalCellCount);

        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                byte value = problem.GetValue(row, col);
                builder.Append((char)('0' + value));
            }

        return builder.ToString();
    }
}