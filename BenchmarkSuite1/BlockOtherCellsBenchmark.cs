using System.Collections.Generic;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Sudoku.Benchmarks
{
    // Derived class to expose protected BlockOtherCells for benchmarking
    internal class DerivedSudokuMatrix: SudokuMatrix
    {
        public bool CallBlockOtherCells(List<BaseCell> enabledCells, int block)
        {
            return base.BlockOtherCells(enabledCells, block);
        }
    }

    [MemoryDiagnoser]
    public class BlockOtherCellsBenchmark
    {
        private DerivedSudokuMatrix matrix;
        private List<BaseCell> enabledCells;
        private int block = 1;
        [GlobalSetup]
        public void Setup()
        {
            matrix = new DerivedSudokuMatrix();
            matrix.Init();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Re-initialize to reset state modified by BlockOtherCells
            matrix.Init();
            enabledCells = new List<BaseCell>();
            // collect enabled cells for block 'block' from first row
            foreach(BaseCell cell in matrix.Rows[0])
            {
                if(cell.nPossibleValues > 0 && cell.Enabled(block))
                    enabledCells.Add(cell);
            }

            // Ensure at least some cells are present; if not, pick first two
            if(enabledCells.Count == 0)
            {
                enabledCells.Add(matrix.Cell(0, 0));
                if(matrix.Cell(0, 1) != null)
                    enabledCells.Add(matrix.Cell(0, 1));
            }
        }

        [Benchmark]
        public bool RunBlockOtherCells()
        {
            return matrix.CallBlockOtherCells(enabledCells, block);
        }
    }
}