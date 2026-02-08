using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sudoku.Sudoku.Tests;

[TestClass]
public sealed class SudokuBatchTests
{
    private const int SudokuBatchSize = 20;
    private const byte ReadOnlyEncodingOffset = 64;
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory!, "..", "..", "..", ".."));
    private static readonly string NormalSudokusFilePath = Path.Combine(RepoRoot, "WebClient", "NormalSudokus.sudoku");
    private static readonly string NormalSudokusSolutionsFilePath = Path.Combine(RepoRoot, "WebClient", "NormalSudokus.solutions");
    private static readonly string XSudokusFilePath = Path.Combine(RepoRoot, "WebClient", "XSudokus.sudoku");
    private static readonly string XSudokusSolutionsFilePath = Path.Combine(RepoRoot, "WebClient", "XSudokus.solutions");

    private WinFormsSettings settings;
    public TestContext? TestContext { get; set; }

    [TestMethod]
    public async Task SolveAndMinimizeNormalSudokus()
    {
        settings = CreateSettings(false);
        string[] puzzles = LoadPuzzles(NormalSudokusFilePath);
        string[] referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, NormalSudokusSolutionsFilePath, out bool referenceExists);
        await SolveAndMinimizeSudokus(puzzles, referenceSolutions, referenceExists, NormalSudokusSolutionsFilePath);
    }

    [TestMethod]
    public async Task SolveAndMinimizeXSudokus()
    {
        settings = CreateSettings(true);
        string[] puzzles = LoadPuzzles(XSudokusFilePath);
        string[] referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, XSudokusSolutionsFilePath, out bool referenceExists);
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

    private async Task GenerateAndSolveSudokus()
    { 
        TestContext?.WriteLine($"Generiere und löse {SudokuBatchSize} Sudokus mit der Einstellung \"{(settings.GenerateXSudoku? "X-Sudoku": "Normal-Sudoku")}\".");
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
            TestContext?.WriteLine($"{index+1,4:D4} {problem, -81} {solution, -81} {stopwatch.Elapsed.TotalSeconds,10:F2}");
        }
    }
    public void GenerationFinished(Object o, string s) {  }

    private async Task SolveAndMinimizeSudokus(string[] puzzles, string[] referenceSolutions, bool referenceExists, string solutionFilename)
    {
        TestContext?.WriteLine($"Löse und minimiere {puzzles.Length} Sudokus mit der Einstellung \"{(settings.GenerateXSudoku? "X-Sudoku": "Normal-Sudoku")}\". Referenzlösungen {(referenceExists? "werden verglichen": "werden erstellt")}.");
        TestContext?.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Lösung",-81} {"Sekunden",10}");
        for(int index = 0; index < puzzles.Length; index++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string serializedPuzzle = puzzles[index];

            var sudoku = CreateProblem(serializedPuzzle);
            serializedPuzzle = SerializeProblem(sudoku); // remove read-only encoding for display

            var minimizedProblem = await sudoku.Minimize(settings.SeverityLevel, CancellationToken.None);
            minimizedProblem.ResetMatrix();
            Assert.IsNotNull(minimizedProblem, $"Minimalproblem für Sudoku #{index + 1} konnte nicht ermittelt werden.");
            Assert.IsTrue(minimizedProblem.nValues <= sudoku.nValues, $"Das ermittelte Minimalproblem für Sudoku #{index + 1} ist nicht minimal.");
            string minimizedSerialized = SerializeProblem(minimizedProblem);

            await FindSolution(sudoku, settings.FindAllSolutions ? settings.MaxSolutions : 1);
            string computedSolution = SerializeSolution(sudoku);

            await FindSolution(minimizedProblem!, settings.FindAllSolutions? settings.MaxSolutions : 1);
            string minimizedSolution = SerializeSolution(minimizedProblem!);
            Assert.AreEqual(computedSolution, minimizedSolution, $"Minimierte Lösung für Sudoku #{index + 1} stimmt nicht mit der Original-Lösung überein.");

            stopwatch.Stop();

            if(referenceExists)
            {
                Assert.AreEqual(referenceSolutions[index], computedSolution, $"Lösung #{index + 1} weicht von der Referenz in \"{solutionFilename}\" ab.");
            }
            else
            {
                referenceSolutions[index] = computedSolution;
            }

            TestContext?.WriteLine($"{index + 1,4:D4} {serializedPuzzle,-81} {minimizedSerialized,-81} {minimizedSolution,-81}{stopwatch.Elapsed.TotalSeconds,10:F2}");
        }

        if(!referenceExists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(solutionFilename) ?? RepoRoot);
            File.WriteAllLines(solutionFilename, referenceSolutions);
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
            SeverityLevel = 15,
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

    private static string[] LoadOrCreateReferenceSolutions(int expectedCount, string filename, out bool referenceExists)
    {
        referenceExists = File.Exists(filename);
        if(!referenceExists)
            return new string[expectedCount];

        string[] reference = File.ReadLines(filename)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .Take(expectedCount)
            .ToArray();

        Assert.AreEqual(expectedCount, reference.Length, $"Die Datei \"{filename}\" enthält nicht genügend Referenzlösungen.");
        return reference;
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
        problem.FindSolutions(numberOfSolutions, CancellationToken.None);

        if(problem.SolverTask != null) await problem.SolverTask;

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