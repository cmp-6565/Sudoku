using System;

namespace Sudoku;

[Serializable]
public class Solution: Values
{
    private ISudokuSettings settings;

    private byte[][] values;

    public Solution(ISudokuSettings settings)
    {
        values = new byte[WinFormsSettings.SudokuSize][];
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            values[row] = new byte[WinFormsSettings.SudokuSize];
        Init();
        this.settings = settings;
    }

    // Neuer Kopierkonstruktor
    protected Solution(Solution clone) : base(clone)
    {
        this.settings = clone.settings;
        this.Counter = clone.Counter;
        this.values = new byte[clone.values.Length][];
        for(int i = 0; i < clone.values.Length; i++)
        {
            this.values[i] = (byte[])clone.values[i].Clone();
        }
    }

    public override object Clone()
    {
        return new Solution(this);
    }

    public override void SetValue(int row, int col, byte value, Boolean fixedValue)
    {
        if(((value < 1 || value > WinFormsSettings.SudokuSize) && value != Values.Undefined) || row < 0 || col < 0 || row > WinFormsSettings.SudokuSize || col > WinFormsSettings.SudokuSize)
            throw new InvalidSudokuValueException();
        values[row][col] = value;
    }

    public override byte GetValue(int row, int col)
    {
        return values[row][col];
    }

    public override Boolean FixedValue(int row, int col)
    {
        return true;
    }

    public override Boolean ComputedValue(int row, int col)
    {
        return false;
    }

    public override Boolean ReadOnly(int row, int col)
    {
        return true;
    }

    public override void Init()
    {
        int row, col;

        for(row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(col = 0; col < WinFormsSettings.SudokuSize; col++)
                values[row][col] = Values.Undefined;
    }
}
