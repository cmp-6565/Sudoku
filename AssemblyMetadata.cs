using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sudoku;

/// <summary>
/// Provides access to assembly metadata attributes for the current assembly.
/// Reads and caches AssemblyMetadataAttribute values.
/// </summary>
public static class AssemblyMetadata
{
    private static readonly Lazy<Dictionary<string, string>> metadata =
        new Lazy<Dictionary<string, string>>(LoadMetadata);

    /// <summary>
    /// Gets all assembly metadata as a read-only dictionary.
    /// </summary>
    public static IReadOnlyDictionary<string, string> All => metadata.Value;

    /// <summary>
    /// Retrieves a single metadata value by key.
    /// </summary>
    /// <param name="key">The metadata key to look up.</param>
    /// <returns>The metadata value, or null if the key does not exist.</returns>
    public static string Get(string key)
    {
        metadata.Value.TryGetValue(key, out var value);
        return value ?? String.Empty;
    }

    /// <summary>
    /// Loads all AssemblyMetadataAttribute values from the current assembly.
    /// </summary>
    /// <returns>A dictionary of metadata key-value pairs.</returns>
    private static Dictionary<string, string> LoadMetadata()
    {
        return Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .GroupBy(a => a.Key)
            .ToDictionary(g => g.Key, g => g.First().Value ?? "");
    }
}
