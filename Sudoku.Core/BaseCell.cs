#nullable enable
using System;
using System.Collections.Generic;

namespace Sudoku.Core;

/// <summary>
/// Abstract base class representing a single cell in a Sudoku grid.
/// Manages the cell value, candidates, and relationships with neighboring cells.
/// Provides methods for constraint checking and candidate elimination.
/// </summary>
[Serializable]
public abstract class BaseCell: EventArgs, IComparable
{
	private CoreValue coreValue = new CoreValue();
	private byte definitiveValue = Values.Undefined;
	private int nNeighbors = 0;
    private int[]? directBlocks;
    private int[]? indirectBlocks;
	private int candidatesMask = 0;
	private int exclusionCandidatesMask = 0;
	private int enabledMask = 0;
	private bool enabledMaskInitialized = false;
	private int possibleValuesCount = 0;
	private bool fixedValue = false;
	private bool computedValue = false;
	private bool readOnly = false;
    protected BaseCell[]? neighborCells;
	private int startCol = 0;
	private int startRow = 0;

	// cached helpers
    private static byte[]? popcountCache;
    private static int[]? lowbitIndex;

	/// <summary>
	/// Temporary scratch space for naked subset solving techniques.
	/// </summary>
	public struct NakedScratch
	{
		public int[]? NeighborMasks;
		public byte[]? NeighborCounts;
		public BaseCell[]? CandidateArr;
		public int[]? CommonStamp;

		public void Ensure(int neighborLen)
		{
			var intPool = System.Buffers.ArrayPool<int>.Shared;
			var bytePool = System.Buffers.ArrayPool<byte>.Shared;
			var cellPool = System.Buffers.ArrayPool<BaseCell>.Shared;

			if(NeighborMasks == null || NeighborMasks.Length < neighborLen)
				NeighborMasks = intPool.Rent(neighborLen);
			if(NeighborCounts == null || NeighborCounts.Length < neighborLen)
				NeighborCounts = bytePool.Rent(neighborLen);
			if(CandidateArr == null || CandidateArr.Length < SudokuGrid.SudokuSize)
				CandidateArr = cellPool.Rent(SudokuGrid.SudokuSize);
			if(CommonStamp == null || CommonStamp.Length < SudokuGrid.TotalCellCount)
				CommonStamp = intPool.Rent(SudokuGrid.TotalCellCount);
		}

		public void Release()
		{
			var intPool = System.Buffers.ArrayPool<int>.Shared;
			var bytePool = System.Buffers.ArrayPool<byte>.Shared;
			var cellPool = System.Buffers.ArrayPool<BaseCell>.Shared;

			if(NeighborMasks != null) { intPool.Return(NeighborMasks, true); NeighborMasks = null; }
			if(NeighborCounts != null) { bytePool.Return(NeighborCounts, true); NeighborCounts = null; }
			if(CommonStamp != null) { intPool.Return(CommonStamp, true); CommonStamp = null; }
			if(CandidateArr != null) { Array.Clear(CandidateArr, 0, CandidateArr.Length); cellPool.Return(CandidateArr, true); CandidateArr = null; }
		}
	}

	/// <summary>
	/// Determines if this cell supports upward movement (used in X-Sudoku).
	/// </summary>
	/// <returns>True if this cell is on a special diagonal; false otherwise.</returns>
	public abstract bool Up();

	/// <summary>
	/// Determines if this cell supports downward movement (used in X-Sudoku).
	/// </summary>
	/// <returns>True if this cell is on a special diagonal; false otherwise.</returns>
	public abstract bool Down();

	/// <summary>
	/// Compares two cells for equality.
	/// </summary>
	/// <param name="op1">The first cell to compare.</param>
	/// <param name="op2">The second cell to compare.</param>
	/// <returns>True if the cells are at the same position; false otherwise.</returns>
    public static bool operator ==(BaseCell? op1, BaseCell? op2)
	{
        if(ReferenceEquals(op1, op2)) return true;
        if(op1 is null || op2 is null) return false;
        return op1.Row == op2.Row && op1.Col == op2.Col;
	}

	/// <summary>
	/// Compares two cells for inequality.
	/// </summary>
    public static bool operator !=(BaseCell? op1, BaseCell? op2) => !(op1 == op2);

	/// <summary>
	/// Compares two cells by hash code (greater than).
	/// </summary>
    public static bool operator >(BaseCell? op1, BaseCell? op2) => (op1?.GetHashCode() ?? 0) > (op2?.GetHashCode() ?? 0);

	/// <summary>
	/// Compares two cells by hash code (less than).
	/// </summary>
    public static bool operator <(BaseCell? op1, BaseCell? op2) => (op1?.GetHashCode() ?? 0) < (op2?.GetHashCode() ?? 0);

	/// <summary>
	/// Determines whether the specified object is equal to this cell.
	/// </summary>
	/// <param name="obj">The object to compare.</param>
	/// <returns>True if the object represents the same cell; false otherwise.</returns>
	public override bool Equals(object? obj) => this == (obj as BaseCell);

	/// <summary>
	/// Gets the hash code for this cell based on its position.
	/// </summary>
	/// <returns>The hash code combining row and column coordinates.</returns>
	public override int GetHashCode() => Row * SudokuGrid.SudokuSize + Col;

	/// <summary>
	/// Gets or sets the current cell value (1-9 or undefined).
	/// </summary>
	public byte CellValue
	{
		get => coreValue.CellValue;
		set
		{
			if(CellValue == value) return;
			if(value != Values.Undefined)
			{
				DefinitiveValue = Values.Undefined;
				if(!Enabled(value)) throw new ArgumentException("value not possible", "value");
			}
			SetBlocks(CellValue, value, true);
			coreValue.CellValue = value;
		}
	}

	/// <summary>
	/// Gets or sets the definitive value that can be computed for this cell (1-9 or undefined).
	/// </summary>
	public byte DefinitiveValue
	{
		get => definitiveValue;
		set
		{
			if(DefinitiveValue == value) return;
			SetBlocks(DefinitiveValue, value, false);
			definitiveValue = value;
		}
	}

	/// <summary>
	/// Gets or sets the row coordinate of this cell (0-based).
	/// </summary>
	public int Row { get => coreValue.Row; set { coreValue.Row = value; startRow = (int)Math.Truncate((double)value / SudokuGrid.RectSize) * SudokuGrid.RectSize; } }

	/// <summary>
	/// Gets or sets the column coordinate of this cell (0-based).
	/// </summary>
	public int Col { get => coreValue.Col; set { coreValue.Col = value; startCol = (int)Math.Truncate((double)value / SudokuGrid.RectSize) * SudokuGrid.RectSize; } }

	/// <summary>
	/// Gets the starting row of the rectangle (box) containing this cell.
	/// </summary>
	public int StartRow => startRow;

	/// <summary>
	/// Gets the starting column of the rectangle (box) containing this cell.
	/// </summary>
	public int StartCol => startCol;

	/// <summary>
	/// Gets the array of all neighbor cells (sharing row, column, box, or diagonal).
	/// </summary>
    public BaseCell[] Neighbors => neighborCells ?? Array.Empty<BaseCell>();

	/// <summary>
	/// Gets the number of possible values remaining for this cell.
	/// </summary>
	public int nPossibleValues => (FixedValue || DefinitiveValue != Values.Undefined) ? 0 : possibleValuesCount - 1;

	/// <summary>
	/// Gets or sets a value indicating whether this cell contains a fixed (given) value.
	/// </summary>
	public bool FixedValue { get => fixedValue; set => fixedValue = value; }

	/// <summary>
	/// Gets or sets a value indicating whether this cell is read-only.
	/// </summary>
	public bool ReadOnly { get => readOnly; set => readOnly = value; }

	/// <summary>
	/// Gets or sets a value indicating whether this cell value was computed by the solver.
	/// </summary>
	public bool ComputedValue { get => computedValue; set => computedValue = value; }

	/// <summary>
	/// Initializes a new instance of the BaseCell class at the specified position.
	/// </summary>
	/// <param name="row">The row coordinate (0-based).</param>
	/// <param name="col">The column coordinate (0-based).</param>
	public BaseCell(int row, int col) { Row = row; Col = col; Init(); }

	/// <summary>
	/// Gets the number of neighbors that currently have a value assigned.
	/// </summary>
	public int FilledNeighborCount
	{
		get
		{
			int count = 0;
			if(neighborCells != null)
			{
				foreach(var neighbor in neighborCells)
				{
					if(neighbor.CellValue != Values.Undefined)
					{
						count++;
					}
				}
			}
			return count;
		}
	}

	/// <summary>
	/// Adds a neighbor cell to this cell's neighbor list.
	/// </summary>
	/// <param name="neighbor">The neighbor cell to add.</param>
    public void AddNeighbor(ref BaseCell neighbor)
    {
        if(neighborCells == null)
            neighborCells = new BaseCell[SudokuGrid.TotalCellCount];
        neighborCells[nNeighbors++] = neighbor;
    }

	/// <summary>
	/// Compares this cell with another object for ordering purposes.
	/// Cells with fixed values are considered greater, then cells are ordered by constraint level and position.
	/// </summary>
	/// <param name="obj">The object to compare with.</param>
	/// <returns>A negative number if this cell should be processed earlier; positive if later; zero if equal.</returns>
    public int CompareTo(object? obj)
	{
		if(obj == null) return -1;
        BaseCell? tmpObj = obj as BaseCell;
        if(tmpObj == null) throw new ArgumentException(obj.ToString());
		if(FixedValue) return int.MaxValue;
		if(tmpObj.FixedValue) return int.MinValue;
		return ((nPossibleValues * SudokuGrid.TotalCellCount + Row * SudokuGrid.SudokuSize + Col) - (tmpObj.nPossibleValues * SudokuGrid.TotalCellCount + tmpObj.Row * SudokuGrid.SudokuSize + tmpObj.Col));
	}

    /// <summary>
    /// Determines whether the specified candidate value is currently enabled for this cell.
    /// Ensures the internal enabled mask is initialized before testing the bit corresponding to the value.
    /// </summary>
    /// <param name="value">The candidate value to check. Valid range is 1..SudokuGridConstants.SudokuSize.</param>
    /// <returns>
    /// <c>true</c> if the candidate is allowed for this cell; otherwise, <c>false</c>.
    /// Returns <c>false</c> for values outside the valid range.
    /// </returns>
    /// <remarks>
    /// Calling this method may lazily initialize the cell's enabled mask via <see cref="EnsureEnabledMaskInitialized"/>.
    /// The enabled mask stores possible values as bit flags where bit (1 &lt;&lt; value) indicates availability.
    /// </remarks>
    public bool Enabled(int value)
    {
        if(value < 1 || value > SudokuGrid.SudokuSize) return false;
        EnsureEnabledMaskInitialized();
        return (enabledMask & (1 << value)) != 0;
    }

    /// <summary>
    /// Returns whether the specified value is blocked by a direct neighbor (i.e. same row/col/box/diagonal).
    /// </summary>
    /// <param name="value">Candidate value to check.</param>
    /// <returns>True if at least one direct neighbor currently blocks the value; otherwise false.</returns>
    public bool Blocked(int value) => directBlocks![value] != 0;

    /// <summary>
    /// Returns whether the specified value is blocked indirectly (e.g. by other derived constraints).
    /// </summary>
    /// <param name="value">Candidate value to check.</param>
    /// <returns>True if the value is indirectly blocked; otherwise false.</returns>
    public bool IndirectlyBlocked(int value) => indirectBlocks![value] != 0;

    /// <summary>
    /// Initializes internal block arrays, candidate masks and flags for this cell.
    /// This prepares the cell to be used in a fresh puzzle or after resetting state.
    /// </summary>
    public void Init()
    {
        InitDirectBlocks();
        InitIndirectBlocks();
        InitCandidates();
        possibleValuesCount = directBlocks!.Length;
        coreValue.CellValue = Values.Undefined;
        DefinitiveValue = Values.Undefined;
        FixedValue = false;
        ComputedValue = false;
        ReadOnly = false;
    }

    /// <summary>
    /// Resets the candidate bitmasks for this cell.
    /// Candidates and exclusion candidates are cleared; enabled mask is not forcibly reinitialized
    /// because it is derived from block arrays.
    /// </summary>
    public void InitCandidates()
    {
        candidatesMask = 0;
        exclusionCandidatesMask = 0;
        // enabledMask is derived from direct/indirect blocks, not from candidates
        // so keep enabledMaskInitialized as-is to avoid unnecessary re-initialization.
    }

    /// <summary>
    /// Ensure the internal enabled-mask is initialized.
    /// The enabled mask contains one bit per candidate value that is currently allowed,
    /// computed from direct and indirect block counters.
    /// </summary>
    private void EnsureEnabledMaskInitialized()
    {
        if(enabledMaskInitialized) return;
        enabledMask = 0;
        for(int i = 1; i <= SudokuGrid.SudokuSize; i++) if(directBlocks![i] == 0 && indirectBlocks![i] == 0) enabledMask |= (1 << i);
        enabledMaskInitialized = true;
    }

    /// <summary>
    /// Initialize indirect block counters and compute an initial enabled mask.
    /// This method also sets the definitive value to undefined.
    /// </summary>
    public void InitIndirectBlocks()
    {
        indirectBlocks = new int[SudokuGrid.SudokuSize + 1];
        possibleValuesCount = directBlocks!.Length;
        enabledMask = 0;
        for(int i = 1; i <= SudokuGrid.SudokuSize; i++)
        {
            if(directBlocks![i] == 0 && indirectBlocks![i] == 0) enabledMask |= (1 << i); else possibleValuesCount--;
        }
        enabledMaskInitialized = true;
        definitiveValue = Values.Undefined;
    }

    /// <summary>
    /// Initializes direct block counters for each candidate value.
    /// A direct block count is incremented when a direct neighbor holds the value.
    /// </summary>
    private void InitDirectBlocks() { directBlocks = new int[SudokuGrid.SudokuSize + 1]; }

    /// <summary>
    /// Ensure the static low-bit index lookup table is initialized.
    /// This table speeds up computing the index/position of a single set bit.
    /// </summary>
    private static void EnsureLowbitIndex()
    {
        if(lowbitIndex != null) return;
        int size = 1 << 10;
        lowbitIndex = new int[size];
        for(int i = 0; i < size; i++) lowbitIndex[i] = -1;
        for(int b = 0; b < 10; b++) lowbitIndex[1 << b] = b;
    }

    /// <summary>
    /// Returns the zero-based index of the provided low-bit integer (e.g. 1<<n => n).
    /// If the value is small and within the lookup table, the cached index is returned;
    /// otherwise the index is computed by bit-shifting.
    /// </summary>
    /// <param name="lowbit">An integer containing exactly one set bit.</param>
    /// <returns>The index (0-based) of the set bit.</returns>
    internal static int LowBitIndex(int lowbit)
    {
        EnsureLowbitIndex();
        if(lowbit > 0 && lowbit < lowbitIndex!.Length) return lowbitIndex![lowbit];
        int idx = 0; while(lowbit > 1) { lowbit >>= 1; idx++; }
        return idx;
    }

    /// <summary>
    /// Returns the population count (number of set bits) of the given integer.
    /// Uses a lazily-initialized 16-bit lookup table for performance on wide integers.
    /// </summary>
    /// <param name="v">The integer whose bits should be counted.</param>
    /// <returns>The number of one-bits in the integer.</returns>
    private static int PopCount(int v)
    {
        // 16-bit lookup table population count (lazy initialized)
        if(popcountCache == null)
        {
            // initialize table for 0..65535
            popcountCache = new byte[1 << 16];
            for(int i = 0; i < popcountCache.Length; i++)
            {
                int x = i;
                x = x - ((x >> 1) & 0x5555);
                x = (x & 0x3333) + ((x >> 2) & 0x3333);
                x = (x + (x >> 4)) & 0x0F0F;
                popcountCache[i] = (byte)((x * 0x0101) >> 8);
            }
        }
        uint ux = (uint)v;
        return popcountCache![ux & 0xFFFF] + popcountCache![(ux >> 16) & 0xFFFF];
    }

    /// <summary>
    /// Computes a definitive value for this cell if one exists, otherwise returns undefined.
    /// The method inspects enabled candidates and uses the current nPossibleValues to decide.
    /// </summary>
    /// <returns>The single definite candidate value or <see cref="Values.Undefined"/> if none or ambiguous.</returns>
    private byte GetDefiniteValue()
    {
        if(DefinitiveValue != Values.Undefined) return DefinitiveValue;
        bool found = false; byte dv = Values.Undefined;
        for(byte possibleValue = 1; possibleValue < SudokuGrid.SudokuSize + 1; possibleValue++)
            if(Enabled(possibleValue) && nPossibleValues == 1)
            {
                if(found) return Values.Undefined; found = true; dv = possibleValue;
            }
        return dv;
    }

    /// <summary>
    /// Attempts to fill the DefinitiveValue property from computed state.
    /// Throws <see cref="InvalidSudokuValueException"/> if no definite value can be determined.
    /// </summary>
    public void FillDefiniteValue() { if((DefinitiveValue = GetDefiniteValue()) == Values.Undefined) throw new InvalidSudokuValueException(); }

    /// <summary>
    /// Update block counters and neighbor masks when a cell's value changes.
    /// This method adjusts either direct or indirect block counters for the old and new values
    /// and enables/disables neighbor candidates accordingly.
    /// </summary>
    /// <param name="oldValue">Previous value (may be <see cref="Values.Undefined"/>).</param>
    /// <param name="newValue">New value to apply (may be <see cref="Values.Undefined"/>).</param>
    /// <param name="direct">If true, update direct block counters; otherwise update indirect counters.</param>
    private void SetBlocks(byte oldValue, byte newValue, bool direct)
    {
        if(oldValue != Values.Undefined)
        {
            if(direct)
                SetBlock(oldValue, true, direct);
            else
                for(int i = 1; i < SudokuGrid.SudokuSize + 1; i++)
                    SetBlock(i, true, direct);
            EnableNeighbors(oldValue, direct);
        }
        if(newValue != Values.Undefined)
        {
            if(direct)
                SetBlock(newValue, false, direct);
            else
                for(int i = 1; i < SudokuGrid.SudokuSize + 1; i++)
                    SetBlock(i, false, direct);
            DisableNeighbors(newValue, direct);
        }
    }

    /// <summary>
    /// Enable a previously blocked candidate on all neighbor cells.
    /// </summary>
    /// <param name="value">The candidate value to enable.</param>
    /// <param name="direct">Whether to update direct or indirect block counters on neighbors.</param>
    private void EnableNeighbors(byte value, bool direct) { SetNeighborBlocks(value, true, direct); }

    /// <summary>
    /// Disable a candidate on all neighbor cells.
    /// </summary>
    /// <param name="value">The candidate value to disable.</param>
    /// <param name="direct">Whether to update direct or indirect block counters on neighbors.</param>
    private void DisableNeighbors(byte value, bool direct) { SetNeighborBlocks(value, false, direct); }

    /// <summary>
    /// Set or clear block counters for the specified candidate on every neighbor.
    /// </summary>
    /// <param name="newValue">Candidate value to change.</param>
    /// <param name="enable">True to decrement (enable) the block counter; false to increment (disable) it.</param>
    /// <param name="direct">True to modify directBlocks; false to modify indirectBlocks.</param>
    private void SetNeighborBlocks(byte newValue, bool enable, bool direct) { foreach(BaseCell neighbor in (neighborCells ?? Array.Empty<BaseCell>())) neighbor.SetBlock(newValue, enable, direct); }

    /// <summary>
    /// Public entry to set or clear a block for a specific candidate on this cell.
    /// Ensures the enabled mask has been initialized before updating internal counters.
    /// </summary>
    /// <param name="value">Candidate value (1..SudokuSize).</param>
    /// <param name="enable">True to enable the value (decrement counters), false to disable (increment).</param>
    /// <param name="direct">Whether this is a direct or indirect block update.</param>
    public void SetBlock(int value, bool enable, bool direct) { EnsureEnabledMaskInitialized(); SetBlockInternal(value, enable, direct); }

    /// <summary>
    /// Internal implementation that manipulates block counters and updates the enabled mask.
    /// It increments/decrements the appropriate block array and adjusts the cached possible count.
    /// </summary>
    /// <param name="value">Candidate value index to change.</param>
    /// <param name="enable">If true, decrement the block count (making the value available when counters reach zero).</param>
    /// <param name="direct">If true operate on directBlocks, otherwise on indirectBlocks.</param>
    private void SetBlockInternal(int value, bool enable, bool direct)
    {
        int bit = 1 << value;
        bool beforeEnabled = (enabledMask & bit) != 0;
        if(enable)
        {
            if(direct) { if(--directBlocks![value] < 0) throw new ArgumentException("enable not possible", "enable"); }
            else { if(--indirectBlocks![value] < 0) throw new ArgumentException("enable not possible", "enable"); }
            if((directBlocks![value] == 0 && indirectBlocks![value] == 0)) possibleValuesCount++;
        }
        else
        {
            if((directBlocks![value] == 0 && indirectBlocks![value] == 0)) possibleValuesCount--;
            if(direct) directBlocks![value]++; else indirectBlocks![value]++;
        }
        bool afterEnabled = (directBlocks![value] == 0 && indirectBlocks![value] == 0);
        if(beforeEnabled != afterEnabled) { if(afterEnabled) enabledMask |= bit; else enabledMask &= ~bit; }
    }

    /// <summary>
    /// Attempt to set or clear a block and return whether the enabled state actually changed.
    /// The enabled mask is ensured to be initialized before the operation.
    /// </summary>
    /// <param name="value">Candidate value to modify.</param>
    /// <param name="enable">True to enable, false to disable.</param>
    /// <param name="direct">Whether to update direct or indirect counters.</param>
    /// <returns>True if the effective enabled/disabled state of the value changed; otherwise false.</returns>
    public bool TrySetBlock(int value, bool enable, bool direct)
    {
        EnsureEnabledMaskInitialized();
        int bit = 1 << value;
        bool before = (enabledMask & bit) != 0;
        SetBlockInternal(value, enable, direct);
        bool after = (enabledMask & bit) != 0;
        return before != after;
    }

    /// <summary>
    /// Disable all candidate bits present in the mask for this cell (as direct or indirect blocks).
    /// Returns true if any of the specified bits changed their enabled state.
    /// </summary>
    /// <param name="mask">Bitmask of candidate values to disable.</param>
    /// <param name="direct">Whether to apply the change as direct or indirect blocks.</param>
    /// <returns>True if at least one candidate in the mask had its enabled state changed.</returns>
    public bool TryDisableMask(int mask, bool direct)
    {
        EnsureEnabledMaskInitialized();
        int before = enabledMask & mask;
        int m = mask;
        while(m != 0)
        {
            int lowbit = m & -m;
            int value = LowBitIndex(lowbit);
            SetBlockInternal(value, false, direct);
            m &= (m - 1);
        }
        int after = enabledMask & mask;
        return before != after;
    }

    /// <summary>
    /// Returns the internal enabled mask for this cell (bit per candidate).
    /// Ensures lazy initialization before returning.
    /// </summary>
    public int GetEnabledMask() { EnsureEnabledMaskInitialized(); return enabledMask; }

    /// <summary>
    /// Indicates whether this cell currently has any candidate or exclusion-candidate marks set.
    /// </summary>
    /// <returns>True when either candidate or exclusion-candidate masks are non-zero.</returns>
    public bool HasCandidate()
    {
        return candidatesMask != 0 || exclusionCandidatesMask != 0;
    }

    /// <summary>
    /// Tests whether a particular candidate is set either as a normal or an exclusion candidate.
    /// </summary>
    /// <param name="candidate">Candidate value to check.</param>
    /// <param name="exclusionCandidate">Set true to test the exclusion mask; false to test the normal candidate mask.</param>
    /// <returns>True if the requested mask contains the candidate; otherwise false.</returns>
    public bool GetCandidateMask(int candidate, bool exclusionCandidate)
    {
        if(candidate < 1 || candidate > SudokuGrid.SudokuSize) return false;
        int bit = 1 << candidate;
        if(exclusionCandidate) return (exclusionCandidatesMask & bit) != 0;
        return (candidatesMask & bit) != 0;
    }

    /// <summary>
    /// Toggle a candidate bit in either the normal or exclusion candidate mask.
    /// Ensures a candidate is not present in both masks simultaneously.
    /// </summary>
    /// <param name="candidate">The candidate value to toggle.</param>
    /// <param name="exclusionCandidate">If true toggle the exclusion mask, otherwise the normal candidate mask.</param>
    public void ToggleCandidateMask(int candidate, bool exclusionCandidate)
    {
        if(candidate < 1 || candidate > SudokuGrid.SudokuSize) throw new ArgumentOutOfRangeException(nameof(candidate));
        if(GetCandidateMask(candidate, !exclusionCandidate)) ToggleCandidateMask(candidate, !exclusionCandidate); // prevent candidate from being in both masks
        int bit = 1 << candidate;
        if(exclusionCandidate) exclusionCandidatesMask ^= bit; else candidatesMask ^= bit;
    }

    /// <summary>
    /// Helper to check if any of the allowedMask bits overlap with the enabled mask.
    /// </summary>
    /// <param name="allowedMask">Bitmask representing candidate values to test.</param>
    /// <returns>True if at least one allowed candidate is currently enabled.</returns>
    private bool Change(int allowedMask) { return (GetEnabledMask() & allowedMask) != 0; }

    /// <summary>
    /// Public entry that performs a naked-cell detection using neighbor cells and returns a metric value.
    /// Allocates scratch arrays temporarily and ensures they are released.
    /// </summary>
    /// <param name="neighborCells">Array of neighbor cells to analyze.</param>
    /// <returns>Positive encoded metric when a naked set is found; otherwise -1.</returns>
    public int FindNakedCells(BaseCell[] neighborCells)
    {
        NakedScratch scratch = default;
        try { return FindNakedCells(neighborCells, ref scratch); }
        finally { scratch.Release(); }
    }

    /// <summary>
    /// Naked-cell detection entry that accepts an externally provided scratch buffer to avoid allocations.
    /// </summary>
    /// <param name="neighborCells">Array of neighbor cells to analyze.</param>
    /// <param name="scratch">Scratch storage reused during the computation.</param>
    /// <returns>Positive encoded metric when a naked set is found; otherwise -1.</returns>
    public int FindNakedCells(BaseCell[] neighborCells, ref NakedScratch scratch)
    {
        if(FindNakedCombination(neighborCells, ref scratch)) return nPossibleValues * 2;
        return -1;
    }

    /// <summary>
    /// Core algorithm to find a naked combination among neighbor cells.
    /// When found, disables candidate bits in non-involved neighbors.
    /// </summary>
    /// <param name="neighborCells">Array of neighbor cells to inspect.</param>
    /// <param name="scratch">Temporary storage used to avoid per-call allocations.</param>
    /// <returns>True if any candidates were removed from other neighbor cells as a result.</returns>
    private bool FindNakedCombination(BaseCell[] neighborCells, ref NakedScratch scratch)
    {
        bool rc = false;

        // fast guards
        if(CellValue != Values.Undefined) return false;
        int count = nPossibleValues;
        if(count <= 1 || count >= 8) return false;

        int allowedMask = GetEnabledMask();
        if(allowedMask == 0) return false;

        int nlen = neighborCells.Length;

        scratch.Ensure(nlen);

        int[] threadNeighborMasks = scratch.NeighborMasks!;
        byte[] threadNeighborCounts = scratch.NeighborCounts!;
        BaseCell[] threadCandidateArr = scratch.CandidateArr!;
        int[] threadCommonStamp = scratch.CommonStamp!;

        // collect neighbor masks and popcounts into reused arrays
        for(int ni = 0; ni < nlen; ni++)
        {
            var nc = neighborCells[ni];
            if(nc.CellValue == Values.Undefined)
            {
                int nm = nc.GetEnabledMask();
                threadNeighborMasks[ni] = nm;
                threadNeighborCounts[ni] = (byte)PopCount(nm);
            }
            else
            {
                threadNeighborMasks[ni] = 0;
                threadNeighborCounts[ni] = 0;
            }
        }

        // cheap early rejects: not enough candidate cells or insufficient union bits
        int cheapCandidateCount = 0;
        int unionMasks = 0;
        for(int ni = 0; ni < nlen; ni++)
        {
            int nm = threadNeighborMasks[ni];
            if(threadNeighborCounts[ni] == 0) continue;
            if((nm & ~allowedMask) != 0) continue;
            cheapCandidateCount++;
            unionMasks |= nm;
        }
        if(cheapCandidateCount < count) return false;
        if(PopCount(unionMasks) < count) return false;

        // collect candidate neighbor cells (masks subset of allowed and popcount <= count)
		int candidateCount = 0;
		for(int ni = 0; ni < nlen; ni++)
		{
			if(threadNeighborCounts[ni] == 0) continue;
			int nm = threadNeighborMasks[ni];
			if(threadNeighborCounts[ni] <= count && (nm & ~allowedMask) == 0)
				threadCandidateArr[candidateCount++] = neighborCells[ni];
		}

		if(candidateCount != count || candidateCount == 0) return false;

		// mark candidate cells
		Array.Clear(threadCommonStamp, 0, SudokuGrid.TotalCellCount);
		for(int ci = 0; ci < candidateCount; ci++)
		{
			var c = threadCandidateArr[ci];
			int idx = c.Row * SudokuGrid.SudokuSize + c.Col;
			threadCommonStamp[idx] = 1;
		}

		for(int ni = 0; ni < nlen; ni++)
		{
			BaseCell updateCell = neighborCells[ni];
			if(updateCell == this) continue;
			if(updateCell.CellValue != Values.Undefined) continue;
			int uidx = updateCell.Row * SudokuGrid.SudokuSize + updateCell.Col;
			if(threadCommonStamp[uidx] != 0) continue;
            // quick check: use cached neighbor mask collected earlier to skip TryDisableMask
            int updateMask = threadNeighborMasks[ni];
            if((updateMask & allowedMask) == 0) continue;
            if(updateCell.TryDisableMask(allowedMask, false)) rc = true;
        }

        return rc;
    }

    /// <summary>
    /// Build a list of neighbor cells that are common to a set of candidate neighbors.
    /// The returned list contains neighbors that are not part of the candidateNeighbors set and are currently unset.
    /// </summary>
    /// <param name="candidateNeighbors">List of neighbor cells that form the candidate set.</param>
    /// <param name="neighborCells">All neighbor cells to consider.</param>
    /// <returns>A list of common neighbor cells that can be affected by the candidate set.</returns>
    protected virtual List<BaseCell> GetCommonNeighbors(List<BaseCell> candidateNeighbors, BaseCell[] neighborCells)
    {
        int total = SudokuGrid.TotalCellCount;
        // rent stamp array
        var intPool2 = System.Buffers.ArrayPool<int>.Shared;
        int[] stamp = intPool2.Rent(total);
        try
        {
            Array.Clear(stamp, 0, total);
            foreach(BaseCell c in candidateNeighbors)
            {
                int idx = c.Row * SudokuGrid.SudokuSize + c.Col;
                stamp[idx] = 1;
            }

            List<BaseCell> commonNeighbors = new List<BaseCell>();
            foreach(BaseCell cell in neighborCells)
            {
                if(cell == this || cell.CellValue != Values.Undefined) continue;
                int idx = cell.Row * SudokuGrid.SudokuSize + cell.Col;
                if(stamp[idx] == 0) commonNeighbors.Add(cell);
            }

            return commonNeighbors;
        }
        finally
        {
            intPool2.Return(stamp, true);
        }
    }

    /// <summary>
    /// Determines whether the provided cell is one of this cell's neighbors.
    /// </summary>
    /// <param name="neighbor">Cell to test.</param>
    /// <returns>True if the provided cell is contained in the neighbors array; otherwise false.</returns>
    public bool CommonNeighbor(BaseCell neighbor) { bool common = false; foreach(BaseCell cell in Neighbors) common = (cell == neighbor || common); return common; }

    /// <summary>
    /// Tests whether this cell and the supplied cell are inside the same box/rectangle.
    /// </summary>
    /// <param name="value">Cell to compare with.</param>
    /// <returns>True if both cells share the same Sudoku rectangle (box); otherwise false.</returns>
    public bool SameRectangle(BaseCell value) { return (Col >= value.StartCol && Col < value.StartCol + SudokuGrid.RectSize && Row >= value.StartRow && Row < value.StartRow + SudokuGrid.RectSize); }

    /// <summary>
    /// Copies the internal state of this cell to the provided target cell instance.
    /// Only internal arrays are cloned to avoid sharing mutable state between cells.
    /// </summary>
    /// <param name="target">Target cell to receive a copy of the state.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
    public void CopyTo(BaseCell target)
    {
        if(target == null) throw new ArgumentNullException(nameof(target));

        target.definitiveValue = this.definitiveValue;
        target.candidatesMask = this.candidatesMask;
        target.exclusionCandidatesMask = this.exclusionCandidatesMask;
        target.enabledMask = this.enabledMask;
        target.enabledMaskInitialized = this.enabledMaskInitialized;
        target.possibleValuesCount = this.possibleValuesCount;
        target.fixedValue = this.fixedValue;
        target.computedValue = this.computedValue;
        target.readOnly = this.readOnly;

        target.coreValue.CellValue = this.coreValue.CellValue;
        target.coreValue.UnformatedValue = this.coreValue.UnformatedValue;

        if(this.directBlocks != null)
        {
            if(target.directBlocks == null || target.directBlocks.Length != this.directBlocks.Length)
                target.directBlocks = (int[])this.directBlocks.Clone();
            else
                Array.Copy(this.directBlocks, target.directBlocks, this.directBlocks.Length);
        }

        if(this.indirectBlocks != null)
        {
            if(target.indirectBlocks == null || target.indirectBlocks.Length != this.indirectBlocks.Length)
                target.indirectBlocks = (int[])this.indirectBlocks.Clone();
            else
                Array.Copy(this.indirectBlocks, target.indirectBlocks, this.indirectBlocks.Length);
        }
    }
}