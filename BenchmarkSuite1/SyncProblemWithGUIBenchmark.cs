using System.Globalization;

using BenchmarkDotNet.Attributes;

using Sudoku;

namespace BenchmarkSuite1
{
    [MemoryDiagnoser]
    public class SyncProblemWithGUIBenchmark
    {
        private BaseProblem _problem;
        private string[,] _grid;
        private CultureInfo _culture;
        private int _incorrectTries;

        [GlobalSetup]
        public void Setup()
        {
            _problem = new SudokuProblem();
            _culture = CultureInfo.InvariantCulture;
            _grid = new string[SudokuForm.SudokuSize, SudokuForm.SudokuSize];

            // Fill grid with a moderate number of values (valid 9x9 Sudoku seed)
            // Using a simple pattern to avoid invalid states
            byte[,] seed =
            {
                {5,3,0,0,7,0,0,0,0},
                {6,0,0,1,9,5,0,0,0},
                {0,9,8,0,0,0,0,6,0},
                {8,0,0,0,6,0,0,0,3},
                {4,0,0,8,0,3,0,0,1},
                {7,0,0,0,2,0,0,0,6},
                {0,6,0,0,0,0,2,8,0},
                {0,0,0,4,1,9,0,0,5},
                {0,0,0,0,8,0,0,7,9}
            };

            for(int r = 0; r < SudokuForm.SudokuSize; r++)
                for(int c = 0; c < SudokuForm.SudokuSize; c++)
                    _grid[r, c] = seed[r, c] == 0 ? string.Empty : seed[r, c].ToString(_culture);
        }

        [Benchmark]
        public bool RunSync()
        {
            return SyncHelper.TrySyncGrid(_problem, _grid, _culture, autoCheck: true, ref _incorrectTries, out var synced);
        }
    }
}
