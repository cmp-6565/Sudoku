using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Sudoku.Core.BaseProblem;

namespace Sudoku.Core.Minimizing;

internal sealed class GreedyMinimizeStrategy: IMinimizeStrategy
{
    public async Task<BaseProblem?> MinimizeAsync(BaseProblem problem, GivenState initialState, GivenState greedyState, int maxSeverity, Dictionary<ulong, bool> cache, CancellationToken token)
    {
        problem.ResetMatrix();

        if(initialState.FixedCount <= problem.Matrix.MinimumValues) return problem;

        GivenState? bestState = UpdateBestState(problem, null, greedyState);

        BaseCell[] minimizationOrder = problem.Matrix.Cells
            .Where(cell => initialState[cell.Row, cell.Col] != Values.Undefined)
            .OrderByDescending(cell => cell.FilledNeighborCount)
            .ToArray();

        GivenState? recursiveResult = await MinimizeGreedyRecursive(
            problem, initialState, minimizationOrder, 0, maxSeverity, cache, token).ConfigureAwait(false);

        if(recursiveResult.HasValue)
            bestState = UpdateBestState(problem, bestState, recursiveResult.Value);

        GivenState finalState = bestState ?? greedyState;
        problem.minimalProblem = problem.Materialize(finalState);
        problem.minimalProblem.SeverityLevel = float.NaN;
        await problem.minimalProblem.RunSolver(2, token).ConfigureAwait(false);

        return problem.minimalProblem.NumberOfSolutions == 1 ? problem.minimalProblem : null;
    }

    private async Task<GivenState?> MinimizeGreedyRecursive(BaseProblem problem, GivenState state, BaseCell[] order, int startIndex, int maxSeverity, Dictionary<ulong, bool> cache, CancellationToken token)
    {
        if(token.IsCancellationRequested) return null;

        if(state.FixedCount <= problem.Matrix.MinimumValues)
            return await problem.IsUnique(state, maxSeverity, cache, token).ConfigureAwait(false) ? state : null;

        GivenState? best = null;

        for(int i = startIndex; i < order.Length; i++)
        {
            BaseCell cell = order[i];
            if(state[cell.Row, cell.Col] == Values.Undefined) continue;

            problem.OnTestCell(problem, cell);

            GivenState reducedState = state.WithRemoved(cell.Row, cell.Col);
            if(await problem.IsUnique(reducedState, maxSeverity, cache, token).ConfigureAwait(false))
            {
                best = UpdateBestState(problem, best, reducedState);
                if(best.HasValue && best.Value.FixedCount <= problem.Matrix.MinimumValues)
                {
                    problem.OnResetCell(problem, cell);
                    return best;
                }

                GivenState? candidate = await MinimizeGreedyRecursive(
                    problem, reducedState, order, i + 1, maxSeverity, cache, token).ConfigureAwait(false);
                if(candidate.HasValue)
                {
                    best = UpdateBestState(problem, best, candidate.Value);
                    if(best.HasValue && best.Value.FixedCount <= problem.Matrix.MinimumValues)
                    {
                        problem.OnResetCell(problem, cell);
                        return best;
                    }
                }
            }

            problem.OnResetCell(problem, cell);
        }

        return best;
    }

    private static GivenState? UpdateBestState(BaseProblem problem, GivenState? currentBest, GivenState candidate)
    {
        if(!currentBest.HasValue || candidate.FixedCount < currentBest.Value.FixedCount)
        {
            problem.minimalProblem = problem.Materialize(candidate);
            problem.OnMinimizing(problem, problem.minimalProblem);
            return candidate;
        }

        return currentBest;
    }
}