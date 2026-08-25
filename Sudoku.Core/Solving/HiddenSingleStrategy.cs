// Sudoku.Core/Solving/HiddenSingleStrategy.cs
#nullable enable
using System.Collections.Generic;

namespace Sudoku.Core.Solving;

/// <summary>
/// Identifies hidden singles in each unit of the puzzle.
/// A hidden single is a value that appears as the only possible candidate in a row, column, or box.
/// </summary>
public sealed class HiddenSingleStrategy: ISolvingStrategy
{
    /// <inheritdoc />
    public string Name => "Hidden Single";

    /// <inheritdoc />
    public int Difficulty => 1;

    /// <summary>
    /// Searches all rows, columns, and blocks for hidden single candidates.
    /// </summary>
    /// <param name="matrix">The Sudoku matrix to analyze.</param>
    /// <returns>A list of findings describing every hidden single found.</returns>
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
    /// Checks a single unit for a hidden single candidate.
    /// </summary>
    /// <param name="unit">The unit to inspect.</param>
    /// <param name="unitLabel">The unit label used for the description text.</param>
    /// <param name="findings">The collection of findings to append to.</param>
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
                    Description: $"In this {unitLabel}, only this cell can take the value {value}."));
            }
        }
    }
}