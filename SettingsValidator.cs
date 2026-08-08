#nullable enable
using System;

namespace Sudoku;

/// <summary>
/// Provides validation and sanitization for Sudoku application settings.
/// Ensures settings are within valid ranges and prevents invalid operational states.
/// </summary>
public static class SettingsValidator
{
    // --- Validation Constants ---

    /// <summary>Minimum allowed booklet size (at least 1 page).</summary>
    public const int MinBookletSize = 1;

    /// <summary>Maximum allowed booklet size (practical limit for memory).</summary>
    public const int MaxBookletSize = 1000;

    /// <summary>Minimum allowed solutions count.</summary>
    public const int MinSolutionCount = 1;

    /// <summary>Maximum allowed solutions count.</summary>
    public const int MaxSolutionCount = 10000;

    /// <summary>Minimum allowed puzzle grid size.</summary>
    public const int MinGridSize = 1;

    /// <summary>Maximum allowed puzzle grid size (practical limit).</summary>
    public const int MaxGridSize = 16;

    /// <summary>Minimum value count for problem generation.</summary>
    public const int MinValueCount = 1;

    /// <summary>Maximum value count for problem generation.</summary>
    public const int MaxValueCount = 81;

    /// <summary>Minimum horizontal layout cells.</summary>
    public const int MinHorizontalCells = 1;

    /// <summary>Maximum horizontal layout cells (practical limit).</summary>
    public const int MaxHorizontalCells = 20;

    /// <summary>Minimum auto-pause lag in milliseconds.</summary>
    public const decimal MinAutoPauseLag = 0;

    /// <summary>Maximum auto-pause lag in milliseconds.</summary>
    public const decimal MaxAutoPauseLag = 60000; // 60 seconds

    /// <summary>Minimum contrast level.</summary>
    public const int MinContrast = 0;

    /// <summary>Maximum contrast level.</summary>
    public const int MaxContrast = 100;

    // --- Validation Methods ---

    /// <summary>
    /// Validates and constrains an integer value within a specified range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="minimum">The minimum allowed value (inclusive).</param>
    /// <param name="maximum">The maximum allowed value (inclusive).</param>
    /// <param name="paramName">The parameter name for error reporting.</param>
    /// <returns>The value constrained within the valid range.</returns>
    /// <exception cref="ArgumentException">Thrown when minimum > maximum.</exception>
    public static int ValidateRange(int value, int minimum, int maximum, string paramName)
    {
        ArgumentNullException.ThrowIfNull(paramName);
        if (minimum > maximum)
        {
            throw new ArgumentException($"Minimum ({minimum}) cannot be greater than maximum ({maximum}).", paramName);
        }

        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    /// <summary>
    /// Validates and constrains a decimal value within a specified range.
    /// </summary>
    public static decimal ValidateRange(decimal value, decimal minimum, decimal maximum, string paramName)
    {
        ArgumentNullException.ThrowIfNull(paramName);
        if (minimum > maximum)
        {
            throw new ArgumentException($"Minimum ({minimum}) cannot be greater than maximum ({maximum}).", paramName);
        }

        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    /// <summary>
    /// Validates a booklet size setting.
    /// </summary>
    /// <param name="size">The booklet size to validate.</param>
    /// <returns>The validated booklet size within acceptable range.</returns>
    public static int ValidateBookletSize(int size)
    {
        return ValidateRange(size, MinBookletSize, MaxBookletSize, nameof(size));
    }

    /// <summary>
    /// Validates the maximum solutions count for the solver.
    /// </summary>
    /// <param name="maxSolutions">The maximum solutions count to validate.</param>
    /// <returns>The validated solutions count within acceptable range.</returns>
    public static int ValidateMaxSolutions(int maxSolutions)
    {
        return ValidateRange(maxSolutions, MinSolutionCount, MaxSolutionCount, nameof(maxSolutions));
    }

    /// <summary>
    /// Validates the minimum value count for puzzle generation.
    /// </summary>
    /// <param name="minValues">The minimum values count to validate.</param>
    /// <returns>The validated values count within acceptable range.</returns>
    public static int ValidateMinValues(int minValues)
    {
        return ValidateRange(minValues, MinValueCount, MaxValueCount, nameof(minValues));
    }

    /// <summary>
    /// Validates the puzzle grid size.
    /// </summary>
    /// <param name="size">The grid size to validate.</param>
    /// <returns>The validated grid size within acceptable range.</returns>
    public static int ValidateGridSize(int size)
    {
        return ValidateRange(size, MinGridSize, MaxGridSize, nameof(size));
    }

    /// <summary>
    /// Validates the horizontal layout cell count.
    /// </summary>
    /// <param name="count">The horizontal cell count to validate.</param>
    /// <returns>The validated cell count within acceptable range.</returns>
    public static int ValidateHorizontalCellCount(int count)
    {
        return ValidateRange(count, MinHorizontalCells, MaxHorizontalCells, nameof(count));
    }

    /// <summary>
    /// Validates the trace frequency for diagnostics.
    /// </summary>
    /// <param name="frequency">The trace frequency to validate.</param>
    /// <returns>The validated frequency within acceptable range.</returns>
    public static int ValidateTraceFrequency(int frequency)
    {
        return ValidateRange(frequency, 0, 10000, nameof(frequency));
    }

    /// <summary>
    /// Validates the auto-pause lag setting.
    /// </summary>
    /// <param name="lag">The lag duration in milliseconds to validate.</param>
    /// <returns>The validated lag within acceptable range.</returns>
    public static decimal ValidateAutoPauseLag(decimal lag)
    {
        return ValidateRange(lag, MinAutoPauseLag, MaxAutoPauseLag, nameof(lag));
    }

    /// <summary>
    /// Validates a contrast level setting (0-100 percent).
    /// </summary>
    /// <param name="contrast">The contrast level to validate.</param>
    /// <returns>The validated contrast level within acceptable range.</returns>
    public static int ValidateContrast(int contrast)
    {
        return ValidateRange(contrast, MinContrast, MaxContrast, nameof(contrast));
    }

    /// <summary>
    /// Validates a severity level setting.
    /// </summary>
    /// <param name="severity">The severity level to validate.</param>
    /// <returns>The validated severity level within acceptable range.</returns>
    public static int ValidateSeverityLevel(int severity)
    {
        return ValidateRange(severity, 0, 10, nameof(severity));
    }

    /// <summary>
    /// Validates a string setting and provides a default if null or empty.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="defaultValue">The default value if input is null or empty.</param>
    /// <returns>The validated string or default value.</returns>
    public static string ValidateString(string? value, string defaultValue = "")
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    /// <summary>
    /// Validates a directory path exists or can be created.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <param name="defaultPath">The default path if validation fails.</param>
    /// <returns>A valid directory path.</returns>
    public static string ValidateDirectoryPath(string? path, string defaultPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return defaultPath;
        }

        try
        {
            var dirInfo = new System.IO.DirectoryInfo(path);

            if (!dirInfo.Exists)
            {
                // Try to create the directory
                dirInfo.Create();
            }

            return dirInfo.FullName;
        }
        catch (System.IO.IOException)
        {
            // If I/O operations fail, return default path
            return defaultPath;
        }
        catch (UnauthorizedAccessException)
        {
            // If access is denied, return default path
            return defaultPath;
        }
        catch (Exception)
        {
            // Unexpected error: return default path
            return defaultPath;
        }
    }
}