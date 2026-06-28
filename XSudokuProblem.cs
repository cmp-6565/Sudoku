using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sudoku;

/// <summary>
/// Represents an X-Sudoku (Diagonal Sudoku) problem with additional diagonal constraints.
/// Manages problem solving, solution finding, and minimization for X-Sudoku puzzles.
/// </summary>
internal class XSudokuProblem: BaseProblem
{
    /// <summary>
    /// The identifier character for X-Sudoku problems.
    /// </summary>
    public new static Char ProblemIdentifier = 'X';

    /// <summary>
    /// Gets the Sudoku type identifier for this problem.
    /// </summary>
    public override Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }

    /// <summary>
    /// The severity limit for X-Sudoku problems.
    /// </summary>
    public new static int Limit = 25;

    /// <summary>
    /// Gets the minimize limit for this problem type.
    /// </summary>
    public override int MinimizeLimit { get { return Limit; } }

    /// <summary>
    /// Determines if this problem is considered tricky based on its severity level.
    /// </summary>
    public override Boolean IsTricky { get { return SeverityLevel > settings.UploadLevelXSudoku; } }

    /// <summary>
    /// Initializes a new instance of the XSudokuProblem class with the specified settings.
    /// </summary>
    /// <param name="settings">The application settings for controlling solver behavior.</param>
    public XSudokuProblem(ISudokuSettings settings) : base(settings)
    {
    }

    /// <summary>
    /// Creates the matrix for an X-Sudoku problem with diagonal constraints.
    /// </summary>
    protected override void createMatrix()
    {
        matrix = new XSudokuMatrix();
    }

    /// <summary>
    /// Creates a new instance of XSudokuProblem with the same settings.
    /// </summary>
    /// <returns>A new XSudokuProblem instance.</returns>
    protected override BaseProblem CreateInstance()
    {
        return new XSudokuProblem(settings);
    }

    /// <summary>
    /// Determines if the current problem state is resolvable, including diagonal constraints.
    /// </summary>
    /// <returns>True if the problem is resolvable with valid diagonal constraints; false otherwise.</returns>
    public override Boolean Resolvable()
    {
        if(!base.Resolvable()) return false;
        return ((XSudokuMatrix)Matrix).CheckDiagonals();
    }
}
