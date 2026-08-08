#nullable enable
using System;

namespace Sudoku;

/// <summary>
/// Represents a complete Sudoku solution with all cells filled.
/// All values in a Solution are treated as fixed and read-only.
/// </summary>
[Serializable]
public class Solution: Values
{
    private ISudokuSettings settings;

    private byte[][] values;

    /// <summary>
    /// Initializes a new instance of the Solution class with the specified application settings.
    /// </summary>
    /// <param name="settings">The application settings that define the Sudoku size and configuration.</param>
    public Solution(ISudokuSettings settings)
    {
        values = new byte[WinFormsSettings.SudokuSize][];
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            values[row] = new byte[WinFormsSettings.SudokuSize];
        Init();
        this.settings = settings;
    }

    /// <summary>
    /// Initializes a new instance of the Solution class by copying data from another Solution instance.
    /// </summary>
    /// <param name="clone">The Solution instance to copy from.</param>
    protected Solution(Solution clone) : base(clone)
    {
        this.settings = clone.settings;
        this.Counter = clone.Counter;
        this.values = new byte[clone.values.Length][];
        for(int i = 0; i < clone.values.Length; i++)
        {
            this.values[i] = (byte[])clone.values[i].Clone();
        }
    }

    /// <summary>
    /// Creates a deep copy of the current Solution instance.
    /// </summary>
    /// <returns>A new Solution instance that is a copy of the current object.</returns>
    public override object Clone()
    {
        return new Solution(this);
    }

    /// <summary>
    /// Sets the value of a cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <param name="value">The value to set (1-9 or Undefined).</param>
    /// <param name="fixedValue">Ignored; all values in a Solution are always treated as fixed.</param>
    /// <exception cref="InvalidSudokuValueException">Thrown if the value or coordinates are out of range.</exception>
    public override void SetValue(int row, int col, byte value, Boolean fixedValue)
    {
        if(((value < 1 || value > WinFormsSettings.SudokuSize) && value != Values.Undefined) || row < 0 || col < 0 || row > WinFormsSettings.SudokuSize || col > WinFormsSettings.SudokuSize)
            throw new InvalidSudokuValueException();
        values[row][col] = value;
    }

    /// <summary>
    /// Gets the value of a cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>The cell value (1-9 or Undefined).</returns>
    public override byte GetValue(int row, int col)
    {
        return values[row][col];
    }

    /// <summary>
    /// Determines if the cell at the specified row and column contains a fixed value.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>Always returns true as all values in a Solution are fixed.</returns>
    public override Boolean FixedValue(int row, int col)
    {
        return true;
    }

    /// <summary>
    /// Determines if the cell at the specified row and column contains a computed value.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>Always returns false as all values in a Solution are pre-computed.</returns>
    public override Boolean ComputedValue(int row, int col)
    {
        return false;
    }

    /// <summary>
    /// Determines if the cell at the specified row and column is read-only.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>Always returns true as all values in a Solution are read-only.</returns>
    public override Boolean ReadOnly(int row, int col)
    {
        return true;
    }

    /// <summary>
    /// Initializes the Solution grid with all cells set to undefined.
    /// </summary>
    public override void Init()
    {
        int row, col;

        for(row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(col = 0; col < WinFormsSettings.SudokuSize; col++)
                values[row][col] = Values.Undefined;
    }
}
