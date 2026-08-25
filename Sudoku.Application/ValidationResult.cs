using System.Collections.Generic;

namespace Sudoku.Application;

/// <summary>
/// Result object returned after parsing and validating an externally provided puzzle grid.
/// Contains a validity flag, an optional message and a list of specific cell errors.
/// </summary>
public class ValidationResult
{
    /// <summary>Represents a validation error for a specific cell.</summary>
    public struct Error
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public string Message { get; set; }
    }

    /// <summary>True when no validation errors were found.</summary>
    public bool IsValid { get; set; }
    /// <summary>Optional human-readable message about the validation result.</summary>
    public string Message { get; set; }
    /// <summary>List of cell-level validation errors.</summary>
    public List<Error> Errors { get; set; }

    /// <summary>
    /// Adds an error entry to the validation result.
    /// </summary>
    /// <param name="error">The error to add.</param>
    public void AddError(Error error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Creates a new empty ValidationResult that is considered valid by default.
    /// </summary>
    public ValidationResult()
    {
        IsValid = true;
        Message = string.Empty;
        Errors = new List<Error>();
    }
}
