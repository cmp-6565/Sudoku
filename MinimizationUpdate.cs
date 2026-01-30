namespace Sudoku;

public enum MinimizationUpdateType { Status, TestCell, ResetCell }
internal class MinimizationUpdate
{
    public MinimizationUpdateType Type { get; set; }
    public BaseCell Cell { get; set; }
    public BaseProblem Problem { get; set; } // Für Status-Updates
}
