using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    public enum SudokuPart { Row, Column, Block, UpDiagonal, DownDiagonal };

    public partial class SudokuForm: Form
    {
        public const int RectSize = 3;
        public const int SudokuSize = RectSize * RectSize;
        public const int TotalCellCount = SudokuSize * SudokuSize;

        private System.Windows.Forms.Timer autoPauseTimer = new System.Windows.Forms.Timer();

        private SudokuPrinterService printerService = null;
        private GenerationParameters generationParameters;
        private int currentSolution = 0;
        private Boolean abortRequested = false;
        private Boolean applicationExiting = false;
        private CultureInfo cultureInfo;
        private OptionsDialog optionsDialog = null;
        private Boolean usePrecalculatedProblem = false;
        private int severityLevel = 0;
        private int incorrectTries = 0;

        private SudokuController controller;
        public CancellationTokenSource FormCTS { get; set; }

        // Für das Pause-Overlay
        private Label pauseOverlay;
        private DateTime pauseStartTimestamp;

        /// <summary>
        /// Constructor for the form, mainly used for defaulting some variables and initializing of the gui.
        /// </summary>
        public SudokuForm()
        {
            Thread.CurrentThread.CurrentUICulture = (cultureInfo = new CultureInfo(Settings.Default.DisplayLanguage));

            InitializeComponent();
            SudokuTable.Initialize(RectSize);
            InitializeController();

            sudokuMenu.Renderer = new FlatRenderer();

            debug.Checked = Settings.Default.Debug;
            autoCheck.Checked = Settings.Default.AutoCheck;
            showPossibleValues.Checked = Settings.Default.ShowHints;
            findallSolutions.Checked = Settings.Default.FindAllSolutions;
            ShowInTaskbar = !Settings.Default.HideWhenMinimized;
            markNeighbors.Checked = Settings.Default.MarkNeighbors;
            highlightSameValues.Checked = Settings.Default.HighlightSameValues;

            Deactivate += new EventHandler(FocusLost);
            Activated += new EventHandler(FocusGotten);
            autoPauseTimer.Interval = 1000;
            autoPauseTimer.Tick += new EventHandler(AutoPauseTick);

            generationParameters = new GenerationParameters();

            FormatTable();
            EnableGUI();
            UpdateGUI();
            ResetUndo();
            ResetTexts();

            CheckVersion();
            try
            {
                String fn=AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[0];
                if(fn.Contains("file:///"))
                    fn=fn.Remove(0, 8);

                LoadProblem(fn);
            }
            catch(Exception) { }
        }

        private void FocusLost(object sender, EventArgs e)
        {
            if(SudokuTable.Enabled && Settings.Default.AutoPause)
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
            SudokuTable.FormatBoard();
            ResizeForm();
            // to allow all cell-hints to be redrawn, the table itself must be redrawn
            SudokuTable.Refresh();
        }

        private void ResizeForm()
        {
            int height=SudokuTable.ResizeBoard();
            status.Location = new Point(status.Location.X, SudokuTable.Location.Y + height + 10);
            next.Location = new Point(next.Location.X, SudokuTable.Location.Y + height + 10);
            prior.Location = new Point(prior.Location.X, SudokuTable.Location.Y + height + 10);
        }

        /// <summary>
        /// Displays the current status of the controller.CurrentProblem is the status line; 
        /// if requested (option 'autocheck') a check whether the controller.CurrentProblem is solvable is done.
        /// </summary>
        private void CurrentStatus(Boolean silent)
        {
            int count;
            String problemStatus;

            count = SudokuTable.FilledCells;
            problemStatus = Resources.FilledCells + count;

            Boolean inputOK = SudokuTable.SyncProblemWithGUI(true, autoCheck.Checked);

            ResetTexts();
            if(autoCheck.Checked && (!inputOK || !controller.IsProblemResolvable()))
            {
                problemStatus += (Environment.NewLine + Resources.NotResolvable);
                System.Media.SystemSounds.Hand.Play();
            }

            status.Text = problemStatus;

            if(!silent && count == TotalCellCount)
            {
                status.ForeColor = Color.Green;
                status.Text += " - " + Resources.ProblemSolved;

                System.Media.SystemSounds.Asterisk.Play();

                MessageBox.Show(this, inputOK ? Resources.Congratulations + Environment.NewLine + Resources.ProblemSolved : Resources.ProblemNotSolved, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);

                status.ForeColor = Color.Black;
                sudokuStatusBarText.Text = Resources.Ready;
            }
        }
        /// <summary>
        /// Sets the status text to a string stating that the current generation has been aborted by the user
        /// </summary>
        private void GenerationAborted()
        {
            status.Text =
                String.Format(cultureInfo, Resources.GenerationAborted.Replace("\\n", Environment.NewLine),
                generationParameters.GenerateBooklet ? String.Format(cultureInfo, Resources.GeneratedProblems.Replace("\\n", Environment.NewLine), generationParameters.CurrentProblem, Settings.Default.BookletSizeNew) + Environment.NewLine : String.Empty,
                generationParameters.CheckedProblems, generationParameters.TotalPasses);
            status.Update();
            abortRequested = false;
            ResetDetachedProcess();
            controller.RestoreProblem();
            SudokuTable.DisplayValues();
            generationParameters = new GenerationParameters();
        }

        /// <summary>
        /// Displays the current status of the generation of a controller.CurrentProblem in the status field
        /// </summary>
        private void GenerationStatus(TimeSpan elapsed)
        {
            status.Text =
                (usePrecalculatedProblem ? String.Format(cultureInfo, Resources.RetrieveProblem) :
                    (generationParameters.GenerateBooklet ? String.Format(cultureInfo, Resources.GeneratedProblems, generationParameters.CurrentProblem, Settings.Default.BookletSizeNew) + Environment.NewLine : String.Empty) +
                    String.Format(cultureInfo, Resources.GeneratingStatus, generationParameters.CheckedProblems) + Environment.NewLine + String.Format(cultureInfo, Resources.CheckingStatus, generationParameters.TotalPasses + controller.CurrentProblem.TotalPassCounter) +
                    Environment.NewLine +
                    Resources.PreAllocatedValues + generationParameters.PreAllocatedValues.ToString(cultureInfo)) +
                    Environment.NewLine + Resources.TimeNeeded + String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#},{3:0#}", elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds); /* +", "+
                    Resources.ComplexityLevel+SeverityLevel(controller.CurrentProblem); */
            status.Update();
        }

        /// <summary>
        /// Resets all texts to their default values
        /// </summary>
        private void ResetTexts()
        {
            status.Text = String.Empty;
            prior.Enabled = next.Enabled = false;
            sudokuStatusBarText.Text = Resources.Ready;
            Text = ProductName;
        }

        /// <summary>
        /// Clears the undo-stack
        /// </summary>
        private void ResetUndo()
        {
            SudokuTable.ResetUndo();
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
            if(!SudokuTable.InSync || !controller.IsProblemResolvable())
            {
                CheckProblem();
                return false;
            }
            return true;
        }

        private void SetReadOnly(Boolean readOnly)
        {
            if(readOnly && !SudokuTable.SyncProblemWithGUI(true, autoCheck.Checked))
            {
                MessageBox.Show(this, Resources.NotFixable);
                return;
            }
            SudokuTable.SetReadOnly(readOnly);
        }

        private void CheckVersion()
        {
            if(Settings.Default.LastVersion != AssemblyInfo.AssemblyVersion)
                VersionHistoryClicked(null, null);
            Settings.Default.LastVersion = AssemblyInfo.AssemblyVersion;
        }

        // Main functions
        private async void GenerateProblems(int nProblems, Boolean xSudoku)
        {
            // 1. Initialisierung
            SudokuTable.CreateNewProblem(xSudoku);

            // Severity Logik
            if(!(generationParameters.GenerateBooklet = (nProblems != 1)))
            {
                severityLevel = GetSeverity();
            }
            else
                severityLevel = Settings.Default.SeverityLevel;

            if(severityLevel == 0) return; // Abbrechen

            abortRequested = false;
            DisableGUI();

            var progress = new Progress<GenerationProgressState>(state =>
            {
                SudokuTable.UpdateProblemState(state);
                if(state.StatusText != null)
                {
                    GenerationStatus(controller.CurrentProblem.GenerationTime);
                }
            });

            FormCTS = new CancellationTokenSource();
            try
            {
                sudokuStatusBarText.Text = usePrecalculatedProblem ? Resources.Loading : Resources.Generating;

                // Controller Batch Aufruf
                await controller.GenerateBatch(generationParameters.GenerateBooklet? Settings.Default.BookletSizeNew: 1, generationParameters, severityLevel, usePrecalculatedProblem,
                    async (problem, index) =>
                    {
                        if(generationParameters.GenerateBooklet)
                        {
                            printerService.AddProblem(problem);
                            if(Settings.Default.AutoSaveBooklet)
                            {
                                string filename = generationParameters.BaseDirectory + Path.DirectorySeparatorChar + "Problem-" + (index + 1).ToString(cultureInfo) + "(" + problem.SeverityLevelText + ") (" + problem.SeverityLevel + ")" + Settings.Default.DefaultFileExtension;
                                if(!SaveProblem(filename)) Settings.Default.AutoSaveBooklet = false;
                            }
                        }
                    },
                    progress,
                    FormCTS.Token
                );

                if(generationParameters.GenerateBooklet)
                    GenerationBookletProblemFinished();
                else
                    GenerationSingleProblemFinished();
            }
            catch(OperationCanceledException)
            {
                GenerationAborted();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error generating: " + ex.Message);
                GenerationAborted();
            }
            finally
            {
                EnableGUI();
            }
        }
        private void ShowDefiniteValues()
        {
            if(!PreCheck()) return;

            controller.BackupProblem();
            controller.CurrentProblem.PrepareMatrix();
            SudokuTable.DisplayValues();
            ResetUndo();
            SudokuTable.SyncProblemWithGUI(true, autoCheck.Checked);
            status.Text = String.Format(cultureInfo, Resources.ProblemInfo.Replace("\\n", Environment.NewLine), controller.GetFilledCellCount - controller.GetComputedCellCount, controller.GetComputedCellCount, controller.GetVariableCellCount);
            status.Update();
        }

        private async void Hints()
        {
            if(!PreCheck()) return;

            int count;
            Color hintColor = Color.Red;

            List<BaseCell> hints = controller.GetHints();
            if(hints.Count == 0)
            {
                MessageBox.Show(this, Resources.NoHints);
                return;
            }

            DataGridViewSelectedCellCollection cells = SudokuTable.SelectedCells;
            foreach(DataGridViewCell cell in cells)
                cell.Selected = false;

            for(count = 0; count < hints.Count; count++)
                await ShowHint(hints[count]); // Await statt synchronem Aufruf

            foreach(DataGridViewCell cell in cells)
                cell.Selected = true;
            SudokuTable.Update();

            // controller.UpdateProblem(tmp);
        }

        // Neue asynchrone Methode
        private async Task ShowHint(BaseCell hint)
        {
            await SudokuTable.AnimateHint(hint.Row, hint.Col, hint.nPossibleValues == 1);
        }
        private void DisplayProblemInfo()
        {
            String problemInfo;
            Boolean modified = controller.CurrentProblem.Dirty;
            Boolean problemValid = SudokuTable.SyncProblemWithGUI(true, autoCheck.Checked);

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
            MessageBox.Show(this, problemInfo, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            controller.CurrentProblem.Dirty = modified;
        }

        private void DisplayCellInfo(int row, int col)
        {
            // TODO:
            // Die Gründe für die indirekten Blocks ausgeben (pair, ...)
            String cellInfo=controller.GetCellInfoText(row, col);
            MessageBox.Show(this, cellInfo);
            return;
        }

        private void InitializePauseOverlay()
        {
            if(pauseOverlay != null) return;

            pauseOverlay = new Label();
            pauseOverlay.Text = Resources.PausedMessage.Replace("\\n", Environment.NewLine);
            pauseOverlay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            pauseOverlay.Dock = DockStyle.Fill;
            // Halbtransparentes Weiß (Alpha=200)
            pauseOverlay.BackColor = Color.FromArgb(200, 255, 255, 255);
            pauseOverlay.ForeColor = Color.DarkSlateGray;
            // Dynamische Schriftgröße basierend auf Formular
            pauseOverlay.Font = new Font(this.Font.FontFamily, 24, FontStyle.Bold);
            pauseOverlay.Visible = false;
            pauseOverlay.Cursor = Cursors.Hand;

            // Klick auf das Overlay setzt das Spiel fort
            pauseOverlay.Click += (s, e) => ResumeGame();

            this.Controls.Add(pauseOverlay);
            pauseOverlay.BringToFront();
        }

        private void ResumeGame()
        {
            if(pauseOverlay != null) pauseOverlay.Visible = false;

            sudokuStatusBarText.Text = sudokuStatusBarText.Text.Replace(Resources.Paused, "").Trim();
        }
        private void PublishTrickyProblems()
        {
            if(!controller.HasTrickyProblems()) return;

            if(MessageBox.Show(this, (generationParameters.GenerateBooklet ? Resources.OneOrMoreProblems : Resources.OneProblem) + Resources.Publish, ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if(controller.PublishTrickyProblems())
                    MessageBox.Show(this, String.Format(Resources.PublishOK, controller.NumberOfTrickyProblems));
                else
                    MessageBox.Show(this, String.Format(Resources.PublishFailed, Settings.Default.MailAddress));
            }
        }

        private void CheckProblem()
        {
            if(SudokuTable.SyncProblemWithGUI(false, autoCheck.Checked))
                MessageBox.Show(this, controller.IsProblemResolvable()? String.Format(cultureInfo, Resources.ValidationStatus, controller.CurrentProblem is XSudokuProblem ? "X-": Resources.Classic): Resources.NotResolvable, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show(this, Resources.InvalidProblem + Environment.NewLine + Resources.NotResolvable, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private async void ValidateProblem()
        {
            if(!PreCheck()) return;

            DisableGUI();
            sudokuStatusBarText.Text = Resources.Checking;

            // Setup cancellation
            FormCTS = new CancellationTokenSource();

            var progress = new Progress<GenerationProgressState>(state =>
            {
                status.Text = String.Format(cultureInfo, Resources.CheckingStatus, state.PassCount) + Environment.NewLine + Resources.TimeElapsed + state.Elapsed.ToString(); // Formatierung ggf. anpassen
                status.Update();
                if(debug.Checked) SudokuTable.Update();
            });

            try
            {
                bool solvable = await controller.Validate(progress, FormCTS.Token);

                status.Text = String.Empty;
                sudokuStatusBarText.Text = Resources.Ready;

                MessageBox.Show(this,
                    String.Format(cultureInfo, Resources.CheckResult, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic, solvable ? Resources.AtLeast : Resources.No),
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch(OperationCanceledException)
            {
                sudokuStatusBarText.Text = Resources.Ready;
                status.Text = Resources.GenerationAborted;
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, "Error validating: " + ex.Message);
            }
            finally
            {
                EnableGUI();
            }
        }
        private void ResetProblem()
        {
            controller.RestoreProblem();
            controller.CurrentProblem.ResetSolutions();
            ResetUndo();
            ResetTexts();
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    SudokuTable[col, row].ErrorText = String.Empty;
            SudokuTable.DisplayValues();
        }
        private Boolean SudokuOfTheDay()
        {
            if(controller.SudokuOfTheDay())
            {
                UpdateGUI();
                SudokuTable.SetCellFont();
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
            SudokuTable.DisplayValues(controller.CurrentProblem.Solutions[++currentSolution]);
            next.Enabled = (currentSolution < controller.CurrentProblem.Solutions.Count - 1);
            prior.Enabled = (currentSolution > 0);
            Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
        }

        private void PriorSolution()
        {
            SudokuTable.DisplayValues(controller.CurrentProblem.Solutions[--currentSolution]);
            prior.Enabled = (currentSolution > 0);
            next.Enabled = (controller.CurrentProblem.Solutions.Count > 1);
            Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
        }

        private void ResetDetachedProcess()
        {
            sudokuStatusBarText.Text = Resources.Ready;
            if(debug.Checked) controller.CurrentProblem.Matrix.CellChanged -= HandleCellChanged;
            EnableGUI();
        }

        // Dialogs
        private Boolean UnsavedChanges()
        {
            Boolean rc = true;
            DialogResult dialogResult;

            if(controller.CurrentProblem.Dirty && SudokuTable.FilledCells != TotalCellCount)
            {
                dialogResult = MessageBox.Show(this, Resources.UnsavedChanges, ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
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
                openSudokuDialog.InitialDirectory = Settings.Default.ProblemDirectory;
                openSudokuDialog.DefaultExt = "*" + Settings.Default.DefaultFileExtension;
                openSudokuDialog.Filter = String.Format(cultureInfo, Resources.FilterString, Settings.Default.DefaultFileExtension);
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
                MessageBox.Show(this, String.Format(cultureInfo, Resources.InvalidSudokuFile, filename), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                controller.UpdateProblem(tmp);
            }
            catch(InvalidDataException)
            {
                MessageBox.Show(this, Resources.InvalidSudokuIdentifier, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                controller.UpdateProblem(tmp);
            }
            catch(Exception e)
            {
                MessageBox.Show(this, Resources.OpenFailed + Environment.NewLine + e.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                controller.UpdateProblem(tmp);
            }

            controller.BackupProblem();

            UpdateGUI();
            SudokuTable.SetCellFont();
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
                MessageBox.Show(this, Resources.SaveFailed + Environment.NewLine + e.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(this, Resources.SaveFailed + Environment.NewLine + e.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                returnCode = false;
            }
            return returnCode;
        }

        private Boolean SaveProblem()
        {
            if(!SudokuTable.SyncProblemWithGUI(true, false))
            {
                MessageBox.Show(this, Resources.InvalidProblem + Environment.NewLine + Resources.SaveNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            if(ShowSaveDialog(Settings.Default.DefaultFileExtension) == DialogResult.OK)
                return SaveProblem(saveSudokuDialog.FileName);
            else
                return false;
        }

        private Boolean ExportProblem()
        {
            if(!SudokuTable.SyncProblemWithGUI(true, false))
            {
                MessageBox.Show(this, Resources.InvalidProblem + Environment.NewLine + Resources.ExportNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            if(ShowSaveDialog(Settings.Default.HTMLFileExtension) == DialogResult.OK)
                return ExportProblem(saveSudokuDialog.FileName);
            else
                return false;
        }

        private Boolean TwitterProblem()
        {
            if(!SudokuTable.SyncProblemWithGUI(true, false))
            {
                MessageBox.Show(this, Resources.InvalidProblem + Environment.NewLine + Resources.TwitterNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            System.Diagnostics.Process.Start(controller.TwitterURL);

            return true;
        }

        private DialogResult ShowSaveDialog(String Extension)
        {
            DialogResult result = DialogResult.OK;
            saveSudokuDialog.InitialDirectory = Settings.Default.ProblemDirectory;
            saveSudokuDialog.RestoreDirectory = true;
            saveSudokuDialog.DefaultExt = "*" + Extension;
            saveSudokuDialog.Filter = String.Format(cultureInfo, Resources.FilterString, Extension);
            saveSudokuDialog.FileName = "Problem-" + DateTime.Now.ToString("yyyy.MM.dd-hh-mm", cultureInfo);
            result = saveSudokuDialog.ShowDialog();
            return result;
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
            Settings.Default.HighlightSameValues=highlightSameValues.Checked;
            if(Settings.Default.HighlightSameValues)
                SudokuTable.UpdateHighligts();
            else
                SudokuTable.ClearHighlights();
        }
        public void TogglePencilModeClick(object sender, EventArgs e)
        {
            pencilMode.Checked = !pencilMode.Checked;
            SudokuTable.Cursor = pencilMode.Checked ? Cursors.Help : Cursors.Default;
        }

        private void HandleCellChanged(object sender, BaseCell v)
        {
            if(InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    SudokuTable.DisplayValue(v.Row, v.Col, v.CellValue);
                    SudokuTable.Update();
                }));

                if(Settings.Default.TraceFrequence > 0)
                {
                    try { Thread.Sleep(Settings.Default.TraceFrequence); } catch { }
                }
                return;
            }

            SudokuTable.DisplayValue(v.Row, v.Col, v.CellValue);
            SudokuTable.Update();
        }
        private void DisplayCellInfo(object sender, EventArgs e)
        {
            DataGridViewSelectedCellCollection cells = SudokuTable.SelectedCells;
            if(cells.Count == 1)
                DisplayCellInfo(cells[0].RowIndex, cells[0].ColumnIndex);
        }

        private void ActivateGrid(object sender, EventArgs e)
        {
            SudokuTable.Focus();
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
            if(WindowState != FormWindowState.Minimized)
                PauseClick(sender, e);
        }

        private void GenerationSingleProblemFinished()
        {
            TimeSpan elapsed = controller.CurrentProblem.GenerationTime;

            status.Text = usePrecalculatedProblem ?
                Resources.ProblemRetrieved :
                String.Format(cultureInfo, Resources.NewProblemGenerated.Replace("\\n", Environment.NewLine), controller.CurrentProblem.SeverityLevelText, controller.CurrentProblem.nValues, generationParameters.CheckedProblems, generationParameters.TotalPasses, elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
            SudokuTable.DisplayValues(controller.CurrentProblem.Matrix);
            PublishTrickyProblems();
            ResetDetachedProcess();
            generationParameters = new GenerationParameters();
        }

        private async void GenerationBookletProblemFinished()
        {
            status.Text = String.Format(cultureInfo, Resources.NewProblems, generationParameters.CurrentProblem);
            PrintBooklet();
            PublishTrickyProblems();
            ResetTexts();
            ResetDetachedProcess();
            controller.CurrentProblem.Dirty = false;
            generationParameters = new GenerationParameters();
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
            // resetTimer.Enabled = solvingTimer.Enabled && enable;
            clearCandidates.Enabled = controller.CurrentProblem.HasCandidates() && enable;

            if(SudokuTable.Enabled = enable)
                SudokuTable.Focus();
        }

        public void EnableGUI()
        {
            EnableGUI(true);
        }

        public void DisableGUI()
        {
            EnableGUI(false);
        }

        // Menu Entries
        private void AboutSudokuClick(object sender, EventArgs e)
        {
            new AboutSudoku().ShowDialog();
        }

        static private int GetSeverity()
        {
            if(Settings.Default.SelectSeverity)
            {
                SeverityLevelDialog severityLevelDialog = new SeverityLevelDialog();

                if(severityLevelDialog.ShowDialog() == DialogResult.OK)
                    return severityLevelDialog.SeverityLevel;
                else
                    return 0;
            }
            else
                return Settings.Default.SeverityLevel;
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
            SudokuTable.Refresh();
        }

        private void NewSudokuClick(object sender, EventArgs e)
        {
            if(UnsavedChanges()) controller.CreateNewProblem(false);
        }

        private void NewXSudokuClick(object sender, EventArgs e)
        {
            if(UnsavedChanges()) controller.CreateNewProblem(true);
        }

        private async void SolveClick(object sender, EventArgs e)
        {
            await SolveProblem();
        }

        private void GenerateClick(object sender, EventArgs e)
        {
            if(UnsavedChanges()) GenerateProblems(1, false);
        }

        private async Task SolveProblem(Boolean showResult=true)
        {
            if(!PreCheck()) return;

            controller.BackupProblem();
            DisableGUI();
            if(debug.Checked) controller.CurrentProblem.Matrix.CellChanged += HandleCellChanged;
            DateTime computingStart = DateTime.Now;

            FormCTS = new CancellationTokenSource();

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

                if(controller.CurrentProblem.ProblemSolved || controller.CurrentProblem.NumberOfSolutions > 0)
                {
                    if(controller.CurrentProblem.NumberOfSolutions > 0)
                    {
                        status.Text = Resources.ProblemSolved + Environment.NewLine + Resources.NeededTime + (DateTime.Now - computingStart).ToString() + (findallSolutions.Checked ? Environment.NewLine + Resources.TotalNumberOfSolutions + controller.CurrentProblem.NumberOfSolutions.ToString("n0", cultureInfo) : String.Empty) + Environment.NewLine + Resources.NeededPasses + controller.CurrentProblem.TotalPassCounter.ToString("n0", cultureInfo);
                        currentSolution = -1;
                        NextSolution();
                    }
                    else
                        status.Text = Resources.NotResolvable + Environment.NewLine + Resources.NeededTime + (DateTime.Now - computingStart).ToString() + Environment.NewLine + Resources.NeededPasses + controller.CurrentProblem.TotalPassCounter.ToString("n0", cultureInfo);

                    sudokuStatusBarText.Text = Resources.Ready;

                    string msg = Resources.ProblemSolved;
                    if(findallSolutions.Checked)
                        msg += Environment.NewLine + Resources.TotalNumberOfSolutions + controller.CurrentProblem.NumberOfSolutions.ToString("n0", cultureInfo);

                    if(showResult) MessageBox.Show(this, msg, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if(showResult) MessageBox.Show(this, Resources.NotResolvable, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                ResetDetachedProcess();
            }
            catch(OperationCanceledException)
            {
                sudokuStatusBarText.Text = Resources.GenerationAborted;
                if(controller.CurrentProblem.NumberOfSolutions > 0)
                {
                    status.Text = String.Format(cultureInfo, Resources.SolutionsFound, (controller.CurrentProblem.TotalPassCounter > 0 ? Resources.Plural : String.Empty)) + Environment.NewLine + Resources.NeededTime + (DateTime.Now - computingStart).ToString() + (findallSolutions.Checked ? Environment.NewLine + Resources.TotalNumberOfSolutionsSoFar + controller.CurrentProblem.NumberOfSolutions.ToString("n0", cultureInfo) : String.Empty) + Environment.NewLine + Resources.NeededPasses + controller.CurrentProblem.TotalPassCounter.ToString("n0", cultureInfo);
                    currentSolution = -1;
                    NextSolution();
                }
                else
                    status.Text = String.Format(cultureInfo, Resources.Interrupt.Replace("\\n", Environment.NewLine), DateTime.Now - computingStart, controller.CurrentProblem.TotalPassCounter);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, "Error: " + ex.Message);
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
            optionsDialog = new OptionsDialog();
            optionsDialog.MinBookletSize = (generationParameters.GenerateBooklet ? Math.Max(generationParameters.CurrentProblem + 1, 2) : 2);

            if(optionsDialog.ShowDialog() == DialogResult.OK)
            {
                Thread.CurrentThread.CurrentUICulture = (cultureInfo = new System.Globalization.CultureInfo(Settings.Default.DisplayLanguage));
                ShowInTaskbar = !Settings.Default.HideWhenMinimized;
                usePrecalculatedProblem = Settings.Default.UsePrecalculatedProblems;

                if(generationParameters.GenerateBooklet) severityLevel = Settings.Default.SeverityLevel;

                SudokuTable.UpdateFonts();

                autoPauseTimer.Interval = Convert.ToInt32(Settings.Default.AutoPauseLag) * 1000;

                UpdateGUI();
            }
        }

        private void EditCommentClicked(object sender, EventArgs e)
        {
            String oldComment = String.Empty;
            Comment commentDialog = new Comment();

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

            try { controller.Cancel(); }
            catch { /* ignore */ }

            if(FormCTS != null && !FormCTS.IsCancellationRequested)
            {
                FormCTS.Cancel();
            }

            int waited = 0;
            const int waitStep = 50;
            const int maxWait = 5000;

            // Asynchrones Polling statt DoEvents
            while(controller.CurrentProblem.SolverTask != null && !controller.CurrentProblem.SolverTask.IsCompleted && waited < maxWait)
            {
                await Task.Delay(waitStep);
                waited += waitStep;
            }

            if(controller.CurrentProblem.SolverTask != null && !controller.CurrentProblem.SolverTask.IsCompleted)
            {
                try { await Task.Run(() => controller.CurrentProblem.SolverTask.Wait(500)); } catch { }
            }

            controller.CurrentProblem.Aborted = true;
            abortRequested = true;

            try
            {
                SudokuTable.DisplayValues(controller.CurrentProblem.Matrix);
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
            if(!SudokuTable[cv.Col, cv.Row].ReadOnly)
            {
                SudokuTable[cv.Col, cv.Row].Value = cv.UnformatedValue;
                SudokuTable.Update();
                CurrentStatus(true);
                SudokuTable[cv.Col, cv.Row].Selected = true;

                undo.Enabled = controller.CanUndo();
                controller.CurrentProblem.Dirty = undo.Enabled;
            }
            else
                controller.PushUndo(cv);
        }

        private void DebugClick(object sender, EventArgs e)
        {
            Settings.Default.Debug = debug.Checked;
        }

        private void FindallSolutionsClick(object sender, EventArgs e)
        {
            Settings.Default.FindAllSolutions = findallSolutions.Checked;
        }

        private void ShowPossibleValuesClick(object sender, EventArgs e)
        {
            Settings.Default.ShowHints = showPossibleValues.Checked;
            UpdateGUI();
        }

        private void AutoCheckClick(object sender, EventArgs e)
        {
            Settings.Default.AutoCheck = autoCheck.Checked;
        }

        private void VisitHomepageClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Resources.Homepage);
        }

        private void ResetTimerClick(object sender, EventArgs e)
        {
            controller.CurrentProblem.SolvingTime = TimeSpan.Zero;
            sudokuStatusBarText.Text = Resources.Ready;
        }

        private void PauseClick(object sender, EventArgs e)
        {
            // Overlay initialisieren, falls noch nicht geschehen
            InitializePauseOverlay();

            pauseStartTimestamp = DateTime.Now;

            if(!sudokuStatusBarText.Text.Contains(Resources.Paused))
                sudokuStatusBarText.Text += Resources.Paused;

            autoPauseTimer.Stop();
            SudokuTable.HideCells();

            // Statt MessageBox nun das Overlay zeigen
            pauseOverlay.Visible = true;
            pauseOverlay.BringToFront();
        }
        private void MarkNeighborsClicked(object sender, EventArgs e)
        {
            Settings.Default.MarkNeighbors = markNeighbors.Checked;
            if(!Settings.Default.MarkNeighbors)
                FormatTable();
        }

        private async void MinimizeClick(object sender, EventArgs e)
        {
            int before = controller.CurrentProblem.nValues;
            Boolean dirty = controller.CurrentProblem.Dirty;

            if(!SudokuTable.SyncProblemWithGUI(true, false))
            {
                MessageBox.Show(this, Resources.MinimizationNotPossible);
                return;
            }

            await Minimize(int.MaxValue);

            if(before - controller.CurrentProblem.nValues == 0)
            {
                MessageBox.Show(this, Resources.NoMinimizationPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                controller.CurrentProblem.Dirty = dirty;
            }
            else
            {
                MessageBox.Show(this, String.Format(Resources.Minimized, (before - controller.CurrentProblem.nValues).ToString()), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                controller.CurrentProblem.Dirty = true;
            }
        }
        public async Task<Boolean> Minimize(int maxSeverity)
        {
            String statusbarText = sudokuStatusBarText.Text;
            BaseProblem minimizedProblem = null;
            Boolean rc = false;

            controller.BackupProblem();

            // Berechnung in Hintergrund-Task auslagern
            controller.CurrentProblem.Minimizing += HandleMinimizing;
            controller.CurrentProblem.TestCell += SudokuTable.HandleOnTestCell;
            controller.CurrentProblem.ResetCell += SudokuTable.HandleOnResetCell;

            FormCTS = new CancellationTokenSource();
            try
            {
                minimizedProblem = await controller.Minimize(maxSeverity, FormCTS.Token);

                if(minimizedProblem != null)
                {
                    rc = true;
                    controller.UpdateProblem(minimizedProblem);
                }
            }
            finally
            {
                controller.CurrentProblem.ResetCell -= SudokuTable.HandleOnResetCell;
                controller.CurrentProblem.TestCell -= SudokuTable.HandleOnTestCell;
                controller.CurrentProblem.Minimizing -= HandleMinimizing;
                controller.CurrentProblem.ResetMatrix();

                UpdateGUI();
                SudokuTable.DisplayValues(controller.CurrentProblem.Matrix);
                SudokuTable.SetCellFont();
                ResetUndo();
                EnableGUI();
                Cursor = Cursors.Default;
                sudokuStatusBarText.Text = statusbarText;
            }

            return rc;
        }
        private void FixClick(object sender, EventArgs e)
        {
            SetReadOnly(true);
        }

        private void ReleaseClick(object sender, EventArgs e)
        {
            SetReadOnly(false);
        }

        private void InitializeController()
        {
            controller = new SudokuController();
            controller.Generating += (s, e) => OnGenerating(s, e);
            if(Settings.Default.State.Length > 0)
                controller.RestoreProblemState(false);
            else
                controller.CreateNewProblem(false, false);
            SudokuTable.Controller = controller;
            SudokuTable.UndoAvailableChanged += (s, canUndo) =>  { undo.Enabled = canUndo; };
            SudokuTable.CandidatesAvailableChanged += (s, hasCandidates) => { clearCandidates.Enabled = hasCandidates; };
            SudokuTable.UpdateStatus += (s, silent) => { CurrentStatus(silent); };
            SudokuTable.UpdateHints+= (s, e) => 
            {
                if(Settings.Default.ShowHints && MessageBox.Show(Resources.CandidatesNotShown, ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    showPossibleValues.Checked = Settings.Default.ShowHints = false;
            };
            SudokuTable.StatusTextChanged += (s, text) => 
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
            System.Diagnostics.Process.Start(Resources.VersionHistory);
        }

        private void OptionsMenuOpening(object sender, EventArgs e)
        {
            pause.Enabled = resetTimer.Enabled; // = solvingTimer.Enabled;
        }

        private void SudokuOfTheDayClicked(object sender, EventArgs e)
        {
            if(SudokuOfTheDay())
                MessageBox.Show(this, String.Format(Resources.SudokuOfTheDayInfo, controller.CurrentProblem.SeverityLevelText));
            else
                MessageBox.Show(this, Resources.SudokuOfTheDayNotLoaded);
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
        }
        // Exit Sudoku
        private void ExitSudoku(object sender, FormClosingEventArgs e)
        {
            // Synchroner Fallback für das Schließen der Anwendung
            if(controller.CurrentProblem != null)
            {
                try { controller.Cancel(); } catch { }
                if(controller.CurrentProblem.SolverTask != null && !controller.CurrentProblem.SolverTask.IsCompleted)
                    try { controller.CurrentProblem.SolverTask.Wait(2000); } catch { } // Einfaches Join statt DoEvents-Loop
            }

            if(e.CloseReason != CloseReason.TaskManagerClosing && e.CloseReason != CloseReason.WindowsShutDown)
            {
                if(Settings.Default.AutoSaveState)
                {
                    // if(solvingTimer.Enabled) controller.CurrentProblem.SolvingTime = DateTime.Now - interactiveStart;
                    Settings.Default.State = controller.CurrentProblem.Serialize();
                    Settings.Default.Save();
                }
                else
                {
                    Settings.Default.State = "";
                    Settings.Default.Save();
                    if(!SudokuTable.SyncProblemWithGUI(true, autoCheck.Checked))
                    {
                        e.Cancel = MessageBox.Show(this, Resources.CloseAnyway, ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.No;
                    }
                    else
                        e.Cancel = !UnsavedChanges();
                }
            }
            applicationExiting = !e.Cancel;
        }
    }
}