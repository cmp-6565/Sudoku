using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using static Sudoku.Core.BaseProblem;

namespace Sudoku.Core.Minimizing;

/// <summary>
/// Eine Strategie zum Minimieren der vorgegebenen Werte (Givens) eines Sudoku-Problems.
/// Jede Implementierung nimmt der übergebenen <see cref="BaseProblem"/>-Instanz Werte weg
/// und liefert das minimierte Ergebnis (oder null, wenn keine eindeutig lösbare Minimalvariante gefunden wurde).
/// </summary>
internal interface IMinimizeStrategy
{
    Task<BaseProblem?> MinimizeAsync(
        BaseProblem problem,
        GivenState initialState,
        GivenState greedyState,
        int maxSeverity,
        Dictionary<ulong, bool> cache,
        CancellationToken token);
}