using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sudoku.Sudoku.Tests;

[TestClass]
public sealed class SudokuProblemTests
{
    [TestMethod]
    public void SudokuTypeIdentifier_ShouldMatchStaticIdentifier()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());

        Assert.AreEqual(SudokuProblem.ProblemIdentifier, problem.SudokuTypeIdentifier);
    }

    [TestMethod]
    public void IsTricky_ShouldRespectNormalThreshold()
    {
        var settings = ProblemTestHelper.CreateSettings();

        var nonTricky = new SudokuProblem(settings);
        ProblemTestHelper.ForceSeverity(nonTricky, settings.UploadLevelNormalSudoku);
        Assert.IsFalse(nonTricky.IsTricky);

        var tricky = new SudokuProblem(settings);
        ProblemTestHelper.ForceSeverity(tricky, settings.UploadLevelNormalSudoku + 1);
        Assert.IsTrue(tricky.IsTricky);
    }

    [TestMethod]
    public void Constructor_ShouldCreateSudokuMatrix()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());

        Assert.IsInstanceOfType(problem.Matrix, typeof(SudokuMatrix));
    }

    [TestMethod]
    public void CreateInstance_ShouldReturnDistinctSudokuProblem()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());
        var clone = ProblemTestHelper.InvokeCreateInstance(problem);

        Assert.IsInstanceOfType(clone, typeof(SudokuProblem));
        Assert.AreNotSame(problem, clone);
    }
}

[TestClass]
public sealed class XSudokuProblemTests
{
    [TestMethod]
    public void SudokuTypeIdentifier_ShouldMatchStaticIdentifier()
    {
        var problem = new XSudokuProblem(ProblemTestHelper.CreateSettings());

        Assert.AreEqual(XSudokuProblem.ProblemIdentifier, problem.SudokuTypeIdentifier);
    }

    [TestMethod]
    public void IsTricky_ShouldRespectXThreshold()
    {
        var settings = ProblemTestHelper.CreateSettings();

        var nonTricky = new XSudokuProblem(settings);
        ProblemTestHelper.ForceSeverity(nonTricky, settings.UploadLevelXSudoku);
        Assert.IsFalse(nonTricky.IsTricky);

        var tricky = new XSudokuProblem(settings);
        ProblemTestHelper.ForceSeverity(tricky, settings.UploadLevelXSudoku + 1);
        Assert.IsTrue(tricky.IsTricky);
    }

    [TestMethod]
    public void Constructor_ShouldCreateXSudokuMatrix()
    {
        var problem = new XSudokuProblem(ProblemTestHelper.CreateSettings());

        Assert.IsInstanceOfType(problem.Matrix, typeof(XSudokuMatrix));
    }

    [TestMethod]
    public void CreateInstance_ShouldReturnDistinctXSudokuProblem()
    {
        var problem = new XSudokuProblem(ProblemTestHelper.CreateSettings());
        var clone = ProblemTestHelper.InvokeCreateInstance(problem);

        Assert.IsInstanceOfType(clone, typeof(XSudokuProblem));
        Assert.AreNotSame(problem, clone);
    }

    [TestMethod]
    public void Resolvable_ShouldReturnFalse_WhenBaseConstraintsFail()
    {
        var problem = new XSudokuProblem(ProblemTestHelper.CreateSettings());
        for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
            problem.SetValue(0, col, 1, true);

        Assert.IsFalse(problem.Resolvable());
    }

    [TestMethod]
    public void Resolvable_ShouldReturnFalse_WhenDiagonalConstraintsFail()
    {
        var problem = new XSudokuProblem(ProblemTestHelper.CreateSettings());
        for(int index = 0; index < WinFormsSettings.SudokuSize - 1; index++)
            problem.SetValue(index, index, (byte)(index + 1), true);

        problem.SetValue(WinFormsSettings.SudokuSize - 1, WinFormsSettings.SudokuSize - 1, 1, true);

        Assert.IsFalse(problem.Resolvable());
    }

    [TestMethod]
    public void Resolvable_ShouldReturnTrue_WhenBaseAndDiagonalConstraintsSucceed()
    {
        var problem = new XSudokuProblem(ProblemTestHelper.CreateSettings());
        for(int index = 0; index < WinFormsSettings.SudokuSize; index++)
            problem.SetValue(index, index, (byte)(index + 1), true);

        Assert.IsTrue(problem.Resolvable());
    }
}

[TestClass]
public sealed class BaseProblemTests
{
    [TestMethod]
    public void ResetSolutions_ShouldClearAllStoredSolutions()
    {
        var settings = ProblemTestHelper.CreateSettings();
        var problem = new SudokuProblem(settings);
        problem.Solutions.Add(new Solution(settings));

        problem.ResetSolutions();

        Assert.AreEqual(0, problem.NumberOfSolutions);
    }

    [TestMethod]
    public void Clone_ShouldReturnDetachedCopyWithSameState()
    {
        var settings = ProblemTestHelper.CreateSettings();
        var problem = new SudokuProblem(settings);
        problem.SetValue(0, 0, 5, true);
        problem.SetValue(0, 1, 4, true);
        problem.Comment = "Sample comment";
        problem.Filename = "baseline.sudoku";
        problem.Dirty = true;
        problem.SolvingTime = TimeSpan.FromSeconds(1);
        problem.GenerationTime = TimeSpan.FromSeconds(2);

        var solution = new Solution(settings);
        solution.SetValue(0, 0, 5, true);
        problem.Solutions.Add(solution);

        var clone = problem.Clone();

        Assert.AreNotSame(problem, clone);
        Assert.AreNotSame(problem.Matrix, clone.Matrix);
        Assert.AreEqual(5, clone.GetValue(0, 0));
        Assert.AreEqual("Sample comment", clone.Comment);
        Assert.AreEqual("baseline.sudoku", clone.Filename);
        Assert.AreEqual(problem.SolvingTime, clone.SolvingTime);
        Assert.AreEqual(problem.GenerationTime, clone.GenerationTime);
        Assert.AreEqual(problem.NumberOfSolutions, clone.NumberOfSolutions);

        try 
        {
            clone.SetValue(0, 0, 3, true); // should fail because the value 3 is not a valid candidate for that cell, but we want to ensure the original problem remains unchanged
        }
        catch { }

        Assert.AreEqual(5, clone.GetValue(0, 0));
        Assert.AreEqual(5, problem.GetValue(0, 0));
    }

    [TestMethod]
    public void CloneMatrix_ShouldCreateIndependentMatrixInstance()
    {
        var settings = ProblemTestHelper.CreateSettings();
        var problem = new SudokuProblem(settings);
        problem.SetValue(0, 0, 7, true);

        var clone = problem.CloneMatrix();

        Assert.AreNotSame(problem.Matrix, clone);
        Assert.AreEqual(7, clone.GetValue(0, 0));

        clone.SetValue(0, 1, 3, true);

        Assert.AreEqual(Values.Undefined, problem.GetValue(0, 1));
        Assert.AreEqual(3, clone.GetValue(0, 1));
    }

    [TestMethod]
    public void SetCandidate_ShouldToggleStateAndUpdateDirtyFlag()
    {
        var settings = ProblemTestHelper.CreateSettings();
        var problem = new SudokuProblem(settings);

        problem.SetCandidate(0, 0, 1, false);

        Assert.IsTrue(problem.GetCandidate(0, 0, 1, false));
        Assert.IsFalse(problem.Dirty);

        problem.SetCandidate(0, 0, 1, true);
        Assert.IsFalse(problem.GetCandidate(0, 0, 1, false));
        Assert.IsTrue(problem.Dirty);
    }

    [TestMethod]
    public void ResetCandidates_ShouldClearAllCandidatesAndSetDirtyFlag()
    {
        var settings = ProblemTestHelper.CreateSettings();
        var problem = new SudokuProblem(settings);
        problem.SetCandidate(0, 0, 1, false);
        problem.Dirty = false;

        problem.ResetCandidates();

        Assert.IsTrue(problem.Dirty);
        Assert.IsFalse(problem.HasCandidates());
    }

    [TestMethod]
    public void SetReadOnly_ShouldChangeReadOnlyState()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());

        problem.SetReadOnly(0, 0, true);
        Assert.IsTrue(problem.IsCellReadOnly(0, 0));

        problem.SetReadOnly(0, 0, false);
        Assert.IsFalse(problem.IsCellReadOnly(0, 0));
    }

    [TestMethod]
    public void SetValue_ShouldUpdateCellAndResetFilename()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings())
        {
            Filename = "before.sudoku"
        };

        problem.SetValue(0, 0, 9);

        Assert.AreEqual(9, problem.GetValue(0, 0));
        Assert.IsTrue(problem.FixedValue(0, 0));
        Assert.AreEqual(string.Empty, problem.Filename);
        Assert.IsTrue(problem.Dirty);
    }

    [TestMethod]
    public void NumDistinctValues_ShouldCountUniqueEntries()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());
        problem.SetValue(0, 0, 1, true);
        problem.SetValue(0, 1, 2, true);
        problem.SetValue(1, 0, 3, true);
        problem.SetValue(1, 1, 4, true);

        Assert.AreEqual(4, problem.NumDistinctValues());
    }

    [TestMethod]
    public void GetNeighbors_ShouldExposeAllRelatedCells()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());

        BaseCell[] neighbors = problem.GetNeighbors(0, 0);

        Assert.AreEqual(20, neighbors.Length);
        Assert.IsTrue(neighbors.All(cell => cell is not null));
        Assert.AreEqual(neighbors.Length, neighbors.Distinct().Count());
        Assert.IsTrue(neighbors.All(cell => cell.Row != 0 || cell.Col != 0));
    }

    [TestMethod]
    public async Task FindSolutions_ShouldProduceSolution_ForCompletedGrid()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());
        LoadSolvedPuzzle(problem);

        await problem.FindSolutions(1, CancellationToken.None);
        await AwaitSolverTask(problem);

        Assert.AreEqual(1, problem.NumberOfSolutions);
        Assert.AreEqual(1, problem.Solutions.Count);
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                Assert.AreEqual(SolvedGrid[row, col], problem.Solutions[0].GetValue(row, col));
    }

    [TestMethod]
    public async Task FindSolutions_ShouldSkipRun_WhenMaximumSolutionsReached()
    {
        var problem = new SudokuProblem(ProblemTestHelper.CreateSettings());
        LoadSolvedPuzzle(problem);

        await problem.FindSolutions(1, CancellationToken.None);
        await AwaitSolverTask(problem);
        var previousTask = problem.SolverTask;

        await problem.FindSolutions(1, CancellationToken.None);

        Assert.AreSame(previousTask, problem.SolverTask);
        Assert.AreEqual(1, problem.NumberOfSolutions);
    }

    private static readonly byte[,] SolvedGrid =
    {
        {5, 3, 4, 6, 7, 8, 9, 1, 2},
        {6, 7, 2, 1, 9, 5, 3, 4, 8},
        {1, 9, 8, 3, 4, 2, 5, 6, 7},
        {8, 5, 9, 7, 6, 1, 4, 2, 3},
        {4, 2, 6, 8, 5, 3, 7, 9, 1},
        {7, 1, 3, 9, 2, 4, 8, 5, 6},
        {9, 6, 1, 5, 3, 7, 2, 8, 4},
        {2, 8, 7, 4, 1, 9, 6, 3, 5},
        {3, 4, 5, 2, 8, 6, 1, 7, 9}
    };

    private static void LoadSolvedPuzzle(BaseProblem problem)
    {
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                problem.SetValue(row, col, SolvedGrid[row, col], true);
    }

    private static async Task AwaitSolverTask(BaseProblem problem)
    {
        if(problem.SolverTask is null) return;
        await problem.SolverTask;
    }
}

internal static class ProblemTestHelper
{
    internal static TestSudokuSettings CreateSettings()
    {
        return new TestSudokuSettings
        {
            UploadLevelNormalSudoku = 20,
            UploadLevelXSudoku = 25
        };
    }

    internal static void ForceSeverity(BaseProblem problem, float severity)
    {
        int requiredValues = problem.Matrix.MinimumValues;
        if(problem.Matrix.nValues < requiredValues)
        {
            SeedFixedValues(problem, requiredValues - problem.Matrix.nValues);
        }

        FieldInfo severityField = typeof(BaseMatrix).GetField("severityLevel", BindingFlags.Instance | BindingFlags.NonPublic)!;
        severityField.SetValue(problem.Matrix, severity);
    }

    internal static BaseProblem InvokeCreateInstance(BaseProblem problem)
    {
        MethodInfo method = problem.GetType().GetMethod("CreateInstance", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (BaseProblem)method.Invoke(problem, null)!;
    }

    private static void SeedFixedValues(BaseProblem problem, int valuesToAdd)
    {
        byte value = 1;
        for(int row = 0; row < WinFormsSettings.SudokuSize && valuesToAdd > 0; row++)
        {
            for(int col = 0; col < WinFormsSettings.SudokuSize && valuesToAdd > 0; col++)
            {
                if(problem.FixedValue(row, col)) continue;

                problem.SetValue(row, col, value, true);
                value = (byte)(value % WinFormsSettings.SudokuSize + 1);
                valuesToAdd--;
            }
        }
    }
}

internal sealed class TestSudokuSettings: ISudokuSettings
{
    public string DisplayLanguage { get; set; } = "de-DE";
    public int BookletSizeNew { get; set; }
    public bool PrintSolution { get; set; }
    public int MaxSolutions { get; set; } = 2;
    public int MinValues { get; set; } = 17;
    public bool AutoSaveBooklet { get; set; }
    public string ProblemDirectory { get; set; } = string.Empty;
    public int Size { get; set; } = 9;
    public bool PrintHints { get; set; }
    public bool ShowHints { get; set; }
    public int HorizontalProblems { get; set; }
    public int HorizontalSolutions { get; set; }
    public bool AutoCheck { get; set; }
    public bool TraceMode { get; set; }
    public bool FindAllSolutions { get; set; }
    public int BookletSizeExisting { get; set; }
    public bool BookletSizeUnlimited { get; set; }
    public int SeverityLevel { get; set; }
    public bool HideWhenMinimized { get; set; }
    public int TraceFrequence { get; set; }
    public bool UseWatchHandHints { get; set; }
    public bool GenerateXSudoku { get; set; }
    public bool GenerateNormalSudoku { get; set; } = true;
    public bool SelectSeverity { get; set; }
    public int XSudokuConstrast { get; set; }
    public string State { get; set; } = string.Empty;
    public bool AutoSaveState { get; set; }
    public bool GenerateMinimalProblems { get; set; }
    public bool MarkNeighbors { get; set; }
    public bool UsePrecalculatedProblems { get; set; }
    public string LastVersion { get; set; } = "1.0.0";
    public bool SudokuOfTheDay { get; set; }
    public bool PrintInternalSeverity { get; set; }
    public bool AutoPause { get; set; }
    public decimal AutoPauseLag { get; set; }
    public int Contrast { get; set; }
    public bool HighlightSameValues { get; set; }
    public float CellWidth { get; set; } = 1f;
    public float SmallCellWidth { get; set; } = 1f;
    public float Intermediate { get; set; } = 10f;
    public string DefaultFileExtension { get; set; } = ".sudoku";
    public string SupportedCultures { get; set; } = "de-DE";
    public int Trivial { get; set; } = 5;
    public float MagnificationFactor { get; set; } = 1f;
    public string FontSizes { get; set; } = string.Empty;
    public string TableFont { get; set; } = "Segoe UI";
    public string PrintFont { get; set; } = "Segoe UI";
    public string FixedFont { get; set; } = "Consolas";
    public string HorizontalProblemsAlternatives { get; set; } = string.Empty;
    public string HorizontalSolutionsAlternatives { get; set; } = string.Empty;
    public string MailAddress { get; set; } = "test@example.com";
    public string HTMLFileExtension { get; set; } = ".html";
    public int NormalSudokuPublicationLimit { get; set; } = 100;
    public int XSudokuPublicationLimit { get; set; } = 100;
    public float Hard { get; set; } = 30f;
    public int UploadLevelNormalSudoku { get; set; } = 20;
    public int UploadLevelXSudoku { get; set; } = 25;
    public int MaxValues { get; set; } = 81;
    public int MaxHints { get; set; } = 81;
    public int MaxProblems { get; set; } = 250;

    public void Save()
    {
    }
}