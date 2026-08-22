#nullable enable
using System;

namespace Sudoku.Core;

/// <summary>
/// Holds and manages parameters for Sudoku problem generation.
/// </summary>
public class GenerationParameters
{
    private readonly ISudokuEngineSettings settings;

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
    private Random rand = new Random(unchecked((int)DateTime.Now.Ticks));

    /// <summary>
    /// Initializes a new instance of the GenerationParameters class with the specified settings.
    /// </summary>
    /// <param name="settings">The application settings to use for generation.</param>
    public GenerationParameters(ISudokuEngineSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.settings = settings;
    }

    /// <summary>
    /// Gets or sets the base directory for generated Sudoku files.
    /// </summary>
    public String BaseDirectory
    {
        get { return baseDirectory; }
        set { baseDirectory = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to generate a Sudoku booklet.
    /// </summary>
    public Boolean GenerateBooklet
    {
        get { return generateBooklet; }
        set { generateBooklet = value; }
    }

    /// <summary>
    /// Gets or sets the current problem number being processed.
    /// </summary>
    public int CurrentProblem
    {
        get { return currentProblem; }
        set { currentProblem = value; }
    }

    /// <summary>
    /// Gets or sets the number of pre-allocated values for generation.
    /// </summary>
    public int PreAllocatedValues
    {
        get { return preAllocatedValues; }
        set { preAllocatedValues = value; }
    }

    /// <summary>
    /// Gets or sets the current row coordinate being processed.
    /// </summary>
    public int Row
    {
        get { return row; }
        set { row = value; }
    }

    /// <summary>
    /// Gets or sets the current column coordinate being processed.
    /// </summary>
    public int Col
    {
        get { return col; }
        set { col = value; }
    }

    /// <summary>
    /// Gets or sets the generated value (1-9).
    /// </summary>
    public Byte GeneratedValue
    {
        get { return generatedValue; }
        set { generatedValue = value; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to reset the generation parameters.
    /// </summary>
    public Boolean Reset
    {
        get { return reset; }
        set { reset = value; }
    }

    /// <summary>
    /// Gets or sets the total number of passes performed during generation.
    /// </summary>
    public Int64 TotalPasses
    {
        get { return totalPasses; }
        set { totalPasses = value; }
    }

    /// <summary>
    /// Gets or sets the number of problems checked during generation.
    /// </summary>
    public Int64 CheckedProblems
    {
        get { return checkedProblems; }
        set { checkedProblems = value; }
    }

    /// <summary>
    /// Generates a new random value and coordinates for Sudoku generation.
    /// </summary>
    public void NewValue()
    {
        generatedValue = (Byte)rand.Next(1, SudokuGrid.SudokuSize + 1);
        row = rand.Next(0, SudokuGrid.SudokuSize);
        col = rand.Next(0, SudokuGrid.SudokuSize);
    }
}