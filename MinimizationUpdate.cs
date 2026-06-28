namespace Sudoku;

/// <summary>
/// Specifies the type of minimization update that occurred.
/// </summary>
public enum MinimizationUpdateType
{
    /// <summary>A status update during problem minimization.</summary>
    Status,
    /// <summary>A cell was tested during minimization.</summary>
    TestCell,
    /// <summary>A cell was reset during minimization.</summary>
    ResetCell
}

/// <summary>
/// Represents an update event during Sudoku problem minimization.
/// Carries information about what type of update occurred and which cell or problem was affected.
/// </summary>
internal class MinimizationUpdate
{
    /// <summary>
    /// Gets or sets the type of minimization update.
    /// </summary>
    public MinimizationUpdateType Type { get; set; }

    /// <summary>
    /// Gets or sets the cell that was tested or reset (for TestCell and ResetCell updates).
    /// </summary>
    public BaseCell Cell { get; set; }

    /// <summary>
    /// Gets or sets the problem state (used for Status updates).
    /// </summary>
    public BaseProblem Problem { get; set; }
}
