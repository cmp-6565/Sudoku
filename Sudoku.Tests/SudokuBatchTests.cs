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

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

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
    private static readonly string SolutionsFilePath = Path.Combine(RepoRoot, "WebClient", "AllSudokus.solutions");
    private static readonly string XSudokusFilePath = Path.Combine(RepoRoot, "WebClient", "XSudokus.sudoku");
    Random rand = new Random(/* unchecked((int)DateTime.Now.Ticks)*/ 1);

    private record struct TestResult(string Puzzle, string MinimalProblem, int Diff, string Solution, double TotalRuntime, double GreedyRuntime, double CandidateRuntime, Boolean XSudoku, BaseProblem.AlgorithmParameters Parameters);

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
        List<TestResult> referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, SolutionsFilePath);
        Title(puzzles.Length, referenceSolutions.Count() != 0);
        await SolveAndMinimizeSudokus(puzzles, referenceSolutions, SolutionsFilePath);
    }

    [TestMethod]
    public async Task SolveAndMinimizeXSudokus()
    {
        settings = CreateSettings(true);
        string[] puzzles = LoadPuzzles(XSudokusFilePath);
        List<TestResult> referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, SolutionsFilePath);
        Title(puzzles.Length, referenceSolutions.Count() != 0);
        await SolveAndMinimizeSudokus(puzzles, referenceSolutions, SolutionsFilePath);
    }
    [TestMethod]
    public async Task SolveAndMinimizeLongRunningNormalSudokus()
    {
        settings = CreateSettings(false);
        string[] puzzles = {
            "0rtu00000000t0w0vs00000q00t000vwus0qxq0r0y0tu00uq0xwry000s0vt0xy00000q000s0yq0000",
            "0000y00v0t0us000qx0w00vx0u000yx0s00wrq00tvxy0000w000rvs00r000w00rq0s0t0y0t0y0000u",
            "uvr000x0t0w000qvy00t0x0r00000w0u000000t00s00xru0y0v00struv00s000000y00wq0qys00r0u"};
        List<TestResult> referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, SolutionsFilePath);
        Title(puzzles.Length, referenceSolutions[0].Puzzle != null);
        await SolveAndMinimizeSudokus(puzzles, referenceSolutions, SolutionsFilePath);
    }

    [TestMethod]
    public async Task SolveAndMinimizeLongRunningXSudokus()
    {
        settings = CreateSettings(true);
        string[] puzzles = {
            "wx000tv00000u0swxqy000x0u00000y00xt0s0000q0ur00r000q000s00qx0vu0000tvsq00vt00yr0x",
/*            "yx00wr000u000t000q0000x0rw0s000000vu0w0ruv0s0vy0w0000rw00000s0000ytvw000xuqs0yvt0",
            "tq0vw00yrxv00sy0000u00rx00000v000qx0qyt0xr0000000v0rs0y00x0s0t0s0000v0000xwr00u0s",
            "0s000ryq000wq000r0000u0t0v000u000v00vq000wx00y000000w000000s000000r0q00y000000000",
            "x00000v0ww0r00vq00v00qs0ux00y0000w00s0000t0uxu00vw00t00vx0ru00sqr000xy0u00w000x0v",
            "0x00000r0s0q00000x0000tu00q000t0y000uy0000r00vt000s000000y00v00000s000xu0qv0w0000",
            "00swr0vqu000000tys0v0ut00000qrx00u00000s00000utx0vw0s0000t0x0rq0sqywv00000t0s000v",
            "0000000q0000w0000x000us0000xw0t0000q0qvx0s000r000w0uxs000r0000ty0000000v00000qwr0",
            "00y0vw0000s000000yt00r000vq0000000swwqv00x000000000000s000x0y00uy00qs000000yu0000",
            "00000000y00y0xvq000000sy00000000q0rw0q0y00000ysr0000000y00u00s0t00w000000000000qx",
            "000t000s0q00v0000000uy00v000w00xqu00r00u00q0y000s0w00000y000000000q0y0xww0000s000" */};
        List<TestResult> referenceSolutions = LoadOrCreateReferenceSolutions(puzzles.Length, SolutionsFilePath);
        Title(puzzles.Length, referenceSolutions[0].Puzzle != null);
        await SolveAndMinimizeSudokus(puzzles, referenceSolutions, SolutionsFilePath);
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
        TestContext?.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Values",-6} {"Diff.",-6} {"Lösung",-81} {"Total",-10} {"Minimierung",-10}");

        Trace.WriteLine($"Löse ein minimales Problem ({minimalSudoku}) und setze {randomCells} zufällige Werte, minimiere und vergleiche mit der Originalllösung, es werden Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\" betrachtet.");
        Trace.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Values",-6} {"Diff.",-6} {"Lösung",-81} {"Total",-10} {"Minimierung",-10}");

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
            var minimizedProblem = await sudoku.Minimize(settings.SeverityLevel, BaseProblem.MinimizeAlgorithm.Calculate, CancellationToken.None);
            minimize.Stop();
            minimizedProblem.ResetMatrix();

            Assert.IsNotNull(minimizedProblem, $"Minimalproblem konnte nicht ermittelt werden.");
            string minimizedSerialized = SerializeProblem(minimizedProblem);
            Assert.AreEqual(originalCount, CountValues(minimizedSerialized.ToCharArray()), $"Das ermittelte Minimalproblem für Sudoku #{i + 1} ist nicht minimal.");

            if(minimalSudoku != minimizedSerialized) // a minimized problem with the same number of values found which is different to the original one; thus we have check whether the solutions of both are equal
            {
                await FindSolution(minimizedProblem!, settings.FindAllSolutions ? settings.MaxSolutions : 1);
                string minimizedSolution = SerializeSolution(minimizedProblem!);
                Assert.AreEqual(computedSolution, minimizedSolution, $"Minimierte Lösung stimmt nicht mit der Original-Lösung überein.");
            }
            stopwatch.Stop();
            TestContext?.WriteLine($"{i + 1,4:D4} {problem,-81} {minimizedSerialized,-81} {CountValues(problem.ToCharArray())} {CountValues(problem.ToCharArray())- CountValues(minimizedSerialized.ToCharArray()),6:d2} {computedSolution,-81} {stopwatch.Elapsed.TotalSeconds,-10:F2} {minimize.Elapsed.TotalSeconds,-10:F2}");
            Trace.WriteLine($"{i + 1,4:D4} {problem,-81} {minimizedSerialized,-81} {CountValues(problem.ToCharArray())} {CountValues(problem.ToCharArray()) - CountValues(minimizedSerialized.ToCharArray()),6:d2} {computedSolution,-81} {stopwatch.Elapsed.TotalSeconds,-10:F2} {minimize.Elapsed.TotalSeconds,-10:F2}");
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
    private async Task<BaseProblem.AlgorithmParameters[]> GetAlgorithm(string[] puzzles)
    {
        BaseProblem.AlgorithmParameters[] algorithmParameters=new BaseProblem.AlgorithmParameters[puzzles.Length];
        for(int index = 0; index < puzzles.Length; index++)
        {
            var sudoku = CreateProblem(puzzles[index]);
            algorithmParameters[index]=await sudoku.GetAlgorithm(int.MaxValue, CancellationToken.None);
        }
        return algorithmParameters;
    }
    public void GenerationFinished(Object o, string s) {  }

    private void Title(int numberOfProblems, Boolean referenceExists)
    {
        TestContext?.WriteLine($"Löse und minimiere {numberOfProblems} Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\". Referenzlösungen {(referenceExists ? "werden verglichen" : "werden erstellt")}.");
        TestContext?.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Values",6} {"Diff.",6} {"Lösung",-81} {"Sekunden",10} {"Greedy",10} {"Candidate",10} {"Fav. Algo.",-10}");

        Trace.WriteLine($"Löse und minimiere {numberOfProblems} Sudokus mit der Einstellung \"{(settings.GenerateXSudoku ? "X-Sudoku" : "Normal-Sudoku")}\". Referenzlösungen {(referenceExists ? "werden verglichen" : "werden erstellt")}.");
        Trace.WriteLine($"{"Nr.",-4} {"Problem",-81} {"Minimales Problem",-81} {"Values",6} {"Diff.",6} {"Lösung",-81} {"Sekunden",10} {"Greedy",10} {"Candidate",10} {"Fav. Algo.",-10}");
    }

    private async Task<int> SolveAndMinimizeSudokus(string[] puzzles, List<TestResult> referenceSolutions, string solutionFilename)
    {
        BaseProblem.AlgorithmParameters[] algorithmParameters = await GetAlgorithm(puzzles); 
        int referenceIndex = -1;

        for(int index = 0; index < puzzles.Length; index++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string serializedPuzzle = puzzles[index];

            var sudoku = CreateProblem(serializedPuzzle);
            serializedPuzzle = SerializeProblem(sudoku); // remove read-only encoding for display
            int diff = sudoku.nValues;

            await FindSolution(sudoku, settings.FindAllSolutions ? settings.MaxSolutions : 1);
            string computedSolution = SerializeSolution(sudoku);

            Stopwatch greedyRuntime = Stopwatch.StartNew();
            var greedyMinimizedProblem = await sudoku.Minimize(settings.SeverityLevel, BaseProblem.MinimizeAlgorithm.Greedy, CancellationToken.None);
            greedyRuntime.Stop();

            greedyMinimizedProblem.ResetMatrix();

            Stopwatch candidateRuntime = Stopwatch.StartNew();
            var candidateMinimizedProblem = await sudoku.Minimize(settings.SeverityLevel, BaseProblem.MinimizeAlgorithm.Candidate, CancellationToken.None);
            candidateRuntime.Stop();

            candidateMinimizedProblem.ResetMatrix();
            diff -= candidateMinimizedProblem.nValues;

            Assert.IsNotNull(greedyMinimizedProblem, $"Minimalproblem für Sudoku #{index + 1} konnte mit dem Greedy-Algorithmus nicht ermittelt werden.");
            Assert.IsTrue(diff >= 0, $"Das mit dem Greedy-Algorithmus ermittelte Minimalproblem für Sudoku #{index + 1} ist nicht minimal.");
            Assert.IsNotNull(candidateMinimizedProblem, $"Minimalproblem für Sudoku #{index + 1} konnte mit dem Candidate-Algorithmus nicht ermittelt werden.");
            Assert.IsTrue(candidateMinimizedProblem.nValues == greedyMinimizedProblem.nValues, $"Das mit dem Candidate-Algorithmus ermittelte Minimalproblem für Sudoku #{index + 1} ist nicht minimal.");

            string minimizedSerialized = SerializeProblem(candidateMinimizedProblem);

            await FindSolution(candidateMinimizedProblem!, settings.FindAllSolutions? settings.MaxSolutions : 1);
            string minimizedSolution = SerializeSolution(candidateMinimizedProblem!);

            Assert.AreEqual(computedSolution, minimizedSolution, $"Minimierte Lösung für Sudoku #{index + 1} stimmt nicht mit der Original-Lösung überein.");

            stopwatch.Stop();

            TestResult testResult = referenceSolutions.Find(x => x.Puzzle == serializedPuzzle && x.XSudoku == settings.GenerateXSudoku);
            if(testResult.Puzzle != null)
            {
                Assert.AreEqual(testResult.Solution, computedSolution, $"Lösung #{index + 1} weicht von der Referenz ab.");
                Assert.AreEqual(testResult.Diff, diff, $"Diff #{index + 1} weicht von der Referenz in ab.");
            }
            else
            {
                testResult = new TestResult { Puzzle = serializedPuzzle, Solution = computedSolution, MinimalProblem = minimizedSerialized, Diff = diff, TotalRuntime = stopwatch.Elapsed.TotalSeconds, XSudoku=settings.GenerateXSudoku };
                referenceSolutions.Add(testResult);
            }

            referenceIndex=referenceSolutions.FindIndex(x => x.Puzzle == serializedPuzzle && x.XSudoku == settings.GenerateXSudoku);
            referenceSolutions[referenceIndex] = referenceSolutions[referenceIndex] with { CandidateRuntime = candidateRuntime.Elapsed.TotalSeconds, GreedyRuntime = greedyRuntime.Elapsed.TotalSeconds, Parameters = algorithmParameters[index] };

            TestContext?.WriteLine($"{index + 1,4:D4} {serializedPuzzle,-81} {minimizedSerialized,-81} {CountValues(serializedPuzzle.ToCharArray()),6:D2} {diff,6:d2} {minimizedSolution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2} {greedyRuntime.Elapsed.TotalSeconds,10:F2} {candidateRuntime.Elapsed.TotalSeconds,10:F2} {algorithmParameters[index].FavoriteAlgorithm}");
            Trace.WriteLine($"{index + 1,4:D4} {serializedPuzzle,-81} {minimizedSerialized,-81} {CountValues(serializedPuzzle.ToCharArray()),6:D2} {diff,6:d2} {minimizedSolution,-81} {stopwatch.Elapsed.TotalSeconds,10:F2} {greedyRuntime.Elapsed.TotalSeconds,10:F2} {candidateRuntime.Elapsed.TotalSeconds,10:F2} {algorithmParameters[index].FavoriteAlgorithm}");
        }

        if(solutionFilename != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(solutionFilename) ?? RepoRoot);
            File.WriteAllText(solutionFilename, JsonSerializer.Serialize(referenceSolutions));
        }
        return referenceIndex;
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

    private static List<TestResult> LoadOrCreateReferenceSolutions(int expectedCount, string filename)
    {
        if(!File.Exists(filename))
            return new List<TestResult>(expectedCount);

        var testResults = new List<TestResult>();
        string fileContent = File.ReadAllText(filename);
        try
        {
            testResults = JsonSerializer.Deserialize<List<TestResult>>(fileContent);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Fehler beim Lesen der Datei \"{filename}\": {ex.Message}");
        }

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