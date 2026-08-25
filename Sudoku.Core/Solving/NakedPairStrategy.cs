// Sudoku.Core/Solving/NakedPairStrategy.cs
namespace Sudoku.Core.Solving;

public sealed class NakedPairStrategy: NakedSubsetStrategyBase
{
    public NakedPairStrategy() : base(subsetSize: 2) { }
    public override string Name => "Naked Pair";
    public override int Difficulty => 2;
}