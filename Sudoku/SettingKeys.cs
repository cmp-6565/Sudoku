namespace Sudoku;

#nullable enable
/// <summary>
/// Defines constant keys for accessing application settings through Settings.Default.
/// Provides type-safe access to setting names, eliminating magic strings and reducing refactoring risks.
/// </summary>
public static class SettingKeys
{
    // --- User Settings (Read/Write) ---

    /// <summary>UI display language identifier.</summary>
    public const string DisplayLanguage = nameof(DisplayLanguage);

    /// <summary>Booklet size for new problems.</summary>
    public const string BookletSizeNew = nameof(BookletSizeNew);

    /// <summary>Include solutions in printed output.</summary>
    public const string PrintSolution = nameof(PrintSolution);

    /// <summary>Maximum number of solutions for solver.</summary>
    public const string MaxSolutions = nameof(MaxSolutions);

    /// <summary>Minimum number of givens for generated puzzles.</summary>
    public const string MinValues = nameof(MinValues);

    /// <summary>Auto-save created booklets.</summary>
    public const string AutoSaveBooklet = nameof(AutoSaveBooklet);

    /// <summary>Directory path for Sudoku problems.</summary>
    public const string ProblemDirectory = nameof(ProblemDirectory);

    /// <summary>Puzzle grid size (cells per side).</summary>
    public const string Size = nameof(Size);

    /// <summary>Include hints in printed output.</summary>
    public const string PrintHints = nameof(PrintHints);

    /// <summary>Show hints in UI.</summary>
    public const string ShowHints = nameof(ShowHints);

    /// <summary>Number of problems displayed horizontally.</summary>
    public const string HorizontalProblems = nameof(HorizontalProblems);

    /// <summary>Number of solutions displayed horizontally.</summary>
    public const string HorizontalSolutions = nameof(HorizontalSolutions);

    /// <summary>Auto-check for conflicts.</summary>
    public const string AutoCheck = nameof(AutoCheck);

    /// <summary>Diagnostic trace mode (maps to Settings property "Debug").</summary>
    public const string TraceMode = "Debug";

    /// <summary>Solver should find all solutions.</summary>
    public const string FindAllSolutions = nameof(FindAllSolutions);

    /// <summary>Booklet size for existing problems.</summary>
    public const string BookletSizeExisting = nameof(BookletSizeExisting);

    /// <summary>Treat booklet size as unlimited.</summary>
    public const string BookletSizeUnlimited = nameof(BookletSizeUnlimited);

    /// <summary>Severity threshold for generation algorithms.</summary>
    public const string SeverityLevel = nameof(SeverityLevel);

    /// <summary>Hide window when minimized.</summary>
    public const string HideWhenMinimized = nameof(HideWhenMinimized);

    /// <summary>Frequency for diagnostic tracing operations (legacy name: TraceFrequence).</summary>
    public const string TraceFrequency = nameof(TraceFrequency);

    /// <summary>Use watch-hand style hints in UI.</summary>
    public const string UseWatchHandHints = nameof(UseWatchHandHints);

    /// <summary>Generate X-Sudoku (diagonal constraints).</summary>
    public const string GenerateXSudoku = nameof(GenerateXSudoku);

    /// <summary>Generate standard (non-X) Sudoku puzzles.</summary>
    public const string GenerateNormalSudoku = nameof(GenerateNormalSudoku);

    /// <summary>Prompt user to select severity during generation.</summary>
    public const string SelectSeverity = nameof(SelectSeverity);

    /// <summary>Contrast level for X-Sudoku visual presentation (legacy name: XSudokuConstrast).</summary>
    public const string XSudokuContrast = nameof(XSudokuContrast);

    /// <summary>Serialized UI state string for restoration.</summary>
    public const string State = nameof(State);

    /// <summary>Auto-save application state.</summary>
    public const string AutoSaveState = nameof(AutoSaveState);

    /// <summary>Post-process generated puzzles to be minimal.</summary>
    public const string GenerateMinimalProblems = nameof(GenerateMinimalProblems);

    /// <summary>Highlight neighboring cells in UI.</summary>
    public const string MarkNeighbors = nameof(MarkNeighbors);

    /// <summary>Use pre-calculated problems instead of generating on demand.</summary>
    public const string UsePrecalculatedProblems = nameof(UsePrecalculatedProblems);

    /// <summary>Last application version that was run.</summary>
    public const string LastVersion = nameof(LastVersion);

    /// <summary>Enable "Sudoku of the Day" feature.</summary>
    public const string SudokuOfTheDay = nameof(SudokuOfTheDay);

    /// <summary>Print internal severity value in output.</summary>
    public const string PrintInternalSeverity = nameof(PrintInternalSeverity);

    /// <summary>Auto-pause on lag during long-running operations.</summary>
    public const string AutoPause = nameof(AutoPause);

    /// <summary>Lag threshold for auto-pause feature in milliseconds.</summary>
    public const string AutoPauseLag = nameof(AutoPauseLag);

    /// <summary>UI contrast level throughout application.</summary>
    public const string Contrast = nameof(Contrast);

    /// <summary>Highlight cells with identical values.</summary>
    public const string HighlightSameValues = nameof(HighlightSameValues);

    // --- Application Settings (Read-Only) ---

    /// <summary>Configured cell width for rendering puzzles.</summary>
    public const string CellWidth = nameof(CellWidth);

    /// <summary>Configured small-cell width for compact rendering.</summary>
    public const string SmallCellWidth = nameof(SmallCellWidth);

    /// <summary>Intermediate severity threshold.</summary>
    public const string Intermediate = nameof(Intermediate);

    /// <summary>Default file extension for Sudoku files.</summary>
    public const string DefaultFileExtension = nameof(DefaultFileExtension);

    /// <summary>Comma-separated list of supported cultures/locales.</summary>
    public const string SupportedCultures = nameof(SupportedCultures);

    /// <summary>Severity threshold considered trivial.</summary>
    public const string Trivial = nameof(Trivial);

    /// <summary>Magnification factor for rendering and zoom.</summary>
    public const string MagnificationFactor = nameof(MagnificationFactor);

    /// <summary>Configuration string listing supported font sizes.</summary>
    public const string FontSizes = nameof(FontSizes);

    /// <summary>Default table font configuration string.</summary>
    public const string TableFont = nameof(TableFont);

    /// <summary>Configured print font name.</summary>
    public const string PrintFont = nameof(PrintFont);

    /// <summary>Configured font name for fixed (given) values.</summary>
    public const string FixedFont = nameof(FixedFont);

    /// <summary>Configuration string listing allowed horizontal problems alternatives.</summary>
    public const string HorizontalProblemsAlternatives = nameof(HorizontalProblemsAlternatives);

    /// <summary>Configuration string listing allowed horizontal solutions alternatives.</summary>
    public const string HorizontalSolutionsAlternatives = nameof(HorizontalSolutionsAlternatives);

    /// <summary>Configured contact email address.</summary>
    public const string MailAddress = nameof(MailAddress);

    /// <summary>Configured HTML file extension for export.</summary>
    public const string HTMLFileExtension = nameof(HTMLFileExtension);

    /// <summary>Publication limit for normal Sudoku sharing/uploading.</summary>
    public const string NormalSudokuPublicationLimit = nameof(NormalSudokuPublicationLimit);

    /// <summary>Publication limit for X-Sudoku sharing/uploading.</summary>
    public const string XSudokuPublicationLimit = nameof(XSudokuPublicationLimit);

    /// <summary>Numeric threshold considered hard difficulty.</summary>
    public const string Hard = nameof(Hard);

    /// <summary>Upload level threshold for normal Sudoku puzzles.</summary>
    public const string UploadLevelNormalSudoku = nameof(UploadLevelNormalSudoku);

    /// <summary>Upload level threshold for X-Sudoku puzzles.</summary>
    public const string UploadLevelXSudoku = nameof(UploadLevelXSudoku);

    /// <summary>Maximum allowed number of givens.</summary>
    public const string MaxValues = nameof(MaxValues);

    /// <summary>Maximum number of hints that can be requested.</summary>
    public const string MaxHints = nameof(MaxHints);

    /// <summary>Maximum number of problems that can be held in memory or booklet.</summary>
    public const string MaxProblems = nameof(MaxProblems);
}