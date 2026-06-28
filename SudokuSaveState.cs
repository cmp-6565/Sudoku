using System;

namespace Sudoku;

/// <summary>
/// Represents the saved state of a Sudoku puzzle for restoration purposes.
/// Contains all necessary data to reconstruct a previous puzzle state.
/// </summary>
internal class SudokuSaveState
{
    /// <summary>
    /// Gets or sets the unique identifier for this saved state.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of Sudoku (Standard or X-Sudoku).
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the serialized grid data containing all cell values.
    /// </summary>
    public string GridData { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time for solving this puzzle.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Gets or sets any user-provided comment for this puzzle state.
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// Gets or sets the serialized candidate values for each cell.
    /// </summary>
    public string Candidates { get; set; }
}
