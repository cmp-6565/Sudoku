// Sudoku.Core/Solving/SwordfishStrategy.cs
namespace Sudoku.Core.Solving;

/// <summary>
/// Detects Swordfish patterns, where a candidate value is confined to the same three rows and three columns.
/// This pattern allows the candidate to be eliminated from the corresponding crossing lines outside the fish.
/// </summary>
public sealed class SwordfishStrategy: FishStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwordfishStrategy"/> class.
    /// </summary>
    public SwordfishStrategy() : base(size: 3) { }

    /// <inheritdoc />
    public override string Name => "Swordfish";

    /// <inheritdoc />
    public override int Difficulty => 6;
}