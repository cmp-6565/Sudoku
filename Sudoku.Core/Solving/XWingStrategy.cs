// Sudoku.Core/Solving/XWingStrategy.cs
namespace Sudoku.Core.Solving;

public sealed class XWingStrategy: FishStrategyBase
{
    public XWingStrategy() : base(size: 2) { }
    public override string Name => "X-Wing";
    public override int Difficulty => 5;
}