using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sudoku;

public partial class SudokuForm: Form
{
    /// <summary>
    /// Displays the print dialog for the current Sudoku problem and initiates printing if valid.
    /// </summary>
    private async Task PrintDialog()
    {
        if(!SudokuGrid.InSync || !SudokuGrid.SyncProblemWithGUI(true, false))
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

            controller.PrintSingleProblem(sc);
        }

        controller.UpdateProblem(tmp);
    }

    /// <summary>
    /// Initiates the printing of a Sudoku booklet with all queued problems.
    /// </summary>
    private void PrintBooklet()
    {
        controller.PrintBooklet();
    }

    /// <summary>
    /// Generates new Sudoku problems for a booklet and initiates the printing service.
    /// </summary>
    private void GenerateProblems4Booklet()
    {
        if(!UnsavedChanges()) return;

        controller.CreateBookletDirectory();
        controller.InitializePrinterService();
        GenerateProblems(settings.BookletSizeNew, controller.NewSudokuType());
    }

    /// <summary>
    /// Allows user to select a directory of Sudoku problems to load for booklet printing.
    /// </summary>
    private async Task LoadProblems4Booklet()
    {
        controller.InitializePrinterService();

        selectBookletDirectory.SelectedPath = settings.ProblemDirectory;
        selectBookletDirectory.ShowNewFolderButton = false;

        if(selectBookletDirectory.ShowDialog() == DialogResult.OK)
        {
            DisableGUI();

            List<String> filenames = new List<string>();

            controller.LoadProblemFilenames(new DirectoryInfo(selectBookletDirectory.SelectedPath), filenames, FormCTS.Token);
            if(!AbortRequested)
            {
                int totalNumber = filenames.Count;
                if(totalNumber < 1)
                    ShowInfo(Resources.NoProblems);
                else
                {
                    int count = await controller.LoadProblems(filenames, new Action<Object>(o =>
                        {
                            int remainder;
                            Math.DivRem(controller.NumberOfProblems / 10, 25, out remainder);
                            sudokuStatusBarText.Text = Resources.LoadingFiles.PadRight(Resources.LoadingFiles.Length + remainder, '.');
                            sudokuStatusBar.Update();
                        }), FormCTS.Token);
                    if(!AbortRequested)
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
}