using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Sudoku.Core;

namespace Sudoku;
#nullable enable

/// <summary>
/// Main form for the Sudoku application. Implements the user interface for creating, solving, and managing Sudoku puzzles.
/// Inherits from Form and implements IUserInteraction and IDisposable interfaces for resource management and user feedback.
/// This is a partial class that contains the core logic for the Sudoku form, with additional designer-generated components
/// in the corresponding designer file.
/// </summary>
public partial class SudokuForm: Form, IUserInteraction, IDisposable
{
    /// <summary>
    /// Application settings instance injected via dependency injection. Provides configuration for the application behavior.
    /// </summary>
    private ISudokuSettings settings;

    /// <summary>
    /// Factory for creating instances of SudokuController with appropriate configuration.
    /// </summary>
    private readonly SudokuControllerFactory controllerFactory;

    /// <summary>
    /// Timer for handling automatic pause functionality when the form loses focus.
    /// </summary>
    private System.Windows.Forms.Timer autoPauseTimer;

    /// <summary>
    /// Timer for periodic status updates, typically updated every second.
    /// </summary>
    private System.Windows.Forms.Timer statusUpdateTimer;

    /// <summary>
    /// Stopwatch for measuring puzzle generation time.
    /// </summary>
    private Stopwatch generationTimer = new Stopwatch();

    /// <summary>
    /// Tracks the current solution index when multiple solutions are being explored.
    /// </summary>
    private int currentSolution = 0;

    /// <summary>
    /// Property that indicates whether an abort has been requested via the cancellation token.
    /// </summary>
    private Boolean AbortRequested { get { if(FormCTS != null) return FormCTS.Token.IsCancellationRequested; return false; } }

    /// <summary>
    /// Flag indicating whether the application is currently exiting.
    /// </summary>
    private Boolean applicationExiting = false;

    /// <summary>
    /// Current culture information for localization and formatting.
    /// </summary>
    private CultureInfo cultureInfo;

    /// <summary>
    /// Reference to the options dialog, if currently open.
    /// </summary>
    private OptionsDialog? optionsDialog;

    /// <summary>
    /// Flag indicating whether to use a pre-calculated problem instead of generating a new one.
    /// </summary>
    private Boolean usePrecalculatedProblem = false;

    /// <summary>
    /// The current severity level for puzzle generation (difficulty).
    /// </summary>
    private int severityLevel = 0;

    /// <summary>
    /// The main controller handling puzzle logic and operations.
    /// </summary>
    private SudokuController controller = default!;

    /// <summary>
    /// Gets or sets the cancellation token source for coordinating async operations and form lifecycle.
    /// </summary>
    public CancellationTokenSource FormCTS { get; set; } = default!;

    /// <summary>
    /// Label control that displays a pause overlay on the form.
    /// </summary>
    private Label? pauseOverlay;

    /// <summary>
    /// Progress reporter for handling updates during puzzle minimization operations.
    /// </summary>
    private Progress<MinimizationUpdate>? minimizationProgress;

    /// <summary>
    /// Parameterless constructor kept for Windows Forms designer compatibility.
    /// Creates a SudokuForm with default settings and no controller factory specified.
    /// </summary>
    public SudokuForm() : this(new WinFormsSettings(), null) { }

    /// <summary>
    /// Constructor used by dependency injection. Initializes the form with injected application settings and controller factory.
    /// Sets up the UI, timers, controllers, and loads any puzzle file specified via command line arguments.
    /// </summary>
    /// <param name="applicationSettings">The injected application settings instance for configuration.</param>
    /// <param name="applicationControllerFactory">The injected controller factory; if null, a local factory is created.</param>
    internal SudokuForm(ISudokuSettings applicationSettings, SudokuControllerFactory? applicationControllerFactory)
    {
        settings = applicationSettings ?? new WinFormsSettings();
        // wenn DI null (Designer-Fall) lokale Factory erstellen
        controllerFactory = applicationControllerFactory ?? new SudokuControllerFactory(settings);

        Thread.CurrentThread.CurrentUICulture = (cultureInfo = new System.Globalization.CultureInfo(settings.DisplayLanguage));

        InitializeComponent();
        InitializeFormCTS();
        SudokuGrid.Initialize(settings, this);
        InitializeController();
        InitializeMinimizationProgress();

        sudokuMenu.Renderer = new FlatRenderer();

        traceMode.Checked = settings.TraceMode;
        autoCheck.Checked = settings.AutoCheck;
        showPossibleValues.Checked = settings.ShowHints;
        findallSolutions.Checked = settings.FindAllSolutions;
        ShowInTaskbar = !settings.HideWhenMinimized;
        markNeighbors.Checked = settings.MarkNeighbors;
        highlightSameValues.Checked = settings.HighlightSameValues;

        Deactivate += new EventHandler(FocusLost);
        Activated += new EventHandler(FocusGotten);

        autoPauseTimer = new System.Windows.Forms.Timer();
        autoPauseTimer.Interval = Convert.ToInt32(settings.AutoPauseLag) * 1000;
        autoPauseTimer.Tick += new EventHandler(AutoPauseTick);

        statusUpdateTimer = new System.Windows.Forms.Timer();
        statusUpdateTimer.Interval = 1000;
        statusUpdateTimer.Tick += new EventHandler(StatusUpdateTick);

        FormatTable();
        EnableGUI();
        UpdateGUI();
        ResetUndo();
        ResetTexts();

        CheckVersion();
        string[] args = Environment.GetCommandLineArgs();
        if(args.Length > 1)
        {
            string fn = args[1];
            if(fn.Contains("file:///"))
                fn = fn.Remove(0, 8);

            LoadProblem(fn);
        }
    }

    /// <summary>
    /// Disposes of managed resources including timers, dialogs, UI elements, and the controller.
    /// </summary>
    public new void Dispose()
    {
        base.Dispose();
        autoPauseTimer?.Dispose();
        statusUpdateTimer?.Dispose();
        optionsDialog?.Dispose();
        pauseOverlay?.Dispose();
        controller?.Dispose();
    }

    /// <summary>
    /// Initializes or reinitializes the cancellation token source for managing async operations.
    /// Cancels and disposes the existing token source before creating a new one.
    /// </summary>
    private void InitializeFormCTS()
    {
        try { FormCTS?.Cancel(); } catch { }
        FormCTS?.Dispose();
        FormCTS = new CancellationTokenSource();
    }

    /// <summary>
    /// Initializes the progress reporter for minimization operations.
    /// Handles updates for cell visualization and status text during puzzle minimization.
    /// </summary>
    private void InitializeMinimizationProgress()
    {
        minimizationProgress = new Progress<MinimizationUpdate>(update =>
        {
            switch(update.Type)
            {
            case MinimizationUpdateType.TestCell:
                if(update.Cell != null) SudokuGrid.HandleOnTestCell(this, update.Cell);
                break;
            case MinimizationUpdateType.ResetCell:
                if(update.Cell != null) SudokuGrid.ResetCellVisuals(this, update.Cell);
                break;
            case MinimizationUpdateType.Status:
                BaseProblem? problem = update.Problem ?? controller?.CurrentProblem;
                if(problem == null)
                {
                    status.Text = Resources.Minimizing;
                    status.Update();
                    break;
                }

                int totalValues = controller?.CurrentProblem?.nValues ?? problem.nValues;

                status.Text = String.Format(Resources.CurrentMinimalProblem,
                    problem.SeverityLevelCategory.ToDisplayText(),
                    problem.nValues,
                    totalValues).Replace("\\n", Environment.NewLine);
                status.Update();
                break;
            }
        });
    }

    /// <summary>
    /// Helper method to safely open URLs in .NET Core/.NET 8+.
    /// In .NET 8 UseShellExecute defaults to false, which prevents URLs from opening without this flag.
    /// </summary>
    /// <param name="url">The URL to open in the default browser.</param>
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch(Exception ex)
        {
            ShowError($"{Resources.OpenFailed}: {ex.Message}");
        }
    }

    /// <summary>
    /// Displays an error message to the user in a message box with an error icon.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void ShowError(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        MessageBox.Show(this, message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// Displays an informational message to the user in a message box with an information icon.
    /// </summary>
    /// <param name="message">The information message to display.</param>
    public void ShowInfo(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        MessageBox.Show(this, message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Displays a confirmation dialog to the user with customizable buttons.
    /// </summary>
    /// <param name="message">The question or confirmation message to display.</param>
    /// <param name="buttons">The button configuration for the dialog; defaults to YesNo.</param>
    /// <returns>The dialog result indicating which button the user clicked.</returns>
    public DialogResult Confirm(string message, MessageBoxButtons buttons = MessageBoxButtons.YesNo)
    {
        ArgumentNullException.ThrowIfNull(message);
        return MessageBox.Show(this, message, ProductName, buttons, MessageBoxIcon.Question);
    }

    /// <summary>
    /// Prompts the user to specify a filename for saving a Sudoku puzzle.
    /// Returns the selected filename or an empty string if cancelled.
    /// </summary>
    /// <param name="defaultExt">The default file extension to use (e.g., ".sdk").</param>
    /// <returns>The selected filename, or empty string if the user cancels the dialog.</returns>
    public string AskForFilename(string defaultExt)
    {
        ArgumentNullException.ThrowIfNull(defaultExt);
        String filename = String.Empty;
        saveSudokuDialog.InitialDirectory = settings?.ProblemDirectory ?? string.Empty;
        saveSudokuDialog.RestoreDirectory = true;
        saveSudokuDialog.DefaultExt = "*" + defaultExt;
        saveSudokuDialog.Filter = String.Format(cultureInfo ?? CultureInfo.InvariantCulture, Resources.FilterString, defaultExt);
        saveSudokuDialog.FileName = "Problem-" + DateTime.Now.ToString("yyyy.MM.dd-hh-mm", cultureInfo ?? CultureInfo.InvariantCulture);
        if(saveSudokuDialog.ShowDialog() == DialogResult.OK)
            filename = saveSudokuDialog.FileName;
        return filename;
    }

    /// <summary>
    /// Updates the GUI display by refreshing menu items, buttons, and text in the correct language.
    /// Applies localized resources to all menu items at multiple hierarchy levels and status bar elements.
    /// Clears any existing status text and calls FormatTable to recalculate grid layout.
    /// </summary>
    private void UpdateGUI()
    {
        FormatTable();

        ComponentResourceManager resources = new ComponentResourceManager(typeof(SudokuForm));
        for(int i = 0; i < sudokuMenu.Items.Count; i++)
        {
            var item = sudokuMenu.Items[i];
            ToolStripMenuItem? mi = item as ToolStripMenuItem;
            resources.ApplyResources(item, item?.Name ?? string.Empty);
            if(mi?.HasDropDownItems == true)
                for(int j = 0; j < mi.DropDownItems.Count; j++)
                {
                    if(mi.DropDownItems[j] is ToolStripMenuItem)
                    {
                        ToolStripMenuItem ddm = (ToolStripMenuItem)mi.DropDownItems[j];
                        resources.ApplyResources(mi.DropDownItems[j], mi.DropDownItems[j].Name ?? string.Empty);
                        if(ddm.HasDropDownItems)
                            for(int k = 0; k < ddm.DropDownItems.Count; k++)
                                resources.ApplyResources(ddm.DropDownItems[k], ddm.DropDownItems[k].Name ?? string.Empty);
                    }
                }
        }
        resources.ApplyResources(sudokuStatusBarText, sudokuStatusBarText.Name ?? string.Empty);
        status.Text = String.Empty;
    }

    /// <summary>
    /// Sets the layout and visual appearance of the Sudoku grid based on settings (contrast, colors, etc.).
    /// Calls FormatBoard on the SudokuGrid, resizes the form to accommodate grid dimensions,
    /// and refreshes the grid display to redraw all cell hints and visual elements.
    /// </summary>
    private void FormatTable()
    {
        SudokuGrid.FormatBoard();
        ResizeForm();
        // to allow all cell-hints to be redrawn, the table itself must be redrawn
        SudokuGrid.Refresh();
    }

    /// <summary>
    /// Resizes the form window to fit all Sudoku grid and UI elements appropriately.
    /// Calculates required dimensions based on grid size, DPI scaling, and layout margins.
    /// Repositions status bar elements and navigation buttons to align with the grid dimensions.
    /// </summary>
    private void ResizeForm()
    {
        int height = SudokuGrid.ResizeBoard();

        int newClientWidth = SudokuGrid.Location.X + SudokuGrid.Width + SudokuGrid.Location.X;
        int newClientHeight = height + 140 + (int)(60 * settings.Size * (float)DeviceDpi / 96f);

        ClientSize = new Size(newClientWidth, newClientHeight);

        status.Location = new Point(status.Location.X, SudokuGrid.Bottom + 10);
        next.Location = new Point(SudokuGrid.Location.X + SudokuGrid.Width - next.Width, status.Location.Y);
        prior.Location = new Point(SudokuGrid.Location.X + SudokuGrid.Width - next.Width - prior.Width - 5, status.Location.Y);
    }
    /// <summary>
    /// Updates and displays the current game status including filled cells count, puzzle validity, and completion status.
    /// Starts the game timer on first call, validates puzzle state if auto-check is enabled, and plays appropriate sounds.
    /// Displays a congratulations message if the puzzle is solved and updates status text accordingly.
    /// </summary>
    /// <param name="silent">If true, suppresses the congratulations message when the puzzle is completed. Useful for non-interactive status updates.</param>
    private void CurrentStatus(Boolean silent)
    {
        if(!controller.IsTimerRunning)
        {
            controller.StartTimer();
            statusUpdateTimer.Start();
        }

        Boolean inputOK = SudokuGrid.SyncProblemWithGUI(true, autoCheck.Checked);

        ResetTexts();
        status.Text = Resources.FilledCells + SudokuGrid.FilledCells;

        if(autoCheck.Checked && (!inputOK || !controller.IsProblemResolvable()))
        {
            status.Text += (Environment.NewLine + Resources.NotResolvable);
            System.Media.SystemSounds.Hand.Play();
        }

        if(!silent && SudokuGrid.IsCompleted)
        {
            controller.StopTimer();
            statusUpdateTimer.Stop();
            status.ForeColor = Color.Green;
            status.Text += " - " + Resources.ProblemSolved;

            System.Media.SystemSounds.Asterisk.Play();

            ShowInfo(inputOK ?
                Resources.Congratulations + Environment.NewLine + Resources.ProblemSolved + Environment.NewLine + Resources.TimeNeeded + String.Format("{0:0#}:{1:0#}:{2:0#},{3:0#}", controller.CurrentProblem.SolvingTime.Days * 24 + controller.CurrentProblem.SolvingTime.Hours, controller.CurrentProblem.SolvingTime.Minutes, controller.CurrentProblem.SolvingTime.Seconds, controller.CurrentProblem.SolvingTime.Milliseconds) :
                Resources.ProblemNotSolved);

            status.ForeColor = Color.Black;
            sudokuStatusBarText.Text = Resources.Ready;
        }
    }
    /// <summary>
    /// Sets the status text to indicate that puzzle generation has been aborted by the user.
    /// Restores the previous puzzle state from backup and displays the original puzzle values.
    /// Resets any detached background processes and updates the UI accordingly.
    /// </summary>
    private void GenerationAborted()
    {
        status.Text = controller.GenerationAborted();
        status.Update();
        ResetDetachedProcess();
        controller.RestoreProblem();
        SudokuGrid.DisplayValues();
    }

    /// <summary>
    /// Displays the current progress status of an ongoing puzzle generation operation in the status bar.
    /// Shows elapsed time and other generation metrics based on current controller state.
    /// </summary>
    /// <param name="elapsed">The TimeSpan representing the elapsed time since generation started.</param>
    private void GenerationStatus(TimeSpan elapsed)
    {
        status.Text = controller.GenerationStatus(usePrecalculatedProblem, generationTimer.Elapsed);
        status.Update();
    }
    
    /// <summary>
    /// Resets all status text labels to their default or empty values.
    /// Disables solution navigation buttons, resets form title to product name,
    /// and clears status bar text unless the timer is actively running.
    /// </summary>
    private void ResetTexts()
    {
        status.Text = String.Empty;
        prior.Enabled = next.Enabled = false;
        if(!controller.IsTimerRunning) sudokuStatusBarText.Text = Resources.Ready;
        Text = ProductName;
    }

    /// <summary>
    /// Clears the undo operation stack and disables the undo menu option.
    /// Ensures that no previous operations can be undone after calling this method.
    /// Delegates to the SudokuGrid's undo reset mechanism.
    /// </summary>
    private void ResetUndo()
    {
        SudokuGrid.ResetUndo();
        undo.Enabled = false;
    }

    /// <summary>
    /// Validates whether the current puzzle in the controller is valid and solvable.
    /// Performs a pre-check by verifying that the grid is in sync with the controller state
    /// and that the puzzle can be resolved using solving algorithms.
    /// </summary>
    /// <returns>True if the puzzle is valid and resolvable; false if validation fails or puzzle state is inconsistent.</returns>
    private Boolean PreCheck()
    {
        if(!SudokuGrid.InSync || !controller.IsProblemResolvable())
        {
            CheckProblem();
            return false;
        }
        return true;
    }

    /// <summary>
    /// Sets the puzzle grid to read-only (locked) or editable mode.
    /// When locking (readOnly=true), synchronizes puzzle state with GUI and validates the puzzle is valid before locking.
    /// Disables or enables grid cell editing based on the readOnly parameter.
    /// </summary>
    /// <param name="readOnly">If true, locks the puzzle for solving mode; if false, enables editing mode.</param>
    private void SetReadOnly(Boolean readOnly)
    {
        if(readOnly && !SudokuGrid.SyncProblemWithGUI(true, autoCheck.Checked))
        {
            ShowInfo(Resources.NotFixable);
            return;
        }
        SudokuGrid.SetReadOnly(readOnly);
    }

    private void CheckVersion()
    {
        if(settings.LastVersion != AssemblyInfo.AssemblyVersion)
            VersionHistoryClicked(this, EventArgs.Empty);
        settings.LastVersion = AssemblyInfo.AssemblyVersion;
    }

    /// <summary>
    /// Generates a new batch of Sudoku puzzles asynchronously with the specified parameters.
    /// Updates the UI with progress information and handles cancellation via the form's cancellation token.
    /// Supports both generating new puzzles and loading pre-calculated puzzle sets.
    /// </summary>
    /// <param name="count">The number of puzzles to generate in the batch.</param>
    /// <param name="usePrecalculated">If true, loads pre-calculated puzzles; if false, generates new puzzles.</param>
    private async void GenerateProblems(int nProblems, Boolean xSudoku)
    {
        SudokuGrid.CreateNewProblem(xSudoku);
        generationTimer.Reset();
        generationTimer.Start();

        severityLevel = controller.GetSeverityLevel(nProblems);
        if(severityLevel == 0) return; // Abbrechen

        DisableGUI();

        var progress = new Progress<GenerationProgressState>(state =>
        {
            SudokuGrid.UpdateProblemState(state);
            if(state.StatusText != null)
                GenerationStatus(controller.CurrentProblem.GenerationTime);
        });

        InitializeFormCTS();
        try
        {
            sudokuStatusBarText.Text = usePrecalculatedProblem ? Resources.Loading : Resources.Generating;

            await controller.GenerateBatch(severityLevel, usePrecalculatedProblem, new Action<object, string>(GenerationFinished), progress, minimizationProgress, FormCTS.Token);
        }
        catch(OperationCanceledException)
        {
            GenerationAborted();
        }
        catch(Exception ex)
        {
            ShowError("Error generating: " + ex.Message);
            GenerationAborted();
        }
        finally
        {
            generationTimer.Stop();
            generationTimer.Reset();
            EnableGUI();
        }
    }
    
    /// <summary>
    /// Displays definite or computable values in the current puzzle using simplified solving rules.
    /// Backs up the current puzzle state, computes determined cell values, and updates the grid display.
    /// Resets undo history and displays puzzle statistics in the status bar.
    /// </summary>
    private void ShowDefiniteValues()
    {
        if(!PreCheck()) return;

        controller.BackupProblem();
        controller.CurrentProblem.PrepareMatrix();
        SudokuGrid.DisplayValues();
        ResetUndo();
        SudokuGrid.SyncProblemWithGUI(true, autoCheck.Checked);
        status.Text = String.Format(cultureInfo, Resources.ProblemInfo.Replace("\\n", Environment.NewLine), controller.GetFilledCellCount - controller.GetComputedCellCount, controller.GetComputedCellCount, controller.GetVariableCellCount);
        status.Update();
    }

    /// <summary>
    /// Asynchronously retrieves and displays visual hints for solving the current puzzle.
    /// Highlights cells that can be determined using standard Sudoku solving techniques.
    /// Shows an information message if no hints are available.
    /// </summary>
    private async void Hints()
    {
        if(!PreCheck()) return;

        List<BaseCell> hints = controller.GetHints();
        if(hints.Count == 0)
        {
            ShowInfo(Resources.NoHints);
            return;
        }

        await SudokuGrid.VisualizeHints(hints);
    }

    /// <summary>
    /// Displays comprehensive information about the current puzzle including filled/computed cells,
    /// difficulty level, solvability status, file path, and comments.
    /// Preserves the puzzle's modified flag during the display operation.
    /// </summary>
    private void DisplayProblemInfo()
    {
        String problemInfo;
        Boolean modified = controller.CurrentProblem.Dirty;
        Boolean problemValid = SudokuGrid.SyncProblemWithGUI(true, autoCheck.Checked);

        problemInfo = Resources.PreAllocatedValues + controller.CurrentProblem.nValues.ToString(cultureInfo);
        controller.CurrentProblem.PrepareMatrix();
        problemInfo += Environment.NewLine + Resources.DefiniteCells + controller.CurrentProblem.nComputedValues.ToString(cultureInfo);
        controller.CurrentProblem.ResetMatrix();
        if(problemValid)
            problemInfo += Environment.NewLine + Resources.ComplexityLevel + controller.CurrentProblem.SeverityLevelCategory.ToDisplayText() + " (" + String.Format(cultureInfo, "{0:0.00}", controller.CurrentProblem.SeverityLevel) + ")";
        problemInfo += Environment.NewLine +
            (controller.CurrentProblem.ProblemSolved ? String.Format(cultureInfo, Resources.CheckResult, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic, Resources.AtLeast) :
             controller.IsProblemResolvable() && problemValid ? String.Format(cultureInfo, Resources.ValidationStatus, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic) : Resources.NotResolvable);
        if(!String.IsNullOrEmpty(controller.CurrentProblem.Filename))
            problemInfo += Environment.NewLine + Resources.Filename + Environment.NewLine + controller.CurrentProblem.Filename;
        if(!String.IsNullOrEmpty(controller.CurrentProblem.Comment))
            problemInfo += Environment.NewLine + controller.CurrentProblem.Comment;
        ShowInfo(problemInfo);
        controller.CurrentProblem.Dirty = modified;
    }

    /// <summary>
    /// Displays detailed information about a specific cell in the puzzle including candidates,
    /// constraints violated, and solving techniques that could determine the cell value.
    /// </summary>
    /// <param name="row">The zero-based row index of the cell (0-8).</param>
    /// <param name="col">The zero-based column index of the cell (0-8).</param>
    private void DisplayCellInfo(int row, int col)
    {
        // TODO:
        // Die Gründe für die indirekten Blocks ausgeben (pair, ...)
        String cellInfo = controller.GetCellInfoText(row, col);
        ShowInfo(cellInfo);
        return;
    }

    /// <summary>
    /// Initializes the pause overlay control that displays when the puzzle game is paused.
    /// Creates a semi-transparent label covering the entire form with pause message and click-to-resume functionality.
    /// Stops timers and hides puzzle values when initialized.
    /// </summary>
    private void InitializePauseOverlay()
    {
        if(pauseOverlay != null) return;

        pauseOverlay = new Label();
        pauseOverlay.Text = Resources.PausedMessage.Replace("\\n", Environment.NewLine);
        pauseOverlay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        pauseOverlay.Dock = DockStyle.Fill;

        pauseOverlay.BackColor = Color.FromArgb(200, 255, 255, 255);
        pauseOverlay.ForeColor = Color.DarkSlateGray;

        pauseOverlay.Font = new Font(this.Font.FontFamily, 24, FontStyle.Bold);
        pauseOverlay.Visible = false;
        pauseOverlay.Cursor = Cursors.Hand;

        pauseOverlay.Click += (s, e) => ResumeGame();

        this.Controls.Add(pauseOverlay);
        pauseOverlay.BringToFront();
        controller.StopTimer();
        statusUpdateTimer.Stop();
    }

    /// <summary>
    /// Resumes a paused game by hiding the pause overlay and restarting the game timer.
    /// Removes the "paused" indicator from the status bar and restores puzzle value visibility.
    /// Restarts status update timer for clock display.
    /// </summary>
    private void ResumeGame()
    {
        if(pauseOverlay != null) pauseOverlay.Visible = false;

        sudokuStatusBarText.Text = sudokuStatusBarText.Text?.Replace(Resources.Paused, "").Trim();
        SudokuGrid.ShowValues();
        controller.StartTimer();
    }
    /// <summary>
    /// Asynchronously publishes difficult or tricky puzzles to a designated publication server or destination.
    /// Confirms with the user before publishing and provides feedback on success or failure.
    /// Only proceeds if the controller has identified tricky problems worth publishing.
    /// </summary>
    private async void PublishTrickyProblems()
    {
        if(!controller.HasTrickyProblems()) return;

        if(Confirm((controller.GenerateBooklet ? Resources.OneOrMoreProblems : Resources.OneProblem) + Resources.Publish) == DialogResult.Yes)
        {
            if(await controller.PublishTrickyProblems())
                ShowInfo(String.Format(Resources.PublishOK, controller.NumberOfTrickyProblems));
            else
                ShowInfo(String.Format(Resources.PublishFailed, settings.MailAddress));
        }
    }

    /// <summary>
    /// Validates the current puzzle state for correctness and displays validation results.
    /// Synchronizes puzzle data with GUI input, checks puzzle validity, and reports resolvability status.
    /// Handles invalid puzzles by displaying appropriate error messages.
    /// </summary>
    private void CheckProblem()
    {
        if(SudokuGrid.SyncProblemWithGUI(false, autoCheck.Checked))
            ShowInfo(controller.IsProblemResolvable() ? String.Format(cultureInfo, Resources.ValidationStatus, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic) : Resources.NotResolvable);
        else
            ShowInfo(Resources.InvalidProblem + Environment.NewLine + Resources.NotResolvable);
    }

    /// <summary>
    /// Asynchronously validates the current puzzle and determines all possible solutions.
    /// Displays progress during validation and reports the total number of solutions found.
    /// Respects the "Find All Solutions" setting for extended validation.
    /// Handles cancellation via the form's cancellation token.
    /// </summary>
    private async void ValidateProblem()
    {
        if(!PreCheck()) return;

        DisableGUI();
        sudokuStatusBarText.Text = Resources.Checking;

        // Setup cancellation
        InitializeFormCTS();

        var progress = new Progress<GenerationProgressState>(state =>
        {
            status.Text = String.Format(cultureInfo, Resources.CheckingStatus, state.PassCount) + Environment.NewLine + Resources.TimeElapsed + state.Elapsed.ToString(); // Formatierung ggf. anpassen
            status.Update();
            if(traceMode.Checked) SudokuGrid.Update();
        });

        try
        {
            bool solvable = await controller.Validate(progress, FormCTS.Token);

            status.Text = String.Empty;
            sudokuStatusBarText.Text = Resources.Ready;

            ShowInfo(String.Format(cultureInfo, Resources.CheckResult, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic, solvable ? Resources.AtLeast : Resources.No));
        }
        catch(OperationCanceledException)
        {
            sudokuStatusBarText.Text = Resources.Ready;
            status.Text = Resources.GenerationAborted;
        }
        catch(Exception ex)
        {
            ShowInfo("Error validating: " + ex.Message);
        }
        finally
        {
            EnableGUI();
        }
    }
    /// <summary>
    /// Resets the current puzzle to its original state by restoring from backup.
    /// Clears all solutions found, resets undo history, resets text labels,
    /// and restores the grid to initial puzzle state with original values displayed.
    /// </summary>
    private void ResetProblem()
    {
        controller.RestoreProblem();
        controller.ResetSolutions();
        ResetUndo();
        ResetTexts();
        SudokuGrid.ResetMatrix();
        SudokuGrid.DisplayValues();
    }
    
    /// <summary>
    /// Asynchronously loads the Sudoku of the Day puzzle from a server or predefined source.
    /// Updates the GUI and resets grid formatting after successful loading.
    /// Clears undo history and resets cell font settings.
    /// </summary>
    /// <returns>True if the Sudoku of the Day was successfully loaded and displayed; false if loading failed.</returns>
    private async Task<Boolean> SudokuOfTheDay()
    {
        if(await controller.SudokuOfTheDay())
        {
            UpdateGUI();
            SudokuGrid.SetCellFont();
            ResetUndo();
            return true;
        }
        else
            return false;
    }
    /// <summary>
    /// Creates a new puzzle from a file and loads it into the current controller.
    /// Enables cell and block boundaries display and prepares the puzzle for solving or editing.
    /// </summary>
    /// <param name="filename">The full path to the file containing the puzzle definition.</param>
    private void CreateProblemFromFile(String filename)
    {
        controller.CreateProblemFromFile(filename, true, true, true);
    }
    /// <summary>
    /// Displays the next solution in the solutions list.
    /// Increments the solution index and updates button states and form title to reflect the current solution number.
    /// </summary>
    private void NextSolution()
    {
        SudokuGrid.DisplayValues(controller.CurrentProblem.Solutions[++currentSolution]);
        next.Enabled = (currentSolution < controller.CurrentProblem.Solutions.Count - 1);
        prior.Enabled = (currentSolution > 0);
        Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
    }

    /// <summary>
    /// Displays the previous solution in the solutions list.
    /// Decrements the solution index and updates button states and form title to reflect the current solution number.
    /// </summary>
    private void PriorSolution()
    {
        SudokuGrid.DisplayValues(controller.CurrentProblem.Solutions[--currentSolution]);
        prior.Enabled = (currentSolution > 0);
        next.Enabled = (controller.CurrentProblem.Solutions.Count > 1);
        Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
    }

    /// <summary>
    /// Resets the application state after a detached or background process completes.
    /// Re-enables the GUI and sets the status bar to ready state.
    /// </summary>
    private void ResetDetachedProcess()
    {
        sudokuStatusBarText.Text = Resources.Ready;
        EnableGUI();
    }

    /// <summary>
    /// Checks whether the current puzzle has unsaved changes and prompts the user to save if necessary.
    /// Returns false if the user cancels the operation, true otherwise.
    /// Only prompts if the puzzle is marked as dirty and not yet completed.
    /// </summary>
    /// <returns>True if the user confirmed to continue (saving optional); False if user cancelled the operation.</returns>
    private Boolean UnsavedChanges()
    {
        Boolean rc = true;
        DialogResult dialogResult;

        if(controller.CurrentProblem.Dirty && !SudokuGrid.IsCompleted)
        {
            dialogResult = Confirm(Resources.UnsavedChanges, MessageBoxButtons.YesNoCancel);
            if(dialogResult == DialogResult.Yes)
                rc = SaveProblem();
            else
                rc = (dialogResult == DialogResult.No);
        }
        return rc;
    }

    /// <summary>
    /// Opens a file dialog for user to select and load an existing Sudoku puzzle file.
    /// First checks for unsaved changes in the current puzzle.
    /// Sets the file filter and initial directory from application settings.
    /// </summary>
    private void OpenProblem()
    {
        if(UnsavedChanges())
        {
            openSudokuDialog.InitialDirectory = settings.ProblemDirectory;
            openSudokuDialog.DefaultExt = "*" + settings.DefaultFileExtension;
            openSudokuDialog.Filter = String.Format(cultureInfo, Resources.FilterString, settings.DefaultFileExtension);
            if(openSudokuDialog.ShowDialog() == DialogResult.OK)
                LoadProblem(openSudokuDialog.FileName);
        }
    }

    /// <summary>
    /// Loads a Sudoku problem from the specified file and updates the application state.
    /// </summary>
    /// <param name="filename">The path to the file containing the Sudoku problem to load.</param>
    /// <remarks>
    /// This method creates a backup of the current problem before attempting to load the new one.
    /// If loading fails, the previous problem state is restored and an error message is displayed.
    /// After successful loading, the GUI is updated and the undo history is cleared.
    /// </remarks>
    /// <exception cref="ArgumentException">Caught and handled; displays an invalid Sudoku file error message.</exception>
    /// <exception cref="InvalidDataException">Caught and handled; displays an invalid Sudoku identifier error message.</exception>
    /// <exception cref="Exception">Caught and handled; displays a generic file open error message with exception details.</exception>
    private void LoadProblem(String filename)
    {
        BaseProblem tmp = controller.CurrentProblem.Clone();

        try
        {
            CreateProblemFromFile(filename);
        }
        catch(ArgumentException)
        {
            ShowError(String.Format(cultureInfo, Resources.InvalidSudokuFile, filename));
            controller.UpdateProblem(tmp);
        }
        catch(InvalidDataException)
        {
            ShowError(Resources.InvalidSudokuIdentifier);
            controller.UpdateProblem(tmp);
        }
        catch(Exception e)
        {
            ShowError(Resources.OpenFailed + Environment.NewLine + e.Message);
            controller.UpdateProblem(tmp);
        }

        controller.BackupProblem();

        UpdateGUI();
        SudokuGrid.SetCellFont();
        ResetUndo();
    }

    /// <summary>
    /// Saves the current puzzle to a file.
    /// Prompts the user for a filename if the puzzle hasn't been previously saved.
    /// Handles file I/O errors and provides user feedback on success or failure.
    /// </summary>
    /// <returns>True if the puzzle was successfully saved; false if the operation was cancelled or failed.</returns>
    private Boolean SaveProblem(String filename)
    {
        Boolean returnCode = true;
        try
        {
            controller.SaveProblem(filename);
        }
        catch(Exception e)
        {
            ShowError(Resources.SaveFailed + Environment.NewLine + e.Message);
            returnCode = false;
        }
        return returnCode;
    }

    /// <summary>
    /// Exports the current puzzle to an HTML file.
    /// Attempts to export the current problem using the controller's <c>ExportHTML</c> method.
    /// If an error occurs during export, an error message is shown to the user and the method returns <c>false</c>.
    /// </summary>
    /// <param name="filename">The destination file path for the exported HTML.</param>
    /// <returns>
    /// <c>true</c> if the export completed successfully; otherwise <c>false</c> if an exception was caught during export.
    /// </returns>
    /// <remarks>
    /// This method handles exceptions internally and reports errors to the user via <c>ShowError</c>.
    /// It does not rethrow exceptions to the caller.
    /// </remarks>
    private Boolean ExportProblem(String filename)
    {
        Boolean returnCode = true;
        try
        {
            controller.ExportHTML(filename);
        }
        catch(Exception e)
        {
            ShowError(Resources.SaveFailed + Environment.NewLine + e.Message);
            returnCode = false;
        }
        return returnCode;
    }
    /// <summary>
    /// Saves the current puzzle by prompting the user for a filename and delegating the actual save to the
    /// <c>SaveProblem(String)</c> overload.
    /// The method first synchronizes the GUI with the internal problem representation. If synchronization fails,
    /// an informational message is shown and no save dialog is displayed.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the problem was saved successfully; otherwise <c>false</c>. This includes cases where the
    /// problem is invalid or the user cancels the save file dialog.
    /// </returns>
    /// <remarks>
    /// This method displays UI dialogs (<c>ShowInfo</c> and the save file dialog) and relies on
    /// <c>SaveProblem(String)</c> to perform the file write. No exceptions are propagated from this method.
    /// </remarks>
    private Boolean SaveProblem()
    {
        if(!SudokuGrid.SyncProblemWithGUI(true, false))
        {
            ShowInfo(Resources.InvalidProblem + Environment.NewLine + Resources.SaveNotPossible);
            return false;
        }

        if(AskForFilename(settings.DefaultFileExtension) != String.Empty)
            return SaveProblem(saveSudokuDialog.FileName);
        else
            return false;
    }
    /// <summary>
    /// Exports the current puzzle as HTML by prompting the user for a filename and delegating the actual export to
    /// the <c>ExportProblem(String)</c> overload.
    /// The method first synchronizes the GUI with the internal problem representation. If synchronization fails,
    /// an error message is shown and no export dialog is displayed.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the problem was exported successfully; otherwise <c>false</c>. This includes cases where the
    /// problem is invalid or the user cancels the save file dialog.
    /// </returns>
    /// <remarks>
    /// Displays an error dialog via <c>ShowError</c> on invalid problems and uses <c>AskForFilename</c> with the
    /// HTML file extension from <c>settings.HTMLFileExtension</c>. The actual file export is performed by
    /// <c>ExportProblem(String)</c>. No exceptions are propagated from this method.
    /// </remarks>
    private Boolean ExportProblem()
    {
        if(!SudokuGrid.SyncProblemWithGUI(true, false))
        {
            ShowError(Resources.InvalidProblem + Environment.NewLine + Resources.ExportNotPossible);
            return false;
        }

        if(AskForFilename(settings.HTMLFileExtension) != String.Empty)
            return ExportProblem(saveSudokuDialog.FileName);
        else
            return false;
    }
    /// <summary>
    /// Attempts to share the current Sudoku problem via Twitter.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the current problem was successfully synchronized with the GUI and the Twitter URL was opened;
    /// otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The method first calls <c>SudokuGrid.SyncProblemWithGUI(true, false)</c> to validate and synchronize the GUI
    /// state with the internal problem representation. If synchronization fails an error dialog is displayed via
    /// <c>ShowError</c> and the method returns <c>false</c>. On success the Twitter share URL from
    /// <c>controller.TwitterURL</c> is opened using <c>OpenUrl</c>. This method performs UI interactions and does not
    /// propagate exceptions.
    /// </remarks>
    private Boolean TwitterProblem()
    {
        if(!SudokuGrid.SyncProblemWithGUI(true, false))
        {
            ShowError(Resources.InvalidProblem + Environment.NewLine + Resources.TwitterNotPossible);
            return false;
        }

        OpenUrl(controller.TwitterURL);

        return true;
    }
    // Diverse Events
    /// <summary>
    /// Handles completion of single puzzle generation by updating GUI and displaying status.
    /// </summary>
    /// <param name="puzzleData">The puzzle data as a string from the generation callback.</param>
    private void GenerationSingleProblemFinished(String puzzleData)
    {
        TimeSpan elapsed = generationTimer.Elapsed;

        status.Text = usePrecalculatedProblem ? Resources.ProblemRetrieved : puzzleData + Environment.NewLine + Resources.TimeNeeded + String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours * 24 + elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
        SudokuGrid.DisplayValues(controller.CurrentProblem.Matrix);
        PublishTrickyProblems();
        ResetDetachedProcess();
        ShowInfo(status.Text);
    }

    /// <summary>
    /// Handles completion of booklet (batch) puzzle generation.
    /// Updates GUI to display the generated puzzles in batch format.
    /// </summary>
    /// <param name="puzzleData">The puzzle collection data as a string from the generation callback.</param>
    private async void GenerationBookletProblemFinished(String puzzleData)
    {
        status.Text = puzzleData;
        try
        {
            PrintBooklet();
        }
        catch(Exception ex)
        {
            ShowError("Error printing booklet: " + ex.Message);
        }
        PublishTrickyProblems();
        ResetTexts();
        ResetDetachedProcess();
        controller.CurrentProblem.Dirty = false;
    }

    /// <summary>
    /// Enables or disables GUI controls based on application state and available operations.
    /// Processes menu items with tags to conditionally enable/disable based on current puzzle and controller state.
    /// Updates button states for undo, solution navigation, and other context-dependent controls.
    /// </summary>
    /// <param name="enable">If true, enables appropriate controls based on state; if false, disables all interactive controls.</param>
    private void EnableGUI(Boolean enable)
    {
        const String disableTag = "disable";
        int disableTagLength = disableTag.Length;
        String menuTag = String.Empty;

        for(int i = 0; i < sudokuMenu.Items.Count; i++)
        {
            ToolStripMenuItem mi = (ToolStripMenuItem)sudokuMenu.Items[i];
            if(mi.HasDropDownItems)
                for(int j = 0; j < mi.DropDownItems.Count; j++)
                {
                    if(mi.DropDownItems[j] is ToolStripMenuItem)
                    {
                        ToolStripMenuItem ddm = (ToolStripMenuItem)mi.DropDownItems[j];
                        if(mi.DropDownItems[j].Tag != null)
                        {
                            menuTag = mi.DropDownItems[j].Tag!.ToString()!;
                            if(!String.IsNullOrEmpty(menuTag) && menuTag.StartsWith(disableTag))
                                mi.DropDownItems[j].Enabled = ((menuTag.Substring(disableTagLength + 1, 1) == "1") == enable);
                        }
                        if(ddm.HasDropDownItems)
                            for(int k = 0; k < ddm.DropDownItems.Count; k++)
                            {
                                if(ddm.DropDownItems[k].Tag != null)
                                {
                                    menuTag = ddm.DropDownItems[k].Tag!.ToString()!;
                                    if(!String.IsNullOrEmpty(menuTag) && menuTag.StartsWith(disableTag))
                                        ddm.DropDownItems[k].Enabled = ((menuTag.Substring(disableTagLength + 1, 1) == "1") == enable);
                                }
                            }
                    }
                }
        }
        undo.Enabled = controller.CanUndo() && enable;
        resetTimer.Enabled = controller.IsTimerRunning && enable;
        clearCandidates.Enabled = controller.CurrentProblem.HasCandidates() && enable;
        next.Enabled = (currentSolution < controller.CurrentProblem.Solutions.Count - 1) && enable;
        prior.Enabled = (currentSolution > 0) && enable;

        if(SudokuGrid.Enabled = enable)
            SudokuGrid.Focus();
    }

    /// <summary>
    /// Enables all interactive GUI controls and sets focus to the Sudoku grid.
    /// Delegates to EnableGUI(true) to perform the actual enabling.
    /// </summary>
    public void EnableGUI()
    {
        EnableGUI(true);
    }

    /// <summary>
    /// Disables all interactive GUI controls to prevent user interaction during long-running operations.
    /// Stops the game timer and status update timer.
    /// Delegates to EnableGUI(false) to perform the actual disabling.
    /// </summary>
    public void DisableGUI()
    {
        EnableGUI(false);
        controller.StopTimer();
        statusUpdateTimer.Stop();
    }

    /// <summary>
    /// Retrieves the desired severity level for puzzle generation from user input or settings.
    /// If settings specify that severity should be user-selected, displays a dialog.
    /// Otherwise, returns the default severity level from settings.
    /// </summary>
    /// <returns>The selected severity level (0-based), or 0 if the user cancels selection.</returns>
    public int GetSeverity()
    {
        if(settings.SelectSeverity)
        {
            SeverityLevelDialog severityLevelDialog = new SeverityLevelDialog();

            if(severityLevelDialog.ShowDialog() == DialogResult.OK)
                return severityLevelDialog.SeverityLevel;
            else
                return 0;
        }
        else
            return settings.SeverityLevel;
    }

    /// <summary>
    /// Asynchronously solves the current puzzle using the selected solving algorithm.
    /// Displays progress during solving and reports solving time and pass count.
    /// Optionally displays found solutions for user review.
    /// Handles multiple solutions if "Find All Solutions" is enabled.
    /// Supports cancellation via the form's cancellation token.
    /// </summary>
    /// <param name="showResult">If true, displays result dialog; if false, silently updates grid with solution.</param>
    private async Task SolveProblem(Boolean showResult = true)
    {
        if(!PreCheck()) return;

        controller.BackupProblem();
        DisableGUI();
        DateTime computingStart = DateTime.Now;

        InitializeFormCTS();

        var progress = new Progress<GenerationProgressState>(state =>
        {
            state.Elapsed = DateTime.Now - computingStart;

            string timeStr = String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#}", state.Elapsed.Hours, state.Elapsed.Minutes, state.Elapsed.Seconds);

            sudokuStatusBarText.Text = $"{state.StatusText} | {Resources.TimeElapsed} {timeStr}";
            if(findallSolutions.Checked)
            {
                status.Text = String.Format(cultureInfo, Resources.SolutionsSoFar, state.SolutionCount);
            }
        });

        try
        {
            await controller.Solve(findallSolutions.Checked, progress, FormCTS.Token);
            TimeSpan elapsed = DateTime.Now - computingStart;

            if(controller.CurrentProblem.ProblemSolved || controller.CurrentProblem.NumberOfSolutions > 0)
            {
                if(controller.CurrentProblem.NumberOfSolutions > 0)
                {
                    status.Text = Resources.ProblemSolved + Environment.NewLine + Resources.TimeNeeded + String.Format("{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds) + (findallSolutions.Checked ? Environment.NewLine + Resources.TotalNumberOfSolutions + controller.CurrentProblem.NumberOfSolutions.ToString("n0", cultureInfo) : String.Empty) + Environment.NewLine + Resources.NeededPasses + controller.CurrentProblem.TotalPassCounter.ToString("n0", cultureInfo);
                    currentSolution = -1;
                    NextSolution();
                }
                else
                    status.Text = Resources.NotResolvable + Environment.NewLine + Resources.TimeNeeded + String.Format("{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds) + Environment.NewLine + Resources.NeededPasses + controller.CurrentProblem.TotalPassCounter.ToString("n0", cultureInfo);

                sudokuStatusBarText.Text = Resources.Ready;

                string msg = Resources.ProblemSolved;
                if(findallSolutions.Checked)
                    msg += Environment.NewLine + Resources.TotalNumberOfSolutions + controller.CurrentProblem.NumberOfSolutions.ToString("n0", cultureInfo);

                if(showResult) ShowInfo(msg);
            }
            else
            {
                if(showResult) ShowError(Resources.NotResolvable);
            }
            ResetDetachedProcess();
        }
        catch(OperationCanceledException)
        {
            sudokuStatusBarText.Text = Resources.GenerationAborted;
            if(controller.CurrentProblem.NumberOfSolutions > 0)
            {
                TimeSpan elapsed = DateTime.Now - computingStart;

                status.Text = String.Format(cultureInfo, Resources.SolutionsFound, (controller.CurrentProblem.TotalPassCounter > 0 ? Resources.Plural : String.Empty)) + Environment.NewLine + Resources.TimeNeeded + String.Format("{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds) + (findallSolutions.Checked ? Environment.NewLine + Resources.TotalNumberOfSolutionsSoFar + controller.CurrentProblem.NumberOfSolutions.ToString("n0", cultureInfo) : String.Empty) + Environment.NewLine + Resources.NeededPasses + controller.CurrentProblem.TotalPassCounter.ToString("n0", cultureInfo);
                currentSolution = -1;
                NextSolution();
            }
            else
                status.Text = String.Format(cultureInfo, Resources.Interrupt.Replace("\\n", Environment.NewLine), DateTime.Now - computingStart, controller.CurrentProblem.TotalPassCounter);
        }
        catch(Exception ex)
        {
            ShowError("Error: " + ex.Message);
        }
        finally
        {
            EnableGUI();
        }
    }
    private async Task AbortThread()
    {
        if(controller.CurrentProblem == null) return;

        try { FormCTS.Cancel(); }
        catch { /* ignore */ }

        if(FormCTS != null && !FormCTS.IsCancellationRequested)
        {
            FormCTS.Cancel();
        }

        int waited = 0;
        const int waitStep = 50;
        const int maxWait = 5000;

        while(controller.CurrentProblem.SolverTask != null && !controller.CurrentProblem.SolverTask.IsCompleted && waited < maxWait)
        {
            await Task.Delay(waitStep);
            waited += waitStep;
        }

        if(controller.CurrentProblem.SolverTask != null && !controller.CurrentProblem.SolverTask.IsCompleted)
        {
            try { await Task.Run(() => controller.CurrentProblem.SolverTask.Wait(500)); } catch { }
        }

        try
        {
            FormCTS?.Cancel();
            SudokuGrid.DisplayValues(controller.CurrentProblem.Matrix);
            if(controller.CurrentProblem.NumberOfSolutions > 0)
            {
                currentSolution = -1;
                NextSolution();
            }
        }
        catch { }
    }

    /// <summary>
    /// Asynchronously minimizes the current puzzle to achieve the specified maximum severity level.
    /// Disables the GUI during minimization and displays progress information.
    /// Supports cancellation via the form's cancellation token.
    /// Restores the minimal puzzle to the controller, updates the display, and re-enables GUI when complete.
    /// </summary>
    /// <param name="maxSeverity">The maximum severity level allowed for the minimized puzzle. Use int.MaxValue for unconstrained minimization.</param>
    /// <returns>True if minimization succeeded and a minimal puzzle was found; false if minimization was cancelled or failed.</returns>
    public async Task<Boolean> Minimize(int maxSeverity)
    {
        String? oldStatusText = sudokuStatusBarText.Text;
        BaseProblem? minimizedProblem = null;
        Boolean rc = false;

        DisableGUI();
        controller.BackupProblem();
        InitializeFormCTS();

        try
        {
            minimizedProblem = await controller.Minimize(maxSeverity, minimizationProgress, FormCTS.Token);

            if(minimizedProblem != null)
            {
                rc = true;
                controller.UpdateProblem(minimizedProblem);
            }
        }
        finally
        {
            controller.CurrentProblem.ResetMatrix();

            UpdateGUI();
            SudokuGrid.DisplayValues(controller.CurrentProblem.Matrix);
            SudokuGrid.SetCellFont();
            ResetUndo();
            EnableGUI();
            Cursor = Cursors.Default;
            sudokuStatusBarText.Text = oldStatusText;
        }

        return rc;
    }
    public void Cancel()
    {
        try
        {
            if(FormCTS != null && !FormCTS.IsCancellationRequested)
                FormCTS.Cancel();
        }
        catch { }
    }

    /// <summary>
    /// Initializes the main Sudoku controller with all necessary event subscriptions and state restoration.
    /// Creates the controller from the factory or creates a new instance directly if factory is unavailable.
    /// Subscribes to controller events for generation updates and restores application state if saved previously.
    /// Configures grid controller reference and sets up grid event handlers for undo availability, candidates, and status updates.
    /// </summary>
    private void InitializeController()
    {
        if(controllerFactory != null)
            controller = controllerFactory.Create(this);
        else
            controller = new SudokuController(settings, this);

        controller.Generating += (s, e) => OnGenerating(s!, e);
        if(settings.State.Length > 0)
            controller.Deserialize();
        else
            controller.CreateNewProblem(false, false);
        SudokuGrid.Controller = controller;
        SudokuGrid.UndoAvailableChanged += (s, canUndo) => { undo.Enabled = canUndo; };
        SudokuGrid.CandidatesAvailableChanged += (s, hasCandidates) => { clearCandidates.Enabled = hasCandidates; };
        SudokuGrid.UpdateStatus += (s, silent) => { CurrentStatus(silent); };
        SudokuGrid.UpdateHints += (s, e) =>
        {
            if(settings.ShowHints && Confirm(Resources.CandidatesNotShown, MessageBoxButtons.YesNo) == DialogResult.Yes)
                showPossibleValues.Checked = settings.ShowHints = false;
        };
        SudokuGrid.StatusTextChanged += (s, text) =>
        {
            status.Text = text;
            status.Update();
        };
    }
}