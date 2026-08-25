// Sudoku.Core/Solving/SwordfishStrategy.cs
namespace Sudoku.Core.Solving;

public sealed class SwordfishStrategy: FishStrategyBase
{
    public SwordfishStrategy() : base(size: 3) { }
    public override string Name => "Swordfish";
    public override int Difficulty => 6;
}