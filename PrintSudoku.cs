using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    public partial class SudokuForm: Form
    {
        private void PrintDocument()
        {
            try
            {
                printSudokuDialog.Document.Print();
            }
            catch(Win32Exception)
            {
                if(printParameters.PrintResult != 0)
                    MessageBox.Show(this, Resources.NotPrinted + Environment.NewLine + PrintParameters.PrintError(printParameters.PrintResult));
            }
            catch(System.Runtime.InteropServices.ExternalException)
            {
                // This happens in the case the user presses "Cancel" while printing
            }
            catch(Exception)
            {
                throw;
            }
            finally
            {
                // Known problem: The FinePrint-Dialog is not deleted from the screen:-(
                printSudokuDialog.Document.Dispose();
                printSudokuDialog.Dispose();
            }
        }

        private async Task PrintDialog()
        {
            if(!SyncProblemWithGUI(true))
            {
                MessageBox.Show(this, Resources.InvalidProblem + Environment.NewLine + Resources.PrintNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            BaseProblem tmp = controller.CurrentProblem.Clone();

            DisplayValues(controller.CurrentProblem.Matrix);
            SolveProblem();

            if(controller.CurrentProblem.SolverTask != null && !controller.CurrentProblem.SolverTask.IsCompleted)
                controller.CurrentProblem.SolverTask.Wait();

            ResetDetachedProcess();
            ResetTexts();
            DisplayValues(tmp.Matrix);

            if(!controller.CurrentProblem.Aborted)
            {
                // This local variable is needed since the _global_ variable <code>showCandidates</code> might be used reset by other functions before the printout has started
                Boolean sc;
                if((sc = controller.CurrentProblem.HasCandidates()) && Settings.Default.PrintHints)
                    sc = MessageBox.Show(this, Resources.PrintCandidates, ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes;

                printSudokuDialog.UseEXDialog = true;
                if(printSudokuDialog.ShowDialog() == DialogResult.OK)
                {
                    printParameters = new PrintParameters();
                    controller.CurrentProblem.ResetMatrix();
                    printParameters.Problems.Add(controller.CurrentProblem);
                    showCandidates = sc;
                    PrintDocument();
                }
            }

            controller.SyncWithGui(tmp);
        }

        private void PrintBooklet()
        {
            showCandidates = false;
            if(printParameters.Problems.Count < 1)
                MessageBox.Show(this, Resources.NoProblems);
            else
            {
                printSudokuDialog.UseEXDialog = true;
                if(printSudokuDialog.ShowDialog() == DialogResult.OK)
                {
                    printParameters.Problems.Sort();
                    PrintDocument();
                }
            }
        }

        private void GenerateProblems4Booklet()
        {
            if(!UnsavedChanges()) return;

            if(Settings.Default.AutoSaveBooklet)
            {
                if(!Directory.Exists(Settings.Default.ProblemDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(Settings.Default.ProblemDirectory);
                    }
                    catch
                    {
                        MessageBox.Show(this, String.Format(cultureInfo, Resources.CreateDirectoryFailed, Settings.Default.ProblemDirectory), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Settings.Default.AutoSaveBooklet = false;
                    }
                }
            }
            if(Settings.Default.AutoSaveBooklet)
            {
                generationParameters.BaseDirectory = Settings.Default.ProblemDirectory + Path.DirectorySeparatorChar + "Booklet-" + DateTime.Now.ToString("yyyy.MM.dd-hh-mm", cultureInfo);
                try
                {
                    Directory.CreateDirectory(generationParameters.BaseDirectory);
                }
                catch
                {
                    MessageBox.Show(this, String.Format(cultureInfo, Resources.CreateDirectoryFailed, generationParameters.BaseDirectory), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Settings.Default.AutoSaveBooklet = false;
                }
            }
            printParameters = new PrintParameters();
            GenerateProblems(Settings.Default.BookletSizeNew, generationParameters.NewSudokuType());
        }

        private void LoadProblems4Booklet()
        {
            printParameters = new PrintParameters();

            selectBookletDirectory.SelectedPath = Settings.Default.ProblemDirectory;
            selectBookletDirectory.ShowNewFolderButton = false;

            if(selectBookletDirectory.ShowDialog() == DialogResult.OK)
            {
                DateTime interactiveStartReset = DateTime.MinValue;
                List<String> filenames = new List<string>();

                DisableGUI();

                if(solvingTimer.Enabled)
                    interactiveStartReset = interactiveStart;

                abortRequested = false;
                LoadProblemFilenames(new DirectoryInfo(selectBookletDirectory.SelectedPath), filenames);
                if(!abortRequested)
                {

                    int totalNumber = filenames.Count;
                    if(totalNumber < 1)
                        MessageBox.Show(this, Resources.NoProblems);
                    else
                    {
                        int count = LoadProblems(filenames);
                        if(!abortRequested)
                        {
                            sudokuStatusBarText.Text = String.Format(cultureInfo, Resources.ProblemsLoaded, count, totalNumber);
                            sudokuStatusBar.Update();

                            PrintBooklet();
                        }
                    }
                }
                if(interactiveStartReset != DateTime.MinValue)
                {
                    interactiveStart = interactiveStartReset;
                    solvingTimer.Start();
                }
                if(!applicationExiting)
                {
                    CurrentStatus(true);
                    sudokuStatusBarText.Text = Resources.Ready;
                    EnableGUI();
                }
            }
        }

        private void LoadProblemFilenames(DirectoryInfo directoryInfo, List<String> filenames)
        {
            sudokuStatusBarText.Text = String.Format(cultureInfo, Resources.LoadingFiles);
            sudokuStatusBar.Update();

            // avoid Application.DoEvents(); — respect cooperative cancellation
            if(abortRequested) return;

            foreach(FileInfo fileInfo in directoryInfo.GetFiles())
                filenames.Add(fileInfo.FullName);

            foreach(DirectoryInfo di in directoryInfo.GetDirectories())
                LoadProblemFilenames(di, filenames);
        }

        private int LoadProblems(List<String> filenames)
        {
            Boolean ready = false;
            Random rand = new Random();

            BaseProblem tmp = controller.CurrentProblem.Clone();

            while(!ready)
            {
                int problemNumber = rand.Next(0, filenames.Count - 1);
                try
                {
                    SudokuController bookletController = new SudokuController(filenames[problemNumber], false);
                    if(bookletController.CurrentProblem != null && (SeverityLevelInt(bookletController.CurrentProblem) & Settings.Default.SeverityLevel) != 0)
                    {
                        bookletController.CurrentProblem.FindSolutions(2);

                        if(bookletController.CurrentProblem.SolverTask != null && !bookletController.CurrentProblem.SolverTask.IsCompleted)
                            bookletController.CurrentProblem.SolverTask.Wait();

                        if(bookletController.CurrentProblem.NumberOfSolutions == 1)
                        {
                            bookletController.CurrentProblem.ResetMatrix();
                            bookletController.CurrentProblem.Filename = filenames[problemNumber];
                            printParameters.Problems.Add(bookletController.CurrentProblem);

                            int remainder;
                            Math.DivRem(printParameters.Problems.Count / 10, 25, out remainder);
                            sudokuStatusBarText.Text = Resources.LoadingFiles.PadRight(Resources.LoadingFiles.Length + remainder, '.');
                            sudokuStatusBar.Update();

                            // cooperative cancellation check instead of Application.DoEvents
                            if(abortRequested) break;
                        }
                    }
                }
                catch
                {
                    // do nothing
                }

                filenames.RemoveAt(problemNumber);
                ready = (printParameters.Problems.Count == Settings.Default.BookletSizeExisting && !Settings.Default.BookletSizeUnlimited) || filenames.Count == 0 || abortRequested;
            }
            controller.SyncWithGui(tmp);

            return printParameters.Problems.Count;
        }
        // Printing
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

            printParameters.CellWidthDots = (printParameters.CellHeightDots = printParameters.PageWidthDots / (Settings.Default.HorizontalProblems * horizontalOffset));
            printParameters.SmallCellWidthDots = (printParameters.SmallCellHeightDots = printParameters.PageWidthDots / (Settings.Default.HorizontalSolutions * verticalOffset));

            Boolean printSolutions = true;

            // Draw Title and copyright information on every page
            g.DrawString(CompanyName + " " + ProductName, PrintParameters.HeaderFont, PrintParameters.SolidBrush, new RectangleF(currentX, currentY, printParameters.PageWidthDots, PrintParameters.HeaderFont.GetHeight(g)), PrintParameters.Centered);
            g.DrawString(Copyright, PrintParameters.SmallFont, PrintParameters.SolidBrush, new RectangleF(currentX + printParameters.PageWidthDots, 0, printParameters.CellWidthDots, printSudoku.DefaultPageSettings.PaperSize.Height), PrintParameters.Vertical);
            g.DrawString(ProductName + " " + Resources.Version + Version, PrintParameters.SmallFont, PrintParameters.SolidBrush, (rf = new RectangleF(currentX, printSudoku.DefaultPageSettings.PaperSize.Height - printSudoku.DefaultPageSettings.Margins.Bottom, printParameters.PageWidthDots, printParameters.CellHeightDots)), PrintParameters.LeftBounded);
            g.DrawString(Resources.Page + (++printParameters.CurrentPage).ToString("n0", cultureInfo), PrintParameters.SmallFont, PrintParameters.SolidBrush, rf, PrintParameters.RightBounded);
            currentY += 2 * PrintParameters.HeaderFont.GetHeight(g);

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

            e.HasMorePages = Settings.Default.PrintSolution || printParameters.CurrentProblem < printParameters.Problems.Count;

            if(Settings.Default.PrintSolution && printSolutions)
            {
                if(printParameters.Problems.Count == 1)
                {
                    currentX = (e.PageSettings.PrintableArea.Left + printSudoku.DefaultPageSettings.Margins.Left + (printParameters.PageWidthDots - (horizontalOffset * printParameters.CellWidthDots)) / 2f) + (horizontalOffset * printParameters.CellWidthDots);
                    currentY = e.PageSettings.PrintableArea.Top + printSudoku.DefaultPageSettings.Margins.Top + 2 * PrintParameters.HeaderFont.GetHeight(g);
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
                    g.DrawString(Resources.Solution + Resources.Plural, PrintParameters.TitleFont, PrintParameters.SolidBrush, new RectangleF(currentX, currentY - 1.5f * PrintParameters.TitleFont.GetHeight(g), printParameters.PageWidthDots, PrintParameters.TitleFont.GetHeight(g)), PrintParameters.Centered);

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
            int row = 0;
            int col = 0;
            Font printFont = (Settings.Default.HorizontalProblems > 3 ? PrintParameters.SmallFont : (Settings.Default.HorizontalProblems < 2 ? PrintParameters.LargeFont : PrintParameters.NormalFont));
            Font hintFont = (printParameters.CellWidthDots < 45 ? PrintParameters.SmallFixedFont : PrintParameters.NormalFont);

            BaseProblem currentProblem = printParameters.Problems[printParameters.CurrentProblem];
            RectangleF rf = new RectangleF(x, y + SudokuSize * printParameters.CellHeightDots + PrintParameters.TitleFont.GetHeight(g), SudokuSize * printParameters.CellWidthDots, printParameters.CellHeightDots);

            if(currentProblem.NumberOfSolutions == 0)
                g.DrawString(Resources.TitleNotResolvable, PrintParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            else
                if(currentProblem.NumberOfSolutions == 1)
            {
                String problemTitle = (printParameters.Problems.Count > 1 ? String.Format(cultureInfo, Resources.Problem, printParameters.CurrentProblem + 1) + ": " : String.Empty) + SeverityLevel(currentProblem) + (Settings.Default.PrintInternalSeverity ? " (" + InternalSeverityLevel(currentProblem) + ")" : "");
                g.DrawString(problemTitle, PrintParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            }
            else
                g.DrawString(Resources.MoreThanOne, PrintParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);

            String subTitle = String.Empty;
            if(String.IsNullOrEmpty(subTitle = currentProblem.Comment))
            {
                subTitle = currentProblem.Filename;
                if(PrintParameters.SmallFixedFont.SizeInPoints * subTitle.Length > SudokuSize * printParameters.CellWidthDots)
                    subTitle = "..." + subTitle.Substring(subTitle.Length - (int)(SudokuSize * printParameters.CellWidthDots / PrintParameters.SmallFixedFont.SizeInPoints) - 3);
            }
            else
            {
                if(PrintParameters.SmallFixedFont.SizeInPoints * subTitle.Length > SudokuSize * printParameters.CellWidthDots)
                    subTitle = subTitle.Substring(0, (int)(SudokuSize * printParameters.CellWidthDots / PrintParameters.SmallFixedFont.SizeInPoints) - 3) + "...";
            }
            g.DrawString(subTitle, PrintParameters.SmallFixedFont, PrintParameters.SolidBrush, new RectangleF(x, y + SudokuSize * printParameters.CellHeightDots + .5f * PrintParameters.SmallFixedFont.GetHeight(g), SudokuSize * printParameters.CellWidthDots, PrintParameters.SmallFixedFont.GetHeight(g)), PrintParameters.Centered);

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
                    else if(Settings.Default.PrintHints || showCandidates)
                        if(Settings.Default.UseWatchHandHints || printParameters.CellWidthDots < 30)
                            PrintWatchHands(currentProblem.Matrix.Cell(row, col), cell, g);
                        else
                            PrintHints(currentProblem.Matrix.Cell(row, col), cell, g, hintFont);
                }
            printParameters.CurrentProblem++;
        }

        private void PrintWatchHands(BaseCell value, RectangleF rf, Graphics g)
        {
            float diameter = rf.Width / 10;
            float xStart = 0, xEnd = 0, yStart = 0, yEnd = 0;

            for(int i = 1; i <= SudokuForm.SudokuSize; i++)
                if((!showCandidates && (value.Enabled(i) || value.DefinitiveValue == i)) || (showCandidates && (value.GetCandidateMask(i, false) || value.GetCandidateMask(i, true))))
                {
                    if(i == 5)
                        g.FillEllipse(showCandidates ? (value.GetCandidateMask(i, false) ? PrintParameters.GreenSolidBrush : PrintParameters.RedSolidBrush) : PrintParameters.SolidBrush, rf.X + rf.Width / 2, rf.Y + rf.Height / 2, diameter, diameter);
                    else
                    {
                        switch(i)
                        {
                        case 1:
                        case 6:
                            xStart = xEnd = rf.X + rf.Width / 2f;
                            break;
                        case 2:
                        case 3:
                        case 4:
                            xStart = rf.X + rf.Width / 10 * 8f;
                            xEnd = rf.X + rf.Width;
                            break;
                        case 7:
                        case 8:
                        case 9:
                            xStart = rf.X + rf.Width / 10 * 2f;
                            xEnd = rf.X;
                            break;
                        }

                        switch(i)
                        {
                        case 1:
                        case 2:
                        case 9:
                            yStart = rf.Y + rf.Height / 10 * 2f;
                            yEnd = rf.Y;
                            break;
                        case 3:
                        case 8:
                            yStart = yEnd = rf.Y + rf.Height / 2f;
                            break;
                        case 4:
                        case 6:
                        case 7:
                            yStart = rf.Y + rf.Height / 10 * 8f;
                            yEnd = rf.Y + rf.Height;
                            break;
                        }
                        g.DrawLine(showCandidates ? (value.GetCandidateMask(i, false) ? PrintParameters.GreenTinySolidLine : PrintParameters.RedTinySolidLine) : PrintParameters.TinySolidLine, xStart, yStart, xEnd, yEnd);
                    }
                }
        }

        private void PrintHints(BaseCell value, RectangleF rf, Graphics g, Font printFont)
        {
            PrintHints(value, rf, g, printFont, Color.Black);
        }

        private void PrintHints(BaseCell value, RectangleF rf, Graphics g, Font printFont, Color color)
        {
            float x = 0, y = 0;
            SolidBrush normalBrush = new SolidBrush(color);
            SolidBrush candidateBrush = new SolidBrush(Color.Green);
            SolidBrush exclusionCandidateBrush = new SolidBrush(Color.Red);

            for(int i = 1; i <= SudokuForm.SudokuSize; i++)
                if((!showCandidates && (value.Enabled(i) || value.DefinitiveValue == i)) || (showCandidates && (value.GetCandidateMask(i, false) || value.GetCandidateMask(i, true))))
                {
                    switch(i)
                    {
                    case 2:
                    case 5:
                    case 8:
                        x = rf.X + rf.Width / 2f - (printFont.SizeInPoints * .75f);
                        break;
                    case 1:
                    case 4:
                    case 7:
                        x = rf.X + printFont.SizeInPoints / 8f;
                        break;
                    case 3:
                    case 6:
                    case 9:
                        x = rf.X + rf.Width - (printFont.SizeInPoints * 1.5f);
                        break;
                    }

                    switch(i)
                    {
                    case 1:
                    case 2:
                    case 3:
                        y = rf.Y + printFont.SizeInPoints / 8f;
                        break;
                    case 4:
                    case 5:
                    case 6:
                        y = rf.Y + rf.Height / 2f - (printFont.SizeInPoints * .75f);
                        break;
                    case 7:
                    case 8:
                    case 9:
                        y = rf.Y + rf.Height - (printFont.SizeInPoints * 1.75f);
                        break;
                    }
                    g.DrawString(i.ToString(), printFont, showCandidates ? (value.GetCandidateMask(i, false) ? candidateBrush : exclusionCandidateBrush) : normalBrush, x, y);
                }
        }

        private void PrintSolution(float x, float y, Graphics g)
        {
            int row = 0;
            int col = 0;

            RectangleF rf = new RectangleF(x, y + SudokuSize * printParameters.SmallCellHeightDots, SudokuSize * printParameters.SmallCellWidthDots, PrintParameters.TitleFont.GetHeight(g));

            if(printParameters.Problems[printParameters.CurrentSolution].NumberOfSolutions > 1)
                g.DrawString(Resources.SolutionOne, PrintParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            else if(printParameters.Problems[printParameters.CurrentSolution].NumberOfSolutions == 0)
                g.DrawString(Resources.InvalidProblem, PrintParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);
            else
                g.DrawString(printParameters.Problems.Count > 1 ? String.Format(cultureInfo, Resources.Problem, printParameters.CurrentSolution + 1) : Resources.Solution, PrintParameters.TitleFont, PrintParameters.SolidBrush, rf, PrintParameters.Centered);

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
                            printParameters.Problems[printParameters.CurrentSolution].FixedValue(row, col) && !printParameters.Problems[printParameters.CurrentSolution].Matrix.ComputedValue(row, col) ? PrintParameters.SmallBoldFont : PrintParameters.SmallFont,
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