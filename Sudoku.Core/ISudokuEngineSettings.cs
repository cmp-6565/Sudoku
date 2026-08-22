#nullable enable
using System;

namespace Sudoku.Core;

/// <summary>
/// Die Teilmenge der Anwendungseinstellungen, die die reine Sudoku-Engine
/// (Solver, Generator, Bewertung) benötigt – unabhängig von jeder UI-Technologie.
/// </summary>
public interface ISudokuEngineSettings
{
    /// <summary>Anzeigesprache, wird für lokalisierte Ressourcen/Meldungen der Engine benötigt.</summary>
    string DisplayLanguage { get; set; }

    /// <summary>Maximale Anzahl an Lösungen, die der Solver suchen soll.</summary>
    int MaxSolutions { get; set; }

    /// <summary>Schwellwert für den Schweregrad "mittel".</summary>
    float Intermediate { get; }

    /// <summary>Schwellwert für den Schweregrad "schwer".</summary>
    float Hard { get; }

    /// <summary>Schwellwert für den Schweregrad "trivial".</summary>
    int Trivial { get; }

    /// <summary>Upload-Schwellwert für normale Sudokus (steuert IsTricky).</summary>
    int UploadLevelNormalSudoku { get; }

    /// <summary>Upload-Schwellwert für X-Sudokus (steuert IsTricky).</summary>
    int UploadLevelXSudoku { get; }
}