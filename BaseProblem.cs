using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Sudoku;

internal abstract class BaseProblem: EventArgs, IComparable
{
    protected ISudokuSettings settings;

    private Guid id = Guid.NewGuid();
    private Int64 totalPassCount = 0;
    private Int64 passCount = 0;
    private int nVarValues = 0;
    private Boolean findAll = false;
    protected BaseMatrix matrix;
    private List<Solution> solutions;
    private Boolean checkWellDefined = false;
    private Boolean problemSolved = false;

    private Task solverTask = null;

    private float severityLevel = float.NaN;
    private String filename = String.Empty;
    private String comment = String.Empty;
    private Boolean dirty = false;
    private Boolean preparing = false;
    private TimeSpan solvingTime;
    private TimeSpan generationTime;
    private BaseProblem minimalProblem;
    private readonly IncrementalSolver incrementalSolver;

    public static Char ProblemIdentifier = ' ';
    public virtual Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }

    public Action<Object, BaseProblem> Minimizing;
    protected virtual void OnMinimizing(Object o, BaseProblem p)
    {
        Action<Object, BaseProblem> handler = Minimizing;
        if(handler != null) handler(o, p);
    }

    public Action<Object, BaseCell> TestCell;
    protected virtual void OnTestCell(Object o, BaseCell c)
    {
        Action<Object, BaseCell> handler = TestCell;
        if(handler != null) handler(o, c);
    }

    public Action<Object, BaseCell> ResetCell;
    protected virtual void OnResetCell(Object o, BaseCell c)
    {
        Action<Object, BaseCell> handler = ResetCell;
        if(handler != null) handler(o, c);
    }
    public event EventHandler SolutionFound;
    private void NotifySolutionFound()
    {
        SolutionFound?.Invoke(this, EventArgs.Empty);
    }
    public BaseProblem(ISudokuSettings settings)
    {
        createMatrix();
        solutions = new List<Solution>();
        solverTask = null;
        solvingTime = TimeSpan.Zero;
        generationTime = TimeSpan.Zero;
        this.settings = settings;
        incrementalSolver = new IncrementalSolver();
    }

    protected abstract void createMatrix();
    protected abstract BaseProblem CreateInstance();
    public virtual Boolean IsTricky { get { return false; } }

    public BaseMatrix Matrix { get { return matrix; } }
    public List<Solution> Solutions { get { return solutions; } }

    public int nValues { get { return Matrix.nValues; } }
    public int nVariableValues { get { return Matrix.nVariableValues; } }
    public int nComputedValues { get { return Matrix.nComputedValues; } }

    public int MinimumValues { get { return Matrix.MinimumValues; } }

    public Guid Id { get { return id; } set { id = value; } }
    public bool IsCellReadOnly(int row, int col)
    {
        return Cell(row, col).ReadOnly;
    }

    public void SetReadOnly(int row, int col, Boolean readOnly)
    {
        Cell(row, col).ReadOnly = readOnly;
    }
    public Int64 TotalPassCounter
    {
        get { return totalPassCount; }
        set { totalPassCount = value; }
    }
    public int NumberOfSolutions { get { return Solutions.Count; } }
    public Task SolverTask
    {
        get { return solverTask; }
    }

    public Boolean ProblemSolved
    {
        get { return problemSolved; }
    }

    public float SeverityLevel
    {
        get
        {
            severityLevel = Matrix.SeverityLevel;
            return severityLevel;
        }
        set { severityLevel = value; }
    }

    public String SeverityLevelText
    {
        get { return float.IsNaN(SeverityLevel) ? "-" : (SeverityLevel > settings.Hard ? Resources.Hard : (SeverityLevel > settings.Intermediate ? Resources.Intermediate : (SeverityLevel > settings.Trivial ? Resources.Easy : Resources.Trivial))); }
    }

    public int SeverityLevelInt
    {
        get { return float.IsNaN(SeverityLevel) ? 0 : (SeverityLevel > settings.Hard ? 8 : (SeverityLevel > settings.Intermediate ? 4 : (SeverityLevel > settings.Trivial ? 2 : 1))); }
    }

    public String Filename { get { return filename; } set { filename = value; } }
    public String Comment { get { return comment; } set { Dirty=Dirty || comment != value; comment = value; } }
    public Boolean Dirty { get { return dirty; } set { dirty = value; } }
    public Boolean Preparing { get { return preparing; } set { preparing = value; } }
    public TimeSpan SolvingTime { get { return solvingTime; } set { solvingTime = value; } }
    public TimeSpan GenerationTime { get { return generationTime; } set { generationTime = value; } }

    private Task<int> CountSolutionsIncrementalAsync(int maxSolutions, CancellationToken token)
    {
        return Task.Run(() => incrementalSolver.CountSolutions(this, maxSolutions, token), token);
    }
    public int CompareTo(System.Object obj)
    {
        if(obj == null) return -1;
        BaseProblem tmpProblem;
        if(!((tmpProblem = (BaseProblem)obj) is BaseProblem)) throw new ArgumentException(obj.ToString());
        return SeverityLevel.CompareTo(tmpProblem.SeverityLevel);
    }

    public void ResetSolutions()
    {
        solutions = new List<Solution>();
    }

    public BaseProblem Clone()
    {
        BaseProblem dest = CreateInstance();
        dest.matrix = CloneMatrix();

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

    public BaseMatrix CloneMatrix()
    {
        return (BaseMatrix)Matrix.Clone();
    }

    public Solution CopyTo(ref Solution dest)
    {
        dest = new Solution(settings);
        dest.Init();
        dest.Counter = passCount;

        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                dest.SetValue(row, col, Matrix.GetValue(row, col), true);

        return dest;
    }

    public List<BaseCell> GetObviousCells()
    {
        return Matrix.GetObviousCells(true);
    }

    public List<BaseCell> GetHints()
    {
        return Matrix.GetHints(false);
    }

    public List<BaseCell> GetDeepHints()
    {
        return Matrix.GetHints(true);
    }

    private void SaveResult()
    {
        if(NumberOfSolutions < settings.MaxSolutions)
        {
            Solution solution = null;
            Solutions.Add((Solution)CopyTo(ref solution));
            NotifySolutionFound();
        }
        else
        {
            throw new MaxResultsReached();
        }
        passCount = 0;
    }

    public void PrepareMatrix()
    {
        Matrix.Prepare();
    }

    public void ResetMatrix()
    {
        Matrix.Reset();
    }

    public void ResetCandidates()
    {
        Dirty = Dirty || HasCandidates();
        Matrix.ResetCandidates();
    }

    public void ResetCandidates(int row, int col)
    {
        Matrix.ResetCandidates(row, col);
    }

    public Boolean GetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        return Matrix.GetCandidate(row, col, candidate, exclusionCandidate);
    }

    public void SetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        Dirty = Dirty || GetCandidate(row, col, candidate, exclusionCandidate) != exclusionCandidate;   
        Matrix.SetCandidate(row, col, candidate, exclusionCandidate);
    }

    public Boolean HasCandidates()
    {
        return Matrix.HasCandidates();
    }

    public Boolean HasCandidate(int row, int col)
    {
        return Matrix.HasCandidate(row, col);
    }

    public BaseCell[] GetNeighbors(int row, int col)
    {
        return Matrix.Cell(row, col).Neighbors;
    }

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

    public void SetValue(int row, int col, byte value)
    {
        dirty = dirty || (value != GetValue(row, col));
        SetValue(row, col, value, value != Values.Undefined);
    }

    public void SetValue(BaseCell cell, byte value)
    {
        SetValue(cell.Row, cell.Col, value);
    }

    private void ResetValue(int row, int col)
    {
        float sv = severityLevel;
        dirty = dirty || (GetValue(row, col) != Values.Undefined);
        SetValue(row, col, Values.Undefined, false);
        severityLevel = sv;
    }

    private void TryValue(int row, int col, byte value)
    {
        float sv = severityLevel;
        dirty = dirty || (value != GetValue(row, col));
        SetValue(row, col, value, true);
        severityLevel = sv;
    }

    public BaseCell Cell(int row, int col)
    {
        return Matrix.Cell(row, col);
    }

    public byte GetValue(int row, int col)
    {
        return Matrix.GetValue(row, col);
    }

    public Boolean ComputedValue(int row, int col)
    {
        return Matrix.ComputedValue(row, col);
    }

    public Boolean FixedValue(int row, int col)
    {
        return Matrix.FixedValue(row, col);
    }

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

    private async Task RunSolver(int maxSolutions, CancellationToken token)
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
            while(!problemSolved && ++value <= WinFormsSettings.SudokuSize)
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

    private async Task GreedyReduce(int maxSeverity, CancellationToken token)
    {
        int previousValueCount;

        do
        {
            OnMinimizing(this, minimalProblem != null ? minimalProblem : this);
            previousValueCount = nValues;
            bool removedInPass = false;

            var ordered = Matrix.Cells
                .Where(cell => cell.FixedValue)
                .OrderByDescending(cell => cell.FilledNeighborCount)
                .ToList();

            foreach(BaseCell cell in ordered)
            {
                if(token.IsCancellationRequested) return;

                byte original = cell.CellValue;
                OnTestCell(this, cell);
                SetValue(cell, Values.Undefined);
                ResetMatrix();

                bool unique = false;
                if(SeverityLevelInt <= maxSeverity)
                {
                    int solutionCount = await CountSolutionsIncrementalAsync(2, token).ConfigureAwait(false);
                    unique = solutionCount == 1;
                }

                if(unique)
                {
                    ResetMatrix();                          // Brett in Zustand „ohne diese Vorgabe“ zurücksetzen
                    minimalProblem = Clone();
                    OnMinimizing(this, minimalProblem);
                    removedInPass = true;
                    break;
                }

                ResetMatrix();
                SetValue(cell, original);
                OnResetCell(this, cell);
            }

            if(!removedInPass) break;
        }
        while(nValues < previousValueCount && !token.IsCancellationRequested);

        ResetMatrix();
    }
    public async Task<BaseProblem> Minimize(int maxSeverity, CancellationToken token)
    {
        ResetMatrix();

        if(nValues <= Matrix.MinimumValues) return this;

        minimalProblem = Clone();

        BaseProblem greedyCandidate = Clone();
        greedyCandidate.TestCell = TestCell;
        greedyCandidate.ResetCell = ResetCell;
        greedyCandidate.Minimizing = Minimizing;

        await greedyCandidate.GreedyReduce(maxSeverity, token);

        if(greedyCandidate.minimalProblem != null && greedyCandidate.minimalProblem.nValues < minimalProblem.nValues)
        {
            minimalProblem = greedyCandidate.minimalProblem.Clone();
        }

        ResetMatrix();

        if(await MinimizeRecursive(maxSeverity, token))
        {
            minimalProblem.severityLevel = float.NaN;

            await minimalProblem.RunSolver(2, token);

            return minimalProblem.NumberOfSolutions == 1 ? minimalProblem : null;
        }

        return null;
    }
    private async Task<bool> MinimizeRecursive(int maxSeverity, CancellationToken token)
    {
        if(nValues <= Matrix.MinimumValues)
        {
            if(nValues < minimalProblem.nValues)
            {
                minimalProblem = Clone();
                OnMinimizing(this, minimalProblem);
            }
            return true;
        }

        var ordered = Matrix.Cells
            .Where(cell => cell.FixedValue)
            .OrderByDescending(cell => cell.FilledNeighborCount)
            .ToList();

        foreach(BaseCell cell in ordered)
        {
            if(token.IsCancellationRequested) return false;

            OnTestCell(this, cell);
            byte cellValue = cell.CellValue;
            SetValue(cell, Values.Undefined);

            bool unique = false;
            if(SeverityLevelInt <= maxSeverity)
            {
                int solutionCount = await CountSolutionsIncrementalAsync(2, token).ConfigureAwait(false);
                unique = solutionCount == 1;
            }

            if(unique)
            {
                if(nValues < minimalProblem.nValues)
                {
                    minimalProblem = Clone();
                    OnMinimizing(this, minimalProblem);
                }

                if(!await MinimizeRecursive(maxSeverity, token).ConfigureAwait(false)) return false;
            }

            OnResetCell(this, cell);
            SetValue(cell, cellValue);
        }

        return true;
    }
    private async Task<List<BaseCell>> GetCandidates(IReadOnlyList<BaseCell> source, CancellationToken token)
    {
        List<BaseCell> candidates = new List<BaseCell>();

        for(int i = 0; i < source.Count; i++)
        {
            if(token.IsCancellationRequested) return null;

            BaseCell cell = source[i];
            byte cellValue = cell.CellValue;
            if(cellValue == Values.Undefined)
                continue;

            SetValue(cell, Values.Undefined);
            if(cell.DefinitiveValue == cellValue)
            {
                candidates.Add(cell);
            }
            else
            {
                int solutionCount = await CountSolutionsIncrementalAsync(2, token).ConfigureAwait(false);
                if(solutionCount == 1) candidates.Add(cell);
            }
            SetValue(cell, cellValue);
        }

        return candidates;
    }
    public virtual Boolean Resolvable()
    {
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                if(!Check(row, col)) return false;

        for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
            if(!Matrix.Check(Matrix.Rows[i]) || !Matrix.Check(Matrix.Cols[i]) || !Matrix.Check(Matrix.Rectangles[i])) return false;

        return true;
    }

    public int NumDistinctValues()
    {
        int i, j;
        int count = 0;
        Boolean[] exists = new Boolean[WinFormsSettings.SudokuSize + 1];

        for(i = 0; i <= WinFormsSettings.SudokuSize; i++) exists[i] = false;
        for(i = 0; i < WinFormsSettings.SudokuSize; i++)
            for(j = 0; j < WinFormsSettings.SudokuSize; j++)
                exists[GetValue(i, j)] = true;
        for(i = 1; i <= WinFormsSettings.SudokuSize; i++)
            if(exists[i]) count++;

        return count;
    }

    public event EventHandler Progress;
    protected virtual void OnProgress()
    {
        EventHandler handler = Progress;
        if(handler != null) handler(this, EventArgs.Empty);
    }

    private Boolean IsSolved()
    {
        int i, j;
        for(i = 0; i < WinFormsSettings.SudokuSize; i++)
            for(j = 0; j < WinFormsSettings.SudokuSize; j++)
                if(GetValue(i, j) == Values.Undefined || !Check(i, j)) return false;

        return true;
    }

    private Boolean Check(int row, int col)
    {
        return !(Matrix.Cell(row, col).nPossibleValues == 0 && GetValue(row, col) == Values.Undefined && Matrix.Cell(row, col).DefinitiveValue == Values.Undefined);
    }
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
            size = WinFormsSettings.SudokuSize;
            rectSize = WinFormsSettings.RectSize;
            totalCells = WinFormsSettings.TotalCellCount;
            undefinedValue = Values.Undefined;
            grid = new byte[totalCells];
            cellOrder = new int[totalCells];
            rowMask = new int[size];
            colMask = new int[size];
            boxMask = new int[size];
            valueMask = (1 << (size + 1)) - 2;
        }

        public int CountSolutions(BaseProblem problem, int maxSolutions, CancellationToken token)
        {
            this.token = token;
            solutionLimit = Math.Max(1, maxSolutions);
            Prepare(problem);
            if(emptyCount < 0)
                return 0;

            Search(0);
            return solutionCount;
        }

        private void Prepare(BaseProblem problem)
        {
            token.ThrowIfCancellationRequested();
            Array.Clear(rowMask, 0, size);
            Array.Clear(colMask, 0, size);
            Array.Clear(boxMask, 0, size);
            diagMainMask = 0;
            diagAntiMask = 0;
            emptyCount = 0;
            solutionCount = 0;
            enforceDiagonals = problem.Matrix is XSudokuMatrix;

            for(int row = 0; row < size; row++)
            {
                for(int col = 0; col < size; col++)
                {
                    byte value = problem.GetValue(row, col);
                    int index = row * size + col;
                    grid[index] = value;

                    if(value == undefinedValue)
                    {
                        cellOrder[emptyCount++] = index;
                        continue;
                    }

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
        }

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

        private void Swap(int a, int b)
        {
            if(a == b) return;
            int tmp = cellOrder[a];
            cellOrder[a] = cellOrder[b];
            cellOrder[b] = tmp;
        }

        private int GetBoxIndex(int row, int col) => (row / rectSize) * rectSize + (col / rectSize);
    }
}
