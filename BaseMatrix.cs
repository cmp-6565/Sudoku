using System;
using System.Collections;
using System.Collections.Generic;

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
            if(exclusionCandidate)
                return Cell(row, col).ExclusionCandiates[candidate];
            else
                return Cell(row, col).Candiates[candidate];
        }

        public void SetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
        {
            if(exclusionCandidate)
            {
                Cell(row, col).ExclusionCandiates[candidate]=!Cell(row, col).ExclusionCandiates[candidate];
                Cell(row, col).Candiates[candidate]=false;
            }
            else
            {
                Cell(row, col).Candiates[candidate]=!Cell(row, col).Candiates[candidate];
                Cell(row, col).ExclusionCandiates[candidate]=false;
            }
        }

        public Boolean HasCandidates()
        {
            for(int row=0; row<SudokuForm.SudokuSize; row++)
                for(int col=0; col<SudokuForm.SudokuSize; col++)
                    for(int candidate=1; candidate<SudokuForm.SudokuSize+1; candidate++)
                        if(Cell(row, col).Candiates[candidate]||Cell(row, col).ExclusionCandiates[candidate]) return true;

            return false;
        }

        public override void SetValue(int row, int col, byte value, Boolean fixedValue)
        {
            if(((value<1||value>SudokuForm.SudokuSize)&&value!=Values.Undefined)||row<0||col<0||row>SudokuForm.SudokuSize||col>SudokuForm.SudokuSize)
                throw new InvalidSudokuValueException();

            if(Cell(row, col).FixedValue!=fixedValue)
                nVarValues=fixedValue ? nVarValues-1: nVarValues+1;

            Cell(row, col).FixedValue=fixedValue;
            Cell(row, col).ComputedValue=false;
            if(GetValue(row, col)!=value)
            {
                lock(this)
                {
                    if(SetPredefinedValues&&value==Values.Undefined) ResetIndirectBlocks();
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
            for(int row=0; row<SudokuForm.SudokuSize; row++)
                for(int col=0; col<SudokuForm.SudokuSize; col++)
                    if(!FixedValue(row, col)||ComputedValue(row, col)) SetValue(row, col, Values.Undefined, false);
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
                if(cell.DefinitiveValue!=Values.Undefined)
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

        public List<BaseCell> GetObviousCells(Boolean reset)
        {
            List<BaseCell> values=new List<BaseCell>();

            if(reset) ResetIndirectBlocks();

            foreach(BaseCell cell in this)
                if(cell.nPossibleValues==1)
                    values.Add(cell);

            return values;
        }

        private Boolean FillObviousCells(Boolean reset)
        {
            List<BaseCell> values=GetObviousCells(reset);
            Boolean rc=values.Count>0;

            while(values.Count>0)
            {
                for(int i=0; i<values.Count; i++)
                    if(values[i].nPossibleValues==1) values[i].FillDefiniteValue();
                values=GetObviousCells(reset);
            }
            return rc;
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

        // <DEBUG CODE!>
        public void Print(int dcc)
        {
            System.Console.WriteLine(dcc.ToString());
            for(int row=0; row<SudokuForm.SudokuSize; row++)
            {
                for(int col=0; col<SudokuForm.SudokuSize; col++)
                    System.Console.Write("|"+(matrix[row][col].CellValue!=Values.Undefined ? " "+matrix[row][col].CellValue.ToString()+" ": (matrix[row][col].DefinitiveValue!=Values.Undefined ? "("+matrix[row][col].DefinitiveValue.ToString()+")": "-")));
                System.Console.WriteLine("|");
            }
            System.Console.WriteLine("-----------------------------------------");
        }
        // </DEBUG CODE!>

        private Boolean HandleNakedCells(BaseCell[] part)
        {
            if(FillObviousCells(false)) return true;

            if(part==null||part.Length==0) return false;

            int counterIncrease=0;
            foreach(BaseCell cell in part)
                counterIncrease=Math.Max(cell.FindNakedCells(part), counterIncrease);
            definitiveCalculatorCounter+=counterIncrease;
            return counterIncrease>0;
        }

        private Boolean HandleIsolatedCells(BaseCell[] part)
        {
            if(FillObviousCells(false)) return true;

            if(part==null||part.Length==0) return false;

            Boolean rc=false;
            List<BaseCell>[] enabled=new List<BaseCell>[SudokuForm.SudokuSize];
            for(int i=0; i<SudokuForm.SudokuSize; i++)
                enabled[i]=new List<BaseCell>();

            foreach(BaseCell cell in part)
                for(int i=0; i<SudokuForm.SudokuSize; i++)
                    if(cell.nPossibleValues>0&&cell.Enabled(i+1))
                        enabled[i].Add(cell);

            for(int i=0; i<SudokuForm.SudokuSize; i++)
                if(enabled[i].Count>0)
                    rc|=BlockOtherCells(enabled[i], i+1);

            return rc;
        }

        protected virtual Boolean BlockOtherCells(List<BaseCell> enabledCells, int block)
        {
            Boolean rc=false;
            Boolean proceed=true;
            Boolean definitive=enabledCells.Count==1;
            BaseCell[] neighborCells;

            if(definitive)
            {
                rc=enabledCells[0].DefinitiveValue==Values.Undefined;
                enabledCells[0].DefinitiveValue=(byte)block;
            }
            else
                foreach(BaseCell cell in enabledCells) proceed&=enabledCells[0].Row==cell.Row;
            if(proceed)
            {
                neighborCells=Rows[enabledCells[0].Row];
                foreach(BaseCell cell in neighborCells)
                    if(!enabledCells.Contains(cell))
                    {
                        rc|=cell.Enabled(block);
                        cell.SetBlock(block, false, false);
                    }
            }

            proceed=true;
            if(!definitive) foreach(BaseCell cell in enabledCells) proceed&=enabledCells[0].Col==cell.Col;
            if(proceed)
            {
                neighborCells=Cols[enabledCells[0].Col];
                foreach(BaseCell cell in neighborCells)
                    if(!enabledCells.Contains(cell))
                    {
                        rc|=cell.Enabled(block);
                        cell.SetBlock(block, false, false);
                    }
            }

            proceed=true;
            if(!definitive) foreach(BaseCell cell in enabledCells) proceed&=enabledCells[0].SameRectangle(cell);
            if(proceed)
            {
                neighborCells=Rectangles[enabledCells[0].StartRow+((enabledCells[0].StartCol/SudokuForm.RectSize)%SudokuForm.RectSize)];
                foreach(BaseCell cell in neighborCells)
                    if(!enabledCells.Contains(cell))
                    {
                        rc|=cell.Enabled(block);
                        cell.SetBlock(block, false, false);
                    }
            }

            return rc;
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