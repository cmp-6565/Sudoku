using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static Sudoku.Core.BaseProblem;

namespace Sudoku.Core.Minimizing;

/// <summary>
/// Minimiert die vorgegebenen Werte (Givens) mittels kandidatengeführter, rekursiver Suche.
/// Entspricht dem bisherigen MinimizeAlgorithm.Candidate.
/// </summary>
internal sealed class CandidateMinimizeStrategy: IMinimizeStrategy
{
    public async Task<BaseProblem?> MinimizeAsync(BaseProblem problem, GivenState initialState, GivenState greedyState, int maxSeverity, Dictionary<ulong, bool> cache, CancellationToken token)
    {
        problem.ResetMatrix();

        problem.minimalProblem = problem.Clone();

        List<BaseCell> candidates = await GetCandidates(problem, problem.Matrix.Cells, 0, CancellationToken.None)
            .ConfigureAwait(false);
        candidates.Sort(new NeighborCountComparer());

        if(await MinimizeWithCandidatesRecursive(problem, candidates, maxSeverity, token).ConfigureAwait(false))
        {
            problem.minimalProblem.SeverityLevel = float.NaN;

            await problem.minimalProblem.RunSolver(2, token).ConfigureAwait(false);

            return problem.minimalProblem.NumberOfSolutions == 1 ? problem.minimalProblem : null;
        }

        return null;
    }

    /// <summary>
    /// Rekursive Schleife für die kandidatenbasierte Minimierung. Versucht, Kandidaten-Zellen zu
    /// entfernen und verfeinert das Ergebnis rekursiv.
    /// </summary>
    private async Task<bool> MinimizeWithCandidatesRecursive(BaseProblem problem, List<BaseCell>? candidates, int maxSeverity, CancellationToken token)
    {
        if(candidates == null) return true;
        if(problem.nValues <= problem.Matrix.MinimumValues) return true;

        int start = 0;
        foreach(BaseCell cell in candidates)
        {
            if(token.IsCancellationRequested) return false;
            if(problem.SeverityLevelInt > maxSeverity) return false;

            if(problem.nValues - (candidates.Count - start) < problem.minimalProblem!.nValues)
            {
                problem.OnTestCell(problem, cell);
                byte cellValue = cell.CellValue;
                problem.SetValue(cell, Values.Undefined);

                problem.ResetMatrix();
                if(problem.nValues < problem.minimalProblem.nValues)
                    problem.minimalProblem = problem.Clone();

                problem.OnMinimizing(problem, problem.minimalProblem);

                var nextCandidates = await GetCandidates(problem, candidates, ++start, token).ConfigureAwait(false);

                if(token.IsCancellationRequested) return false;
                if(!await MinimizeWithCandidatesRecursive(problem, nextCandidates, maxSeverity, token).ConfigureAwait(false))
                    return false;

                problem.OnResetCell(problem, cell);
                problem.ResetMatrix();
                problem.SetValue(cell, cellValue);
            }
        }

        return true;
    }

    /// <summary>
    /// Baut die Menge der Kandidaten-Zellen aus der übergebenen Quelle ab Index 'start' auf.
    /// Ein Kandidat ist eine Zelle, die entfernt werden kann, während das Rätsel weiterhin
    /// eindeutig lösbar bleibt.
    /// </summary>
    private async Task<List<BaseCell>> GetCandidates(BaseProblem problem, List<BaseCell> source, int start, CancellationToken token)
    {
        List<BaseCell> candidates = new List<BaseCell>();

        for(int i = start; i < source.Count; i++)
        {
            if(problem.nValues - candidates.Count - (source.Count - i) > problem.minimalProblem!.nValues)
                return new List<BaseCell>();

            byte cellValue = source[i].CellValue;
            if(cellValue != Values.Undefined)
            {
                problem.SetValue(source[i], Values.Undefined);
                if(source[i].DefinitiveValue == cellValue)
                    candidates.Add(source[i]);
                else
                {
                    if(token.IsCancellationRequested) return new List<BaseCell>();

                    await problem.RunSolver(2, token).ConfigureAwait(false);

                    if(problem.NumberOfSolutions == 1) candidates.Add(source[i]);
                }
                problem.ResetMatrix();
                problem.SetValue(source[i], cellValue);
            }

            if(token.IsCancellationRequested) return new List<BaseCell>();
        }

        return candidates;
    }
}