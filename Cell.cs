using System;

namespace Sudoku;

[Serializable]
internal class Cell: BaseCell
{
    public Cell(int row, int col): base(row, col)
    {
        neighbors=new BaseCell[20];
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
