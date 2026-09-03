using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Sudoku.Core;
using Sudoku.Core.Solving;

namespace Sudoku.Application;

/// <summary>
/// Controls creation, generation, solving and persistence of Sudoku problems.
/// Provides operations for generation, validation, printing, undo management and state serialization.
/// </summary>
internal class SudokuController: IDisposable
{
    private readonly ISudokuSettings settings;
    private IUserInteraction ui;

    /// <summary>
    /// The currently active sudoku problem instance managed by this controller.
    /// </summary>
    public BaseProblem CurrentProblem { get; private set; } = default!;

    /// <summary>
    /// A backup clone of the last saved or restored problem used for undo/restore operations.
    /// </summary>
    public BaseProblem Backup { get; private set; } = default!;
    private Stack<CoreValue> undoStack;
    public TimeSpan TotalGenerationTime { get; private set; }
    private TrickyProblems trickyProblems;
    private GenerationParameters generationParameters;
    private readonly IPrintServiceFactory printServiceFactory;
    private IPrintService printerService;
    private readonly HintExplainer hintExplainer = new();
    private BaseMatrix? matrixBeforeLastHintSearch;

    // Events
    /// <summary>
    /// Raised when the sudoku matrix has changed and consumers should refresh their view.
    /// </summary>
    public event EventHandler? MatrixChanged;

    /// <summary>
    /// Raised periodically while problems are being generated to indicate generation progress.
    /// </summary>
    public event EventHandler? Generating;

    /// <summary>
    /// Callback invoked when a minimization attempt fails. The parameter may carry context information.
    /// </summary>
    public Action<Object>? MinimizedFailed;

    private Stopwatch solvingTimer = new Stopwatch();
    private static readonly TimeSpan SolverProgressInterval = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// Initializes a new controller instance with the provided settings and user interaction helper.
    /// </summary>
    /// <param name="settings">Application settings used for generation and behavior.</param>
    /// <param name="ui">UI callback interface used for user interaction and prompts.</param>
    /// <param name="printServiceFactory">Factory for creating print service instances.</param>
    public SudokuController(ISudokuSettings settings, IUserInteraction ui, IPrintServiceFactory printServiceFactory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(printServiceFactory);
        undoStack = new Stack<CoreValue>();
        trickyProblems = new TrickyProblems(settings, ui);
        generationParameters = new GenerationParameters(settings);
        this.printServiceFactory = printServiceFactory;
        printerService = printServiceFactory.Create();
        this.settings = settings;
        this.ui = ui;
        // Initialize default problem placeholders so other members can assume non-null
        CreateNewProblem(false, false);
        BackupProblem();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SudokuController"/> by loading a problem from the specified file.
    /// </summary>
    /// <param name="filenname">Path to the problem file to load.</param>
    /// <param name="loadCandidates">If true, candidate lists are loaded together with the problem.</param>
    /// <param name="settings">Application settings used by the controller.</param>
    /// <param name="ui">UI interaction helper used for prompts and messages.</param>
    /// <param name="printServiceFactory">Factory for creating print service instances.</param>
    public SudokuController(String filenname, Boolean loadCandidates, ISudokuSettings settings, IUserInteraction ui, IPrintServiceFactory printServiceFactory) : this(settings, ui, printServiceFactory)
    {
        ArgumentNullException.ThrowIfNull(filenname);
        try
        {
            CreateProblemFromFile(filenname, settings.GenerateNormalSudoku, settings.GenerateXSudoku, loadCandidates);
            BackupProblem();
        }
        catch (ArgumentException)
        {
            ui?.ShowError(String.Format(Thread.CurrentThread.CurrentUICulture, Resources.InvalidSudokuFile, filenname));
            CreateNewProblem(false);
            BackupProblem();
        }
        catch (InvalidDataException)
        {
            ui?.ShowError(Resources.InvalidSudokuIdentifier);
            CreateNewProblem(false);
            BackupProblem();
        }
        catch (System.Text.Json.JsonException ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
            CreateNewProblem(false);
            BackupProblem();
        }
        catch (IOException ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
            CreateNewProblem(false);
            BackupProblem();
        }
        catch (UnauthorizedAccessException ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
            CreateNewProblem(false);
            BackupProblem();
        }
        catch (Exception ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
            CreateNewProblem(false);
            BackupProblem();
        }
    }

    /// <summary>
    /// Disposes controller resources such as the internal printer service.
    /// </summary>
    public void Dispose()
    {
        printerService?.Dispose();
    }

    /// <summary>
    /// Creates a new problem instance. When <paramref name="xSudoku"/> is true an X-Sudoku is created.
    /// Optionally notifies listeners about the matrix change.
    /// </summary>
    /// <param name="xSudoku">True to create an X-Sudoku; otherwise a standard sudoku.</param>
    /// <param name="notify">If true, invoke <see cref="NotifyMatrixChanged"/> after creation.</param>
    public void CreateNewProblem(bool xSudoku, bool notify = true)
    {
        CurrentProblem = xSudoku ? (BaseProblem)new XSudokuProblem(settings) : new SudokuProblem(settings);
        BackupProblem();
        if (notify) NotifyMatrixChanged();
    }

    /// <summary>
    /// Solves the current Sudoku problem. If <paramref name="findAllSolutions"/> is true, searches for all solutions; otherwise stops after the first solution.
    /// Progress updates are reported via the provided <paramref name="progress"/> reporter. The operation can be cancelled via <paramref name="token"/>.
    /// </summary>
    /// <param name="findAllSolutions">When true, find all solutions instead of stopping at the first.</param>
    /// <param name="progress">Optional progress reporter for generation/solver updates.</param>
    /// <param name="token">Cancellation token to cancel solving.</param>
    public async Task Solve(bool findAllSolutions, IProgress<GenerationProgressState>? progress, CancellationToken token)
    {
        if(CurrentProblem == null) return;
        // no-op formatting adjustment

        int maxSolutions = findAllSolutions ? int.MaxValue : 1;
        var stopwatch = Stopwatch.StartNew();
        await CurrentProblem.FindSolutions(maxSolutions, token);
        if(CurrentProblem.SolverTask != null)
        {
            await MonitorSolverTask(Resources.Thinking, progress, stopwatch, token);
        }

        stopwatch.Stop();
        CurrentProblem.SolvingTime = stopwatch.Elapsed;
        NotifyMatrixChanged();
    }
    /// <summary>
    /// Monitors the solver task and reports progress updates at regular intervals.
    /// </summary>
    /// <remarks>
    /// This method runs a loop that periodically checks the solver task for completion and reports
    /// progress updates when pass count, solution count, or task completion changes. The monitoring
    /// continues until the solver task completes or the cancellation token is cancelled.
    /// </remarks>
    /// <param name="statusText">The status message to include in each progress report.</param>
    /// <param name="progress">The progress reporter for communicating updates to subscribers. Can be null to skip reporting.</param>
    /// <param name="stopwatch">The stopwatch tracking elapsed time since solving started.</param>
    /// <param name="token">The cancellation token to stop monitoring if requested.</param>
    /// <returns>A task that completes when the solver task finishes or monitoring is cancelled.</returns>
    private async Task MonitorSolverTask(string statusText, IProgress<GenerationProgressState>? progress, Stopwatch stopwatch, CancellationToken token)
    {
        Task? solverTask = CurrentProblem.SolverTask;
        if(solverTask == null) return;

        long lastPass = -1;
        long lastSolution = -1;

        while(true)
        {
            Task delayTask = Task.Delay(SolverProgressInterval, token);
            Task completedTask = await Task.WhenAny(solverTask, delayTask).ConfigureAwait(false);

            if(progress != null)
            {
                long passCount = CurrentProblem.TotalPassCounter;
                long solutionCount = CurrentProblem.NumberOfSolutions;

                if(passCount != lastPass || solutionCount != lastSolution || completedTask == solverTask)
                {
                    lastPass = passCount;
                    lastSolution = solutionCount;

                    progress.Report(new GenerationProgressState
                    {
                        StatusText = statusText,
                        PassCount = passCount,
                        SolutionCount = solutionCount,
                        Elapsed = stopwatch.Elapsed
                    });
                }
            }

            if(completedTask == solverTask)
            {
                await solverTask.ConfigureAwait(false);
                break;
            }
        }
    }

    /// <summary>
    /// Notifies subscribers that the Sudoku matrix has changed by raising the <see cref="MatrixChanged"/> event.
    /// </summary>
    /// <remarks>
    /// Invokes <see cref="MatrixChanged"/> with <see cref="EventArgs.Empty"/> if any handlers are attached.
    /// This method is intended for internal use to signal UI or other components to refresh their view.
    /// </remarks>
    /// <seealso cref="MatrixChanged"/>
    private void NotifyMatrixChanged()
    {
        MatrixChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Notifies subscribers about the generation progress and updates the generation time metric.
    /// </summary>
    /// <param name="stopwatch">The stopwatch tracking the elapsed time for the current generation phase.</param>
    /// <param name="token">The cancellation token to check for cancellation requests.</param>
    /// <remarks>
    /// This method accumulates the elapsed time in CurrentProblem.GenerationTime, restarts the stopwatch 
    /// for the next measurement interval, and raises the Generating event to notify subscribers of progress.
    /// If cancellation has been requested, the method returns without executing any operations.
    /// </remarks>
    private void NotifyGeneration(Stopwatch stopwatch, CancellationToken token)
    {
        if(token.IsCancellationRequested) return;

                if (CurrentProblem != null) CurrentProblem.GenerationTime += stopwatch.Elapsed;
        stopwatch.Restart();
        Generating?.Invoke(this, EventArgs.Empty);
    }
    /// <summary>
    /// Starts or restarts the internal solving timer.
    /// </summary>
    public void StartTimer()
    {
        solvingTimer.Restart();
    }

    /// <summary>
    /// Stops the solving timer and accumulates elapsed time into the current problem's SolvingTime.
    /// </summary>
    public void StopTimer()
    {
        solvingTimer.Stop();
                if (CurrentProblem != null) CurrentProblem.SolvingTime += solvingTimer.Elapsed;
        solvingTimer.Reset();
    }

    /// <summary>
    /// Pauses the solving timer without resetting elapsed time.
    /// </summary>
    public void PauseTimer()
    {
        solvingTimer.Stop();
    }

    /// <summary>
    /// Resumes the solving timer.
    /// </summary>
    public void ResumeTimer()
    {
        solvingTimer.Start();
    }

    /// <summary>
    /// Gets the elapsed time of the internal solving timer.
    /// </summary>
    public TimeSpan ElapsedTime { get { return solvingTimer.Elapsed; } }

    /// <summary>
    /// Indicates whether the internal solving timer is currently running.
    /// </summary>
    public Boolean IsTimerRunning { get { return solvingTimer.IsRunning; } }

    /// <summary>
    /// Restores the application state from the settings.State string and rebuilds the current problem.
    /// Throws InvalidDataException when the stored state is not recognized.
    /// </summary>
    /// <param name="notify">When true, notifies listeners after the state is restored.</param>
    public void RestoreProblemState(bool notify = true)
    {
        Char sudokuType = (Char)settings.State[0];
        if(sudokuType != SudokuProblem.ProblemIdentifier && sudokuType != XSudokuProblem.ProblemIdentifier)
            throw new InvalidDataException();
        CreateNewProblem(sudokuType == XSudokuProblem.ProblemIdentifier, notify);
        try
        {
            // CurrentProblem is non-nullable and initialized by CreateNewProblem
            var fileService = new SudokuFileService(CurrentProblem, settings, ui);
            fileService.InitProblem(settings.State.Substring(1, SudokuGrid.TotalCellCount).ToCharArray(), settings.State.Substring(SudokuGrid.TotalCellCount + 1, 16).ToCharArray(), null);

            if(settings.State.IndexOf('\n') > 0)
            {
                fileService.LoadCandidates(settings.State.Substring(settings.State.IndexOf('\n') + 1), false);
                fileService.LoadCandidates(settings.State.Substring(settings.State.LastIndexOf('\n') + 1), true);
            }
        }
        catch(Exception)
        {
            // ignore restore errors
        }
    }
    /// <summary>
    /// Returns true when there are collected tricky problems to publish.
    /// </summary>
    public Boolean HasTrickyProblems()
    {
        return trickyProblems.Count > 0;
    }

    /// <summary>
    /// Gets the count of tricky problems currently collected.
    /// </summary>
    public int NumberOfTrickyProblems { get { return trickyProblems.Count; } }

    /// <summary>
    /// Publishes collected tricky problems asynchronously and clears the collection when successful.
    /// </summary>
    /// <returns>True when publishing succeeded and problems were cleared; otherwise false.</returns>
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

    /// <summary>
    /// Gets a URL string suitable for posting the current puzzle to Twitter including a serialized puzzle representation.
    /// </summary>
    public string TwitterURL
    {
        get
        {
            return Resources.TwitterURL + String.Format(Thread.CurrentThread.CurrentUICulture, Resources.TwitterText, (CurrentProblem is XSudokuProblem ? "X" : ""), SerializeProblem(false).Substring(1, SudokuGrid.TotalCellCount));
        }
    }

    /// <summary>
    /// Validates the current problem by attempting to find a solution. Returns true when the problem is solvable.
    /// Progress updates are reported to <paramref name="progress"/> and the operation can be cancelled with <paramref name="token"/>.
    /// </summary>
    /// <param name="progress">Optional progress reporter for validation steps.</param>
    /// <param name="token">Cancellation token to cancel validation.</param>
    /// <returns>True if the current problem is solvable; otherwise false.</returns>
    public async Task<bool> Validate(IProgress<GenerationProgressState>? progress, CancellationToken token)
    {
        BackupProblem();
        var stopwatch = Stopwatch.StartNew();
        bool result = false;

        try
        {
            await CurrentProblem.FindSolutions(1, token);

            if(CurrentProblem.SolverTask != null)
            {
                await MonitorSolverTask(Resources.Checking, progress, stopwatch, token);
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
    /// <summary>
    /// Adds a problem to the internal printer service for later printing.
    /// </summary>
    /// <param name="problem">Problem to add to the print queue.</param>
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
                string filename = generationParameters.BaseDirectory + Path.DirectorySeparatorChar + "Problem-" + (index + 1).ToString() + "(" + problem.SeverityLevelCategory.ToDisplayText() + ") (" + problem.SeverityLevel + ")" + settings.DefaultFileExtension;
                if(!SaveProblem(filename)) settings.AutoSaveBooklet = false;
            }
        }
    }
    /// <summary>
    /// Randomly chooses whether to create an X-Sudoku when both normal and X-Sudoku generation are enabled.
    /// </summary>
    /// <returns>True to create an X-Sudoku; otherwise false.</returns>
    public Boolean NewSudokuType()
    {
        Random rand = new Random(unchecked((int)DateTime.Now.Ticks));

        if(settings.GenerateXSudoku && settings.GenerateNormalSudoku)
            return rand.Next() % 2 == 0;
        else
            return settings.GenerateXSudoku;
    }

    /// <summary>
    /// Gets a value indicating whether generation is currently producing a booklet of problems.
    /// </summary>
    public Boolean GenerateBooklet
    {
        get { return generationParameters.GenerateBooklet; }
    }
    /// <summary>
    /// Gets the index of the current problem within the booklet generation sequence.
    /// </summary>
    public int CurrentBookletProblem
    {
        get { return generationParameters.CurrentProblem; }
    }

    /// <summary>
    /// Generates a batch of puzzles according to the current generation parameters.
    /// </summary>
    /// <param name="severityLevel">Target severity level mask for generated puzzles.</param>
    /// <param name="usePrecalculated">When true, attempt to use precomputed problems.</param>
    /// <param name="finalize">Optional callback invoked after batch generation completes with a status message.</param>
    /// <param name="progress">Progress reporter for generation steps.</param>
    /// <param name="minimizeProgress">Progress reporter for minimization steps.</param>
    /// <param name="token">Cancellation token to cancel the operation.</param>
    /// <returns>A task that completes when batch generation finishes.</returns>
    public async Task GenerateBatch(int severityLevel, bool usePrecalculated, Action<object, String> finalize, IProgress<GenerationProgressState>? progress, IProgress<MinimizationUpdate>? minimizeProgress, CancellationToken token)
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
            statusMessage = String.Format(Thread.CurrentThread.CurrentCulture, Resources.NewProblemGenerated.Replace("\\n", Environment.NewLine), CurrentProblem.SeverityLevelCategory.ToDisplayText(), CurrentProblem.nValues, generationParameters.CheckedProblems, generationParameters.TotalPasses);
        }
        finalize?.Invoke(this, statusMessage);
        generationParameters = new GenerationParameters(settings);
    }

    /// <summary>
    /// Asynchronously creates and saves the "Sudoku of the Day" based on the current settings.
    /// </summary>
    /// <returns>
    /// A <see cref="Task{Boolean}"/> that completes with <c>true</c> if the Sudoku of the Day
    /// was saved successfully; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method:
    /// - Creates a new problem using <c>settings.SudokuOfTheDay</c>.
    /// - Uses <see cref="SudokuFileService"/> to persist the problem.
    /// - If persistence succeeds, it calls <c>BackupProblem()</c> and <c>NotifyMatrixChanged()</c>.
    /// Side effects include mutating <c>CurrentProblem</c> and raising UI notifications.
    /// Exceptions thrown by <see cref="SudokuFileService.SudokuOfTheDay"/> are propagated to the caller.
    /// </remarks>
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

    /// <summary>
    /// Returns a list of hint cells for the current problem. The method prefers "obvious" cells and limits the number of hints to the configured maximum.
    /// </summary>
    public List<BaseCell> GetHints()
    {
        matrixBeforeLastHintSearch = CurrentProblem.Matrix.Clone();
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

    /// <summary>
    /// Liefert zu jeder "orangenen" Hint-Zelle (die keinen direkten Single hat) den Namen
    /// der einfachsten Technik, die dort aktuell greift.
    /// </summary>
    public IReadOnlyDictionary<BaseCell, StrategyFinding> ExplainHints(List<BaseCell> hints)
    {
        if(matrixBeforeLastHintSearch == null)
            return new Dictionary<BaseCell, StrategyFinding>();

        // "Orange" = Zellen, die VOR der Tiefensuche mehr als einen Kandidaten hatten
        // (auf dem sauberen Snapshot geprüft, nicht auf der inzwischen mutierten Live-Matrix!).
        var positionsNeedingExplanation = hints
            .Select(c => (c.Row, c.Col))
            .Where(pos => matrixBeforeLastHintSearch.Cell(pos.Row, pos.Col).nPossibleValues != 1);

        var findingsByPosition = hintExplainer.ExplainPositions(matrixBeforeLastHintSearch, positionsNeedingExplanation);

        var result = new Dictionary<BaseCell, StrategyFinding>();
        foreach(var (pos, finding) in findingsByPosition)
            result[CurrentProblem.Matrix.Cell(pos.Row, pos.Col)] = finding;   // zurück auf Live-Zellen mappen

        return result;
    }
    /// <summary>
    /// Generates the base puzzle either by loading a precalculated problem or by constructing one incrementally.
    /// </summary>
    /// <param name="generationParameters">Generation state and working parameters used during base problem construction.</param>
    /// <param name="usePrecalculated">
    /// If <c>true</c> attempt to load a precalculated problem from storage. On load failure the method falls back to incremental generation.
    /// </param>
    /// <param name="progress">Optional progress reporter that receives cell-level updates and generation status messages.</param>
    /// <param name="token">Cancellation token that can be used to abort generation. OperationCanceledException will be thrown when cancelled.</param>
    /// <returns>
    /// <c>true</c> when a base problem was successfully produced or loaded. The method normally returns <c>true</c> on successful completion;
    /// cancellation or exceptions from called routines may short-circuit execution by throwing.
    /// </returns>
    /// <remarks>
    /// - When <paramref name="usePrecalculated"/> is <c>true</c> this method first attempts to load a saved problem via <see cref="LoadProblem(Boolean)"/>.
    ///   If loading succeeds the loaded problem is applied, subscribers are notified and a backup is created.
    /// - If precalculated data is not used or not available, the method performs incremental generation on a background task.
    /// - Progress reporting is throttled (~75ms) and also forced when the active cell changes or at regular update intervals.
    /// - Timing metrics are tracked in <see cref="TotalGenerationTime"/> and in <see cref="CurrentProblem.GenerationTime"/>; callers receive periodic
    ///   notifications via <see cref="NotifyGeneration(Stopwatch, CancellationToken)"/>.
    /// - The method updates <paramref name="generationParameters"/> (pre-allocated count, reset flag, checked problems, etc.) while constructing the base grid.
    /// </remarks>
    public async Task<bool> GenerateBaseProblem(GenerationParameters generationParameters, bool usePrecalculated, IProgress<GenerationProgressState>? progress, CancellationToken token)
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

            TimeSpan throttlingInterval = TimeSpan.FromMilliseconds(75);
            long minProgressTicks = Math.Max(1, (long)(Stopwatch.Frequency * throttlingInterval.TotalSeconds));
            long nextProgressTick = 0;
            int lastRow = -1;
            int lastCol = -1;
            byte lastValue = byte.MaxValue;
            bool lastReadOnly = false;

            void ReportProgressIfNeeded(int row, int col, byte value, bool readOnly, string? statusText, bool force)
            {
                if(progress == null) return;

                long now = Stopwatch.GetTimestamp();
                bool cellChanged = row != lastRow || col != lastCol || value != lastValue || readOnly != lastReadOnly;

                if(!force && !cellChanged && now < nextProgressTick)
                    return;

                nextProgressTick = now + minProgressTicks;
                lastRow = row;
                lastCol = col;
                lastValue = value;
                lastReadOnly = readOnly;

                progress.Report(new GenerationProgressState
                {
                    Row = row,
                    Col = col,
                    Value = value,
                    ReadOnly = readOnly,
                    StatusText = statusText,
                    Elapsed = TotalGenerationTime + stopwatch.Elapsed
                });
            }

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

                        ReportProgressIfNeeded(generationParameters.Row, generationParameters.Col, Values.Undefined, false, null, true);
                    }

                    generationParameters.NewValue();
                    try
                    {
                        CurrentProblem.SetValue(generationParameters.Row, generationParameters.Col, generationParameters.GeneratedValue);
                        CurrentProblem.SetReadOnly(generationParameters.Row, generationParameters.Col, true);

                        bool updateText = (counter % 100) == 0;
                        string? statusText = updateText ? Resources.Generating : null;

                        ReportProgressIfNeeded(generationParameters.Row, generationParameters.Col, generationParameters.GeneratedValue, true, statusText, updateText);

                        if(generationParameters.PreAllocatedValues >= minPreAllocations)
                            generationParameters.CheckedProblems += 1;

                        generationParameters.PreAllocatedValues = CurrentProblem.nValues - CurrentProblem.nComputedValues;
                        generationParameters.Reset = !CurrentProblem.Resolvable();
                    }
                    catch(ArgumentException)
                    {
                        generationParameters.Reset = true;
                    }

                    if((counter % 100) == 0 && stopwatch.ElapsedMilliseconds > 50)
                    {
                        NotifyGeneration(stopwatch, token);
                    }

                } while(!token.IsCancellationRequested && (generationParameters.Reset || CurrentProblem.NumDistinctValues() < SudokuGrid.SudokuSize - 1 || generationParameters.PreAllocatedValues < minPreAllocations));
            }, token);
        }

        stopwatch.Stop();

        NotifyGeneration(stopwatch, token);
        BackupProblem();
        return true;
    }

    /// <summary>
    /// Repeatedly generates a complete Sudoku problem until a problem matching the requested
    /// <paramref name="targetSeverity"/> is produced or the operation is cancelled.
    /// </summary>
    /// <param name="generationParameters">
    /// State and working parameters used during generation (row/col selection, pre-allocations, reset flag, counters).
    /// The object is mutated during generation and returned to the caller for subsequent calls.
    /// </param>
    /// <param name="targetSeverity">
    /// Bitmask describing the desired severity level(s) a completed problem must match to be considered successful.
    /// </param>
    /// <param name="progress">
    /// Optional progress reporter that receives solver/generation updates (<see cref="GenerationProgressState"/>).
    /// </param>
    /// <param name="minimizeProgress">
    /// Optional progress reporter that receives minimization updates (<see cref="MinimizationUpdate"/>).
    /// </param>
    /// <param name="token">Cancellation token used to abort generation. The method returns <c>false</c> when cancelled.</param>
    /// <returns>
    /// A <see cref="Task{Boolean}"/> that completes with <c>true</c> when a problem matching <paramref name="targetSeverity"/>
    /// was successfully generated and applied to <see cref="CurrentProblem"/>; otherwise <c>false</c> (cancelled or no match).
    /// </returns>
    /// <remarks>
    /// The method performs the following high-level steps in a loop:
    /// - Calls <see cref="GenerateBaseProblem"/> to produce a candidate base grid (or load a precalculated one).
    /// - Invokes the solver to find up to two solutions and monitors solver progress.
    /// - If exactly one solution exists, either attempts minimization (when settings request minimal problems)
    ///   or fills additional cells until the target severity is reached.
    /// - When a candidate meets the <paramref name="targetSeverity"/>, the method resets the matrix,
    ///   optionally collects "tricky" problems, notifies listeners via <see cref="NotifyMatrixChanged"/>,
    ///   and returns <c>true</c>.
    /// The method mutates <paramref name="generationParameters"/> and <see cref="CurrentProblem"/>, may raise
    /// generation events via <see cref="NotifyGeneration(Stopwatch, CancellationToken)"/>, and triggers
    /// <see cref="MinimizedFailed"/> when minimization fails. Progress is reported through the provided reporters.
    /// The operation is cooperative with <paramref name="token"/> and will return <c>false</c> if cancellation is requested.
    /// </remarks>
    private async Task<bool> GenerateCompleteProblem(GenerationParameters generationParameters, int targetSeverity, IProgress<GenerationProgressState>? progress, IProgress<MinimizationUpdate>? minimizeProgress, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        int counter = 0;
        TotalGenerationTime = TimeSpan.Zero;

        while(!token.IsCancellationRequested)
        {
            counter++;
            await GenerateBaseProblem(generationParameters, settings.UsePrecalculatedProblems, progress, token);

            if(token.IsCancellationRequested) return false;

            await CurrentProblem.FindSolutions(2, token);
            await MonitorSolverTask(Resources.Checking, progress, stopwatch, token);

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
                        var minimized = await Minimize(targetSeverity, minimizeProgress, token);
                        if(minimized != null)
                        {
                            CurrentProblem = minimized;
                            processProblem = true;
                        }
                        else
                        {
                            MinimizedFailed?.Invoke(this);
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

    /// <summary>
    /// Attempts to minimize the currently loaded <see cref="BaseProblem"/> to reach the specified severity.
    /// The method creates a backup of the current problem, subscribes to problem events and forwards
    /// progress updates to the provided <see cref="IProgress{MinimizationUpdate}"/> instance.
    /// </summary>
    /// <param name="targetSeverity">Target severity level (bitmask or level) to reach during minimization.</param>
    /// <param name="progress">Optional progress reporter that receives <see cref="MinimizationUpdate"/> objects for cell tests, resets and status updates.</param>
    /// <param name="token">Cancellation token used to cancel the minimization operation.</param>
    /// <returns>
    /// A <see cref="BaseProblem"/> instance representing the minimized problem if minimization succeeds;
    /// otherwise returns the result produced by the underlying minimization call. Returns <c>null</c> immediately if no current problem is set.
    /// </returns>
    /// <remarks>
    /// Subscribes to <see cref="BaseProblem.TestCell"/>, <see cref="BaseProblem.ResetCell"/> and <see cref="BaseProblem.Minimizing"/> events
    /// and ensures they are unsubscribed in a <c>finally</c> block. The actual minimization is delegated to
    /// <see cref="BaseProblem.Minimize(int, BaseProblem.MinimizeAlgorithm, CancellationToken)"/> using
    /// <see cref="BaseProblem.MinimizeAlgorithm.Calculate"/>.
    /// </remarks>
    public async Task<BaseProblem?> Minimize(int targetSeverity, IProgress<MinimizationUpdate>? progress, CancellationToken token)
    {
        // CurrentProblem is initialized in the controller construction.
        BackupProblem();

        // Lokale Event-Handler, die an IProgress weiterleiten
        Action<object, BaseCell> onTestCell = (s, cell) =>
            progress?.Report(new MinimizationUpdate { Type = MinimizationUpdateType.TestCell, Cell = cell });

        Action<object, BaseCell> onResetCell = (s, cell) =>
            progress?.Report(new MinimizationUpdate { Type = MinimizationUpdateType.ResetCell, Cell = cell });

        Action<object, BaseProblem?> onMinimizing = (s, problem) =>
            progress?.Report(new MinimizationUpdate { Type = MinimizationUpdateType.Status, Problem = problem });

        // Events abonnieren
        CurrentProblem.TestCell += onTestCell;
        CurrentProblem.ResetCell += onResetCell;
        CurrentProblem.Minimizing += onMinimizing;

        try
        {
             return await CurrentProblem.Minimize(targetSeverity, BaseProblem.MinimizeAlgorithm.Calculate, token);
        }
        finally
        {
            CurrentProblem.TestCell -= onTestCell;
            CurrentProblem.ResetCell -= onResetCell;
            CurrentProblem.Minimizing -= onMinimizing;
        }
    }

    /// <summary>
    /// Fills cells in the puzzle with values from the solution based on generation parameters and severity level.
    /// </summary>
    /// <remarks>
    /// This method populates the puzzle matrix in two phases:
    /// 1. First phase: Fills cells until the minimum number of values (MinValues) is reached.
    /// 2. Second phase: Continues filling cells to achieve the target severity level, up to the maximum number of values (MaxValues).
    /// 
    /// Each filled cell is marked as read-only to prevent modification during gameplay.
    /// The method respects cancellation requests and provides periodic progress notifications every 10 iterations.
    /// </remarks>
    /// <param name="generationParameters">The parameters controlling which cells are selected for filling during the generation process.</param>
    /// <param name="targetSeverity">The target difficulty level to achieve for the puzzle.</param>
    /// <param name="stopwatch">The stopwatch for tracking elapsed time during generation.</param>
    /// <param name="token">The cancellation token to allow cancellation of the fill operation.</param>
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

    /// <summary>
    /// Invalidates any cached floating-point severity value and returns the problem's integer severity.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="CurrentProblem.SeverityLevel"/> to <see cref="float.NaN"/> forces
    /// <see cref="CurrentProblem.SeverityLevelInt"/> to recompute its value (if it derives from the float value).
    /// </remarks>
    /// <returns>
    /// The integer representation of the current problem's severity after forcing recalculation.
    /// </returns>
    private int SeverityLevelInt()
    {
        CurrentProblem.SeverityLevel = float.NaN;
        return CurrentProblem.SeverityLevelInt;
    }

    /// <summary>
    /// Parses a string grid and synchronizes the values with the current problem.
    /// </summary>
    /// <param name="grid">A 2D array of strings representing Sudoku cell values. Each element can be null, empty, or contain a numeric value.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> object containing validation status and any errors encountered during parsing.
    /// If validation fails, the problem state is restored to its previous state.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="grid"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="grid"/> dimensions do not match <see cref="WinFormsSettings.SudokuSize"/>.</exception>
    /// <remarks>
    /// This method iterates through each cell in the grid, trims whitespace, and attempts to parse the value as a byte.
    /// Values outside the valid range or that fail parsing are recorded as validation errors.
    /// The method creates a backup before processing and restores the problem state if any validation errors occur.
    /// </remarks>
    public ValidationResult ParseAndSync(string[,] grid)
    {
        if(grid == null) throw new ArgumentNullException(nameof(grid));
        if(grid.GetLength(0) != SudokuGrid.SudokuSize || grid.GetLength(1) != SudokuGrid.SudokuSize)
            throw new ArgumentException("grid must be SudokuSize x SudokuSize", nameof(grid));

        ValidationResult result = new ValidationResult();

        BackupProblem();

        for(int row = 0; row < SudokuGrid.SudokuSize; row++)
        {
            for(int col = 0; col < SudokuGrid.SudokuSize; col++)
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
                    result.AddError(new ValidationResult.Error
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
                    result.AddError(new ValidationResult.Error
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

    /// <summary>
    /// Parses a string grid and synchronizes the values with the current problem.
    /// </summary>
    /// <param name="grid">A 2D array of strings representing Sudoku cell values. Each element can be null, empty, or contain a numeric value.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> object containing validation status and any errors encountered during parsing.
    /// If validation fails, the problem state is restored to its previous state.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="grid"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="grid"/> dimensions do not match <see cref="WinFormsSettings.SudokuSize"/>.</exception>
    /// <remarks>
    /// This method iterates through each cell in the grid, trims whitespace, and attempts to parse the value as a byte.
    /// Values outside the valid range or that fail parsing are recorded as validation errors.
    /// The method creates a backup before processing and restores the problem state if any validation errors occur.
    /// </remarks>
    public void CreateProblemFromFile(String filename, Boolean normalSudoku, Boolean xSudoku, Boolean loadCandidates)
    {
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.ReadProblem += (b) =>
        {
            CreateNewProblem(b);
            fileService.Sudoku = CurrentProblem;
        };
        try
        {
            fileService.LoadProblem(filename, normalSudoku, xSudoku, loadCandidates);
            NotifyMatrixChanged();
        }
        catch (ArgumentException)
        {
            ui?.ShowError(String.Format(Thread.CurrentThread.CurrentUICulture, Resources.InvalidSudokuFile, filename));
        }
        catch (InvalidDataException)
        {
            ui?.ShowError(Resources.InvalidSudokuIdentifier);
        }
        catch (System.Text.Json.JsonException ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
        }
        catch (IOException ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
        }
        catch (Exception ex)
        {
            ui?.ShowError(Resources.OpenFailed + Environment.NewLine + ex.Message);
        }
    }
    /// <summary>
    /// Returns whether a specific cell is read-only.
    /// </summary>
    public bool IsCellReadOnly(int row, int col)
    {
        return CurrentProblem.IsCellReadOnly(row, col);
    }

    /// <summary>
    /// Sets the read-only flag for a specific cell.
    /// </summary>
    public void SetCellReadOnly(int row, int col, bool readOnly)
    {
        CurrentProblem.SetReadOnly(row, col, readOnly);
    }

    /// <summary>
    /// Gets the number of filled cells in the current problem.
    /// </summary>
    public int GetFilledCellCount { get { return CurrentProblem.nValues; } }

    /// <summary>
    /// Gets the number of computed (solver-derived) cells in the current problem.
    /// </summary>
    public int GetComputedCellCount { get { return CurrentProblem.nComputedValues; } }

    /// <summary>
    /// Gets the number of variable (non-fixed) cells in the current problem.
    /// </summary>
    public int GetVariableCellCount { get { return CurrentProblem.nVariableValues; } }

    /// <summary>
    /// Returns the neighboring cells for a given cell coordinate.
    /// </summary>
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
    /// <summary>
    /// Replaces the current problem with a cloned instance of the provided problem.
    /// </summary>
    public void UpdateProblem(BaseProblem problem)
    {
        CurrentProblem = problem.Clone();
    }

    /// <summary>
    /// Restores the current problem from the backup if it differs or has unsaved changes.
    /// </summary>
    public void RestoreProblem()
    {
        if(CurrentProblem.Id != Backup.Id || CurrentProblem.Dirty)
            CurrentProblem = Backup.Clone();
    }

    /// <summary>
    /// Creates a backup clone of the current problem.
    /// </summary>
    public void BackupProblem()
    {
        Backup = CurrentProblem.Clone();
    }

    /// <summary>
    /// Returns true when the current problem is resolvable by the solver logic.
    /// </summary>
    public Boolean IsProblemResolvable()
    {
        return CurrentProblem.Resolvable();
    }
    /// <summary>
    /// Pushes an undo entry representing a change to the problem.
    /// </summary>
    public void PushUndo(CoreValue value)
    {
        undoStack.Push(value);
    }

    /// <summary>
    /// Pops the most recent undo entry, or returns null when no undo entries are available.
    /// </summary>
    public CoreValue? PopUndo()
    {
        if(undoStack.Count > 0)
            return undoStack.Pop();
        return null;
    }

    /// <summary>
    /// Clears the undo stack and marks the current problem as not dirty.
    /// </summary>
    public void ClearUndo()
    {
        undoStack.Clear();
        CurrentProblem.Dirty = false;
    }

    /// <summary>
    /// Indicates whether an undo operation is available.
    /// </summary>
    public Boolean CanUndo()
    {
        return undoStack.Count > 0;
    }
    /// <summary>
    /// Saves the current problem to the specified filename. Stops the solving timer before saving.
    /// </summary>
    /// <param name="filename">Path to the file to save.</param>
    /// <returns>True when the file was saved successfully.</returns>
    public Boolean SaveProblem(String filename)
    {
        StopTimer();
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        return fileService.SaveToFile(filename);
    }

    /// <summary>
    /// Exports the current problem as an HTML file.
    /// </summary>
    public void ExportHTML(String filename)
    {
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.SaveToHTMLFile(filename);
    }

    /// <summary>
    /// Builds a human-readable information string for a cell, including definite, fixed values and blocked candidates.
    /// </summary>
    public string GetCellInfoText(int row, int col)
    {
        CultureInfo cultureInfo = Thread.CurrentThread.CurrentUICulture;
        BaseCell cell = CurrentProblem.Cell(row, col);

        String cellInfo = String.Format(cultureInfo, Resources.Cellinfo, row + 1, col + 1, (cell.ReadOnly ? " (" + Resources.ReadOnly + ") " : "")) + Environment.NewLine;
        if(cell.DefinitiveValue != Values.Undefined)
            cellInfo += Environment.NewLine + String.Format(cultureInfo, Resources.DefiniteValue) + cell.DefinitiveValue.ToString();
        else
            if(cell.FixedValue)
                cellInfo += Environment.NewLine + String.Format(cultureInfo, Resources.CellValue) + cell.CellValue.ToString() + " (" + Resources.Remaining + ": " + (SudokuGrid.SudokuSize - CurrentProblem.Matrix.nCells(cell.CellValue)).ToString() + ", " + Resources.Used + ": " + CurrentProblem.Matrix.nCells(cell.CellValue).ToString() + ")";

        String directBlockedCells = "";
        String indirectBlockedCells = "";

        for(int i = 1; i <= SudokuGrid.SudokuSize; i++)
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

    /// <summary>
    /// Ensures the directory for booklet output exists and is created if necessary.
    /// </summary>
    public void CreateBookletDirectory()
    {
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.CreateBookletDirectory(generationParameters);
    }

    /// <summary>
    /// Serializes the current problem into a string representation.
    /// </summary>
    /// <param name="includeROFlag">When true include read-only flags in the serialized output.</param>
    /// <returns>Serialized representation of the current problem.</returns>    
    public String SerializeProblem(Boolean includeROFlag)
    {
        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        return fileService.Serialize(includeROFlag);
    }

    /// <summary>
    /// Returns a human-readable status string describing current generation progress and elapsed time.
    /// </summary>
    /// <param name="usePrecalculatedProblem">When true, indicates we are retrieving a precomputed problem.</param>
    /// <param name="elapsed">Elapsed time to display in the status message.</param>
    /// <returns>Status string for display or logging.</returns>
    public String GenerationStatus(Boolean usePrecalculatedProblem, TimeSpan elapsed)
    {
        return (usePrecalculatedProblem ? String.Format(Thread.CurrentThread.CurrentCulture, Resources.RetrieveProblem) :
                (generationParameters.GenerateBooklet ? String.Format(Thread.CurrentThread.CurrentCulture, Resources.GeneratedProblems, generationParameters.CurrentProblem, settings.BookletSizeNew) + Environment.NewLine : String.Empty) +
                String.Format(Thread.CurrentThread.CurrentCulture, Resources.GeneratingStatus, generationParameters.CheckedProblems) + Environment.NewLine + String.Format(Thread.CurrentThread.CurrentCulture, Resources.CheckingStatus, generationParameters.TotalPasses + CurrentProblem.TotalPassCounter) +
                Environment.NewLine +
                Resources.PreAllocatedValues + generationParameters.PreAllocatedValues.ToString(Thread.CurrentThread.CurrentCulture)) +
                Environment.NewLine + Resources.TimeNeeded + String.Format("{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
    }

    /// <summary>
    /// Builds a message to describe an aborted generation operation and resets generation parameters.
    /// </summary>
    /// <returns>A localized message describing the abort and generation statistics.</returns>
    public String GenerationAborted()
    {
        String result = String.Format(Thread.CurrentThread.CurrentCulture, Resources.GenerationAborted.Replace("\\n", Environment.NewLine),
            generationParameters.GenerateBooklet ? String.Format(Thread.CurrentThread.CurrentCulture, Resources.GeneratedProblems.Replace("\\n", Environment.NewLine), generationParameters.CurrentProblem, settings.BookletSizeNew) + Environment.NewLine : String.Empty,
            generationParameters.CheckedProblems, generationParameters.TotalPasses);
        generationParameters = new GenerationParameters(settings);

        return result;
    }

    /// <summary>
    /// Determines the severity level for a generation request. If multiple problems will be generated, the controller
    /// switches into booklet mode and uses settings; otherwise prompts the UI for a severity selection.
    /// </summary>
    /// <param name="nProblems">The number of problems the caller intends to generate.</param>
    /// <returns>Computed severity level mask for generation.</returns>
    public int GetSeverityLevel(int nProblems)
    {
        if(!(generationParameters.GenerateBooklet = (nProblems != 1)))
            return ui.GetSeverity();
        else
            return settings.SeverityLevel;
    }

    /// <summary>
    /// Gets the last print operation result code from the internal printer service.
    /// </summary>
    public int PrintResult { get { return printerService.PrintResult; } }

    /// <summary>
    /// Provides a detailed error message from the printer service when printing fails.
    /// </summary>
    public String PrintErrorMessage { get { return printerService.PrintErrorMessage; } }

    /// <summary>
    /// Prints the currently collected booklet of problems. Shows UI messages when no problems exist or when printing fails.
    /// </summary>
    public void PrintBooklet()
    {
        printerService.ShowCandidates = false;
        if(NumberOfProblems < 1)
            ui.ShowInfo(Resources.NoProblems);
        else
        {
            try
            {
                printerService.SortProblems();
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

    /// <summary>
    /// Re-initializes the internal printer service instance (dispose + new instance).
    /// </summary>
    public void InitializePrinterService()
    {
        printerService?.Dispose();
        printerService = printServiceFactory.Create();
    }

    /// <summary>
    /// Prints a single problem. The caller can request candidate display.
    /// </summary>
    /// <param name="showCandidates">If true, include candidate digits in the printout.</param>
    public void PrintSingleProblem(Boolean showCandidates)
    {
        IPrintService printerService = printServiceFactory.Create();
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

    /// <summary>
    /// Resets any cached solver solutions for the current problem.
    /// </summary>
    public void ResetSolutions()
    {
        CurrentProblem.ResetSolutions();
    }

    /// <summary>
    /// Saves the application state (including current problem and RO flags) into settings.
    /// </summary>
    public void SaveApplicationState()
    {
        if(IsTimerRunning)
        {
            StopTimer();
        }
        settings.State = SerializeProblem(true);
        settings.Save();
    }

    /// <summary>
    /// Attempts to deserialize the controller state from settings. If deserialization fails the controller restores a default problem state.
    /// </summary>
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

    /// <summary>
    /// Populates <paramref name="filenames"/> with problem filenames found under the provided directory.
    /// </summary>
    /// <param name="directoryInfo">Directory to scan for puzzle files.</param>
    /// <param name="filenames">List to receive discovered filenames.</param>
    /// <param name="token">Cancellation token to abort scanning.</param>
    public void LoadProblemFilenames(DirectoryInfo directoryInfo, List<String> filenames, CancellationToken token)
    {

        SudokuFileService fileService = new SudokuFileService(CurrentProblem, settings, ui);
        fileService.LoadProblemFilenames(directoryInfo, filenames, token);
    }

    /// <summary>
    /// Loads multiple problems from the provided list of filenames and adds suitable puzzles to the booklet.
    /// </summary>
    /// <param name="filenames">List of candidate filenames to load.</param>
    /// <param name="progress">Optional callback invoked to indicate progress.</param>
    /// <param name="token">Cancellation token to cancel loading early.</param>
    /// <returns>The number of problems successfully added to the internal booklet.</returns>
    public async Task<int> LoadProblems(List<string> filenames, Action<object> progress, CancellationToken token)
    {
        Boolean ready = false;
        Random rand = new Random();

        BaseProblem tmp = CurrentProblem.Clone();

        while(!ready)
        {
            int problemNumber = rand.Next(0, filenames.Count - 1);
            try
            {
                SudokuController bookletController = new SudokuController(filenames[problemNumber], false, settings, ui, printServiceFactory);
                if(bookletController.CurrentProblem != null && (bookletController.CurrentProblem.SeverityLevelInt & settings.SeverityLevel) != 0)
                {
                    await bookletController.CurrentProblem.FindSolutions(2, token);

                    if(bookletController.CurrentProblem.SolverTask != null && !bookletController.CurrentProblem.SolverTask.IsCompleted)
                        await bookletController.CurrentProblem.SolverTask.WaitAsync(token);

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

    /// <summary>
    /// Number of problems currently collected in the internal printer/booklet service.
    /// </summary>
    public int NumberOfProblems => printerService.NumberOfProblems;
}