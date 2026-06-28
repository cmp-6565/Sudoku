using System;

namespace Sudoku;

/// <summary>
/// Represents the core data of a Sudoku cell including its value and coordinates.
/// </summary>
[Serializable]
internal class CoreValue
{
    private Byte content = Values.Undefined;
    private int row = 0;
    private int col = 0;
    private String unformated = "";

    /// <summary>
    /// Gets or sets the unformatted string representation of the cell value.
    /// </summary>
    public String UnformatedValue
    {
        get { return unformated; }
        set { unformated = value; }
    }

    /// <summary>
    /// Gets or sets the numeric value of the cell (1-9 or undefined).
    /// </summary>
    public Byte CellValue
    {
        get { return this.content; }
        set { this.content = value; }
    }

    /// <summary>
    /// Gets or sets the row coordinate (0-based index) of the cell.
    /// </summary>
    public int Row
    {
        get { return row; }
        set { row = value; }
    }

    /// <summary>
    /// Gets or sets the column coordinate (0-based index) of the cell.
    /// </summary>
    public int Col
    {
        get { return col; }
        set { col = value; }
    }
}
