using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sudoku.Sudoku.Tests;

[TestClass]
public sealed class NormalSudokuBatchTests
{
    private const int SudokuBatchSize = 1000;
    private const byte ReadOnlyEncodingOffset = 64;
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory!, "..", "..", "..", ".."));
    private static readonly string PuzzlesFilePath = Path.Combine(RepoRoot, "WebClient", "NormalSudokus.sudoku");
    private static readonly string SolutionsFilePath = Path.Combine(RepoRoot, "WebClient", "NormalSudokus.solutions");

    private readonly WinFormsSettings settings = CreateSettings();

    [TestMethod]
    public async Task SolveAndMinimizeNormalSudokusAsync()
    {
        string[] puzzles = LoadPuzzles();
        string[] referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, out bool referenceExists);

        for(int index = 0; index < puzzles.Length; index++)
        {
            string serializedPuzzle = puzzles[index];

            var originalProblem = CreateProblem(serializedPuzzle);
            string computedSolution = await SolveProblemAsync(originalProblem, CancellationToken.None);

            if(referenceExists)
            {
                Assert.AreEqual(referenceSolutions[index], computedSolution, $"Lösung #{index + 1} weicht von der Referenz in \"{SolutionsFilePath}\" ab.");
            }
            else
            {
                referenceSolutions[index] = computedSolution;
            }

            var minimizationSource = CreateProblem(serializedPuzzle);
            var minimizedCandidate = await minimizationSource.Minimize(int.MaxValue, CancellationToken.None);
            Assert.IsNotNull(minimizedCandidate, $"Minimalproblem für Sudoku #{index + 1} konnte nicht ermittelt werden.");

            string minimizedSolution = await SolveProblemAsync(minimizedCandidate!, CancellationToken.None);
            Assert.AreEqual(computedSolution, minimizedSolution, $"Minimierte Lösung für Sudoku #{index + 1} stimmt nicht mit der Original-Lösung überein.");
        }

        if(!referenceExists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SolutionsFilePath) ?? RepoRoot);
            File.WriteAllLines(SolutionsFilePath, referenceSolutions);
        }
    }

    private static WinFormsSettings CreateSettings()
    {
        var configuration = new WinFormsSettings
        {
            GenerateNormalSudoku = true,
            GenerateXSudoku = false,
            UsePrecalculatedProblems = false,
            GenerateMinimalProblems = true,
            FindAllSolutions = false,
            TraceMode = false,
            AutoCheck = false
        };

        if(configuration.MaxSolutions < 2)
            configuration.MaxSolutions = 2;

        configuration.DisplayLanguage = CultureInfo.GetCultureInfo("de-DE").Name;
        return configuration;
    }

    private static string[] LoadPuzzles()
    {
        Assert.IsTrue(File.Exists(PuzzlesFilePath), $"Die Datei \"{PuzzlesFilePath}\" wurde nicht gefunden.");

        string[] puzzles = File.ReadLines(PuzzlesFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .Take(SudokuBatchSize)
            .ToArray();

        Assert.AreEqual(SudokuBatchSize, puzzles.Length, $"Es konnten nur {puzzles.Length} von {SudokuBatchSize} Sudokus geladen werden.");

        foreach(string puzzle in puzzles)
            Assert.AreEqual(WinFormsSettings.TotalCellCount, puzzle.Length, "Eine Sudoku-Zeile besitzt nicht exakt 81 Zeichen.");

        return puzzles;
    }

    private static string[] LoadOrCreateReferenceSolutions(int expectedCount, out bool referenceExists)
    {
        referenceExists = File.Exists(SolutionsFilePath);
        if(!referenceExists)
            return new string[expectedCount];

        string[] reference = File.ReadLines(SolutionsFilePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .Take(expectedCount)
            .ToArray();

        Assert.AreEqual(expectedCount, reference.Length, $"Die Datei \"{SolutionsFilePath}\" enthält nicht genügend Referenzlösungen.");
        return reference;
    }

    private SudokuProblem CreateProblem(string serializedPuzzle)
    {
        var problem = new SudokuProblem(settings);
        LoadSerializedPuzzle(problem, serializedPuzzle);
        return problem;
    }

    private static void LoadSerializedPuzzle(BaseProblem problem, string serializedPuzzle)
    {
        Assert.AreEqual(WinFormsSettings.TotalCellCount, serializedPuzzle.Length, "Ungültige Sudoku-Länge.");

        problem.ResetSolutions();
        problem.Matrix.Init();
        problem.Matrix.SetPredefinedValues = false;

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

        problem.Matrix.SetPredefinedValues = true;
    }

    private static async Task<string> SolveProblemAsync(BaseProblem problem, CancellationToken token)
    {
        problem.ResetMatrix();
        problem.ResetSolutions();
        problem.FindSolutions(1, token);

        if(problem.SolverTask != null)
            await problem.SolverTask;

        Assert.IsTrue(problem.ProblemSolved, "Das Sudoku konnte nicht gelöst werden.");
        Assert.IsTrue(problem.NumberOfSolutions > 0, "Es wurde keine Lösung gefunden.");

        return SerializeSolution(problem);
    }

    private static string SerializeSolution(BaseProblem problem)
    {
        var builder = new StringBuilder(WinFormsSettings.TotalCellCount);

        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                byte value = problem.GetValue(row, col);
                Assert.AreNotEqual(Values.Undefined, value, "Die berechnete Lösung ist unvollständig.");
                builder.Append((char)('0' + value));
            }

        return builder.ToString();
    }
}