using BenchmarkDotNet.Attributes;

using Microsoft.VSDiagnostics;

namespace Sudoku.Benchmarks
{
    [CPUUsageDiagnoser]
    public class GenerationBenchmark
    {
        ISudokuSettings settings = new WinFormsSettings();

        private SudokuProblem problem;
        private int[][] puzzles;
        [GlobalSetup]
        public void Setup()
        {
            puzzles = new int[1][];
            // a moderately filled puzzle to exercise candidate logic
            puzzles[0] = new int[]
            {
                5,
                3,
                0,
                0,
                7,
                0,
                0,
                0,
                0,
                6,
                0,
                0,
                1,
                9,
                5,
                0,
                0,
                0,
                0,
                9,
                8,
                0,
                0,
                0,
                0,
                6,
                0,
                8,
                0,
                0,
                0,
                6,
                0,
                0,
                0,
                3,
                4,
                0,
                0,
                8,
                0,
                3,
                0,
                0,
                1,
                7,
                0,
                0,
                0,
                2,
                0,
                0,
                0,
                6,
                0,
                6,
                0,
                0,
                0,
                0,
                2,
                8,
                0,
                0,
                0,
                0,
                4,
                1,
                9,
                0,
                0,
                5,
                0,
                0,
                0,
                0,
                8,
                0,
                0,
                7,
                9
            };
            problem = new SudokuProblem(settings);
            int size = SudokuForm.SudokuSize;
            problem.Matrix.Init();
            problem.Matrix.SetPredefinedValues = false;
            for(int i = 0; i < puzzles[0].Length; i++)
            {
                int v = puzzles[0][i];
                if(v != 0)
                    problem.SetValue(i / size, i % size, (byte)v);
            }

            problem.Matrix.SetPredefinedValues = true;
        }

        [Benchmark(Baseline = true)]
        public int GetObviousCells()
        {
            var list = problem.Matrix.GetObviousCells(true);
            return list.Count;
        }

        [Benchmark]
        public int FindNakedCells_AllParts()
        {
            int total = 0;
            var m = problem.Matrix;
            // rows
            for(int r = 0; r < SudokuForm.SudokuSize; r++)
            {
                var part = m.Rows[r];
                foreach(var cell in part)
                {
                    total += cell.FindNakedCells(part);
                }
            }

            // cols
            for(int c = 0; c < SudokuForm.SudokuSize; c++)
            {
                var part = m.Cols[c];
                foreach(var cell in part)
                {
                    total += cell.FindNakedCells(part);
                }
            }

            // rectangles
            for(int b = 0; b < SudokuForm.SudokuSize; b++)
            {
                var part = m.Rectangles[b];
                foreach(var cell in part)
                {
                    total += cell.FindNakedCells(part);
                }
            }

            return total;
        }
    }
}