using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Events
        public event EventHandler MatrixChanged;
        public event EventHandler SolutionFound;
        public event EventHandler Generating;

        public SudokuController()
        {
            undoStack = new Stack<CoreValue>();
        }

        public SudokuController(String filenname, Boolean loadCandidates) : this()
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

        public void UpdateProblemState(BaseProblem updatedProblem)
        {
            CurrentProblem = updatedProblem.Clone();
        }

        public async Task Solve(bool findAllSolutions, IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            if(CurrentProblem == null) return;

            ulong maxSolutions = findAllSolutions ? UInt64.MaxValue : 1;
            var stopwatch = Stopwatch.StartNew();

            CurrentProblem.FindSolutions(maxSolutions);
            if(CurrentProblem.SolverTask != null)
            {
                while(!CurrentProblem.SolverTask.IsCompleted)
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
        public async Task<bool> Validate(IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            if(CurrentProblem == null) return false;

            BackupProblem();
            var stopwatch = Stopwatch.StartNew();
            bool result = false;

            try
            {
                CurrentProblem.FindSolutions(1);

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
        public async Task GenerateBatch(int count, GenerationParameters parameters, int severityLevel, TrickyProblems trickyProblems, bool usePrecalculated, Func<BaseProblem, int, Task> onProblemGenerated, IProgress<GenerationProgressState> progress, CancellationToken token)
        {
            parameters.CurrentProblem = 0;

            for(int i = 0; i < count; i++)
            {
                CreateNewProblem((i == 0)? (CurrentProblem is XSudokuProblem): parameters.NewSudokuType());

                parameters.Reset = false;
                parameters.PreAllocatedValues = 0;

                bool success = await GenerateCompleteProblem(parameters, severityLevel, trickyProblems, progress, token);

                if(!success || token.IsCancellationRequested) return;

                if(onProblemGenerated != null)
                {
                    await onProblemGenerated(CurrentProblem, i);
                }

                parameters.CurrentProblem++;
            }
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

        public async Task<bool> GenerateCompleteProblem(GenerationParameters generationParameters, int targetSeverity, TrickyProblems trickyProblemsCollection, IProgress<GenerationProgressState> progress, CancellationToken token)
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
                    CurrentProblem.FindSolutions(2);
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
                            var minimized = await Minimize(targetSeverity);
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
                                trickyProblemsCollection?.Add(CurrentProblem);
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

        public async Task<BaseProblem> Minimize(int targetSeverity)
        {
            if(CurrentProblem == null) return null;

            BackupProblem();
            return await CurrentProblem.Minimize(targetSeverity);
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

        private Boolean LoadProblem(Boolean xSudoku)
        {
            CreateNewProblem(xSudoku);
            return CurrentProblem.Load();
        }

        public void SyncWithGui(BaseProblem problemFromGui)
        {
            CurrentProblem = problemFromGui.Clone();
        }

        public void RestoreProblem()
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
}