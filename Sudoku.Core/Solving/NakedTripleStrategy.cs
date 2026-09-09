// Sudoku.Core/Solving/NakedTripleStrategy.cs
namespace Sudoku.Core.Solving;

/// <summary>
/// Detects a naked triple pattern, where exactly three cells in the same unit share the same three candidate values.
/// This restricts the candidate set enough to safely eliminate those values from other cells in the unit.
/// </summary>
public sealed class NakedTripleStrategy: NakedSubsetStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NakedTripleStrategy"/> class.
    /// </summary>
    public NakedTripleStrategy() : base(subsetSize: 3) { }

    /// <inheritdoc />
    public override string Name => "Naked Triple";

    /// <inheritdoc />
    public override int Difficulty => 3;
}