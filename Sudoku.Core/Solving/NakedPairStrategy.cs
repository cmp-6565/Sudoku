// Sudoku.Core/Solving/NakedPairStrategy.cs
namespace Sudoku.Core.Solving;

/// <summary>
/// Detects a naked pair pattern, where exactly two cells in the same unit contain the same two candidate values.
/// Once the pattern is identified, those values can be removed from the remaining cells in that unit.
/// </summary>
public sealed class NakedPairStrategy: NakedSubsetStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NakedPairStrategy"/> class.
    /// </summary>
    public NakedPairStrategy() : base(subsetSize: 2) { }

    /// <inheritdoc />
    public override string Name => "Naked Pair";

    /// <inheritdoc />
    public override int Difficulty => 2;
}