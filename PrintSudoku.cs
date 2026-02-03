using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sudoku;

public partial class SudokuForm: Form
{
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
                    int count = controller.LoadProblems(filenames, new Action<Object>(o =>
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