using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using static System.Windows.Forms.DataFormats;

namespace Sudoku;

internal class SudokuController: IDisposable
{
    private readonly ISudokuSettings settings;
    private IUserInteraction ui;
    public BaseProblem CurrentProblem { get; private set; }
    public BaseProblem Backup { get; private set; }
    private Stack<CoreValue> undoStack;
    public TimeSpan TotalGenerationTime { get; private set; }
    private TrickyProblems trickyProblems;
    private GenerationParameters generationParameters;
    private SudokuPrinterService printerService;

    // Events
    public event EventHandler MatrixChanged;
    public event EventHandler Generating;
    public Action<Object> MinimizedFailed;

    private Stopwatch solvingTimer = new Stopwatch();
    public SudokuController(ISudokuSettings settings, IUserInteraction ui)
    {
        undoStack = new Stack<CoreValue>();
        trickyProblems = new TrickyProblems(settings, ui);
        generationParameters = new GenerationParameters(settings);
        printerService = new SudokuPrinterService(WinFormsSettings.SudokuSize, settings);
        this.settings = settings;
        this.ui = ui;
    }

    public SudokuController(String filenname, Boolean loadCandidates, ISudokuSettings settings, IUserInteraction ui) : this(settings, ui)
    {
        CreateProblemFromFile(filenname, settings.GenerateNormalSudoku, settings.GenerateXSudoku, loadCandidates);
        BackupProblem();
    }
    public void Dispose()
    {
        printerService?.Dispose();
    }
    public void CreateNewProblem(bool xSudoku, bool notify = true)
    {
        CurrentProblem = xSudoku ? (BaseProblem)new XSudokuProblem(settings) : new SudokuProblem(settings);
        BackupProblem();
        if(notify) NotifyMatrixChanged();
    }

    public async Task Solve(bool findAllSolutions, IProgress<GenerationProgressState> progress, CancellationToken token)
    {
        if(CurrentProblem == null) return;

        int maxSolutions = findAllSolutions ? int.MaxValue : 1;
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
    }

    private void NotifyMatrixChanged()
    {
        MatrixChanged?.Invoke(this, EventArgs.Empty);
    }
    private void NotifyGeneration(Stopwatch stopwatch, CancellationToken token)
    {
        if(token.IsCancellationRequested) return;

        CurrentProblem.GenerationTime += stopwatch.Elapsed;
        stopwatch.Restart();
        Generating?.Invoke(this, EventArgs.Empty);
    }
    public void StartTimer()
    {
        solvingTimer.Restart();
    }
    public void StopTimer()
    {
        solvingTimer.Stop();
        CurrentProblem.SolvingTime += solvingTimer.Elapsed;
        solvingTimer.Reset();
    }
    public void PauseTimer()
    {
        solvingTimer.Stop();
    }
    public void ResumeTimer()
    {
        solvingTimer.Start();
    }
    public TimeSpan ElapsedTime { get { return solvingTimer.Elapsed; } }
    public Boolean IsTimerRunning { get { return solvingTimer.IsRunning; } }
    public void RestoreProblemState(bool notify = true)
    {
        Char sudokuType = (Char)settings.State[0];
        if(sudokuType != SudokuProblem.ProblemIdentifier && sudokuType != XSudokuProblem.ProblemIdentifier)
            throw new InvalidDataException();
        CreateNewProblem(sudokuType == XSudokuProblem.ProblemIdentifier, notify);
        try
        {
            SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
            fileService.InitProblem(settings.State.Substring(1, WinFormsSettings.TotalCellCount).ToCharArray(), settings.State.Substring(WinFormsSettings.TotalCellCount + 1, 16).ToCharArray(), null);
            if(settings.State.IndexOf('\n') > 0)
            {
                fileService.LoadCandidates(settings.State.Substring(settings.State.IndexOf('\n') + 1), false);
                fileService.LoadCandidates(settings.State.Substring(settings.State.LastIndexOf('\n') + 1), true);
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
    public async Task<Boolean> PublishTrickyProblems()
    {
        if(trickyProblems.Count > 0)
        {
            await trickyProblems.Publish();
            trickyProblems.Clear();
            return true;
        }
        return false;
    }
    public string TwitterURL
    {
        get
        {
            return Resources.TwitterURL + String.Format(Thread.CurrentThread.CurrentUICulture, Resources.TwitterText, (CurrentProblem is XSudokuProblem ? "X" : ""), SerializeProblem(false).Substring(1, WinFormsSettings.TotalCellCount));
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
    public void AddProblem(BaseProblem problem)
    {
        printerService.AddProblem(problem);
    }
    private async Task ProblemGenerated(BaseProblem problem, int index)
    {
        if(generationParameters.GenerateBooklet)
        {
            printerService.AddProblem(problem);
            if(settings.AutoSaveBooklet)
            {
                string filename = generationParameters.BaseDirectory + Path.DirectorySeparatorChar + "Problem-" + (index + 1).ToString() + "(" + problem.SeverityLevelText + ") (" + problem.SeverityLevel + ")" + settings.DefaultFileExtension;
                if(!SaveProblem(filename)) settings.AutoSaveBooklet = false;
            }
        }
    }
    public Boolean NewSudokuType()
    {
        Random rand = new Random(unchecked((int)DateTime.Now.Ticks));

        if(settings.GenerateXSudoku && settings.GenerateNormalSudoku)
            return rand.Next() % 2 == 0;
        else
            return settings.GenerateXSudoku;
    }

    public Boolean GenerateBooklet
    {
        get { return generationParameters.GenerateBooklet; }
    }
    public int CurrentBookletProblem
    {
        get { return generationParameters.CurrentProblem; }
    }
    public async Task GenerateBatch(int severityLevel, bool usePrecalculated, Action<object, String> finalize, IProgress<GenerationProgressState> progress, IProgress<MinimizationUpdate> minimizeProgress, CancellationToken token)
    {
        int count = generationParameters.GenerateBooklet ? settings.BookletSizeNew : 1;
        trickyProblems.Clear();
        generationParameters.CurrentProblem = 0;

        for(int i = 0; i < count; i++)
        {
            CreateNewProblem((i == 0) ? (CurrentProblem is XSudokuProblem) : NewSudokuType());

            generationParameters.Reset = false;
            generationParameters.PreAllocatedValues = 0;

            bool success = await GenerateCompleteProblem(generationParameters, severityLevel, progress, minimizeProgress, token);

            if(!success || token.IsCancellationRequested) return;

            await ProblemGenerated(CurrentProblem, i);

            generationParameters.CurrentProblem++;
        }

        String statusMessage;
        if(generationParameters.GenerateBooklet)
            statusMessage = String.Format(Thread.CurrentThread.CurrentCulture, Resources.NewProblems, generationParameters.CurrentProblem);
        else
        {
            statusMessage = String.Format(Thread.CurrentThread.CurrentCulture, Resources.NewProblemGenerated.Replace("\\n", Environment.NewLine), CurrentProblem.SeverityLevelText, CurrentProblem.nValues, generationParameters.CheckedProblems, generationParameters.TotalPasses);
        }
        finalize?.Invoke(this, statusMessage);
        generationParameters = new GenerationParameters(settings);
    }

    public async Task<Boolean> SudokuOfTheDay()
    {
        CreateNewProblem(settings.SudokuOfTheDay);
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        if(await fileService.SudokuOfTheDay())
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
        if(values.Count > settings.MaxHints)
        {
            List<BaseCell> hints = new List<BaseCell>();
            Random rand = new Random();
            int index;
            do
                if(!hints.Contains(values[(index = rand.Next(values.Count))]))
                    hints.Add(values[index]);
            while(hints.Count < settings.MaxHints);
            values = hints;
        }
        return values;
    }
    public async Task<bool> GenerateBaseProblem(GenerationParameters generationParameters, bool usePrecalculated, IProgress<GenerationProgressState> progress, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        int counter = 0;
        int minPreAllocations = CurrentProblem.MinimumValues;

        if(usePrecalculated)
        {
            if(await LoadProblem(CurrentProblem is XSudokuProblem))
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
                    CurrentProblem.SetReadOnly(generationParameters.Row, generationParameters.Col, false);

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
                    CurrentProblem.SetReadOnly(generationParameters.Row, generationParameters.Col, true);

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

            } while(!token.IsCancellationRequested && (generationParameters.Reset || CurrentProblem.NumDistinctValues() < WinFormsSettings.SudokuSize - 1 || generationParameters.PreAllocatedValues < minPreAllocations));
            }, token);
        }

        stopwatch.Stop();

        NotifyGeneration(stopwatch, token);
        BackupProblem();
        return true;
    }

    private async Task<bool> GenerateCompleteProblem(GenerationParameters generationParameters, int targetSeverity, IProgress<GenerationProgressState> progress, IProgress<MinimizationUpdate> mimimizeProgress, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        int counter = 0;
        TotalGenerationTime = TimeSpan.Zero;

        while(!token.IsCancellationRequested)
        {
            counter++;
            await GenerateBaseProblem(generationParameters, settings.UsePrecalculatedProblems, progress, token);

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
            else if(CurrentProblem.NumberOfSolutions == 1 && !token.IsCancellationRequested)
            {
                bool processProblem = true;

                if(settings.GenerateMinimalProblems)
                {
                    if(SeverityLevelInt() <= targetSeverity)
                    {
                        var minimized = await Minimize(targetSeverity, mimimizeProgress, token);
                        if(minimized != null)
                        {
                            CurrentProblem = minimized;
                            processProblem = true;
                        }
                        else
                        {
                            MinimizedFailed(this);
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
                        if(CurrentProblem.IsTricky && !settings.UsePrecalculatedProblems)
                        {
                            trickyProblems?.Add(CurrentProblem);
                        }
                        NotifyMatrixChanged();
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

    public async Task<BaseProblem> Minimize(int targetSeverity, IProgress<MinimizationUpdate> progress, CancellationToken token)
    {
        if(CurrentProblem == null) return null;

        BackupProblem();

        // Lokale Event-Handler, die an IProgress weiterleiten
        Action<object, BaseCell> onTestCell = (s, cell) =>
            progress?.Report(new MinimizationUpdate { Type = MinimizationUpdateType.TestCell, Cell = cell });

        Action<object, BaseCell> onResetCell = (s, cell) =>
            progress?.Report(new MinimizationUpdate { Type = MinimizationUpdateType.ResetCell, Cell = cell });

        Action<object, BaseProblem> onMinimizing = (s, problem) =>
            progress?.Report(new MinimizationUpdate { Type = MinimizationUpdateType.Status, Problem = problem });

        // Events abonnieren
        CurrentProblem.TestCell += onTestCell;
        CurrentProblem.ResetCell += onResetCell;
        CurrentProblem.Minimizing += onMinimizing;

        try
        {
            return await CurrentProblem.Minimize(targetSeverity, token);
        }
        finally
        {
            CurrentProblem.TestCell -= onTestCell;
            CurrentProblem.ResetCell -= onResetCell;
            CurrentProblem.Minimizing -= onMinimizing;
        }
    }
    private void FillCells(GenerationParameters generationParameters, int targetSeverity, Stopwatch stopwatch, CancellationToken token)
    {
        int counter = 0;
        CurrentProblem.ResetMatrix();

        // Fülle bis MinValues oder TargetSeverity
        while(CurrentProblem.nValues < settings.MinValues)
        {
            if((counter++ % 10) == 0) NotifyGeneration(stopwatch, token);

            generationParameters.NewValue();
            if(CurrentProblem.GetValue(generationParameters.Row, generationParameters.Col) == Values.Undefined && !token.IsCancellationRequested)
            {
                byte solValue = CurrentProblem.Solutions[0].GetValue(generationParameters.Row, generationParameters.Col);
                CurrentProblem.SetValue(generationParameters.Row, generationParameters.Col, solValue);
                CurrentProblem.SetReadOnly(generationParameters.Row, generationParameters.Col, true);
            }
        }
        while((SeverityLevelInt() & targetSeverity) == 0 && CurrentProblem.nValues < settings.MaxValues && !token.IsCancellationRequested)
        {
            if((counter++ % 10) == 0) NotifyGeneration(stopwatch, token);

            generationParameters.NewValue();
            if(CurrentProblem.GetValue(generationParameters.Row, generationParameters.Col) == Values.Undefined)
            {
                byte solValue = CurrentProblem.Solutions[0].GetValue(generationParameters.Row, generationParameters.Col);
                CurrentProblem.SetValue(generationParameters.Row, generationParameters.Col, solValue);
                CurrentProblem.SetReadOnly(generationParameters.Row, generationParameters.Col, true);
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
        if(grid.GetLength(0) != WinFormsSettings.SudokuSize || grid.GetLength(1) != WinFormsSettings.SudokuSize)
            throw new ArgumentException("grid must be SudokuSize x SudokuSize", nameof(grid));

        ValidationResult result = new ValidationResult();

        BackupProblem();

        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
        {
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
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
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.ReadProblem += (b) =>
        {
            CreateNewProblem(b);
            fileService.Sudoku = CurrentProblem;
        };
        fileService.LoadProblem(filename, normalSudoku, xSudoku, loadCandidates);

        NotifyMatrixChanged();
    }
    public bool IsCellReadOnly(int row, int col)
    {
        return CurrentProblem.IsCellReadOnly(row, col);
    }
    public void SetCellReadOnly(int row, int col, bool readOnly)
    {
        CurrentProblem.SetReadOnly(row, col, readOnly);
    }
    public int GetFilledCellCount { get { return CurrentProblem.nValues; } }
    public int GetComputedCellCount { get { return CurrentProblem.nComputedValues; } }
    public int GetVariableCellCount { get { return CurrentProblem.nVariableValues; } }
    public BaseCell[] GetNeighbors(int row, int col)
    {
        return CurrentProblem.GetNeighbors(row, col);
    }
    private async Task<Boolean> LoadProblem(Boolean xSudoku)
    {
        CreateNewProblem(xSudoku);
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        return await fileService.Load();
    }
    public void UpdateProblem(BaseProblem problem)
    {
        CurrentProblem = problem.Clone();
    }
    public void RestoreProblem()
    {
        if(CurrentProblem.Id != Backup.Id || CurrentProblem.Dirty)
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
    public Boolean SaveProblem(String filename)
    {
        StopTimer();
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        return fileService.SaveToFile(filename);
    }
    public void ExportHTML(String filename)
    {
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.SaveToHTMLFile(filename);
    }
    public string GetCellInfoText(int row, int col)
    {
        CultureInfo cultureInfo = Thread.CurrentThread.CurrentUICulture;
        BaseCell cell = CurrentProblem.Cell(row, col);

        String cellInfo = String.Format(cultureInfo, Resources.Cellinfo, row + 1, col + 1, (cell.ReadOnly ? " (" + Resources.ReadOnly + ") " : "")) + Environment.NewLine;
        if(cell.DefinitiveValue != Values.Undefined)
            cellInfo += Environment.NewLine + String.Format(cultureInfo, Resources.DefiniteValue) + cell.DefinitiveValue.ToString();
        else
            if(cell.FixedValue)
            cellInfo += Environment.NewLine + String.Format(cultureInfo, Resources.CellValue) + cell.CellValue.ToString();

        String directBlockedCells = "";
        String indirectBlockedCells = "";

        for(int i = 1; i <= WinFormsSettings.SudokuSize; i++)
        {
            if(i != cell.DefinitiveValue && i != cell.CellValue)
            {
                if(cell.Blocked(i))
                    directBlockedCells += (directBlockedCells.Length == 0 ? i.ToString() : ", " + i.ToString());
                else
                    if(cell.IndirectlyBlocked(i)) indirectBlockedCells += (indirectBlockedCells.Length == 0 ? i.ToString() : ", " + i.ToString());
            }
        }

        cellInfo += Environment.NewLine + String.Format(cultureInfo, Resources.DirectBlocks) + (directBlockedCells.Length == 0 ? Resources.None : directBlockedCells) +
            Environment.NewLine + String.Format(cultureInfo, Resources.IndirectBlocks) + (indirectBlockedCells.Length == 0 ? Resources.None : indirectBlockedCells);

        return cellInfo;
    }
    public void CreateBookletDirectory()
    {
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.CreateBookletDirectory(generationParameters);
    }
    public String SerializeProblem(Boolean includeROFlag)
    {
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        return fileService.Serialize(includeROFlag);
    }
    public String GenerationStatus(Boolean usePrecalculatedProblem, TimeSpan elapsed)
    {
        return (usePrecalculatedProblem ? String.Format(Thread.CurrentThread.CurrentCulture, Resources.RetrieveProblem) :
                (generationParameters.GenerateBooklet ? String.Format(Thread.CurrentThread.CurrentCulture, Resources.GeneratedProblems, generationParameters.CurrentProblem, settings.BookletSizeNew) + Environment.NewLine : String.Empty) +
                String.Format(Thread.CurrentThread.CurrentCulture, Resources.GeneratingStatus, generationParameters.CheckedProblems) + Environment.NewLine + String.Format(Thread.CurrentThread.CurrentCulture, Resources.CheckingStatus, generationParameters.TotalPasses + CurrentProblem.TotalPassCounter) +
                Environment.NewLine +
                Resources.PreAllocatedValues + generationParameters.PreAllocatedValues.ToString(Thread.CurrentThread.CurrentCulture)) +
                Environment.NewLine + Resources.TimeNeeded + String.Format("{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
    }
    public String GenerationAborted()
    {
        String result = String.Format(Thread.CurrentThread.CurrentCulture, Resources.GenerationAborted.Replace("\\n", Environment.NewLine),
            generationParameters.GenerateBooklet ? String.Format(Thread.CurrentThread.CurrentCulture, Resources.GeneratedProblems.Replace("\\n", Environment.NewLine), generationParameters.CurrentProblem, settings.BookletSizeNew) + Environment.NewLine : String.Empty,
            generationParameters.CheckedProblems, generationParameters.TotalPasses);
        generationParameters = new GenerationParameters(settings);

        return result;
    }
    public int GetSeverityLevel(int nProblems)
    {
        if(!(generationParameters.GenerateBooklet = (nProblems != 1)))
            return ui.GetSeverity();
        else
            return settings.SeverityLevel;
    }
    public int PrintResult { get { return printerService.PrintResult; } }
    public String PrintErrorMessage { get { return printerService.PrintErrorMessage; } }
    public void PrintBooklet()
    {
        printerService.ShowCandidates = false;
        if(NumberOfProblems < 1)
            ui.ShowInfo(Resources.NoProblems);
        else
        {
            try
            {
                printerService.Print();
            }
            catch(Win32Exception)
            {
                if(PrintResult != 0)
                    ui.ShowError(Resources.NotPrinted + Environment.NewLine + PrintErrorMessage);
                return;
            }
        }
    }
    public void InitializePrinterService()
    {
        printerService?.Dispose();
        printerService = new SudokuPrinterService(WinFormsSettings.SudokuSize, settings);
    }
    public void PrintSingleProblem(Boolean showCandidates)
    {
        SudokuPrinterService printerService = new SudokuPrinterService(WinFormsSettings.SudokuSize, settings);
        printerService.ShowCandidates = showCandidates;
        CurrentProblem.ResetMatrix();
        printerService.AddProblem(CurrentProblem);

        try
        {
            printerService.Print();
        }
        catch(Win32Exception)
        {
            if(PrintResult != 0)
                ui.ShowError(Resources.NotPrinted + Environment.NewLine + PrintErrorMessage);
        }
    }
    public void SaveApplicationState()
    {
        if(IsTimerRunning)
        {
            StopTimer();
        }
        settings.State = SerializeProblem(true);
        settings.Save();
    }
    public void Deserialize()
    {
        try
        {
            SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
            fileService.ReadProblem += (b) =>
            {
                CreateNewProblem(b);
                fileService.Sudoku = CurrentProblem;
            };
            fileService.Deserialize(settings.State);
        }
        catch(Exception)
        {
            RestoreProblemState();
        }
    }
    public void LoadProblemFilenames(DirectoryInfo directoryInfo, List<String> filenames, CancellationToken token)
    {

        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.LoadProblemFilenames(directoryInfo, filenames, token);
    }
    public int LoadProblems(List<String> filenames, Action<Object> progress, CancellationToken token)
    {
        Boolean ready = false;
        Random rand = new Random();

        BaseProblem tmp = CurrentProblem.Clone();

        while(!ready)
        {
            int problemNumber = rand.Next(0, filenames.Count - 1);
            try
            {
                SudokuController bookletController = new SudokuController(filenames[problemNumber], false, settings, ui);
                if(bookletController.CurrentProblem != null && (bookletController.CurrentProblem.SeverityLevelInt & settings.SeverityLevel) != 0)
                {
                    bookletController.CurrentProblem.FindSolutions(2, token);

                    if(bookletController.CurrentProblem.SolverTask != null && !bookletController.CurrentProblem.SolverTask.IsCompleted)
                        bookletController.CurrentProblem.SolverTask.Wait();

                    if(bookletController.CurrentProblem.NumberOfSolutions == 1)
                    {
                        bookletController.CurrentProblem.ResetMatrix();
                        bookletController.CurrentProblem.Filename = filenames[problemNumber];
                        AddProblem(bookletController.CurrentProblem);

                        progress?.Invoke(this);
                        // cooperative cancellation check instead of Application.DoEvents
                        if(token.IsCancellationRequested) break;
                    }
                }
            }
            catch
            {
                // do nothing
            }

            filenames.RemoveAt(problemNumber);
            ready = (NumberOfProblems == settings.BookletSizeExisting && !settings.BookletSizeUnlimited) || filenames.Count == 0 || token.IsCancellationRequested;
        }
        UpdateProblem(tmp);

        return NumberOfProblems;
    }
    public int NumberOfProblems => printerService.NumberOfProblems;
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