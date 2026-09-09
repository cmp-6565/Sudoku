// Sudoku.Core/Solving/PointingPairStrategy.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Core.Solving;

/// <summary>
/// Detects a pointing-pair pattern where a candidate value appears in only one row or column within a box.
/// When such a pattern exists, the candidate can be eliminated from the remaining cells in that row or column
/// outside the current box.
/// </summary>
public sealed class PointingPairStrategy: ISolvingStrategy
{
    /// <inheritdoc />
    public string Name => "Pointing Pair";

    /// <inheritdoc />
    public int Difficulty => 4;

    /// <summary>
    /// Scans each 3x3 block for candidate values restricted to a single row or column.
    /// </summary>
    /// <param name="matrix">The Sudoku matrix to analyze.</param>
    /// <returns>All pointing-pair findings found in the current board state.</returns>
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
    /// Adds a finding when the candidate in the block is confined to a single row or column.
    /// </summary>
    /// <param name="line">The row or column to inspect.</param>
    /// <param name="keyCells">The cells in the block carrying the candidate.</param>
    /// <param name="value">The candidate value under analysis.</param>
    /// <param name="findings">The collection to append the finding to.</param>
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
            StrategyFindingResourceProvider.GetPointingPairDescription(value)));
    }
}