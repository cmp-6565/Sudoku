#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("Sudoku.Tests")]

namespace Sudoku.Application;

/// <summary>
/// Helper class that exposes common assembly metadata as strongly-typed properties.
/// </summary>
public static class AssemblyInfo
{
    /// <summary>
    /// Gets the assembly title.
    /// </summary>
    /// <remarks>
    /// This returns the value of the <see cref="AssemblyTitleAttribute"/> when present.
    /// If the attribute is missing or its title is empty, the file name of the executing
    /// assembly (without extension) is returned as a fallback.
    /// </remarks>
    /// <returns>The assembly title or the assembly file name without extension when no title is defined.</returns>
    public static string AssemblyTitle
    {
        get
        {
            // Get all Title attributes on this assembly
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);

            // If there is at least one Title attribute
            if(attributes.Length > 0)
            {
                // Select the first one
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                // If it is not an empty string, return it
                if(!String.IsNullOrEmpty(titleAttribute.Title))
                    return titleAttribute.Title;
            }
            // If there was no Title attribute, or if the Title attribute was the empty string, return the .exe name
            return System.IO.Path.GetFileNameWithoutExtension(assembly.Location);
        }
    }

    /// <summary>
    /// Gets the assembly file version.
    /// </summary>
    /// <remarks>
    /// This reads the value from the <see cref="AssemblyFileVersionAttribute"/> applied to the assembly.
    /// It returns the literal version string stored in that attribute.
    /// </remarks>
    /// <returns>The assembly file version string.</returns>
    public static string AssemblyVersion
    {
        get
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            AssemblyFileVersionAttribute t = (AssemblyFileVersionAttribute)attributes[0];
            return t.Version;
        }
    }

    /// <summary>
    /// Gets the assembly build date/time as a localized string.
    /// </summary>
    /// <remarks>
    /// This attempts to read the "BuildDate" metadata from the assembly via <see cref="AssemblyMetadata.Get(string)"/>.
    /// If present, it parses the value using the invariant culture and ISO 8601 roundtrip format,
    /// converts the resulting <see cref="DateTime"/> to local time and formats it according to the current UI culture.
    /// If the metadata is missing or cannot be parsed, an empty string is returned.
    /// </remarks>
    /// <returns>A localized representation of the assembly build date, or an empty string if not available.</returns>
    public static string AssemblyDate
    {
        get
        {
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            if(DateTime.TryParse(AssemblyMetadata.Get("BuildDate"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
            {
                return dt.ToLocalTime().ToString(Thread.CurrentThread.CurrentUICulture);
            }
            return String.Empty;
        }
    }

    /// <summary>
    /// Gets the assembly description.
    /// </summary>
    /// <remarks>
    /// Reads the <see cref="AssemblyDescriptionAttribute"/> value if available.
    /// Returns an empty string when no description attribute is present.
    /// </remarks>
    /// <returns>The assembly description or an empty string if none is defined.</returns>
    public static string AssemblyDescription
    {
        get
        {
            // Get all Description attributes on this assembly
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            // If there aren't any Description attributes, return an empty string
            if(attributes.Length == 0)
                return String.Empty;
            // If there is a Description attribute, return its value
            return ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }
    }

    /// <summary>
    /// Gets the repository URL associated with the assembly.
    /// </summary>
    /// <remarks>
    /// This returns the value of the assembly metadata key "RepositoryUrl" obtained via <see cref="AssemblyMetadata.Get(string)"/>.
    /// The value is typically provided at build time (e.g. via MSBuild properties).
    /// </remarks>
    /// <returns>The repository URL string or null/empty if not set.</returns>
    public static string AssemblyGitRepository
    {
        get
        {
            return AssemblyMetadata.Get("RepositoryUrl") ?? String.Empty;
        }
    }

    /// <summary>
    /// Gets the product name for the assembly.
    /// </summary>
    /// <remarks>
    /// Reads the value from the <see cref="AssemblyProductAttribute"/>. Returns an empty string when absent.
    /// </remarks>
    /// <returns>The product name or an empty string if not defined.</returns>
    public static string AssemblyProduct
    {
        get
        {
            // Get all Product attributes on this assembly
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            // If there aren't any Product attributes, return an empty string
            if(attributes.Length == 0)
                return String.Empty;
            // If there is a Product attribute, return its value
            return ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    /// <summary>
    /// Gets the assembly copyright text.
    /// </summary>
    /// <remarks>
    /// Reads the value from the <see cref="AssemblyCopyrightAttribute"/> and
    /// returns an empty string if the attribute is not present.
    /// </remarks>
    /// <returns>The copyright string or an empty string if not defined.</returns>
    public static string AssemblyCopyright
    {
        get
        {
            // Get all Copyright attributes on this assembly
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            // If there aren't any Copyright attributes, return an empty string
            if(attributes.Length == 0)
                return String.Empty;
            // If there is a Copyright attribute, return its value
            return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    /// <summary>
    /// Gets the company name associated with the assembly.
    /// </summary>
    /// <remarks>
    /// Reads the value from the <see cref="AssemblyCompanyAttribute"/>. Returns an empty string when absent.
    /// </remarks>
    /// <returns>The company name or an empty string if not defined.</returns>
    public static string AssemblyCompany
    {
        get
        {
            // Get all Company attributes on this assembly
            Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            // If there aren't any Company attributes, return an empty string
            if(attributes.Length == 0)
                return String.Empty;
            // If there is a Company attribute, return its value
            return ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }
}