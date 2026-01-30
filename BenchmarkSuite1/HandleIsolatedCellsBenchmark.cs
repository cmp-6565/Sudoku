using BenchmarkDotNet.Attributes;

namespace Sudoku.Benchmarks
{
    [MemoryDiagnoser]
    public class HandleIsolatedCellsBenchmark
    {
        private DerivedSudokuMatrix matrix;
        private BaseCell[] part;

        [GlobalSetup]
        public void Setup()
        {
            matrix = new DerivedSudokuMatrix();
            matrix.Init();
            // Use first row as part
            part = matrix.Rows[0];
            // Ensure some enabled candidates exist
            foreach(var c in part)
            {
                c.InitCandidates();
                // ensure enabled mask is initialized
                var _ = c.GetEnabledMask();
            }
        }

        [Benchmark]
        public bool RunHandleIsolatedCells()
        {
            return matrix.CallHandleIsolatedCells(part);
        }
    }
}
