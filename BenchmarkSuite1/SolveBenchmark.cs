using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using Microsoft.VSDiagnostics;

namespace Sudoku.Benchmarks
{
    [CPUUsageDiagnoser]
    public class SolveBenchmark
    {
        private List<SudokuProblem> preparedProblems;
        private int checksum = 0;
        private int[][] puzzles;

        [GlobalSetup]
        public void Setup()
        {
            preparedProblems = new List<SudokuProblem>(2);
            puzzles = new int[2][];
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
            puzzles[1] = new int[]
            {
                0,
                0,
                0,
                2,
                6,
                0,
                7,
                0,
                1,
                6,
                8,
                0,
                0,
                7,
                0,
                0,
                9,
                0,
                1,
                9,
                0,
                0,
                0,
                4,
                5,
                0,
                0,
                8,
                2,
                0,
                1,
                0,
                0,
                0,
                4,
                0,
                0,
                0,
                4,
                6,
                0,
                2,
                9,
                0,
                0,
                0,
                5,
                0,
                0,
                0,
                3,
                0,
                2,
                8,
                0,
                0,
                9,
                3,
                0,
                0,
                0,
                7,
                4,
                0,
                4,
                0,
                0,
                5,
                0,
                0,
                3,
                6,
                7,
                0,
                3,
                0,
                1,
                8,
                0,
                0,
                0
            };
            int size = SudokuForm.SudokuSize;
            for (int p = 0; p < puzzles.Length; p++)
            {
                // keep puzzles in memory; do not pre-create SudokuProblem instances to avoid cloning issues
                // initialization of problem instances will happen in each benchmark iteration
                // (preparing puzzles array only)
                // nothing to do here besides storing the puzzle
                // preparedProblems list kept for backward compatibility but left empty
                
                // no-op
            }
        }

        [Benchmark(Baseline = true)]
        public int Solve_Puzzle1()
        {
            var prob = CreateProblemFromArray(puzzles[0]);
            prob.FindSolutions(1UL);
            int local = prob.Solutions.Count;
            checksum ^= local;
            return local;
        }

        [Benchmark]
        public int Solve_Puzzle2()
        {
            var prob = CreateProblemFromArray(puzzles[1]);
            prob.FindSolutions(1UL);
            int local = prob.Solutions.Count;
            checksum ^= local;
            return local;
        }

        [Benchmark]
        public int Solve_All()
        {
            int total = 0;
            for (int i = 0; i < puzzles.Length; i++)
            {
                var prob = CreateProblemFromArray(puzzles[i]);
                prob.FindSolutions(1UL);
                total += prob.Solutions.Count;
            }

            checksum ^= total;
            return total;
        }

        private SudokuProblem CloneProblem(SudokuProblem src)
        {
            // retain for compatibility but not used
            return src;
        }

        private SudokuProblem CreateProblemFromArray(int[] arr)
        {
            var prob = new SudokuProblem();
            int size = SudokuForm.SudokuSize;
            prob.Matrix.Init();
            prob.Matrix.SetPredefinedValues = false;
            for (int i = 0; i < arr.Length; i++)
            {
                int v = arr[i];
                if (v != 0) prob.SetValue(i / size, i % size, (byte)v);
            }
            prob.Matrix.SetPredefinedValues = true;
            return prob;
        }
    }
 }