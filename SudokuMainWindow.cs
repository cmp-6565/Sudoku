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

namespace Sudoku;

public enum SudokuPart { Row, Column, Block, UpDiagonal, DownDiagonal };

public partial class SudokuForm: Form, IUserInteraction, IDisposable
{
    ISudokuSettings settings = new WinFormsSettings();

    private System.Windows.Forms.Timer autoPauseTimer;
    private System.Windows.Forms.Timer statusUpdateTimer;

    private Stopwatch generationTimer = new Stopwatch();

    private int currentSolution = 0;
    private Boolean AbortRequested { get { if(FormCTS != null) return FormCTS.Token.IsCancellationRequested; return false; } }
    private Boolean applicationExiting = false;
    private CultureInfo cultureInfo;
    private OptionsDialog optionsDialog = null;
    private Boolean usePrecalculatedProblem = false;
    private int severityLevel = 0;

    private SudokuController controller;
    public CancellationTokenSource FormCTS { get; set; }

    // Für das Pause-Overlay
    private Label pauseOverlay;
    private Progress<MinimizationUpdate> minimizationProgress;

    /// <summary>
    /// Constructor for the form, mainly used for defaulting some variables and initializing of the gui.
    /// </summary>
    public SudokuForm()
    {
        Thread.CurrentThread.CurrentUICulture = (cultureInfo = new CultureInfo(settings.DisplayLanguage));

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
    public new void Dispose()
    {
        base.Dispose();
        autoPauseTimer?.Dispose();
        statusUpdateTimer?.Dispose();
        optionsDialog?.Dispose();
        pauseOverlay?.Dispose();
        controller?.Dispose();
    }

    private void InitializeFormCTS()
    {
        try { FormCTS?.Cancel(); } catch { }
        FormCTS?.Dispose();
        FormCTS = new CancellationTokenSource();
    }

    private void InitializeMinimizationProgress()
    {
        minimizationProgress = new Progress<MinimizationUpdate>(update =>
        {
            switch(update.Type)
            {
            case MinimizationUpdateType.TestCell:
                SudokuGrid.HandleOnTestCell(this, update.Cell);
                break;
            case MinimizationUpdateType.ResetCell:
                SudokuGrid.ResetCellVisuals(this, update.Cell);
                break;
            case MinimizationUpdateType.Status:
                status.Text = String.Format(Resources.CurrentMinimalProblem,
                    update.Problem.SeverityLevelText,
                    update.Problem.nValues,
                    controller.CurrentProblem.nValues).Replace("\\n", Environment.NewLine);
                status.Update();
                break;
            }
        });
    }
    /// <summary>
    /// Helper method to safely open URLs in .NET Core/.NET 8+
    /// In .NET 8 UseShellExecute defaults to false, which prevents URLs from opening without this flag.
    /// </summary>
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

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    public void ShowInfo(string message)
    {
        MessageBox.Show(this, message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    public DialogResult Confirm(string message, MessageBoxButtons buttons = MessageBoxButtons.YesNo)
    {
        return MessageBox.Show(this, message, ProductName, buttons, MessageBoxIcon.Question);
    }
    public string AskForFilename(string defaultExt)
    {
        String filename = String.Empty;
        saveSudokuDialog.InitialDirectory = settings.ProblemDirectory;
        saveSudokuDialog.RestoreDirectory = true;
        saveSudokuDialog.DefaultExt = "*" + defaultExt;
        saveSudokuDialog.Filter = String.Format(cultureInfo, Resources.FilterString, defaultExt);
        saveSudokuDialog.FileName = "Problem-" + DateTime.Now.ToString("yyyy.MM.dd-hh-mm", cultureInfo);
        if(saveSudokuDialog.ShowDialog() == DialogResult.OK)
            filename = saveSudokuDialog.FileName;
        return filename;
    }

    private void FocusLost(object sender, EventArgs e)
    {
        if(SudokuGrid.Enabled && settings.AutoPause)
            autoPauseTimer.Start();
    }

    private void FocusGotten(object sender, EventArgs e)
    {
        autoPauseTimer.Stop();
    }

    /// <summary>
    /// Updates the GUI, i.e. displays the texts in the correct language and reformats the table by calling <code>FormatTable()</code>
    /// </summary>
    private void UpdateGUI()
    {
        FormatTable();

        ComponentResourceManager resources = new ComponentResourceManager(typeof(SudokuForm));
        for(int i = 0; i < sudokuMenu.Items.Count; i++)
        {
            ToolStripMenuItem mi = (ToolStripMenuItem)sudokuMenu.Items[i];
            resources.ApplyResources(sudokuMenu.Items[i], sudokuMenu.Items[i].Name);
            if(mi.HasDropDownItems)
                for(int j = 0; j < mi.DropDownItems.Count; j++)
                {
                    if(mi.DropDownItems[j] is ToolStripMenuItem)
                    {
                        ToolStripMenuItem ddm = (ToolStripMenuItem)mi.DropDownItems[j];
                        resources.ApplyResources(mi.DropDownItems[j], mi.DropDownItems[j].Name);
                        if(ddm.HasDropDownItems)
                            for(int k = 0; k < ddm.DropDownItems.Count; k++)
                                resources.ApplyResources(ddm.DropDownItems[k], ddm.DropDownItems[k].Name);
                    }
                }
        }
        resources.ApplyResources(sudokuStatusBarText, sudokuStatusBarText.Name);
        status.Text = String.Empty;
    }

    // GUI Handling
    /// <summary>
    /// Sets the layout of the table, mainly the Contrast set in the application's options
    /// </summary>
    private void FormatTable()
    {
        SudokuGrid.FormatBoard();
        ResizeForm();
        // to allow all cell-hints to be redrawn, the table itself must be redrawn
        SudokuGrid.Refresh();
    }

    private void ResizeForm()
    {
        int height = SudokuGrid.ResizeBoard();

        int newClientWidth = SudokuGrid.Location.X + SudokuGrid.Width + SudokuGrid.Location.X;
        int newClientHeight = height + 140 + (60 * settings.Size);

        ClientSize = new Size(newClientWidth, newClientHeight);

        status.Location = new Point(status.Location.X, SudokuGrid.Bottom + 10);
        next.Location = new Point(SudokuGrid.Location.X + SudokuGrid.Width - next.Width, status.Location.Y);
        prior.Location = new Point(SudokuGrid.Location.X + SudokuGrid.Width - next.Width - prior.Width - 5, status.Location.Y);
    }
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
    /// Sets the status text to a string stating that the current generation has been aborted by the user
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
    /// Displays the current status of the generation of a controller.CurrentProblem in the status field
    /// </summary>
    private void GenerationStatus(TimeSpan elapsed)
    {
        status.Text = controller.GenerationStatus(usePrecalculatedProblem, generationTimer.Elapsed);
        status.Update();
    }

    /// <summary>
    /// Resets all texts to their default values
    /// </summary>
    private void ResetTexts()
    {
        status.Text = String.Empty;
        prior.Enabled = next.Enabled = false;
        if(!controller.IsTimerRunning) sudokuStatusBarText.Text = Resources.Ready;
        Text = ProductName;
    }

    /// <summary>
    /// Clears the undo-stack
    /// </summary>
    private void ResetUndo()
    {
        SudokuGrid.ResetUndo();
        undo.Enabled = false;
    }

    // Misc functions
    /// <summary>
    /// Checks whether or not the current controller.CurrentProblem is valid an solvable
    /// </summary>
    /// <returns>
    /// true: Problem is valid and resolvable
    /// false: otherwise
    /// </returns>
    private Boolean PreCheck()
    {
        if(!SudokuGrid.InSync || !controller.IsProblemResolvable())
        {
            CheckProblem();
            return false;
        }
        return true;
    }

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
            VersionHistoryClicked(null, null);
        settings.LastVersion = AssemblyInfo.AssemblyVersion;
    }

    // Main functions
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
    public void GenerationFinished(Object o, string s)
    {
        if(controller.GenerateBooklet)
            GenerationBookletProblemFinished(s);
        else
            GenerationSingleProblemFinished(s);
    }
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
    // Neue asynchrone Methode
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
            problemInfo += Environment.NewLine + Resources.ComplexityLevel + controller.CurrentProblem.SeverityLevelText + " (" + String.Format(cultureInfo, "{0:0.00}", controller.CurrentProblem.SeverityLevel) + ")";
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

    private void DisplayCellInfo(int row, int col)
    {
        // TODO:
        // Die Gründe für die indirekten Blocks ausgeben (pair, ...)
        String cellInfo = controller.GetCellInfoText(row, col);
        ShowInfo(cellInfo);
        return;
    }

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

    private void ResumeGame()
    {
        if(pauseOverlay != null) pauseOverlay.Visible = false;

        sudokuStatusBarText.Text = sudokuStatusBarText.Text.Replace(Resources.Paused, "").Trim();
        SudokuGrid.ShowValues();
        controller.StartTimer();
    }
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

    private void CheckProblem()
    {
        if(SudokuGrid.SyncProblemWithGUI(false, autoCheck.Checked))
            ShowInfo(controller.IsProblemResolvable() ? String.Format(cultureInfo, Resources.ValidationStatus, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic) : Resources.NotResolvable);
        else
            ShowInfo(Resources.InvalidProblem + Environment.NewLine + Resources.NotResolvable);
    }

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
    private void ResetProblem()
    {
        controller.RestoreProblem();
        controller.ResetSolutions();
        ResetUndo();
        ResetTexts();
        SudokuGrid.ResetMatrix();
        SudokuGrid.DisplayValues();
    }
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
    private void CreateProblemFromFile(String filename)
    {
        controller.CreateProblemFromFile(filename, true, true, true);
    }
    private void NextSolution()
    {
        SudokuGrid.DisplayValues(controller.CurrentProblem.Solutions[++currentSolution]);
        next.Enabled = (currentSolution < controller.CurrentProblem.Solutions.Count - 1);
        prior.Enabled = (currentSolution > 0);
        Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
    }

    private void PriorSolution()
    {
        SudokuGrid.DisplayValues(controller.CurrentProblem.Solutions[--currentSolution]);
        prior.Enabled = (currentSolution > 0);
        next.Enabled = (controller.CurrentProblem.Solutions.Count > 1);
        Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
    }

    private void ResetDetachedProcess()
    {
        sudokuStatusBarText.Text = Resources.Ready;
        EnableGUI();
    }

    // Dialogs
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
    private void DropProblem(object sender, DragEventArgs e)
    {
        if(UnsavedChanges())
        {
            try
            {
                String[] droppedData = (String[])e.Data.GetData(DataFormats.FileDrop.ToString());
                LoadProblem(droppedData[0]);
            }
            catch
            {
                // do nothing if the droped object was not a file
            }
        }
    }

    private void DragOverForm(object sender, DragEventArgs e)
    {
        if((e.AllowedEffect & DragDropEffects.Move) == DragDropEffects.Move)
            e.Effect = DragDropEffects.Move;
    }

    private void ToggleHighlightSameValuesClicked(object sender, EventArgs e)
    {
        highlightSameValues.Checked = !highlightSameValues.Checked;
        settings.HighlightSameValues = highlightSameValues.Checked;
        if(settings.HighlightSameValues)
            SudokuGrid.UpdateHighligts();
        else
            SudokuGrid.ClearHighlights();
    }
    public void TogglePencilModeClick(object sender, EventArgs e)
    {
        pencilMode.Checked = !pencilMode.Checked;
        SudokuGrid.Cursor = pencilMode.Checked ? Cursors.Help : Cursors.Default;
    }

    private void DisplayCellInfo(object sender, EventArgs e)
    {
        DataGridViewSelectedCellCollection cells = SudokuGrid.SelectedCells;
        if(cells.Count == 1)
            DisplayCellInfo(cells[0].RowIndex, cells[0].ColumnIndex);
    }

    private void ActivateGrid(object sender, EventArgs e)
    {
        SudokuGrid.Focus();
    }

    private void ResizeForm(object sender, EventArgs e)
    {
        Opacity = (WindowState == FormWindowState.Minimized) ? 0 : 100;
    }

    // Buttons
    private void NextClick(object sender, EventArgs e)
    {
        NextSolution();
    }

    private void PriorClick(object sender, EventArgs e)
    {
        PriorSolution();
    }

    // Timer Events
    private void AutoPauseTick(object sender, EventArgs e)
    {
        if(WindowState != FormWindowState.Minimized) PauseClick(sender, e);
    }

    private void StatusUpdateTick(object sender, EventArgs e)
    {
        TimeSpan elapsed = controller.ElapsedTime + controller.CurrentProblem.SolvingTime;
        sudokuStatusBarText.Text = Resources.SolutionTime + String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours * 24 + elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
    }

    private void GenerationSingleProblemFinished(String s)
    {
        TimeSpan elapsed = generationTimer.Elapsed;

        status.Text = usePrecalculatedProblem ? Resources.ProblemRetrieved : s + Environment.NewLine + Resources.TimeNeeded + String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours * 24 + elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
        SudokuGrid.DisplayValues(controller.CurrentProblem.Matrix);
        PublishTrickyProblems();
        ResetDetachedProcess();
        ShowInfo(status.Text);
    }

    private async void GenerationBookletProblemFinished(String s)
    {
        status.Text = s;
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

    // Menu handling
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
                            menuTag = mi.DropDownItems[j].Tag.ToString();
                            if(!String.IsNullOrEmpty(menuTag) && menuTag.StartsWith(disableTag))
                                mi.DropDownItems[j].Enabled = ((menuTag.Substring(disableTagLength + 1, 1) == "1") == enable);
                        }
                        if(ddm.HasDropDownItems)
                            for(int k = 0; k < ddm.DropDownItems.Count; k++)
                            {
                                if(ddm.DropDownItems[k].Tag != null)
                                {
                                    menuTag = ddm.DropDownItems[k].Tag.ToString();
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

    public void EnableGUI()
    {
        EnableGUI(true);
    }

    public void DisableGUI()
    {
        EnableGUI(false);
        controller.StopTimer();
        statusUpdateTimer.Stop();
    }

    // Menu Entries
    private void AboutSudokuClick(object sender, EventArgs e)
    {
        new AboutSudoku(settings).ShowDialog();
    }

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

    private void ExitClick(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private void ResetClick(object sender, EventArgs e)
    {
        ResetProblem();
    }

    private void ClearCandidatesClick(object sender, EventArgs e)
    {
        controller.CurrentProblem.ResetCandidates();
        clearCandidates.Enabled = false;
        SudokuGrid.Refresh();
    }

    private void NewSudokuClick(object sender, EventArgs e)
    {
        if(UnsavedChanges())
        {
            controller.CreateNewProblem(false);
            SudokuGrid.FormatBoard(true);
        }
    }

    private void NewXSudokuClick(object sender, EventArgs e)
    {
        if(UnsavedChanges())
        {
            controller.CreateNewProblem(true);
            SudokuGrid.FormatBoard(true);
        }
    }

    private async void SolveClick(object sender, EventArgs e)
    {
        await SolveProblem();
    }

    private void GenerateClick(object sender, EventArgs e)
    {
        if(UnsavedChanges()) GenerateProblems(1, false);
    }

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
    private void OpenClick(object sender, EventArgs e)
    {
        OpenProblem();
    }

    private void SaveClick(object sender, EventArgs e)
    {
        SaveProblem();
    }

    private void ExportClick(object sender, EventArgs e)
    {
        ExportProblem();
    }

    private void TwitterProblemClick(object sender, EventArgs e)
    {
        TwitterProblem();
    }

    private void DefiniteClick(object sender, EventArgs e)
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

    private void ValidateClick(object sender, EventArgs e)
    {
        ValidateProblem();
    }

    private void CheckClick(object sender, EventArgs e)
    {
        CheckProblem();
    }

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

    private void EditCommentClicked(object sender, EventArgs e)
    {
        String oldComment = String.Empty;
        Comment commentDialog = new Comment(settings);

        oldComment = commentDialog.SudokuComment = controller.CurrentProblem.Comment;
        if(commentDialog.ShowDialog() == DialogResult.OK)
        {
            controller.CurrentProblem.Comment = commentDialog.SudokuComment;
            controller.CurrentProblem.Dirty = controller.CurrentProblem.Comment != oldComment;
        }
    }

    private async void AbortClick(object sender, EventArgs e)
    {
        await AbortThread();
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
            FormCTS.Cancel();
            SudokuGrid.DisplayValues(controller.CurrentProblem.Matrix);
            if(controller.CurrentProblem.NumberOfSolutions > 0)
            {
                currentSolution = -1;
                NextSolution();
            }
        }
        catch { }
    }

    private async void PrintClick(object sender, EventArgs e)
    {
        await PrintDialog();
    }

    private void PrintBookletClick(object sender, EventArgs e)
    {
        GenerateProblems4Booklet();
    }

    private void LoadBookletClick(object sender, EventArgs e)
    {
        LoadProblems4Booklet();
    }

    private void InfoClick(object sender, EventArgs e)
    {
        DisplayProblemInfo();
    }

    private void ShowHintsClick(object sender, EventArgs e)
    {
        Hints();
    }

    private void UndoClick(object sender, EventArgs e)
    {
        if(!controller.CanUndo())
            throw (new ApplicationException());

        CoreValue cv = controller.PopUndo();
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

    private void DebugClick(object sender, EventArgs e)
    {
        settings.TraceMode = traceMode.Checked;
        SudokuGrid.SetDebugMode(traceMode.Checked);
    }

    private void FindallSolutionsClick(object sender, EventArgs e)
    {
        settings.FindAllSolutions = findallSolutions.Checked;
    }

    private void ShowPossibleValuesClick(object sender, EventArgs e)
    {
        settings.ShowHints = showPossibleValues.Checked;
        UpdateGUI();
    }

    private void AutoCheckClick(object sender, EventArgs e)
    {
        settings.AutoCheck = autoCheck.Checked;
    }

    private void VisitHomepageClick(object sender, EventArgs e)
    {
        OpenUrl(Resources.Homepage);
    }

    private void ResetTimerClick(object sender, EventArgs e)
    {
        sudokuStatusBarText.Text = Resources.Ready;
        controller.StopTimer();
        controller.CurrentProblem.SolvingTime = TimeSpan.Zero;
        statusUpdateTimer.Stop();
    }

    private void PauseClick(object sender, EventArgs e)
    {
        // Overlay initialisieren, falls noch nicht geschehen
        InitializePauseOverlay();

        if(!sudokuStatusBarText.Text.Contains(Resources.Paused))
            sudokuStatusBarText.Text += Resources.Paused;

        // Statt MessageBox nun das Overlay zeigen
        pauseOverlay.Visible = true;
        pauseOverlay.BringToFront();
    }
    private void MarkNeighborsClicked(object sender, EventArgs e)
    {
        settings.MarkNeighbors = markNeighbors.Checked;
        if(!settings.MarkNeighbors)
            FormatTable();
    }

    private async void MinimizeClick(object sender, EventArgs e)
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
    // In SudokuMainWindow.cs

    public async Task<Boolean> Minimize(int maxSeverity)
    {
        String oldStatusText = sudokuStatusBarText.Text;
        BaseProblem minimizedProblem = null;
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
    private void StartGameClick(object sender, EventArgs e)
    {
        SetReadOnly(true);
        controller.StartTimer();
        statusUpdateTimer.Start();
    }
    private void FixClick(object sender, EventArgs e)
    {
        SetReadOnly(true);
    }

    private void ReleaseClick(object sender, EventArgs e)
    {
        SetReadOnly(false);
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

    private void InitializeController()
    {
        controller = new SudokuController(settings, this);
        controller.Generating += (s, e) => OnGenerating(s, e);
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

    private void OnGenerating(object s, EventArgs e)
    {
        if(InvokeRequired)
        {
            Invoke(new EventHandler(OnGenerating), s, e);
            return;
        }
        GenerationStatus(((SudokuController)s).CurrentProblem.GenerationTime);
    }
    private void VersionHistoryClicked(object sender, EventArgs e)
    {
        OpenUrl(Resources.VersionHistory);
    }

    private void OptionsMenuOpening(object sender, EventArgs e)
    {
        pause.Enabled = resetTimer.Enabled = controller.IsTimerRunning;
    }

    private async void SudokuOfTheDayClicked(object sender, EventArgs e)
    {
        if(await SudokuOfTheDay())
            ShowInfo(String.Format(Resources.SudokuOfTheDayInfo, controller.CurrentProblem.SeverityLevelText));
        else
            ShowError(Resources.SudokuOfTheDayNotLoaded);
    }
    private void HandleMinimizing(object sender, BaseProblem minimalProblem)
    {
        if(InvokeRequired)
        {
            Invoke(new Action<object, BaseProblem>(HandleMinimizing), sender, minimalProblem);
            return;
        }
        status.Text = String.Format(Resources.CurrentMinimalProblem, minimalProblem.SeverityLevelText, minimalProblem.nValues, controller.CurrentProblem.nValues).Replace("\\n", Environment.NewLine);
        status.Update();
        sudokuStatusBarText.Text = Resources.Minimizing;
    }
    // Exit Sudoku
    private void ExitSudoku(object sender, FormClosingEventArgs e)
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
                    e.Cancel = Confirm(Resources.CloseAnyway) == DialogResult.No;
                }
                else
                    e.Cancel = !UnsavedChanges();
            }
        }
        applicationExiting = !e.Cancel;
        if(applicationExiting) { Dispose(); }
    }
}