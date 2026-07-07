using System.Drawing;

namespace Sudoku;

public static class SudokuRenderer
{
    /// <summary>
    /// Render a "watch-hands" style visualization for a single cell.
    /// </summary>
    /// <param name="value">The <see cref="BaseCell"/> whose enabled/candidate values should be visualized.</param>
    /// <param name="rf">The bounding <see cref="RectangleF"/> in which to draw the visualization.</param>
    /// <param name="g">The <see cref="Graphics"/> surface to draw on.</param>
    /// <param name="showCandidates">
    /// When true, render explicit candidate/exclusion indicators (uses candidate and exclusion candidate masks).
    /// When false, render enabled/definitive hints derived from the cell's enabled mask and definitive value.
    /// </param>
    /// <remarks>
    /// The method currently assumes a Sudoku size of 9 for the "watch-hands" layout.
    /// Uses brushes and pens from <c>PrintParameters</c> for coloring. This method does not modify cell state.
    /// </remarks>
    internal static void DrawWatchHands(BaseCell value, RectangleF rf, Graphics g, bool showCandidates)
    {
        float diameter = rf.Width / 10;
        float xStart = 0, xEnd = 0, yStart = 0, yEnd = 0;

        for(int i = 1; i <= 9; i++) // Annahme: SudokuSize ist immer 9 für WatchHands Visualisierung
        {
            if((!showCandidates && (value.Enabled(i) || value.DefinitiveValue == i)) || (showCandidates && (value.GetCandidateMask(i, false) || value.GetCandidateMask(i, true))))
            {
                if(i == 5)
                    g.FillEllipse(showCandidates ? (value.GetCandidateMask(i, false) ? PrintParameters.GreenSolidBrush : PrintParameters.RedSolidBrush) : PrintParameters.SolidBrush, rf.X + rf.Width / 2, rf.Y + rf.Height / 2, diameter, diameter);
                else
                {
                    // Koordinatenberechnung (aus PrintSudoku übernommen)
                    switch(i)
                    {
                    case 1: case 6: xStart = xEnd = rf.X + rf.Width / 2f; break;
                    case 2: case 3: case 4: xStart = rf.X + rf.Width / 10 * 8f; xEnd = rf.X + rf.Width; break;
                    case 7: case 8: case 9: xStart = rf.X + rf.Width / 10 * 2f; xEnd = rf.X; break;
                    }

                    switch(i)
                    {
                    case 1: case 2: case 9: yStart = rf.Y + rf.Height / 10 * 2f; yEnd = rf.Y; break;
                    case 3: case 8: yStart = yEnd = rf.Y + rf.Height / 2f; break;
                    case 4: case 6: case 7: yStart = rf.Y + rf.Height / 10 * 8f; yEnd = rf.Y + rf.Height; break;
                    }
                    g.DrawLine(showCandidates ? (value.GetCandidateMask(i, false) ? PrintParameters.GreenTinySolidLine : PrintParameters.RedTinySolidLine) : PrintParameters.TinySolidLine, xStart, yStart, xEnd, yEnd);
                }
            }
        }
    }

    /// <summary>
    /// Draw small hint digits inside a cell's rectangle to indicate enabled candidates or explicit candidate marks.
    /// </summary>
    /// <param name="value">The <see cref="BaseCell"/> providing candidate/definitive information.</param>
    /// <param name="rf">The bounding <see cref="RectangleF"/> for the cell.</param>
    /// <param name="g">The <see cref="Graphics"/> instance to draw on.</param>
    /// <param name="printFont">Font used for rendering the small hint digits.</param>
    /// <param name="color">Color used to draw normal enabled hints when <paramref name="showCandidates"/> is false.</param>
    /// <param name="showCandidates">
    /// When true, the method draws explicit candidate marks (green for normal candidates, red for exclusion candidates).
    /// When false, it draws enabled/definitive hints using the supplied <paramref name="color"/>.
    /// </param>
    /// <param name="screen">
    /// If true, apply a screen correction factor to the calculated positions (useful for on-screen rendering).
    /// If false, use full-size layout (useful for printing).
    /// </param>
    /// <remarks>
    /// Positions digits in a 3x3 layout within the rectangle and disposes locally created brushes.
    /// The method does not mutate the provided <see cref="BaseCell"/>; it only reads candidate/enable state.
    /// </remarks>
    internal static void DrawHints(BaseCell value, RectangleF rf, Graphics g, Font printFont, Color color, bool showCandidates, bool screen=true)
    {
        float x = 0, y = 0;
        float screenCorrectionFactor = screen ? 0.9f : 1;
        using(SolidBrush normalBrush = new SolidBrush(color))
        using(SolidBrush candidateBrush = new SolidBrush(Color.Green))
        using(SolidBrush exclusionCandidateBrush = new SolidBrush(Color.Red))
        {
            for(int i = 1; i <= 9; i++)
            {
                if((!showCandidates && (value.Enabled(i) || value.DefinitiveValue == i)) || (showCandidates && (value.GetCandidateMask(i, false) || value.GetCandidateMask(i, true))))
                {
                    // Koordinatenberechnung
                    switch(i)
                    {
                    case 2: case 5: case 8: x = rf.X + rf.Width / 2f * screenCorrectionFactor - (printFont.SizeInPoints * .75f); break;
                    case 1: case 4: case 7: x = rf.X + printFont.SizeInPoints / 8f * screenCorrectionFactor; break;
                    case 3: case 6: case 9: x = rf.X + rf.Width * screenCorrectionFactor - (printFont.SizeInPoints * 1.5f); break;
                    }

                    switch(i)
                    {
                    case 1: case 2: case 3: y = rf.Y + (printFont.SizeInPoints / 8f); break;
                    case 4: case 5: case 6: y = rf.Y + rf.Height / 2f * screenCorrectionFactor - (printFont.SizeInPoints * .75f); break;
                    case 7: case 8: case 9: y = rf.Y + rf.Height * screenCorrectionFactor - (printFont.SizeInPoints * 1.75f); break;
                    }

                    var brush = showCandidates ? (value.GetCandidateMask(i, false) ? candidateBrush : exclusionCandidateBrush) : normalBrush;
                    g.DrawString(i.ToString(), printFont, brush, x, y);
                }
            }
        }
    }
}