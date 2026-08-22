#nullable enable
using System;

namespace Sudoku.Core;

/// <summary>
/// Exception thrown when an invalid value is assigned to a Sudoku cell.
/// </summary>
[Serializable()]
public class InvalidSudokuValueException: Exception
{
    /// <summary>
    /// Initializes a new instance of the InvalidSudokuValueException class with a specified error message.
    /// </summary>
    /// <param name="s">The message that describes the error.</param>
    public InvalidSudokuValueException(String s) : base(s) { }

    /// <summary>
    /// Initializes a new instance of the InvalidSudokuValueException class with a specified error message and inner exception.
    /// </summary>
    /// <param name="s">The message that describes the error.</param>
    /// <param name="ex">The exception that is the cause of the current exception.</param>
    public InvalidSudokuValueException(String s, Exception ex) : base(s, ex) { }

    /// <summary>
    /// Initializes a new instance of the InvalidSudokuValueException class.
    /// </summary>
    public InvalidSudokuValueException() : base() { }
}

/// <summary>
/// Exception thrown when the maximum number of solutions has been reached during solving.
/// </summary>
public class MaxResultsReached: Exception
{
    /// <summary>
    /// Initializes a new instance of the MaxResultsReached class.
    /// </summary>
    public MaxResultsReached() : base() { }
}