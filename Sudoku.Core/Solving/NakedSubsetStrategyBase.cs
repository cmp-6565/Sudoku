// Sudoku.Core/Solving/NakedSubsetStrategyBase.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Core.Solving;

/// <summary>
/// Common base class for naked pair and naked triple solving strategies.
/// If n cells in a unit jointly contain exactly n candidate values, those values can be eliminated
/// from the remaining cells in the same unit.
/// </summary>
public abstract class NakedSubsetStrategyBase: ISolvingStrategy
{
    private readonly int subsetSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="NakedSubsetStrategyBase"/> class.
    /// </summary>
    /// <param name="subsetSize">The subset size to detect, such as 2 for a naked pair or 3 for a naked triple.</param>
    protected NakedSubsetStrategyBase(int subsetSize) => this.subsetSize = subsetSize;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract int Difficulty { get; }

    /// <summary>
    /// Searches all units for naked subset patterns.
    /// </summary>
    /// <param name="matrix">The matrix to analyze.</param>
    /// <returns>A list of findings with all identified naked subsets.</returns>
    public IReadOnlyList<StrategyFinding> FindAll(BaseMatrix matrix)
    {
        var findings = new List<StrategyFinding>();
        for(int i = 0; i < SudokuGrid.SudokuSize; i++)
        {
            FindInUnit(matrix.Rows[i], findings);
            FindInUnit(matrix.Cols[i], findings);
            FindInUnit(matrix.Rectangles[i], findings);
        }
        return findings;
    }

    /// <summary>
    /// Checks a single unit for a naked subset pattern.
    /// </summary>
    /// <param name="unit">The unit to inspect.</param>
    /// <param name="findings">The collection of findings to append to.</param>
    private void FindInUnit(BaseCell[] unit, List<StrategyFinding> findings)
    {
        var candidates = unit
            .Where(c => c.CellValue == Values.Undefined && c.nPossibleValues >= 2 && c.nPossibleValues <= subsetSize)
            .ToArray();

        if(candidates.Length < subsetSize) return;

        foreach(var combo in Combinations(candidates, subsetSize))
        {
            var unionOfValues = new HashSet<int>();
            for(int value = 1; value <= SudokuGrid.SudokuSize; value++)
                if(combo.Any(cell => cell.Enabled(value)))
                    unionOfValues.Add(value);

            if(unionOfValues.Count != subsetSize) continue;

            var affected = unit
                .Where(c => c.CellValue == Values.Undefined && !combo.Contains(c) && unionOfValues.Any(c.Enabled))
                .ToArray();

            if(affected.Length == 0) continue;

            findings.Add(new StrategyFinding(
                Name,
                KeyCells: combo,
                AffectedCells: affected,
                Description: $"The cells {CellList(combo)} share exactly the candidates {string.Join(",", unionOfValues.OrderBy(v => v))}; these candidates can be removed from the remaining cells in the unit."));
        }
    }

    /// <summary>
    /// Formats a set of cells as a readable coordinate list.
    /// </summary>
    /// <param name="cells">The cells to format.</param>
    /// <returns>A formatted list of coordinates in row/column format.</returns>
    private static string CellList(IEnumerable<BaseCell> cells) =>
        string.Join(", ", cells.Select(c => $"({c.Row + 1},{c.Col + 1})"));

    /// <summary>
    /// Enumerates all combinations of the specified size from the given cell collection.
    /// </summary>
    /// <param name="items">The items to combine.</param>
    /// <param name="size">The subset size.</param>
    /// <returns>All combinations of the specified size.</returns>
    private static IEnumerable<BaseCell[]> Combinations(BaseCell[] items, int size)
    {
        if(size == 0) { yield return System.Array.Empty<BaseCell>(); yield break; }
        for(int i = 0; i <= items.Length - size; i++)
            foreach(var tail in Combinations(items[(i + 1)..], size - 1))
                yield return new[] { items[i] }.Concat(tail).ToArray();
    }
}