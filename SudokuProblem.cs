using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sudoku;

internal class SudokuProblem: BaseProblem
{
    public new static Char ProblemIdentifier = '9';
    public override Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }
    public new static int Limit = 25;
    public override int MinimizeLimit { get { return Limit; } }
    public override Boolean IsTricky { get { return SeverityLevel > settings.UploadLevelNormalSudoku; } }

    public SudokuProblem(ISudokuSettings settings) : base(settings)
    {
    }
    protected override void createMatrix()
    {
        matrix = new SudokuMatrix();
    }

    protected override BaseProblem CreateInstance()
    {
        return new SudokuProblem(settings);
    }
}
