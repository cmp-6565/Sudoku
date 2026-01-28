using System;

namespace Sudoku
{
    [Serializable]
    internal class Cell: BaseCell
    {
        public Cell(int row, int col, ISudokuSettings settings) : base(row, col, settings)
        {
            neighbors = new BaseCell[20];
        }

        public override Boolean Up()
        {
            return false;
        }

        public override Boolean Down()
        {
            return false;
        }
    }
}
