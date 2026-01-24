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
                if(printerService.PrintResult != 0)
                    MessageBox.Show(this, Resources.NotPrinted + Environment.NewLine + printerService.PrintErrorMessage);
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
                MessageBox.Show(this, Resources.InvalidProblem + Environment.NewLine + Resources.PrintNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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

            if(!controller.CurrentProblem.Aborted)
            {
                Boolean sc;
                if((sc = controller.CurrentProblem.HasCandidates()) && Settings.Default.PrintHints)
                    sc = MessageBox.Show(this, Resources.PrintCandidates, ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes;

                printSudokuDialog.UseEXDialog = true;
                if(printSudokuDialog.ShowDialog() == DialogResult.OK)
                {
                    SudokuPrinterService printerService = new SudokuPrinterService(SudokuSize);

                    printSudokuDialog.Document = printerService.Document;

                    controller.CurrentProblem.ResetMatrix();
                    printerService.AddProblem(controller.CurrentProblem);
                    printerService.ShowCandidates=sc;
                    PrintDocument();
                }
            }

            controller.UpdateProblem(tmp);
        }

        private void PrintBooklet()
        {
            printerService.ShowCandidates = false;
            if(printerService.NumberOfProblems < 1)
                MessageBox.Show(this, Resources.NoProblems);
            else
            {
                printSudokuDialog.UseEXDialog = true;
                if(printSudokuDialog.ShowDialog() == DialogResult.OK)
                {
                    printSudokuDialog.Document = printerService.Document;

                    printerService.SortProblems();
                    PrintDocument();
                }
            }
        }

        private SudokuPrinterService InitializePrinterService()
        {
            printerService?.Dispose();
            printerService = new SudokuPrinterService(SudokuSize);
            return printerService;
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
            printerService = InitializePrinterService();
            GenerateProblems(Settings.Default.BookletSizeNew, generationParameters.NewSudokuType());
        }

        private void LoadProblems4Booklet()
        {
            printerService = InitializePrinterService();

            selectBookletDirectory.SelectedPath = Settings.Default.ProblemDirectory;
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
                    SudokuController bookletController = new SudokuController(filenames[problemNumber], false, settings);
                    if(bookletController.CurrentProblem != null && (bookletController.CurrentProblem.SeverityLevelInt & Settings.Default.SeverityLevel) != 0)
                    {
                        bookletController.CurrentProblem.FindSolutions(2, FormCTS.Token);

                        if(bookletController.CurrentProblem.SolverTask != null && !bookletController.CurrentProblem.SolverTask.IsCompleted)
                            bookletController.CurrentProblem.SolverTask.Wait();

                        if(bookletController.CurrentProblem.NumberOfSolutions == 1)
                        {
                            bookletController.CurrentProblem.ResetMatrix();
                            bookletController.CurrentProblem.Filename = filenames[problemNumber];
                            printerService.AddProblem(bookletController.CurrentProblem);

                            int remainder;
                            Math.DivRem(printerService.NumberOfProblems / 10, 25, out remainder);
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
                ready = (printerService.NumberOfProblems == Settings.Default.BookletSizeExisting && !Settings.Default.BookletSizeUnlimited) || filenames.Count == 0 || abortRequested;
            }
            controller.UpdateProblem(tmp);

            return printerService.NumberOfProblems;
        }
    }
}