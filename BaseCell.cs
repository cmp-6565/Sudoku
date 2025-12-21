using System;
using System.Collections.Generic;

namespace Sudoku
{
    [Serializable]
    internal abstract class BaseCell: EventArgs, IComparable
    {
        private CoreValue coreValue=new CoreValue();
        private Byte definitiveValue=Values.Undefined;
        private int nNeighbors=0;
        private int[] directBlocks;
        private int[] indirectBlocks;
        private Boolean[] candidates;
        private Boolean[] exclusionCandidates;
        private int possibleValuesCount=0;
        private Boolean fixedValue=false;
        private Boolean computedValue=false;
        private Boolean readOnly=false;
        protected BaseCell[] neighbors;
        private int startCol=0;
        private int startRow=0;

        public abstract Boolean Up();
        public abstract Boolean Down();

        public static bool operator ==(BaseCell op1, BaseCell op2)
        {
            return op1.Row==op2.Row&&op1.Col==op2.Col;
        }

        public static bool operator !=(BaseCell op1, BaseCell op2)
        {
            return !(op1==op2);
        }

        public static bool operator >(BaseCell op1, BaseCell op2)
        {
            return op1.GetHashCode()>op2.GetHashCode();
        }

        public static bool operator <(BaseCell op1, BaseCell op2)
        {
            return op1.GetHashCode()<op2.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this==(BaseCell)obj;
        }

        public override int GetHashCode()
        {
            return Row*SudokuForm.SudokuSize+Col;
        }

        public Byte CellValue
        {
            get { return coreValue.CellValue; }
            set
            {
                if(CellValue==value) return;

                if(value!=Values.Undefined) DefinitiveValue=Values.Undefined;
                if(!Enabled(value))
                    throw new ArgumentException("value not possible", "value");

                SetBlocks(CellValue, value, true);
                coreValue.CellValue=value;
            }
        }

        public Byte DefinitiveValue
        {
            get { return definitiveValue; }
            set
            {
                if(DefinitiveValue==value) return;

                SetBlocks(DefinitiveValue, value, false);
                definitiveValue=value;
            }
        }

        public int Row
        {
            get { return coreValue.Row; }
            set
            {
                coreValue.Row=value;
                startRow=(int)Math.Truncate((double)value/SudokuForm.RectSize)*SudokuForm.RectSize;
            }
        }

        public int Col
        {
            get { return coreValue.Col; }
            set
            {
                coreValue.Col=value;
                startCol=(int)Math.Truncate((double)value/SudokuForm.RectSize)*SudokuForm.RectSize;
            }
        }

        public int StartRow
        {
            get { return startRow; }
        }

        public int StartCol
        {
            get { return startCol; }
        }

        public BaseCell[] Neighbors
        {
            get { return neighbors; }
        }

        public int nPossibleValues
        {
            get { return (FixedValue||DefinitiveValue!=Values.Undefined) ? 0: possibleValuesCount-1; }
        }

        public Boolean FixedValue
        {
            get { return fixedValue; }
            set { fixedValue=value; }
        }

        public Boolean ReadOnly
        {
            get { return readOnly; }
            set { readOnly=value; }
        }

        public Boolean ComputedValue
        {
            get { return computedValue; }
            set { computedValue=value; }
        }

        public Boolean[] Candiates
        {
            get { return candidates; }
        }

        public Boolean[] ExclusionCandiates
        {
            get { return exclusionCandidates; }
        }

        public BaseCell()
        {
        }

        public BaseCell(int row, int col)
        {
            Row=row;
            Col=col;
            Init();
        }

        public void AddNeighbor(ref BaseCell neighbor)
        {
            neighbors[nNeighbors++]=neighbor;
        }

        public int CompareTo(System.Object obj)
        {
            if(obj==null) return -1;
            BaseCell tmpObj;
            if(!((tmpObj=(BaseCell)obj) is BaseCell)) throw new ArgumentException(obj.ToString());

            if(FixedValue) return int.MaxValue;
            if(tmpObj.FixedValue) return int.MinValue;
            return
                ((nPossibleValues*SudokuForm.TotalCellCount+Row*SudokuForm.SudokuSize+Col)-
                                     (tmpObj.nPossibleValues*SudokuForm.TotalCellCount+tmpObj.Row*SudokuForm.SudokuSize+tmpObj.Col));
        }

        public Boolean Enabled(int value)
        {
            return !(Blocked(value)||IndirectlyBlocked(value));
        }

        public Boolean Blocked(int value)
        {
            return directBlocks[value]!=0;
        }

        public Boolean IndirectlyBlocked(int value)
        {
            return indirectBlocks[value]!=0;
        }

        public void Init()
        {
            InitDirectBlocks();
            InitIndirectBlocks();
            InitCandidates();

            possibleValuesCount=directBlocks.Length;
            coreValue.CellValue=Values.Undefined;
            DefinitiveValue=Values.Undefined;
            FixedValue=false;
            ComputedValue=false;
        }

        public void InitCandidates()
        {
            candidates=new Boolean[SudokuForm.SudokuSize+1];
            exclusionCandidates=new Boolean[SudokuForm.SudokuSize+1];
        }

        private void InitDirectBlocks()
        {
            directBlocks=new int[SudokuForm.SudokuSize+1];
        }

        public void InitIndirectBlocks()
        {
            indirectBlocks=new int[SudokuForm.SudokuSize+1];

            possibleValuesCount=directBlocks.Length;
            for(int i=1; i<SudokuForm.SudokuSize+1; i++)
                if(!Enabled(i)) possibleValuesCount--;
            definitiveValue=Values.Undefined;
        }

        private Byte GetDefiniteValue()
        {
            if(DefinitiveValue!=Values.Undefined) return DefinitiveValue;

            Boolean found=false;
            Byte dv=Values.Undefined;

            for(byte possibleValue=1; possibleValue<SudokuForm.SudokuSize+1; possibleValue++)
                if(Enabled(possibleValue)&&nPossibleValues==1)
                {
                    if(found) return Values.Undefined;
                    found=true;
                    dv=possibleValue;
                }

            return dv;
        }

        public void FillDefiniteValue()
        {
            if((DefinitiveValue=GetDefiniteValue())==Values.Undefined)
                throw new InvalidSudokuValueException();
        }

        private void SetBlocks(Byte oldValue, Byte newValue, Boolean direct)
        {
            if(oldValue!=Values.Undefined)
            {
                if(direct)
                    SetBlock(oldValue, true, direct);
                else
                    for(int i=1; i<SudokuForm.SudokuSize+1; i++)
                        SetBlock(i, true, direct);
                EnableNeighbors(oldValue, direct);
            }

            if(newValue!=Values.Undefined)
            {
                if(direct)
                    SetBlock(newValue, false, direct);
                else
                    for(int i=1; i<SudokuForm.SudokuSize+1; i++)
                        SetBlock(i, false, direct);
                DisableNeighbors(newValue, direct);
            }
        }

        private void EnableNeighbors(byte value, Boolean direct)
        {
            SetNeighborBlocks(value, true, direct);
        }

        private void DisableNeighbors(byte value, Boolean direct)
        {
            SetNeighborBlocks(value, false, direct);
        }

        private void SetNeighborBlocks(byte newValue, Boolean enable, Boolean direct)
        {
            foreach(BaseCell neighbor in neighbors)
                neighbor.SetBlock(newValue, enable, direct);
        }

        public void SetBlock(int value, Boolean enable, Boolean direct)
        {
            if(enable)
            {
                if(direct)
                {
                    if(--directBlocks[value]<0)
                        throw new ArgumentException("enable not possible", "enable");
                }
                else
                {
                    if(--indirectBlocks[value]<0)
                        throw new ArgumentException("enable not possible", "enable");
                }
                if(Enabled(value)) possibleValuesCount++;
            }
            else
            {
                if(Enabled(value)) possibleValuesCount--;
                if(direct)
                    directBlocks[value]++;
                else
                    indirectBlocks[value]++;
            }
        }

        private Boolean Enabled(List<int> enabled)
        {
            if(DefinitiveValue!=Values.Undefined) return false;

            for(int i=1; i<=SudokuForm.SudokuSize; i++)
                if(Enabled(i)&&!enabled.Contains(i)) return false;
            return true;
        }

        private Boolean Change(List<int> enabled)
        {
            foreach(int i in enabled)
                if(Enabled(i)) return true;
            return false;
        }

        public int FindNakedCells(BaseCell[] neighborCells)
        {
            if(FindNakedCombination(neighborCells))
                return nPossibleValues*2;
            return -1;
        }

        private Boolean FindNakedCombination(BaseCell[] neighborCells)
        {
            Boolean rc=false;

            if(CellValue==Values.Undefined&&nPossibleValues>1&&nPossibleValues<8)
            {
                int count=nPossibleValues;
                List<int> enabled=new List<int>();
                List<BaseCell> candidateNeighbors=new List<BaseCell>();

                for(int i=1; i<=SudokuForm.SudokuSize; i++)
                    if(Enabled(i)) enabled.Add(i);

                foreach(BaseCell neighborCell in neighborCells)
                {
                    if(neighborCell.CellValue==Values.Undefined&&neighborCell.nPossibleValues<=count&&neighborCell.Enabled(enabled))
                        candidateNeighbors.Add(neighborCell);
                }

                if(candidateNeighbors.Count==count)
                {
                    List<BaseCell> commonNeighbors=GetCommonNeighbors(candidateNeighbors, neighborCells);
                    foreach(BaseCell updateCell in commonNeighbors)
                    {
                        rc=updateCell.Change(enabled);
                        for(int i=0; i<count; i++)
                            updateCell.SetBlock(enabled[i], false, false);
                    }
                }
            }
            return rc;
        }

        protected virtual List<BaseCell> GetCommonNeighbors(List<BaseCell> candidateNeighbors, BaseCell[] neighborCells)
        {
            List<BaseCell> commonNeighbors=new List<BaseCell>();
            foreach(BaseCell cell in neighborCells)
                if(cell!=this&&cell.CellValue==Values.Undefined&&!candidateNeighbors.Contains(cell))
                    commonNeighbors.Add(cell);

            return commonNeighbors;
        }

        public Boolean CommonNeighbor(BaseCell neighbor)
        {
            Boolean common=false;
            foreach(BaseCell cell in Neighbors)
                common=(cell==neighbor||common);
            return common;
        }

        public Boolean SameRectangle(BaseCell value)
        {
            return (Col>=value.StartCol&&Col<value.StartCol+SudokuForm.RectSize&&Row>=value.StartRow&&Row<value.StartRow+SudokuForm.RectSize);
        }
    }
}
