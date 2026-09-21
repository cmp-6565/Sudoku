#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Core.Solving;

/// <summary>
/// Selects, for each cell that requires an explanation, the simplest applicable solving technique available
/// in the current candidate state. This analyzer works on the current board state only and does not perform
/// multi-step look-ahead or simulation. In other words, it answers: "Which technique helps me most right now?"
/// rather than "Which technique will directly solve this cell in a future step?"
/// </summary>
public sealed class HintExplainer
{
    private readonly IReadOnlyList<ISolvingStrategy> strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="HintExplainer"/> class.
    /// </summary>
    /// <param name="strategies">
    /// An optional strategy list. When omitted, the default strategy set is used and ordered by difficulty.
    /// </param>
    public HintExplainer(IReadOnlyList<ISolvingStrategy>? strategies = null)
    {
        this.strategies = (strategies ?? DefaultStrategies()).OrderBy(s => s.Difficulty).ToList();
    }

    /// <summary>
    /// Gets the default strategy set in the preferred evaluation order.
    /// </summary>
    /// <returns>A read-only list of strategies ordered from easiest to most advanced.</returns>
    public static IReadOnlyList<ISolvingStrategy> DefaultStrategies() => new ISolvingStrategy[]
    {
        new HiddenSingleStrategy(),
        new NakedPairStrategy(),
        new NakedTripleStrategy(),
        new PointingPairStrategy(),
        new XWingStrategy(),
        new SwordfishStrategy(),
    };

    /// <summary>
    /// Explains the supplied positions by assigning the simplest currently applicable solving technique to each.
    /// </summary>
    /// <param name="matrix">The current Sudoku matrix to analyze.</param>
    /// <param name="positionsNeedingExplanation">
    /// The coordinates that require an explanation, usually cells highlighted as hint targets.
    /// </param>
    /// <returns>
    /// A map whose keys are the positions to explain and whose values are the matching strategy findings.
    /// Positions that cannot be explained by any known strategy are omitted.
    /// </returns>
    public IReadOnlyDictionary<(int Row, int Col), StrategyFinding> ExplainPositions(BaseMatrix matrix, IEnumerable<(int Row, int Col)> positionsNeedingExplanation)
    {
        var remaining = new HashSet<(int Row, int Col)>(positionsNeedingExplanation);
        var result = new Dictionary<(int Row, int Col), StrategyFinding>();

        foreach(ISolvingStrategy strategy in strategies)
        {
            if(remaining.Count == 0) break;

            foreach(StrategyFinding finding in strategy.FindAll(matrix))
            {
                foreach(BaseCell cell in finding.AffectedCells)
                {
                    var pos = (cell.Row, cell.Col);
                    if(remaining.Remove(pos))
                        result[pos] = finding;
                }
            }
        }

        return result;
    }
}