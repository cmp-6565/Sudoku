#nullable enable
using System;

namespace Sudoku;

/// <summary>
/// Defines the interface for managing Sudoku application settings and configuration.
/// Includes user preferences and application-level read-only properties.
/// </summary>
public interface ISudokuSettings
{
    // --- Benutzer-Einstellungen (Lese-/Schreibzugriff) ---
    /// <summary>Gets or sets the display language for the application.</summary>
    string DisplayLanguage { get; set; }

    /// <summary>Gets or sets the size of the booklet for new problems.</summary>
    int BookletSizeNew { get; set; }

    /// <summary>Gets or sets a value indicating whether to print solutions.</summary>
    bool PrintSolution { get; set; }

    /// <summary>Gets or sets the maximum number of solutions to find.</summary>
    int MaxSolutions { get; set; }

    /// <summary>Gets or sets the minimum number of values for problems.</summary>
    int MinValues { get; set; }

    /// <summary>Gets or sets a value indicating whether to auto-save booklets.</summary>
    bool AutoSaveBooklet { get; set; }

    /// <summary>Gets or sets the directory path for Sudoku problems.</summary>
    string ProblemDirectory { get; set; }

    /// <summary>Gets or sets the size of the Sudoku grid.</summary>
    int Size { get; set; }

    /// <summary>Gets or sets a value indicating whether to print hints.</summary>
    bool PrintHints { get; set; }

    /// <summary>Gets or sets a value indicating whether to show hints in the UI.</summary>
    bool ShowHints { get; set; }

    /// <summary>Gets or sets the number of problems to display horizontally.</summary>
    int HorizontalProblems { get; set; }

    /// <summary>Gets or sets the number of solutions to display horizontally.</summary>
    int HorizontalSolutions { get; set; }

    /// <summary>Gets or sets a value indicating whether to auto-check solutions.</summary>
    bool AutoCheck { get; set; }

    /// <summary>Gets or sets a value indicating whether trace mode is enabled.</summary>
    bool TraceMode { get; set; }

    /// <summary>Gets or sets a value indicating whether to find all solutions.</summary>
    bool FindAllSolutions { get; set; }

    /// <summary>Gets or sets the size of the booklet for existing problems.</summary>
    int BookletSizeExisting { get; set; }

    /// <summary>Gets or sets a value indicating whether booklet size is unlimited.</summary>
    bool BookletSizeUnlimited { get; set; }

    /// <summary>Gets or sets the severity level for problem generation.</summary>
    int SeverityLevel { get; set; }

    /// <summary>Gets or sets a value indicating whether to hide the window when minimized.</summary>
    bool HideWhenMinimized { get; set; }

    /// <summary>Gets or sets the trace frequency for solver operations.</summary>
    int TraceFrequency { get; set; }

    /// <summary>Gets or sets a value indicating whether to use watch hand hints.</summary>
    bool UseWatchHandHints { get; set; }

    /// <summary>Gets or sets a value indicating whether to generate X-Sudoku problems.</summary>
    bool GenerateXSudoku { get; set; }

    /// <summary>Gets or sets a value indicating whether to generate normal Sudoku problems.</summary>
    bool GenerateNormalSudoku { get; set; }

    /// <summary>Gets or sets a value indicating whether to select severity during generation.</summary>
    bool SelectSeverity { get; set; }

    /// <summary>Gets or sets the contrast level for X-Sudoku display.</summary>
    int XSudokuContrast { get; set; }

    /// <summary>Gets or sets the application state string.</summary>
    string State { get; set; }

    /// <summary>Gets or sets a value indicating whether to auto-save the application state.</summary>
    bool AutoSaveState { get; set; }

    /// <summary>Gets or sets a value indicating whether to generate minimal problems.</summary>
    bool GenerateMinimalProblems { get; set; }

    /// <summary>Gets or sets a value indicating whether to mark neighbors of selected cells.</summary>
    bool MarkNeighbors { get; set; }

    /// <summary>Gets or sets a value indicating whether to use pre-calculated problems.</summary>
    bool UsePrecalculatedProblems { get; set; }

    /// <summary>Gets or sets the last version of the application that was run.</summary>
    string LastVersion { get; set; }

    /// <summary>Gets or sets a value indicating whether to use the Sudoku of the Day feature.</summary>
    bool SudokuOfTheDay { get; set; }

    /// <summary>Gets or sets a value indicating whether to print internal severity values.</summary>
    bool PrintInternalSeverity { get; set; }

    /// <summary>Gets or sets a value indicating whether to auto-pause on lag.</summary>
    bool AutoPause { get; set; }

    /// <summary>Gets or sets the lag threshold for auto-pause in milliseconds.</summary>
    decimal AutoPauseLag { get; set; }

    /// <summary>Gets or sets the contrast level for display.</summary>
    int Contrast { get; set; }

    /// <summary>Gets or sets a value indicating whether to highlight same values in the grid.</summary>
    bool HighlightSameValues { get; set; }

    // --- Anwendungs-Einstellungen (Nur Lesezugriff) ---
    /// <summary>Gets the width of a single cell in the grid.</summary>
    float CellWidth { get; }

    /// <summary>Gets the width of a small cell in candidate display.</summary>
    float SmallCellWidth { get; }

    /// <summary>Gets the intermediate severity threshold.</summary>
    float Intermediate { get; }

    /// <summary>Gets the default file extension for Sudoku files.</summary>
    string DefaultFileExtension { get; }

    /// <summary>Gets the supported cultures for the application.</summary>
    string SupportedCultures { get; }

    /// <summary>Gets the trivial severity threshold.</summary>
    int Trivial { get; }

    /// <summary>Gets the magnification factor for UI scaling.</summary>
    float MagnificationFactor { get; }

    /// <summary>Gets the available font sizes for the application.</summary>
    string FontSizes { get; }

    /// <summary>Gets the font name for table display.</summary>
    string TableFont { get; }

    /// <summary>Gets the font name for printing.</summary>
    string PrintFont { get; }

    /// <summary>Gets the font name for fixed-width display.</summary>
    string FixedFont { get; }

    /// <summary>Gets the alternatives for horizontal problem count.</summary>
    string HorizontalProblemsAlternatives { get; }

    /// <summary>Gets the alternatives for horizontal solution count.</summary>
    string HorizontalSolutionsAlternatives { get; }

    /// <summary>Gets the contact mail address for the application.</summary>
    string MailAddress { get; }

    /// <summary>Gets the HTML file extension for export.</summary>
    string HTMLFileExtension { get; }

    /// <summary>Gets the publication limit for normal Sudoku problems.</summary>
    int NormalSudokuPublicationLimit { get; }

    /// <summary>Gets the publication limit for X-Sudoku problems.</summary>
    int XSudokuPublicationLimit { get; }

    /// <summary>Gets the hard severity threshold.</summary>
    float Hard { get; }

    /// <summary>Gets the upload severity level for normal Sudoku.</summary>
    int UploadLevelNormalSudoku { get; }

    /// <summary>Gets the upload severity level for X-Sudoku.</summary>
    int UploadLevelXSudoku { get; }

    /// <summary>Gets the maximum number of values allowed in a problem.</summary>
    int MaxValues { get; }

    /// <summary>Gets the maximum number of hints that can be requested.</summary>
    int MaxHints { get; }

    /// <summary>Gets the maximum number of problems to process.</summary>
    int MaxProblems { get; }

    /// <summary>
    /// Saves the current user settings permanently to storage.
    /// </summary>
    void Save();

}