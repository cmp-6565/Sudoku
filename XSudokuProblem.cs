using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sudoku;

internal class XSudokuProblem: BaseProblem
{
    public new static Char ProblemIdentifier = 'X';
    public override Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }
    public new static int Limit = 19;
    public override int MinimizeLimit { get { return Limit; } }
    public override Boolean IsTricky { get { return SeverityLevel > settings.UploadLevelXSudoku; } }

    public XSudokuProblem(ISudokuSettings settings) : base(settings)
    {
    }
    protected override void createMatrix()
    {
        matrix = new XSudokuMatrix();
    }

    protected override BaseProblem CreateInstance()
    {
        return new XSudokuProblem(settings);
    }

    public override Boolean Resolvable()
    {
        if(!base.Resolvable()) return false;
        return ((XSudokuMatrix)Matrix).CheckDiagonals();
    }
}
