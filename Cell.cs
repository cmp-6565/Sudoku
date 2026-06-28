using System;

namespace Sudoku;

/// <summary>
/// Represents a single cell in a standard 9x9 Sudoku grid.
/// </summary>
[Serializable]
internal class Cell: BaseCell
{
    /// <summary>
    /// Initializes a new instance of the Cell class with the specified row and column coordinates.
    /// </summary>
    /// <param name="row">The row coordinate of the cell (0-based index).</param>
    /// <param name="col">The column coordinate of the cell (0-based index).</param>
    public Cell(int row, int col) : base(row, col)
    {
        neighbors = new BaseCell[20];
    }

    /// <summary>
    /// Moves to the previous cell in the row (always returns false for standard Sudoku).
    /// </summary>
    /// <returns>False, as standard Sudoku cells do not support vertical wrapping.</returns>
    public override Boolean Up()
    {
        return false;
    }

    /// <summary>
    /// Moves to the next cell in the row (always returns false for standard Sudoku).
    /// </summary>
    /// <returns>False, as standard Sudoku cells do not support vertical wrapping.</returns>
    public override Boolean Down()
    {
        return false;
    }
}
