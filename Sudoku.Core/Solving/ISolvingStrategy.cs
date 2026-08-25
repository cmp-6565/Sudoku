// Sudoku.Core/Solving/ISolvingStrategy.cs
#nullable enable
using System.Collections.Generic;

namespace Sudoku.Core.Solving;

/// <summary>
/// Eine benannte, menschlich nachvollziehbare Lösungstechnik (z. B. "Hidden Single", "X-Wing").
/// Untersucht den aktuellen Zustand einer <see cref="BaseMatrix"/> rein lesend – verändert nie
/// selbst Kandidaten oder Werte. Die tatsächliche Anwendung (Kandidaten eliminieren, Wert setzen)
/// bleibt Aufgabe des Solvers; diese Klassen dienen ausschließlich der Erklärung/Anzeige.
/// </summary>
public interface ISolvingStrategy
{
    /// <summary>Anzeigename der Technik, z. B. "Naked Pair".</summary>
    string Name { get; }

    /// <summary>
    /// Grober Schwierigkeitsgrad zur Sortierung – niedrigere Werte werden zuerst geprüft,
    /// damit stets die einfachste anwendbare Erklärung gefunden wird (wie ein Mensch lösen würde).
    /// </summary>
    int Difficulty { get; }

    /// <summary>
    /// Findet alle aktuell im Gitter anwendbaren Vorkommen dieser Technik.
    /// </summary>
    IReadOnlyList<StrategyFinding> FindAll(BaseMatrix matrix);
}

/// <summary>
/// Ein einzelner Fund einer Lösungstechnik: die "Schlüsselzellen", die das Muster bilden,
/// und die Zellen, deren Kandidaten dadurch reduziert werden (bzw. die dadurch lösbar werden).
/// </summary>
public sealed record StrategyFinding(string StrategyName, IReadOnlyList<BaseCell> KeyCells, IReadOnlyList<BaseCell> AffectedCells, string Description);