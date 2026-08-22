#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Hashing;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Sudoku.Core.Minimizing;

namespace Sudoku.Core;

/// <summary>
/// Base class representing a Sudoku problem instance. Provides matrix management, solver orchestration and basic metadata.
/// </summary>
public abstract class BaseProblem: EventArgs, IComparable
{
    protected ISudokuEngineSettings settings;

    private Guid id = Guid.NewGuid();
    private Int64 totalPassCount = 0;
    private Int64 passCount = 0;
    private int nVarValues = 0;
    private Boolean findAll = false;
    protected BaseMatrix? cellMatrix;
    private List<Solution>? solutions;
    private Boolean checkWellDefined = false;
    private Boolean problemSolved = false;

    private Task? solverTask = null;

    private float severityLevel = float.NaN;
    private String filename = String.Empty;
    private String comment = String.Empty;
    private Boolean dirty = false;
    private Boolean preparing = false;
    private TimeSpan solvingTime;
    private TimeSpan generationTime;
    internal BaseProblem? minimalProblem;
    private readonly IncrementalSolver incrementalSolver = new IncrementalSolver();

    public static Char ProblemIdentifier = ' ';
    public virtual Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }
    public static int Limit = 0;
    public virtual int MinimizeLimit{ get { return Limit; } }

    public Action<Object, BaseProblem?>? Minimizing;
    protected internal virtual void OnMinimizing(Object o, BaseProblem? p)
    {
        // Only notify listeners when we have a concrete minimalProblem instance.
        Action<Object, BaseProblem?>? handler = Minimizing;
        if(handler != null && p != null) handler(o, p);
    }

    public Action<Object, BaseCell>? TestCell;
    protected internal virtual void OnTestCell(Object o, BaseCell c)
    {
        Action<Object, BaseCell>? handler = TestCell;
        if(handler != null) handler(o, c);
    }

    public Action<Object, BaseCell>? ResetCell;
    protected internal virtual void OnResetCell(Object o, BaseCell c)
    {
        Action<Object, BaseCell>? handler = ResetCell;
        if(handler != null) handler(o, c);
    }
    public event EventHandler? SolutionFound;
    private void NotifySolutionFound()
    {
        SolutionFound?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Initializes a new BaseProblem instance using the provided settings.
    /// </summary>
    /// <param name="settings">Application settings controlling generation and solver behavior.</param>
    public BaseProblem(ISudokuEngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        createMatrix();
        solutions = new List<Solution>();
        solverTask = null;
        solvingTime = TimeSpan.Zero;
        generationTime = TimeSpan.Zero;
        this.settings = settings;
        incrementalSolver = new IncrementalSolver();
    }

    /// <summary>
    /// Create and initialize the concrete matrix instance for this problem type.
    /// Concrete subclasses must implement this to construct their specific matrix.
    /// </summary>
    protected abstract void createMatrix();

    /// <summary>
    /// Create an empty instance of the concrete problem type.
    /// Used for materialization and cloning operations.
    /// </summary>
    /// <returns>A new instance of the concrete BaseProblem subclass.</returns>
    protected abstract BaseProblem CreateInstance();

    /// <summary>
    /// Indicates whether this problem is considered tricky according to problem-specific heuristics.
    /// Subclasses may override to provide a different classification.
    /// </summary>
    public virtual Boolean IsTricky { get { return false; } }

    /// <summary>
    /// Gets the associated matrix instance.
    /// </summary>
    public BaseMatrix Matrix { get { return cellMatrix!; } }

    /// <summary>
    /// Gets the currently stored list of found solutions.
    /// </summary>
    public List<Solution> Solutions { get { return solutions!; } }

    /// <summary>
    /// Number of fixed (given) values in the matrix.
    /// Delegates to the matrix implementation.
    /// </summary>
    public int nValues { get { return Matrix.nValues; } }

    /// <summary>
    /// Number of variable (non-fixed) values in the matrix.
    /// Delegates to the matrix implementation.
    /// </summary>
    public int nVariableValues { get { return Matrix.nVariableValues; } }

    /// <summary>
    /// Number of values computed by the solver (not original givens).
    /// Delegates to the matrix implementation.
    /// </summary>
    public int nComputedValues { get { return Matrix.nComputedValues; } }

    /// <summary>
    /// Minimum allowed number of clues for a valid puzzle instance for this matrix type.
    /// Delegates to the matrix implementation (can be overridden by matrix).
    /// </summary>
    public int MinimumValues { get { return Matrix.MinimumValues; } }

    /// <summary>
    /// Unique identifier for this problem instance.
    /// </summary>
    public Guid Id { get { return id; } set { id = value; } }

    /// <summary>
    /// Returns whether the specified cell is marked as read-only.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    /// <returns>True if the cell is read-only; otherwise false.</returns>
    public bool IsCellReadOnly(int row, int col)
    {
        return Cell(row, col).ReadOnly;
    }

    /// <summary>
    /// Mark or unmark a cell as read-only.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    /// <param name="readOnly">Desired read-only state.</param>
    public void SetReadOnly(int row, int col, Boolean readOnly)
    {
        Cell(row, col).ReadOnly = readOnly;
    }
    /// <summary>
    /// Total number of recursive/backtracking passes executed across solver runs.
    /// </summary>
    public Int64 TotalPassCounter
    {
        get { return totalPassCount; }
        set { totalPassCount = value; }
    }
    /// <summary>
    /// Number of solutions currently stored for this problem.
    /// </summary>
    public int NumberOfSolutions { get { return Solutions.Count; } }
    /// <summary>
    /// The asynchronous task running the solver, if any.
    /// </summary>
    public Task? SolverTask
    {
        get { return solverTask; }
    }

    /// <summary>
    /// Indicates whether the problem solving process has finished with at least one solution.
    /// </summary>
    public Boolean ProblemSolved
    {
        get { return problemSolved; }
    }

    /// <summary>
    /// Heuristic severity level cached for the problem.
    /// Getting retrieves a computed value from the matrix, setting overrides the cached value.
    /// </summary>
    public float SeverityLevel
    {
        get
        {
            severityLevel = Matrix.SeverityLevel;
            return severityLevel;
        }
        set { severityLevel = value; }
    }

    public SeverityCategory SeverityLevelCategory
    {
        get
        {
            if(float.IsNaN(SeverityLevel)) return SeverityCategory.Undefined;
            if(SeverityLevel > settings.Hard) return SeverityCategory.Hard;
            if(SeverityLevel > settings.Intermediate) return SeverityCategory.Intermediate;
            if(SeverityLevel > settings.Trivial) return SeverityCategory.Easy;
            return SeverityCategory.Trivial;
        }
    }
    /// <summary>
    /// Numeric severity classification used by algorithms that need integer thresholds.
    /// </summary>
    public int SeverityLevelInt
    {
        get { return float.IsNaN(SeverityLevel) ? 0 : (SeverityLevel > settings.Hard ? 8 : (SeverityLevel > settings.Intermediate ? 4 : (SeverityLevel > settings.Trivial ? 2 : 1))); }
    }

    /// <summary>
    /// Associated filename for the problem (if loaded/saved from disk).
    /// </summary>
    public String Filename { get { return filename; } set { filename = value; } }

    /// <summary>
    /// User-provided textual comment for the problem instance.
    /// Setting updates the Dirty flag if the comment changed.
    /// </summary>
    public String Comment { get { return comment; } set { Dirty = Dirty || comment != value; comment = value; } }

    /// <summary>
    /// Gets or sets a value indicating if the problem has unsaved changes.
    /// </summary>
    public Boolean Dirty { get { return dirty; } set { dirty = value; } }

    /// <summary>
    /// Indicates whether the problem is currently in its preparation phase (matrix preparation / heuristic passes).
    /// </summary>
    public Boolean Preparing { get { return preparing; } set { preparing = value; } }

    /// <summary>
    /// Time spent solving this problem (accumulated).
    /// </summary>
    public TimeSpan SolvingTime { get { return solvingTime; } set { solvingTime = value; } }

    /// <summary>
    /// Time spent generating this problem (accumulated).
    /// </summary>
    public TimeSpan GenerationTime { get { return generationTime; } set { generationTime = value; } }

    /// <summary>
    /// Compare problems by severity level to provide ordering for lists and collections.
    /// </summary>
    public int CompareTo(System.Object? obj)
    {
        if(obj == null) return -1;
        BaseProblem tmpProblem;
        if(!((tmpProblem = (BaseProblem)obj) is BaseProblem)) throw new ArgumentException(obj.ToString());
        return SeverityLevel.CompareTo(tmpProblem.SeverityLevel);
    }

    /// <summary>
    /// Clear the stored solution list.
    /// </summary>
    public void ResetSolutions()
    {
        solutions = new List<Solution>();
    }

    /// <summary>
    /// Create a shallow clone of the problem including a cloned matrix and selected metadata.
    /// The returned instance contains copies of stored solutions up to the configured max.
    /// </summary>
    /// <returns>A cloned BaseProblem instance.</returns>
    public BaseProblem Clone()
    {
        BaseProblem dest = CreateInstance();
        dest.cellMatrix = CloneMatrix();

        dest.ResetSolutions();
        for(int i = 0; i < NumberOfSolutions && i < settings.MaxSolutions; i++)
            dest.Solutions.Add(Solutions[i]);

        dest.severityLevel = SeverityLevel;
        dest.problemSolved = ProblemSolved;
        dest.Filename = Filename;
        dest.Comment = Comment;
        dest.Dirty = Dirty;
        dest.SolvingTime = SolvingTime;
        dest.GenerationTime = GenerationTime;

        return dest;
    }

    /// <summary>
    /// Clone the internal matrix by delegating to the matrix implementation.
    /// </summary>
    /// <returns>A cloned BaseMatrix instance.</returns>
    public BaseMatrix CloneMatrix()
    {
        return (BaseMatrix)Matrix.Clone();
    }

    /// <summary>
    /// Copy the current problem grid into a new Solution object.
    /// </summary>
    /// <param name="dest">Reference to the destination Solution instance which will be created and returned.</param>
    /// <returns>The created Solution instance containing the problem's current grid.</returns>
    public Solution CopyTo(ref Solution? dest)
    {
        dest = new Solution(settings);
        dest.Init();
        dest.Counter = passCount;

        for(int row = 0; row < SudokuGrid.SudokuSize; row++)
            for(int col = 0; col < SudokuGrid.SudokuSize; col++)
                dest.SetValue(row, col, Matrix.GetValue(row, col), true);

        return dest;
    }

    /// <summary>
    /// Return a list of obvious cells (cells with exactly one possible candidate).
    /// Delegates to the matrix implementation.
    /// </summary>
    public List<BaseCell> GetObviousCells()
    {
        return Matrix.GetObviousCells(true);
    }

    /// <summary>
    /// Return a list of hint cells discovered by light heuristics.
    /// Delegates to the matrix implementation.
    /// </summary>
    public List<BaseCell> GetHints()
    {
        return Matrix.GetHints(false);
    }

    /// <summary>
    /// Return a list of hint cells discovered by deeper heuristics (naked/isolated, diagonals if applicable).
    /// Delegates to the matrix implementation.
    /// </summary>
    public List<BaseCell> GetDeepHints()
    {
        return Matrix.GetHints(true);
    }

    /// <summary>
    /// Save the current filled grid as a solution if the solution list has room.
    /// Notifies listeners when a solution is added.
    /// </summary>
    private void SaveResult()
    {
        if(NumberOfSolutions < settings.MaxSolutions)
        {
            Solution? solution = null;
            Solutions.Add(CopyTo(ref solution));
            NotifySolutionFound();
        }
        else
        {
            throw new MaxResultsReached();
        }
        passCount = 0;
    }

    /// <summary>
    /// Prepare the matrix for solving by invoking matrix preparation logic.
    /// </summary>
    public void PrepareMatrix()
    {
        Matrix.Prepare();
    }

    /// <summary>
    /// Reset the matrix to an initial state (clear non-fixed cells and reset block information).
    /// </summary>
    public void ResetMatrix()
    {
        Matrix.Reset();
    }

    /// <summary>
    /// Reset all candidate markings in the matrix and mark the problem Dirty if any candidates were present.
    /// </summary>
    public void ResetCandidates()
    {
        Dirty = Dirty || HasCandidates();
        Matrix.ResetCandidates();
    }

    /// <summary>
    /// Reset candidate markings for a single cell.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    public void ResetCandidates(int row, int col)
    {
        Matrix.ResetCandidates(row, col);
    }

    /// <summary>
    /// Query whether a candidate or exclusion candidate is set for a given cell.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    /// <param name="candidate">Candidate value to test.</param>
    /// <param name="exclusionCandidate">If true test exclusion mask, otherwise the normal candidate mask.</param>
    public Boolean GetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        return Matrix.GetCandidate(row, col, candidate, exclusionCandidate);
    }

    /// <summary>
    /// Toggle a candidate or exclusion-candidate for the specified cell and update Dirty state.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    /// <param name="candidate">Candidate value to toggle.</param>
    /// <param name="exclusionCandidate">If true toggle the exclusion mask, otherwise the normal candidate mask.</param>
    public void SetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        Dirty = Dirty || GetCandidate(row, col, candidate, exclusionCandidate) != exclusionCandidate;
        Matrix.SetCandidate(row, col, candidate, exclusionCandidate);
    }

    /// <summary>
    /// Returns whether any candidate markings are present in the matrix.
    /// </summary>
    public Boolean HasCandidates()
    {
        return Matrix.HasCandidates();
    }

    /// <summary>
    /// Returns whether a specific cell currently has any candidate marks.
    /// </summary>
    public Boolean HasCandidate(int row, int col)
    {
        return Matrix.HasCandidate(row, col);
    }

    /// <summary>
    /// Get all neighbor cells for the specified cell coordinates.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    /// <returns>Array of neighboring BaseCell instances.</returns>
    public BaseCell[] GetNeighbors(int row, int col)
    {
        return Matrix.Cell(row, col).Neighbors;
    }

    /// <summary>
    /// Public wrapper to set a cell value and optionally mark it as fixed.
    /// Updates problem metadata and invalidates cached severity.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    /// <param name="value">Value to set or <see cref="Values.Undefined"/> to clear.</param>
    /// <param name="fix">If true mark as a fixed given.</param>
    public void SetValue(int row, int col, byte value, Boolean fix)
    {
        if(GetValue(row, col) != value || FixedValue(row, col) != fix)
        {
            Matrix.SetValue(row, col, value, fix);
            severityLevel = float.NaN;
            problemSolved = false;
            filename = String.Empty;
        }
    }

    /// <summary>
    /// Convenience overload for setting a value and deriving the fixed flag from the value.
    /// </summary>
    /// <param name="row">Row index (0-based).</param>
    /// <param name="col">Column index (0-based).</param>
    /// <param name="value">Value to set.</param>
    public void SetValue(int row, int col, byte value)
    {
        dirty = dirty || (value != GetValue(row, col));
        SetValue(row, col, value, value != Values.Undefined);
    }

    /// <summary>
    /// Convenience overload to set a value using a BaseCell reference.
    /// </summary>
    public void SetValue(BaseCell cell, byte value)
    {
        SetValue(cell.Row, cell.Col, value);
    }

    /// <summary>
    /// Reset a single cell to undefined while preserving current severity cache.
    /// Marks dirty if the cell held a value.
    /// </summary>
    private void ResetValue(int row, int col)
    {
        float sv = severityLevel;
        dirty = dirty || (GetValue(row, col) != Values.Undefined);
        SetValue(row, col, Values.Undefined, false);
        severityLevel = sv;
    }

    /// <summary>
    /// Try a candidate value on a cell and mark it as fixed for the trial; preserves the severity cache.
    /// </summary>
    private void TryValue(int row, int col, byte value)
    {
        float sv = severityLevel;
        dirty = dirty || (value != GetValue(row, col));
        SetValue(row, col, value, true);
        severityLevel = sv;
    }

    /// <summary>
    /// Returns the BaseCell instance at the specified coordinates.
    /// </summary>
    public BaseCell Cell(int row, int col)
    {
        return Matrix.Cell(row, col);
    }

    /// <summary>
    /// Returns the current stored value for the specified cell.
    /// </summary>
    public byte GetValue(int row, int col)
    {
        return Matrix.GetValue(row, col);
    }

    /// <summary>
    /// Indicates whether the value at the specified cell was computed by the solver.
    /// </summary>
    public Boolean ComputedValue(int row, int col)
    {
        return Matrix.ComputedValue(row, col);
    }

    /// <summary>
    /// Indicates whether the value at the specified cell is a fixed given.
    /// </summary>
    public Boolean FixedValue(int row, int col)
    {
        return Matrix.FixedValue(row, col);
    }

    /// <summary>
    /// Asynchronously begin searching for solutions up to maxSolutions.
    /// If an existing solver task is running, await its completion first.
    /// The actual search is executed in the background and results are stored in <see cref="Solutions"/>
    /// </summary>
    /// <param name="maxSolutions">Maximum number of solutions to find (use int.MaxValue to find all).</param>
    /// <param name="token">Cancellation token to cancel the search.</param>
    public async Task FindSolutions(int maxSolutions, CancellationToken token)
    {
        if(solverTask != null && !solverTask.IsCompleted)
        {
            await solverTask; // Wait for the existing solver task to complete before starting a new one
        }
        solverTask?.Dispose();

        if(NumberOfSolutions >= maxSolutions) return;

        solverTask = RunSolver(maxSolutions, token);
    }

    /// <summary>
    /// Prepare and run the solver asynchronously. Sets up internal flags and invokes the synchronous Solve entry point.
    /// </summary>
    internal async Task RunSolver(int maxSolutions, CancellationToken token)
    {
        if(token.IsCancellationRequested) return;

        preparing = true;
        findAll = (maxSolutions == int.MaxValue);
        checkWellDefined = (maxSolutions == 2);
        passCount = 0;
        totalPassCount = 0;
        problemSolved = false;
        solvingTime = TimeSpan.Zero;

        ResetSolutions();
        severityLevel = Matrix.SeverityLevel;

        try
        {
            PrepareMatrix();
        }
        catch(ArgumentException)
        {
            preparing = false;
            return;
        }
        finally
        {
            preparing = false;
        }

        if(Matrix.nVariableValues == 0)
        {
            problemSolved = true;
            SaveResult();
            return;
        }

        if(!Resolvable()) return;

        await Task.Run(() => Solve(token), token);
    }

    /// <summary>
    /// Run the solver on the current thread. Sets culture according to settings and invokes recursive solver.
    /// </summary>
    private void Solve(CancellationToken token)
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(settings.DisplayLanguage);
        try
        {
            nVarValues = Matrix.nVariableValues;
            if(token.IsCancellationRequested) return;

            Solve(0, token);
        }
        catch(Exception)
        {
            ResetMatrix();
            // Cancel();
        }
    }

    /// <summary>
    /// Core recursive/backtracking solver. Selects next cell by matrix heuristics and tries candidate values.
    /// Manages progress notifications and solution collection according to flags like findAll and checkWellDefined.
    /// </summary>
    /// <param name="current">Index in the sorted list of variable cells.</param>
    /// <param name="token">Cancellation token.</param>
    private void Solve(int current, CancellationToken token)
    {
        if(token.IsCancellationRequested) return;

        BaseCell currentValue = Matrix.Get(current);
        byte value = 0;

        passCount++;
        totalPassCount++;

        const int progressInterval = 2000;

        if(passCount % progressInterval == 0)
        {
            OnProgress();
            if(token.IsCancellationRequested) return;
        }

        if(currentValue.nPossibleValues > 0)
        {
            while(!problemSolved && ++value <= SudokuGrid.SudokuSize)
            {
                if(token.IsCancellationRequested) return;

                ResetValue(currentValue.Row, currentValue.Col);
                if(currentValue.Enabled(value))
                {
                    try
                    {
                        TryValue(currentValue.Row, currentValue.Col, value);
                        currentValue.ComputedValue = true;

                        if(current < nVarValues - 1) // Resolvable Check entfernen für Performance in tiefer Rekursion
                        {
                            if(Resolvable()) Solve(current + 1, token);
                        }
                        else
                        {
                            if(problemSolved = IsSolved()) SaveResult();
                            if(findAll || (checkWellDefined && NumberOfSolutions < 2)) problemSolved = false;
                        }
                    }
                    catch(ArgumentException) { }
                }
            }
        }
        else if(currentValue.DefinitiveValue != Values.Undefined)
        {
            if(token.IsCancellationRequested) return;

            TryValue(currentValue.Row, currentValue.Col, currentValue.DefinitiveValue);
            currentValue.ComputedValue = true;

            if(current < nVarValues - 1 && Resolvable())
                Solve(current + 1, token);
            else
            {
                if(problemSolved = IsSolved()) SaveResult();
                if(findAll || (checkWellDefined && NumberOfSolutions < 2)) problemSolved = false;
            }
        }

        if(!problemSolved) ResetValue(currentValue.Row, currentValue.Col);

        if((findAll || checkWellDefined) && current == 0) problemSolved = (NumberOfSolutions > 0);
    }

    /// <summary>
    /// Greedy reduction phase used by the minimizer to remove givens while preserving uniqueness up to severity constraints.
    /// Processes givens by priority and tests uniqueness incrementally using cached checks.
    /// </summary>
    /// <param name="state">Current given state.</param>
    /// <param name="maxSeverity">Maximum allowed severity for candidate uniqueness.</param>
    /// <param name="cache">Cache mapping state signatures to uniqueness boolean.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The reduced GivenState after greedy removals.</returns>
    internal async Task<GivenState> GreedyReduce(GivenState state, int maxSeverity, Dictionary<ulong, bool> cache, CancellationToken token)
    {
        var queue = new PriorityQueue<int, int>();

        foreach(BaseCell cell in Matrix.Cells.Where(c => c.FixedValue))
        {
            int index = cell.Row * SudokuGrid.SudokuSize + cell.Col;
            queue.Enqueue(index, -cell.FilledNeighborCount);
        }

        while(queue.TryDequeue(out int index, out _))
        {
            if(token.IsCancellationRequested) break;
            if(state.values[index] == Values.Undefined) continue;

            byte original = state.values[index];
            state.values[index] = Values.Undefined;

            bool unique = await IsUnique(state, maxSeverity, cache, token).ConfigureAwait(false);

            if(unique)
            {
                state = state with { FixedCount = state.FixedCount - 1 };
                OnMinimizing(this, minimalProblem);
            }
            else
            {
                state.values[index] = original;
            }
        }

        return state;
    }

    /// <summary>
    /// Create a defensive clone of a GivenState.
    /// </summary>
    internal static GivenState CloneState(GivenState state)
    {
        return new GivenState((byte[])state.values.Clone(), state.FixedCount);
    }

    /// <summary>
    /// Use the incremental solver to count the number of solutions for the current matrix up to maxSolutions.
    /// </summary>
    private Task<int> CountSolutionsIncremental(int maxSolutions, CancellationToken token)
    {
        GivenState snapshot = GivenState.FromMatrix(Matrix);
        bool enforceDiagonals = Matrix is XSudokuMatrix;
        return CountSolutionsIncremental(snapshot.values, enforceDiagonals, maxSolutions, token);
    }
    private Task<int> CountSolutionsIncremental(ReadOnlyMemory<byte> givens, bool enforceDiagonals, int maxSolutions, CancellationToken token)
    {
        return Task.Run(() => incrementalSolver.CountSolutions(givens.Span, enforceDiagonals, maxSolutions, token), token);
    }

    /// <summary>
    /// Test whether the state encodes a uniquely solvable puzzle (optionally constrained by severity).
    /// Uses an incremental solver and caches results by state signature.
    /// </summary>
    /// <param name="state">Current GivenState to test.</param>
    /// <param name="maxSeverity">Maximum allowed severity for a unique candidate to be accepted (-1 for unlimited).</param>
    /// <param name="cache">Cache for seen state signatures.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>True when the supplied state has a unique solution respecting severity constraints.</returns>
    internal async Task<bool> IsUnique(GivenState state, int maxSeverity, Dictionary<ulong, bool> cache, CancellationToken token)
    {
        ulong signature = XxHash64.HashToUInt64(state.values);
        if(cache.TryGetValue(signature, out bool unique)) return unique;

        bool enforceDiagonals = Matrix is XSudokuMatrix;
        int count = await CountSolutionsIncremental(state.values, enforceDiagonals, 2, token).ConfigureAwait(false);

        unique = count == 1;

        bool severityLimited = maxSeverity >= 0 && maxSeverity < int.MaxValue;
        if(unique && severityLimited)
        {
            BaseProblem candidate = Materialize(state);
            candidate.severityLevel = float.NaN;
            unique = candidate.SeverityLevelInt <= maxSeverity;
        }

        cache[signature] = unique;
        return unique;
    }
    /// <summary>
    /// Materialize a BaseProblem instance from a GivenState by applying givens to a fresh clone.
    /// </summary>
    /// <param name="state">Source GivenState describing fixed cells.</param>
    /// <returns>A BaseProblem instance representing the provided givens.</returns>
    internal BaseProblem Materialize(GivenState state)
    {
        BaseProblem clone = CreateInstance();
        clone.cellMatrix = cellMatrix!.Clone();
        clone.Matrix.SetPredefinedValues = false;

        for(int r = 0; r < SudokuGrid.SudokuSize; r++)
        {
            for(int c = 0; c < SudokuGrid.SudokuSize; c++)
            {
                byte value = state[r, c];
                bool fixedValue = value != Values.Undefined;

                clone.SetValue(r, c, value, fixedValue);
                clone.SetReadOnly(r, c, fixedValue);   // ← Schreibschutz an/vs. aus
            }
        }

        clone.Matrix.SetPredefinedValues = true;
        clone.Matrix.Prepare();
        return clone;
    }

    public enum MinimizeAlgorithm { Calculate, Candidate, Greedy }
    public record struct AlgorithmParameters(MinimizeAlgorithm FavoriteAlgorithm, int InitialFixedCount, int TotalRemovable, int RemovedByGreedy, int RemainingMargin, int GreedyStateFixedCount, int NumberOfSeldomValues, int NumberOfFrequentValues, float SeverityLevel);

    /// <summary>
    /// Compute metadata used to pick a minimization strategy: counts of removables, frequency distribution and severity.
    /// </summary>
    private AlgorithmParameters GetAlgorithmParameters(GivenState initial, GivenState greedyState)
    {
        AlgorithmParameters parameters = new AlgorithmParameters();

        parameters.InitialFixedCount = initial.FixedCount;
        parameters.TotalRemovable=initial.FixedCount - Matrix.MinimumValues;
        parameters.RemovedByGreedy=initial.FixedCount - greedyState.FixedCount;
        parameters.RemainingMargin= greedyState.FixedCount - Matrix.MinimumValues;
        parameters.GreedyStateFixedCount = greedyState.FixedCount;
        parameters.NumberOfSeldomValues = greedyState.CountValues().Count(x => x < 2);
        parameters.NumberOfFrequentValues = greedyState.CountValues().Count(y => y > 3);
        parameters.SeverityLevel = SeverityLevel;

        return parameters;
    }
    /// <summary>
    /// Heuristic decision whether a candidate-based minimization search is likely beneficial.
    /// Returns parameters used for diagnostics and tuning.
    /// </summary>
    internal bool ShouldUseCandidateSearch(GivenState initial, GivenState greedyState, out AlgorithmParameters parameters)
    {
        const int GreedyOffset = 2; // If greedy is within this many clues of the minimum, skip candidate search
        parameters=GetAlgorithmParameters(initial, greedyState);

        int count=parameters.NumberOfFrequentValues+parameters.NumberOfSeldomValues;

        bool manyRemovalsPossible = parameters.TotalRemovable >= 10;
        bool greedyProgressLow = parameters.RemovedByGreedy < parameters.TotalRemovable * (Matrix is XSudokuMatrix? 0.4: 0.6);
        bool notFarFromMinimum = parameters.RemainingMargin < 5;
        bool lowSeverity = SeverityLevel < 25;
        bool lowNumberOfDefinitiveCells = Matrix.DefinitiveCellCount < parameters.TotalRemovable / 10;
        bool isXSudoku = Matrix is XSudokuMatrix;

        if(isXSudoku) return false; // For XSudoku, greedy reduction is often more effective and candidate search can be less beneficial due to the additional constraints
        if(greedyState.FixedCount <= Matrix.MinimumValues + GreedyOffset || NumDistinctValues() < SudokuGrid.SudokuSize) return false; // Already at or below minimum, no need for candidate search
        return (parameters.RemovedByGreedy > 9 && (parameters.GreedyStateFixedCount > 22 || parameters.NumberOfSeldomValues > 0)); // Heuristic: If greedy removed a significant number of clues and left a relatively high fixed count or there is a low number of seldom values, candidate search is likely beneficial
    }

    /// <summary>
    /// Decide on an algorithmic strategy for minimization by running a cheap greedy pass and analyzing results.
    /// </summary>
    /// <param name="maxSeverity">Maximum allowed severity for candidates.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Parameters describing the recommended algorithm.</returns>
    public async Task<AlgorithmParameters> GetAlgorithm(int maxSeverity, CancellationToken token)
    {
        ResetMatrix();
        AlgorithmParameters parameters;

        GivenState initialState = GivenState.FromMatrix(Matrix);

        var cache = new Dictionary<ulong, bool>();
        GivenState greedyState = await GreedyReduce(CloneState(initialState), maxSeverity, cache, token).ConfigureAwait(false);

        if(ShouldUseCandidateSearch(initialState, greedyState, out parameters))
            parameters.FavoriteAlgorithm=MinimizeAlgorithm.Candidate;
        else
            parameters.FavoriteAlgorithm=MinimizeAlgorithm.Greedy; 

        return parameters;
    }
    private static readonly Dictionary<MinimizeAlgorithm, IMinimizeStrategy> strategies = BuildStrategies();

    private static Dictionary<MinimizeAlgorithm, IMinimizeStrategy> BuildStrategies()
    {
        var candidate = new CandidateMinimizeStrategy();
        var greedy = new GreedyMinimizeStrategy();
        return new()
        {
            [MinimizeAlgorithm.Candidate] = candidate,
            [MinimizeAlgorithm.Greedy] = greedy,
            [MinimizeAlgorithm.Calculate] = new AutoMinimizeStrategy(candidate, greedy)
        };
    }

    public async Task<BaseProblem?> Minimize(int maxSeverity, MinimizeAlgorithm minimizeAlgorithm, CancellationToken token)
    {
        ResetMatrix();
        GivenState initialState = GivenState.FromMatrix(Matrix);
        if(initialState.FixedCount <= Matrix.MinimumValues) return this;

        var cache = new Dictionary<ulong, bool>();
        GivenState greedyState = await GreedyReduce(CloneState(initialState), maxSeverity, cache, token).ConfigureAwait(false);

        return await strategies[minimizeAlgorithm].MinimizeAsync(this, initialState, greedyState, maxSeverity, cache, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Check whether the current problem is potentially solvable by ensuring each unit and each cell has at least one possible placement for every value.
    /// </summary>
    public virtual Boolean Resolvable()
    {
        for(int row = 0; row < SudokuGrid.SudokuSize; row++)
            for(int col = 0; col < SudokuGrid.SudokuSize; col++)
                if(!Check(row, col)) return false;

        for(int i = 0; i < SudokuGrid.SudokuSize; i++)
            if(!Matrix.Check(Matrix.Rows[i]) || !Matrix.Check(Matrix.Cols[i]) || !Matrix.Check(Matrix.Rectangles[i])) return false;

        return true;
    }

    /// <summary>
    /// Count of distinct values present on the grid (ignores undefined).
    /// </summary>
    public int NumDistinctValues()
    {
        int count = 0;
        bool[] exists = new bool[SudokuGrid.SudokuSize + 1];

        for(int row = 0; row < SudokuGrid.SudokuSize; row++)
        {
            for(int col = 0; col < SudokuGrid.SudokuSize; col++)
            {
                byte value = GetValue(row, col);
                if(value == Values.Undefined) continue;

                if(!exists[value])
                {
                    exists[value] = true;
                    if(++count == SudokuGrid.SudokuSize) return count;
                }
            }
        }

        return count;
    }

    public event EventHandler? Progress;
    protected virtual void OnProgress()
    {
        EventHandler? handler = Progress;
        if(handler != null) handler(this, EventArgs.Empty);
    }

    /// <summary>
    /// Validate whether the entire puzzle is solved: no undefined cells and all unit checks pass.
    /// </summary>
    private Boolean IsSolved()
    {
        int i, j;
        for(i = 0; i < SudokuGrid.SudokuSize; i++)
            for(j = 0; j < SudokuGrid.SudokuSize; j++)
                if(GetValue(i, j) == Values.Undefined || !Check(i, j)) return false;

        return true;
    }

    /// <summary>
    /// Internal per-cell consistency check used by Resolvable: ensures at least one candidate or definitive value exists.
    /// </summary>
    private Boolean Check(int row, int col)
    {
        return !(Matrix.Cell(row, col).nPossibleValues == 0 && GetValue(row, col) == Values.Undefined && Matrix.Cell(row, col).DefinitiveValue == Values.Undefined);
    }

    /// <summary>
    /// Immutable struct representing a snapshot of given values and the count of fixed givens.
    /// Provides helpers to create from a matrix and to remove a given.
    /// </summary>
    internal record struct GivenState(byte[] values, int FixedCount)
    {
        public static GivenState FromMatrix(BaseMatrix matrix)
        {
            int size = SudokuGrid.SudokuSize;
            byte[] values = new byte[SudokuGrid.TotalCellCount];
            int fixedCount = 0;

            for(int r = 0; r < size; r++)
            {
                for(int c = 0; c < size; c++)
                {
                    byte cellValue = matrix.GetValue(r, c);
                    values[r * size + c] = cellValue;
                    if(cellValue != Values.Undefined && matrix.Cell(r, c).FixedValue) fixedCount++;
                }
            }

            return new GivenState(values, fixedCount);
        }

        public byte this[int row, int col]
        {
            readonly get => values[row * SudokuGrid.SudokuSize + col];
            set => values[row * SudokuGrid.SudokuSize + col] = value;
        }

        /// <summary>
        /// Create a new GivenState with the specified cell removed (set to undefined).
        /// </summary>
        /// <param name="row">Row index of the removed cell.</param>
        /// <param name="col">Column index of the removed cell.</param>
        /// <returns>A new GivenState with the cell removed and FixedCount adjusted.</returns>
        public readonly GivenState WithRemoved(int row, int col)
        {
            var clone = (byte[])values.Clone();
            int index = row * SudokuGrid.SudokuSize + col;
            if(clone[index] == Values.Undefined) return new GivenState(clone, FixedCount);

            clone[index] = Values.Undefined;
            return new GivenState(clone, FixedCount - 1);
        }
        /// <summary>
        /// Count occurrences of each digit in the given state.
        /// </summary>
        public readonly int[] CountValues()
        {
            int size = SudokuGrid.SudokuSize;
            int[] counts = new int[size];

            for(int index = 0; index < values.Length; index++)
            {
                byte value = values[index];
                if(value == Values.Undefined) continue;

                counts[value-1]++;
            }

            return counts;
        }
    }
    /// <summary>
    /// Lightweight incremental solver optimized for quick uniqueness checks.
    /// It operates on a compact grid representation and uses masks for fast candidate operations.
    /// </summary>
    private sealed class IncrementalSolver
    {
        private readonly int size;
        private readonly int rectSize;
        private readonly int totalCells;
        private readonly byte undefinedValue;
        private readonly byte[] grid;
        private readonly int[] cellOrder;
        private readonly int[] rowMask;
        private readonly int[] colMask;
        private readonly int[] boxMask;
        private readonly int valueMask;

        private int diagMainMask;
        private int diagAntiMask;
        private int emptyCount;
        private int solutionCount;
        private int solutionLimit;
        private bool enforceDiagonals;
        private CancellationToken token;

        public IncrementalSolver()
        {
            size = SudokuGrid.SudokuSize;
            rectSize = SudokuGrid.RectSize;
            totalCells = SudokuGrid.TotalCellCount;
            undefinedValue = Values.Undefined;
            grid = new byte[totalCells];
            cellOrder = new int[totalCells];
            rowMask = new int[size];
            colMask = new int[size];
            boxMask = new int[size];
            valueMask = (1 << (size + 1)) - 2;
        }
        /// <summary>
        /// Count solutions for the provided givens using a depth-first masked search up to maxSolutions.
        /// </summary>
        /// <param name="givens">Span of given values in row-major order.</param>
        /// <param name="enforceDiagonals">Whether to treat diagonals as additional constraints (X-Sudoku).</param>
        /// <param name="maxSolutions">Maximum number of solutions to search for.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of solutions found up to the specified limit.</returns>
        public int CountSolutions(ReadOnlySpan<byte> givens, bool enforceDiagonals, int maxSolutions, CancellationToken token)
        {
            this.token = token;
            this.enforceDiagonals = enforceDiagonals;
            solutionLimit = Math.Max(1, maxSolutions);
            Prepare(givens);
            if(emptyCount < 0) return 0;

            Search(0);
            return solutionCount;
        }
        /// <summary>
        /// Prepare internal masks and ordering based on the givens; detect immediate contradictions.
        /// </summary>
        private void Prepare(ReadOnlySpan<byte> givens)
        {
            token.ThrowIfCancellationRequested();
            Array.Clear(rowMask, 0, size);
            Array.Clear(colMask, 0, size);
            Array.Clear(boxMask, 0, size);
            diagMainMask = 0;
            diagAntiMask = 0;
            emptyCount = 0;
            solutionCount = 0;

            for(int index = 0; index < totalCells; index++)
            {
                byte value = givens[index];
                grid[index] = value;

                if(value == undefinedValue)
                {
                    cellOrder[emptyCount++] = index;
                    continue;
                }

                int row = index / size;
                int col = index % size;
                int bit = 1 << value;
                int box = GetBoxIndex(row, col);

                if(((rowMask[row] | colMask[col] | boxMask[box]) & bit) != 0 ||
                   (enforceDiagonals && row == col && (diagMainMask & bit) != 0) ||
                   (enforceDiagonals && row + col == size - 1 && (diagAntiMask & bit) != 0))
                {
                    emptyCount = -1;
                    return;
                }

                rowMask[row] |= bit;
                colMask[col] |= bit;
                boxMask[box] |= bit;

                if(enforceDiagonals)
                {
                    if(row == col) diagMainMask |= bit;
                    if(row + col == size - 1) diagAntiMask |= bit;
                }
            }
        }
        /// <summary>
        /// Depth-first masked search for filling empty cells using minimum-candidate heuristics.
        /// Stops once solutionLimit is reached.
        /// </summary>
        private void Search(int depth)
        {
            if(solutionCount >= solutionLimit) return;
            token.ThrowIfCancellationRequested();

            if(depth == emptyCount)
            {
                solutionCount++;
                return;
            }

            int candidateMask;
            int selectedIndex = SelectCell(depth, out candidateMask);
            if(selectedIndex < 0 || candidateMask == 0) return;

            Swap(depth, selectedIndex);
            int cellIndex = cellOrder[depth];
            int row = cellIndex / size;
            int col = cellIndex % size;
            int box = GetBoxIndex(row, col);

            while(candidateMask != 0 && solutionCount < solutionLimit)
            {
                int bit = candidateMask & -candidateMask;
                candidateMask ^= bit;
                byte value = (byte)BitOperations.TrailingZeroCount((uint)bit);

                PlaceValue(cellIndex, row, col, box, bit);
                Search(depth + 1);
                RemoveValue(cellIndex, row, col, box, bit);
            }
        }

        /// <summary>
        /// Choose the next empty cell using a minimum-candidates heuristic; returns mask of candidates.
        /// </summary>
        private int SelectCell(int start, out int candidateMask)
        {
            int bestIndex = -1;
            int bestMask = 0;
            int bestCount = int.MaxValue;

            for(int i = start; i < emptyCount; i++)
            {
                int mask = GetCandidateMask(cellOrder[i]);
                if(mask == 0)
                {
                    candidateMask = 0;
                    return i;
                }

                int count = BitOperations.PopCount((uint)mask);
                if(count < bestCount)
                {
                    bestCount = count;
                    bestMask = mask;
                    bestIndex = i;
                    if(bestCount == 1) break;
                }
            }

            candidateMask = bestMask;
            return bestIndex;
        }

        /// <summary>
        /// Compute a candidate bitmask for the specified cell index by combining row/col/box (and diagonal) masks.
        /// </summary>
        private int GetCandidateMask(int cellIndex)
        {
            int row = cellIndex / size;
            int col = cellIndex % size;
            int box = GetBoxIndex(row, col);
            int used = rowMask[row] | colMask[col] | boxMask[box];

            if(enforceDiagonals)
            {
                if(row == col) used |= diagMainMask;
                if(row + col == size - 1) used |= diagAntiMask;
            }

            return valueMask & ~used;
        }

        /// <summary>
        /// Place a value into the internal grid and update masks.
        /// </summary>
        private void PlaceValue(int cellIndex, int row, int col, int box, int bit)
        {
            grid[cellIndex] = (byte)BitOperations.TrailingZeroCount((uint)bit);
            rowMask[row] |= bit;
            colMask[col] |= bit;
            boxMask[box] |= bit;

            if(enforceDiagonals)
            {
                if(row == col) diagMainMask |= bit;
                if(row + col == size - 1) diagAntiMask |= bit;
            }
        }

        /// <summary>
        /// Remove a previously placed value and restore masks.
        /// </summary>
        private void RemoveValue(int cellIndex, int row, int col, int box, int bit)
        {
            grid[cellIndex] = undefinedValue;
            rowMask[row] &= ~bit;
            colMask[col] &= ~bit;
            boxMask[box] &= ~bit;

            if(enforceDiagonals)
            {
                if(row == col) diagMainMask &= ~bit;
                if(row + col == size - 1) diagAntiMask &= ~bit;
            }
        }

        /// <summary>
        /// Swap two entries in the cellOrder used by the search.
        /// </summary>
        private void Swap(int a, int b)
        {
            if(a == b) return;
            int tmp = cellOrder[a];
            cellOrder[a] = cellOrder[b];
            cellOrder[b] = tmp;
        }

        /// <summary>
        /// Get the box index for a given row and column.
        /// </summary>
        private int GetBoxIndex(int row, int col) => (row / rectSize) * rectSize + (col / rectSize);
    }
}