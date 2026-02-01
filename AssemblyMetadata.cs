using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sudoku;

/// <summary>
/// Liest alle AssemblyMetadataAttribute-Werte der aktuellen Assembly aus.
/// </summary>
public static class AssemblyMetadata
{
    private static readonly Lazy<Dictionary<string, string>> metadata =
        new Lazy<Dictionary<string, string>>(LoadMetadata);

    /// <summary>
    /// Liefert alle Metadaten als Dictionary.
    /// </summary>
    public static IReadOnlyDictionary<string, string> All => metadata.Value;

    /// <summary>
    /// Liefert einen einzelnen Wert oder null, wenn der Schlüssel nicht existiert.
    /// </summary>
    public static string Get(string key)
    {
        metadata.Value.TryGetValue(key, out var value);
        return value;
    }

    private static Dictionary<string, string> LoadMetadata()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .GroupBy(a => a.Key)
            .ToDictionary(g => g.Key, g => g.First().Value ?? "");
    }
}
