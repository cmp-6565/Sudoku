using System;

namespace Sudoku
{
    [Serializable]
    internal class SudokuMatrix: BaseMatrix
    {
        public SudokuMatrix(ISudokuSettings settings) : base(settings)
        {
        }
        public override BaseCell CreateValue(int row, int col)
        {
            return new Cell(row, col, settings);
        }

        protected override BaseCell[] GetDiagonal(SudokuPart direction)
        {
            return null;
        }
    }
}