using System;

namespace Sudoku
{
    [Serializable]
    public class Solution: Values
    {
        private ISudokuSettings settings;

        private byte[][] values;

        public Solution(ISudokuSettings settings)
        {
            values = new byte[settings.SudokuSize][];
            for(int row = 0; row < settings.SudokuSize; row++)
                values[row] = new byte[settings.SudokuSize];
            Init();
            this.settings = settings;
        }

        public override void SetValue(int row, int col, byte value, Boolean fixedValue)
        {
            if(((value < 1 || value > settings.SudokuSize) && value != Values.Undefined) || row < 0 || col < 0 || row > settings.SudokuSize || col > settings.SudokuSize)
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

            for(row = 0; row < settings.SudokuSize; row++)
                for(col = 0; col < settings.SudokuSize; col++)
                    values[row][col] = Values.Undefined;
        }
    }
}
