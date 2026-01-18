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

        private TrickyProblems trickyProblems;
        private PrintParameters printParameters;
        private DateTime computingStart;
        private DateTime interactiveStart;
        private GenerationParameters generationParameters;
        private int currentSolution = 0;
        private Font normalDisplayFont;
        private Font boldDisplayFont;
        private Font strikethroughFont;
        private String[] fontSizes;
        private Boolean abortRequested = false;
        private Boolean applicationExiting = false;
        private Boolean showCandidates = false;
        private CultureInfo cultureInfo;
        private OptionsDialog optionsDialog = null;
        private Boolean mouseWheelEditing = false;
        private Boolean usePrecalculatedProblem = false;
        private int severityLevel = 0;
        private int incorrectTries = 0;
        private Boolean valuesVisible = true;

        // Kontextmenü Variable
        private ContextMenuStrip cellContextMenu;

        // Für das Pause-Overlay
        private Label pauseOverlay;
        private DateTime pauseStartTimestamp;

        private SudokuController controller;
        private CancellationTokenSource cts;

        // Für das Highlighting
        private List<Point> currentlyHighlightedCells = new List<Point>();
        private Color highlightColor = Color.Cyan; // Farbe für gleiche Zahlen
        Color gray;
        Color lightGray;
        Color green;
        Color lightGreen;
        Color textColor;

        private delegate void PerformAction();

        /// <summary>
        /// Constructor for the form, mainly used for defaulting some variables and initializing of the gui.
        /// </summary>
        public SudokuForm()
        {
            Thread.CurrentThread.CurrentUICulture = (cultureInfo = new CultureInfo(Settings.Default.DisplayLanguage));

            InitializeComponent();
            InitializeInputValidation();
            InitializeCellContextMenu();
            InitializeController();

            sudokuMenu.Renderer = new FlatRenderer();

            SudokuTable.BorderStyle = BorderStyle.None;
            SudokuTable.BackgroundColor = Color.White;
            SudokuTable.GridColor = Color.Gainsboro; // Dezenteres Gitter

            SudokuTable.RowHeadersVisible = false;
            SudokuTable.ColumnHeadersVisible = false;

            SudokuTable.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 210, 255);
            SudokuTable.DefaultCellStyle.SelectionForeColor = Color.Black;

            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, SudokuTable, new object[] { true });

            SudokuTable.MouseWheel += new MouseEventHandler(MouseWheelHandler);
            SudokuTable.Rows.Add(SudokuSize);

            solutionTimer.Interval = 1000;
            solvingTimer.Interval = 1000;
            autoPauseTimer.Interval = Convert.ToInt32(Settings.Default.AutoPauseLag) * 1000;

            fontSizes = Settings.Default.FontSizes.Split('|');
            normalDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Regular);
            boldDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Bold);
            strikethroughFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Bold | FontStyle.Strikeout);

            int colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 100f));
            gray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
            green = Color.FromArgb(127, colorIndex, 127);
            colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 220f));
            lightGray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
            colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 1000f));
            lightGreen = Color.FromArgb(200, colorIndex, 200);
            textColor = Settings.Default.Contrast > 50 ? Color.White : Color.Black;

            debug.Checked = Settings.Default.Debug;
            autoCheck.Checked = Settings.Default.AutoCheck;
            showPossibleValues.Checked = Settings.Default.ShowHints;
            findallSolutions.Checked = Settings.Default.FindAllSolutions;
            ShowInTaskbar = !Settings.Default.HideWhenMinimized;
            markNeighbors.Checked = Settings.Default.MarkNeighbors;

            Deactivate += new EventHandler(FocusLost);
            Activated += new EventHandler(FocusGotten);

            generationParameters = new GenerationParameters();
            printParameters = new PrintParameters();
            trickyProblems = new TrickyProblems();

            FormatTable();
            EnableGUI();
            UpdateGUI();
            ResetUndoStack();
            ResetTexts();
            ResetMatrix();
            DisplayValues(controller.CurrentProblem.Matrix);


            CheckVersion();
            try
            {
                /*
                String fn=AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[0];
                if(fn.Contains("file:///"))
                    fn=fn.Remove(0, 8);

                LoadProblem(fn);
                */
            }
            catch(Exception) { }
        }

        private void InitializeController()
        {
            controller = new SudokuController();
            controller.MatrixChanged += (s, e) => OnMatrixChanged(s, e);
            controller.SolutionFound += (s, e) => OnSolutionFound(s, e);
            controller.Generating += (s, e) => OnGenerating(s, e);
            if(Settings.Default.State.Length > 0)
                controller.RestoreProblemState(false);
            else
                controller.CreateNewProblem(false, false);
        }

        private void OnMatrixChanged(object s, EventArgs e)
        {
            if(InvokeRequired)
            {
                Invoke(new EventHandler(OnMatrixChanged), s, e);
                return;
            }

            if(controller.CurrentProblem != null)
            {
                DisplayValues(controller.CurrentProblem.Matrix);
            }

            SudokuTable.Refresh();
            // CurrentStatus(true);
        }

        // Platzhalter für das SolutionFound Event, falls noch nicht implementiert
        private void OnSolutionFound(object s, EventArgs e)
        {
            if(InvokeRequired)
            {
                Invoke(new EventHandler(OnSolutionFound), s, e);
                return;
            }
            // Logic wenn Lösung gefunden wurde (z.B. Timer stoppen, Meldung anzeigen)
            solvingTimer.Stop();
            SudokuTable.Refresh();
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
        /// Returns the copyright string of the application
        /// </summary>
        static public String Copyright
        {
            get { return AssemblyInfo.AssemblyCopyright; }
        }

        /// <summary>
        /// Returns the version of the application (which is the version of the assembly)
        /// </summary>
        static public String Version
        {
            get { return AssemblyInfo.AssemblyVersion; }
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
            int row, col;

            for(row = 0; row < SudokuSize; row++)
                for(col = 0; col < SudokuSize; col++)
                    FormatCell(row, col);
            if(markNeighbors.Checked)
                MarkNeighbors(SudokuTable);

            ResizeTable();
            SetCellFont();
            // to allow all cell-hints to be redrawn, the table itself must be redrawn
            SudokuTable.Refresh();
        }

        private void FormatCell(int row, int col)
        {
            Boolean obfuscated = ((row / 3) % 2 == 1 && (col / 3) % 2 == 0) || ((row / 3) % 2 == 0 && (col / 3) % 2 == 1);
            SudokuTable[row, col].Style.BackColor = (obfuscated ? gray : ((controller.CurrentProblem is XSudokuProblem) && (row == col || row + col == SudokuSize - 1) ? lightGray : Color.White));
            SudokuTable[row, col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
            SudokuTable[row, col].Style.SelectionBackColor = System.Drawing.SystemColors.AppWorkspace;
            if(controller.CurrentProblem.Matrix.Cell(row, col).CellValue == Values.Undefined)
            {
                SudokuTable[col, row].Value = "";
                controller.CurrentProblem.Matrix.Cell(row, col).ReadOnly = false;
            }
        }

        private void MarkNeighbors(DataGridView dgv)
        {
            BaseCell[] neighbors = controller.CurrentProblem.Matrix.Cell(dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y).Neighbors;
            Boolean obfuscated;

            UpdateHighligts(dgv);

            obfuscated = ((dgv.CurrentCellAddress.X / 3) % 2 == 1 && (dgv.CurrentCellAddress.Y / 3) % 2 == 0) || ((dgv.CurrentCellAddress.X / 3) % 2 == 0 && (dgv.CurrentCellAddress.Y / 3) % 2 == 1);
            SudokuTable[dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y].Style.BackColor = (obfuscated ? green : lightGreen);
            SudokuTable[dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y].Style.SelectionBackColor = (obfuscated ? Color.DarkGreen : Color.SeaGreen);
            foreach(BaseCell cell in neighbors)
            {
                obfuscated = ((cell.Row / 3) % 2 == 1 && (cell.Col / 3) % 2 == 0) || ((cell.Row / 3) % 2 == 0 && (cell.Col / 3) % 2 == 1);
                SudokuTable[cell.Row, cell.Col].Style.BackColor = (obfuscated ? green : lightGreen);
                SudokuTable[cell.Row, cell.Col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
            }
        }

        /// <summary>
        /// Resizes the table as defined in the application's options, the actual size of the cells also depents on the application setting 
        /// 'MagnifactionFactor'
        /// </summary>
        private void ResizeTable()
        {
            int width = 0;
            int height = 0;
            int cellSize = (int)((float)Settings.Default.Size * Settings.Default.MagnificationFactor * Settings.Default.CellWidth * .7f);

            for(int i = 0; i < SudokuSize; i++)
            {
                width += (SudokuTable.Columns[i].Width = cellSize);
                height += (SudokuTable.Rows[i].Height = cellSize);
            }

            Width = width + 30;
            Height = height + 140 + (60 * Settings.Default.Size);

            SudokuTable.Width = width + 1;
            SudokuTable.Height = height + 1;
            status.Location = new Point(status.Location.X, SudokuTable.Location.Y + height + 10);
            next.Location = new Point(next.Location.X, SudokuTable.Location.Y + height + 10);
            prior.Location = new Point(prior.Location.X, SudokuTable.Location.Y + height + 10);
        }

        /// <summary>
        /// Sets the cell font for all cells which depends on the cell's size; the actual font name is an application setting
        /// </summary>
        private void SetCellFont()
        {
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    SetCellFont(row, col);
            status.Font = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]) * .8f, FontStyle.Regular);
        }

        /// <summary>
        /// Sets the cell font for a specific cell
        /// </summary>
        /// <param name="row">
        /// Row of the cell which font is to be changed
        /// </param>
        /// <param name="col">
        /// Column of the cell which font is to be changed
        /// </param>
        private void SetCellFont(int row, int col)
        {
            SudokuTable[col, row].Style.Font = controller.CurrentProblem.Matrix.Cell(row, col).ReadOnly ? boldDisplayFont : normalDisplayFont;
            SudokuTable[col, row].ReadOnly = controller.CurrentProblem.Matrix.Cell(row, col).ReadOnly;
        }

        /// <summary>
        /// Reads all values from the gui into the main controller.CurrentProblem
        /// </summary>
        /// <param name="silent">
        /// true: Do not show any error if any; leave cell font unchanged.
        /// false: Display message box, if an error occurs and activates the incorrect cell; set cell font accordingly.
        /// </param>
        /// <returns>
        /// true: Everything is ok.
        /// false: otherwise.
        /// </returns>
        private Boolean SyncProblemWithGUI(Boolean silent)
        {
            Text = ProductName;
            SudokuTable.EndEdit();
            mouseWheelEditing = false;

            // Marshal UI grid to string[,] with minimal processing
            string[,] grid = new string[SudokuForm.SudokuSize, SudokuForm.SudokuSize];
            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                {
                    SudokuTable[col, row].ErrorText = String.Empty;
                    grid[row, col] = SudokuTable[col, row].Value as string;
                }

            bool ok = SyncHelper.TrySyncGrid(controller.CurrentProblem, grid, cultureInfo, autoCheck.Checked, ref incorrectTries, out var syncedProblem);
            if(!ok)
            {
                status.Text = String.Empty;

                // On error, mark cells lazily (only when failure) and optionally show message
                string firstError = null;

                for(int row = 0; row < SudokuForm.SudokuSize; row++)
                {
                    for(int col = 0; col < SudokuForm.SudokuSize; col++)
                    {
                        string raw = grid[row, col];
                        if(raw == null) continue;
                        string value = raw.Trim();
                        if(value.Length == 0) continue;

                        byte parsed;
                        bool parseOk = byte.TryParse(value, NumberStyles.Integer, cultureInfo, out parsed) && parsed >= 1 && parsed <= SudokuForm.SudokuSize;
                        if(!parseOk)
                        {
                            string msg = String.Format(cultureInfo, Resources.InvalidValue, value, row + 1, col + 1);
                            SudokuTable[col, row].ErrorText = msg;
                            if(firstError == null) firstError = msg;
                        }
                    }
                }

                if(!silent && firstError != null)
                {
                    MessageBox.Show(this, firstError, Resources.SudokuError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                return false;
            }

            controller.SyncWithGui(syncedProblem);
            ResetTexts();
            if(Settings.Default.ShowHints) SudokuTable.Refresh();
            return true;
        }

        /// <summary>
        /// Counts the number of filled cells in the current controller.CurrentProblem
        /// </summary>
        /// <returns>
        /// Number of filled cells in the current controller.CurrentProblem
        /// </returns>
        private int FilledCells()
        {
            int count = 0;

            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    if(SudokuTable[col, row].Value != null && (((string)SudokuTable[col, row].Value).Trim()).Length > 0)
                        count++;
            return count;
        }

        /// <summary>
        /// Displays the current status of the controller.CurrentProblem is the status line; 
        /// if requested (option 'autocheck') a check whether the controller.CurrentProblem is solvable is done.
        /// </summary>
        private void CurrentStatus(Boolean silent)
        {
            int count;
            String problemStatus;

            count = FilledCells();
            problemStatus = Resources.FilledCells + count;

            Boolean inputOK = SyncProblemWithGUI(true);
            if(autoCheck.Checked && (!inputOK || !controller.CurrentProblem.Resolvable()))
            {
                problemStatus += (Environment.NewLine + Resources.NotResolvable);
                System.Media.SystemSounds.Hand.Play();
            }

            status.Text = problemStatus;

            if(!silent && count == TotalCellCount)
            {
                solvingTimer.Stop();

                TimeSpan ts = DateTime.Now - interactiveStart;

                status.ForeColor = Color.Green;
                status.Text += " - " + Resources.ProblemSolved;

                System.Media.SystemSounds.Asterisk.Play();

                MessageBox.Show(this, inputOK ? Resources.Congratulations + Environment.NewLine + Resources.ProblemSolved : Resources.ProblemNotSolved, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);

                status.ForeColor = Color.Black;
                sudokuStatusBarText.Text = Resources.Ready;
            }
        }

        /// <summary>
        /// Set the given value into the given cell within the given controller.CurrentProblem
        /// </summary>
        /// <param name="currentProblem">
        /// Problem where the value should be changed
        /// </param>
        /// <param name="row">
        /// Row of the cell to be changed
        /// </param>
        /// <param name="col">
        /// Column of the cell to be changed
        /// </param>
        /// <param name="value">
        /// New value of the cell
        /// </param>
        static private void SetValue(BaseProblem currentProblem, int row, int col, byte value)
        {
            currentProblem.SetValue(row, col, value);
        }

        /// <summary>
        /// Resets the gui, i.e. resets all cell values and the fonts
        /// </summary>
        private void ResetMatrix()
        {
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                {
                    SudokuTable[col, row].Style.Font = normalDisplayFont;
                    SudokuTable[col, row].Value = String.Empty;
                    SudokuTable[col, row].ErrorText = String.Empty;
                }
        }

        /// <summary>
        /// Displays a matrix in the gui
        /// </summary>
        /// <param name="values">
        /// Matrix to be displayed
        /// </param>
        public void DisplayValues(Values values)
        {
            for(int i = 0; i < SudokuForm.SudokuSize; i++)
                for(int j = 0; j < SudokuForm.SudokuSize; j++)
                    DisplayValue(i, j, values.GetValue(i, j));
        }

        /// <summary>
        /// Displays the value in the specified cell
        /// </summary>
        /// <param name="row">
        /// Row of the cell to be displayed
        /// </param>
        /// <param name="col">
        /// Column of the cell to be displayed
        /// </param>
        /// <param name="value">
        /// Value to displayed in the cell at position row, col
        /// </param>
        public void DisplayValue(int row, int col, byte value)
        {
            SudokuTable[col, row].Value = (value == Values.Undefined ? " " : value.ToString(cultureInfo));
            SetCellFont(row, col);
        }

        /// <summary>
        /// Calculates a string representation of the severity level of the given controller.CurrentProblem
        /// </summary>
        /// <param name="currentProblem">
        /// Problem of interest
        /// </param>
        /// <returns>
        /// Severity level of the controller.CurrentProblem in the current language
        /// </returns>
        static private String SeverityLevel(BaseProblem currentProblem)
        {
            currentProblem.SeverityLevel = float.NaN;
            return currentProblem.SeverityLevelText;
        }

        /// <summary>
        /// Calculates a numeric representation of the severity level of the given controller.CurrentProblem, not to muddle up with the internal severity level
        /// </summary>
        /// <param name="currentProblem">
        /// Problem of interest
        /// </param>
        /// <returns>
        /// Severity level of the controller.CurrentProblem (0: not defined, 1: trivial, 2: easy, 4: intermediate, 8: hard)
        /// </returns>
        static private int SeverityLevelInt(BaseProblem currentProblem)
        {
            currentProblem.SeverityLevel = float.NaN;
            return currentProblem.SeverityLevelInt;
        }

        static private String InternalSeverityLevel(BaseProblem currentProblem)
        {
            currentProblem.SeverityLevel = float.NaN;
            return currentProblem.SeverityLevel.ToString("f");
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
            DisplayValues(controller.CurrentProblem.Matrix);
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
            DisplayValues(controller.CurrentProblem.Matrix);
            status.Text = String.Empty;
            prior.Enabled = next.Enabled = false;
            if(!solvingTimer.Enabled) sudokuStatusBarText.Text = Resources.Ready;
            Text = ProductName;
        }

        /// <summary>
        /// Clears the undo-stack
        /// </summary>
        private void ResetUndoStack()
        {
            controller.ClearUndo();
            undo.Enabled = false;
            solvingTimer.Stop();
            interactiveStart = DateTime.MinValue;
            controller.CurrentProblem.Dirty = false;
        }

        // Misc functions
        /// <summary>
        /// Checks whether or not the current controller.CurrentProblem is valid an solvable
        /// </summary>
        /// <param name="silent">
        /// true: Displays error message, if any
        /// </param>
        /// <returns>
        /// true: Problem is valid and resolvable
        /// false: otherwise
        /// </returns>
        private Boolean PreCheck(Boolean silent)
        {
            if(!SyncProblemWithGUI(silent)) return false;
            if(!controller.CurrentProblem.Resolvable())
            {
                CheckProblem();
                return false;
            }
            return true;
        }

        private Boolean PreCheck()
        {
            return PreCheck(false);
        }

        private void SetReadOnly(Boolean readOnly)
        {
            if(readOnly && !SyncProblemWithGUI(true))
            {
                MessageBox.Show(this, Resources.NotFixable);
                return;
            }
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    controller.CurrentProblem.Matrix.Cell(row, col).ReadOnly = (readOnly && SudokuTable[col, row].Value.ToString().Trim() != String.Empty);
            DisplayValues(controller.CurrentProblem.Matrix);
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
            controller.CreateNewProblem(xSudoku);
            controller.BackupProblem();

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
            trickyProblems.Clear();

            var progress = new Progress<GenerationProgressState>(state =>
            {
                if(state.Value != Values.Undefined)
                    DisplayValue(state.Row, state.Col, state.Value);
                else
                    SudokuTable[state.Col, state.Row].Value = "";

                if(state.ReadOnly) SetCellFont(state.Row, state.Col);

                if(state.StatusText != null)
                {
                    GenerationStatus(controller.CurrentProblem.GenerationTime);
                }
            });

            cts = new CancellationTokenSource();
            try
            {
                sudokuStatusBarText.Text = usePrecalculatedProblem ? Resources.Loading : Resources.Generating;
                computingStart = DateTime.Now;

                // Controller Batch Aufruf
                await controller.GenerateBatch(generationParameters.GenerateBooklet? Settings.Default.BookletSizeNew: 1, generationParameters, severityLevel, trickyProblems, usePrecalculatedProblem,
                    async (problem, index) =>
                    {
                        if(generationParameters.GenerateBooklet)
                        {
                            printParameters.Problems.Add(problem);
                            if(Settings.Default.AutoSaveBooklet)
                            {
                                string filename = generationParameters.BaseDirectory + Path.DirectorySeparatorChar + "Problem-" + (index + 1).ToString(cultureInfo) + "(" + SeverityLevel(problem) + ") (" + InternalSeverityLevel(problem) + ")" + Settings.Default.DefaultFileExtension;
                                if(!SaveProblem(filename)) Settings.Default.AutoSaveBooklet = false;
                            }
                        }
                    },
                    progress,
                    cts.Token
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
            DisplayValues(controller.CurrentProblem.Matrix);
            ResetUndoStack();
            SyncProblemWithGUI(true);
            status.Text = String.Format(cultureInfo, Resources.ProblemInfo.Replace("\\n", Environment.NewLine), controller.CurrentProblem.nValues - controller.CurrentProblem.nComputedValues, controller.CurrentProblem.nComputedValues, controller.CurrentProblem.nVariableValues);
            status.Update();
        }

        private async void Hints()
        {
            if(!PreCheck(true)) return;

            BaseProblem tmp = controller.CurrentProblem.Clone();
            int count;
            Color hintColor = Color.Red;

            List<BaseCell> values = controller.CurrentProblem.GetObviousCells();
            if(values.Count == 0)
            {
                hintColor = Color.Orange;
                values = controller.CurrentProblem.GetHints();
            }

            if(values.Count == 0)
            {
                MessageBox.Show(this, Resources.NoHints);
                return;
            }

            DataGridViewSelectedCellCollection cells = SudokuTable.SelectedCells;
            foreach(DataGridViewCell cell in cells)
                cell.Selected = false;

            if(values.Count <= Settings.Default.MaxHints)
                for(count = 0; count < values.Count; count++)
                    await ShowHint(values[count], hintColor); // Await statt synchronem Aufruf
            else
            {
                List<BaseCell> hints = new List<BaseCell>();
                Random rand = new Random();
                int index;
                do
                    if(!hints.Contains(values[(index = rand.Next(values.Count))]))
                        hints.Add(values[index]);
                while(hints.Count < Settings.Default.MaxHints);

                for(count = 0; count < Settings.Default.MaxHints; count++)
                    await ShowHint(hints[count], hintColor); // Await statt synchronem Aufruf
            }

            foreach(DataGridViewCell cell in cells)
                cell.Selected = true;
            SudokuTable.Update();

            controller.SyncWithGui(tmp);
        }

        // Neue asynchrone Methode
        private async Task ShowHint(BaseCell hint, Color hintColor)
        {
            Color currentColor = SudokuTable[hint.Col, hint.Row].Style.BackColor;
            SudokuTable[hint.Col, hint.Row].Style.BackColor = hintColor;
            SudokuTable.Update();

            // Ersetzt Thread.Sleep(500) durch nicht-blockierendes Warten
            await Task.Delay(500);

            SudokuTable[hint.Col, hint.Row].Style.BackColor = currentColor;
        }


        private void DisplayProblemInfo()
        {
            String problemInfo;
            Boolean modified = controller.CurrentProblem.Dirty;
            Boolean problemValid = SyncProblemWithGUI(true);

            problemInfo = Resources.PreAllocatedValues + controller.CurrentProblem.nValues.ToString(cultureInfo);
            controller.CurrentProblem.PrepareMatrix();
            problemInfo += Environment.NewLine + Resources.DefiniteCells + controller.CurrentProblem.nComputedValues.ToString(cultureInfo);
            controller.CurrentProblem.ResetMatrix();
            if(problemValid)
                problemInfo += Environment.NewLine + Resources.ComplexityLevel + SeverityLevel(controller.CurrentProblem) + " (" + String.Format(cultureInfo, "{0:0.00}", controller.CurrentProblem.SeverityLevel) + ")";
            problemInfo += Environment.NewLine +
                (controller.CurrentProblem.ProblemSolved ? String.Format(cultureInfo, Resources.CheckResult, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic, Resources.AtLeast) :
                 controller.CurrentProblem.Resolvable() && problemValid ? String.Format(cultureInfo, Resources.ValidationStatus, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic) : Resources.NotResolvable);
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
            String cellInfo;
            BaseCell cell = controller.CurrentProblem.Matrix.Cell(row, col);

            cellInfo = String.Format(cultureInfo, Resources.Cellinfo, row + 1, col + 1, (cell.ReadOnly ? " (" + Resources.ReadOnly + ") " : "")) + Environment.NewLine;
            if(cell.DefinitiveValue != Values.Undefined)
                cellInfo += Environment.NewLine + String.Format(cultureInfo, Resources.DefiniteValue) + cell.DefinitiveValue.ToString();
            else
                if(cell.FixedValue)
                cellInfo += Environment.NewLine + String.Format(cultureInfo, Resources.CellValue) + cell.CellValue.ToString();

            String directBlockedCells = "";
            String indirectBlockedCells = "";

            for(int i = 1; i <= SudokuSize; i++)
            {
                if(i != cell.DefinitiveValue && i != cell.CellValue)
                {
                    if(cell.Blocked(i))
                        directBlockedCells += (directBlockedCells.Length == 0 ? i.ToString() : ", " + i.ToString());
                    else
                        if(cell.IndirectlyBlocked(i)) indirectBlockedCells += (indirectBlockedCells.Length == 0 ? i.ToString() : ", " + i.ToString());
                }
            }

            cellInfo +=
                Environment.NewLine + String.Format(cultureInfo, Resources.DirectBlocks) +
                (directBlockedCells.Length == 0 ? Resources.None : directBlockedCells) +
                Environment.NewLine + String.Format(cultureInfo, Resources.IndirectBlocks) +
                (indirectBlockedCells.Length == 0 ? Resources.None : indirectBlockedCells);

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

            solvingTimer.Start();
            ShowValues();

            TimeSpan pauseDuration = DateTime.Now - pauseStartTimestamp;
            interactiveStart += pauseDuration;

            sudokuStatusBarText.Text = sudokuStatusBarText.Text.Replace(Resources.Paused, "").Trim();
        }
        private void PublishTrickyProblems()
        {
            if(trickyProblems.Empty) return;

            if(MessageBox.Show(this, (generationParameters.GenerateBooklet ? Resources.OneOrMoreProblems : Resources.OneProblem) + Resources.Publish, ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if(trickyProblems.Publish())
                    MessageBox.Show(this, String.Format(Resources.PublishOK, trickyProblems.Count));
                else
                    MessageBox.Show(this, String.Format(Resources.PublishFailed, Settings.Default.MailAddress));
            }
        }

        private void CheckProblem()
        {
            if(!SyncProblemWithGUI(false)) return;
            MessageBox.Show(this, controller.CurrentProblem.Resolvable() ? String.Format(cultureInfo, Resources.ValidationStatus, controller.CurrentProblem is XSudokuProblem ? "X-" : Resources.Classic) : Resources.NotResolvable, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void ValidateProblem()
        {
            if(!PreCheck(true)) return;

            DisableGUI();
            sudokuStatusBarText.Text = Resources.Checking;
            computingStart = DateTime.Now;

            // Setup cancellation
            cts = new CancellationTokenSource();

            var progress = new Progress<GenerationProgressState>(state =>
            {
                status.Text = String.Format(cultureInfo, Resources.CheckingStatus, state.PassCount) + Environment.NewLine +
                              Resources.TimeElapsed + state.Elapsed.ToString(); // Formatierung ggf. anpassen
                status.Update();
                if(debug.Checked) SudokuTable.Update();
            });

            try
            {
                // Aufruf der neuen Controller-Methode
                bool solvable = await controller.Validate(progress, cts.Token);

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
            ResetUndoStack();
            ResetTexts();
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    SudokuTable[col, row].ErrorText = String.Empty;
        }

        private Boolean SudokuOfTheDay()
        {
            controller.CreateNewProblem(Settings.Default.SudokuOfTheDay);
            if(controller.CurrentProblem.SudokuOfTheDay())
            {
                UpdateGUI();
                DisplayValues(controller.CurrentProblem.Matrix);
                SetCellFont();

                controller.BackupProblem();
                ResetUndoStack();
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
            DisplayValues(controller.CurrentProblem.Solutions[++currentSolution]);
            next.Enabled = (currentSolution < controller.CurrentProblem.Solutions.Count - 1);
            prior.Enabled = (currentSolution > 0);
            Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
        }

        private void PriorSolution()
        {
            DisplayValues(controller.CurrentProblem.Solutions[--currentSolution]);
            prior.Enabled = (currentSolution > 0);
            next.Enabled = (controller.CurrentProblem.Solutions.Count > 1);
            Text = String.Format(cultureInfo, Resources.DisplaySolution, currentSolution + 1, controller.CurrentProblem.Solutions[currentSolution].Counter);
        }

        private void ResetDetachedProcess()
        {
            sudokuStatusBarText.Text = Resources.Ready;
            solutionTimer.Stop();
            solvingTimer.Stop();
            if(debug.Checked) controller.CurrentProblem.Matrix.CellChanged -= HandleCellChanged;
            EnableGUI();
        }

        // Dialogs
        private Boolean UnsavedChanges()
        {
            Boolean rc = true;
            DialogResult dialogResult;

            if(controller.CurrentProblem.Dirty && FilledCells() != TotalCellCount)
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
                controller.SyncWithGui(tmp);
            }
            catch(InvalidDataException)
            {
                MessageBox.Show(this, Resources.InvalidSudokuIdentifier, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                controller.SyncWithGui(tmp);
            }
            catch(Exception e)
            {
                MessageBox.Show(this, Resources.OpenFailed + Environment.NewLine + e.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                controller.SyncWithGui(tmp);
            }

            controller.BackupProblem();

            UpdateGUI();
            SetCellFont();
            ResetUndoStack();
        }

        private Boolean SaveProblem(String filename)
        {
            Boolean returnCode = true;
            try
            {
                if(solvingTimer.Enabled)
                    controller.CurrentProblem.SolvingTime = DateTime.Now - interactiveStart;
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
            if(!SyncProblemWithGUI(true))
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
            if(!SyncProblemWithGUI(true))
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
            if(!SyncProblemWithGUI(true))
            {
                MessageBox.Show(this, Resources.InvalidProblem + Environment.NewLine + Resources.TwitterNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            System.Diagnostics.Process.Start(Resources.TwitterURL + String.Format(cultureInfo, Resources.TwitterText, (controller.CurrentProblem is XSudokuProblem ? "X" : ""), controller.CurrentProblem.Serialize(false).Substring(1, SudokuForm.TotalCellCount)));

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

        private void PushOnUndoStack(DataGridView dgv)
        {
            CoreValue cv = new CoreValue();
            cv.Row = dgv.CurrentCell.RowIndex;
            cv.Col = dgv.CurrentCell.ColumnIndex;
            if(dgv.CurrentCell.Value != null)
                cv.UnformatedValue = (String)dgv.CurrentCell.Value;
            controller.PushUndo(cv);
            undo.Enabled = true;
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

        private void BeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if(sender is DataGridView)
                if(!pencilMode.Checked && !((DataGridView)sender).CurrentCell.ReadOnly)
                    PushOnUndoStack((DataGridView)sender);
        }

        private void EndEdit(object sender, DataGridViewCellEventArgs e)
        {
            CellEndEdit(sender);
        }

        private void CellEndEdit(object sender)
        {
            if(sender is DataGridView)
            {
                if(!solvingTimer.Enabled)
                {
                    controller.CurrentProblem.SolvingTime = TimeSpan.Zero;
                    solvingTimer.Start();
                    interactiveStart = DateTime.Now - controller.CurrentProblem.SolvingTime;
                }

                DataGridView dgv = (DataGridView)sender;
                controller.CurrentProblem.SetValue(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex, Values.Undefined);
                SetCellFont(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex);
            }
            mouseWheelEditing = false;

            CurrentStatus(false);
        }
        public void TogglePencilModeClick(object sender, EventArgs e)
        {
            pencilMode.Checked = !pencilMode.Checked;
            SudokuTable.Cursor = pencilMode.Checked ? Cursors.Help : Cursors.Default;
        }
        private new void KeyUp(object sender, KeyEventArgs e)
        {
            int candidate;
            candidate = e.KeyValue - 96; // Numpad
            if(candidate < 0) candidate = e.KeyValue - 48;

            if(sender is DataGridView && (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == (Keys.Control | Keys.Shift)) && candidate > 0 && candidate <= SudokuSize)
            {
                if(Settings.Default.ShowHints && MessageBox.Show(this, Resources.CandidatesNotShown, ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    showPossibleValues.Checked = Settings.Default.ShowHints = false;

                DataGridView dgv = (DataGridView)sender;
                if(!controller.CurrentProblem.Cell(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex).ReadOnly)
                {
                    controller.CurrentProblem.SetCandidate(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex, candidate, Control.ModifierKeys == (Keys.Control | Keys.Shift));
                    clearCandidates.Enabled = controller.CurrentProblem.HasCandidates();
                }

                dgv.Refresh();
            }
        }

        private void HandleSpecialChar(object sender, KeyEventArgs e)
        {
            if(sender is DataGridView)
            {
                if(e.KeyCode == Keys.Delete || e.KeyCode == Keys.Apps || e.KeyCode == Keys.Back)
                {
                    DataGridView dgv = (DataGridView)sender;
                    if(e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                    {
                        if(!SudokuTable[dgv.CurrentCell.ColumnIndex, dgv.CurrentCell.RowIndex].ReadOnly)
                        {
                            PushOnUndoStack(dgv);
                            SudokuTable[dgv.CurrentCell.ColumnIndex, dgv.CurrentCell.RowIndex].Value = "";
                            CellEndEdit(sender);
                        }
                    }
                    else
                    {
                        HandleRightMouseButton(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex);
                    }
                }
                else
                {
                    if(!pencilMode.Checked || e.Control || e.Alt) return;

                    int value = -1;
                    if(e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) value = e.KeyCode - Keys.D0;
                    else if(e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) value = e.KeyCode - Keys.NumPad0;

                    if(value > 0)
                    {
                        DataGridViewCell current = SudokuTable.CurrentCell;
                        if(current != null && !current.ReadOnly)
                        {
                            controller.CurrentProblem.SetCandidate(current.RowIndex, current.ColumnIndex, value, false);
                            clearCandidates.Enabled = controller.CurrentProblem.HasCandidates();

                            e.Handled = true;
                            e.SuppressKeyPress = true;

                            SudokuTable.InvalidateCell(current.ColumnIndex, current.RowIndex);
                            SudokuTable.Refresh();
                        }
                    }
                }
            }
        }

        private void MouseWheelHandler(object sender, MouseEventArgs e)
        {
            if(sender is DataGridView)
            {
                DataGridView dgv = (DataGridView)sender;

                if(dgv.EditingControl == null && !dgv.CurrentCell.ReadOnly)
                {
                    if(!mouseWheelEditing) PushOnUndoStack(dgv);

                    try
                    {
                        int currentValue = (dgv.CurrentCell.Value == null || ((String)dgv.CurrentCell.Value).Trim().Length == 0 ? 0 : Convert.ToInt32(dgv.CurrentCell.Value, cultureInfo));
                        currentValue += Math.Sign(e.Delta);
                        if(currentValue > 0 && currentValue <= SudokuSize)
                            dgv.CurrentCell.Value = currentValue.ToString();
                        else if(currentValue == Values.Undefined)
                            dgv.CurrentCell.Value = "";
                        else
                            System.Media.SystemSounds.Hand.Play();
                        mouseWheelEditing = true;
                    }
                    catch(FormatException)
                    { /* do nothing; this happens if the current cell does not contain a number but anything else, e.g., a character */ }
                }
            }
        }

        private void CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            if(mouseWheelEditing)
                CellEndEdit(sender);

            if(Settings.Default.MarkNeighbors)
            {
                DataGridView dgv = (DataGridView)sender;
                BaseCell[] neighbors = controller.CurrentProblem.Matrix.Cell(dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y).Neighbors;

                FormatCell(dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y);
                foreach(BaseCell cell in neighbors)
                    FormatCell(cell.Row, cell.Col);
            }
        }

        private void ShowValues()
        {
            if(!valuesVisible)
            {
                DisplayValues(controller.CurrentProblem.Matrix);
                valuesVisible = true;
                if(!solvingTimer.Enabled)
                {
                    controller.CurrentProblem.SolvingTime = TimeSpan.Zero;
                    solvingTimer.Start();
                    interactiveStart = DateTime.Now;
                }
            }
        }
        private void CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if(sender is DataGridView && controller.CurrentProblem != null && Settings.Default.MarkNeighbors)
            {
                DataGridView dgv = (DataGridView)sender;

                if(controller.CurrentProblem != null && Settings.Default.MarkNeighbors)
                    MarkNeighbors(dgv);
            }
            ShowValues();
        }

        private void HandleCellChanged(object sender, BaseCell v)
        {
            if(InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    DisplayValue(v.Row, v.Col, v.CellValue);
                    SudokuTable.Update();
                }));

                if(Settings.Default.TraceFrequence > 0)
                {
                    try { Thread.Sleep(Settings.Default.TraceFrequence); } catch { }
                }
                return;
            }

            DisplayValue(v.Row, v.Col, v.CellValue);
            SudokuTable.Update();
        }
        private void HandleOnTestCell(object sender, BaseCell cell)
        {
            if(InvokeRequired)
            {
                Invoke(new Action<object, BaseCell>(HandleOnTestCell), sender, cell);
                return;
            }
            SudokuTable[cell.Col, cell.Row].Style.Font = strikethroughFont;
            SudokuTable[cell.Col, cell.Row].Style.BackColor = Color.Coral;
        }

        private void HandleOnResetCell(object sender, BaseCell cell)
        {
            if(InvokeRequired)
            {
                Invoke(new Action<object, BaseCell>(HandleOnResetCell), sender, cell);
                return;
            }
            SudokuTable[cell.Col, cell.Row].Style.Font = boldDisplayFont;
            FormatCell(cell.Col, cell.Row);
        }

        private void HandleMinimizing(object sender, BaseProblem minimalProblem)
        {
            if(InvokeRequired)
            {
                Invoke(new Action<object, BaseProblem>(HandleMinimizing), sender, minimalProblem);
                return;
            }
            status.Text = String.Format(Resources.CurrentMinimalProblem, SeverityLevel(minimalProblem), minimalProblem.nValues, controller.CurrentProblem.nValues).Replace("\\n", Environment.NewLine);
            status.Update();
        }

        private void ShowCellHints(object sender, PaintEventArgs e)
        {
            if(sender is DataGridView)
            {
                Font printFont = (Settings.Default.Size == 1 ? PrintParameters.SmallFont : PrintParameters.NormalFont);

                showCandidates = !Settings.Default.ShowHints;
                DataGridView dgv = (DataGridView)sender;
                float cellSize = dgv.Columns[0].Width;
                for(int row = 0; row < SudokuSize; row++)
                    for(int col = 0; col < SudokuSize; col++)
                        if(controller.CurrentProblem.GetValue(row, col) == Values.Undefined)
                        {
                            RectangleF rf = new RectangleF(col * cellSize, row * cellSize, cellSize, cellSize);
                            if(rf.IntersectsWith(e.ClipRectangle))
                                if(Settings.Default.UseWatchHandHints)
                                    PrintWatchHands(controller.CurrentProblem.Cell(row, col), rf, e.Graphics);
                                else
                                    PrintHints(controller.CurrentProblem.Cell(row, col), rf, e.Graphics, printFont, SudokuTable[row, col].Style.ForeColor);
                        }
            }
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
        private void SolvingTimerTick(object sender, EventArgs e)
        {
            TimeSpan ts = DateTime.Now - interactiveStart;
            sudokuStatusBarText.Text = Resources.SolutionTime + String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#}", ts.Days * 24 + ts.Hours, ts.Minutes, ts.Seconds);
        }

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
                String.Format(cultureInfo, Resources.NewProblemGenerated.Replace("\\n", Environment.NewLine), SeverityLevel(controller.CurrentProblem), controller.CurrentProblem.nValues, generationParameters.CheckedProblems, generationParameters.TotalPasses, elapsed.Hours, elapsed.Minutes, elapsed.Seconds, elapsed.Milliseconds);
            DisplayValues(controller.CurrentProblem.Matrix);
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
            // special handling for the "Undo", the "Clear Candidates", and the "Reset Timer" menu items
            undo.Enabled = controller.CanUndo() && enable;
            resetTimer.Enabled = solvingTimer.Enabled && enable;
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
            solvingTimer.Stop();
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
            SolveProblem();
        }

        private void GenerateClick(object sender, EventArgs e)
        {
            if(UnsavedChanges()) GenerateProblems(1, false);
        }

        private async void SolveProblem()
        {
            if(!PreCheck()) return;

            controller.UpdateProblemState(controller.CurrentProblem);
            DisableGUI();
            if(debug.Checked) controller.CurrentProblem.Matrix.CellChanged += HandleCellChanged;

            cts = new CancellationTokenSource();

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
                await controller.Solve(findallSolutions.Checked, progress, cts.Token);

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

                    MessageBox.Show(this, msg, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, Resources.NotResolvable, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                int colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 100f));
                gray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
                green = Color.FromArgb(64, colorIndex, 64);
                colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 220f));
                lightGray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
                colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 1000f));
                lightGreen = Color.FromArgb(191, colorIndex, 191);
                textColor = Settings.Default.Contrast > 50 ? Color.White : Color.Black;
                normalDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Regular);
                boldDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Bold);
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

            if(cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
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
                DisplayValues(controller.CurrentProblem.Matrix);
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
            solvingTimer.Stop();
            sudokuStatusBarText.Text = Resources.Ready;
        }

        private void PauseClick(object sender, EventArgs e)
        {
            // Overlay initialisieren, falls noch nicht geschehen
            InitializePauseOverlay();

            pauseStartTimestamp = DateTime.Now;

            solvingTimer.Stop();
            controller.CurrentProblem.SolvingTime = DateTime.Now - interactiveStart;

            if(!sudokuStatusBarText.Text.Contains(Resources.Paused))
                sudokuStatusBarText.Text += Resources.Paused;

            autoPauseTimer.Stop();
            HideCells();

            // Statt MessageBox nun das Overlay zeigen
            pauseOverlay.Visible = true;
            pauseOverlay.BringToFront();
        }
        private void HideCells()
        {
            int row, col;

            for(row = 0; row < SudokuSize; row++)
                for(col = 0; col < SudokuSize; col++)
                    SudokuTable[row, col].Value = "";
            valuesVisible = false;
        }

        private void UpdateHighligts(DataGridView dgv)
        {
            ClearHighlights();

            if(dgv.CurrentCell == null || dgv.CurrentCell.Value == null) return;

            string selectedValue = dgv.CurrentCell.Value.ToString();
            if(string.IsNullOrWhiteSpace(selectedValue)) return;

            int currentRow = dgv.CurrentCell.RowIndex;
            int currentCol = dgv.CurrentCell.ColumnIndex;

            for(int row = 0; row < SudokuSize; row++)
            {
                for(int col = 0; col < SudokuSize; col++)
                {
                    if(string.Equals(SudokuTable[col, row].Value as string, selectedValue))
                    {
                        // Hintergrund ändern und Position merken
                        SudokuTable[col, row].Style.BackColor = highlightColor;
                        currentlyHighlightedCells.Add(new Point(col, row)); // X=Col, Y=Row
                    }
                }
            }
        }

        private void ClearHighlights()
        {
            foreach(Point p in currentlyHighlightedCells)
            {
                FormatCell(p.X, p.Y);
            }
            currentlyHighlightedCells.Clear();
        }
        private void MarkNeighborsClicked(object sender, EventArgs e)
        {
            if(!markNeighbors.Checked)
                FormatTable();
            Settings.Default.MarkNeighbors = markNeighbors.Checked;
        }

        private async Task<Boolean> Minimize(int maxSeverity)
        {
            String statusbarText = sudokuStatusBarText.Text;
            BaseProblem minimizedProblem = null;
            Boolean rc = false;

            controller.BackupProblem();
            sudokuStatusBarText.Text = Resources.Minimizing;
            Cursor = Cursors.WaitCursor;
            DisableGUI();

            // Berechnung in Hintergrund-Task auslagern
            controller.CurrentProblem.Minimizing += HandleMinimizing;
            controller.CurrentProblem.TestCell += HandleOnTestCell;
            controller.CurrentProblem.ResetCell += HandleOnResetCell;

            try
            {
                minimizedProblem = await controller.Minimize(maxSeverity);

                if(minimizedProblem != null)
                {
                    rc = true;
                    controller.SyncWithGui(minimizedProblem);
                }
            }
            finally
            {
                controller.CurrentProblem.ResetCell -= HandleOnResetCell;
                controller.CurrentProblem.TestCell -= HandleOnTestCell;
                controller.CurrentProblem.Minimizing -= HandleMinimizing;
                controller.CurrentProblem.ResetMatrix();

                UpdateGUI();
                DisplayValues(controller.CurrentProblem.Matrix);
                SetCellFont();
                ResetUndoStack();
                EnableGUI();
                Cursor = Cursors.Default;
                sudokuStatusBarText.Text = statusbarText;
            }

            return rc;
        }

        private async void MinimizeClick(object sender, EventArgs e)
        {
            int before = controller.CurrentProblem.nValues;
            Boolean dirty = controller.CurrentProblem.Dirty;

            if(!SyncProblemWithGUI(true))
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
        private void FixClick(object sender, EventArgs e)
        {
            SetReadOnly(true);
        }

        private void ReleaseClick(object sender, EventArgs e)
        {
            SetReadOnly(false);
        }

        private void InitializeInputValidation()
        {
            // Event abonnieren, das feuert, wenn eine Zelle in den Editiermodus wechselt
            SudokuTable.EditingControlShowing += EditingControl;
        }

        private void EditingControl(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            // Wir prüfen, ob das Control eine TextBox ist (Standard bei DataGridViewTextBoxColumn)
            if(e.Control is System.Windows.Forms.TextBox textBox)
            {
                textBox.KeyPress -= CellKeyPressValidation;
                textBox.KeyPress += CellKeyPressValidation;
            }
        }

        private void CellKeyPressValidation(object sender, KeyPressEventArgs e)
        {
            bool isValidDigit = char.IsDigit(e.KeyChar) && e.KeyChar != '0';
            bool isControl = char.IsControl(e.KeyChar);

            if(!isValidDigit && !isControl)
            {
                e.Handled = true;
                System.Media.SystemSounds.Beep.Play();
            }
        }
        private void InitializeCellContextMenu()
        {
            cellContextMenu = new ContextMenuStrip();

            // Menüpunkt: Zelle leeren
            var itemClear = cellContextMenu.Items.Add(Resources.ClearContent);
            itemClear.Enabled = true;
            itemClear.Click += (s, e) =>
            {
                if(SudokuTable.CurrentCell != null && !SudokuTable.CurrentCell.ReadOnly)
                {
                    // Nutzen der existierenden Logik via Undo-Stack
                    PushOnUndoStack(SudokuTable);
                    SudokuTable.CurrentCell.Value = "";
                    CellEndEdit(SudokuTable);
                }
            };

            // Menüpunkt: Als Kandidaten speichern (Beispiel für erweiterte Interaktion)
            var itemCandidate = cellContextMenu.Items.Add(Resources.ClearCandidates);
            itemCandidate.Enabled = true;
            itemCandidate.Click += (s, e) =>
            {
                if(controller.CurrentProblem != null)
                {
                    controller.CurrentProblem.ResetCandidates(SudokuTable.CurrentCell.RowIndex, SudokuTable.CurrentCell.ColumnIndex);
                    clearCandidates.Enabled = controller.CurrentProblem.HasCandidates();
                    SudokuTable.Refresh();
                }
            };

            // Menü zuweisen
            SudokuTable.ContextMenuStrip = cellContextMenu;

            // Selektion beim Rechtsklick korrigieren (Standard WinForms selektiert nicht bei Rechtsklick)
            SudokuTable.CellMouseDown += CellMouseDown;
        }

        private void CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                HandleRightMouseButton(e.RowIndex, e.ColumnIndex);
            }
        }

        private void HandleRightMouseButton(int row, int col)
        {
            SudokuTable.CurrentCell = SudokuTable[col, row];
            cellContextMenu.Items[0].Enabled = SudokuTable.CurrentCell.Value.ToString().Trim().Length != 0;
            cellContextMenu.Items[1].Enabled = controller.CurrentProblem.HasCandidate(row, col);
        }
        private void VersionHistoryClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Resources.VersionHistory);
        }

        private void OptionsMenuOpening(object sender, EventArgs e)
        {
            pause.Enabled = resetTimer.Enabled = solvingTimer.Enabled;
        }

        private void SudokuOfTheDayClicked(object sender, EventArgs e)
        {
            if(SudokuOfTheDay())
                MessageBox.Show(this, String.Format(Resources.SudokuOfTheDayInfo, controller.CurrentProblem.SeverityLevelText));
            else
                MessageBox.Show(this, Resources.SudokuOfTheDayNotLoaded);
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
                    if(solvingTimer.Enabled) controller.CurrentProblem.SolvingTime = DateTime.Now - interactiveStart;
                    Settings.Default.State = controller.CurrentProblem.Serialize();
                    Settings.Default.Save();
                }
                else
                {
                    Settings.Default.State = "";
                    Settings.Default.Save();
                    if(!SyncProblemWithGUI(true))
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