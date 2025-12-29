using System;
using BenchmarkDotNet.Attributes;
using Sudoku;

namespace Sudoku.Benchmarks
{
    [MemoryDiagnoser]
    public class GetObviousCellsBenchmark
    {
        private DerivedSudokuMatrix matrix;

        [GlobalSetup]
        public void Setup()
        {
            matrix = new DerivedSudokuMatrix();
            matrix.Init();
        }

        [Benchmark]
        public bool RunFillObviousCells()
        {
            return matrix.CallFillObviousCells(false);
        }
    }
}
