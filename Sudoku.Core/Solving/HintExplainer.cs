// Sudoku.Core/Solving/HintExplainer.cs
#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.Core.Solving;

/// <summary>
/// Ordnet Zellen, die eine Lösungstechnik benötigen, die einfachste dafür anwendbare Technik zu.
/// Arbeitet auf dem aktuellen Kandidatenzustand (kein mehrstufiges Voraus-Simulieren) –
/// entspricht damit "welche Technik hilft mir jetzt an dieser Zelle weiter", nicht
/// zwingend "welche Technik löst diese Zelle direkt".
/// </summary>
public sealed class HintExplainer
{
    private readonly IReadOnlyList<ISolvingStrategy> strategies;

    public HintExplainer(IReadOnlyList<ISolvingStrategy>? strategies = null)
    {
        this.strategies = (strategies ?? DefaultStrategies()).OrderBy(s => s.Difficulty).ToList();
    }

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
    /// Liefert für jede der übergebenen Zellen (typischerweise die "orangenen" Hint-Zellen)
    /// den Namen der einfachsten Technik, die für diese Zelle aktuell greift.
    /// Zellen, für die keine der bekannten Techniken (noch) etwas findet, fehlen im Ergebnis.
    /// </summary>
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