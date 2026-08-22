#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using Sudoku.Core;

namespace Sudoku;

/// <summary>
/// Manages print parameters including cell dimensions, fonts, brushes, and formatting for Sudoku print operations.
/// </summary>
internal class PrintParameters: IDisposable
{
    private readonly ISudokuSettings settings;

    private List<BaseProblem> problems;
    private int currentProblem = 0;
    private int currentSolution = 0;
    private int currentPage = 0;

    private float pageWidthDots = 0;
    private float pageHeightDots = 0;
    private float cellWidthDots = 0;
    private float cellHeightDots = 0;
    private float smallCellWidthDots = 0;
    private float smallCellHeightDots = 0;

    private int printResult = 0;

    static private Pen thickSolidLine = new Pen(Color.Black, 2.5f);
    static private Pen thinSolidLine = new Pen(Color.Black, 2.0f);
    static private Pen tinySolidLine = new Pen(Color.Black, 0.5f);
    static private Pen redTinySolidLine = new Pen(Color.Red, 0.5f);
    static private Pen greenTinySolidLine = new Pen(Color.Green, 0.5f);
    static private Brush solidBrush = new SolidBrush(Color.Black);
    static private Brush lightGraySolidBrush = new SolidBrush(Color.LightGray);
    static private Brush greenSolidBrush = new SolidBrush(Color.Green);
    static private Brush redSolidBrush = new SolidBrush(Color.Red);
    private Font titleFont;
    private Font headerFont;
    private Font largeFont;
    private Font normalFont;
    private Font normalBoldFont;
    private Font smallFont;
    private Font smallBoldFont;
    private Font smallFixedFont;

    static private StringFormat centered = new StringFormat();
    static private StringFormat vertical = new StringFormat();
    static private StringFormat leftBounded = new StringFormat();
    static private StringFormat rightBounded = new StringFormat();

    /// <summary>
    /// Initializes a new instance of the PrintParameters class with fonts and brushes based on application settings.
    /// </summary>
    /// <param name="settings">The application settings containing font and constraint preferences.</param>
    public PrintParameters(ISudokuSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        problems = new List<BaseProblem>();

        int colorIndex = 255 - (int)(255f * ((float)settings.XSudokuContrast / 100f));
        lightGraySolidBrush = new SolidBrush(Color.FromArgb(colorIndex, colorIndex, colorIndex));

        centered.FormatFlags = StringFormatFlags.NoWrap;
        centered.Alignment = StringAlignment.Center;
        centered.LineAlignment = StringAlignment.Center;

        vertical.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.DirectionVertical;
        vertical.Alignment = StringAlignment.Center;
        vertical.LineAlignment = StringAlignment.Center;

        leftBounded.FormatFlags = StringFormatFlags.NoWrap;
        leftBounded.Alignment = StringAlignment.Near;
        leftBounded.LineAlignment = StringAlignment.Far;

        rightBounded.FormatFlags = StringFormatFlags.NoWrap;
        rightBounded.Alignment = StringAlignment.Far;
        rightBounded.LineAlignment = StringAlignment.Far;

        titleFont = new Font(settings.TableFont, 12, FontStyle.Bold);
        headerFont = new Font(settings.TableFont, 24, FontStyle.Bold);
        largeFont = new Font(settings.PrintFont, 14, FontStyle.Regular);
        normalFont = new Font(settings.PrintFont, 10, FontStyle.Regular);
        normalBoldFont = new Font(settings.PrintFont, 10, FontStyle.Bold);
        smallFont = new Font(settings.PrintFont, 6, FontStyle.Regular);
        smallBoldFont = new Font(settings.PrintFont, 6, FontStyle.Bold);
        smallFixedFont = new Font(settings.FixedFont, 6, FontStyle.Regular);

        this.settings = settings;
    }

    /// <summary>
    /// Releases all resources used by the PrintParameters instance.
    /// </summary>
    public void Dispose()
    {
        titleFont.Dispose();
        headerFont.Dispose();
        largeFont.Dispose();
        normalFont.Dispose();
        normalBoldFont.Dispose();
        smallFont.Dispose();
        smallBoldFont.Dispose();
        smallFixedFont.Dispose();
    }

    /// <summary>
    /// Returns a localized error message for the specified print error code.
    /// </summary>
    /// <param name="errorCode">The print error code.</param>
    /// <returns>A descriptive error message.</returns>
    /// <exception cref="ArgumentException">Thrown when the error code is invalid.</exception>
    public static String PrintError(int errorCode)
    {
        String[] errors = { Resources.InvalidSize, Resources.UnknownError };

        if(errorCode < 1 || errorCode > errors.Length)
            throw new ArgumentException(errorCode.ToString());

        return errors[errorCode - 1];
    }

    /// <summary>
    /// Gets or sets the index of the currently printing problem.
    /// </summary>
    public int CurrentProblem
    {
        get { return currentProblem; }
        set { currentProblem = value; }
    }

    /// <summary>
    /// Gets or sets the index of the currently printing solution.
    /// </summary>
    public int CurrentSolution
    {
        get { return currentSolution; }
        set { currentSolution = value; }
    }

    /// <summary>
    /// Gets or sets the printable page width in dots.
    /// </summary>
    public float PageWidthDots
    {
        get { return pageWidthDots; }
        set { pageWidthDots = value; }
    }

    /// <summary>
    /// Gets or sets the printable page height in dots.
    /// </summary>
    public float PageHeightDots
    {
        get { return pageHeightDots; }
        set { pageHeightDots = value; }
    }

    /// <summary>
    /// Gets or sets the width of a single cell in dots for standard-sized problems.
    /// </summary>
    public float CellWidthDots
    {
        get { return cellWidthDots; }
        set { cellWidthDots = value; }
    }

    /// <summary>
    /// Gets or sets the height of a single cell in dots for standard-sized problems.
    /// </summary>
    public float CellHeightDots
    {
        get { return cellHeightDots; }
        set { cellHeightDots = value; }
    }

    /// <summary>
    /// Gets or sets the width of a single cell in dots for small-sized problems (solutions).
    /// </summary>
    public float SmallCellWidthDots
    {
        get { return smallCellWidthDots; }
        set { smallCellWidthDots = value; }
    }

    /// <summary>
    /// Gets or sets the height of a single cell in dots for small-sized problems (solutions).
    /// </summary>
    public float SmallCellHeightDots
    {
        get { return smallCellHeightDots; }
        set { smallCellHeightDots = value; }
    }

    /// <summary>
    /// Gets or sets the print operation result code.
    /// </summary>
    public int PrintResult
    {
        get { return printResult; }
        set { printResult = value; }
    }

    /// <summary>
    /// Gets the list of Sudoku problems queued for printing.
    /// </summary>
    public List<BaseProblem> Problems
    {
        get { return problems; }
    }

    /// <summary>
    /// Gets the list of solutions for a specific problem.
    /// </summary>
    /// <param name="problem">The index of the problem.</param>
    /// <returns>The list of solutions for the specified problem.</returns>
    public List<Solution> Solutions(int problem)
    {
        return problems[problem].Solutions;
    }

    /// <summary>
    /// Gets the thick pen used for drawing primary grid lines.
    /// </summary>
    static public Pen ThickSolidLine
    {
        get { return thickSolidLine; }
    }

    /// <summary>
    /// Gets the thin pen used for drawing secondary grid lines.
    /// </summary>
    static public Pen ThinSolidLine
    {
        get { return thinSolidLine; }
    }

    /// <summary>
    /// Gets the tiny pen used for drawing cell borders.
    /// </summary>
    public static Pen TinySolidLine
    {
        get { return PrintParameters.tinySolidLine; }
    }

    /// <summary>
    /// Gets the red tiny pen used for drawing red cell borders.
    /// </summary>
    public static Pen RedTinySolidLine
    {
        get { return PrintParameters.redTinySolidLine; }
    }

    /// <summary>
    /// Gets the green tiny pen used for drawing green cell borders.
    /// </summary>
    public static Pen GreenTinySolidLine
    {
        get { return PrintParameters.greenTinySolidLine; }
    }

    /// <summary>
    /// Gets the black solid brush used for drawing text and fills.
    /// </summary>
    static public Brush SolidBrush
    {
        get { return solidBrush; }
    }

    /// <summary>
    /// Gets the light gray brush used for highlighting X-Sudoku diagonal cells.
    /// </summary>
    public static Brush LightGraySolidBrush
    {
        get { return PrintParameters.lightGraySolidBrush; }
    }

    /// <summary>
    /// Gets the red brush used for drawing red elements.
    /// </summary>
    static public Brush RedSolidBrush
    {
        get { return PrintParameters.redSolidBrush; }
    }

    /// <summary>
    /// Gets the green brush used for drawing green elements.
    /// </summary>
    static public Brush GreenSolidBrush
    {
        get { return PrintParameters.greenSolidBrush; }
    }

    /// <summary>
    /// Gets the font used for drawing problem titles.
    /// </summary>
    public Font TitleFont
    {
        get { return titleFont; }
    }

    /// <summary>
    /// Gets the font used for drawing page headers.
    /// </summary>
    public Font HeaderFont
    {
        get { return headerFont; }
    }

    /// <summary>
    /// Gets the large font used for drawing values in larger problems.
    /// </summary>
    public Font LargeFont
    {
        get { return largeFont; }
    }

    /// <summary>
    /// Gets the normal font used for drawing values.
    /// </summary>
    public Font NormalFont
    {
        get { return normalFont; }
    }

    /// <summary>
    /// Gets the bold normal font used for fixed values.
    /// </summary>
    public Font NormalBoldFont
    {
        get { return normalBoldFont; }
    }

    /// <summary>
    /// Gets the small font used for drawing values in compact layouts.
    /// </summary>
    public Font SmallFont
    {
        get { return smallFont; }
    }

    /// <summary>
    /// Gets the small bold font used for fixed values in compact layouts.
    /// </summary>
    public Font SmallBoldFont
    {
        get { return smallBoldFont; }
    }

    /// <summary>
    /// Gets the small fixed-width font used for drawing candidates and hints.
    /// </summary>
    public Font SmallFixedFont
    {
        get { return smallFixedFont; }
    }

    /// <summary>
    /// Gets the string format for centered text alignment.
    /// </summary>
    static public StringFormat Centered
    {
        get { return centered; }
    }

    /// <summary>
    /// Gets the string format for vertically aligned text.
    /// </summary>
    static public StringFormat Vertical
    {
        get { return vertical; }
    }

    /// <summary>
    /// Gets the string format for left-aligned text.
    /// </summary>
    static public StringFormat LeftBounded
    {
        get { return leftBounded; }
    }

    /// <summary>
    /// Gets the string format for right-aligned text.
    /// </summary>
    static public StringFormat RightBounded
    {
        get { return rightBounded; }
    }

    /// <summary>
    /// Gets or sets the current page number being printed.
    /// </summary>
    public int CurrentPage
    {
        get { return currentPage; }
        set { currentPage = value; }
    }
}