using System;
using System.Threading;
using System.Windows.Forms;
using Sudoku.Core;
using Sudoku.Application;

namespace Sudoku;
#nullable enable

/// <summary>
/// Partial class containing all menu and button event handlers for the SudokuForm.
/// This class isolates menu-related event handling logic from the main form code.
/// All methods in this file respond to user interactions with menu items and toolbar buttons.
/// </summary>
public partial class SudokuForm
{
    /// <summary>
    /// Handles the controller's Generating event callback.
    /// Ensures the method executes on the UI thread and updates the display with current generation status.
    /// </summary>
    /// <param name="s">The controller object raising the event.</param>
    /// <param name="e">The event arguments.</param>
    private void OnGenerating(object? s, EventArgs e)
    {
        if(InvokeRequired)
        {
            Invoke(new EventHandler(OnGenerating!), s, e);
            return;
        }
        GenerationStatus(((SudokuController)s!).CurrentProblem.GenerationTime);
    }
    /// <summary>
    /// Handles the completion of puzzle generation for either a single puzzle or a booklet.
    /// Delegates to the appropriate handler method based on the current generation mode.
    /// </summary>
    /// <param name="o">Reserved object parameter from the generation callback.</param>
    /// <param name="s">String parameter containing puzzle data or result information from generation.</param>
    public void GenerationFinished(Object? o, string s)
    {
        if(controller.GenerateBooklet)
            GenerationBookletProblemFinished(s);
        else
            GenerationSingleProblemFinished(s);
    }

    /// <summary>
    /// Handles the form focus lost event.
    /// Initiates the auto-pause mechanism if the Sudoku grid is enabled and auto-pause is configured in settings.
    /// </summary>
    /// <param name="sender">The event sender (typically the SudokuForm instance).</param>
    /// <param name="e">The event arguments.</param>
    private void FocusLost(object? sender, EventArgs e)
    {
        if(SudokuGrid.Enabled && (settings?.AutoPause ?? false))
            autoPauseTimer?.Start();
    }

    /// <summary>
    /// Handles the form focus gained event.
    /// Stops the auto-pause timer when the form regains focus, allowing normal puzzle interaction to resume.
    /// </summary>
    /// <param name="sender">The event sender (typically the SudokuForm instance).</param>
    /// <param name="e">The event arguments.</param>
    private void FocusGotten(object? sender, EventArgs e)
    {
        autoPauseTimer?.Stop();
    }

    /// <summary>
    /// Handles the "About Sudoku" menu click event.
    /// Displays the about dialog with application information and credits.
    /// Creates and shows an AboutSudoku dialog instance populated with current settings.
    /// </summary>
    /// <param name="sender">The event sender (menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void AboutSudokuClick(object? sender, EventArgs e)
    {
        new AboutSudoku(settings).ShowDialog();
    }

    /// <summary>
    /// Handles the form closing event (application exit).
    /// Cancels any in-progress asynchronous operations by requesting cancellation through the form's cancellation token.
    /// Waits for any active solver task to complete within a timeout period.
    /// Saves the application state if auto-save is enabled, otherwise clears saved state and prompts user for unsaved changes.
    /// </summary>
    /// <param name="sender">The event sender (the form).</param>
    /// <param name="e">The form closing event arguments; can set e.Cancel to true to prevent closing.</param>
    private void ExitSudoku(object? sender, FormClosingEventArgs e)
    {
        if(controller.CurrentProblem != null)
        {
            try { FormCTS?.Cancel(); } catch { }
            if(controller.CurrentProblem.SolverTask != null && !controller.CurrentProblem.SolverTask.IsCompleted)
                try { controller.CurrentProblem.SolverTask.Wait(2000); } catch { } // Einfaches Join statt DoEvents-Loop
        }

        if(e.CloseReason != CloseReason.TaskManagerClosing && e.CloseReason != CloseReason.WindowsShutDown)
        {
            if(settings.AutoSaveState)
            {
                controller.SaveApplicationState();
            }
            else
            {
                settings.State = "";
                settings.Save();
                if(!SudokuGrid.SyncProblemWithGUI(true, autoCheck.Checked))
                {
                    e.Cancel = Confirm(Resources.CloseAnyway) == ConfirmResult.No;
                }
            }
        }
    }

    /// <summary>
    /// Handles the "Exit" menu click event.
    /// Terminates the application immediately via Application.Exit().
    /// </summary>
    /// <param name="sender">The event sender (Exit menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ExitClick(object? sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Handles the "Reset Problem" menu click event.
    /// Resets the current puzzle to its original state by delegating to the ResetProblem() method.
    /// </summary>
    /// <param name="sender">The event sender (Reset menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ResetClick(object? sender, EventArgs e)
    {
        ResetProblem();
    }

    /// <summary>
    /// Handles the "Validate" menu click event.
    /// Initiates validation of the current puzzle to determine if it has exactly one solution.
    /// Delegates to the ValidateProblem() method which performs the actual validation asynchronously.
    /// </summary>
    /// <param name="sender">The event sender (Validate menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ValidateClick(object sender, EventArgs e)
    {
        ValidateProblem();
    }

    /// <summary>
    /// Handles the "Check" menu click event.
    /// Checks the validity of the current puzzle without extensive solving.
    /// Delegates to the CheckProblem() method which validates puzzle state and resolvability.
    /// </summary>
    /// <param name="sender">The event sender (Check menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void CheckClick(object sender, EventArgs e)
    {
        CheckProblem();
    }

    /// <summary>
    /// Handles the "Options" menu click event.
    /// Opens the Options dialog allowing users to configure application settings.
    /// Updates applicable settings including display language, UI scaling, timers, and grid formatting after OK is clicked.
    /// Configures the minimum booklet size based on current generation mode and booklet problem count.
    /// </summary>
    /// <param name="sender">The event sender (Options menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void OptionsClick(object sender, EventArgs e)
    {
        optionsDialog = new OptionsDialog(settings, this);
        optionsDialog.MinBookletSize = (controller.GenerateBooklet ? Math.Max(controller.CurrentBookletProblem + 1, 2) : 2);

        if(optionsDialog.ShowDialog() == DialogResult.OK)
        {
            Thread.CurrentThread.CurrentUICulture = (cultureInfo = new System.Globalization.CultureInfo(settings.DisplayLanguage));
            ShowInTaskbar = !settings.HideWhenMinimized;
            usePrecalculatedProblem = settings.UsePrecalculatedProblems;

            if(controller.GenerateBooklet) severityLevel = settings.SeverityLevel;

            SudokuGrid.UpdateFonts();

            autoPauseTimer.Interval = Convert.ToInt32(settings.AutoPauseLag) * 1000;

            UpdateGUI();
        }
    }

    /// <summary>
    /// Handles the "Edit Comment" menu click event or comment editing action.
    /// Opens a dialog allowing the user to add or modify comments associated with the current puzzle.
    /// Preserves the original comment and marks the puzzle as dirty if changes are made.
    /// </summary>
    /// <param name="sender">The event sender (Edit Comment menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void EditCommentClicked(object? sender, EventArgs e)
    {
        String oldComment = String.Empty;
        Comment commentDialog = new Comment(settings);

        oldComment = commentDialog.SudokuComment = controller.CurrentProblem.Comment;
        if(commentDialog.ShowDialog() == DialogResult.OK)
        {
            controller.CurrentProblem.Comment = commentDialog.SudokuComment;
            controller.CurrentProblem.Dirty = (oldComment != controller.CurrentProblem.Comment);
        }
    }

    /// <summary>
    /// Handles the "Print" menu click event.
    /// Initiates printing functionality for the current puzzle.
    /// Opens the print dialog asynchronously to allow user print configuration.
    /// Delegates to the PrintDialog() method which manages print setup and execution.
    /// </summary>
    /// <param name="sender">The event sender (Print menu item).</param>
    /// <param name="e">The event arguments.</param>
    private async void PrintClick(object? sender, EventArgs e)
    {
        await PrintDialog();
    }

    /// <summary>
    /// Handles the "Load Booklet" menu click event.
    /// Initiates loading of a previously saved Sudoku puzzle booklet from disk.
    /// Opens a file dialog allowing user selection of a booklet file.
    /// Delegates to the LoadProblems4Booklet() method which manages the file loading process.
    /// </summary>
    /// <param name="sender">The event sender (Load Booklet menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void LoadBookletClick(object? sender, EventArgs e)
    {
        _ = LoadProblems4Booklet();
    }

    /// <summary>
    /// Handles the "Debug Mode" / "Trace Mode" menu check state changed event.
    /// Toggles debug/trace mode which enables detailed logging and visual tracing of solver operations.
    /// Updates the SudokuGrid's debug mode to match the menu item's checked state.
    /// Persists the trace mode setting to application configuration.
    /// </summary>
    /// <param name="sender">The event sender (Debug/Trace Mode menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void DebugClick(object? sender, EventArgs e)
    {
        settings.TraceMode = traceMode.Checked;
        SudokuGrid.SetDebugMode(traceMode.Checked);
    }

    /// <summary>
    /// Handles the "Find All Solutions" menu check state changed event.
    /// Toggles the "find all solutions" mode for the solver.
    /// When enabled, the solver continues searching after finding the first solution to discover all possible solutions.
    /// Persists the setting to application configuration.
    /// </summary>
    /// <param name="sender">The event sender (Find All Solutions menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void FindallSolutionsClick(object? sender, EventArgs e)
    {
        settings.FindAllSolutions = findallSolutions.Checked;
    }

    /// <summary>
    /// Handles the "Show Possible Values" / "Show Hints" menu check state changed event.
    /// Toggles the display of candidate values (pencil marks) in cells.
    /// When enabled, displays all possible values for each empty cell based on Sudoku rules.
    /// Updates the grid display and persists the setting to application configuration.
    /// </summary>
    /// <param name="sender">The event sender (Show Possible Values menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ShowPossibleValuesClick(object? sender, EventArgs e)
    {
        settings.ShowHints = showPossibleValues.Checked;
        UpdateGUI();
    }

    /// <summary>
    /// Handles the "Auto Check" menu check state changed event.
    /// Toggles automatic validation of user input during puzzle solving.
    /// When enabled, checks puzzle validity after each cell entry and provides feedback on invalid placements.
    /// Persists the setting to application configuration.
    /// </summary>
    /// <param name="sender">The event sender (Auto Check menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void AutoCheckClick(object? sender, EventArgs e)
    {
        settings.AutoCheck = autoCheck.Checked;
    }

    /// <summary>
    /// Handles the "Visit Homepage" menu click event.
    /// Opens the application's official homepage in the default web browser.
    /// Retrieves the homepage URL from application resources and uses the OpenUrl helper method.
    /// </summary>
    /// <param name="sender">The event sender (Visit Homepage menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void VisitHomepageClick(object? sender, EventArgs e)
    {
        OpenUrl(Resources.Homepage);
    }

    /// <summary>
    /// Handles the "Mark Neighbors" menu check state changed event.
    /// Toggles the highlighting of neighboring cells related to the currently selected cell.
    /// When enabled, displays visual markers for cells in the same row, column, and 3x3 block.
    /// Reformats the grid display when disabling the feature to clear neighbor highlighting.
    /// Persists the setting to application configuration.
    /// </summary>
    /// <param name="sender">The event sender (Mark Neighbors menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void MarkNeighborsClicked(object? sender, EventArgs e)
    {
        settings.MarkNeighbors = markNeighbors.Checked;
        if(!settings.MarkNeighbors)
            FormatTable();
    }

    /// <summary>
    /// Handles the "Abort" / "Cancel Operation" menu click event.
    /// Cancels any currently running asynchronous operation (puzzle generation, solving, minimization, etc.).
    /// Triggers the abort process asynchronously and allows the operation to terminate gracefully.
    /// Delegates to the AbortThread() method which manages cancellation token signaling.
    /// </summary>
    /// <param name="sender">The event sender (Abort/Cancel menu item).</param>
    /// <param name="e">The event arguments.</param>
    private async void AbortClick(object? sender, EventArgs e)
    {
        await AbortThread();
    }
    /// <summary>
    /// Handles the "Print Booklet" menu click event.
    /// Initiates printing of a complete booklet of Sudoku puzzles.
    /// Delegates to the PrintBooklet() method which manages booklet print setup and execution.
    /// </summary>
    /// <param name="sender">The event sender (Print Booklet menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void PrintBookletClick(object? sender, EventArgs e)
    {
        GenerateProblems4Booklet();
    }

    /// <summary>
    /// Handles the "Display Problem Info" menu click event.
    /// Displays detailed information about the current puzzle in a dialog.
    /// Shows statistics including cell counts, difficulty level, validity, and file information.
    /// Delegates to the DisplayProblemInfo() method which gathers and formats the information.
    /// </summary>
    /// <param name="sender">The event sender (Problem Info menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void DisplayProblemInfoClicked(object? sender, EventArgs e)
    {
        DisplayProblemInfo();
    }

    /// <summary>
    /// Handles the "Show Hints" menu click event.
    /// Displays visual hints or candidate values for cells in the current puzzle.
    /// Delegates to the Hints() method which computes and displays appropriate suggestions.
    /// </summary>
    /// <param name="sender">The event sender (Show Hints menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ShowHintsClick(object? sender, EventArgs e)
    {
        Hints();
    }

    /// <summary>
    /// Handles the "Open" menu click event.
    /// Initiates the file open dialog for loading an existing Sudoku puzzle from disk.
    /// Checks for unsaved changes in the current puzzle before opening a new file.
    /// Delegates to the OpenProblem() method which manages the file selection and loading process.
    /// </summary>
    /// <param name="sender">The event sender (Open/Open File menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void OpenClick(object? sender, EventArgs e)
    {
        OpenProblem();
    }

    /// <summary>
    /// Handles the "Save" menu click event.
    /// Initiates saving of the current puzzle to disk.
    /// Delegates to the SaveProblem() method which manages file path selection and save operations.
    /// Returns silently if save operation is cancelled by the user.
    /// </summary>
    /// <param name="sender">The event sender (Save menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void SaveClick(object? sender, EventArgs e)
    {
        SaveProblem();
    }

    /// <summary>
    /// Handles the "Export" menu click event.
    /// Exports the current puzzle to an external file format for sharing or backup purposes.
    /// Opens a file save dialog to allow user selection of export location and format.
    /// Delegates to the ExportProblem() method which manages format conversion and file writing.
    /// </summary>
    /// <param name="sender">The event sender (Export menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ExportClick(object? sender, EventArgs e)
    {
        ExportProblem();
    }

    /// <summary>
    /// Handles the "Share on Twitter" menu click event.
    /// Shares the current puzzle on Twitter via the application's Twitter integration.
    /// First validates the puzzle to ensure it is in a shareable state.
    /// Opens the Twitter share URL in the default browser with puzzle data encoded.
    /// Delegates to the TwitterProblem() method which manages validation and URL generation.
    /// </summary>
    /// <param name="sender">The event sender (Share on Twitter menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void TwitterProblemClick(object? sender, EventArgs e)
    {
        TwitterProblem();
    }
    /// <summary>
    /// Handles the "Undo" menu click event.
    /// Reverses the last user action on the puzzle (cell value entries or candidate marks).
    /// Delegates to the SudokuGrid's undo mechanism and updates the GUI accordingly.
    /// Only active if there are operations available in the undo stack.
    /// </summary>
    /// <param name="sender">The event sender (Undo menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void UndoClick(object? sender, EventArgs e)
    {
        if(!controller.CanUndo())
            throw (new ApplicationException());
        CoreValue? cv = controller.PopUndo();
        if(cv == null)
            return;

        if(!SudokuGrid[cv.Col, cv.Row].ReadOnly)
        {
            SudokuGrid[cv.Col, cv.Row].Value = cv.UnformatedValue;
            SudokuGrid.Update();
            CurrentStatus(true);
            SudokuGrid[cv.Col, cv.Row].Selected = true;

            undo.Enabled = controller.CanUndo();
            controller.CurrentProblem.Dirty = undo.Enabled;
        }
        else
            controller.PushUndo(cv);
    }

    /// <summary>
    /// Handles the "Resume" menu click event.
    /// Resumes a paused puzzle game.
    /// Removes the pause overlay and restarts the game timer and status update timer.
    /// Delegates to the ResumeGame() method which manages the resume state.
    /// </summary>
    /// <param name="sender">The event sender (Resume menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ResumeClick(object? sender, EventArgs e)
    {
        ResumeGame();
    }

    /// <summary>
    /// Handles the "Reset Timer" menu click event.
    /// Stops and resets the game timer back to zero elapsed time.
    /// Preserves puzzle progress and game state; only clears the elapsed time counter.
    /// </summary>
    /// <param name="sender">The event sender (Reset Timer menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ResetTimerClick(object? sender, EventArgs e)
    {
        sudokuStatusBarText.Text = Resources.Ready;
        controller.StopTimer();
        controller.CurrentProblem.SolvingTime = TimeSpan.Zero;
        statusUpdateTimer.Stop();
    }

    /// <summary>
    /// Handles the "Pause" menu click event.
    /// Pauses the currently active puzzle game or solving operation.
    /// Hides puzzle values and displays a pause overlay; stops all timers.
    /// Delegates to the PauseGame() method which manages the pause state.
    /// </summary>
    /// <param name="sender">The event sender (Pause menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void PauseClick(object? sender, EventArgs e)
    {
        // Overlay initialisieren, falls noch nicht geschehen
        InitializePauseOverlay();

        if(!sudokuStatusBarText.Text?.Contains(Resources.Paused) == true)
            sudokuStatusBarText.Text += Resources.Paused;

        // Statt MessageBox nun das Overlay zeigen
        pauseOverlay!.Visible = true;
        pauseOverlay?.BringToFront();
    }
    /// <summary>
    /// Handles the "Clear Candidates" menu click event.
    /// Clears all candidate/pencil mark values from the current puzzle.
    /// Disables the clear candidates menu option after completion and refreshes the grid display.
    /// </summary>
    /// <param name="sender">The event sender (Clear Candidates menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ClearCandidatesClick(object? sender, EventArgs e)
    {
        controller.CurrentProblem.ResetCandidates();
        clearCandidates.Enabled = false;
        SudokuGrid.Refresh();
    }

    /// <summary>
    /// Handles the "New Sudoku" menu click event.
    /// Creates a new classic (non-diagonal) Sudoku puzzle after checking for unsaved changes.
    /// If unsaved changes exist, prompts the user appropriately before creating a new puzzle.
    /// Reformats the grid layout to prepare for the new puzzle.
    /// </summary>
    /// <param name="sender">The event sender (New Sudoku menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void NewSudokuClick(object? sender, EventArgs e)
    {
        if(UnsavedChanges())
        {
            controller.CreateNewProblem(false);
            SudokuGrid.FormatBoard(true);
        }
    }

    /// <summary>
    /// Handles the "New X-Sudoku" menu click event.
    /// Creates a new X-Sudoku (with diagonal constraints) puzzle after checking for unsaved changes.
    /// If unsaved changes exist, prompts the user appropriately before creating a new puzzle.
    /// Reformats the grid layout to prepare for the new puzzle variant.
    /// </summary>
    /// <param name="sender">The event sender (New X-Sudoku menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void NewXSudokuClick(object? sender, EventArgs e)
    {
        if(UnsavedChanges())
        {
            controller.CreateNewProblem(true);
            SudokuGrid.FormatBoard(true);
        }
    }

    /// <summary>
    /// Handles the "Solve" menu click event.
    /// Asynchronously initiates solving of the current puzzle.
    /// Delegates to the SolveProblem() async method to perform the actual solving operation.
    /// </summary>
    /// <param name="sender">The event sender (Solve menu item).</param>
    /// <param name="e">The event arguments.</param>
    private async void SolveClick(object? sender, EventArgs e)
    {
        await SolveProblem();
    }

    /// <summary>
    /// Handles the "Generate" menu click event.
    /// Generates a new puzzle batch after checking for unsaved changes.
    /// Prompts the user to select a severity level if configured to do so.
    /// If unsaved changes exist, prompts the user appropriately before generating new puzzles.
    /// </summary>
    /// <param name="sender">The event sender (Generate menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void GenerateClick(object? sender, EventArgs e)
    {
        if(UnsavedChanges()) GenerateProblems(1, false);
    }

    /// <summary>
    /// Handles the "Start Game" menu click event.
    /// Locks the puzzle in read-only mode for solving and starts the game timer for elapsed time tracking.
    /// Starts the status update timer to display real-time elapsed time.
    /// </summary>
    /// <param name="sender">The event sender (Start Game menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void StartGameClick(object? sender, EventArgs e)
    {
        SetReadOnly(true);
        controller.StartTimer();
        statusUpdateTimer.Start();
    }

    /// <summary>
    /// Handles the "Fix" menu click event.
    /// Sets the puzzle to read-only mode (locks puzzle for solving, preventing further editing).
    /// Does not explicitly start the timer unlike StartGameClick.
    /// </summary>
    /// <param name="sender">The event sender (Fix menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void FixClick(object? sender, EventArgs e)
    {
        SetReadOnly(true);
    }

    /// <summary>
    /// Handles the "Release" menu click event.
    /// Disables read-only mode and enables full editing of the puzzle.
    /// Allows the user to modify previously fixed or solved cell values.
    /// </summary>
    /// <param name="sender">The event sender (Release menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ReleaseClick(object? sender, EventArgs e)
    {
        SetReadOnly(false);
    }

    /// <summary>
    /// Handles the "Minimize" menu click event.
    /// Asynchronously minimizes the current puzzle to its minimal form (fewest clues required).
    /// Backs up the current puzzle state before beginning minimization.
    /// Updates the puzzle with the minimized version if successful, restores original if no minimization is possible.
    /// Displays feedback to the user on the number of clues removed or if minimization was not possible.
    /// </summary>
    /// <param name="sender">The event sender (Minimize menu item).</param>
    /// <param name="e">The event arguments.</param>
    private async void MinimizeClick(object? sender, EventArgs e)
    {
        int before = controller.CurrentProblem.nValues;
        Boolean dirty = controller.CurrentProblem.Dirty;

        if(!SudokuGrid.SyncProblemWithGUI(true, false))
        {
            ShowError(Resources.MinimizationNotPossible);
            return;
        }

        await Minimize(int.MaxValue);

        if(before - controller.CurrentProblem.nValues == 0)
        {
            ShowInfo(Resources.NoMinimizationPossible);
            controller.CurrentProblem.Dirty = dirty;
        }
        else
        {
            ShowInfo(String.Format(Resources.Minimized, (before - controller.CurrentProblem.nValues).ToString()));
            controller.CurrentProblem.Dirty = true;
        }
    }

    /// <summary>
    /// Handles minimization progress updates received from the minimization operation.
    /// Updates the status bar with current minimization progress information.
    /// Ensures the method executes on the UI thread by invoking if necessary.
    /// </summary>
    /// <param name="sender">The source of the minimization operation (typically the controller).</param>
    /// <param name="minimalProblem">The current minimal puzzle candidate, or null if update is only for status.</param>
    private void HandleMinimizing(object? sender, BaseProblem? minimalProblem)
    {
        if(InvokeRequired)
        {
            Invoke(new Action<object, BaseProblem?>(HandleMinimizing), sender, minimalProblem);
            return;
        }
        if (minimalProblem != null)
            status.Text = String.Format(Resources.CurrentMinimalProblem, minimalProblem.SeverityLevelCategory.ToDisplayText(), minimalProblem.nValues, controller.CurrentProblem.nValues).Replace("\\n", Environment.NewLine);
        status.Update();
        sudokuStatusBarText.Text = Resources.Minimizing;
    }
    /// <summary>
    /// Handles the "Version History" menu click event.
    /// Opens the application's version history or release notes webpage in the default browser.
    /// Retrieves the URL from application resources and uses the OpenUrl helper method.
    /// </summary>
    /// <param name="sender">The event sender (Version History menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void VersionHistoryClicked(object? sender, EventArgs e)
    {
        OpenUrl(Resources.VersionHistory);
    }

    /// <summary>
    /// Handles the Options menu opening event.
    /// Updates the enabled state of pause and reset timer menu options based on whether the game timer is currently running.
    /// Called each time the user opens the Options menu to ensure current state is reflected.
    /// </summary>
    /// <param name="sender">The event sender (Options menu).</param>
    /// <param name="e">The event arguments.</param>
    private void OptionsMenuOpening(object? sender, EventArgs e)
    {
        pause.Enabled = resetTimer.Enabled = controller.IsTimerRunning;
    }

    /// <summary>
    /// Handles the "Sudoku of the Day" menu click event.
    /// Asynchronously loads the daily Sudoku puzzle from a server or predefined source.
    /// Displays a dialog with puzzle difficulty information if loading succeeds.
    /// Shows an error message if the Sudoku of the Day could not be loaded or retrieved.
    /// </summary>
    /// <param name="sender">The event sender (Sudoku of the Day menu item).</param>
    /// <param name="e">The event arguments.</param>
    private async void SudokuOfTheDayClicked(object? sender, EventArgs e)
    {
        if(await SudokuOfTheDay())
            ShowInfo(String.Format(Resources.SudokuOfTheDayInfo, controller.CurrentProblem.SeverityLevelCategory.ToDisplayText()));
        else
            ShowError(Resources.SudokuOfTheDayNotLoaded);
    }

    // Diverse Events
    /// <summary>
    /// Handles the drag-over event on the form for drag-and-drop file operations.
    /// Sets the drag-drop effect to Move if the dropped item supports move operations.
    /// Called when the user drags a file over the form to indicate that dropping is allowed.
    /// </summary>
    /// <param name="sender">The event sender (the form).</param>
    /// <param name="e">The drag event arguments containing information about the drag operation.</param>
    private void DragOverForm(object? sender, DragEventArgs e)
    {
        if((e.AllowedEffect & DragDropEffects.Move) == DragDropEffects.Move)
            e.Effect = DragDropEffects.Move;
    }

    /// <summary>
    /// Handles the drop event when a file is dragged and dropped onto the form.
    /// Extracts the file path from the dropped data and attempts to load it as a Sudoku puzzle.
    /// First checks for unsaved changes before loading the new puzzle.
    /// Silently ignores dropped objects that are not files.
    /// </summary>
    /// <param name="sender">The event sender (the form).</param>
    /// <param name="e">The drag event arguments containing the dropped file data.</param>
    private void DropProblem(object? sender, DragEventArgs e)
    {
        if(UnsavedChanges())
        {
            try
            {
                String[] droppedData = (String[])e.Data!.GetData(DataFormats.FileDrop.ToString())!;
                LoadProblem(droppedData[0]);
            }
            catch
            {
                // do nothing if the droped object was not a file
            }
        }
    }

    /// <summary>
    /// Handles the "Toggle Highlight Same Values" menu click event.
    /// Toggles the highlight functionality that emphasizes cells with the same value as the selected cell.
    /// Updates the setting and either highlights matching values or clears all highlights accordingly.
    /// </summary>
    /// <param name="sender">The event sender (Highlight Same Values menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ToggleHighlightSameValuesClicked(object? sender, EventArgs e)
    {
        highlightSameValues.Checked = !highlightSameValues.Checked;
        settings.HighlightSameValues = highlightSameValues.Checked;
        if(settings.HighlightSameValues)
            SudokuGrid.UpdateHighligts();
        else
            SudokuGrid.ClearHighlights();
    }

    /// <summary>
    /// Handles the "Toggle Pencil Mode" menu click event.
    /// Toggles between normal entry mode and pencil mark (candidate) entry mode.
    /// Changes the cursor to indicate the current mode (Help cursor for pencil mode, Default for normal mode).
    /// Pencil mode allows entering multiple candidate values in a cell, while normal mode enters fixed values.
    /// </summary>
    /// <param name="sender">The event sender (Pencil Mode menu item).</param>
    /// <param name="e">The event arguments.</param>
    public void TogglePencilModeClick(object? sender, EventArgs e)
    {
        pencilMode.Checked = !pencilMode.Checked;
        SudokuGrid.Cursor = pencilMode.Checked ? Cursors.Help : Cursors.Default;
    }

    /// <summary>
    /// Handles the "Display Cell Info" context menu or event.
    /// Retrieves information about the currently selected cell and displays it in a dialog.
    /// Works only if exactly one cell is selected; does nothing if multiple cells are selected.
    /// </summary>
    /// <param name="sender">The event sender (the grid or context menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void DisplayCellInfo(object? sender, EventArgs e)
    {
        DataGridViewSelectedCellCollection cells = SudokuGrid.SelectedCells;
        if(cells.Count == 1)
            DisplayCellInfo(cells[0].RowIndex, cells[0].ColumnIndex);
    }

    /// <summary>
    /// Handles the "Activate Grid" event to set focus to the Sudoku grid.
    /// Ensures the grid receives keyboard and mouse focus for user input.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void ActivateGrid(object? sender, EventArgs e)
    {
        SudokuGrid.Focus();
    }

    /// <summary>
    /// Handles the form window state change event (minimize/maximize/restore).
    /// Sets the form's opacity to 0 when minimized and 100 when restored.
    /// This creates a hide effect when the form is minimized if HideWhenMinimized setting is enabled.
    /// </summary>
    /// <param name="sender">The event sender (the form).</param>
    /// <param name="e">The event arguments.</param>
    private void ResizeForm(object? sender, EventArgs e)
    {
        Opacity = (WindowState == FormWindowState.Minimized) ? 0 : 100;
    }

    /// <summary>
    /// Handles the "Previous Solution" button click event.
    /// Displays the previous solution if multiple solutions were found during solving.
    /// Decrements the solution index and updates button states and form title accordingly.
    /// </summary>
    /// <param name="sender">The event sender (Prior button).</param>
    /// <param name="e">The event arguments.</param>
    private void PriorClick(object? sender, EventArgs e)
    {
        PriorSolution();
    }

    /// <summary>
    /// Handles the "Next Solution" button click event.
    /// Displays the next solution if multiple solutions were found during solving.
    /// Increments the solution index and updates button states and form title accordingly.
    /// </summary>
    /// <param name="sender">The event sender (Next button).</param>
    /// <param name="e">The event arguments.</param>
    private void NextClick(object? sender, EventArgs e)
    {
        NextSolution();
    }

    /// <summary>
    /// Handles the "Show Definite Values" menu click event.
    /// Displays cells that can be determined through logical deduction without full solving.
    /// Delegates to the ShowDefiniteValues() method which applies simplified solving techniques.
    /// </summary>
    /// <param name="sender">The event sender (Show Definite Values menu item).</param>
    /// <param name="e">The event arguments.</param>
    private void ShowDefiniteValuesClick(object sender, EventArgs e)
    {
        ShowDefiniteValues();
    }

    private void GenerateSudokuClick(object sender, EventArgs e)
    {
        if(UnsavedChanges()) GenerateProblems(1, false);
    }

    private void GenerateXSudokuClick(object sender, EventArgs e)
    {
        if(UnsavedChanges()) GenerateProblems(1, true);
    }

    // Timer Events
    /// <summary>
    /// Handles the auto-pause timer tick event.
    /// Automatically pauses the game if the form is not minimized and auto-pause timeout has expired.
    /// Triggered at intervals defined by the auto-pause timer's Interval property.
    /// </summary>
    /// <param name="sender">The event sender (the autoPauseTimer).</param>
    /// <param name="e">The event arguments.</param>
    private void AutoPauseTick(object? sender, EventArgs e)
    {
        if(WindowState != FormWindowState.Minimized) PauseClick(sender!, e);
    }

    /// <summary>
    /// Handles the status update timer tick event.
    /// Updates the status bar with the current elapsed time including both controller elapsed time and puzzle solving time.
    /// Called every second (as defined by the statusUpdateTimer's 1000ms interval) to update the real-time clock display.
    /// </summary>
    /// <param name="sender">The event sender (the statusUpdateTimer).</param>
    /// <param name="e">The event arguments.</param>
    private void StatusUpdateTick(object? sender, EventArgs e)
    {
        TimeSpan elapsed = controller.ElapsedTime + controller.CurrentProblem.SolvingTime;
        sudokuStatusBarText.Text = Resources.SolutionTime + String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours * 24 + elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
    }
}