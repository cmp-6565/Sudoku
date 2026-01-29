using System;

using Sudoku.Properties;

namespace Sudoku
{
    internal class GenerationParameters
    {
		private readonly ISudokuSettings settings;
		
        private int row = 0;
        private int col = 0;
        private Byte generatedValue = 0;
        private Boolean reset = false;
        private Int64 totalPasses = 0;
        private Int64 checkedProblems = 0;
        private int preAllocatedValues = 0;
        private int currentProblem = 0;
        private Boolean generateBooklet = false;
        private String baseDirectory = String.Empty;

        public GenerationParameters(ISudokuSettings settings)
		{
            this.settings = settings;
		}

		public String BaseDirectory
        {
            get { return baseDirectory; }
            set { baseDirectory = value; }
        }

        public Boolean GenerateBooklet
        {
            get { return generateBooklet; }
            set { generateBooklet = value; }
        }

        public int CurrentProblem
        {
            get { return currentProblem; }
            set { currentProblem = value; }
        }

        public int PreAllocatedValues
        {
            get { return preAllocatedValues; }
            set { preAllocatedValues = value; }
        }

        public int Row
        {
            get { return row; }
            set { row = value; }
        }

        public int Col
        {
            get { return col; }
            set { col = value; }
        }

        public Byte GeneratedValue
        {
            get { return generatedValue; }
            set { generatedValue = value; }
        }

        public Boolean Reset
        {
            get { return reset; }
            set { reset = value; }
        }

        public Int64 TotalPasses
        {
            get { return totalPasses; }
            set { totalPasses = value; }
        }

        public Int64 CheckedProblems
        {
            get { return checkedProblems; }
            set { checkedProblems = value; }
        }

        public void NewValue()
        {
            Random rand = new Random();
            generatedValue = (Byte)rand.Next(1, WinFormsSettings.SudokuSize + 1);
            row = rand.Next(0, WinFormsSettings.SudokuSize);
            col = rand.Next(0, WinFormsSettings.SudokuSize);
        }
    }
}