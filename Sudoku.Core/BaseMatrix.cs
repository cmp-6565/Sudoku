#nullable enable
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace Sudoku.Core;

/// <summary>
/// Abstract base class representing the matrix/grid structure of a Sudoku puzzle.
/// Manages rows, columns, boxes (rectangles), and cell relationships.
/// Provides core functionality for constraint checking and value manipulation.
/// </summary>
[Serializable]
public abstract class BaseMatrix: Values
{
	/// <summary>
	/// The main matrix storing cells organized by rows.
	/// </summary>
	protected BaseCell[][]? cellMatrix;

	/// <summary>
	/// Cells organized by columns for efficient column-based access.
	/// </summary>
	protected BaseCell[][]? allCols;

	/// <summary>
	/// Cells organized by boxes/rectangles for 3x3 constraint checking.
	/// </summary>
	protected BaseCell[][]? allRectangles;

	private List<BaseCell> sortableValues = new List<BaseCell>();
	private List<BaseCell> cells = new List<BaseCell>();
	private Boolean sorted = false;
	private int nVarValues = 0;
	protected float internalSeverityLevel = float.NaN;
	private int definitiveCalculatorCounter = 0;
	private Boolean setPredefinedValues = true;

	// [ThreadStatic]
	// private static int[]? memberStamp;
	// [ThreadStatic]
	// private static int memberStampId;
	[ThreadStatic]
	private static BaseCell[]? isolatedBuffer;
	[ThreadStatic]
	private static int[]? isolatedEnabledCounts;
	[ThreadStatic]
	private static int[]? isolatedCandidateIndex;

	/// <summary>
	/// Gets the count of cells with definitive (computed) values.
	/// </summary>
	public int DefinitiveCellCount {get { return definitiveCalculatorCounter; }} 

	/// <summary>
	/// Event raised when a cell value changes in the matrix.
	/// </summary>
	public event EventHandler<BaseCell>? CellChanged;

	/// <summary>
	/// Raises the CellChanged event for the specified cell.
	/// </summary>
	/// <param name="v">The cell that changed.</param>
	protected virtual void OnCellChanged(BaseCell v)
	{
		EventHandler<BaseCell>? handler = CellChanged;
		if(handler != null) handler(this, v);
	}

	/// <summary>
	/// Initializes a new instance of the BaseMatrix class.
	/// </summary>
	public BaseMatrix()
	{
		InitializeMatrix();
	}

	/// <summary>
	/// Initializes the matrix grid structure with all cells and their relationships.
	/// </summary>
	protected void InitializeMatrix()
	{
		int size = SudokuGrid.SudokuSize;
		int rectSize = SudokuGrid.RectSize;
		Matrix = new BaseCell[size][];
		Cols = new BaseCell[size][];
		Rectangles = new BaseCell[size][];
		sortableValues = new List<BaseCell>(size * size);
		cells = new List<BaseCell>(size * size);
		nVarValues = int.MinValue; // not initialized
		internalSeverityLevel = float.NaN;

		for(int index = 0; index < size; index++)
		{
			Matrix[index] = new BaseCell[size];
			Cols[index] = new BaseCell[size];
			Rectangles[index] = new BaseCell[size];
		}

		for(int row = 0; row < size; row++)
		{
			BaseCell[] rowCells = Matrix[row]!;
			for(int col = 0; col < size; col++)
			{
				BaseCell cell = CreateValue(row, col);
				rowCells[col] = cell;
				Cols[col]![row] = cell;
				int rectRow = row / rectSize;
				int rectCol = col / rectSize;
				int rectIndex = rectRow * rectSize + rectCol;
				int rectOffset = (row % rectSize) * rectSize + (col % rectSize);
				Rectangles[rectIndex]![rectOffset] = cell;
			}
		}

		for(int row = 0; row < size; row++)
		{
			BaseCell[] rowCells = Matrix[row];
			for(int col = 0; col < size; col++)
			{
				BaseCell cell = rowCells[col];
				sortableValues!.Add(cell);
				cells!.Add(cell);
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
	
	/// <summary>
	/// Creates a deep clone of the matrix instance including per-cell state.
	/// </summary>
	/// <returns>A new <see cref="BaseMatrix"/> instance with copied state.</returns>
	public override BaseMatrix Clone()
	{
		BaseMatrix clonedMatrix = (BaseMatrix)Activator.CreateInstance(this.GetType())!;

		clonedMatrix.sorted = this.sorted;
		clonedMatrix.nVarValues = this.nVarValues;
		clonedMatrix.internalSeverityLevel = this.internalSeverityLevel;
		clonedMatrix.definitiveCalculatorCounter = this.definitiveCalculatorCounter;
		clonedMatrix.setPredefinedValues = this.setPredefinedValues;

		for(int row = 0; row < SudokuGrid.SudokuSize; row++)
		{
			for(int col = 0; col < SudokuGrid.SudokuSize; col++)
			{
				this.Matrix[row][col].CopyTo(clonedMatrix.Matrix[row][col]);
			}
		}

		return clonedMatrix;
	}

	/// <summary>
	/// Factory method to create a concrete <see cref="BaseCell"/> instance for the specified coordinates.
	/// Concrete matrix implementations must override to return the appropriate cell subclass.
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	/// <returns>A new <see cref="BaseCell"/> positioned at the specified coordinates.</returns>
	public abstract BaseCell CreateValue(int row, int col);

	/// <summary>
	/// Enumerates all cells in row-major order.
	/// </summary>
	/// <returns>An enumerator yielding each <see cref="BaseCell"/> in the matrix.</returns>
	public IEnumerator GetEnumerator()
	{
		for(int row = 0; row < SudokuGrid.SudokuSize; row++)
			for(int col = 0; col < SudokuGrid.SudokuSize; col++)
				yield return Cell(row, col);
	}

	/// <summary>
	/// Matrix property accessor (row-based).
	/// </summary>
	public BaseCell[][] Matrix
	{
		set => cellMatrix = value;
		get => cellMatrix!;
	}

	/// <summary>
	/// Rows property alias for the matrix.
	/// </summary>
	public BaseCell[][] Rows
	{
		set => cellMatrix = value;
		get => cellMatrix!;
	}

	/// <summary>
	/// Columns accessor for the matrix (column-major).
	/// </summary>
	public BaseCell[][] Cols
	{
		set { allCols = value; }
		get { return allCols!; }
	}

	/// <summary>
	/// Rectangles (boxes) accessor for the matrix.
	/// </summary>
	public BaseCell[][] Rectangles
	{
		set { allRectangles = value; }
		get { return allRectangles!; }
	}

	/// <summary>
	/// Returns the internal list of all cell instances in row-major order.
	/// </summary>
	public List<BaseCell> Cells
	{
		get { return cells ??= new List<BaseCell>(); }
	}

	/// <summary>
	/// Returns the number of fixed (given) values in the puzzle.
	/// </summary>
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

	/// <summary>
	/// Returns the number of cells currently holding the specified value.
	/// </summary>
	/// <param name="value">Value to count (1..SudokuSize).</param>
	/// <returns>Number of cells equal to the specified value.</returns>
	public int nCells(int value)
	{
		int nVal = 0;
		foreach(BaseCell cell in this)
			if(cell.CellValue == value) nVal++;
		return nVal;
	}

	/// <summary>
	/// Returns the number of values that were computed by the solver (not fixed).
	/// </summary>
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

	/// <summary>
	/// Minimum number of clues required for a valid puzzle. Subclasses may override.
	/// </summary>
	public virtual int MinimumValues
	{
		get { return 17; }
	}

	/// <summary>
	/// Number of variable (non-fixed) cells. May be uninitialized if matrix not prepared.
	/// </summary>
	public int nVariableValues
	{
		get { return nVarValues; }
	}

	/// <summary>
	/// When enabled, the solver will set and propagate predefined values (givens).
	/// </summary>
	public Boolean SetPredefinedValues
	{
		get { return setPredefinedValues; }
		set
		{
			setPredefinedValues = value;
			if(setPredefinedValues) SearchDefiniteValues(true);
		}
	}

	/// <summary>
	/// Returns whether the specified candidate is set on the cell at the given coordinates.
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	/// <param name="candidate">Candidate value to test.</param>
	/// <param name="exclusionCandidate">True to test exclusion-candidate mask, false to test normal candidate mask.</param>
	/// <returns>True when the candidate is present on the specified cell.</returns>
	public Boolean GetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
	{
		BaseCell c = Cell(row, col);
		return c.GetCandidateMask(candidate, exclusionCandidate);
	}

	/// <summary>
	/// Toggles a candidate mark on the specified cell.
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	/// <param name="candidate">Candidate value to toggle.</param>
	/// <param name="exclusionCandidate">True to toggle exclusion mask; false to toggle normal candidate mask.</param>
	public void SetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
	{
		BaseCell c = Cell(row, col);
		c.ToggleCandidateMask(candidate, exclusionCandidate);
	}

	/// <summary>
	/// Returns whether any candidate marks exist anywhere in the matrix.
	/// </summary>
	/// <returns>True if at least one candidate or exclusion-candidate is present.</returns>
	public Boolean HasCandidates()
	{
		for(int row = 0; row < SudokuGrid.SudokuSize; row++)
			for(int col = 0; col < SudokuGrid.SudokuSize; col++)
				for(int candidate = 1; candidate < SudokuGrid.SudokuSize + 1; candidate++)
					if(Cell(row, col).GetCandidateMask(candidate, false) || Cell(row, col).GetCandidateMask(candidate, true)) return true;

		return false;
	}

	/// <summary>
	/// Returns whether the specified cell currently has any candidates (normal or exclusion).
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	/// <returns>True if the cell has candidate marks.</returns>
	public Boolean HasCandidate(int row, int col)
	{
		return Cell(row, col).HasCandidate();
	}

	/// <summary>
	/// Sets the value of a cell and optionally marks it as fixed (given).
	/// This method validates indices and ranges, updates variable counters, and triggers propagation.
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	/// <param name="value">Value to set or <see cref="Values.Undefined"/> to clear.</param>
	/// <param name="fixedValue">If true, mark the value as a fixed given.</param>
	public override void SetValue(int row, int col, byte value, Boolean fixedValue)
	{
		if(((value < 1 || value > SudokuGrid.SudokuSize) && value != Values.Undefined) || row < 0 || col < 0 || row > SudokuGrid.SudokuSize || col > SudokuGrid.SudokuSize)
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

			internalSeverityLevel = float.NaN;
			OnCellChanged(Cell(row, col));
		}
	}

	/// <summary>
	/// Returns the current value of the cell at the specified coordinates.
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	/// <returns>The stored cell value or <see cref="Values.Undefined"/>.</returns>
	public override byte GetValue(int row, int col)
	{
		return Cell(row, col).CellValue;
	}

	/// <summary>
	/// Returns whether the cell at the specified coordinates is a fixed given.
	/// </summary>
	public override Boolean FixedValue(int row, int col)
	{
		return Cell(row, col).FixedValue;
	}

	/// <summary>
	/// Returns whether the cell at the specified coordinates was computed by the solver.
	/// </summary>
	public override Boolean ComputedValue(int row, int col)
	{
		return Cell(row, col).ComputedValue;
	}

	/// <summary>
	/// Returns whether the cell at the specified coordinates is read-only.
	/// </summary>
	public override Boolean ReadOnly(int row, int col)
	{
		return Cell(row, col).ReadOnly;
	}

	/// <summary>
	/// Reinitializes per-cell runtime state while keeping the matrix layout intact.
	/// </summary>
	public override void Init()
	{
		nVarValues = int.MinValue; // not initialized
		internalSeverityLevel = float.NaN;

		foreach(BaseCell cell in this)
			cell.Init();
	}

	/// <summary>
	/// Returns the <see cref="BaseCell"/> instance located at the specified coordinates.
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	/// <returns>The cell at the requested position.</returns>
	public BaseCell Cell(int row, int col)
	{
		return Matrix[row][col];
	}

	/// <summary>
	/// Reset candidate marks for all cells in the matrix.
	/// </summary>
	public void ResetCandidates()
	{
		foreach(BaseCell cell in this)
			cell.InitCandidates();
	}

	/// <summary>
	/// Reset candidate marks for a single cell.
	/// </summary>
	/// <param name="row">Row index (0-based).</param>
	/// <param name="col">Column index (0-based).</param>
	public void ResetCandidates(int row, int col)
	{
		Cell(row, col).InitCandidates();
	}

	/// <summary>
	/// Clears non-fixed values and resets indirect block state.
	/// Marks original fixed values as read-only.
	/// </summary>
	public void Reset()
	{
		SetPredefinedValues = false;
		for(int row = 0; row < SudokuGrid.SudokuSize; row++)
			for(int col = 0; col < SudokuGrid.SudokuSize; col++)
				if(!FixedValue(row, col) || ComputedValue(row, col))
					SetValue(row, col, Values.Undefined, false);
				else
					Cell(row, col).ReadOnly = true;
		ResetIndirectBlocks();
		SetPredefinedValues = true;
	}

	/// <summary>
	/// Recompute indirect block counters for all cells and reset the definitive counter.
	/// </summary>
	private void ResetIndirectBlocks()
	{
		foreach(BaseCell cell in this)
			cell.InitIndirectBlocks();
		definitiveCalculatorCounter = 0;
	}

	/// <summary>
	/// Prepare the matrix for solving: set definite values, sort candidate list and compute variable count.
	/// </summary>
	public void Prepare()
	{
		SetDefiniteValues();
		if(sortableValues == null) sortableValues = new List<BaseCell>(SudokuGrid.TotalCellCount);
		sortableValues.Sort();
		sorted = true;
		nVarValues = (SudokuGrid.TotalCellCount) - nValues;
	}

	/// <summary>
	/// Convert detected definitive candidate hints into fixed computed values.
	/// This method marks computed values as fixed and triggers change notifications.
	/// </summary>
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

	/// <summary>
	/// Return a list of hint cells (cells for which a definite value was found).
	/// </summary>
	/// <param name="deep">If true, run deeper detection heuristics.</param>
	/// <returns>List of cells that have a computed definitive value.</returns>
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
	private static List<BaseCell>? obviousBuffer;

	/// <summary>
	/// Returns a copy of the currently obvious cells (cells with exactly one possible value).
	/// </summary>
	/// <param name="reset">If true, reset indirect blocks before computing obvious cells.</param>
	/// <returns>List of obvious cells.</returns>
	public List<BaseCell> GetObviousCells(Boolean reset)
	{
		List<BaseCell> values = GetObviousCellsPooled(reset);
		var copy = new List<BaseCell>(values.Count);
		copy.AddRange(values);
		return copy;
	}

	/// <summary>
	/// Internal pooled implementation used to gather obvious cells without allocating a new list.
	/// </summary>
	/// <param name="reset">If true, reset indirect blocks before computing obvious cells.</param>
	/// <returns>Pooled list of obvious cells (do not hold long-term).</returns>
	private List<BaseCell> GetObviousCellsPooled(Boolean reset)
	{
		if(reset) ResetIndirectBlocks();

		if(obviousBuffer == null)
			obviousBuffer = new List<BaseCell>(SudokuGrid.TotalCellCount);
		else
			obviousBuffer.Clear();

		for(int i = 0; i < cells.Count; i++)
		{
			var cell = cells[i];
			if(cell.nPossibleValues == 1) obviousBuffer.Add(cell);
		}
		return obviousBuffer;
	}

	/// <summary>
	/// Fill all obvious cells by converting their definite candidate into a definitive value.
	/// </summary>
	/// <param name="reset">If true, reset indirect blocks before the operation.</param>
	/// <returns>True if at least one obvious value was filled.</returns>
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

	/// <summary>
	/// Exposed helper to call <see cref="FillObviousCells(Boolean)"/>.
	/// </summary>
	/// <param name="reset">If true, reset indirect blocks before the operation.</param>
	/// <returns>True if any obvious cells were filled.</returns>
	internal bool CallFillObviousCells(bool reset)
	{
		return FillObviousCells(reset);
	}

	/// <summary>
	/// Search for definite values using a sequence of heuristic passes.
	/// The method iterates obvious, isolated and naked cell heuristics until no further progress is found,
	/// or — when deep is true — until all deep heuristics have been exhausted.
	/// </summary>
	/// <param name="deep">When true, enable deeper heuristics such as naked/isolated detection.</param>
	private void SearchDefiniteValues(Boolean deep)
	{
		Boolean found = false;

		do
		{
			definitiveCalculatorCounter++;
			found = FillObviousCells(false);

			if(!found || deep)
				for(int i = 0; i < SudokuGrid.SudokuSize; i++)
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

	/// <summary>
	/// Apply naked-cell heuristics to the provided unit (row/col/rectangle/diagonal).
	/// </summary>
	/// <param name="part">Array of cells representing the unit to analyze.</param>
	/// <returns>True if any change was made by the heuristic.</returns>
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
				if(possible <= 1 || possible >= SudokuGrid.SudokuSize - 1) continue;
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

	/// <summary>
	/// Apply isolated-candidate heuristics to the provided unit (row/col/rectangle/diagonal).
	/// Builds per-candidate lists of cells and blocks candidates in other cells when isolation is detected.
	/// </summary>
	/// <param name="part">Array of cells representing the unit to analyze.</param>
	/// <returns>True if any candidate was removed or a definitive value detected.</returns>
	private Boolean HandleIsolatedCells(BaseCell[] part)
	{
		if(FillObviousCells(false)) return true;

		if(part == null || part.Length == 0) return false;

		Boolean rc = false;
		int size = SudokuGrid.SudokuSize;
		int plen = part.Length;

		int bufferLength = size * plen;
		BaseCell[] buffer = isolatedBuffer ?? (isolatedBuffer = new BaseCell[bufferLength] );
		if(buffer.Length < bufferLength)
		{
			buffer = new BaseCell[bufferLength];
			isolatedBuffer = buffer;
		}
		int[] enabledCounts = isolatedEnabledCounts ?? (isolatedEnabledCounts = new int[size]);
		if(enabledCounts.Length < size)
		{
			enabledCounts = new int[size];
			isolatedEnabledCounts = enabledCounts;
		}
		int[] usedCandidates = isolatedCandidateIndex ?? (isolatedCandidateIndex = new int[size]);
		if(usedCandidates.Length < size)
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
			// clear only the used prefix of the buffer array to avoid assigning null into pooled array beyond its rented length
			for(int i = 0; i < usedCandidateCount * plen && i < buffer.Length; i++) buffer[i] = default!;
			for(int i = 0; i < usedCandidateCount; i++) enabledCounts[usedCandidates[i]] = 0;
		}

		return rc;
	}

	/// <summary>
	/// Given a contiguous segment of the enabled-cells array, block the candidate in other cells of the unit
	/// according to whether the cells are aligned in the same row/column/rectangle.
	/// </summary>
	/// <param name="enabledCellsArr">Flat array containing candidate->cell mapping.</param>
	/// <param name="offset">Offset into the array where the candidate's list starts.</param>
	/// <param name="count">Number of cells that support this candidate.</param>
	/// <param name="block">Candidate value to block in other cells.</param>
	/// <returns>True if any changes were applied.</returns>
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

		int rectSize = SudokuGrid.RectSize;
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

	/// <summary>
	/// Convenience overload that blocks using the entire array segment starting at zero.
	/// </summary>
	/// <param name="enabledCellsArr">Array containing cells supporting the candidate.</param>
	/// <param name="count">Number of supporting cells.</param>
	/// <param name="block">Candidate value to block.</param>
	/// <returns>True if any changes were applied.</returns>
	private Boolean BlockOtherCellsArray(BaseCell[] enabledCellsArr, int count, int block)
	{
		return BlockOtherCellsArray(enabledCellsArr, 0, count, block);
	}

	/// <summary>
	/// Public wrapper that blocks candidate occurrences using a list of cells.
	/// </summary>
	/// <param name="enabledCells">List of cells that support the candidate.</param>
	/// <param name="block">Candidate value to block in other cells.</param>
	/// <returns>True if changes were applied.</returns>
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
			// clear only the used portion before returning to the pool to avoid leaving references
			Array.Clear(arr, 0, count);
			pool.Return(arr, false);
		}
	}

	/// <summary>
	/// Internal bridge used by tests or other components to call <see cref="BlockOtherCells(List{BaseCell},int)"/>.
	/// </summary>
	/// <param name="enabledCells">List of cells that support the candidate.</param>
	/// <param name="block">Candidate value to block.</param>
	/// <returns>True if changes were applied.</returns>
	internal bool CallBlockOtherCells(List<BaseCell> enabledCells, int block)
	{
		return BlockOtherCells(enabledCells, block);
	}

	/// <summary>
	/// Internal bridge used by tests or other components to call <see cref="HandleIsolatedCells(BaseCell[])"/>.
	/// </summary>
	/// <param name="part">Unit to analyze.</param>
	/// <returns>True if changes were applied.</returns>
	internal bool CallHandleIsolatedCells(BaseCell[] part)
	{
		return HandleIsolatedCells(part);
	}

	/// <summary>
	/// Returns the sorted cell at the given index (sorted by heuristics).
	/// Ensures the sortable list is up-to-date before returning.
	/// </summary>
	/// <param name="current">Index into the sorted list.</param>
	/// <returns>The <see cref="BaseCell"/> at the specified sorted position.</returns>
	public BaseCell Get(int current)
	{
		if(!sorted)
		{
			if(sortableValues == null) sortableValues = new List<BaseCell>();
			sortableValues.Sort();
			sorted = true;
		}
		return sortableValues![current];
	}

	/// <summary>
	/// Returns the diagonal unit for X-Sudoku matrices.
	/// Concrete subclasses must return the requested diagonal array.
	/// </summary>
	/// <param name="direction">Which diagonal to return.</param>
	/// <returns>Array of cells on the requested diagonal.</returns>
	protected abstract BaseCell[] GetDiagonal(SudokuPart direction);

	/// <summary>
	/// Verifies that the provided unit (row/col/rect) is consistent: every candidate value can still be placed.
	/// </summary>
	/// <param name="values">Unit cells to check.</param>
	/// <returns>True if the unit is valid; false if any value has no valid placement.</returns>
	public Boolean Check(BaseCell[] values)
	{
		Boolean checkCurrentValue = true;
		Boolean valueIsPossible = true;
		int currentValue = 0;
		int i = 0;

		for(currentValue = 1; currentValue < SudokuGrid.SudokuSize + 1; currentValue++)
		{
			i = 0;
			checkCurrentValue = true;
			while(i < SudokuGrid.SudokuSize && checkCurrentValue)
				checkCurrentValue = (values[i++].CellValue != currentValue);
			if(checkCurrentValue)
			{
				i = 0;
				valueIsPossible = false;
				while(i < SudokuGrid.SudokuSize && !valueIsPossible)
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

	/// <summary>
	/// Computes a heuristic severity score describing puzzle difficulty/complexity.
	/// The value is computed lazily and cached.
	/// </summary>
	public virtual float SeverityLevel
	{
		get
		{
			if(nValues < MinimumValues) return float.NaN;

			if(float.IsNaN(internalSeverityLevel))
			{
				int totalComplexity = 0;
				int minValuesRow = SudokuGrid.SudokuSize;
				int minValuesCol = SudokuGrid.SudokuSize;
				int minValuesRect = SudokuGrid.SudokuSize;
				int maxValuesRow = 0;
				int maxValuesCol = 0;
				int maxValuesRect = 0;
				byte minNumber = (byte)SudokuGrid.SudokuSize;
				byte maxNumber = 0;
				byte[] digitCounter = new byte[SudokuGrid.SudokuSize];

				if(definitiveCalculatorCounter == 0) SearchDefiniteValues(true);

				for(int row = 0; row < SudokuGrid.SudokuSize; row++)
				{
					int nVal = SudokuGrid.SudokuSize;
					for(int col = 0; col < SudokuGrid.SudokuSize; col++)
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

				for(int col = 0; col < SudokuGrid.SudokuSize; col++)
				{
					int nVal = SudokuGrid.SudokuSize;
					for(int row = 0; row < SudokuGrid.SudokuSize; row++)
						if(!Cell(row, col).FixedValue || Cell(row, col).ComputedValue)
							nVal--;
					minValuesCol = Math.Min(minValuesCol, nVal);
					maxValuesCol = Math.Max(maxValuesCol, nVal);
				}

				for(int row = 0; row < SudokuGrid.SudokuSize; row += SudokuGrid.RectSize)
				{
					for(int col = 0; col < SudokuGrid.SudokuSize; col += SudokuGrid.RectSize)
					{
						int nVal = SudokuGrid.SudokuSize;
						for(int i = 0; i < SudokuGrid.RectSize; i++)
							for(int j = 0; j < SudokuGrid.RectSize; j++)
								if(!Matrix[row + i][col + j].FixedValue || Matrix[row + i][col + j].ComputedValue)
									nVal--;
						minValuesRect = Math.Min(minValuesRect, nVal);
						maxValuesRect = Math.Max(maxValuesRect, nVal);
					}
				}

				for(int number = 0; number < SudokuGrid.SudokuSize; number++)
				{
					maxNumber = Math.Max(maxNumber, digitCounter[number]);
					minNumber = Math.Min(minNumber, digitCounter[number]);
				}

				internalSeverityLevel = (float)((totalComplexity - (nValues - nComputedValues) + (maxValuesCol - minValuesCol) + (maxValuesRow - minValuesRow) + (maxValuesRect - minValuesRect) + (maxNumber - minNumber) * 2f + definitiveCalculatorCounter + 80f) / 3f);
			}
			return internalSeverityLevel;
		}
	}
}