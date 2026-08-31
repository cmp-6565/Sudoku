#nullable enable
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Sudoku.Core;
using Sudoku.Core.Solving;

namespace Sudoku.Tests;

/// <summary>
/// Tests für die einzelnen ISudokuStrategy-Implementierungen. Jedes Testgrid wurde
/// rechnerisch (nicht nur manuell) so konstruiert, dass genau die zu testende Technik
/// mit einem eindeutigen, überprüfbaren Ergebnis greift.
/// </summary>
[TestClass]
public class SolvingStrategyTests
{
    private static SudokuProblem CreateEmptyProblem() => new SudokuProblem(new FakeSudokuSettings());

    // --- Hidden Single ---
    // Zeile 0: Spalten 0-5 gefüllt (1..6). In Spalte 6/7 ist Kandidat 9 durch eine 9
    // in Spalte 6 bzw. 7 (andere Zeile) blockiert -> nur (0,8) kann in Zeile 0 noch eine 9 sein.
    [TestMethod]
    public void HiddenSingleStrategy_FindsSingleCandidateCellInRow()
    {
        var problem = CreateEmptyProblem();
        problem.Matrix.SetPredefinedValues = false;
        try
        {
            for(byte col = 0; col < 6; col++)
                problem.SetValue(0, col, (byte)(col + 1), true);
            problem.SetValue(3, 6, 9, true);
            problem.SetValue(6, 7, 9, true);

            var strategy = new HiddenSingleStrategy();
            var findings = strategy.FindAll(problem.Matrix);

            Assert.IsTrue(findings.Any(f =>
                f.AffectedCells.Count == 1 &&
                f.AffectedCells[0].Row == 0 && f.AffectedCells[0].Col == 8),
                "Erwarteter Hidden Single bei (0,8) wurde nicht gefunden.");
        }
        finally
        {
            problem.Matrix.SetPredefinedValues = true;
        }
    }

    // --- Naked Pair ---
    // Gleiches Grid wie oben: (0,6) und (0,7) haben beide exakt die Kandidaten {7,8}.
    // (0,8) enthält ebenfalls 7 und 8 -> muss als betroffene Zelle gefunden werden.
    [TestMethod]
    public void NakedPairStrategy_FindsPairAndEliminatesFromRemainingCell()
    {
        var problem = CreateEmptyProblem();
        problem.Matrix.SetPredefinedValues = false;
        try
        {
            for(byte col = 0; col < 6; col++)
                problem.SetValue(0, col, (byte)(col + 1), true);
            problem.SetValue(3, 6, 9, true);
            problem.SetValue(6, 7, 9, true);

            var strategy = new NakedPairStrategy();
            var findings = strategy.FindAll(problem.Matrix);

            var rowFinding = findings.FirstOrDefault(f =>
                f.KeyCells.Count == 2 &&
                f.KeyCells.Any(c => c.Row == 0 && c.Col == 6) &&
                f.KeyCells.Any(c => c.Row == 0 && c.Col == 7));

            Assert.IsNotNull(rowFinding, "Erwartetes Naked Pair (0,6)/(0,7) wurde nicht gefunden.");
            Assert.IsTrue(rowFinding!.AffectedCells.Any(c => c.Row == 0 && c.Col == 8),
                "(0,8) hätte als betroffene Zelle erkannt werden müssen.");
        }
        finally
        {
            problem.Matrix.SetPredefinedValues = true;
        }
    }

    // --- Pointing Pair ---
    // Block oben-links: Zeile 0 und Zeile 2 sind gefüllt, nur Zeile 1 (Spalten 0-2) ist leer.
    // Kandidat 5 kommt im Block daher nur in Zeile 1 vor -> muss außerhalb des Blocks
    // in Zeile 1 (z.B. bei (1,5)) eliminierbar sein.
    [TestMethod]
    public void PointingPairStrategy_FindsCandidateConfinedToSingleRowInBox()
    {
        var problem = CreateEmptyProblem();
        problem.Matrix.SetPredefinedValues = false;
        try
        {
            problem.SetValue(0, 0, 1, true);
            problem.SetValue(0, 1, 2, true);
            problem.SetValue(0, 2, 3, true);
            problem.SetValue(2, 0, 4, true);
            problem.SetValue(2, 1, 6, true);
            problem.SetValue(2, 2, 7, true);

            var strategy = new PointingPairStrategy();
            var findings = strategy.FindAll(problem.Matrix);

            var finding = findings.FirstOrDefault(f =>
                f.KeyCells.All(c => c.Row == 1 && c.Col <= 2) &&
                f.AffectedCells.Any(c => c.Row == 1 && c.Col == 5));

            Assert.IsNotNull(finding, "Erwartetes Pointing Pair in Zeile 1 (Block oben-links) wurde nicht gefunden.");
        }
        finally
        {
            problem.Matrix.SetPredefinedValues = true;
        }
    }

    // --- X-Wing ---
    // Zeile 0 und Zeile 3 sind bis auf Spalte 2 und 6 vollständig gefüllt (mit Werten != 4).
    // Kandidat 4 bildet dadurch ein X-Wing über die Spalten 2/6 -> muss z.B. bei (5,2)
    // eliminierbar sein.
    [TestMethod]
    public void XWingStrategy_FindsRectanglePatternAcrossTwoRows()
    {
        var problem = CreateEmptyProblem();
        try
        {
            problem.Matrix.SetPredefinedValues = false;
            SetRow(problem, row: 0, freeColumns: new[] { 2, 6 }, fillerValues: new byte[] { 1, 2, 3, 5, 6, 7, 8 });
            SetRow(problem, row: 3, freeColumns: new[] { 2, 6 }, fillerValues: new byte[] { 2, 3, 5, 6, 7, 8, 1 });

            var strategy = new XWingStrategy();
            var findings = strategy.FindAll(problem.Matrix);

            var finding = findings.FirstOrDefault(f =>
                f.KeyCells.Count == 4 &&
                f.KeyCells.Any(c => c.Row == 0 && c.Col == 2) &&
                f.KeyCells.Any(c => c.Row == 0 && c.Col == 6) &&
                f.KeyCells.Any(c => c.Row == 3 && c.Col == 2) &&
                f.KeyCells.Any(c => c.Row == 3 && c.Col == 6));

            Assert.IsNotNull(finding, "Erwartetes X-Wing-Muster über Zeile 0/3, Spalte 2/6 wurde nicht gefunden.");
            Assert.IsTrue(finding!.AffectedCells.Any(c => c.Row == 5 && c.Col == 2),
                "(5,2) hätte als betroffene Zelle erkannt werden müssen.");
        }
        finally
        {
            problem.Matrix.SetPredefinedValues = true;
        }
    }

    // --- Swordfish ---
    // Zeilen 0, 3 und 6 sind bis auf Spalte 2, 5, 8 gefüllt (mit Werten != 4).
    // Kandidat 4 bildet ein Swordfish über die Spalten 2/5/8 -> muss z.B. bei (4,5)
    // eliminierbar sein.
    [TestMethod]
    public void SwordfishStrategy_FindsRectanglePatternAcrossThreeRows()
    {
        var problem = CreateEmptyProblem();
        problem.Matrix.SetPredefinedValues = false;
        try
        {
            SetRow(problem, row: 0, freeColumns: new[] { 2, 5, 8 }, fillerValues: new byte[] { 1, 2, 3, 5, 6, 7 });
            SetRow(problem, row: 3, freeColumns: new[] { 2, 5, 8 }, fillerValues: new byte[] { 2, 3, 5, 6, 7, 1 });
            SetRow(problem, row: 6, freeColumns: new[] { 2, 5, 8 }, fillerValues: new byte[] { 3, 5, 6, 7, 1, 2 });

            var strategy = new SwordfishStrategy();
            var findings = strategy.FindAll(problem.Matrix);

            var finding = findings.FirstOrDefault(f =>
                f.KeyCells.Count == 9 &&
                new[] { 0, 3, 6 }.All(r => f.KeyCells.Any(c => c.Row == r)));

            Assert.IsNotNull(finding, "Erwartetes Swordfish-Muster über Zeile 0/3/6 wurde nicht gefunden.");
            Assert.IsTrue(finding!.AffectedCells.Any(c => c.Row == 4 && c.Col == 5),
                "(4,5) hätte als betroffene Zelle erkannt werden müssen.");
        }
        finally
        {
            problem.Matrix.SetPredefinedValues = true;
        }
    }

    /// <summary>
    /// Füllt eine Zeile mit den angegebenen Fillern in alle Spalten außer den "freien"
    /// Spalten, in denen absichtlich ein gemeinsamer Kandidat offenbleiben soll.
    /// </summary>
    private static void SetRow(SudokuProblem problem, int row, int[] freeColumns, byte[] fillerValues)
    {
        int fillerIndex = 0;
        for(int col = 0; col < SudokuGrid.SudokuSize; col++)
        {
            if(freeColumns.Contains(col)) continue;
            problem.SetValue(row, col, fillerValues[fillerIndex++], true);
        }
    }
}