// Sudoku.Core/SudokuGrid.cs
namespace Sudoku.Core;

/// <summary>
/// Defines the fixed geometric dimensions of a standard 9x9 Sudoku grid.
/// This class is intentionally independent of any UI technology.
/// </summary>
public static class SudokuGrid
{
    /// <summary>
    /// Gets the size of each 3x3 sub-grid.
    /// </summary>
    public static int RectSize => 3;

    /// <summary>
    /// Gets the number of rows and columns in the Sudoku grid.
    /// </summary>
    public static int SudokuSize => RectSize * RectSize;

    /// <summary>
    /// Gets the total number of cells in the grid.
    /// </summary>
    public static int TotalCellCount => SudokuSize * SudokuSize;
}