using System;
using System.Collections.Generic;

namespace Sudoku
{
    [Serializable]
    internal class XSudokuMatrix: BaseMatrix
    {
        protected BaseCell[] UpDiagonal;
        protected BaseCell[] DownDiagonal;

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

        public override BaseCell CreateValue(int row, int col)
        {
            if(row == col || row + col == WinFormsSettings.SudokuSize - 1)
                return new DiagonalCell(row, col);
            else
                return new Cell(row, col);
        }

        protected override BaseCell[] GetDiagonal(SudokuPart direction)
        {
            if(direction == SudokuPart.DownDiagonal)
                return DownDiagonal;
            else
                return UpDiagonal;
        }

        public Boolean CheckDiagonals()
        {
            return Check(GetDiagonal(SudokuPart.DownDiagonal)) && Check(GetDiagonal(SudokuPart.UpDiagonal));
        }

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

        public override int MinimumValues
        {
            get { return 12; }
        }

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
}