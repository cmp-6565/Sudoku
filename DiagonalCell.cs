using System;
using System.Collections.Generic;

namespace Sudoku;

/// <summary>
/// Represents a cell in an X-Sudoku (Diagonal Sudoku) puzzle where cells on main and anti-diagonals have additional constraints.
/// </summary>
[Serializable]
internal class DiagonalCell: BaseCell
{
    /// <summary>
    /// Initializes a new instance of the DiagonalCell class with the specified row and column coordinates.
    /// </summary>
    /// <param name="row">The row coordinate of the cell (0-based index).</param>
    /// <param name="col">The column coordinate of the cell (0-based index).</param>
    /// <remarks>
    /// The number of neighbors is increased to accommodate cells that share diagonal constraints.
    /// Cells on both diagonals have 32 neighbors, while cells on a single diagonal have 26.
    /// </remarks>
    public DiagonalCell(int row, int col) : base(row, col)
    {
        if(row == col && row + col == WinFormsSettings.SudokuSize - 1)
            neighbors = new BaseCell[32];
        else
            neighbors = new BaseCell[26];
    }

    /// <summary>
    /// Determines if this cell is on the anti-diagonal (bottom-left to top-right).
    /// </summary>
    /// <returns>True if the cell is on the anti-diagonal; otherwise, false.</returns>
    public override Boolean Up()
    {
        return Row + Col == WinFormsSettings.SudokuSize - 1;
    }

    /// <summary>
    /// Determines if this cell is on the main diagonal (top-left to bottom-right).
    /// </summary>
    /// <returns>True if the cell is on the main diagonal; otherwise, false.</returns>
    public override Boolean Down()
    {
        return Row == Col;
    }

    /// <summary>
    /// Retrieves common neighbors considering diagonal constraints in X-Sudoku.
    /// </summary>
    /// <param name="candidateNeighbors">The list of candidate neighbors to filter.</param>
    /// <param name="neighborCells">The array of all neighbors to check.</param>
    /// <returns>A list of cells that are common neighbors considering diagonal constraints.</returns>
    protected override List<BaseCell> GetCommonNeighbors(List<BaseCell> candidateNeighbors, BaseCell[] neighborCells)
    {
        List<BaseCell> commonNeighbors = base.GetCommonNeighbors(candidateNeighbors, neighborCells);

        foreach(BaseCell cell in Neighbors)
        {
            if((cell.Up() || cell.Down()) && cell.CellValue == Values.Undefined && !candidateNeighbors.Contains(cell))
            {
                Boolean common = true;
                foreach(BaseCell candidate in candidateNeighbors)
                    if(candidate != this && common)
                        common = candidate.CommonNeighbor(cell);

                if(common) commonNeighbors.Add(cell);
            }
        }

        return commonNeighbors;
    }

}
