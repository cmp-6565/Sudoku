using System;
using System.Collections.Generic;

namespace Sudoku;

[Serializable]
internal class DiagonalCell: BaseCell
{
    public DiagonalCell(int row, int col): base(row, col)
    {
        if(row == col && row + col == WinFormsSettings.SudokuSize - 1)
            neighbors=new BaseCell[32];
        else
            neighbors=new BaseCell[26];
    }

    public override Boolean Up()
    {
        return Row + Col == WinFormsSettings.SudokuSize - 1;
    }

    public override Boolean Down()
    {
        return Row == Col;
    }

    protected override List<BaseCell> GetCommonNeighbors(List<BaseCell> candidateNeighbors, BaseCell[] neighborCells)
    {
        List<BaseCell> commonNeighbors=base.GetCommonNeighbors(candidateNeighbors, neighborCells);

        foreach(BaseCell cell in Neighbors)
        {
            if((cell.Up() || cell.Down()) && cell.CellValue == Values.Undefined && !candidateNeighbors.Contains(cell))
            {
                Boolean common=true;
                foreach(BaseCell candidate in candidateNeighbors)
                    if(candidate != this && common)
                        common=candidate.CommonNeighbor(cell);

                if(common) commonNeighbors.Add(cell);
            }
        }

        return commonNeighbors;
    }

}
