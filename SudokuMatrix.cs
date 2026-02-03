using System;

namespace Sudoku;

[Serializable]
internal class SudokuMatrix: BaseMatrix
{
    public SudokuMatrix() : base()
    {
    }
    public override BaseCell CreateValue(int row, int col)
    {
        return new Cell(row, col);
    }

    protected override BaseCell[] GetDiagonal(SudokuPart direction)
    {
        return null;
    }
}