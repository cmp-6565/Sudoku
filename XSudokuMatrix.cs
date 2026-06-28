using System;
using System.Collections.Generic;

namespace Sudoku;

/// <summary>
/// Represents the data model for an X-Sudoku (Diagonal Sudoku) grid with additional diagonal constraints.
/// Manages cells on both main and anti-diagonals in addition to standard row, column, and box constraints.
/// </summary>
[Serializable]
internal class XSudokuMatrix: BaseMatrix
{
    /// <summary>
    /// Array of cells on the anti-diagonal (bottom-left to top-right).
    /// </summary>
    protected BaseCell[] UpDiagonal;

    /// <summary>
    /// Array of cells on the main diagonal (top-left to bottom-right).
    /// </summary>
    protected BaseCell[] DownDiagonal;

    /// <summary>
    /// Initializes a new instance of the XSudokuMatrix class with diagonal constraints configured.
    /// </summary>
    public XSudokuMatrix() : base()
    {
        UpDiagonal = new BaseCell[WinFormsSettings.SudokuSize];
        DownDiagonal = new BaseCell[WinFormsSettings.SudokuSize];
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
            {
                if(!Cell(row, row).SameRectangle(Cell(i, i))) Cell(row, row).AddNeighbor(ref Matrix[i][i]);
                if(!Cell(row, WinFormsSettings.SudokuSize - 1 - row).SameRectangle(Cell(i, WinFormsSettings.SudokuSize - 1 - i))) Cell(row, WinFormsSettings.SudokuSize - 1 - row).AddNeighbor(ref Matrix[i][WinFormsSettings.SudokuSize - 1 - i]);
            }
        for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
        {
            DownDiagonal[i] = Cell(i, i);
            UpDiagonal[i] = Cell(i, WinFormsSettings.SudokuSize - 1 - i);
        }
    }

    /// <summary>
    /// Creates a new cell for the X-Sudoku grid.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>A DiagonalCell if the cell is on a diagonal; otherwise, a standard Cell.</returns>
    public override BaseCell CreateValue(int row, int col)
    {
        if(row == col || row + col == WinFormsSettings.SudokuSize - 1)
            return new DiagonalCell(row, col);
        else
            return new Cell(row, col);
    }

    /// <summary>
    /// Gets the diagonal cells for the specified direction.
    /// </summary>
    /// <param name="direction">The diagonal direction (DownDiagonal or UpDiagonal).</param>
    /// <returns>The array of cells on the specified diagonal.</returns>
    protected override BaseCell[] GetDiagonal(SudokuPart direction)
    {
        if(direction == SudokuPart.DownDiagonal)
            return DownDiagonal;
        else
            return UpDiagonal;
    }

    /// <summary>
    /// Verifies that both diagonals have valid constraints.
    /// </summary>
    /// <returns>True if both diagonals are valid; false otherwise.</returns>
    public Boolean CheckDiagonals()
    {
        return Check(GetDiagonal(SudokuPart.DownDiagonal)) && Check(GetDiagonal(SudokuPart.UpDiagonal));
    }

    /// <summary>
    /// Blocks values in cells on the same diagonal when a block is placed.
    /// </summary>
    /// <param name="cells">The list of cells to block.</param>
    /// <param name="block">The value to block (1-9).</param>
    /// <returns>True if any cell had the block value enabled; false otherwise.</returns>
    protected override Boolean BlockOtherCells(List<BaseCell> cells, int block)
    {
        Boolean rc = base.BlockOtherCells(cells, block);
        Boolean proceed = true;
        BaseCell[] neighborCells;

        foreach(BaseCell cell in cells)
            proceed &= cell is DiagonalCell && cell.Up() == cells[0].Up();
        if(proceed)
        {
            neighborCells = (cells[0].Up() && cells[cells.Count - 1].Up() ? UpDiagonal : DownDiagonal);
            foreach(BaseCell cell in neighborCells)
                if(!cells.Contains(cell))
                {
                    rc |= cell.Enabled(block);
                    cell.SetBlock(block, false, false);
                }
        }
        return rc;
    }

    /// <summary>
    /// Gets the minimum number of clues required for an X-Sudoku puzzle.
    /// </summary>
    public override int MinimumValues
    {
        get { return 12; }
    }

    /// <summary>
    /// Gets the severity level for this X-Sudoku matrix (adjusted from base severity).
    /// </summary>
    public override float SeverityLevel
    {
        get
        {
            if((severityLevel = base.SeverityLevel) == float.NaN)
                return float.NaN;
            severityLevel /= 1.1f;

            return severityLevel;
        }
    }
}