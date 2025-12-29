using System;

using Sudoku.Properties;

namespace Sudoku
{
    internal class GenerationParameters
    {
        private int row=0;
        private int col=0;
        private Byte generatedValue=0;
        private Boolean reset=false;
        private Int64 totalPasses=0;
        private Int64 checkedProblems=0;
        private int preAllocatedValues=0;
        private int currentProblem=0;
        private Boolean generateBooklet=false;
        private String baseDirectory=String.Empty;
        // private Random rand=new Random(unchecked((int)DateTime.Now.Ticks));
        private Random rand=new Random(1);

        public String BaseDirectory
        {
            get { return baseDirectory; }
            set { baseDirectory=value; }
        }

        public Boolean GenerateBooklet
        {
            get { return generateBooklet; }
            set { generateBooklet=value; }
        }

        public int CurrentProblem
        {
            get { return currentProblem; }
            set { currentProblem=value; }
        }

        public int PreAllocatedValues
        {
            get { return preAllocatedValues; }
            set { preAllocatedValues=value; }
        }

        public int Row
        {
            get { return row; }
            set { row=value; }
        }

        public int Col
        {
            get { return col; }
            set { col=value; }
        }

        public Byte GeneratedValue
        {
            get { return generatedValue; }
            set { generatedValue=value; }
        }

        public Boolean Reset
        {
            get { return reset; }
            set { reset=value; }
        }

        public Int64 TotalPasses
        {
            get { return totalPasses; }
            set { totalPasses=value; }
        }

        public Int64 CheckedProblems
        {
            get { return checkedProblems; }
            set { checkedProblems=value; }
        }

        public void NewValue()
        {
            generatedValue=(Byte)rand.Next(1, SudokuForm.SudokuSize+1);
            row=rand.Next(0, SudokuForm.SudokuSize);
            col=rand.Next(0, SudokuForm.SudokuSize);
        }

        public Boolean NewSudokuType()
        {
            if(Settings.Default.GenerateXSudoku&&Settings.Default.GenerateNormalSudoku)
                return rand.Next()%2==0;
            else
                return Settings.Default.GenerateXSudoku;
        }
    }
}