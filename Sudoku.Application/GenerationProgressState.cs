using System;

namespace Sudoku.Application;

/// <summary>
/// Represents progress information emitted during generation or solving.
/// </summary>
public class GenerationProgressState
{
    /// <summary>Number of solver/generation passes performed so far.</summary>
    public long PassCount { get; set; }
    /// <summary>Number of solutions found so far.</summary>
    public long SolutionCount { get; set; }
    /// <summary>Elapsed time since the operation started.</summary>
    public TimeSpan Elapsed { get; set; }
    /// <summary>Row index of the current cell related to the progress update.</summary>
    public int Row { get; set; }
    /// <summary>Column index of the current cell related to the progress update.</summary>
    public int Col { get; set; }
    /// <summary>Value of the cell related to the progress update.</summary>
    public byte Value { get; set; }
    /// <summary>Indicates whether the reported cell is read-only.</summary>
    public bool ReadOnly { get; set; }
    /// <summary>Optional status text describing the current operation.</summary>
    public string? StatusText { get; set; }
}