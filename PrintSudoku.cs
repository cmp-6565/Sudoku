using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
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
                if(controller.PrintResult != 0)
                    ShowError(Resources.NotPrinted + Environment.NewLine + controller.PrintErrorMessage);
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
            if(!SudokuGrid.SyncProblemWithGUI(true, false))
            {
                ShowInfo(Resources.InvalidProblem + Environment.NewLine + Resources.PrintNotPossible);
                return;
            }

            BaseProblem tmp = controller.CurrentProblem.Clone();

            SudokuGrid.DisplayValues(controller.CurrentProblem.Matrix);
            if(controller.CurrentProblem.NumberOfSolutions == 0)
            {
                await SolveProblem(false);
            }

            ResetDetachedProcess();
            ResetTexts();
            SudokuGrid.DisplayValues(tmp.Matrix);

            if(!FormCTS.Token.IsCancellationRequested)
            {
                Boolean sc;
                if((sc = controller.CurrentProblem.HasCandidates()) && settings.PrintHints)
                    sc = Confirm(Resources.PrintCandidates) == DialogResult.Yes;

                printSudokuDialog.UseEXDialog = true;
                if(printSudokuDialog.ShowDialog() == DialogResult.OK)
                {
                    SudokuPrinterService printerService = new SudokuPrinterService(settings.SudokuSize, settings);

                    printSudokuDialog.Document = printerService.Document;

                    controller.CurrentProblem.ResetMatrix();
                    printerService.AddProblem(controller.CurrentProblem);
                    printerService.ShowCandidates=sc;
                    PrintDocument();
                }
            }

            controller.UpdateProblem(tmp);
        }
        public Boolean ShowPrintDialog(PrintDocument printDocument)
        {
            printSudokuDialog.UseEXDialog = true;
            printSudokuDialog.Document = printDocument;
            return printSudokuDialog.ShowDialog() == DialogResult.OK;
        }  

        private void PrintBooklet()
        {
            controller.PrintBooklet();
        }

        private void GenerateProblems4Booklet()
        {
            if(!UnsavedChanges()) return;

            controller.CreateBookletDirectory();
            controller.InitializePrinterService();
            GenerateProblems(settings.BookletSizeNew, controller.NewSudokuType());
        }

        private void LoadProblems4Booklet()
        {
            controller.InitializePrinterService();

            selectBookletDirectory.SelectedPath = settings.ProblemDirectory;
            selectBookletDirectory.ShowNewFolderButton = false;

            if(selectBookletDirectory.ShowDialog() == DialogResult.OK)
            {
                DateTime interactiveStartReset = DateTime.MinValue;
                List<String> filenames = new List<string>();

                DisableGUI();

                abortRequested = false;
                LoadProblemFilenames(new DirectoryInfo(selectBookletDirectory.SelectedPath), filenames);
                if(!abortRequested)
                {

                    int totalNumber = filenames.Count;
                    if(totalNumber < 1)
                        ShowInfo(Resources.NoProblems);
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
                    SudokuController bookletController = new SudokuController(filenames[problemNumber], false, settings, this);
                    if(bookletController.CurrentProblem != null && (bookletController.CurrentProblem.SeverityLevelInt & settings.SeverityLevel) != 0)
                    {
                        bookletController.CurrentProblem.FindSolutions(2, FormCTS.Token);

                        if(bookletController.CurrentProblem.SolverTask != null && !bookletController.CurrentProblem.SolverTask.IsCompleted)
                            bookletController.CurrentProblem.SolverTask.Wait();

                        if(bookletController.CurrentProblem.NumberOfSolutions == 1)
                        {
                            bookletController.CurrentProblem.ResetMatrix();
                            bookletController.CurrentProblem.Filename = filenames[problemNumber];
                            controller.AddProblem(bookletController.CurrentProblem);

                            int remainder;
                            Math.DivRem(controller.NumberOfProblems / 10, 25, out remainder);
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
                ready = (controller.NumberOfProblems == settings.BookletSizeExisting && !settings.BookletSizeUnlimited) || filenames.Count == 0 || abortRequested;
            }
            controller.UpdateProblem(tmp);

            return controller.NumberOfProblems;
        }
    }
}