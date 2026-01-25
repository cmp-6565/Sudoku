using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    internal class SudokuPrinterService
    {
        private readonly ISudokuSettings settings;

        private System.Drawing.Printing.PrintDocument printSudoku;
        internal PrintParameters printParameters;
        private CultureInfo cultureInfo;
        public int SudokuSize { get; set; }
        public Boolean ShowCandidates { get; set; }

        public int NumberOfProblems => printParameters.Problems.Count;

        public SudokuPrinterService(int sudokuSize, ISudokuSettings settings)
        {
            printSudoku = new System.Drawing.Printing.PrintDocument();
            printSudoku.PrintPage += PrintSudokuEvent;
            cultureInfo = Thread.CurrentThread.CurrentUICulture;
            printParameters = new PrintParameters(settings);
            SudokuSize = sudokuSize;
            this.settings = settings;
        }
        public void Dispose()
        {
            printSudoku.PrintPage -= PrintSudokuEvent;
            printSudoku.Dispose();
        }
        public System.Drawing.Printing.PrintDocument Document => printSudoku;

        public void AddProblem(BaseProblem problem)
        {
            printParameters.Problems.Add(problem);
        }
        public void ClearProblems()
        {
            printParameters.Problems.Clear();
        }
        public void SortProblems()
        {
            printParameters.Problems.Sort();
        }
        public String PrintErrorMessage { get { return PrintParameters.PrintError(PrintResult); } }

        public int PrintResult { get { return printParameters.PrintResult; } }

        private void PrintSudokuEvent(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            RectangleF rf;
            float currentX = e.PageSettings.PrintableArea.Left + printSudoku.DefaultPageSettings.Margins.Left;
            float currentY = e.PageSettings.PrintableArea.Top + printSudoku.DefaultPageSettings.Margins.Top;
            float horizontalOffset = SudokuSize + 1;
            float verticalOffset = SudokuSize + 3;

            printParameters.PrintResult = 0;

            printParameters.PageWidthDots = e.PageSettings.PrintableArea.Width - printSudoku.DefaultPageSettings.Margins.Right - printSudoku.DefaultPageSettings.Margins.Left;
            printParameters.PageHeightDots = e.PageSettings.PrintableArea.Height - printSudoku.DefaultPageSettings.Margins.Top - printSudoku.DefaultPageSettings.Margins.Bottom;

            printParameters.CellWidthDots = (printParameters.CellHeightDots = printParameters.PageWidthDots / (settings.HorizontalProblems * horizontalOffset));
            printParameters.SmallCellWidthDots = (printParameters.SmallCellHeightDots = printParameters.PageWidthDots / (settings.HorizontalSolutions * verticalOffset));

            Boolean printSolutions = true;

            // Draw Title and copyright information on every page
            g.DrawString(AssemblyInfo.AssemblyCompany+ " " + AssemblyInfo.AssemblyProduct, printParameters.HeaderFont, PrintParameters.SolidBrush, new RectangleF(currentX, currentY, printParameters.PageWidthDots, printParameters.HeaderFont.GetHeight(g)), PrintParameters.Centered);
            g.DrawString(AssemblyInfo.AssemblyCopyright, printParameters.SmallFont, PrintParameters.SolidBrush, new RectangleF(currentX + printParameters.PageWidthDots, 0, printParameters.CellWidthDots, printSudoku.DefaultPageSettings.PaperSize.Height), PrintParameters.Vertical);
            g.DrawString(AssemblyInfo.AssemblyProduct + " " + Resources.Version + AssemblyInfo.AssemblyVersion, printParameters.SmallFont, PrintParameters.SolidBrush, (rf = new RectangleF(currentX, printSudoku.DefaultPageSettings.PaperSize.Height - printSudoku.DefaultPageSettings.Margins.Bottom, printParameters.PageWidthDots, printParameters.CellHeightDots)), PrintParameters.LeftBounded);
            g.DrawString(Resources.Page + (++printParameters.CurrentPage).ToString("n0", cultureInfo), printParameters.SmallFont, PrintParameters.SolidBrush, rf, PrintParameters.RightBounded);
            currentY += 2 * printParameters.HeaderFont.GetHeight(g);

            if(printParameters.CurrentProblem < printParameters.Problems.Count)
            {
                // calculate the numbers of Problems/page
                int problemsHorizontal = (int)(Math.Round(printParameters.PageWidthDots / (horizontalOffset * printParameters.CellWidthDots), 0));
                int problemsVertical = (int)(Math.Round((printParameters.PageHeightDots - currentY) / (verticalOffset * printParameters.CellHeightDots), 0));
                if(problemsHorizontal < 1 || problemsVertical < 1)
                {
                    printParameters.PrintResult = 1;
                    e.HasMorePages = false;
                    e.Cancel = true;
                    return;
                }

                for(int i = 0; i < problemsVertical; i++)
                {
                    currentX = e.PageSettings.PrintableArea.Left + printSudoku.DefaultPageSettings.Margins.Left + (printParameters.PageWidthDots - (problemsHorizontal * horizontalOffset * printParameters.CellWidthDots)) / 2f;
                    for(int j = 0; j < problemsHorizontal; j++)
                    {
                        if(printParameters.CurrentProblem < printParameters.Problems.Count)
                            PrintProblem(currentX, currentY, g);
                        currentX += horizontalOffset * printParameters.CellWidthDots;
                    }
                    currentY += verticalOffset * printParameters.CellHeightDots;
                }
                printSolutions = printParameters.Problems.Count == 1;
            }

            e.HasMorePages = settings.PrintSolution || printParameters.CurrentProblem < printParameters.Problems.Count;

            if(settings.PrintSolution && printSolutions)
            {
                if(printParameters.Problems.Count == 1)
                {
                    currentX = (e.PageSettings.PrintableArea.Left + printSudoku.DefaultPageSettings.Margins.Left + (printParameters.PageWidthDots - (horizontalOffset * printParameters.CellWidthDots)) / 2f) + (horizontalOffset * printParameters.CellWidthDots);
                    currentY = e.PageSettings.PrintableArea.Top + printSudoku.DefaultPageSettings.Margins.Top + 2 * printParameters.HeaderFont.GetHeight(g);
                    if(currentX + horizontalOffset * printParameters.SmallCellWidthDots > e.PageSettings.PrintableArea.Left + printSudoku.DefaultPageSettings.Margins.Left + printParameters.PageWidthDots)
                    {
                        currentX = e.PageSettings.PrintableArea.Left + printSudoku.DefaultPageSettings.Margins.Left + (printParameters.PageWidthDots / 2f) - (SudokuSize / 2f * printParameters.SmallCellWidthDots);
                        currentY += verticalOffset * printParameters.CellHeightDots;
                    }
                    else
                        currentY += (SudokuSize * printParameters.CellHeightDots / 2f) - (SudokuSize * printParameters.SmallCellHeightDots / 2f);
                    PrintSolution(currentX, currentY, g);
                }
                else
                {
                    g.DrawString(Resources.Solution + Resources.Plural, printParameters.TitleFont, PrintParameters.SolidBrush, new RectangleF(currentX, currentY - 1.5f * printParameters.TitleFont.GetHeight(g), printParameters.PageWidthDots, printParameters.TitleFont.GetHeight(g)), PrintParameters.Centered);

                    // calculate the numbers of Solutions/page
                    int problemsHorizontal = (int)(Math.Round(printParameters.PageWidthDots / (horizontalOffset * printParameters.SmallCellWidthDots), 1));
                    int problemsVertical = (int)(Math.Round((printParameters.PageHeightDots - currentY) / (verticalOffset * printParameters.SmallCellHeightDots), 1));

                    for(int i = 0; i < problemsVertical; i++)
                    {
                        currentX = e.PageSettings.PrintableArea.Left + printSudoku.DefaultPageSettings.Margins.Left + (printParameters.PageWidthDots - (problemsHorizontal * horizontalOffset * printParameters.SmallCellWidthDots)) / 2f;
                        for(int j = 0; j < problemsHorizontal; j++)
                        {
                            if(printParameters.CurrentSolution < printParameters.Problems.Count)
                                PrintSolution(currentX, currentY, g);
                            currentX += horizontalOffset * printParameters.SmallCellWidthDots;
                        }
                        currentY += verticalOffset * printParameters.SmallCellHeightDots;
                    }
                }
                e.HasMorePages = printParameters.CurrentSolution < printParameters.Problems.Count;
            }
        }

        private void PrintProblem(float x, float y, Graphics g)
        {
            bool showCandidatesMode = !settings.ShowHints;
            int row = 0;
            int col = 0;
            Font printFont = (settings.HorizontalProblems > 3 ? printParameters.SmallFont : (settings.HorizontalProblems < 2 ? printParameters.LargeFont : printParameters.NormalFont));
            Font hintFont = (printParameters.CellWidthDots < 45 ? printParameters.SmallFixedFont : printParameters.NormalFont);

            BaseProblem currentProblem = printParameters.Problems[printParameters.CurrentProblem];
            RectangleF rf = new RectangleF(x, y + SudokuSize * printParameters.CellHeightDots + printParameters.TitleFont.GetHeight(g), SudokuSize * printParameters.CellWidthDots, printParameters.CellHeightDots);

            if(currentProblem.NumberOfSolutions == 0)
                g.DrawString(Resources.TitleNotResolvable, printParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            else
                if(currentProblem.NumberOfSolutions == 1)
            {
                String problemTitle = (printParameters.Problems.Count > 1 ? String.Format(cultureInfo, Resources.Problem, printParameters.CurrentProblem + 1) + ": " : String.Empty) + currentProblem.SeverityLevelText + (settings.PrintInternalSeverity ? " (" + currentProblem.SeverityLevel + ")" : "");
                g.DrawString(problemTitle, printParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            }
            else
                g.DrawString(Resources.MoreThanOne, printParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);

            String subTitle = String.Empty;
            if(String.IsNullOrEmpty(subTitle = currentProblem.Comment))
            {
                subTitle = currentProblem.Filename;
                if(printParameters.SmallFixedFont.SizeInPoints * subTitle.Length > SudokuSize * printParameters.CellWidthDots)
                    subTitle = "..." + subTitle.Substring(subTitle.Length - (int)(SudokuSize * printParameters.CellWidthDots / printParameters.SmallFixedFont.SizeInPoints) - 3);
            }
            else
            {
                if(printParameters.SmallFixedFont.SizeInPoints * subTitle.Length > SudokuSize * printParameters.CellWidthDots)
                    subTitle = subTitle.Substring(0, (int)(SudokuSize * printParameters.CellWidthDots / printParameters.SmallFixedFont.SizeInPoints) - 3) + "...";
            }
            g.DrawString(subTitle, printParameters.SmallFixedFont, PrintParameters.SolidBrush, new RectangleF(x, y + SudokuSize * printParameters.CellHeightDots + .5f * printParameters.SmallFixedFont.GetHeight(g), SudokuSize * printParameters.CellWidthDots, printParameters.SmallFixedFont.GetHeight(g)), PrintParameters.Centered);

            if(currentProblem is XSudokuProblem)
                for(row = 0; row < SudokuSize; row++)
                    for(col = 0; col < SudokuSize; col++)
                        if(row == col || row + col == SudokuSize - 1)
                            g.FillRectangle(PrintParameters.LightGraySolidBrush, x + col * printParameters.CellWidthDots, y + row * printParameters.CellHeightDots, printParameters.CellWidthDots, printParameters.CellHeightDots);

            for(row = 0; row < SudokuSize / 3; row++)
                for(col = 0; col < SudokuSize / 3; col++)
                    g.DrawRectangle(PrintParameters.ThickSolidLine, x + col * printParameters.CellWidthDots * 3, y + row * printParameters.CellHeightDots * 3, printParameters.CellWidthDots * 3, printParameters.CellHeightDots * 3);
            for(row = 0; row < SudokuSize; row++)
                for(col = 0; col < SudokuSize; col++)
                {
                    g.DrawRectangle(PrintParameters.TinySolidLine, x + col * printParameters.CellWidthDots, y + row * printParameters.CellHeightDots, printParameters.CellWidthDots, printParameters.CellHeightDots);

                    RectangleF cell = new RectangleF(x + col * printParameters.CellWidthDots, y + row * printParameters.CellHeightDots, printParameters.CellWidthDots, printParameters.CellHeightDots);
                    if(currentProblem.GetValue(row, col) != Values.Undefined && !currentProblem.ComputedValue(row, col))
                        g.DrawString(currentProblem.GetValue(row, col).ToString(cultureInfo).Trim(), printFont, PrintParameters.SolidBrush, cell, PrintParameters.Centered);
                    else if(settings.PrintHints || ShowCandidates)
                        if(settings.UseWatchHandHints || printParameters.CellWidthDots < 30)
                            SudokuRenderer.DrawWatchHands(currentProblem.Matrix.Cell(row, col), cell, g, showCandidatesMode);
                        else
                            SudokuRenderer.DrawHints(currentProblem.Matrix.Cell(row, col), rf, g, printFont, Color.Black, showCandidatesMode);
                }
            printParameters.CurrentProblem++;
        }
        private void PrintSolution(float x, float y, Graphics g)
        {
            int row = 0;
            int col = 0;

            RectangleF rf = new RectangleF(x, y + SudokuSize * printParameters.SmallCellHeightDots, SudokuSize * printParameters.SmallCellWidthDots, printParameters.TitleFont.GetHeight(g));

            if(printParameters.Problems[printParameters.CurrentSolution].NumberOfSolutions > 1)
                g.DrawString(Resources.SolutionOne, printParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            else if(printParameters.Problems[printParameters.CurrentSolution].NumberOfSolutions == 0)
                g.DrawString(Resources.InvalidProblem, printParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            else
                g.DrawString(printParameters.Problems.Count > 1 ? String.Format(cultureInfo, Resources.Problem, printParameters.CurrentSolution + 1) : Resources.Solution, printParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);

            if(printParameters.Problems[printParameters.CurrentSolution] is XSudokuProblem)
                for(row = 0; row < SudokuSize; row++)
                    for(col = 0; col < SudokuSize; col++)
                        if(row == col || row + col == SudokuSize - 1)
                            g.FillRectangle(PrintParameters.LightGraySolidBrush, x + col * printParameters.SmallCellWidthDots, y + row * printParameters.SmallCellHeightDots, printParameters.SmallCellWidthDots, printParameters.SmallCellHeightDots);

            for(row = 0; row < SudokuSize / 3; row++)
                for(col = 0; col < SudokuSize / 3; col++)
                    g.DrawRectangle(PrintParameters.ThinSolidLine, x + col * printParameters.SmallCellWidthDots * 3, y + row * printParameters.SmallCellHeightDots * 3, printParameters.SmallCellWidthDots * 3, printParameters.SmallCellHeightDots * 3);
            for(row = 0; row < SudokuSize; row++)
                for(col = 0; col < SudokuSize; col++)
                    g.DrawRectangle(PrintParameters.TinySolidLine, x + col * printParameters.SmallCellWidthDots, y + row * printParameters.SmallCellHeightDots, printParameters.SmallCellWidthDots, printParameters.SmallCellHeightDots);

            if(printParameters.Problems[printParameters.CurrentSolution].NumberOfSolutions > 0)
            {
                Solution solution = printParameters.Solutions(printParameters.CurrentSolution)[0];

                System.Drawing.Drawing2D.Matrix rotateMatrix = new System.Drawing.Drawing2D.Matrix();
                PointF f = new System.Drawing.PointF(x + (float)(SudokuSize / 2) * printParameters.SmallCellWidthDots, y + (float)(SudokuSize / 2) * printParameters.SmallCellHeightDots);
                rotateMatrix.RotateAt(180, f);
                g.Transform = rotateMatrix;
                for(row = 0; row < SudokuSize; row++)
                    for(col = 0; col < SudokuSize; col++)
                    {
                        g.DrawString(solution.GetValue(row, col).ToString(cultureInfo).Trim(),
                            printParameters.Problems[printParameters.CurrentSolution].FixedValue(row, col) && !printParameters.Problems[printParameters.CurrentSolution].Matrix.ComputedValue(row, col) ? printParameters.SmallBoldFont : printParameters.SmallFont,
                            PrintParameters.SolidBrush, new RectangleF(x + ((col - 1) * printParameters.SmallCellWidthDots), y + ((row - 1) * printParameters.SmallCellHeightDots), printParameters.SmallCellWidthDots, printParameters.SmallCellHeightDots), PrintParameters.Centered);
                    }
                rotateMatrix.RotateAt(180, f);
                g.Transform = rotateMatrix;
            }
            else
            {
                g.DrawLine(PrintParameters.ThinSolidLine, x, y, x + 9 * printParameters.SmallCellWidthDots, y + 9 * printParameters.SmallCellHeightDots);
                g.DrawLine(PrintParameters.ThinSolidLine, x, y + 9 * printParameters.SmallCellHeightDots, x + 9 * printParameters.SmallCellWidthDots, y);
            }
            printParameters.CurrentSolution++;
        }
    }
}
