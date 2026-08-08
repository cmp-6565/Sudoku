#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sudoku;

/// <summary>
/// Partial class containing all printing-related functionality for the SudokuForm.
/// Handles single problem printing, booklet generation, and file management for printing operations.
/// </summary>
public partial class SudokuForm
{
    /// <summary>
    /// Asynchronously displays the print dialog for the current Sudoku problem and initiates printing if valid.
    /// Validates the current problem state, ensures solutions are available, optionally includes hints,
    /// and delegates to the controller's print service.
    /// </summary>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Validates the current problem state
    /// 2. Creates a backup of the current problem
    /// 3. Solves the problem if necessary to get candidate hints
    /// 4. Prompts user about including hints if applicable
    /// 5. Initiates the printing process via the controller
    /// 6. Restores the original problem state
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the problem is invalid and cannot be printed.</exception>
    private async Task PrintDialog()
    {
        // Validate that the problem is in sync and valid for printing
        if(!SudokuGrid.InSync || !SudokuGrid.SyncProblemWithGUI(true, false))
        {
            ShowInfo(Resources.InvalidProblem + Environment.NewLine + Resources.PrintNotPossible);
            return;
        }

        // Backup the current problem to restore it later
        BaseProblem tmp = controller!.CurrentProblem.Clone();

        // Display the problem matrix for verification
        SudokuGrid.DisplayValues(controller!.CurrentProblem.Matrix);

        // If no solutions are available, solve the problem to obtain them
        if(controller!.CurrentProblem.NumberOfSolutions == 0)
        {
            await SolveProblem(false);
        }

        // Reset the detached printing process state
        ResetDetachedProcess();
        ResetTexts();

        // Restore the original problem display
        SudokuGrid.DisplayValues(tmp.Matrix);

        // Check if the user hasn't cancelled the operation and proceed with printing
        if(!FormCTS.Token.IsCancellationRequested)
        {
            Boolean sc;
            // Check if candidates/hints are available and if user wants to include them
            if((sc = controller!.CurrentProblem.HasCandidates()) && settings.PrintHints)
                sc = Confirm(Resources.PrintCandidates) == DialogResult.Yes;

            // Initiate printing of the single problem
            controller.PrintSingleProblem(sc);
        }

        // Restore the problem to its original state
        controller!.UpdateProblem(tmp);
    }

    /// <summary>
    /// Initiates the printing of a Sudoku booklet containing all queued problems.
    /// Delegates to the controller's print service to handle the actual booklet printing.
    /// </summary>
    /// <remarks>
    /// This method assumes that problems have been previously loaded or generated and are queued for printing.
    /// The controller handles the formatting and printing of all queued problems into a single booklet.
    /// </remarks>
    private void PrintBooklet()
    {
        controller.PrintBooklet();
    }

    /// <summary>
    /// Generates new Sudoku problems for booklet creation and initiates the printing service.
    /// Checks for unsaved changes before proceeding. Creates the booklet directory structure
    /// and initializes the printer service with the specified problem count and difficulty.
    /// </summary>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Checks if current problem has unsaved changes
    /// 2. Creates the booklet output directory
    /// 3. Initializes the printer service
    /// 4. Generates the specified number of problems with the selected difficulty
    /// Problems are generated and saved during the process, ready for booklet printing.
    /// </remarks>
    private void GenerateProblems4Booklet()
    {
        // Check if there are unsaved changes that need to be handled
        if(!UnsavedChanges()) return;

        // Create the output directory for booklet files
        controller.CreateBookletDirectory();

        // Initialize the printer service for booklet operations
        controller.InitializePrinterService();

        // Generate the configured number of problems with the new Sudoku type
        GenerateProblems(settings.BookletSizeNew, controller.NewSudokuType());
    }

    /// <summary>
    /// Allows the user to select a directory containing Sudoku problem files to load for booklet printing.
    /// Displays a folder browser dialog, loads all problem files from the selected directory,
    /// and initiates the booklet printing process.
    /// </summary>
    /// <remarks>
    /// This method performs the following operations:
    /// 1. Initializes the printer service
    /// 2. Shows folder browser dialog for directory selection
    /// 3. Collects all problem filenames from the selected directory
    /// 4. Loads problems asynchronously with progress reporting
    /// 5. Initiates booklet printing if loading was successful
    /// 6. Re-enables the GUI and displays completion status
    ///
    /// The method shows progress in the status bar (with animated dots) while loading.
    /// If the user cancels the operation via the FormCTS token, the process is aborted cleanly.
    /// </remarks>
    /// <returns>A task representing the asynchronous folder selection and file loading operation.</returns>
    private async Task LoadProblems4Booklet()
    {
        // Initialize the printer service for handling booklet operations
        controller.InitializePrinterService();

        // Configure the folder browser dialog
        selectBookletDirectory.SelectedPath = settings.ProblemDirectory;
        selectBookletDirectory.ShowNewFolderButton = false;

        // Show the folder browser dialog and check if user selected a directory
        if(selectBookletDirectory.ShowDialog() == DialogResult.OK)
        {
            // Disable GUI to prevent user interaction during loading
            DisableGUI();

            // Create list to store found problem filenames
            List<String> filenames = new List<string>();

            // Load all problem filenames from the selected directory
            controller.LoadProblemFilenames(new DirectoryInfo(selectBookletDirectory.SelectedPath), filenames, FormCTS.Token);

            // Check if the loading was not aborted by the user
            if(!AbortRequested)
            {
                int totalNumber = filenames.Count;

                // Check if any problems were found
                if(totalNumber < 1)
                {
                    ShowInfo(Resources.NoProblems);
                }
                else
                {
                    // Load all problems asynchronously with progress reporting
                    int count = await controller.LoadProblems(filenames, new Action<Object>(o =>
                    {
                        // Calculate animated progress indicator (dots)
                        int remainder;
                        Math.DivRem(controller.NumberOfProblems / 10, 25, out remainder);

                        // Update status bar with animated dots
                        sudokuStatusBarText.Text = Resources.LoadingFiles.PadRight(Resources.LoadingFiles.Length + remainder, '.');
                        sudokuStatusBar.Update();
                    }), FormCTS.Token);

                    // Check if loading completed successfully and was not aborted
                    if(!AbortRequested)
                    {
                        // Update status bar with final count
                        sudokuStatusBarText.Text = String.Format(cultureInfo, Resources.ProblemsLoaded, count, totalNumber);
                        sudokuStatusBar.Update();

                        // Proceed with booklet printing
                        PrintBooklet();
                    }
                }
            }

            // Re-enable GUI if application is not exiting
            if(!applicationExiting)
            {
                CurrentStatus(true);
                sudokuStatusBarText.Text = Resources.Ready;
                EnableGUI();
            }
        }
    }
}