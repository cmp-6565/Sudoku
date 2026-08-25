// Sudoku.Core/Solving/PointingPairStrategy.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Core.Solving;

/// <summary>
/// Detects pointing-pair patterns where a candidate value appears in only one row or column within a block.
/// </summary>
public sealed class PointingPairStrategy: ISolvingStrategy
{
    /// <inheritdoc />
    public string Name => "Pointing Pair";

    /// <inheritdoc />
    public int Difficulty => 4;

    /// <summary>
    /// Searches every block for candidate values confined to a single row or column.
    /// </summary>
    /// <param name="matrix">The Sudoku matrix to analyze.</param>
    /// <returns>All pointing-pair findings found in the matrix.</returns>
    public IReadOnlyList<StrategyFinding> FindAll(BaseMatrix matrix)
    {
        var findings = new List<StrategyFinding>();

        for(int b = 0; b < SudokuGrid.SudokuSize; b++)
        {
            BaseCell[] block = matrix.Rectangles[b];

            for(int value = 1; value <= SudokuGrid.SudokuSize; value++)
            {
                var cellsWithCandidate = block
                    .Where(c => c.CellValue == Values.Undefined && c.Enabled(value))
                    .ToArray();

                if(cellsWithCandidate.Length < 2) continue;

                if(cellsWithCandidate.Select(c => c.Row).Distinct().Count() == 1)
                    TryAdd(matrix.Rows[cellsWithCandidate[0].Row], cellsWithCandidate, value, findings);

                if(cellsWithCandidate.Select(c => c.Col).Distinct().Count() == 1)
                    TryAdd(matrix.Cols[cellsWithCandidate[0].Col], cellsWithCandidate, value, findings);
            }
        }

        return findings;
    }

    /// <summary>
    /// Adds a finding if a candidate in a block is restricted to a single row or column.
    /// </summary>
    /// <param name="line">The row or column to inspect.</param>
    /// <param name="keyCells">The cells in the block that carry the candidate.</param>
    /// <param name="value">The candidate value to analyze.</param>
    /// <param name="findings">The findings collection to update.</param>
    private void TryAdd(BaseCell[] line, BaseCell[] keyCells, int value, List<StrategyFinding> findings)
    {
        var affected = line
            .Where(c => c.CellValue == Values.Undefined && !keyCells.Contains(c) && c.Enabled(value))
            .ToArray();

        if(affected.Length == 0) return;

        findings.Add(new StrategyFinding(
            Name,
            keyCells,
            affected,
            $"Candidate {value} appears only in this row/column within this block and can therefore be removed outside the block in that row/column."));
    }
}