using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Sudoku.BenchmarkSuite1;

internal static class MinimizeLogAnalyzer
{
    private const double LongRunningThresholdSeconds = 30.0; // adjust if needed

    public static void Run(string solutionsPath)
    {
        if(!File.Exists(solutionsPath))
        {
            throw new FileNotFoundException($"Log file \"{solutionsPath}\" not found.");
        }

        var json = File.ReadAllText(solutionsPath);
        var raw = JsonSerializer.Deserialize<List<TestResult>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        var samples = raw
            .Where(r => r.GreedyRuntime > 0 && r.CandidateRuntime > 0 && r.TotalRuntime > LongRunningThresholdSeconds)
            .Select(r => new Sample(
                r.XSudoku,
                r.GreedyRuntime,
                r.CandidateRuntime,
                r.TotalRuntime,
                r.Parameters,
                r.Parameters.FavoriteAlgorithm == BaseProblem.MinimizeAlgorithm.Candidate,
                r.CandidateRuntime <= r.GreedyRuntime))
            .ToArray();

        if(samples.Length == 0)
        {
            System.Console.WriteLine("No usable entries found.");
            return;
        }

        DumpOverallStats(samples);

        Evaluate(samples, s => s.PredictedCandidate, "Current FavoriteAlgorithm");
        Evaluate(samples, _ => false, "Always Greedy");
        Evaluate(samples, _ => true, "Always Candidate");

        Evaluate(samples, s =>
            s.Parameters.TotalRemovable >= 35 &&
            s.Parameters.RemainingMargin <= 20 &&
            s.Parameters.RemovedByGreedy >= 4,
            "Simple rule: many removable & low margin => Candidate");

        SearchThresholds(samples);
    }

    private static void DumpOverallStats(IReadOnlyCollection<Sample> samples)
    {
        int candidateWins = samples.Count(s => s.ActualCandidateFaster);
        int greedyWins = samples.Count - candidateWins;
        int longRunning = samples.Count(s => s.LongRunning);

        System.Console.WriteLine($"Samples...........: {samples.Count}");
        System.Console.WriteLine($"XSudoku share.....: {samples.Count(s => s.XSudoku) / (double)samples.Count:P1}");
        System.Console.WriteLine($"Candidate faster..: {candidateWins} ({candidateWins / (double)samples.Count:P1})");
        System.Console.WriteLine($"Greedy faster.....: {greedyWins} ({greedyWins / (double)samples.Count:P1})");
        System.Console.WriteLine($"Long-running cases: {longRunning} ({longRunning / (double)samples.Count:P1})");
        System.Console.WriteLine();
    }

    private static void Evaluate(IEnumerable<Sample> samples, System.Func<Sample, bool> chooseCandidate, string title)
    {
        var stats = CollectStats(samples, chooseCandidate);
        System.Console.WriteLine(FormatStats(title, stats));

        var longStats = CollectStats(samples.Where(s => s.LongRunning), chooseCandidate);
        System.Console.WriteLine(FormatStats($"  ↳ Long (>{LongRunningThresholdSeconds}s)", longStats));
        System.Console.WriteLine();
    }

    private static HeuristicStats CollectStats(IEnumerable<Sample> samples, System.Func<Sample, bool> chooseCandidate)
    {
        double totalRuntime = 0;
        double totalLongRuntime = 0;
        int mistakes = 0;
        int total = 0;
        int totalLong = 0;

        foreach(var sample in samples)
        {
            total++;
            bool pickCandidate = chooseCandidate(sample);
            totalRuntime += pickCandidate ? sample.CandidateRuntime : sample.GreedyRuntime;

            if(pickCandidate != sample.ActualCandidateFaster)
            {
                mistakes++;
            }

            if(!sample.LongRunning)
            {
                continue;
            }

            totalLong++;
            totalLongRuntime += pickCandidate ? sample.CandidateRuntime : sample.GreedyRuntime;
        }

        return new HeuristicStats(total, mistakes, total == 0 ? 0 : totalRuntime / total, totalLong, totalLong == 0 ? 0 : totalLongRuntime / totalLong);
    }

    private static void SearchThresholds(IReadOnlyList<Sample> samples)
    {
        var best = new List<(int removable, int margin, int greedyCount, HeuristicStats stats)>();

        for(int removable = 8; removable <= 25; removable++)
        {
            for(int margin = 4; margin <= 20; margin++)
            {
                for(int initialFixedCount = 25; initialFixedCount <= 50; initialFixedCount++)
                {
                    var stats = CollectStats(samples, s =>
                        s.Parameters.TotalRemovable >= removable &&
                        s.Parameters.InitialFixedCount >= initialFixedCount &&
                        s.Parameters.RemainingMargin <= margin);

                    best.Add((removable, margin, stats.Total - stats.Mistakes, stats));
                }
            }
        }

        var top = best
            .OrderBy(s => s.stats.AverageRuntimeSeconds)
            .ThenBy(s => s.stats.Mistakes)
            .Take(5);

        System.Console.WriteLine("Top threshold combinations (Candidate if TotalRemovable >= r && RemainingMargin <= m):");
        foreach(var entry in top)
        {
            System.Console.WriteLine(
                $"  r>={entry.removable}, m<={entry.margin}: " +
                $"{entry.stats.AverageRuntimeSeconds:F3}s avg, " +
                $"{entry.stats.Mistakes}/{entry.stats.Total} misclass., " +
                $"Long avg {entry.stats.LongAverageRuntimeSeconds:F3}s");
        }
        System.Console.WriteLine();
    }

    private static string FormatStats(string title, HeuristicStats stats)
    {
        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine($"  Avg runtime.......: {stats.AverageRuntimeSeconds:F3}s");
        builder.AppendLine($"  Misclassifications: {stats.Mistakes}/{stats.Total}");
        builder.AppendLine($"  Long cases handled: {stats.LongCount}, avg {stats.LongAverageRuntimeSeconds:F3}s");
        return builder.ToString();
    }

    private sealed record Sample(bool XSudoku, double GreedyRuntime, double CandidateRuntime, double TotalRuntime, BaseProblem.AlgorithmParameters Parameters, bool PredictedCandidate, bool ActualCandidateFaster)
    {
        public bool LongRunning => System.Math.Max(GreedyRuntime, CandidateRuntime) >= LongRunningThresholdSeconds;
    }

    private sealed record HeuristicStats(int Total, int Mistakes, double AverageRuntimeSeconds, int LongCount, double LongAverageRuntimeSeconds);
}

internal sealed record TestResult(string Puzzle, string MinimalProblem, int Diff, string Solution, double TotalRuntime, double GreedyRuntime, double CandidateRuntime, bool XSudoku, BaseProblem.AlgorithmParameters Parameters);
