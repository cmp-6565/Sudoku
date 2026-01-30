namespace Sudoku;

public interface ISudokuSettings
{
    // --- Benutzer-Einstellungen (Lese-/Schreibzugriff) ---
    string DisplayLanguage { get; set; }
    int BookletSizeNew { get; set; }
    bool PrintSolution { get; set; }
    int MaxSolutions { get; set; }
    int MinValues { get; set; }
    bool AutoSaveBooklet { get; set; }
    string ProblemDirectory { get; set; }
    int Size { get; set; }
    bool PrintHints { get; set; }
    bool ShowHints { get; set; }
    int HorizontalProblems { get; set; }
    int HorizontalSolutions { get; set; }
    bool AutoCheck { get; set; }
    bool Debug { get; set; }
    bool FindAllSolutions { get; set; }
    int BookletSizeExisting { get; set; }
    bool BookletSizeUnlimited { get; set; }
    int SeverityLevel { get; set; }
    bool HideWhenMinimized { get; set; }
    int TraceFrequence { get; set; }
    bool UseWatchHandHints { get; set; }
    bool GenerateXSudoku { get; set; }
    bool GenerateNormalSudoku { get; set; }
    bool SelectSeverity { get; set; }
    int XSudokuConstrast { get; set; }
    string State { get; set; }
    bool AutoSaveState { get; set; }
    bool GenerateMinimalProblems { get; set; }
    bool MarkNeighbors { get; set; }
    bool UsePrecalculatedProblems { get; set; }
    string LastVersion { get; set; }
    bool SudokuOfTheDay { get; set; }
    bool PrintInternalSeverity { get; set; }
    bool AutoPause { get; set; }
    decimal AutoPauseLag { get; set; }
    int Contrast { get; set; }
    bool HighlightSameValues { get; set; }

    // --- Anwendungs-Einstellungen (Nur Lesezugriff) ---
    float CellWidth { get; }
    float SmallCellWidth { get; }
    float Intermediate { get; }
    string DefaultFileExtension { get; }
    string SupportedCultures { get; }
    int Trivial { get; }
    float MagnificationFactor { get; }
    string FontSizes { get; }
    string TableFont { get; }
    string PrintFont { get; }
    string FixedFont { get; }
    string HorizontalProblemsAlternatives { get; }
    string HorizontalSolutionsAlternatives { get; }
    string MailAddress { get; }
    string HTMLFileExtension { get; }
    int NormalSudokuPublicationLimit { get; }
    int XSudokuPublicationLimit { get; }
    float Hard { get; }
    int UploadLevelNormalSudoku { get; }
    int UploadLevelXSudoku { get; }
    int MaxValues { get; }
    int MaxHints { get; }
    int MaxProblems { get; }

    /// <summary>
    /// Speichert die aktuellen Benutzereinstellungen dauerhaft.
    /// </summary>
    void Save();
}