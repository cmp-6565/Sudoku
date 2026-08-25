// Sudoku.Core/Solving/NakedTripleStrategy.cs
namespace Sudoku.Core.Solving;

public sealed class NakedTripleStrategy: NakedSubsetStrategyBase
{
    public NakedTripleStrategy() : base(subsetSize: 3) { }
    public override string Name => "Naked Triple";
    public override int Difficulty => 3;
}