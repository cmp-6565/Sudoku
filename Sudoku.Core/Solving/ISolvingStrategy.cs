// Sudoku.Core/Solving/ISolvingStrategy.cs
#nullable enable
using System.Collections.Generic;

namespace Sudoku.Core.Solving;

/// <summary>
/// Represents a named, human-readable solving technique such as "Hidden Single" or "X-Wing".
/// Strategies inspect the current state of a <see cref="BaseMatrix"/> without modifying values or candidate sets.
/// The actual elimination or assignment work is carried out by the solver, while this interface is intended
/// for detection and explanatory reporting only.
/// </summary>
public interface ISolvingStrategy
{
    /// <summary>
    /// Gets the display name of the technique, for example "Naked Pair".
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a rough difficulty rating used to sort strategies. Lower values are checked first so that the simplest
    /// applicable explanation is discovered before more advanced patterns are evaluated.
    /// </summary>
    int Difficulty { get; }

    /// <summary>
    /// Finds all occurrences of this technique that are currently applicable in the provided matrix.
    /// </summary>
    /// <param name="matrix">The Sudoku matrix to inspect.</param>
    /// <returns>A collection of all findings produced by the strategy in the current state.</returns>
    IReadOnlyList<StrategyFinding> FindAll(BaseMatrix matrix);
}

/// <summary>
/// Represents a single finding produced by a solving strategy. The record stores the pattern-defining cells and the
/// cells affected by that pattern, along with a textual explanation suitable for hints and UI display.
/// </summary>
public sealed record StrategyFinding(string StrategyName, IReadOnlyList<BaseCell> KeyCells, IReadOnlyList<BaseCell> AffectedCells, string Description);