using System;
using System.Collections;
using System.Collections.Generic;
using System.Buffers;

namespace Sudoku
{
    [Serializable]
    internal abstract class BaseMatrix: Values
    {
        protected BaseCell[][] matrix;
        protected BaseCell[][] cols;
        protected BaseCell[][] rectangles;
        private List<BaseCell> sortableValues;
        private List<BaseCell> cells;
        private Boolean sorted=false;
        private int nVarValues=0;
        protected float severityLevel=float.NaN;
        private int definitiveCalculatorCounter=0;
        private Boolean setPredefinedValues=true;

        [ThreadStatic]
        private static int[] memberStamp;
        [ThreadStatic]
        private static int memberStampId;

        public event EventHandler<BaseCell> CellChanged;
        protected virtual void OnCellChanged(BaseCell v)
        {
            EventHandler<BaseCell> handler=CellChanged;
            if(handler!=null) handler(this, v);
        }

        public BaseMatrix()
        {
            int row, col;
            int i, j;
            int startCol, startRow;

            Matrix=new BaseCell[SudokuForm.SudokuSize][];
            Cols=new BaseCell[SudokuForm.SudokuSize][];
            Rectangles=new BaseCell[SudokuForm.SudokuSize][];
            sortableValues=new List<BaseCell>();
            cells=new List<BaseCell>();
            nVarValues=int.MinValue; // not initialized
            severityLevel=float.NaN;

            for(row=0; row<SudokuForm.SudokuSize; row++)
            {
                Matrix[row]=new BaseCell[SudokuForm.SudokuSize];
                Cols[row]=new BaseCell[SudokuForm.SudokuSize];
                Rectangles[row]=new BaseCell[SudokuForm.SudokuSize];

                for(col=0; col<SudokuForm.SudokuSize; col++)
                    Matrix[row][col]=CreateValue(row, col);
            }

            for(col=0; col<SudokuForm.SudokuSize; col++)
                for(row=0; row<SudokuForm.SudokuSize; row++)
                    Cols[col][row]=Matrix[row][col];

            for(row=0; row<SudokuForm.SudokuSize; row+=SudokuForm.RectSize)
                for(col=0; col<SudokuForm.SudokuSize; col+=SudokuForm.RectSize)
                {
                    int count=0;
                    for(i=0; i<SudokuForm.RectSize; i++)
                        for(j=0; j<SudokuForm.RectSize; j++)
                            Rectangles[row+((col/SudokuForm.RectSize)%SudokuForm.RectSize)][count++]=Matrix[row+i][col+j];
                }

            for(row=0; row<SudokuForm.SudokuSize; row++)
                for(col=0; col<SudokuForm.SudokuSize; col++)
                {
                    for(i=0; i<SudokuForm.SudokuSize; i++)
                        if(i!=col) Cell(row, col).AddNeighbor(ref Matrix[row][i]);
                    for(i=0; i<SudokuForm.SudokuSize; i++)
                        if(i!=row) Cell(row, col).AddNeighbor(ref Matrix[i][col]);

                    startCol=(int)Math.Truncate((double)col/SudokuForm.RectSize)*SudokuForm.RectSize;
                    startRow=(int)Math.Truncate((double)row/SudokuForm.RectSize)*SudokuForm.RectSize;
                    for(i=startRow; i<startRow+SudokuForm.RectSize; i++)
                        for(j=startCol; j<startCol+SudokuForm.RectSize; j++)
                            if(i!=row&&j!=col) Cell(row, col).AddNeighbor(ref Matrix[i][j]);
                    sortableValues.Add(Cell(row, col));
                    cells.Add(Cell(row, col));
                    Cell(row, col).Init();
                }
        }

        public abstract BaseCell CreateValue(int row, int col);

        public IEnumerator GetEnumerator()
        {
            for(int row=0; row<SudokuForm.SudokuSize; row++)
                for(int col=0; col<SudokuForm.SudokuSize; col++)
                    yield return Cell(row, col);
        }

        public BaseCell[][] Matrix
        {
            set { matrix=value; }
            get { return matrix; }
        }

        public BaseCell[][] Rows
        {
            set { matrix=value; }
            get { return matrix; }
        }

        public BaseCell[][] Cols
        {
            set { cols=value; }
            get { return cols; }
        }

        public BaseCell[][] Rectangles
        {
            set { rectangles=value; }
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
                int nVal=0;
                foreach(BaseCell cell in this)
                    if(cell.FixedValue) nVal++;
                return nVal;
            }
        }

        public int nComputedValues
        {
            get
            {
                int nVal=0;
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
                setPredefinedValues=value;
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
            for(int row=0; row<SudokuForm.SudokuSize; row++)
                for(int col=0; col<SudokuForm.SudokuSize; col++)
                    for(int candidate=1; candidate<SudokuForm.SudokuSize+1; candidate++)
                        if(Cell(row, col).GetCandidateMask(candidate, false) || Cell(row, col).GetCandidateMask(candidate, true)) return true;

            return false;
        }

        public override void SetValue(int row, int col, byte value, Boolean fixedValue)
        {
            if(((value < 1 || value > SudokuForm.SudokuSize) && value != Values.Undefined) || row<0 || col<0 || row > SudokuForm.SudokuSize || col>SudokuForm.SudokuSize)
                throw new InvalidSudokuValueException();

            if(Cell(row, col).FixedValue != fixedValue)
                nVarValues=fixedValue? nVarValues-1: nVarValues+1;

            Cell(row, col).FixedValue=fixedValue;
            Cell(row, col).ComputedValue=false;
            if(GetValue(row, col) != value)
            {
                lock(this)
                {
                    if(SetPredefinedValues && value == Values.Undefined) ResetIndirectBlocks();
                    Cell(row, col).CellValue=value;
                    if(SetPredefinedValues) SearchDefiniteValues(true);
                }

                severityLevel=float.NaN;
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
            nVarValues=int.MinValue; // not initialized
            severityLevel=float.NaN;

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

        public void Reset()
        {
            SetPredefinedValues=false;
            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                    if(!FixedValue(row, col) || ComputedValue(row, col))
                        SetValue(row, col, Values.Undefined, false);
                    else
                        Cell(row, col).ReadOnly = true;
            ResetIndirectBlocks();
            SetPredefinedValues=true;
        }

        private void ResetIndirectBlocks()
        {
            foreach(BaseCell cell in this)
                cell.InitIndirectBlocks();
            definitiveCalculatorCounter=0;
        }

        public void Prepare()
        {
            SetDefiniteValues();
            sortableValues.Sort();
            sorted=true;
            nVarValues=(SudokuForm.TotalCellCount)-nValues;
        }

        private void SetDefiniteValues()
        {
            SearchDefiniteValues(true);

            foreach(BaseCell cell in this)
                if(cell.DefinitiveValue != Values.Undefined)
                {
                    Byte definitiveValue=cell.DefinitiveValue;
                    cell.DefinitiveValue=Values.Undefined;
                    cell.CellValue=definitiveValue;
                    OnCellChanged(cell);
                    if(!cell.FixedValue) nVarValues--;
                    cell.FixedValue=true;
                    cell.ComputedValue=true;
                }
        }

        public List<BaseCell> GetHints(Boolean deep)
        {
            List<BaseCell> values=new List<BaseCell>();

            SearchDefiniteValues(deep);

            foreach(BaseCell cell in this)
                if(cell.DefinitiveValue!=Values.Undefined)
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
                obviousBuffer = new List<BaseCell>(SudokuForm.TotalCellCount);
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
            Boolean rc = values.Count>0;

            while(values.Count>0)
            {
                for(int i=0; i<values.Count; i++)
                    if(values[i].nPossibleValues==1) values[i].FillDefiniteValue();
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
            Boolean found=false;

            do
            {
                definitiveCalculatorCounter++;
                found=FillObviousCells(false);

                if(!found||deep)
                    for(int i=0; i<SudokuForm.SudokuSize; i++)
                    {
                        found|=HandleIsolatedCells(Rows[i]);
                        found|=HandleIsolatedCells(Cols[i]);
                        found|=HandleIsolatedCells(Rectangles[i]);
                        found|=HandleNakedCells(Rows[i]);
                        found|=HandleNakedCells(Cols[i]);
                        found|=HandleNakedCells(Rectangles[i]);
                    }

                if(this is XSudokuMatrix&&(!found||deep))
                {
                    found|=HandleNakedCells(GetDiagonal(SudokuPart.DownDiagonal));
                    found|=HandleIsolatedCells(GetDiagonal(SudokuPart.DownDiagonal));
                    found|=HandleNakedCells(GetDiagonal(SudokuPart.UpDiagonal));
                    found|=HandleIsolatedCells(GetDiagonal(SudokuPart.UpDiagonal));
                }
            } while(found&&deep);
        }

        private Boolean HandleNakedCells(BaseCell[] part)
        {
            if(FillObviousCells(false)) return true;

            if(part==null||part.Length==0) return false;

            int counterIncrease=0;
            BaseCell.NakedScratch scratch = default;
            try
            {
                for(int i=0;i<part.Length;i++)
                {
                    var cell = part[i];
                    counterIncrease=Math.Max(cell.FindNakedCells(part, ref scratch), counterIncrease);
                }
            }
            finally
            {
                scratch.Release();
            }
            definitiveCalculatorCounter+=counterIncrease;
            return counterIncrease>0;
        }

        private Boolean HandleIsolatedCells(BaseCell[] part)
        {
            if(FillObviousCells(false)) return true;

            if(part==null||part.Length==0) return false;

            Boolean rc=false;
            int size = SudokuForm.SudokuSize;
            int plen = part.Length;

            var cellPool = ArrayPool<BaseCell>.Shared;
            var intPool = ArrayPool<int>.Shared;
            int bufferLength = size * plen;
            BaseCell[] buffer = cellPool.Rent(bufferLength);
            int[] enabledCounts = intPool.Rent(size);
            Array.Clear(enabledCounts, 0, size);

            try
            {
                for(int pi = 0; pi < plen; pi++)
                {
                    BaseCell cell = part[pi];
                    if (cell.nPossibleValues <= 0) continue;

                    int mask = cell.GetEnabledMask();
                    while(mask != 0)
                    {
                        int lowbit = mask & -mask;
                        int cand = BaseCell.LowBitIndex(lowbit);

                        if(cand >= 1 && cand <= size)
                        {
                            int idx = cand - 1;
                            int pos = idx * plen + enabledCounts[idx]++;
                            buffer[pos] = cell;
                        }
                        mask &= (mask - 1);
                    }
                }

                for (int i = 0; i < size; i++)
                {
                    int count = enabledCounts[i];
                    if (count > 0)
                        rc |= BlockOtherCellsArray(buffer, i * plen, count, i + 1);
                }
            }
            finally
            {
                for (int i = 0; i < size; i++)
                {
                    int count = enabledCounts[i];
                    int start = i * plen;
                    for (int j = 0; j < count; j++) buffer[start + j] = null;
                }
                cellPool.Return(buffer, false);
                intPool.Return(enabledCounts, false);
            }

            return rc;
        }

        private Boolean BlockOtherCellsArray(BaseCell[] enabledCellsArr, int offset, int count, int block)
        {
            Boolean rc = false;
            Boolean definitive = count == 1;

            BaseCell first = enabledCellsArr[offset];
            if (definitive)
            {
                rc = first.DefinitiveValue == Values.Undefined;
                first.DefinitiveValue = (byte)block;
            }

            int size = SudokuForm.SudokuSize;

            int baseRow = first.Row;
            int baseCol = first.Col;
            int firstRectRow = first.StartRow;
            int firstRectCol = first.StartCol / SudokuForm.RectSize;
            int baseRectIndex = firstRectRow + (firstRectCol % SudokuForm.RectSize);

            bool allSameRow = true;
            bool allSameCol = true;
            bool allSameRect = true;

            for (int i = 1; i < count; i++)
            {
                var c = enabledCellsArr[offset + i];
                if (c.Row != baseRow) allSameRow = false;
                if (c.Col != baseCol) allSameCol = false;
                if (c.StartRow != firstRectRow || (c.StartCol / SudokuForm.RectSize) != firstRectCol) allSameRect = false;
                if (!allSameRow && !allSameCol && !allSameRect) break;
            }

            // membership test via thread-static stamp array (no allocations)
            if (memberStamp == null || memberStamp.Length < SudokuForm.TotalCellCount)
                memberStamp = new int[SudokuForm.TotalCellCount];
            int stamp = ++memberStampId;
            if (stamp == 0)
            {
                Array.Clear(memberStamp, 0, SudokuForm.TotalCellCount);
                stamp = ++memberStampId;
            }

            for (int i = 0; i < count; i++)
            {
                var c = enabledCellsArr[offset + i];
                memberStamp[c.Row * size + c.Col] = stamp;
            }

            bool ContainsCell(BaseCell cell)
            {
                return memberStamp[cell.Row * size + cell.Col] == stamp;
            }

            if (allSameRow)
            {
                var neighborCells = Rows[baseRow];
                for (int i = 0; i < neighborCells.Length; i++)
                {
                    var cell = neighborCells[i];
                    if (!ContainsCell(cell)) rc |= cell.TrySetBlock(block, false, false);
                }
            }

            if (!definitive && allSameCol)
            {
                var neighborCells = Cols[baseCol];
                for (int i = 0; i < neighborCells.Length; i++)
                {
                    var cell = neighborCells[i];
                    if (!ContainsCell(cell)) rc |= cell.TrySetBlock(block, false, false);
                }
            }

            if (!definitive && allSameRect)
            {
                var neighborCells = Rectangles[baseRectIndex];
                for (int i = 0; i < neighborCells.Length; i++)
                {
                    var cell = neighborCells[i];
                    if (!ContainsCell(cell)) rc |= cell.TrySetBlock(block, false, false);
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
            if (enabledCells == null) return false;
            int count = enabledCells.Count;
            if (count == 0) return false;

            var pool = ArrayPool<BaseCell>.Shared;
            BaseCell[] arr = pool.Rent(count);
            try
            {
                for (int i = 0; i < count; i++) arr[i] = enabledCells[i];
                return BlockOtherCellsArray(arr, count, block);
            }
            finally
            {
                for (int i = 0; i < count; i++) arr[i] = null;
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
                sorted=true;
            }
            return (BaseCell)sortableValues[current];
        }

        protected abstract BaseCell[] GetDiagonal(SudokuPart direction);

        public static Boolean Check(BaseCell[] values)
        {
            Boolean checkCurrentValue=true;
            Boolean valueIsPossible=true;
            int currentValue=0;
            int i=0;

            for(currentValue=1; currentValue<SudokuForm.SudokuSize+1; currentValue++)
            {
                i=0;
                checkCurrentValue=true;
                while(i<SudokuForm.SudokuSize&&checkCurrentValue)
                    checkCurrentValue=(values[i++].CellValue!=currentValue);
                if(checkCurrentValue)
                {
                    i=0;
                    valueIsPossible=false;
                    while(i<SudokuForm.SudokuSize&&!valueIsPossible)
                    {
                        valueIsPossible=((!values[i].FixedValue&&values[i].Enabled(currentValue))||values[i].DefinitiveValue==currentValue);
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
                if(nValues<MinimumValues) return float.NaN;

                if(float.IsNaN(severityLevel)||false)
                {
                    int totalComplexity=0;
                    int minValuesRow=SudokuForm.SudokuSize;
                    int minValuesCol=SudokuForm.SudokuSize;
                    int minValuesRect=SudokuForm.SudokuSize;
                    int maxValuesRow=0;
                    int maxValuesCol=0;
                    int maxValuesRect=0;
                    byte minNumber=SudokuForm.SudokuSize;
                    byte maxNumber=0;
                    byte[] digitCounter=new byte[SudokuForm.SudokuSize];

                    if(definitiveCalculatorCounter==0) SearchDefiniteValues(true);

                    for(int row=0; row<SudokuForm.SudokuSize; row++)
                    {
                        int nVal=SudokuForm.SudokuSize;
                        for(int col=0; col<SudokuForm.SudokuSize; col++)
                        {
                            totalComplexity+=Cell(row, col).nPossibleValues;
                            if(!Cell(row, col).FixedValue||Cell(row, col).ComputedValue)
                                nVal--;
                            else if(Cell(row, col).CellValue!=Values.Undefined)
                                digitCounter[Cell(row, col).CellValue-1]++;

                        }
                        minValuesRow=Math.Min(minValuesRow, nVal);
                        maxValuesRow=Math.Max(maxValuesRow, nVal);
                    }

                    for(int col=0; col<SudokuForm.SudokuSize; col++)
                    {
                        int nVal=SudokuForm.SudokuSize;
                        for(int row=0; row<SudokuForm.SudokuSize; row++)
                            if(!Cell(row, col).FixedValue||Cell(row, col).ComputedValue)
                                nVal--;
                        minValuesCol=Math.Min(minValuesCol, nVal);
                        maxValuesCol=Math.Max(maxValuesCol, nVal);
                    }

                    for(int row=0; row<SudokuForm.SudokuSize; row+=SudokuForm.RectSize)
                    {
                        for(int col=0; col<SudokuForm.SudokuSize; col+=SudokuForm.RectSize)
                        {
                            int nVal=SudokuForm.SudokuSize;
                            for(int i=0; i<SudokuForm.RectSize; i++)
                                for(int j=0; j<SudokuForm.RectSize; j++)
                                    if(!Matrix[row+i][col+j].FixedValue||Matrix[row+i][col+j].ComputedValue)
                                        nVal--;
                            minValuesRect=Math.Min(minValuesRect, nVal);
                            maxValuesRect=Math.Max(maxValuesRect, nVal);
                        }
                    }

                    for(int number=0; number<SudokuForm.SudokuSize; number++)
                    {
                        maxNumber=Math.Max(maxNumber, digitCounter[number]);
                        minNumber=Math.Min(minNumber, digitCounter[number]);
                    }

                    severityLevel=(float)((totalComplexity-(nValues-nComputedValues)+(maxValuesCol-minValuesCol)+(maxValuesRow-minValuesRow)+(maxValuesRect-minValuesRect)+(maxNumber-minNumber)*2f+definitiveCalculatorCounter+80f)/3f);
                }
                return severityLevel;
            }
        }
    }
}