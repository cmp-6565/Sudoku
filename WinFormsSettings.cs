using Sudoku.Properties;

namespace Sudoku;

public class WinFormsSettings: ISudokuSettings
{
    // --- Benutzer-Einstellungen ---

    /// <summary>
    /// Gets or sets the UI display language identifier used by the application.
    /// </summary>
    public string DisplayLanguage
    {
        get => Settings.Default.DisplayLanguage;
        set => Settings.Default.DisplayLanguage = value;
    }

    /// <summary>
    /// Gets or sets the default booklet size to use when creating new booklets.
    /// </summary>
    public int BookletSizeNew
    {
        get => Settings.Default.BookletSizeNew;
        set => Settings.Default.BookletSizeNew = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether printed output should include the solution.
    /// </summary>
    public bool PrintSolution
    {
        get => Settings.Default.PrintSolution;
        set => Settings.Default.PrintSolution = value;
    }

    /// <summary>
    /// Gets or sets the maximum number of solutions that the solver should collect/store.
    /// </summary>
    public int MaxSolutions
    {
        get => Settings.Default.MaxSolutions;
        set => Settings.Default.MaxSolutions = value;
    }

    /// <summary>
    /// Gets or sets the preferred minimal number of givens for generated puzzles.
    /// </summary>
    public int MinValues
    {
        get => Settings.Default.MinValues;
        set => Settings.Default.MinValues = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether created booklets are automatically saved.
    /// </summary>
    public bool AutoSaveBooklet
    {
        get => Settings.Default.AutoSaveBooklet;
        set => Settings.Default.AutoSaveBooklet = value;
    }

    /// <summary>
    /// Gets or sets the default directory path used to open and save problem files.
    /// </summary>
    public string ProblemDirectory
    {
        get => Settings.Default.ProblemDirectory;
        set => Settings.Default.ProblemDirectory = value;
    }

    /// <summary>
    /// Gets or sets the configured puzzle grid size (total cells per side).
    /// </summary>
    public int Size
    {
        get => Settings.Default.Size;
        set => Settings.Default.Size = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether hints should be printed on the output.
    /// </summary>
    public bool PrintHints
    {
        get => Settings.Default.PrintHints;
        set => Settings.Default.PrintHints = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether hints are shown in the UI.
    /// </summary>
    public bool ShowHints
    {
        get => Settings.Default.ShowHints;
        set => Settings.Default.ShowHints = value;
    }

    /// <summary>
    /// Gets or sets the number of problems shown horizontally in the UI layout.
    /// </summary>
    public int HorizontalProblems
    {
        get => Settings.Default.HorizontalProblems;
        set => Settings.Default.HorizontalProblems = value;
    }

    /// <summary>
    /// Gets or sets the number of solutions shown horizontally in the UI layout.
    /// </summary>
    public int HorizontalSolutions
    {
        get => Settings.Default.HorizontalSolutions;
        set => Settings.Default.HorizontalSolutions = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application should automatically check for conflicts.
    /// </summary>
    public bool AutoCheck
    {
        get => Settings.Default.AutoCheck;
        set => Settings.Default.AutoCheck = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether diagnostic trace mode is enabled.
    /// </summary>
    public bool TraceMode
    {
        get => Settings.Default.Debug;
        set => Settings.Default.Debug = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the solver should search for all solutions.
    /// </summary>
    public bool FindAllSolutions
    {
        get => Settings.Default.FindAllSolutions;
        set => Settings.Default.FindAllSolutions = value;
    }

    /// <summary>
    /// Gets or sets the booklet size to use when adding puzzles to an existing booklet.
    /// </summary>
    public int BookletSizeExisting
    {
        get => Settings.Default.BookletSizeExisting;
        set => Settings.Default.BookletSizeExisting = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether booklet size is treated as unlimited.
    /// </summary>
    public bool BookletSizeUnlimited
    {
        get => Settings.Default.BookletSizeUnlimited;
        set => Settings.Default.BookletSizeUnlimited = value;
    }

    /// <summary>
    /// Gets or sets the configured severity threshold used by generation algorithms.
    /// </summary>
    public int SeverityLevel
    {
        get => Settings.Default.SeverityLevel;
        set => Settings.Default.SeverityLevel = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the application window should be hidden when minimized.
    /// </summary>
    public bool HideWhenMinimized
    {
        get => Settings.Default.HideWhenMinimized;
        set => Settings.Default.HideWhenMinimized = value;
    }

    /// <summary>
    /// Gets or sets the frequency used for diagnostic tracing operations.
    /// </summary>
    public int TraceFrequence
    {
        get => Settings.Default.TraceFrequence;
        set => Settings.Default.TraceFrequence = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use the watch-hand style hints in the UI.
    /// </summary>
    public bool UseWatchHandHints
    {
        get => Settings.Default.UseWatchHandHints;
        set => Settings.Default.UseWatchHandHints = value;
    }

    /// <summary>
    /// Gets or sets whether X-Sudoku (diagonal constraints) should be generated.
    /// </summary>
    public bool GenerateXSudoku
    {
        get => Settings.Default.GenerateXSudoku;
        set => Settings.Default.GenerateXSudoku = value;
    }

    /// <summary>
    /// Gets or sets whether standard (non-X) Sudoku puzzles should be generated.
    /// </summary>
    public bool GenerateNormalSudoku
    {
        get => Settings.Default.GenerateNormalSudoku;
        set => Settings.Default.GenerateNormalSudoku = value;
    }

    /// <summary>
    /// Gets or sets whether the user should be prompted to select severity when generating puzzles.
    /// </summary>
    public bool SelectSeverity
    {
        get => Settings.Default.SelectSeverity;
        set => Settings.Default.SelectSeverity = value;
    }

    /// <summary>
    /// Gets or sets the contrast level used for X-Sudoku visual presentation.
    /// </summary>
    public int XSudokuConstrast
    {
        get => Settings.Default.XSudokuConstrast;
        set => Settings.Default.XSudokuConstrast = value;
    }

    /// <summary>
    /// Gets or sets a serialized UI state string used to restore application state.
    /// </summary>
    public string State
    {
        get => Settings.Default.State;
        set => Settings.Default.State = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the UI state should be automatically saved.
    /// </summary>
    public bool AutoSaveState
    {
        get => Settings.Default.AutoSaveState;
        set => Settings.Default.AutoSaveState = value;
    }

    /// <summary>
    /// Gets or sets whether generated puzzles should be post-processed to be minimal (no redundant givens).
    /// </summary>
    public bool GenerateMinimalProblems
    {
        get => Settings.Default.GenerateMinimalProblems;
        set => Settings.Default.GenerateMinimalProblems = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether neighboring cells should be highlighted in the UI.
    /// </summary>
    public bool MarkNeighbors
    {
        get => Settings.Default.MarkNeighbors;
        set => Settings.Default.MarkNeighbors = value;
    }

    /// <summary>
    /// Gets or sets whether to prefer pre-calculated problems instead of generating on demand.
    /// </summary>
    public bool UsePrecalculatedProblems
    {
        get => Settings.Default.UsePrecalculatedProblems;
        set => Settings.Default.UsePrecalculatedProblems = value;
    }

    /// <summary>
    /// Gets or sets the last application version string stored in settings.
    /// </summary>
    public string LastVersion
    {
        get => Settings.Default.LastVersion;
        set => Settings.Default.LastVersion = value;
    }

    /// <summary>
    /// Gets or sets whether the application should enable the "Sudoku of the Day" feature.
    /// </summary>
    public bool SudokuOfTheDay
    {
        get => Settings.Default.SudokuOfTheDay;
        set => Settings.Default.SudokuOfTheDay = value;
    }

    /// <summary>
    /// Gets or sets whether the internal severity value should be printed on output.
    /// </summary>
    public bool PrintInternalSeverity
    {
        get => Settings.Default.PrintInternalSeverity;
        set => Settings.Default.PrintInternalSeverity = value;
    }

    /// <summary>
    /// Gets or sets whether automatic pause handling is enabled in long-running operations.
    /// </summary>
    public bool AutoPause
    {
        get => Settings.Default.AutoPause;
        set => Settings.Default.AutoPause = value;
    }

    /// <summary>
    /// Gets or sets the lag time used by the auto-pause feature (in seconds or configured unit).
    /// </summary>
    public decimal AutoPauseLag
    {
        get => Settings.Default.AutoPauseLag;
        set => Settings.Default.AutoPauseLag = value;
    }

    /// <summary>
    /// Gets or sets the UI contrast level used throughout the application.
    /// </summary>
    public int Contrast
    {
        get => Settings.Default.Contrast;
        set => Settings.Default.Contrast = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether cells with identical values should be visually highlighted.
    /// </summary>
    public bool HighlightSameValues
    {
        get => Settings.Default.HighlightSameValues;
        set => Settings.Default.HighlightSameValues = value;
    }

    // --- Anwendungs-Einstellungen (Read-Only) ---

    /// <summary>
    /// Returns the configured cell width used for rendering puzzles.
    /// </summary>
    public float CellWidth => Settings.Default.CellWidth;

    /// <summary>
    /// Returns the configured small-cell width used for compact rendering.
    /// </summary>
    public float SmallCellWidth => Settings.Default.SmallCellWidth;

    /// <summary>
    /// Returns the configured intermediate severity threshold.
    /// </summary>
    public float Intermediate => Settings.Default.Intermediate;

    /// <summary>
    /// Returns the default file extension used when saving puzzle files.
    /// </summary>
    public string DefaultFileExtension => Settings.Default.DefaultFileExtension;

    /// <summary>
    /// Returns the comma-separated list of supported cultures/locales for display language selection.
    /// </summary>
    public string SupportedCultures => Settings.Default.SupportedCultures;

    /// <summary>
    /// Returns the severity threshold considered trivial.
    /// </summary>
    public int Trivial => Settings.Default.Trivial;

    /// <summary>
    /// Returns the magnification factor applied when rendering for print or zoom.
    /// </summary>
    public float MagnificationFactor => Settings.Default.MagnificationFactor;

    /// <summary>
    /// Returns a configuration string listing supported font sizes.
    /// </summary>
    public string FontSizes => Settings.Default.FontSizes;

    /// <summary>
    /// Returns the default table font configuration string.
    /// </summary>
    public string TableFont => Settings.Default.TableFont;

    /// <summary>
    /// Returns the configured print font name.
    /// </summary>
    public string PrintFont => Settings.Default.PrintFont;

    /// <summary>
    /// Returns the configured font name for fixed (given) values.
    /// </summary>
    public string FixedFont => Settings.Default.FixedFont;

    /// <summary>
    /// Returns a configuration string listing allowed horizontal problems alternatives.
    /// </summary>
    public string HorizontalProblemsAlternatives => Settings.Default.HorizontalProblemsAlternatives;

    /// <summary>
    /// Returns a configuration string listing allowed horizontal solutions alternatives.
    /// </summary>
    public string HorizontalSolutionsAlternatives => Settings.Default.HorizontalSolutionsAlternatives;

    /// <summary>
    /// Returns the configured contact email address used in the application.
    /// </summary>
    public string MailAddress => Settings.Default.MailAddress;

    /// <summary>
    /// Returns the configured HTML file extension used for exporting puzzles.
    /// </summary>
    public string HTMLFileExtension => Settings.Default.HTMLFileExtension;

    /// <summary>
    /// Returns the publication limit used for normal Sudoku sharing/uploading.
    /// </summary>
    public int NormalSudokuPublicationLimit => Settings.Default.NormalSudokuPublicationLimit;

    /// <summary>
    /// Returns the publication limit used for X-Sudoku sharing/uploading.
    /// </summary>
    public int XSudokuPublicationLimit => Settings.Default.XSudokuPublicationLimit;

    /// <summary>
    /// Returns the configured numeric threshold considered hard difficulty.
    /// </summary>
    public float Hard => Settings.Default.Hard;

    /// <summary>
    /// Returns the upload level threshold for normal Sudoku puzzles.
    /// </summary>
    public int UploadLevelNormalSudoku => Settings.Default.UploadLevelNormalSudoku;

    /// <summary>
    /// Returns the upload level threshold for X-Sudoku puzzles.
    /// </summary>
    public int UploadLevelXSudoku => Settings.Default.UploadLevelXSudoku;

    /// <summary>
    /// Returns the maximum allowed number of givens.
    /// </summary>
    public int MaxValues => Settings.Default.MaxValues;

    /// <summary>
    /// Returns the maximum number of hints that can be requested.
    /// </summary>
    public int MaxHints => Settings.Default.MaxHints;

    /// <summary>
    /// Returns the maximum number of problems that can be held in memory or a booklet.
    /// </summary>
    public int MaxProblems => Settings.Default.MaxProblems;

    /// <summary>
    /// Returns the size (in cells) of the inner rectangle/box (commonly 3 for standard 9x9 Sudoku).
    /// </summary>
    public static int RectSize => 3;

    /// <summary>
    /// Returns the full Sudoku size (RectSize * RectSize), e.g. 9 for classic Sudoku.
    /// </summary>
    public static int SudokuSize => RectSize * RectSize;

    /// <summary>
    /// Returns the total number of cells in a puzzle (SudokuSize * SudokuSize).
    /// </summary>
    public static int TotalCellCount => SudokuSize * SudokuSize;


    /// <summary>
    /// Persist the current user settings to the backing store.
    /// </summary>
    public void Save()
    {
        Settings.Default.Save();
    }
}