// Sudoku.Core/Solving/HiddenSingleStrategy.cs
#nullable enable
using System.Collections.Generic;

namespace Sudoku.Core.Solving;

/// <summary>
/// Detects hidden singles in rows, columns, and boxes.
/// A hidden single is a candidate value that appears exactly once in a unit, even if it is not the only
/// candidate in its own cell. This pattern means the value is forced in that cell and can be used as a
/// valid solving hint or explanation.
/// </summary>
public sealed class HiddenSingleStrategy: ISolvingStrategy
{
    /// <inheritdoc />
    public string Name => "Hidden Single";

    /// <inheritdoc />
    public int Difficulty => 1;

    /// <summary>
    /// Scans every row, column, and box for hidden single candidates.
    /// </summary>
    /// <param name="matrix">The Sudoku matrix to analyze.</param>
    /// <returns>All hidden-single findings found in the current board state.</returns>
    public IReadOnlyList<StrategyFinding> FindAll(BaseMatrix matrix)
    {
        var findings = new List<StrategyFinding>();
        for(int i = 0; i < SudokuGrid.SudokuSize; i++)
        {
            FindInUnit(matrix.Rows[i], "row", findings);
            FindInUnit(matrix.Cols[i], "column", findings);
            FindInUnit(matrix.Rectangles[i], "block", findings);
        }
        return findings;
    }

    /// <summary>
    /// Checks a single unit for a hidden single.
    /// </summary>
    /// <param name="unit">The row, column, or block to inspect.</param>
    /// <param name="unitLabel">The unit type used in the explanation text.</param>
    /// <param name="findings">The collection to append matching findings to.</param>
    private void FindInUnit(BaseCell[] unit, string unitLabel, List<StrategyFinding> findings)
    {
        for(int value = 1; value <= SudokuGrid.SudokuSize; value++)
        {
            BaseCell? only = null;
            int count = 0;

            foreach(BaseCell cell in unit)
            {
                if(cell.CellValue != Values.Undefined) continue;
                if(!cell.Enabled(value)) continue;
                count++;
                only = cell;
                if(count > 1) break;
            }

            if(count == 1 && only!.nPossibleValues > 1)
            {
                findings.Add(new StrategyFinding(
                    Name,
                    KeyCells: new[] { only },
                    AffectedCells: new[] { only },
                    Description: StrategyFindingResourceProvider.GetHiddenSingleDescription(unitLabel, value)));
            }
        }
    }
}