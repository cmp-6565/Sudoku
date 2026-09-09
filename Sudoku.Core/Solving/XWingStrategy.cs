// Sudoku.Core/Solving/XWingStrategy.cs
namespace Sudoku.Core.Solving;

/// <summary>
/// Detects X-Wing patterns, where a candidate value is restricted to the same two rows and two columns.
/// When the pattern is complete, that candidate can be eliminated from the remaining cells in those columns or rows.
/// </summary>
public sealed class XWingStrategy: FishStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XWingStrategy"/> class.
    /// </summary>
    public XWingStrategy() : base(size: 2) { }

    /// <inheritdoc />
    public override string Name => "X-Wing";

    /// <inheritdoc />
    public override int Difficulty => 5;
}