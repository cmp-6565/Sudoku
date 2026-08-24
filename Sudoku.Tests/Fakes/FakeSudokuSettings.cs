// Sudoku.Tests/Fakes/FakeSudokuSettings.cs
#nullable enable
using Sudoku.Application;

namespace Sudoku.Tests;

/// <summary>
/// Leichtgewichtige, rein speicherbasierte <see cref="ISudokuSettings"/>-Implementierung für Tests.
/// Ersetzt <c>WinFormsSettings</c> in Tests, die keine echte Windows-Benutzerkonfiguration
/// (Settings.Default) brauchen oder wollen. Alle Werte sind über Object-Initializer setzbar,
/// genau wie bisher bei WinFormsSettings.
/// </summary>
internal sealed class FakeSudokuSettings: ISudokuSettings
{
    // --- ISudokuEngineSettings (aus Sudoku.Core) ---
    public string DisplayLanguage { get; set; } = "de-DE";
    public int MaxSolutions { get; set; } = 2;
    public float Intermediate { get; init; } = 50f;
    public float Hard { get; init; } = 150f;
    public int Trivial { get; init; } = 0;
    public int UploadLevelNormalSudoku { get; init; } = 100;
    public int UploadLevelXSudoku { get; init; } = 100;

    // --- ISudokuSettings: Benutzer-Einstellungen ---
    public int BookletSizeNew { get; set; } = 1;
    public bool PrintSolution { get; set; }
    public int MinValues { get; set; } = 17;
    public bool AutoSaveBooklet { get; set; }
    public string ProblemDirectory { get; set; } = string.Empty;
    public int Size { get; set; } = 9;
    public bool PrintHints { get; set; }
    public bool ShowHints { get; set; }
    public int HorizontalProblems { get; set; } = 1;
    public int HorizontalSolutions { get; set; } = 1;
    public bool AutoCheck { get; set; }
    public bool TraceMode { get; set; }
    public bool FindAllSolutions { get; set; }
    public int BookletSizeExisting { get; set; } = 1;
    public bool BookletSizeUnlimited { get; set; }
    public int SeverityLevel { get; set; } = int.MaxValue;
    public bool HideWhenMinimized { get; set; }
    public int TraceFrequency { get; set; } = 1;
    public bool UseWatchHandHints { get; set; }
    public bool GenerateXSudoku { get; set; }
    public bool GenerateNormalSudoku { get; set; } = true;
    public bool SelectSeverity { get; set; }
    public int XSudokuContrast { get; set; }
    public string State { get; set; } = string.Empty;
    public bool AutoSaveState { get; set; }
    public bool GenerateMinimalProblems { get; set; }
    public bool MarkNeighbors { get; set; }
    public bool UsePrecalculatedProblems { get; set; }
    public string LastVersion { get; set; } = string.Empty;
    public bool SudokuOfTheDay { get; set; }
    public bool PrintInternalSeverity { get; set; }
    public bool AutoPause { get; set; }
    public decimal AutoPauseLag { get; set; }
    public int Contrast { get; set; }
    public bool HighlightSameValues { get; set; }

    // --- ISudokuSettings: reine Lesewerte ---
    public float CellWidth { get; init; } = 32f;
    public float SmallCellWidth { get; init; } = 10f;
    public string DefaultFileExtension { get; init; } = ".sdk";
    public string SupportedCultures { get; init; } = "de-DE;en-US";
    public float MagnificationFactor { get; init; } = 1f;
    public string FontSizes { get; init; } = "8;10;12";
    public string TableFont { get; init; } = "Segoe UI";
    public string PrintFont { get; init; } = "Segoe UI";
    public string FixedFont { get; init; } = "Consolas";
    public string HorizontalProblemsAlternatives { get; init; } = "1;2;3";
    public string HorizontalSolutionsAlternatives { get; init; } = "1;2;3";
    public string MailAddress { get; init; } = string.Empty;
    public string HTMLFileExtension { get; init; } = ".htm";
    public int NormalSudokuPublicationLimit { get; init; } = 100;
    public int XSudokuPublicationLimit { get; init; } = 100;
    public int MaxValues { get; init; } = 81;
    public int MaxHints { get; init; } = 81;
    public int MaxProblems { get; init; } = 100;

    /// <summary>No-op – es gibt nichts zu persistieren, die Werte leben nur im Speicher.</summary>
    public void Save() { }
}