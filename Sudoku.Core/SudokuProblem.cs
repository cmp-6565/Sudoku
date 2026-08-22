using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sudoku.Core;

/// <summary>
/// Represents a standard 9x9 Sudoku problem with a 3x3 box constraint.
/// Manages problem solving, solution finding, and minimization for standard Sudoku puzzles.
/// </summary>
public class SudokuProblem: BaseProblem
{
    /// <summary>
    /// The identifier character for standard Sudoku problems.
    /// </summary>
    public new static Char ProblemIdentifier = '9';

    /// <summary>
    /// Gets the Sudoku type identifier for this problem.
    /// </summary>
    public override Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }

    /// <summary>
    /// The severity limit for standard Sudoku problems.
    /// </summary>
    public new static int Limit = 25;

    /// <summary>
    /// Gets the minimize limit for this problem type.
    /// </summary>
    public override int MinimizeLimit { get { return Limit; } }

    /// <summary>
    /// Determines if this problem is considered tricky based on its severity level.
    /// </summary>
    public override Boolean IsTricky { get { return SeverityLevel > settings.UploadLevelNormalSudoku; } }

    /// <summary>
    /// Initializes a new instance of the SudokuProblem class with the specified settings.
    /// </summary>
    /// <param name="settings">The application settings for controlling solver behavior.</param>
    public SudokuProblem(ISudokuEngineSettings settings) : base(settings)
    {
    }

    /// <summary>
    /// Creates the matrix for a standard Sudoku problem.
    /// </summary>
    protected override void createMatrix()
    {
        cellMatrix = new SudokuMatrix();
    }

    /// <summary>
    /// Creates a new instance of SudokuProblem with the same settings.
    /// </summary>
    /// <returns>A new SudokuProblem instance.</returns>
    protected override BaseProblem CreateInstance()
    {
        return new SudokuProblem(settings);
    }
}
