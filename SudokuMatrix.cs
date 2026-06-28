using System;

namespace Sudoku;

/// <summary>
/// Represents the data model for a standard 9x9 Sudoku grid.
/// Manages the matrix of cells and their constraints.
/// </summary>
[Serializable]
internal class SudokuMatrix: BaseMatrix
{
    /// <summary>
    /// Initializes a new instance of the SudokuMatrix class.
    /// </summary>
    public SudokuMatrix() : base()
    {
    }

    /// <summary>
    /// Creates a new cell for the standard Sudoku grid.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>A new Cell instance at the specified position.</returns>
    public override BaseCell CreateValue(int row, int col)
    {
        return new Cell(row, col);
    }

    /// <summary>
    /// Gets the diagonal cells for the specified direction (not applicable for standard Sudoku).
    /// </summary>
    /// <param name="direction">The diagonal direction (not used for standard Sudoku).</param>
    /// <returns>Always returns null as standard Sudoku does not have diagonal constraints.</returns>
    protected override BaseCell[] GetDiagonal(SudokuPart direction)
    {
        return null;
    }
}
