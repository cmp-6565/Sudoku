using System;

namespace Sudoku
{
    [Serializable]
    public class Solution: Values
    {
        private byte[][] values;

        public Solution()
        {
            values = new byte[SudokuForm.SudokuSize][];
            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                values[row] = new byte[SudokuForm.SudokuSize];
            Init();
        }

        public override void SetValue(int row, int col, byte value, Boolean fixedValue)
        {
            if(((value < 1 || value > SudokuForm.SudokuSize) && value != Values.Undefined) || row < 0 || col < 0 || row > SudokuForm.SudokuSize || col > SudokuForm.SudokuSize)
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

            for(row = 0; row < SudokuForm.SudokuSize; row++)
                for(col = 0; col < SudokuForm.SudokuSize; col++)
                    values[row][col] = Values.Undefined;
        }
    }
}
