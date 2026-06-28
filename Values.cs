using System;

namespace Sudoku;

/// <summary>
/// Abstract base class for managing Sudoku grid values and operations.
/// Provides core functionality for setting, getting, and querying cell values.
/// </summary>
[Serializable]
public abstract class Values: ICloneable
{
    private Int64 count = 0;

    /// <summary>
    /// Gets or sets a counter that tracks the number of operations or passes performed.
    /// </summary>
    public Int64 Counter
    {
        get { return count; }
        set { count = value; }
    }

    /// <summary>
    /// Initializes a new instance of the Values class.
    /// </summary>
    protected Values() { }

    /// <summary>
    /// Initializes a new instance of the Values class by copying data from another Values instance.
    /// </summary>
    /// <param name="clone">The Values instance to copy from.</param>
    protected Values(Values clone)
    {
        this.count = clone.count;
    }

    /// <summary>
    /// Constant representing an undefined cell value.
    /// </summary>
    public const byte Undefined = 0;

    /// <summary>
    /// Sets the value of a cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <param name="value">The value to set (1-9 or Undefined).</param>
    /// <param name="fixedValue">True if the value should be treated as fixed (given); false otherwise.</param>
    public abstract void SetValue(int row, int col, byte value, Boolean fixedValue);

    /// <summary>
    /// Gets the value of a cell at the specified row and column.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>The cell value (1-9 or Undefined).</returns>
    public abstract byte GetValue(int row, int col);

    /// <summary>
    /// Determines if the cell at the specified row and column contains a fixed (given) value.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>True if the cell is fixed; false otherwise.</returns>
    public abstract Boolean FixedValue(int row, int col);

    /// <summary>
    /// Determines if the cell at the specified row and column contains a computed value.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>True if the cell contains a computed value; false otherwise.</returns>
    public abstract Boolean ComputedValue(int row, int col);

    /// <summary>
    /// Determines if the cell at the specified row and column is read-only.
    /// </summary>
    /// <param name="row">The row coordinate (0-based index).</param>
    /// <param name="col">The column coordinate (0-based index).</param>
    /// <returns>True if the cell is read-only; false otherwise.</returns>
    public abstract Boolean ReadOnly(int row, int col);

    /// <summary>
    /// Initializes the grid to its default state with no values set.
    /// </summary>
    public abstract void Init();

    /// <summary>
    /// Creates a deep copy of the current instance.
    /// </summary>
    /// <returns>A new instance that is a copy of the current Values object.</returns>
    public abstract object Clone();
}
