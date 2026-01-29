using BenchmarkDotNet.Attributes;

namespace Sudoku.Benchmarks
{
    [MemoryDiagnoser]
    public class GetEnabledMaskBenchmark
    {
        private BaseMatrix _matrix;
        private BaseCell[] _cells;

        [GlobalSetup]
        public void Setup()
        {
            var matrix = new DerivedSudokuMatrix();
            matrix.Init();
            _matrix = matrix;

            _cells = new BaseCell[SudokuForm.TotalCellCount];
            int idx = 0;
            for(int r = 0; r < SudokuForm.SudokuSize; r++)
                for(int c = 0; c < SudokuForm.SudokuSize; c++)
                    _cells[idx++] = _matrix.Cell(r, c);
        }

        [Benchmark]
        public int SumEnabledMasks()
        {
            int sum = 0;
            var cells = _cells;
            for(int i = 0; i < cells.Length; i++)
                sum ^= cells[i].GetEnabledMask();
            return sum;
        }
    }
}
