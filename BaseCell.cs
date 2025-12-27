using System;
using System.Collections.Generic;
using System.IO;

namespace Sudoku
{
    [Serializable]
    internal abstract class BaseCell : EventArgs, IComparable
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
        [ThreadStatic] private static bool[] threadCommonLookup;
        [ThreadStatic] private static List<int> threadCommonIndices;

        public abstract bool Up();
        public abstract bool Down();

        public static bool operator ==(BaseCell op1, BaseCell op2)
        {
            if (op1 is null || op2 is null) return false;
            return op1.Row == op2.Row && op1.Col == op2.Col;
        }

        public static bool operator !=(BaseCell op1, BaseCell op2) => !(op1 == op2);
        public static bool operator >(BaseCell op1, BaseCell op2) => op1.GetHashCode() > op2.GetHashCode();
        public static bool operator <(BaseCell op1, BaseCell op2) => op1.GetHashCode() < op2.GetHashCode();

        public override bool Equals(object obj) => this == (BaseCell)obj;
        public override int GetHashCode() => Row * SudokuForm.SudokuSize + Col;

        public byte CellValue
        {
            get => coreValue.CellValue;
            set
            {
                if (CellValue == value) return;
                if (value != Values.Undefined)
                {
                    DefinitiveValue = Values.Undefined;
                    if (!Enabled(value)) throw new ArgumentException("value not possible", "value");
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
                if (DefinitiveValue == value) return;
                SetBlocks(DefinitiveValue, value, false);
                definitiveValue = value;
            }
        }

        public int Row { get => coreValue.Row; set { coreValue.Row = value; startRow = (int)Math.Truncate((double)value / SudokuForm.RectSize) * SudokuForm.RectSize; } }
        public int Col { get => coreValue.Col; set { coreValue.Col = value; startCol = (int)Math.Truncate((double)value / SudokuForm.RectSize) * SudokuForm.RectSize; } }
        public int StartRow => startRow;
        public int StartCol => startCol;
        public BaseCell[] Neighbors => neighbors;

        public int nPossibleValues => (FixedValue || DefinitiveValue != Values.Undefined) ? 0 : possibleValuesCount - 1;
        public bool FixedValue { get => fixedValue; set => fixedValue = value; }
        public bool ReadOnly { get => readOnly; set => readOnly = value; }
        public bool ComputedValue { get => computedValue; set => computedValue = value; }

        public BaseCell() { }
        public BaseCell(int row, int col) { Row = row; Col = col; Init(); }

        public void AddNeighbor(ref BaseCell neighbor) { neighbors[nNeighbors++] = neighbor; }

        public int CompareTo(object obj)
        {
            if (obj == null) return -1;
            BaseCell tmpObj = obj as BaseCell;
            if (tmpObj == null) throw new ArgumentException(obj.ToString());
            if (FixedValue) return int.MaxValue;
            if (tmpObj.FixedValue) return int.MinValue;
            return ((nPossibleValues * SudokuForm.TotalCellCount + Row * SudokuForm.SudokuSize + Col) - (tmpObj.nPossibleValues * SudokuForm.TotalCellCount + tmpObj.Row * SudokuForm.SudokuSize + tmpObj.Col));
        }

        public bool Enabled(int value)
        {
            if (value < 1 || value > SudokuForm.SudokuSize) return false;
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
        }

        public void InitCandidates() { candidatesMask = 0; exclusionCandidatesMask = 0; enabledMaskInitialized = false; }

        private void EnsureEnabledMaskInitialized()
        {
            if (enabledMaskInitialized) return;
            enabledMask = 0;
            for (int i = 1; i <= SudokuForm.SudokuSize; i++) if (directBlocks[i] == 0 && indirectBlocks[i] == 0) enabledMask |= (1 << i);
            enabledMaskInitialized = true;
        }

        public void InitIndirectBlocks()
        {
            indirectBlocks = new int[SudokuForm.SudokuSize + 1];
            possibleValuesCount = directBlocks.Length;
            enabledMask = 0;
            for (int i = 1; i <= SudokuForm.SudokuSize; i++)
            {
                if (directBlocks[i] == 0 && indirectBlocks[i] == 0) enabledMask |= (1 << i);
                else possibleValuesCount--;
            }
            enabledMaskInitialized = true;
            definitiveValue = Values.Undefined;
        }

        private void InitDirectBlocks() { directBlocks = new int[SudokuForm.SudokuSize + 1]; }

        private static void EnsureLowbitIndex()
        {
            if (lowbitIndex != null) return;
            int size = 1 << 10;
            lowbitIndex = new int[size];
            for (int i = 0; i < size; i++) lowbitIndex[i] = -1;
            for (int b = 0; b < 10; b++) lowbitIndex[1 << b] = b;
        }

        private static int LowBitIndex(int lowbit)
        {
            EnsureLowbitIndex();
            if (lowbit > 0 && lowbit < lowbitIndex.Length) return lowbitIndex[lowbit];
            int idx = 0; while (lowbit > 1) { lowbit >>= 1; idx++; } return idx;
        }

        private static int PopCount(int v)
        {
            if (popcountCache == null)
            {
                int size = 1 << 10;
                popcountCache = new byte[size];
                for (int i = 0; i < size; i++) { int tmp = i; byte c = 0; while (tmp != 0) { tmp &= (tmp - 1); c++; } popcountCache[i] = c; }
            }
            if (v >= 0 && v < popcountCache.Length) return popcountCache[v];
            int t = v; int cnt = 0; while (t != 0) { t &= (t - 1); cnt++; } return cnt;
        }

        private byte GetDefiniteValue()
        {
            if (DefinitiveValue != Values.Undefined) return DefinitiveValue;
            bool found = false; byte dv = Values.Undefined;
            for (byte possibleValue = 1; possibleValue < SudokuForm.SudokuSize + 1; possibleValue++)
                if (Enabled(possibleValue) && nPossibleValues == 1)
                {
                    if (found) return Values.Undefined; found = true; dv = possibleValue;
                }
            return dv;
        }

        public void FillDefiniteValue() { if ((DefinitiveValue = GetDefiniteValue()) == Values.Undefined) throw new InvalidSudokuValueException(); }

        private void SetBlocks(byte oldValue, byte newValue, bool direct)
        {
            if (oldValue != Values.Undefined)
            {
                if (direct) SetBlock(oldValue, true, direct);
                else for (int i = 1; i < SudokuForm.SudokuSize + 1; i++) SetBlock(i, true, direct);
                EnableNeighbors(oldValue, direct);
            }
            if (newValue != Values.Undefined)
            {
                if (direct) SetBlock(newValue, false, direct);
                else for (int i = 1; i < SudokuForm.SudokuSize + 1; i++) SetBlock(i, false, direct);
                DisableNeighbors(newValue, direct);
            }
        }

        private void EnableNeighbors(byte value, bool direct) { SetNeighborBlocks(value, true, direct); }
        private void DisableNeighbors(byte value, bool direct) { SetNeighborBlocks(value, false, direct); }
        private void SetNeighborBlocks(byte newValue, bool enable, bool direct) { foreach (BaseCell neighbor in neighbors) neighbor.SetBlock(newValue, enable, direct); }

        public void SetBlock(int value, bool enable, bool direct) { EnsureEnabledMaskInitialized(); SetBlockInternal(value, enable, direct); }

        public event EventHandler PossibleValuesChanged;

        private void SetBlockInternal(int value, bool enable, bool direct)
        {
            int prevPossible = nPossibleValues;
            int bit = 1 << value;
            bool beforeEnabled = (enabledMask & bit) != 0;
            if (enable)
            {
                if (direct) { if (--directBlocks[value] < 0) throw new ArgumentException("enable not possible", "enable"); }
                else { if (--indirectBlocks[value] < 0) throw new ArgumentException("enable not possible", "enable"); }
                if ((directBlocks[value] == 0 && indirectBlocks[value] == 0)) possibleValuesCount++;
            }
            else
            {
                if ((directBlocks[value] == 0 && indirectBlocks[value] == 0)) possibleValuesCount--;
                if (direct) directBlocks[value]++; else indirectBlocks[value]++;
            }
            bool afterEnabled = (directBlocks[value] == 0 && indirectBlocks[value] == 0);
            if (beforeEnabled != afterEnabled) { if (afterEnabled) enabledMask |= bit; else enabledMask &= ~bit; }
            int newPossible = nPossibleValues;
            if (prevPossible != newPossible) PossibleValuesChanged?.Invoke(this, EventArgs.Empty);
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
            while (m != 0)
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

        public bool GetCandidateMask(int candidate, bool exclusionCandidate)
        {
            if (candidate < 1 || candidate > SudokuForm.SudokuSize) return false;
            int bit = 1 << candidate;
            if (exclusionCandidate) return (exclusionCandidatesMask & bit) != 0;
            return (candidatesMask & bit) != 0;
        }

        public void ToggleCandidateMask(int candidate, bool exclusionCandidate)
        {
            if (candidate < 1 || candidate > SudokuForm.SudokuSize) throw new ArgumentOutOfRangeException(nameof(candidate));
            int bit = 1 << candidate;
            if (exclusionCandidate) exclusionCandidatesMask ^= bit; else candidatesMask ^= bit;
        }

        private bool Change(int allowedMask) { return (GetEnabledMask() & allowedMask) != 0; }

        public int FindNakedCells(BaseCell[] neighborCells) { if (FindNakedCombination(neighborCells)) return nPossibleValues * 2; return -1; }

        private bool FindNakedCombination(BaseCell[] neighborCells)
        {
            bool rc = false;
            if (CellValue == Values.Undefined && nPossibleValues > 1 && nPossibleValues < 8)
            {
                int count = nPossibleValues;
                int allowedMask = 0;
                int emask = GetEnabledMask();
                for (int v = 1; v <= SudokuForm.SudokuSize; v++) if ((emask & (1 << v)) != 0) allowedMask |= (1 << v);
                if (allowedMask == 0) return false;

                int nlen = neighborCells.Length;
                int[] neighborMasks = new int[nlen];
                byte[] neighborCounts = new byte[nlen];
                for (int ni = 0; ni < nlen; ni++)
                {
                    var nc = neighborCells[ni];
                    if (nc.CellValue == Values.Undefined) { int nm = nc.GetEnabledMask(); neighborMasks[ni] = nm; neighborCounts[ni] = (byte)PopCount(nm); }
                    else { neighborMasks[ni] = 0; neighborCounts[ni] = 0; }
                }

                BaseCell[] candidateNeighborsArr = new BaseCell[SudokuForm.SudokuSize];
                int candidateCount = 0;
                for (int ni = 0; ni < nlen; ni++) if (neighborCounts[ni] > 0)
                {
                    int neighborMask = neighborMasks[ni];
                    if (neighborCounts[ni] <= count && (neighborMask & ~allowedMask) == 0) candidateNeighborsArr[candidateCount++] = neighborCells[ni];
                }

                if (candidateCount == count && candidateCount > 0)
                {
                    int total = SudokuForm.TotalCellCount;
                    if (threadCommonLookup == null || threadCommonLookup.Length < total) { threadCommonLookup = new bool[total]; threadCommonIndices = new List<int>(32); }
                    threadCommonIndices.Clear();

                    for (int ci = 0; ci < candidateCount; ci++) { var c = candidateNeighborsArr[ci]; int idx = c.Row * SudokuForm.SudokuSize + c.Col; if (!threadCommonLookup[idx]) { threadCommonLookup[idx] = true; threadCommonIndices.Add(idx); } }

                    for (int ni = 0; ni < nlen; ni++)
                    {
                        BaseCell updateCell = neighborCells[ni];
                        if (updateCell == this || updateCell.CellValue != Values.Undefined) continue;
                        int uidx = updateCell.Row * SudokuForm.SudokuSize + updateCell.Col;
                        if (threadCommonLookup[uidx]) continue;
                        if (updateCell.TryDisableMask(allowedMask, false)) rc = true;
                    }

                    for (int i = 0; i < threadCommonIndices.Count; i++) threadCommonLookup[threadCommonIndices[i]] = false;
                    threadCommonIndices.Clear();
                }
            }
            return rc;
        }

        protected virtual List<BaseCell> GetCommonNeighbors(List<BaseCell> candidateNeighbors, BaseCell[] neighborCells)
        {
            int total = SudokuForm.TotalCellCount;
            if (threadCommonLookup == null || threadCommonLookup.Length < total) { threadCommonLookup = new bool[total]; threadCommonIndices = new List<int>(32); }
            threadCommonIndices.Clear();
            foreach (BaseCell c in candidateNeighbors) { int idx = c.Row * SudokuForm.SudokuSize + c.Col; if (!threadCommonLookup[idx]) { threadCommonLookup[idx] = true; threadCommonIndices.Add(idx); } }
            List<BaseCell> commonNeighbors = new List<BaseCell>();
            foreach (BaseCell cell in neighborCells) if (cell != this && cell.CellValue == Values.Undefined && !threadCommonLookup[cell.Row * SudokuForm.SudokuSize + cell.Col]) commonNeighbors.Add(cell);
            for (int i = 0; i < threadCommonIndices.Count; i++) threadCommonLookup[threadCommonIndices[i]] = false;
            threadCommonIndices.Clear();
            return commonNeighbors;
        }

        public bool CommonNeighbor(BaseCell neighbor) { bool common = false; foreach (BaseCell cell in Neighbors) common = (cell == neighbor || common); return common; }
        public bool SameRectangle(BaseCell value) { return (Col >= value.StartCol && Col < value.StartCol + SudokuForm.RectSize && Row >= value.StartRow && Row < value.StartRow + SudokuForm.RectSize); }
    }
}