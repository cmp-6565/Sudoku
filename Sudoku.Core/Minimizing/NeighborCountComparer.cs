using System.Collections.Generic;

using Sudoku.Core;

internal class NeighborCountComparer: IComparer<BaseCell>
{
    public int Compare(BaseCell? x, BaseCell? y)
    {
        if(x == null || y == null) return 0;

        // Absteigend sortieren (Meiste Nachbarn zuerst)
        return y.FilledNeighborCount.CompareTo(x.FilledNeighborCount);
    }
}
