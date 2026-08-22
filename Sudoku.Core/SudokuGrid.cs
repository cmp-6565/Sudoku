// Sudoku.Core/SudokuGrid.cs
namespace Sudoku.Core;

/// <summary>
/// Feste geometrische Kenngrößen eines klassischen 9x9-Sudoku-Gitters.
/// Bewusst unabhängig von jeder UI-Technologie.
/// </summary>
public static class SudokuGrid
{
    public static int RectSize => 3;
    public static int SudokuSize => RectSize * RectSize;
    public static int TotalCellCount => SudokuSize * SudokuSize;
}