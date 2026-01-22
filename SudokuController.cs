using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
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
        public TimeSpan TotalGenerationTime { get; private set; }
        private TrickyProblems trickyProblems;

        // Events
        public event EventHandler MatrixChanged;
        public event EventHandler SolutionFound;
        public event EventHandler Generating;

        public SudokuController()
        {
            undoStack = new Stack<CoreValue>();
            trickyProblems = new TrickyProblems();
        }

        public SudokuController(String filenname, Boolean loadCandidates): this()
        {
            CreateProblemFromFile(filenname, Settings.Default.GenerateNormalSudoku, Settings.Default.GenerateXSudoku, loadCandidates);
            BackupProblem();
        }
        public void CreateNewProblem(bool xSudoku, bool notify = true)
        {
            CurrentProblem = xSudoku ? (BaseProblem)new XSudokuProblem() : new SudokuProblem();
            BackupProblem();
            if(notify) NotifyMatrixChanged();
        }

        public async Task Solve(bool findAllSolutions, IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            if(CurrentProblem == null) return;

            int maxSolutions = findAllSolutions? int.MaxValue: 1;
            var stopwatch = Stopwatch.StartNew();

            CurrentProblem.FindSolutions(maxSolutions, token);
            if(CurrentProblem.SolverTask != null)
            {
                while(!CurrentProblem.SolverTask.IsCompleted && CurrentProblem.NumberOfSolutions < maxSolutions)
                {
                    token.ThrowIfCancellationRequested();

                    progress?.Report(new GenerationProgressState
                    {
                        StatusText = Resources.Thinking,
                        PassCount = CurrentProblem.TotalPassCounter,
                        SolutionCount = CurrentProblem.NumberOfSolutions,
                        Elapsed = stopwatch.Elapsed
                    });

                    await Task.Delay(50);
                }

                await CurrentProblem.SolverTask;
            }
            stopwatch.Stop();
            CurrentProblem.SolvingTime = stopwatch.Elapsed;
            NotifyMatrixChanged();
            NotifySolutionFound();
        }

        private async void NotifyMatrixChanged()
        {
            MatrixChanged?.Invoke(this, EventArgs.Empty);
        }
        private void NotifySolutionFound()
        {
            SolutionFound?.Invoke(this, EventArgs.Empty);
        }
        private async void NotifyGeneration(Stopwatch stopwatch, CancellationToken token)
        {
            await Task.Delay(10, token);
            CurrentProblem.GenerationTime += stopwatch.Elapsed;
            stopwatch.Restart();
            Generating?.Invoke(this, EventArgs.Empty);
        }
        public void RestoreProblemState(bool notify = true)
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
        public Boolean HasTrickyProblems()
        {
            return trickyProblems.Count > 0;
        }
        public int NumberOfTrickyProblems { get { return trickyProblems.Count; } }
        public Boolean PublishTrickyProblems()
        {
            if(trickyProblems.Count > 0)
            {
                trickyProblems.Publish();
                trickyProblems.Clear();
                return true;
            }
            return false;
        }
        public string TwitterURL { get { return Resources.TwitterURL + String.Format(Thread.CurrentThread.CurrentUICulture, Resources.TwitterText, (CurrentProblem is XSudokuProblem ? "X" : ""), CurrentProblem.Serialize(false).Substring(1, SudokuForm.TotalCellCount)); } }

        public async Task<bool> Validate(IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            if(CurrentProblem == null) return false;

            BackupProblem();
            var stopwatch = Stopwatch.StartNew();
            bool result = false;

            try
            {
                CurrentProblem.FindSolutions(1, token);

                if(CurrentProblem.SolverTask != null)
                {
                    while(!CurrentProblem.SolverTask.IsCompleted)
                    {
                        token.ThrowIfCancellationRequested();

                        progress?.Report(new GenerationProgressState
                        {
                            StatusText = Resources.Checking,
                            PassCount = CurrentProblem.TotalPassCounter,
                            SolutionCount = CurrentProblem.NumberOfSolutions,
                            Elapsed = stopwatch.Elapsed
                        });

                        await Task.Delay(50);
                    }
                    await CurrentProblem.SolverTask;
                }

                result = CurrentProblem.ProblemSolved;
            }
            finally
            {
                stopwatch.Stop();
                RestoreProblem();
                NotifyMatrixChanged();
            }

            return result;
        }
        public async Task GenerateBatch(int count, GenerationParameters parameters, int severityLevel, bool usePrecalculated, Func<BaseProblem, int, Task> onProblemGenerated, IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            trickyProblems.Clear();
            parameters.CurrentProblem = 0;

            for(int i = 0; i < count; i++)
            {
                CreateNewProblem((i == 0)? (CurrentProblem is XSudokuProblem): parameters.NewSudokuType());

                parameters.Reset = false;
                parameters.PreAllocatedValues = 0;

                bool success = await GenerateCompleteProblem(parameters, severityLevel, progress, token);

                if(!success || token.IsCancellationRequested) return;

                if(onProblemGenerated != null)
                {
                    await onProblemGenerated(CurrentProblem, i);
                }

                parameters.CurrentProblem++;
            }
        }

        public Boolean SudokuOfTheDay()
        {
            CreateNewProblem(Settings.Default.SudokuOfTheDay);
            if(CurrentProblem.SudokuOfTheDay())
            {
                BackupProblem();
                NotifyMatrixChanged();
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<BaseCell> GetHints()
        {
            List<BaseCell> values = CurrentProblem.GetObviousCells();
            if(values.Count == 0)
                values = CurrentProblem.GetHints();
            if(values.Count > Settings.Default.MaxHints)
            {
                List<BaseCell> hints = new List<BaseCell>();
                Random rand = new Random();
                int index;
                do
                    if(!hints.Contains(values[(index = rand.Next(values.Count))]))
                        hints.Add(values[index]);
                while(hints.Count < Settings.Default.MaxHints);
                values = hints;
            }
            return values;
        }
        public async Task<bool> GenerateBaseProblem(GenerationParameters generationParameters, bool usePrecalculated, IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            int counter = 0;
            int minPreAllocations = CurrentProblem.Matrix.MinimumValues;

            if(usePrecalculated)
            {
                if(LoadProblem(CurrentProblem is XSudokuProblem))
                {
                    NotifyMatrixChanged();
                    BackupProblem();
                    return true;
                }
                else
                    usePrecalculated = false;
            }

            if(!usePrecalculated)
            {
                TotalGenerationTime += CurrentProblem.GenerationTime;
                RestoreProblem();
                await Task.Run(async () =>
                {
                    do
                    {
                        token.ThrowIfCancellationRequested();

                        counter++;
                        if(generationParameters.Reset)
                        {
                            CurrentProblem.SetValue(generationParameters.Row, generationParameters.Col, Values.Undefined);
                            CurrentProblem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly = false;

                            progress?.Report(new GenerationProgressState
                            {
                                Row = generationParameters.Row,
                                Col = generationParameters.Col,
                                Value = Values.Undefined,
                                StatusText = null
                            });
                        }

                        generationParameters.NewValue();
                        try
                        {
                            CurrentProblem.SetValue(generationParameters.Row, generationParameters.Col, generationParameters.GeneratedValue);
                            CurrentProblem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly = true;

                            bool updateText = (counter % 100) == 0;

                            progress?.Report(new GenerationProgressState
                            {
                                Row = generationParameters.Row,
                                Col = generationParameters.Col,
                                Value = generationParameters.GeneratedValue,
                                ReadOnly = true,
                                Elapsed = TotalGenerationTime,
                                StatusText = updateText ? Resources.Generating : null
                            });

                            if(generationParameters.PreAllocatedValues >= minPreAllocations)
                                generationParameters.CheckedProblems += 1;

                            generationParameters.PreAllocatedValues = CurrentProblem.nValues - CurrentProblem.nComputedValues;
                            generationParameters.Reset = !CurrentProblem.Resolvable();
                        }
                        catch(ArgumentException)
                        {
                            generationParameters.Reset = true;
                        }

                        if((counter % 100) == 0)
                        {
                            if(stopwatch.ElapsedMilliseconds > 50)
                            {
                                NotifyGeneration(stopwatch, token);
                            }
                        }

                    } while(!token.IsCancellationRequested && (generationParameters.Reset || CurrentProblem.NumDistinctValues() < SudokuForm.SudokuSize - 1 || generationParameters.PreAllocatedValues < minPreAllocations));
                }, token);
            }

            stopwatch.Stop();

            NotifyGeneration(stopwatch, token);
            BackupProblem();
            return true;
        }

        private async Task<bool> GenerateCompleteProblem(GenerationParameters generationParameters, int targetSeverity, IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            int counter = 0;
            TotalGenerationTime = TimeSpan.Zero;

            while(!token.IsCancellationRequested)
            {
                counter++;
                await GenerateBaseProblem(generationParameters, Settings.Default.UsePrecalculatedProblems, progress, token);

                if(token.IsCancellationRequested) return false;

                await Task.Run(() =>
                {
                    CurrentProblem.FindSolutions(2, token);
                    CurrentProblem.SolverTask?.Wait();
                });

                generationParameters.TotalPasses += CurrentProblem.TotalPassCounter;

                if(CurrentProblem.NumberOfSolutions == 0)
                {
                    generationParameters.Reset = true;
                }
                else if(CurrentProblem.NumberOfSolutions == 1 && !CurrentProblem.Aborted)
                {
                    bool processProblem = true;

                    if(Settings.Default.GenerateMinimalProblems)
                    {
                        if(SeverityLevelInt() <= targetSeverity)
                        {
                            var minimized = await Minimize(targetSeverity, token);
                            if(minimized != null)
                            {
                                CurrentProblem = minimized;
                                processProblem = true;
                            }
                            else
                            {
                                processProblem = false; // Minimierung fehlgeschlagen
                            }
                        }
                    }
                    else
                    {
                        FillCells(generationParameters, targetSeverity, stopwatch, token);
                    }

                    if((counter % 100) == 0 && stopwatch.ElapsedMilliseconds > 50)
                    {
                        NotifyGeneration(stopwatch, token);
                    }

                    if(processProblem && (SeverityLevelInt() & targetSeverity) != 0)
                    {
                        CurrentProblem.ResetMatrix();

                        if((SeverityLevelInt() & targetSeverity) != 0)
                        {
                            if(CurrentProblem.IsTricky && !Settings.Default.UsePrecalculatedProblems)
                            {
                                trickyProblems?.Add(CurrentProblem);
                            }
                            return true; // ERFOLG
                        }
                    }
                    generationParameters.Reset = true;
                }
                else
                {
                    generationParameters.Reset = false;
                }
            }

            return false;
        }

        public async Task<BaseProblem> Minimize(int targetSeverity, CancellationToken token)
        {
            if(CurrentProblem == null) return null;

            BackupProblem();
            return await CurrentProblem.Minimize(targetSeverity, token);
        }

        private void FillCells(GenerationParameters generationParameters, int targetSeverity, Stopwatch stopwatch, CancellationToken token)
        {
            int counter = 0;
            CurrentProblem.ResetMatrix();

            // Fülle bis MinValues oder TargetSeverity
            while(CurrentProblem.nValues < Settings.Default.MinValues)
            {
                if((counter++ % 10) == 0) NotifyGeneration(stopwatch, token);

                generationParameters.NewValue();
                if(CurrentProblem.GetValue(generationParameters.Row, generationParameters.Col) == Values.Undefined && !token.IsCancellationRequested)
                {
                    byte solValue = CurrentProblem.Solutions[0].GetValue(generationParameters.Row, generationParameters.Col);
                    CurrentProblem.SetValue(generationParameters.Row, generationParameters.Col, solValue);
                    CurrentProblem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly = true;
                }
            }
            while((SeverityLevelInt() & targetSeverity) == 0 && CurrentProblem.nValues < Settings.Default.MaxValues && !token.IsCancellationRequested)
            {
                if((counter++ % 10) == 0) NotifyGeneration(stopwatch, token);

                generationParameters.NewValue();
                if(CurrentProblem.GetValue(generationParameters.Row, generationParameters.Col) == Values.Undefined)
                {
                    byte solValue = CurrentProblem.Solutions[0].GetValue(generationParameters.Row, generationParameters.Col);
                    CurrentProblem.SetValue(generationParameters.Row, generationParameters.Col, solValue);
                    CurrentProblem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly = true;
                }
            }
        }

        private int SeverityLevelInt()
        {
            CurrentProblem.SeverityLevel = float.NaN;
            return CurrentProblem.SeverityLevelInt;
        }
        public ValidationResult ParseAndSync(string[,] grid)
        {
            if(grid == null) throw new ArgumentNullException(nameof(grid));
            if(grid.GetLength(0) != SudokuForm.SudokuSize || grid.GetLength(1) != SudokuForm.SudokuSize)
                throw new ArgumentException("grid must be SudokuSize x SudokuSize", nameof(grid));

            ValidationResult result = new ValidationResult();

            BackupProblem();

            for(int row = 0; row < SudokuForm.SudokuSize; row++)
            {
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                {
                    string raw = grid[row, col];
                    if(string.IsNullOrEmpty(raw)) continue;

                    string value = raw.Trim();
                    if(value.Length == 0)
                    {
                        CurrentProblem.SetValue(row, col, Values.Undefined);
                        continue;
                    }

                    if(!byte.TryParse(value, NumberStyles.Integer, Thread.CurrentThread.CurrentUICulture, out byte parsed))
                    {
                        result.IsValid = false;
                        result.addError(new ValidationResult.Error
                        {
                            Row = row,
                            Col = col,
                            Message = String.Format(Thread.CurrentThread.CurrentUICulture, Resources.InvalidValue, value, row + 1, col + 1)
                        });
                    }

                    try
                    {
                        CurrentProblem.SetValue(row, col, parsed);
                    }
                    catch(ArgumentException)
                    {
                        result.IsValid = false;
                        result.addError(new ValidationResult.Error
                        {
                            Row = row,
                            Col = col,
                            Message = String.Format(Thread.CurrentThread.CurrentUICulture, Resources.InvalidValue, value, row + 1, col + 1)
                        });
                    }
                }
            }
            if(!result.IsValid) RestoreProblem();

            return result;
        }
        public void CreateProblemFromFile(String filename, Boolean normalSudoku, Boolean xSudoku, Boolean loadCandidates)
        {
            StreamReader sr = null;
            try
            {
                Char sudokuType;
                sr = new StreamReader(filename.Replace("%20", " "), System.Text.Encoding.Default);
                sudokuType = (Char)sr.Read();
                if(sudokuType != SudokuProblem.ProblemIdentifier && sudokuType != XSudokuProblem.ProblemIdentifier) throw new InvalidDataException();
                if(sudokuType == SudokuProblem.ProblemIdentifier && normalSudoku || sudokuType == XSudokuProblem.ProblemIdentifier && xSudoku)
                {
                    CreateNewProblem(sudokuType == XSudokuProblem.ProblemIdentifier);
                    CurrentProblem.ReadFromFile(sr);
                    if(loadCandidates)
                    {
                        CurrentProblem.LoadCandidates(sr, false);
                        CurrentProblem.LoadCandidates(sr, true);
                    }
                }
            }
            catch(Exception) { throw; }
            finally { sr.Close(); }
            CurrentProblem.Filename = filename;
            NotifyMatrixChanged();
        }
        public bool IsCellReadOnly(int row, int col)
        {
            return CurrentProblem.Matrix.Cell(row, col).ReadOnly;
        }
        public void SetCellReadOnly(int row, int col, bool readOnly)
        {
            CurrentProblem.Matrix.Cell(row, col).ReadOnly = readOnly;
        }
        public int GetFilledCellCount { get { return CurrentProblem.nValues; } }
        public int GetComputedCellCount { get { return CurrentProblem.nComputedValues; } }
        public int GetVariableCellCount { get { return CurrentProblem.nVariableValues; } }
        public BaseCell[] GetNeighbors(int row, int col)
        {
            return CurrentProblem.GetNeighbors(row, col);
        }
        private Boolean LoadProblem(Boolean xSudoku)
        {
            CreateNewProblem(xSudoku);
            return CurrentProblem.Load();
        }
        public void UpdateProblem(BaseProblem problem)
        {
            CurrentProblem = problem.Clone();
        }
        public void RestoreProblem()
        {
            CurrentProblem = Backup.Clone();
        }
        public void BackupProblem()
        {
            Backup = CurrentProblem.Clone();
        }
        public Boolean IsProblemResolvable()
        {
            return CurrentProblem.Resolvable();
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
            CurrentProblem.Dirty = false;
        }
        public Boolean CanUndo()
        {
            return undoStack.Count > 0;
        }
        public void SaveProblem(String filename)
        {
            CurrentProblem.SaveToFile(filename);
        }
        public void ExportHTML(String filename)
        {
            CurrentProblem.SaveToHTMLFile(filename);
        }

        public void Cancel()
        {
            CurrentProblem?.Cancel();
        }
    }
    public class GenerationProgressState
    {
        public long PassCount { get; set; }
        public long SolutionCount { get; set; }
        public TimeSpan Elapsed { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public byte Value { get; set; }
        public bool ReadOnly { get; set; }
        public string StatusText { get; set; }
    }

    public class ValidationResult
    {
        public struct Error
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public string Message { get; set; }
        }
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<Error> Errors { get; set; }
        public void addError(Error error)
        {
            Errors.Add(error);
        }
        public ValidationResult()
        {
            IsValid = true;
            Message = string.Empty;
            Errors = new List<Error>();
        }
    }
}