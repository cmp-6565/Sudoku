using BenchmarkDotNet.Attributes;

using Microsoft.VSDiagnostics;

namespace Sudoku.Benchmarks
{
    [CPUUsageDiagnoser]
    public class CandidateMaskVsArrayBenchmark
    {
        private SudokuMatrix matrix;
        private bool[,,] arrayCandidates;
        private bool[,,] arrayExclusion;
        private int size;
        // deterministic sequences to avoid RNG overhead during benchmarks
        private int[] seqR;
        private int[] seqC;
        private int[] seqV;
        private int ops = 20000;
        private int checksum;
        [GlobalSetup]
        public void Setup()
        {
            size = SudokuForm.SudokuSize;
            matrix = new SudokuMatrix();
            matrix.Init();
            arrayCandidates = new bool[size, size, size + 1];
            arrayExclusion = new bool[size, size, size + 1];
            // build deterministic sequences that cycle through all (r,c,v) combos
            seqR = new int[ops];
            seqC = new int[ops];
            seqV = new int[ops];
            int total = size * size * size;
            for(int i = 0; i < ops; i++)
            {
                int idx = i % total; // cycle through all combinations
                seqR[i] = idx / (size * size);
                seqC[i] = (idx / size) % size;
                seqV[i] = (idx % size) + 1; // values 1..size
            }
            checksum = 0;
        }

        [Benchmark(Baseline = true)]
        public int Mask_SetGet()
        {
            int local = 0;
            for(int i = 0; i < ops; i++)
            {
                int r = seqR[i];
                int c = seqC[i];
                int v = seqV[i];
                matrix.SetCandidate(r, c, v, false);
                if(matrix.GetCandidate(r, c, v, false))
                    local++;
            }

            checksum ^= local;
            return local;
        }

        [Benchmark]
        public int Array_SetGet()
        {
            int local = 0;
            for(int i = 0; i < ops; i++)
            {
                int r = seqR[i];
                int c = seqC[i];
                int v = seqV[i];
                // toggle semantics: flip and clear mutually exclusive
                arrayCandidates[r, c, v] = !arrayCandidates[r, c, v];
                arrayExclusion[r, c, v] = false;
                if(arrayCandidates[r, c, v])
                    local++;
            }

            checksum ^= local;
            return local;
        }
    }
}