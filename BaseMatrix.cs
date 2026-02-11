using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace Sudoku;

[Serializable]
internal abstract class BaseMatrix: Values
{
    protected BaseCell[][] matrix;
    protected BaseCell[][] cols;
    protected BaseCell[][] rectangles;
    private List<BaseCell> sortableValues;
    private List<BaseCell> cells;
    private Boolean sorted = false;
    private int nVarValues = 0;
    protected float severityLevel = float.NaN;
    private int definitiveCalculatorCounter = 0;
    private Boolean setPredefinedValues = true;

    [ThreadStatic]
    private static int[] memberStamp;
    [ThreadStatic]
    private static int memberStampId;
    [ThreadStatic]
    private static BaseCell[] isolatedBuffer;
    [ThreadStatic]
    private static int[] isolatedEnabledCounts;
    [ThreadStatic]
    private static int[] isolatedCandidateIndex;

    public event EventHandler<BaseCell> CellChanged;
    protected virtual void OnCellChanged(BaseCell v)
    {
        EventHandler<BaseCell> handler = CellChanged;
        if(handler != null) handler(this, v);
    }

    public BaseMatrix()
    {
        InitializeMatrix();
    }

    protected void InitializeMatrix()
    {
        int size = WinFormsSettings.SudokuSize;
        int rectSize = WinFormsSettings.RectSize;
        Matrix = new BaseCell[size][];
        Cols = new BaseCell[size][];
        Rectangles = new BaseCell[size][];
        sortableValues = new List<BaseCell>(size * size);
        cells = new List<BaseCell>(size * size);
        nVarValues = int.MinValue; // not initialized
        severityLevel = float.NaN;

        for(int index = 0; index < size; index++)
        {
            Matrix[index] = new BaseCell[size];
            Cols[index] = new BaseCell[size];
            Rectangles[index] = new BaseCell[size];
        }

        for(int row = 0; row < size; row++)
        {
            BaseCell[] rowCells = Matrix[row];
            for(int col = 0; col < size; col++)
            {
                BaseCell cell = CreateValue(row, col);
                rowCells[col] = cell;
                Cols[col][row] = cell;
                int rectRow = row / rectSize;
                int rectCol = col / rectSize;
                int rectIndex = rectRow * rectSize + rectCol;
                int rectOffset = (row % rectSize) * rectSize + (col % rectSize);
                Rectangles[rectIndex][rectOffset] = cell;
            }
        }

		for(int row = 0; row < size; row++)
		{
			BaseCell[] rowCells = Matrix[row];
			for(int col = 0; col < size; col++)
			{
				BaseCell cell = rowCells[col];
				sortableValues.Add(cell);
				cells.Add(cell);
			}
		}

		// Verbinde Zeilen-Nachbarn einmalig paarweise
		for(int row = 0; row < size; row++)
		{
			BaseCell[] rowCells = Matrix[row];
			for(int i = 0; i < size - 1; i++)
			{
				for(int j = i + 1; j < size; j++)
				{
					rowCells[i].AddNeighbor(ref rowCells[j]);
					rowCells[j].AddNeighbor(ref rowCells[i]);
				}
			}
		}

		// Verbinde Spalten-Nachbarn einmalig paarweise
		for(int col = 0; col < size; col++)
		{
			BaseCell[] columnCells = Cols[col];
			for(int i = 0; i < size - 1; i++)
			{
				for(int j = i + 1; j < size; j++)
				{
					columnCells[i].AddNeighbor(ref columnCells[j]);
					columnCells[j].AddNeighbor(ref columnCells[i]);
				}
			}
		}

		// Verbinde Block-Nachbarn (ohne Zeilen/Spalten-Duplikate)
		for(int rect = 0; rect < Rectangles.Length; rect++)
		{
			BaseCell[] rectCells = Rectangles[rect];
			for(int i = 0; i < rectCells.Length - 1; i++)
			{
				BaseCell cellA = rectCells[i];
				for(int j = i + 1; j < rectCells.Length; j++)
				{
					BaseCell cellB = rectCells[j];
					if(cellA.Row == cellB.Row || cellA.Col == cellB.Col) continue;
					rectCells[i].AddNeighbor(ref rectCells[j]);
					rectCells[j].AddNeighbor(ref rectCells[i]);
				}
			}
		}

		for(int row = 0; row < size; row++)
		{
			BaseCell[] rowCells = Matrix[row];
			for(int col = 0; col < size; col++)
			{
				rowCells[col].Init();
			}
		}
    }
    public override BaseMatrix Clone()
    {
        BaseMatrix clonedMatrix = (BaseMatrix)Activator.CreateInstance(this.GetType());

        clonedMatrix.sorted = this.sorted;
        clonedMatrix.nVarValues = this.nVarValues;
        clonedMatrix.severityLevel = this.severityLevel;
        clonedMatrix.definitiveCalculatorCounter = this.definitiveCalculatorCounter;
        clonedMatrix.setPredefinedValues = this.setPredefinedValues;

        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
        {
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                this.Matrix[row][col].CopyTo(clonedMatrix.Matrix[row][col]);
            }
        }

        return clonedMatrix;
    }
    public abstract BaseCell CreateValue(int row, int col);

    public IEnumerator GetEnumerator()
    {
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                yield return Cell(row, col);
    }

    public BaseCell[][] Matrix
    {
        set { matrix = value; }
        get { return matrix; }
    }

    public BaseCell[][] Rows
    {
        set { matrix = value; }
        get { return matrix; }
    }

    public BaseCell[][] Cols
    {
        set { cols = value; }
        get { return cols; }
    }

    public BaseCell[][] Rectangles
    {
        set { rectangles = value; }
        get { return rectangles; }
    }

    public List<BaseCell> Cells
    {
        get { return cells; }
    }

    public int nValues
    {
        get
        {
            int nVal = 0;
            foreach(BaseCell cell in this)
                if(cell.FixedValue) nVal++;
            return nVal;
        }
    }

    public int nComputedValues
    {
        get
        {
            int nVal = 0;
            foreach(BaseCell cell in this)
                if(cell.ComputedValue) nVal++;
            return nVal;
        }
    }

    public virtual int MinimumValues
    {
        get { return 17; }
    }

    public int nVariableValues
    {
        get { return nVarValues; }
    }

    public Boolean SetPredefinedValues
    {
        get { return setPredefinedValues; }
        set
        {
            setPredefinedValues = value;
            if(setPredefinedValues) SearchDefiniteValues(true);
        }
    }

    public Boolean GetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        BaseCell c = Cell(row, col);
        return c.GetCandidateMask(candidate, exclusionCandidate);
    }

    public void SetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        BaseCell c = Cell(row, col);
        c.ToggleCandidateMask(candidate, exclusionCandidate);
    }

    public Boolean HasCandidates()
    {
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                for(int candidate = 1; candidate < WinFormsSettings.SudokuSize + 1; candidate++)
                    if(Cell(row, col).GetCandidateMask(candidate, false) || Cell(row, col).GetCandidateMask(candidate, true)) return true;

        return false;
    }

    public Boolean HasCandidate(int row, int col)
    {
        return Cell(row, col).HasCandidate();
    }

    public override void SetValue(int row, int col, byte value, Boolean fixedValue)
    {
        if(((value < 1 || value > WinFormsSettings.SudokuSize) && value != Values.Undefined) || row < 0 || col < 0 || row > WinFormsSettings.SudokuSize || col > WinFormsSettings.SudokuSize)
            throw new InvalidSudokuValueException();

        if(Cell(row, col).FixedValue != fixedValue)
            nVarValues = fixedValue ? nVarValues - 1 : nVarValues + 1;

        Cell(row, col).FixedValue = fixedValue;
        Cell(row, col).ComputedValue = false;
        if(GetValue(row, col) != value)
        {
            lock(this)
            {
                if(SetPredefinedValues && value == Values.Undefined) ResetIndirectBlocks();
                Cell(row, col).CellValue = value;
                if(SetPredefinedValues) SearchDefiniteValues(true);
            }

            severityLevel = float.NaN;
            OnCellChanged(Cell(row, col));
        }
    }

    public override byte GetValue(int row, int col)
    {
        return Cell(row, col).CellValue;
    }

    public override Boolean FixedValue(int row, int col)
    {
        return Cell(row, col).FixedValue;
    }

    public override Boolean ComputedValue(int row, int col)
    {
        return Cell(row, col).ComputedValue;
    }

    public override Boolean ReadOnly(int row, int col)
    {
        return Cell(row, col).ReadOnly;
    }

    public override void Init()
    {
        nVarValues = int.MinValue; // not initialized
        severityLevel = float.NaN;

        foreach(BaseCell cell in this)
            cell.Init();
    }

    public BaseCell Cell(int row, int col)
    {
        return Matrix[row][col];
    }

    public void ResetCandidates()
    {
        foreach(BaseCell cell in this)
            cell.InitCandidates();
    }

    public void ResetCandidates(int row, int col)
    {
        Cell(row, col).InitCandidates();
    }

    public void Reset()
    {
        SetPredefinedValues = false;
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                if(!FixedValue(row, col) || ComputedValue(row, col))
                    SetValue(row, col, Values.Undefined, false);
                else
                    Cell(row, col).ReadOnly = true;
        ResetIndirectBlocks();
        SetPredefinedValues = true;
    }

    private void ResetIndirectBlocks()
    {
        foreach(BaseCell cell in this)
            cell.InitIndirectBlocks();
        definitiveCalculatorCounter = 0;
    }

    public void Prepare()
    {
        SetDefiniteValues();
        sortableValues.Sort();
        sorted = true;
        nVarValues = (WinFormsSettings.TotalCellCount) - nValues;
    }

    private void SetDefiniteValues()
    {
        SearchDefiniteValues(true);

        foreach(BaseCell cell in this)
            if(cell.DefinitiveValue != Values.Undefined)
            {
                Byte definitiveValue = cell.DefinitiveValue;
                cell.DefinitiveValue = Values.Undefined;
                cell.CellValue = definitiveValue;
                OnCellChanged(cell);
                if(!cell.FixedValue) nVarValues--;
                cell.FixedValue = true;
                cell.ComputedValue = true;
            }
    }

    public List<BaseCell> GetHints(Boolean deep)
    {
        List<BaseCell> values = new List<BaseCell>();

        SearchDefiniteValues(deep);

        foreach(BaseCell cell in this)
            if(cell.DefinitiveValue != Values.Undefined)
                values.Add(cell);

        return values;
    }

    [ThreadStatic]
    private static List<BaseCell> obviousBuffer;

    public List<BaseCell> GetObviousCells(Boolean reset)
    {
        List<BaseCell> values = GetObviousCellsPooled(reset);
        var copy = new List<BaseCell>(values.Count);
        copy.AddRange(values);
        return copy;
    }

    private List<BaseCell> GetObviousCellsPooled(Boolean reset)
    {
        if(reset) ResetIndirectBlocks();

        if(obviousBuffer == null)
            obviousBuffer = new List<BaseCell>(WinFormsSettings.TotalCellCount);
        else
            obviousBuffer.Clear();

        for(int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if(cell.nPossibleValues == 1) obviousBuffer.Add(cell);
        }
        return obviousBuffer;
    }

    private Boolean FillObviousCells(Boolean reset)
    {
        List<BaseCell> values = GetObviousCellsPooled(reset);
        Boolean rc = values.Count > 0;

        while(values.Count > 0)
        {
            for(int i = 0; i < values.Count; i++)
                if(values[i].nPossibleValues == 1) values[i].FillDefiniteValue();
            values = GetObviousCellsPooled(reset);
        }
        return rc;
    }

    internal bool CallFillObviousCells(bool reset)
    {
        return FillObviousCells(reset);
    }

    private void SearchDefiniteValues(Boolean deep)
    {
        Boolean found = false;

        do
        {
            definitiveCalculatorCounter++;
            found = FillObviousCells(false);

            if(!found || deep)
                for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
                {
                    found |= HandleIsolatedCells(Rows[i]);
                    found |= HandleIsolatedCells(Cols[i]);
                    found |= HandleIsolatedCells(Rectangles[i]);
                    found |= HandleNakedCells(Rows[i]);
                    found |= HandleNakedCells(Cols[i]);
                    found |= HandleNakedCells(Rectangles[i]);
                }

            if(this is XSudokuMatrix && (!found || deep))
            {
                found |= HandleNakedCells(GetDiagonal(SudokuPart.DownDiagonal));
                found |= HandleIsolatedCells(GetDiagonal(SudokuPart.DownDiagonal));
                found |= HandleNakedCells(GetDiagonal(SudokuPart.UpDiagonal));
                found |= HandleIsolatedCells(GetDiagonal(SudokuPart.UpDiagonal));
            }
        } while(found && deep);
    }

    private Boolean HandleNakedCells(BaseCell[] part)
    {
        if(FillObviousCells(false)) return true;

        if(part == null || part.Length == 0) return false;

        int counterIncrease = 0;
        BaseCell.NakedScratch scratch = default;
		try
		{
			for(int i = 0; i < part.Length; i++)
			{
				var cell = part[i];
				if(cell == null) continue;
				if(cell.CellValue != Values.Undefined) continue;
				int possible = cell.nPossibleValues;
				if(possible <= 1 || possible >= WinFormsSettings.SudokuSize - 1) continue;
				int nakedScore = cell.FindNakedCells(part, ref scratch);
				if(nakedScore > counterIncrease)
					counterIncrease = nakedScore;
			}
		}
        finally
        {
            scratch.Release();
        }
        definitiveCalculatorCounter += counterIncrease;
        return counterIncrease > 0;
    }

    private Boolean HandleIsolatedCells(BaseCell[] part)
    {
        if(FillObviousCells(false)) return true;

        if(part == null || part.Length == 0) return false;

        Boolean rc = false;
        int size = WinFormsSettings.SudokuSize;
        int plen = part.Length;

		int bufferLength = size * plen;
		BaseCell[] buffer = isolatedBuffer;
		if(buffer == null || buffer.Length < bufferLength)
		{
			buffer = new BaseCell[bufferLength];
			isolatedBuffer = buffer;
		}
		int[] enabledCounts = isolatedEnabledCounts;
		if(enabledCounts == null || enabledCounts.Length < size)
		{
			enabledCounts = new int[size];
			isolatedEnabledCounts = enabledCounts;
		}
		int[] usedCandidates = isolatedCandidateIndex;
		if(usedCandidates == null || usedCandidates.Length < size)
		{
			usedCandidates = new int[size];
			isolatedCandidateIndex = usedCandidates;
		}
		int usedCandidateCount = 0;
		int maxBufferIndex = 0;

        try
        {
            for(int pi = 0; pi < plen; pi++)
            {
                BaseCell cell = part[pi];
                if(cell.nPossibleValues <= 0) continue;

                int mask = cell.GetEnabledMask();
                while(mask != 0)
                {
                    int lowbit = mask & -mask;
                    int cand = BaseCell.LowBitIndex(lowbit);

				if(cand >= 1 && cand <= size)
                    {
                        int idx = cand - 1;
					if(enabledCounts[idx] == 0)
						usedCandidates[usedCandidateCount++] = idx;
                        int pos = idx * plen + enabledCounts[idx]++;
                        buffer[pos] = cell;
                        if(pos >= maxBufferIndex) maxBufferIndex = pos + 1;
                    }
                    mask &= (mask - 1);
                }
            }

			for(int i = 0; i < usedCandidateCount; i++)
			{
				int candidateIdx = usedCandidates[i];
				int count = enabledCounts[candidateIdx];
				if(count == 0 || count == plen)
				{
					enabledCounts[candidateIdx] = 0;
					continue;
				}
				if(BlockOtherCellsArray(buffer, candidateIdx * plen, count, candidateIdx + 1))
					rc = true;
				enabledCounts[candidateIdx] = 0;
			}
        }
		finally
		{
			if(maxBufferIndex > 0) Array.Clear(buffer, 0, maxBufferIndex);
			for(int i = 0; i < usedCandidateCount; i++) enabledCounts[usedCandidates[i]] = 0;
        }

        return rc;
    }

    private Boolean BlockOtherCellsArray(BaseCell[] enabledCellsArr, int offset, int count, int block)
    {
        Boolean rc = false;
        Boolean definitive = count == 1;

        BaseCell first = enabledCellsArr[offset];
        if(definitive)
        {
			rc = first.DefinitiveValue == Values.Undefined; 
            first.DefinitiveValue = (byte)block;
        }

		int rectSize = WinFormsSettings.RectSize;
		var rows = Rows;
		var cols = Cols;
		var rectangles = Rectangles;

        int baseRow = first.Row;
        int baseCol = first.Col;
        int firstRectRow = first.StartRow;
        int firstRectCol = first.StartCol / rectSize;
        int baseRectIndex = firstRectRow + (firstRectCol % rectSize);
		int baseRectStartRow = first.StartRow;
		int baseRectStartCol = first.StartCol;

        bool allSameRow = true;
        bool allSameCol = true;
        bool allSameRect = true;

		for(int i = 1; i < count; i++)
		{
			var c = enabledCellsArr[offset + i];
			if(c.Row != baseRow) allSameRow = false;
			if(c.Col != baseCol) allSameCol = false;
			if(c.StartRow != baseRectStartRow || c.StartCol != baseRectStartCol) allSameRect = false;
			if(!allSameRow && !allSameCol && !allSameRect) break;
		}

        bool needRow = allSameRow;
        bool needCol = !definitive && allSameCol;
        bool needRect = !definitive && allSameRect;
        if(!needRow && !needCol && !needRect)
            return rc;

        ulong rowMask = 0UL;
        ulong colMask = 0UL;
        ulong rectMask = 0UL;
		for(int i = 0; i < count; i++)
		{
			var c = enabledCellsArr[offset + i];
			if(needRow) rowMask |= 1UL << c.Col;
			if(needCol) colMask |= 1UL << c.Row;
			if(needRect)
			{
				int localRow = c.Row - baseRectStartRow;
				int localCol = c.Col - baseRectStartCol;
				if((uint)localRow < (uint)rectSize && (uint)localCol < (uint)rectSize)
				{
					int localIndex = localRow * rectSize + localCol;
					rectMask |= 1UL << localIndex;
				}
			}
		}

        if(needRow)
        {
            var neighborCells = rows[baseRow];
            for(int i = 0; i < neighborCells.Length; i++)
            {
                var cell = neighborCells[i];
                if(((rowMask >> cell.Col) & 1UL) == 0)
                    rc |= cell.TrySetBlock(block, false, false);
            }
        }

        if(needCol)
        {
            var neighborCells = cols[baseCol];
            for(int i = 0; i < neighborCells.Length; i++)
            {
                var cell = neighborCells[i];
                if(((colMask >> cell.Row) & 1UL) == 0)
                    rc |= cell.TrySetBlock(block, false, false);
            }
        }

		if(needRect)
        {
            var neighborCells = rectangles[baseRectIndex];
            for(int i = 0; i < neighborCells.Length; i++)
            {
                var cell = neighborCells[i];
                int localRow = cell.Row - baseRectStartRow;
                int localCol = cell.Col - baseRectStartCol;
				if((uint)localRow >= (uint)rectSize || (uint)localCol >= (uint)rectSize)
					continue;
				int localIndex = localRow * rectSize + localCol;
				if(((rectMask >> localIndex) & 1UL) == 0)
                    rc |= cell.TrySetBlock(block, false, false);
            }
        }

        return rc;
    }

    private Boolean BlockOtherCellsArray(BaseCell[] enabledCellsArr, int count, int block)
    {
        return BlockOtherCellsArray(enabledCellsArr, 0, count, block);
    }

    protected virtual Boolean BlockOtherCells(List<BaseCell> enabledCells, int block)
    {
        if(enabledCells == null) return false;
        int count = enabledCells.Count;
        if(count == 0) return false;

        var pool = ArrayPool<BaseCell>.Shared;
        BaseCell[] arr = pool.Rent(count);
        try
        {
            for(int i = 0; i < count; i++) arr[i] = enabledCells[i];
            return BlockOtherCellsArray(arr, count, block);
        }
        finally
        {
            for(int i = 0; i < count; i++) arr[i] = null;
            pool.Return(arr, false);
        }
    }

    internal bool CallBlockOtherCells(List<BaseCell> enabledCells, int block)
    {
        return BlockOtherCells(enabledCells, block);
    }

    internal bool CallHandleIsolatedCells(BaseCell[] part)
    {
        return HandleIsolatedCells(part);
    }

    public BaseCell Get(int current)
    {
        if(!sorted)
        {
            sortableValues.Sort();
            sorted = true;
        }
        return (BaseCell)sortableValues[current];
    }

    protected abstract BaseCell[] GetDiagonal(SudokuPart direction);

    public Boolean Check(BaseCell[] values)
    {
        Boolean checkCurrentValue = true;
        Boolean valueIsPossible = true;
        int currentValue = 0;
        int i = 0;

        for(currentValue = 1; currentValue < WinFormsSettings.SudokuSize + 1; currentValue++)
        {
            i = 0;
            checkCurrentValue = true;
            while(i < WinFormsSettings.SudokuSize && checkCurrentValue)
                checkCurrentValue = (values[i++].CellValue != currentValue);
            if(checkCurrentValue)
            {
                i = 0;
                valueIsPossible = false;
                while(i < WinFormsSettings.SudokuSize && !valueIsPossible)
                {
                    valueIsPossible = ((!values[i].FixedValue && values[i].Enabled(currentValue)) || values[i].DefinitiveValue == currentValue);
                    i++;
                }
                if(!valueIsPossible)
                    return false;
            }
        }
        return true;
    }

    public virtual float SeverityLevel
    {
        get
        {
            if(nValues < MinimumValues) return float.NaN;

            if(float.IsNaN(severityLevel))
            {
                int totalComplexity = 0;
                int minValuesRow = WinFormsSettings.SudokuSize;
                int minValuesCol = WinFormsSettings.SudokuSize;
                int minValuesRect = WinFormsSettings.SudokuSize;
                int maxValuesRow = 0;
                int maxValuesCol = 0;
                int maxValuesRect = 0;
                byte minNumber = (byte)WinFormsSettings.SudokuSize;
                byte maxNumber = 0;
                byte[] digitCounter = new byte[WinFormsSettings.SudokuSize];

                if(definitiveCalculatorCounter == 0) SearchDefiniteValues(true);

                for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
                {
                    int nVal = WinFormsSettings.SudokuSize;
                    for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                    {
                        totalComplexity += Cell(row, col).nPossibleValues;
                        if(!Cell(row, col).FixedValue || Cell(row, col).ComputedValue)
                            nVal--;
                        else if(Cell(row, col).CellValue != Values.Undefined)
                            digitCounter[Cell(row, col).CellValue - 1]++;

                    }
                    minValuesRow = Math.Min(minValuesRow, nVal);
                    maxValuesRow = Math.Max(maxValuesRow, nVal);
                }

                for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                {
                    int nVal = WinFormsSettings.SudokuSize;
                    for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
                        if(!Cell(row, col).FixedValue || Cell(row, col).ComputedValue)
                            nVal--;
                    minValuesCol = Math.Min(minValuesCol, nVal);
                    maxValuesCol = Math.Max(maxValuesCol, nVal);
                }

                for(int row = 0; row < WinFormsSettings.SudokuSize; row += WinFormsSettings.RectSize)
                {
                    for(int col = 0; col < WinFormsSettings.SudokuSize; col += WinFormsSettings.RectSize)
                    {
                        int nVal = WinFormsSettings.SudokuSize;
                        for(int i = 0; i < WinFormsSettings.RectSize; i++)
                            for(int j = 0; j < WinFormsSettings.RectSize; j++)
                                if(!Matrix[row + i][col + j].FixedValue || Matrix[row + i][col + j].ComputedValue)
                                    nVal--;
                        minValuesRect = Math.Min(minValuesRect, nVal);
                        maxValuesRect = Math.Max(maxValuesRect, nVal);
                    }
                }

                for(int number = 0; number < WinFormsSettings.SudokuSize; number++)
                {
                    maxNumber = Math.Max(maxNumber, digitCounter[number]);
                    minNumber = Math.Min(minNumber, digitCounter[number]);
                }

                severityLevel = (float)((totalComplexity - (nValues - nComputedValues) + (maxValuesCol - minValuesCol) + (maxValuesRow - minValuesRow) + (maxValuesRect - minValuesRect) + (maxNumber - minNumber) * 2f + definitiveCalculatorCounter + 80f) / 3f);
            }
            return severityLevel;
        }
    }
}