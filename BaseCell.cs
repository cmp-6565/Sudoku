using System;
using System.Collections.Generic;

namespace Sudoku;

[Serializable]
internal abstract class BaseCell: EventArgs, IComparable
{
    private CoreValue coreValue = new CoreValue();
    private byte definitiveValue = Values.Undefined;
    private int nNeighbors = 0;
    private int[] directBlocks;
    private int[] indirectBlocks;
    private int candidatesMask = 0;
    private int exclusionCandidatesMask = 0;
    private int enabledMask = 0;
    private bool enabledMaskInitialized = false;
    private int possibleValuesCount = 0;
    private bool fixedValue = false;
    private bool computedValue = false;
    private bool readOnly = false;
    protected BaseCell[] neighbors;
    private int startCol = 0;
    private int startRow = 0;

    // cached helpers
    private static byte[] popcountCache;
    private static int[] lowbitIndex;

	internal struct NakedScratch
	{
		public int[] NeighborMasks;
		public byte[] NeighborCounts;
		public BaseCell[] CandidateArr;
		public int[] CommonStamp;

		public void Ensure(int neighborLen)
		{
			var intPool = System.Buffers.ArrayPool<int>.Shared;
			var bytePool = System.Buffers.ArrayPool<byte>.Shared;
			var cellPool = System.Buffers.ArrayPool<BaseCell>.Shared;

			if(NeighborMasks == null || NeighborMasks.Length < neighborLen)
				NeighborMasks = intPool.Rent(neighborLen);
			if(NeighborCounts == null || NeighborCounts.Length < neighborLen)
				NeighborCounts = bytePool.Rent(neighborLen);
			if(CandidateArr == null || CandidateArr.Length < WinFormsSettings.SudokuSize)
				CandidateArr = cellPool.Rent(WinFormsSettings.SudokuSize);
			if(CommonStamp == null || CommonStamp.Length < WinFormsSettings.TotalCellCount)
				CommonStamp = intPool.Rent(WinFormsSettings.TotalCellCount);
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
    public abstract bool Up();
    public abstract bool Down();

    public static bool operator ==(BaseCell op1, BaseCell op2)
    {
        if(ReferenceEquals(op1, op2)) return true;
        if(op1 is null || op2 is null) return false;
        return op1.Row == op2.Row && op1.Col == op2.Col;
    }

    public static bool operator !=(BaseCell op1, BaseCell op2) => !(op1 == op2);
    public static bool operator >(BaseCell op1, BaseCell op2) => op1.GetHashCode() > op2.GetHashCode();
    public static bool operator <(BaseCell op1, BaseCell op2) => op1.GetHashCode() < op2.GetHashCode();

    public override bool Equals(object obj) => this == (obj as BaseCell);
    public override int GetHashCode() => Row * WinFormsSettings.SudokuSize + Col;

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

    public int Row { get => coreValue.Row; set { coreValue.Row = value; startRow = (int)Math.Truncate((double)value / WinFormsSettings.RectSize) * WinFormsSettings.RectSize; } }
    public int Col { get => coreValue.Col; set { coreValue.Col = value; startCol = (int)Math.Truncate((double)value / WinFormsSettings.RectSize) * WinFormsSettings.RectSize; } }
    public int StartRow => startRow;
    public int StartCol => startCol;
    public BaseCell[] Neighbors => neighbors;

    public int nPossibleValues => (FixedValue || DefinitiveValue != Values.Undefined) ? 0 : possibleValuesCount - 1;
    public bool FixedValue { get => fixedValue; set => fixedValue = value; }
    public bool ReadOnly { get => readOnly; set => readOnly = value; }
    public bool ComputedValue { get => computedValue; set => computedValue = value; }

    public BaseCell(int row, int col) { Row = row; Col = col; Init(); }

    public int FilledNeighborCount
    {
        get
        {
            int count = 0;
            if(neighbors != null)
            {
                foreach(var neighbor in neighbors)
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
    public void AddNeighbor(ref BaseCell neighbor) { neighbors[nNeighbors++] = neighbor; }

    public int CompareTo(object obj)
    {
        if(obj == null) return -1;
        BaseCell tmpObj = obj as BaseCell;
        if(tmpObj == null) throw new ArgumentException(obj.ToString());
        if(FixedValue) return int.MaxValue;
        if(tmpObj.FixedValue) return int.MinValue;
        return ((nPossibleValues * WinFormsSettings.TotalCellCount + Row * WinFormsSettings.SudokuSize + Col) - (tmpObj.nPossibleValues * WinFormsSettings.TotalCellCount + tmpObj.Row * WinFormsSettings.SudokuSize + tmpObj.Col));
    }

    public bool Enabled(int value)
    {
        if(value < 1 || value > WinFormsSettings.SudokuSize) return false;
        EnsureEnabledMaskInitialized();
        return (enabledMask & (1 << value)) != 0;
    }

    public bool Blocked(int value) => directBlocks[value] != 0;
    public bool IndirectlyBlocked(int value) => indirectBlocks[value] != 0;

    public void Init()
    {
        InitDirectBlocks();
        InitIndirectBlocks();
        InitCandidates();
        possibleValuesCount = directBlocks.Length;
        coreValue.CellValue = Values.Undefined;
        DefinitiveValue = Values.Undefined;
        FixedValue = false;
        ComputedValue = false;
        ReadOnly = false;
    }

    public void InitCandidates()
    {
        candidatesMask = 0;
        exclusionCandidatesMask = 0;
        // enabledMask is derived from direct/indirect blocks, not from candidates
        // so keep enabledMaskInitialized as-is to avoid unnecessary re-initialization.
    }

    private void EnsureEnabledMaskInitialized()
    {
        if(enabledMaskInitialized) return;
        enabledMask = 0;
        for(int i = 1; i <= WinFormsSettings.SudokuSize; i++) if(directBlocks[i] == 0 && indirectBlocks[i] == 0) enabledMask |= (1 << i);
        enabledMaskInitialized = true;
    }

    public void InitIndirectBlocks()
    {
        indirectBlocks = new int[WinFormsSettings.SudokuSize + 1];
        possibleValuesCount = directBlocks.Length;
        enabledMask = 0;
        for(int i = 1; i <= WinFormsSettings.SudokuSize; i++)
        {
            if(directBlocks[i] == 0 && indirectBlocks[i] == 0) enabledMask |= (1 << i); else possibleValuesCount--;
        }
        enabledMaskInitialized = true;
        definitiveValue = Values.Undefined;
    }

    private void InitDirectBlocks() { directBlocks = new int[WinFormsSettings.SudokuSize + 1]; }

    private static void EnsureLowbitIndex()
    {
        if(lowbitIndex != null) return;
        int size = 1 << 10;
        lowbitIndex = new int[size];
        for(int i = 0; i < size; i++) lowbitIndex[i] = -1;
        for(int b = 0; b < 10; b++) lowbitIndex[1 << b] = b;
    }

    internal static int LowBitIndex(int lowbit)
    {
        EnsureLowbitIndex();
        if(lowbit > 0 && lowbit < lowbitIndex.Length) return lowbitIndex[lowbit];
        int idx = 0; while(lowbit > 1) { lowbit >>= 1; idx++; }
        return idx;
    }

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
        return popcountCache[ux & 0xFFFF] + popcountCache[(ux >> 16) & 0xFFFF];
    }

    private byte GetDefiniteValue()
    {
        if(DefinitiveValue != Values.Undefined) return DefinitiveValue;
        bool found = false; byte dv = Values.Undefined;
        for(byte possibleValue = 1; possibleValue < WinFormsSettings.SudokuSize + 1; possibleValue++)
            if(Enabled(possibleValue) && nPossibleValues == 1)
            {
                if(found) return Values.Undefined; found = true; dv = possibleValue;
            }
        return dv;
    }

    public void FillDefiniteValue() { if((DefinitiveValue = GetDefiniteValue()) == Values.Undefined) throw new InvalidSudokuValueException(); }

    private void SetBlocks(byte oldValue, byte newValue, bool direct)
    {
        if(oldValue != Values.Undefined)
        {
            if(direct)
                SetBlock(oldValue, true, direct);
            else
                for(int i = 1; i < WinFormsSettings.SudokuSize + 1; i++)
                    SetBlock(i, true, direct);
            EnableNeighbors(oldValue, direct);
        }
        if(newValue != Values.Undefined)
        {
            if(direct)
                SetBlock(newValue, false, direct);
            else
                for(int i = 1; i < WinFormsSettings.SudokuSize + 1; i++)
                    SetBlock(i, false, direct);
            DisableNeighbors(newValue, direct);
        }
    }

    private void EnableNeighbors(byte value, bool direct) { SetNeighborBlocks(value, true, direct); }
    private void DisableNeighbors(byte value, bool direct) { SetNeighborBlocks(value, false, direct); }
    private void SetNeighborBlocks(byte newValue, bool enable, bool direct) { foreach(BaseCell neighbor in neighbors) neighbor.SetBlock(newValue, enable, direct); }

    public void SetBlock(int value, bool enable, bool direct) { EnsureEnabledMaskInitialized(); SetBlockInternal(value, enable, direct); }

    private void SetBlockInternal(int value, bool enable, bool direct)
    {
        int bit = 1 << value;
        bool beforeEnabled = (enabledMask & bit) != 0;
        if(enable)
        {
            if(direct) { if(--directBlocks[value] < 0) throw new ArgumentException("enable not possible", "enable"); }
            else { if(--indirectBlocks[value] < 0) throw new ArgumentException("enable not possible", "enable"); }
            if((directBlocks[value] == 0 && indirectBlocks[value] == 0)) possibleValuesCount++;
        }
        else
        {
            if((directBlocks[value] == 0 && indirectBlocks[value] == 0)) possibleValuesCount--;
            if(direct) directBlocks[value]++; else indirectBlocks[value]++;
        }
        bool afterEnabled = (directBlocks[value] == 0 && indirectBlocks[value] == 0);
        if(beforeEnabled != afterEnabled) { if(afterEnabled) enabledMask |= bit; else enabledMask &= ~bit; }
    }

    public bool TrySetBlock(int value, bool enable, bool direct)
    {
        EnsureEnabledMaskInitialized();
        int bit = 1 << value;
        bool before = (enabledMask & bit) != 0;
        SetBlockInternal(value, enable, direct);
        bool after = (enabledMask & bit) != 0;
        return before != after;
    }

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

    public int GetEnabledMask() { EnsureEnabledMaskInitialized(); return enabledMask; }

    public bool HasCandidate()
    {
        return candidatesMask != 0 || exclusionCandidatesMask != 0;
    }

    public bool GetCandidateMask(int candidate, bool exclusionCandidate)
    {
        if(candidate < 1 || candidate > WinFormsSettings.SudokuSize) return false;
        int bit = 1 << candidate;
        if(exclusionCandidate) return (exclusionCandidatesMask & bit) != 0;
        return (candidatesMask & bit) != 0;
    }

    public void ToggleCandidateMask(int candidate, bool exclusionCandidate)
    {
        if(candidate < 1 || candidate > WinFormsSettings.SudokuSize) throw new ArgumentOutOfRangeException(nameof(candidate));
        int bit = 1 << candidate;
        if(exclusionCandidate) exclusionCandidatesMask ^= bit; else candidatesMask ^= bit;
    }

    private bool Change(int allowedMask) { return (GetEnabledMask() & allowedMask) != 0; }

    public int FindNakedCells(BaseCell[] neighborCells)
    {
        NakedScratch scratch = default;
        try { return FindNakedCells(neighborCells, ref scratch); }
        finally { scratch.Release(); }
    }

    public int FindNakedCells(BaseCell[] neighborCells, ref NakedScratch scratch)
    {
        if(FindNakedCombination(neighborCells, ref scratch)) return nPossibleValues * 2;
        return -1;
    }

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

        int[] threadNeighborMasks = scratch.NeighborMasks;
        byte[] threadNeighborCounts = scratch.NeighborCounts;
		BaseCell[] threadCandidateArr = scratch.CandidateArr;
		int[] threadCommonStamp = scratch.CommonStamp;

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
		Array.Clear(threadCommonStamp, 0, WinFormsSettings.TotalCellCount);
		for(int ci = 0; ci < candidateCount; ci++)
		{
			var c = threadCandidateArr[ci];
			int idx = c.Row * WinFormsSettings.SudokuSize + c.Col;
			threadCommonStamp[idx] = 1;
		}

		for(int ni = 0; ni < nlen; ni++)
		{
			BaseCell updateCell = neighborCells[ni];
			if(updateCell == this) continue;
			if(updateCell.CellValue != Values.Undefined) continue;
			int uidx = updateCell.Row * WinFormsSettings.SudokuSize + updateCell.Col;
			if(threadCommonStamp[uidx] != 0) continue;
            // quick check: use cached neighbor mask collected earlier to skip TryDisableMask
            int updateMask = threadNeighborMasks[ni];
            if((updateMask & allowedMask) == 0) continue;
            if(updateCell.TryDisableMask(allowedMask, false)) rc = true;
        }

        return rc;
    }

    protected virtual List<BaseCell> GetCommonNeighbors(List<BaseCell> candidateNeighbors, BaseCell[] neighborCells)
    {
        int total = WinFormsSettings.TotalCellCount;
        // rent stamp array
        var intPool2 = System.Buffers.ArrayPool<int>.Shared;
        int[] stamp = intPool2.Rent(total);
        try
        {
            Array.Clear(stamp, 0, total);
            foreach(BaseCell c in candidateNeighbors)
            {
                int idx = c.Row * WinFormsSettings.SudokuSize + c.Col;
                stamp[idx] = 1;
            }

            List<BaseCell> commonNeighbors = new List<BaseCell>();
            foreach(BaseCell cell in neighborCells)
            {
                if(cell == this || cell.CellValue != Values.Undefined) continue;
                int idx = cell.Row * WinFormsSettings.SudokuSize + cell.Col;
                if(stamp[idx] == 0) commonNeighbors.Add(cell);
            }

            return commonNeighbors;
        }
        finally
        {
            intPool2.Return(stamp, true);
        }
    }

    public bool CommonNeighbor(BaseCell neighbor) { bool common = false; foreach(BaseCell cell in Neighbors) common = (cell == neighbor || common); return common; }
    public bool SameRectangle(BaseCell value) { return (Col >= value.StartCol && Col < value.StartCol + WinFormsSettings.RectSize && Row >= value.StartRow && Row < value.StartRow + WinFormsSettings.RectSize); }

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
internal class NeighborCountComparer: IComparer<BaseCell>
{
    public int Compare(BaseCell x, BaseCell y)
    {
        if(x == null || y == null) return 0;

        // Absteigend sortieren (Meiste Nachbarn zuerst)
        return y.FilledNeighborCount.CompareTo(x.FilledNeighborCount);
    }
}
