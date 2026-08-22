using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using static Sudoku.Core.BaseProblem;

namespace Sudoku.Core.Minimizing;

/// <summary>
/// Wählt per Heuristik zwischen Candidate- und Greedy-Strategie (entspricht dem bisherigen
/// MinimizeAlgorithm.Calculate) und delegiert an die passende Strategie.
/// </summary>
internal sealed class AutoMinimizeStrategy(CandidateMinimizeStrategy candidateStrategy, GreedyMinimizeStrategy greedyStrategy): IMinimizeStrategy
{
    public Task<BaseProblem?> MinimizeAsync(BaseProblem problem, GivenState initialState, GivenState greedyState, int maxSeverity, Dictionary<ulong, bool> cache, CancellationToken token)
    {
        IMinimizeStrategy chosen = problem.ShouldUseCandidateSearch(initialState, greedyState, out _)
            ? candidateStrategy
            : greedyStrategy;

        return chosen.MinimizeAsync(problem, initialState, greedyState, maxSeverity, cache, token);
    }
}