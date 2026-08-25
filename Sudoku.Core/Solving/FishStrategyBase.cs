// Sudoku.Core/Solving/FishStrategyBase.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Core.Solving;

/// <summary>
/// Gemeinsame Basis für X-Wing (n=2) und Swordfish (n=3): ein Kandidat kommt in n Zeilen jeweils
/// nur in denselben n Spalten vor (oder umgekehrt) – dann kann er in diesen Spalten außerhalb
/// der n Zeilen eliminiert werden.
/// </summary>
public abstract class FishStrategyBase: ISolvingStrategy
{
    private readonly int size;
    protected FishStrategyBase(int size) => this.size = size;

    public abstract string Name { get; }
    public abstract int Difficulty { get; }

    public IReadOnlyList<StrategyFinding> FindAll(BaseMatrix matrix)
    {
        var findings = new List<StrategyFinding>();
        for(int value = 1; value <= SudokuGrid.SudokuSize; value++)
        {
            FindOrientation(matrix.Rows, matrix.Cols, value, rowsAreBase: true, findings);
            FindOrientation(matrix.Cols, matrix.Rows, value, rowsAreBase: false, findings);
        }
        return findings;
    }

    private void FindOrientation(BaseCell[][] baseLines, BaseCell[][] coverLines, int value, bool rowsAreBase, List<StrategyFinding> findings)
    {
        // Zeilen (bzw. Spalten), in denen der Kandidat 2..size mal vorkommt
        var candidateLines = new List<(int lineIndex, int[] crossIndices)>();

        for(int i = 0; i < SudokuGrid.SudokuSize; i++)
        {
            int[] crossIndices = baseLines[i]
                .Where(c => c.CellValue == Values.Undefined && c.Enabled(value))
                .Select(c => rowsAreBase ? c.Col : c.Row)
                .ToArray();

            if(crossIndices.Length >= 2 && crossIndices.Length <= size)
                candidateLines.Add((i, crossIndices));
        }

        if(candidateLines.Count < size) return;

        foreach(var combo in Combinations(candidateLines, size))
        {
            var unionCross = combo.SelectMany(l => l.crossIndices).Distinct().ToArray();
            if(unionCross.Length != size) continue;

            var keyCells = combo
                .SelectMany(l => baseLines[l.lineIndex].Where(c => c.CellValue == Values.Undefined && c.Enabled(value)))
                .ToArray();

            var affected = unionCross
                .SelectMany(crossIndex => coverLines[crossIndex])
                .Where(c => c.CellValue == Values.Undefined && c.Enabled(value) && !keyCells.Contains(c))
                .Distinct()
                .ToArray();

            if(affected.Length == 0) continue;

            findings.Add(new StrategyFinding(
                Name, keyCells, affected,
                $"Kandidat {value} bildet ein {Name}-Muster über {size} {(rowsAreBase ? "Zeilen" : "Spalten")} – kann in den zugehörigen {(rowsAreBase ? "Spalten" : "Zeilen")} sonst entfernt werden."));
        }
    }

    private static IEnumerable<T[]> Combinations<T>(List<T> items, int size)
    {
        if(size == 0) { yield return System.Array.Empty<T>(); yield break; }
        for(int i = 0; i <= items.Count - size; i++)
            foreach(var tail in Combinations(items.Skip(i + 1).ToList(), size - 1))
                yield return new[] { items[i] }.Concat(tail).ToArray();
    }
}