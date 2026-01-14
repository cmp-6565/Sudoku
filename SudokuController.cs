using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Sudoku.Properties;

namespace Sudoku
{
    internal class SudokuController
    {
        public BaseProblem CurrentProblem { get; private set; }
        public BaseProblem Backup { get; private set; }
        private Stack<CoreValue> undoStack;

        // Events
        public event EventHandler MatrixChanged;
        public event EventHandler SolutionFound;

        public SudokuController()
        {
            undoStack = new Stack<CoreValue>();
        }

        public void CreateNewProblem(bool xSudoku, bool notify=true)
        {
            CurrentProblem = xSudoku ? (BaseProblem)new XSudokuProblem() : new SudokuProblem();
            BackupProblem();
            if(notify)
                NotifyMatrixChanged();
        }

        public void UpdateProblemState(BaseProblem updatedProblem)
        {
            CurrentProblem = updatedProblem.Clone();
        }

        public async Task SolveAsync(bool findAllSolutions, IProgress<SudokuProgress> progress, CancellationToken token)
        {
            if(CurrentProblem == null) return;

            ulong maxSolutions = findAllSolutions? UInt64.MaxValue: 1;

            CurrentProblem.FindSolutions(maxSolutions);

            if(CurrentProblem.SolverTask != null)
            {
                while(!CurrentProblem.SolverTask.IsCompleted)
                {
                    token.ThrowIfCancellationRequested(); // Abbruch prüfen

                    // Fortschritt melden
                    if(progress != null)
                    {
                        var state = new SudokuProgress
                        {
                            Message = Resources.Thinking,
                            PassCount = CurrentProblem.TotalPassCounter,
                            SolutionCount = CurrentProblem.nSolutions,
                            Elapsed = DateTime.Now - DateTime.Now // Ggf. echte Startzeit übergeben oder im State halten
                        };
                        progress.Report(state);
                    }

                    await Task.Delay(50); // Polling Intervall
                }

                // Auf Exceptions warten
                await CurrentProblem.SolverTask;
            }

            // 4. Abschluss
            NotifyMatrixChanged();
            SolutionFound?.Invoke(this, EventArgs.Empty);
        }

        private void NotifyMatrixChanged()
        {
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }
        public void RestoreProblemState(bool notify=true)
        {
            Char sudokuType = (Char)Settings.Default.State[0];
            if(sudokuType != SudokuProblem.ProblemIdentifier && sudokuType != XSudokuProblem.ProblemIdentifier)
                throw new InvalidDataException();
            CreateNewProblem(sudokuType == XSudokuProblem.ProblemIdentifier, notify);
            try
            {
                CurrentProblem.InitProblem(Settings.Default.State.Substring(1, SudokuForm.TotalCellCount).ToCharArray(), Settings.Default.State.Substring(SudokuForm.TotalCellCount + 1, 16).ToCharArray(), null);
                if(Settings.Default.State.IndexOf('\n') > 0)
                {
                    CurrentProblem.LoadCandidates(Settings.Default.State.Substring(Settings.Default.State.IndexOf('\n') + 1), false);
                    CurrentProblem.LoadCandidates(Settings.Default.State.Substring(Settings.Default.State.LastIndexOf('\n') + 1), true);
                }
            }
            catch(Exception)
            {
                ;
            }
        }

        public void SyncWithGui(BaseProblem problemFromGui)
        {
            CurrentProblem = problemFromGui.Clone();
        }

        public void ResetProblem()
        {
            CurrentProblem = Backup.Clone();
        }

        public void BackupProblem()
        {
            Backup = CurrentProblem.Clone();
        }
        public void PushUndo(CoreValue value)
        {
            undoStack.Push(value);
        }
        public CoreValue PopUndo()
        {
            if(undoStack.Count > 0)
                return undoStack.Pop();
            return null;
        }
        public void ClearUndo()
        {
            undoStack.Clear();
        }

        public Boolean CanUndo()
        {
            return undoStack.Count > 0;
        }
    }
        // Hilfsklasse für den Fortschritt (falls noch nicht vorhanden)
    public class SudokuProgress
    {
        public string Message { get; set; }
        public long PassCount { get; set; }
        public long SolutionCount { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}