using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Sudoku.Core;

using Sudoku.Properties;

namespace Sudoku;

/// <summary>
/// Implements IObservableSudokuSettings with automatic delegation to Settings.Default.
/// Provides validation, error handling, logging, and event notifications for all settings operations.
/// Reduces code duplication through reflection-based property proxying.
/// </summary>
public class WinFormsSettings: IObservableSudokuSettings
{
    private readonly Dictionary<string, object?> _cache = new();
    private static readonly string SettingsSource = typeof(Settings).FullName ?? "Settings.Default";

    /// <summary>
    /// Occurs when any setting value changes.
    /// </summary>
    public event EventHandler<SettingChangedEventArgs>? SettingChanged;

    /// <summary>
    /// Gets a setting value from the backing store (Settings.Default) with caching and error handling.
    /// </summary>
    private T GetSetting<T>(string settingKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(settingKey, nameof(settingKey));

        try
        {
            // Try to return cached value
            if(_cache.TryGetValue(settingKey, out var cachedValue))
            {
                return (T)cachedValue!;
            }

            // Use reflection to get property from Settings.Default (instance properties)
            var property = typeof(Settings).GetProperty(
                settingKey,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            if(property == null)
            {
                var errorMsg = $"Setting '{settingKey}' not found in {SettingsSource}. Ensure the property exists in application settings.";
                Debug.WriteLine($"[ERROR] {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }

            // Get value from the Settings.Default instance (backing store)
            var value = property.GetValue(Settings.Default);

            // Check for null values on non-nullable value types
            if(value == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
            {
                var errorMsg = $"Setting '{settingKey}' returned null but type '{typeof(T).Name}' is not nullable.";
                Debug.WriteLine($"[ERROR] {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }

            // Safe cast: if value is null, use default, otherwise cast to T
            var result = value == null ? default(T)! : (T)value;

            // Cache the value for subsequent reads
            _cache[settingKey] = result;

            return result;
        }
        catch(Exception ex) when(!(ex is InvalidOperationException || ex is ArgumentException))
        {
            var errorMsg = $"Failed to retrieve setting '{settingKey}' from {SettingsSource}.";
            Debug.WriteLine($"[ERROR] {errorMsg} Exception: {ex.Message}");
            throw new InvalidOperationException(errorMsg, ex);
        }
    }

    /// <summary>
    /// Sets a setting value in the backing store (Settings.Default) with validation and error handling.
    /// Raises SettingChanged event if value actually changed.
    /// </summary>
    private void SetSetting<T>(string settingKey, T value, Func<T, T>? validator = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(settingKey, nameof(settingKey));

        try
        {
            // Get current value for comparison
            var currentValue = GetSetting<T>(settingKey);

            // Apply validation if provided
            var validatedValue = validator != null ? validator(value) : value;

            // Skip if value hasn't changed
            if(Equals(currentValue, validatedValue))
            {
                Debug.WriteLine($"[TRACE] Setting '{settingKey}' value unchanged (still '{validatedValue}')");
                return;
            }

            // Use reflection to set property in Settings.Default (instance properties)
            var property = typeof(Settings).GetProperty(settingKey,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            if(property == null)
            {
                var errorMsg = $"Setting '{settingKey}' not found in {SettingsSource}. " +
                              $"Ensure the property exists in application settings.";
                Debug.WriteLine($"[ERROR] {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }

            if(!property.CanWrite)
            {
                var errorMsg = $"Setting '{settingKey}' is read-only and cannot be modified.";
                Debug.WriteLine($"[WARNING] {errorMsg}");
                throw new InvalidOperationException(errorMsg);
            }

            // Set value on the Settings.Default instance (backing store)
            property.SetValue(Settings.Default, validatedValue);

            // Invalidate cache for this key
            _cache.Remove(settingKey);

            Debug.WriteLine($"[TRACE] Setting '{settingKey}' updated from '{currentValue}' to '{validatedValue}'");

            // Raise SettingChanged event
            OnSettingChanged(new SettingChangedEventArgs(settingKey, currentValue, validatedValue));
        }
        catch(Exception ex) when(!(ex is InvalidOperationException || ex is ArgumentException))
        {
            var errorMsg = $"Failed to set setting '{settingKey}' in {SettingsSource}.";
            Debug.WriteLine($"[ERROR] {errorMsg} Exception: {ex.Message}");
            throw new InvalidOperationException(errorMsg, ex);
        }
    }

    /// <summary>
    /// Raises the SettingChanged event with the provided event arguments.
    /// </summary>
    protected virtual void OnSettingChanged(SettingChangedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e, nameof(e));
        SettingChanged?.Invoke(this, e);
    }

    // --- User Settings (Read/Write) ---

    /// <summary>
    /// Gets or sets the UI display language identifier used by the application.
    /// </summary>
    public string DisplayLanguage
    {
        get => GetSetting<string>(SettingKeys.DisplayLanguage);
        set => SetSetting(SettingKeys.DisplayLanguage, value,
            v => SettingsValidator.ValidateString(v, "en-US"));
    }

    /// <summary>
    /// Gets or sets the default booklet size to use when creating new booklets.
    /// </summary>
    public int BookletSizeNew
    {
        get => GetSetting<int>(SettingKeys.BookletSizeNew);
        set => SetSetting(SettingKeys.BookletSizeNew, value,
            SettingsValidator.ValidateBookletSize);
    }

    /// <summary>
    /// Gets or sets a value indicating whether printed output should include the solution.
    /// </summary>
    public bool PrintSolution
    {
        get => GetSetting<bool>(SettingKeys.PrintSolution);
        set => SetSetting(SettingKeys.PrintSolution, value);
    }

    /// <summary>
    /// Gets or sets the maximum number of solutions that the solver should collect/store.
    /// Must be at least 1.
    /// </summary>
    public int MaxSolutions
    {
        get => GetSetting<int>(SettingKeys.MaxSolutions);
        set => SetSetting(SettingKeys.MaxSolutions, value,
            SettingsValidator.ValidateMaxSolutions);
    }

    /// <summary>
    /// Gets or sets the preferred minimal number of givens for generated puzzles.
    /// Must be at least 1.
    /// </summary>
    public int MinValues
    {
        get => GetSetting<int>(SettingKeys.MinValues);
        set => SetSetting(SettingKeys.MinValues, value,
            SettingsValidator.ValidateMinValues);
    }

    /// <summary>
    /// Gets or sets a value indicating whether created booklets are automatically saved.
    /// </summary>
    public bool AutoSaveBooklet
    {
        get => GetSetting<bool>(SettingKeys.AutoSaveBooklet);
        set => SetSetting(SettingKeys.AutoSaveBooklet, value);
    }

    /// <summary>
    /// Gets or sets the default directory path used to open and save problem files.
    /// </summary>
    public string ProblemDirectory
    {
        get => GetSetting<string>(SettingKeys.ProblemDirectory).Length == 0? Path.Combine(Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.User)!, "Sudoku Problems"): GetSetting<string>(SettingKeys.ProblemDirectory);
        set => SetSetting(SettingKeys.ProblemDirectory, value,
            v => SettingsValidator.ValidateDirectoryPath(v, AppContext.BaseDirectory));
    }

    /// <summary>
    /// Gets or sets the configured puzzle grid size (total cells per side).
    /// Must be at least 1 and at most 16.
    /// </summary>
    public int Size
    {
        get => GetSetting<int>(SettingKeys.Size);
        set => SetSetting(SettingKeys.Size, value,
            SettingsValidator.ValidateGridSize);
    }

    /// <summary>
    /// Gets or sets a value indicating whether hints should be printed on the output.
    /// </summary>
    public bool PrintHints
    {
        get => GetSetting<bool>(SettingKeys.PrintHints);
        set => SetSetting(SettingKeys.PrintHints, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether hints are shown in the UI.
    /// </summary>
    public bool ShowHints
    {
        get => GetSetting<bool>(SettingKeys.ShowHints);
        set => SetSetting(SettingKeys.ShowHints, value);
    }

    /// <summary>
    /// Gets or sets the number of problems shown horizontally in the UI layout.
    /// Must be at least 1 and at most 20.
    /// </summary>
    public int HorizontalProblems
    {
        get => GetSetting<int>(SettingKeys.HorizontalProblems);
        set => SetSetting(SettingKeys.HorizontalProblems, value,
            SettingsValidator.ValidateHorizontalCellCount);
    }

    /// <summary>
    /// Gets or sets the number of solutions shown horizontally in the UI layout.
    /// Must be at least 1 and at most 20.
    /// </summary>
    public int HorizontalSolutions
    {
        get => GetSetting<int>(SettingKeys.HorizontalSolutions);
        set => SetSetting(SettingKeys.HorizontalSolutions, value,
            SettingsValidator.ValidateHorizontalCellCount);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application should automatically check for conflicts.
    /// </summary>
    public bool AutoCheck
    {
        get => GetSetting<bool>(SettingKeys.AutoCheck);
        set => SetSetting(SettingKeys.AutoCheck, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether diagnostic trace mode is enabled.
    /// </summary>
    public bool TraceMode
    {
        get => GetSetting<bool>(SettingKeys.TraceMode);
        set => SetSetting(SettingKeys.TraceMode, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the solver should search for all solutions.
    /// </summary>
    public bool FindAllSolutions
    {
        get => GetSetting<bool>(SettingKeys.FindAllSolutions);
        set => SetSetting(SettingKeys.FindAllSolutions, value);
    }

    /// <summary>
    /// Gets or sets the booklet size to use when adding puzzles to an existing booklet.
    /// </summary>
    public int BookletSizeExisting
    {
        get => GetSetting<int>(SettingKeys.BookletSizeExisting);
        set => SetSetting(SettingKeys.BookletSizeExisting, value,
            SettingsValidator.ValidateBookletSize);
    }

    /// <summary>
    /// Gets or sets a value indicating whether booklet size is treated as unlimited.
    /// </summary>
    public bool BookletSizeUnlimited
    {
        get => GetSetting<bool>(SettingKeys.BookletSizeUnlimited);
        set => SetSetting(SettingKeys.BookletSizeUnlimited, value);
    }

    /// <summary>
    /// Gets or sets the configured severity threshold used by generation algorithms.
    /// </summary>
    public int SeverityLevel
    {
        get => GetSetting<int>(SettingKeys.SeverityLevel);
        set => SetSetting(SettingKeys.SeverityLevel, value,
            SettingsValidator.ValidateSeverityLevel);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application window should be hidden when minimized.
    /// </summary>
    public bool HideWhenMinimized
    {
        get => GetSetting<bool>(SettingKeys.HideWhenMinimized);
        set => SetSetting(SettingKeys.HideWhenMinimized, value);
    }

    /// <summary>
    /// Gets or sets the frequency used for diagnostic tracing operations.
    /// </summary>
    public int TraceFrequency
    {
        get => GetSetting<int>(SettingKeys.TraceFrequency);
        set => SetSetting(SettingKeys.TraceFrequency, value,
            SettingsValidator.ValidateTraceFrequency);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use the watch-hand style hints in the UI.
    /// </summary>
    public bool UseWatchHandHints
    {
        get => GetSetting<bool>(SettingKeys.UseWatchHandHints);
        set => SetSetting(SettingKeys.UseWatchHandHints, value);
    }

    /// <summary>
    /// Gets or sets whether X-Sudoku (diagonal constraints) should be generated.
    /// </summary>
    public bool GenerateXSudoku
    {
        get => GetSetting<bool>(SettingKeys.GenerateXSudoku);
        set => SetSetting(SettingKeys.GenerateXSudoku, value);
    }

    /// <summary>
    /// Gets or sets whether standard (non-X) Sudoku puzzles should be generated.
    /// </summary>
    public bool GenerateNormalSudoku
    {
        get => GetSetting<bool>(SettingKeys.GenerateNormalSudoku);
        set => SetSetting(SettingKeys.GenerateNormalSudoku, value);
    }

    /// <summary>
    /// Gets or sets whether the user should be prompted to select severity when generating puzzles.
    /// </summary>
    public bool SelectSeverity
    {
        get => GetSetting<bool>(SettingKeys.SelectSeverity);
        set => SetSetting(SettingKeys.SelectSeverity, value);
    }

    /// <summary>
    /// Gets or sets the contrast level used for X-Sudoku visual presentation.
    /// </summary>
    public int XSudokuContrast
    {
        get => GetSetting<int>(SettingKeys.XSudokuContrast);
        set => SetSetting(SettingKeys.XSudokuContrast, value,
            SettingsValidator.ValidateContrast);
    }

    /// <summary>
    /// Gets or sets a serialized UI state string used to restore application state.
    /// </summary>
    public string State
    {
        get => GetSetting<string>(SettingKeys.State);
        set => SetSetting(SettingKeys.State, value,
            v => SettingsValidator.ValidateString(v, string.Empty));
    }

    /// <summary>
    /// Gets or sets a value indicating whether the UI state should be automatically saved.
    /// </summary>
    public bool AutoSaveState
    {
        get => GetSetting<bool>(SettingKeys.AutoSaveState);
        set => SetSetting(SettingKeys.AutoSaveState, value);
    }

    /// <summary>
    /// Gets or sets whether generated puzzles should be post-processed to be minimal (no redundant givens).
    /// </summary>
    public bool GenerateMinimalProblems
    {
        get => GetSetting<bool>(SettingKeys.GenerateMinimalProblems);
        set => SetSetting(SettingKeys.GenerateMinimalProblems, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether neighboring cells should be highlighted in the UI.
    /// </summary>
    public bool MarkNeighbors
    {
        get => GetSetting<bool>(SettingKeys.MarkNeighbors);
        set => SetSetting(SettingKeys.MarkNeighbors, value);
    }

    /// <summary>
    /// Gets or sets whether to prefer pre-calculated problems instead of generating on demand.
    /// </summary>
    public bool UsePrecalculatedProblems
    {
        get => GetSetting<bool>(SettingKeys.UsePrecalculatedProblems);
        set => SetSetting(SettingKeys.UsePrecalculatedProblems, value);
    }

    /// <summary>
    /// Gets or sets the last application version string stored in settings.
    /// </summary>
    public string LastVersion
    {
        get => GetSetting<string>(SettingKeys.LastVersion);
        set => SetSetting(SettingKeys.LastVersion, value,
            v => SettingsValidator.ValidateString(v, "0.0.0.0"));
    }

    /// <summary>
    /// Gets or sets whether the application should enable the "Sudoku of the Day" feature.
    /// </summary>
    public bool SudokuOfTheDay
    {
        get => GetSetting<bool>(SettingKeys.SudokuOfTheDay);
        set => SetSetting(SettingKeys.SudokuOfTheDay, value);
    }

    /// <summary>
    /// Gets or sets whether the internal severity value should be printed on output.
    /// </summary>
    public bool PrintInternalSeverity
    {
        get => GetSetting<bool>(SettingKeys.PrintInternalSeverity);
        set => SetSetting(SettingKeys.PrintInternalSeverity, value);
    }

    /// <summary>
    /// Gets or sets whether automatic pause handling is enabled in long-running operations.
    /// </summary>
    public bool AutoPause
    {
        get => GetSetting<bool>(SettingKeys.AutoPause);
        set => SetSetting(SettingKeys.AutoPause, value);
    }

    /// <summary>
    /// Gets or sets the lag time used by the auto-pause feature (in milliseconds).
    /// Valid range: 0 to 60000 (60 seconds).
    /// </summary>
    public decimal AutoPauseLag
    {
        get => GetSetting<decimal>(SettingKeys.AutoPauseLag);
        set => SetSetting(SettingKeys.AutoPauseLag, value,
            SettingsValidator.ValidateAutoPauseLag);
    }

    /// <summary>
    /// Gets or sets the UI contrast level used throughout the application.
    /// Valid range: 0 to 100 (percent).
    /// </summary>
    public int Contrast
    {
        get => GetSetting<int>(SettingKeys.Contrast);
        set => SetSetting(SettingKeys.Contrast, value,
            SettingsValidator.ValidateContrast);
    }

    /// <summary>
    /// Gets or sets a value indicating whether cells with identical values should be visually highlighted.
    /// </summary>
    public bool HighlightSameValues
    {
        get => GetSetting<bool>(SettingKeys.HighlightSameValues);
        set => SetSetting(SettingKeys.HighlightSameValues, value);
    }

    // --- Application Settings (Read-Only) ---

    /// <summary>Returns the configured cell width used for rendering puzzles.</summary>
    public float CellWidth => GetSetting<float>(SettingKeys.CellWidth);

    /// <summary>Returns the configured small-cell width used for compact rendering.</summary>
    public float SmallCellWidth => GetSetting<float>(SettingKeys.SmallCellWidth);

    /// <summary>Returns the configured intermediate severity threshold.</summary>
    public float Intermediate => GetSetting<float>(SettingKeys.Intermediate);

    /// <summary>Returns the default file extension used when saving puzzle files.</summary>
    public string DefaultFileExtension => GetSetting<string>(SettingKeys.DefaultFileExtension);

    /// <summary>Returns the comma-separated list of supported cultures/locales for display language selection.</summary>
    public string SupportedCultures => GetSetting<string>(SettingKeys.SupportedCultures);

    /// <summary>Returns the severity threshold considered trivial.</summary>
    public int Trivial => GetSetting<int>(SettingKeys.Trivial);

    /// <summary>Returns the magnification factor applied when rendering for print or zoom.</summary>
    public float MagnificationFactor => GetSetting<float>(SettingKeys.MagnificationFactor);

    /// <summary>Returns a configuration string listing supported font sizes.</summary>
    public string FontSizes => GetSetting<string>(SettingKeys.FontSizes);

    /// <summary>Returns the default table font configuration string.</summary>
    public string TableFont => GetSetting<string>(SettingKeys.TableFont);

    /// <summary>Returns the configured print font name.</summary>
    public string PrintFont => GetSetting<string>(SettingKeys.PrintFont);

    /// <summary>Returns the configured font name for fixed (given) values.</summary>
    public string FixedFont => GetSetting<string>(SettingKeys.FixedFont);

    /// <summary>Returns a configuration string listing allowed horizontal problems alternatives.</summary>
    public string HorizontalProblemsAlternatives => GetSetting<string>(SettingKeys.HorizontalProblemsAlternatives);

    /// <summary>Returns a configuration string listing allowed horizontal solutions alternatives.</summary>
    public string HorizontalSolutionsAlternatives => GetSetting<string>(SettingKeys.HorizontalSolutionsAlternatives);

    /// <summary>Returns the configured contact email address used in the application.</summary>
    public string MailAddress => GetSetting<string>(SettingKeys.MailAddress);

    /// <summary>Returns the configured HTML file extension used for exporting puzzles.</summary>
    public string HTMLFileExtension => GetSetting<string>(SettingKeys.HTMLFileExtension);

    /// <summary>Returns the publication limit used for normal Sudoku sharing/uploading.</summary>
    public int NormalSudokuPublicationLimit => GetSetting<int>(SettingKeys.NormalSudokuPublicationLimit);

    /// <summary>Returns the publication limit used for X-Sudoku sharing/uploading.</summary>
    public int XSudokuPublicationLimit => GetSetting<int>(SettingKeys.XSudokuPublicationLimit);

    /// <summary>Returns the configured numeric threshold considered hard difficulty.</summary>
    public float Hard => GetSetting<float>(SettingKeys.Hard);

    /// <summary>Returns the upload level threshold for normal Sudoku puzzles.</summary>
    public int UploadLevelNormalSudoku => GetSetting<int>(SettingKeys.UploadLevelNormalSudoku);

    /// <summary>Returns the upload level threshold for X-Sudoku puzzles.</summary>
    public int UploadLevelXSudoku => GetSetting<int>(SettingKeys.UploadLevelXSudoku);

    /// <summary>Returns the maximum allowed number of givens.</summary>
    public int MaxValues => GetSetting<int>(SettingKeys.MaxValues);

    /// <summary>Returns the maximum number of hints that can be requested.</summary>
    public int MaxHints => GetSetting<int>(SettingKeys.MaxHints);

    /// <summary>Returns the maximum number of problems that can be held in memory or a booklet.</summary>
    public int MaxProblems => GetSetting<int>(SettingKeys.MaxProblems);

    /// <summary>Returns the size (in cells) of the inner rectangle/box (commonly 3 for standard 9x9 Sudoku).</summary>
    public static int RectSize => SudokuGrid.RectSize;

    /// <summary>Returns the full Sudoku size (RectSize × RectSize), e.g. 9 for classic Sudoku.</summary>
    public static int SudokuSize => SudokuGrid.SudokuSize;

    /// <summary>Returns the total number of cells in a puzzle (SudokuSize × SudokuSize).</summary>
    public static int TotalCellCount => SudokuGrid.TotalCellCount;

    /// <summary>
    /// Persists the current user settings to the backing store.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when settings cannot be persisted.</exception>
    public void Save()
    {
        try
        {
            Debug.WriteLine("[INFO] Persisting settings to backing store...");
            Settings.Default.Save();
            _cache.Clear();
            Debug.WriteLine("[INFO] Settings persisted successfully.");
        }
        catch(System.Configuration.ConfigurationErrorsException ex)
        {
            var errorMsg = "Failed to persist settings: Configuration error in settings file.";
            Debug.WriteLine($"[ERROR] {errorMsg} Exception: {ex.Message}");
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch(UnauthorizedAccessException ex)
        {
            var errorMsg = "Failed to persist settings: Access denied to settings storage location.";
            Debug.WriteLine($"[ERROR] {errorMsg} Exception: {ex.Message}");
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch(System.IO.IOException ex)
        {
            var errorMsg = "Failed to persist settings: I/O error accessing settings storage.";
            Debug.WriteLine($"[ERROR] {errorMsg} Exception: {ex.Message}");
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch(Exception ex)
        {
            var errorMsg = $"Failed to persist settings to the backing store. Reason: {ex.GetType().Name}";
            Debug.WriteLine($"[ERROR] {errorMsg} Exception: {ex.Message}");
            throw new InvalidOperationException(errorMsg, ex);
        }
    }
}