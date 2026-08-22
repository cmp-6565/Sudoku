// Sudoku.Core/SeverityCategory.cs (neu)
namespace Sudoku.Core;

/// <summary>
/// Grobe Schweregrad-Kategorie eines Sudoku-Problems, abgeleitet aus dem
/// numerischen Schweregrad und den konfigurierten Schwellwerten.
/// </summary>
public enum SeverityCategory
{
    Undefined,
    Trivial,
    Easy,
    Intermediate,
    Hard
}