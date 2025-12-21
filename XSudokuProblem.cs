using System;

using Sudoku.Properties;

namespace Sudoku
{
    internal class XSudokuProblem: BaseProblem
    {
        public new static Char ProblemIdentifier='X';
        public override Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }
        public override Boolean IsTricky { get { return SeverityLevel>Settings.Default.UploadLevelXSudoku; } }

        protected override void createMatrix()
        {
            matrix=new XSudokuMatrix();
        }

        protected override BaseProblem CreateInstance()
        {
            return new XSudokuProblem();
        }

        public override Boolean Resolvable()
        {
            if(!base.Resolvable()) return false;
            return ((XSudokuMatrix)Matrix).CheckDiagonals();
        }
    }
}
