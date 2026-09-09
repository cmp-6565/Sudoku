// Sudoku.Core/Solving/FishStrategyBase.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Core.Solving;

/// <summary>
/// Base class for fish-based solving techniques such as X-Wing and Swordfish.
/// A fish pattern occurs when a candidate value appears in a fixed number of rows and is restricted to the same
/// number of columns, or vice versa. If the pattern is complete, the candidate can be eliminated from the
/// corresponding crossing lines outside the identified fish.
/// </summary>
public abstract class FishStrategyBase: ISolvingStrategy
{
    private readonly int size;

    /// <summary>
    /// Initializes a new instance of the <see cref="FishStrategyBase"/> class.
    /// </summary>
    /// <param name="size">The fish size to detect, for example 2 for X-Wing or 3 for Swordfish.</param>
    protected FishStrategyBase(int size) => this.size = size;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract int Difficulty { get; }

    /// <summary>
    /// Searches the matrix for fish patterns for every candidate value.
    /// </summary>
    /// <param name="matrix">The Sudoku matrix to analyze.</param>
    /// <returns>All fish findings discovered in the current position.</returns>
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

    /// <summary>
    /// Checks one board orientation for a fish pattern involving a specific candidate value.
    /// </summary>
    /// <param name="baseLines">The primary lines used for the pattern, such as rows or columns.</param>
    /// <param name="coverLines">The crossing lines that must be validated against the pattern.</param>
    /// <param name="value">The candidate value to analyze.</param>
    /// <param name="rowsAreBase">True when the primary axis is rows; otherwise it is columns.</param>
    /// <param name="findings">The collection of findings to append to.</param>
    private void FindOrientation(BaseCell[][] baseLines, BaseCell[][] coverLines, int value, bool rowsAreBase, List<StrategyFinding> findings)
    {
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
                Name,
                keyCells,
                affected,
                StrategyFindingResourceProvider.GetFishDescription(
                    value,
                    Name,
                    size,
                    rowsAreBase ? "rows" : "columns",
                    rowsAreBase ? "columns" : "rows")));
        }
    }

    /// <summary>
    /// Enumerates all combinations of the given size from the supplied list.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The source collection to combine.</param>
    /// <param name="size">The size of each combination.</param>
    /// <returns>All combinations of the requested size.</returns>
    private static IEnumerable<T[]> Combinations<T>(List<T> items, int size)
    {
        if(size == 0) { yield return System.Array.Empty<T>(); yield break; }
        for(int i = 0; i <= items.Count - size; i++)
            foreach(var tail in Combinations(items.Skip(i + 1).ToList(), size - 1))
                yield return new[] { items[i] }.Concat(tail).ToArray();
    }
}