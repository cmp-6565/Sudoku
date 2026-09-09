using System.Globalization;
using System.Resources;

namespace Sudoku.Core.Solving;

/// <summary>
/// Provides localized description text for strategy findings using the current UI culture.
/// </summary>
internal static class StrategyFindingResourceProvider
{
    private const string HiddenSingleDescriptionKey = "HiddenSingleDescription";
    private const string NakedSubsetDescriptionKey = "NakedSubsetDescription";
    private const string FishDescriptionKey = "FishDescription";
    private const string PointingPairDescriptionKey = "PointingPairDescription";

    private static readonly ResourceManager ResourceManager =
        new("Sudoku.Core.Properties.Resources", typeof(StrategyFindingResourceProvider).Assembly);

    public static string GetHiddenSingleDescription(string unitLabel, int value) =>
        Format(HiddenSingleDescriptionKey, unitLabel, value);

    public static string GetNakedSubsetDescription(string cellList, string candidateList) =>
        Format(NakedSubsetDescriptionKey, cellList, candidateList);

    public static string GetFishDescription(int value, string strategyName, int count, string axisName, string oppositeAxisName) =>
        Format(FishDescriptionKey, value, strategyName, count, axisName, oppositeAxisName);

    public static string GetPointingPairDescription(int value) =>
        Format(PointingPairDescriptionKey, value);

    private static string Format(string resourceKey, params object[] args)
    {
        string? format = ResourceManager.GetString(resourceKey, CultureInfo.CurrentUICulture);

        if(string.IsNullOrWhiteSpace(format))
            format = ResourceManager.GetString(resourceKey, CultureInfo.InvariantCulture);

        if(string.IsNullOrWhiteSpace(format))
            throw new System.InvalidOperationException($"Missing resource string for '{resourceKey}'.");

        return string.Format(CultureInfo.CurrentCulture, format, args);
    }
}