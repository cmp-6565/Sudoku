using Sudoku.Core;

namespace Sudoku.Application;

public static class SeverityCategoryExtensions
{
    public static string ToDisplayText(this SeverityCategory category) => category switch
    {
        SeverityCategory.Trivial => Resources.Trivial,
        SeverityCategory.Easy => Resources.Easy,
        SeverityCategory.Intermediate => Resources.Intermediate,
        SeverityCategory.Hard => Resources.Hard,
        _ => "-"
    };
}