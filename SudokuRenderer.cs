using System.Drawing;
using Sudoku.Properties;

namespace Sudoku
{
    public static class SudokuRenderer
    {
        internal static void DrawWatchHands(BaseCell value, RectangleF rf, Graphics g, bool showCandidates)
        {
            float diameter = rf.Width / 10;
            float xStart = 0, xEnd = 0, yStart = 0, yEnd = 0;

            for (int i = 1; i <= 9; i++) // Annahme: SudokuSize ist immer 9 für WatchHands Visualisierung
            {
                if ((!showCandidates && (value.Enabled(i) || value.DefinitiveValue == i)) || (showCandidates && (value.GetCandidateMask(i, false) || value.GetCandidateMask(i, true))))
                {
                    if (i == 5)
                        g.FillEllipse(showCandidates ? (value.GetCandidateMask(i, false) ? PrintParameters.GreenSolidBrush : PrintParameters.RedSolidBrush) : PrintParameters.SolidBrush, rf.X + rf.Width / 2, rf.Y + rf.Height / 2, diameter, diameter);
                    else
                    {
                        // Koordinatenberechnung (aus PrintSudoku übernommen)
                        switch (i)
                        {
                            case 1: case 6: xStart = xEnd = rf.X + rf.Width / 2f; break;
                            case 2: case 3: case 4: xStart = rf.X + rf.Width / 10 * 8f; xEnd = rf.X + rf.Width; break;
                            case 7: case 8: case 9: xStart = rf.X + rf.Width / 10 * 2f; xEnd = rf.X; break;
                        }

                        switch (i)
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

        internal static void DrawHints(BaseCell value, RectangleF rf, Graphics g, Font printFont, Color color, bool showCandidates)
        {
            float x = 0, y = 0;
            using (SolidBrush normalBrush = new SolidBrush(color))
            using (SolidBrush candidateBrush = new SolidBrush(Color.Green))
            using (SolidBrush exclusionCandidateBrush = new SolidBrush(Color.Red))
            {
                for (int i = 1; i <= 9; i++)
                {
                    if ((!showCandidates && (value.Enabled(i) || value.DefinitiveValue == i)) || (showCandidates && (value.GetCandidateMask(i, false) || value.GetCandidateMask(i, true))))
                    {
                        // Koordinatenberechnung
                        switch (i)
                        {
                            case 2: case 5: case 8: x = rf.X + rf.Width / 2f - (printFont.SizeInPoints * .75f); break;
                            case 1: case 4: case 7: x = rf.X + printFont.SizeInPoints / 8f; break;
                            case 3: case 6: case 9: x = rf.X + rf.Width - (printFont.SizeInPoints * 1.5f); break;
                        }

                        switch (i)
                        {
                            case 1: case 2: case 3: y = rf.Y + printFont.SizeInPoints / 8f; break;
                            case 4: case 5: case 6: y = rf.Y + rf.Height / 2f - (printFont.SizeInPoints * .75f); break;
                            case 7: case 8: case 9: y = rf.Y + rf.Height - (printFont.SizeInPoints * 1.75f); break;
                        }
                        
                        var brush = showCandidates ? (value.GetCandidateMask(i, false) ? candidateBrush : exclusionCandidateBrush) : normalBrush;
                        g.DrawString(i.ToString(), printFont, brush, x, y);
                    }
                }
            }
        }
    }
}