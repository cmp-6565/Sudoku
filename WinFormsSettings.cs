using System;
using Sudoku.Properties;

namespace Sudoku
{
    public class WinFormsSettings : ISudokuSettings
    {
        // --- Benutzer-Einstellungen ---

        public string DisplayLanguage
        {
            get => Settings.Default.DisplayLanguage;
            set => Settings.Default.DisplayLanguage = value;
        }

        public int BookletSizeNew
        {
            get => Settings.Default.BookletSizeNew;
            set => Settings.Default.BookletSizeNew = value;
        }

        public bool PrintSolution
        {
            get => Settings.Default.PrintSolution;
            set => Settings.Default.PrintSolution = value;
        }

        public int MaxSolutions
        {
            get => Settings.Default.MaxSolutions;
            set => Settings.Default.MaxSolutions = value;
        }

        public int MinValues
        {
            get => Settings.Default.MinValues;
            set => Settings.Default.MinValues = value;
        }

        public bool AutoSaveBooklet
        {
            get => Settings.Default.AutoSaveBooklet;
            set => Settings.Default.AutoSaveBooklet = value;
        }

        public string ProblemDirectory
        {
            get => Settings.Default.ProblemDirectory;
            set => Settings.Default.ProblemDirectory = value;
        }

        public int Size
        {
            get => Settings.Default.Size;
            set => Settings.Default.Size = value;
        }

        public bool PrintHints
        {
            get => Settings.Default.PrintHints;
            set => Settings.Default.PrintHints = value;
        }

        public bool ShowHints
        {
            get => Settings.Default.ShowHints;
            set => Settings.Default.ShowHints = value;
        }

        public int HorizontalProblems
        {
            get => Settings.Default.HorizontalProblems;
            set => Settings.Default.HorizontalProblems = value;
        }

        public int HorizontalSolutions
        {
            get => Settings.Default.HorizontalSolutions;
            set => Settings.Default.HorizontalSolutions = value;
        }

        public bool AutoCheck
        {
            get => Settings.Default.AutoCheck;
            set => Settings.Default.AutoCheck = value;
        }

        public bool Debug
        {
            get => Settings.Default.Debug;
            set => Settings.Default.Debug = value;
        }

        public bool FindAllSolutions
        {
            get => Settings.Default.FindAllSolutions;
            set => Settings.Default.FindAllSolutions = value;
        }

        public int BookletSizeExisting
        {
            get => Settings.Default.BookletSizeExisting;
            set => Settings.Default.BookletSizeExisting = value;
        }

        public bool BookletSizeUnlimited
        {
            get => Settings.Default.BookletSizeUnlimited;
            set => Settings.Default.BookletSizeUnlimited = value;
        }

        public int SeverityLevel
        {
            get => Settings.Default.SeverityLevel;
            set => Settings.Default.SeverityLevel = value;
        }

        public bool HideWhenMinimized
        {
            get => Settings.Default.HideWhenMinimized;
            set => Settings.Default.HideWhenMinimized = value;
        }

        public int TraceFrequence
        {
            get => Settings.Default.TraceFrequence;
            set => Settings.Default.TraceFrequence = value;
        }

        public bool UseWatchHandHints
        {
            get => Settings.Default.UseWatchHandHints;
            set => Settings.Default.UseWatchHandHints = value;
        }

        public bool GenerateXSudoku
        {
            get => Settings.Default.GenerateXSudoku;
            set => Settings.Default.GenerateXSudoku = value;
        }

        public bool GenerateNormalSudoku
        {
            get => Settings.Default.GenerateNormalSudoku;
            set => Settings.Default.GenerateNormalSudoku = value;
        }

        public bool SelectSeverity
        {
            get => Settings.Default.SelectSeverity;
            set => Settings.Default.SelectSeverity = value;
        }

        public int XSudokuConstrast
        {
            get => Settings.Default.XSudokuConstrast;
            set => Settings.Default.XSudokuConstrast = value;
        }

        public string State
        {
            get => Settings.Default.State;
            set => Settings.Default.State = value;
        }

        public bool AutoSaveState
        {
            get => Settings.Default.AutoSaveState;
            set => Settings.Default.AutoSaveState = value;
        }

        public bool GenerateMinimalProblems
        {
            get => Settings.Default.GenerateMinimalProblems;
            set => Settings.Default.GenerateMinimalProblems = value;
        }

        public bool MarkNeighbors
        {
            get => Settings.Default.MarkNeighbors;
            set => Settings.Default.MarkNeighbors = value;
        }

        public bool UsePrecalculatedProblems
        {
            get => Settings.Default.UsePrecalculatedProblems;
            set => Settings.Default.UsePrecalculatedProblems = value;
        }

        public string LastVersion
        {
            get => Settings.Default.LastVersion;
            set => Settings.Default.LastVersion = value;
        }

        public bool SudokuOfTheDay
        {
            get => Settings.Default.SudokuOfTheDay;
            set => Settings.Default.SudokuOfTheDay = value;
        }

        public bool PrintInternalSeverity
        {
            get => Settings.Default.PrintInternalSeverity;
            set => Settings.Default.PrintInternalSeverity = value;
        }

        public bool AutoPause
        {
            get => Settings.Default.AutoPause;
            set => Settings.Default.AutoPause = value;
        }

        public decimal AutoPauseLag
        {
            get => Settings.Default.AutoPauseLag;
            set => Settings.Default.AutoPauseLag = value;
        }

        public int Contrast
        {
            get => Settings.Default.Contrast;
            set => Settings.Default.Contrast = value;
        }

        public bool HighlightSameValues
        {
            get => Settings.Default.HighlightSameValues;
            set => Settings.Default.HighlightSameValues = value;
        }

        // --- Anwendungs-Einstellungen (Read-Only) ---

        public float CellWidth => Settings.Default.CellWidth;
        public float SmallCellWidth => Settings.Default.SmallCellWidth;
        public float Intermediate => Settings.Default.Intermediate;
        public string DefaultFileExtension => Settings.Default.DefaultFileExtension;
        public string SupportedCultures => Settings.Default.SupportedCultures;
        public int Trivial => Settings.Default.Trivial;
        public float MagnificationFactor => Settings.Default.MagnificationFactor;
        public string FontSizes => Settings.Default.FontSizes;
        public string TableFont => Settings.Default.TableFont;
        public string PrintFont => Settings.Default.PrintFont;
        public string FixedFont => Settings.Default.FixedFont;
        public string HorizontalProblemsAlternatives => Settings.Default.HorizontalProblemsAlternatives;
        public string HorizontalSolutionsAlternatives => Settings.Default.HorizontalSolutionsAlternatives;
        public string MailAddress => Settings.Default.MailAddress;
        public string HTMLFileExtension => Settings.Default.HTMLFileExtension;
        public int NormalSudokuPublicationLimit => Settings.Default.NormalSudokuPublicationLimit;
        public int XSudokuPublicationLimit => Settings.Default.XSudokuPublicationLimit;
        public float Hard => Settings.Default.Hard;
        public int UploadLevelNormalSudoku => Settings.Default.UploadLevelNormalSudoku;
        public int UploadLevelXSudoku => Settings.Default.UploadLevelXSudoku;
        public int MaxValues => Settings.Default.MaxValues;
        public int MaxHints => Settings.Default.MaxHints;
        public int MaxProblems => Settings.Default.MaxProblems;

        /// <summary>
        /// Speichert die Benutzereinstellungen ab.
        /// </summary>
        public void Save()
        {
            Settings.Default.Save();
        }
    }
}