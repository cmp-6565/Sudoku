using System;

namespace Sudoku;

internal class SudokuSaveState
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string GridData { get; set; }
    public TimeSpan Time { get; set; }
    public string Comment { get; set; }
    public string Candidates { get; set; }
}
