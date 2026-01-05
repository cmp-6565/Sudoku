using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    public enum SudokuPart { Row, Column, Block, UpDiagonal, DownDiagonal };

    public partial class SudokuForm: Form
    {
        public const int RectSize=3;
        public const int SudokuSize=RectSize*RectSize;
        public const int TotalCellCount=SudokuSize*SudokuSize;

        private BaseProblem problem;
        private BaseProblem backup;
        private TrickyProblems trickyProblems;
        private PrintParameters printParameters;
        private DateTime computingStart;
        private DateTime interactiveStart;
        private GenerationParameters generationParameters;
        private int currentSolution=0;
        private Font normalDisplayFont;
        private Font boldDisplayFont;
        private Font strikethroughFont;
        private String[] fontSizes;
        private Boolean abortRequested=false;
        private Boolean applicationExiting=false;
        private Boolean showCandidates=false;
        private CultureInfo cultureInfo;
        private Stack<CoreValue> undoStack;
        private OptionsDialog optionsDialog=null;
        private Boolean mouseWheelEditing=false;
        private Boolean usePrecalculatedProblem=false;
        private int severityLevel=0;
        private int incorrectTries=0;
        private Boolean valuesVisible=true;
        private Boolean hideValues=true;
        private String statusBarText = "";

        // Neue Felder für den asynchronen Solver
        private SudokuSolver activeSolver;
        private CancellationTokenSource solverCts;

        Color gray;
        Color lightGray;
        Color green;
        Color lightGreen;
        Color textColor;

        public delegate void SudokuAction();

        /// <summary>
        /// Constructor for the form, mainly used for defaulting some variables and initializing of the gui.
        /// </summary>
        public SudokuForm()
        {
            Thread.CurrentThread.CurrentUICulture=(cultureInfo=new CultureInfo(Settings.Default.DisplayLanguage));

            InitializeComponent();
            SudokuTable.MouseWheel+=new MouseEventHandler(MouseWheelHandler);
            SudokuTable.Rows.Add(SudokuSize);
            solutionTimer.Interval=1000;
            solvingTimer.Interval=1000;
            autoPauseTimer.Interval=Convert.ToInt32(Settings.Default.AutoPauseLag)*1000;

            fontSizes=Settings.Default.FontSizes.Split('|');
            normalDisplayFont=new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size-1]), FontStyle.Regular);
            boldDisplayFont=new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size-1]), FontStyle.Bold);
            strikethroughFont=new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size-1]), FontStyle.Bold | FontStyle.Strikeout);

            int colorIndex=255-(int)(255f*((float)Settings.Default.Contrast / 100f));
            gray=Color.FromArgb(colorIndex, colorIndex, colorIndex);
            green=Color.FromArgb(127, colorIndex, 127);
            colorIndex=255-(int)(255f*((float)Settings.Default.Contrast / 220f));
            lightGray=Color.FromArgb(colorIndex, colorIndex, colorIndex);
            colorIndex=255-(int)(255f*((float)Settings.Default.Contrast / 1000f));
            lightGreen=Color.FromArgb(200, colorIndex, 200);
            textColor=Settings.Default.Contrast > 50? Color.White: Color.Black;

            debug.Checked=Settings.Default.Debug;
            autoCheck.Checked=Settings.Default.AutoCheck;
            showPossibleValues.Checked=Settings.Default.ShowHints;
            findallSolutions.Checked=Settings.Default.FindAllSolutions;
            ShowInTaskbar=!Settings.Default.HideWhenMinimized;
            markNeighbors.Checked=Settings.Default.MarkNeighbors;

            if(Settings.Default.State.Length > 0)
                problem=RestoreProblemState();
            else
                problem=CreateNewProblem(false);
            FormatTable();
            EnableGUI();

            Deactivate+=new EventHandler(FocusLost);
            Activated+=new EventHandler(FocusGotten);

            generationParameters=new GenerationParameters();
            printParameters=new PrintParameters();
            trickyProblems=new TrickyProblems();
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

        private void CancelSolving(Boolean abort)
        {
            abortRequested = abort;
            CancelSolving();

            if(abort) try { problem.Cancel(); } catch { }
        }
        private void CancelSolving()
        {
            if(solverCts != null && !solverCts.IsCancellationRequested)
                solverCts.Cancel();

            if(activeSolver != null)
            {
                try
                {
                    activeSolver.Cancel();
                }
                catch { }
            }
            solverCts = null;
            activeSolver = null;
        }
        private void FocusLost(object sender, EventArgs e)
        {
            if(SudokuTable.Enabled && hideValues && Settings.Default.AutoPause)
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

            ComponentResourceManager resources=new ComponentResourceManager(typeof(SudokuForm));
            for(int i=0; i < sudokuMenu.Items.Count; i++)
            {
                ToolStripMenuItem mi=(ToolStripMenuItem)sudokuMenu.Items[i];
                resources.ApplyResources(sudokuMenu.Items[i], sudokuMenu.Items[i].Name);
                if(mi.HasDropDownItems)
                    for(int j=0; j < mi.DropDownItems.Count; j++)
                    {
                        if(mi.DropDownItems[j] is ToolStripMenuItem)
                        {
                            ToolStripMenuItem ddm=(ToolStripMenuItem)mi.DropDownItems[j];
                            resources.ApplyResources(mi.DropDownItems[j], mi.DropDownItems[j].Name);
                            if(ddm.HasDropDownItems)
                                for(int k=0; k < ddm.DropDownItems.Count; k++)
                                    resources.ApplyResources(ddm.DropDownItems[k], ddm.DropDownItems[k].Name);
                        }
                    }
            }
            resources.ApplyResources(sudokuStatusBarText, sudokuStatusBarText.Name);
            status.Text=String.Empty;
        }

        // GUI Handling
        /// <summary>
        /// Sets the layout of the table, mainly the Contrast set in the application's options
        /// </summary>
        private void FormatTable()
        {
            int row, col;

            for(row=0; row < SudokuSize; row++)
                for(col=0; col < SudokuSize; col++)
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
            Boolean obfuscated=((row / 3) % 2 == 1 && (col / 3) % 2 == 0) || ((row / 3) % 2 == 0 && (col / 3) % 2 == 1);
            SudokuTable[row, col].Style.BackColor=(obfuscated? gray: ((problem is XSudokuProblem) && (row == col || row+col == SudokuSize-1)? lightGray: Color.White));
            SudokuTable[row, col].Style.ForeColor=(obfuscated? textColor: Color.Black);
            SudokuTable[row, col].Style.SelectionBackColor=System.Drawing.SystemColors.AppWorkspace;
            if(problem.Matrix.Cell(row, col).CellValue == Values.Undefined)
            {
                SudokuTable[col, row].Value="";
                problem.Matrix.Cell(row, col).ReadOnly = false;
            }
        }

        private void MarkNeighbors(DataGridView dgv)
        {
            BaseCell[] neighbors=problem.Matrix.Cell(dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y).Neighbors;
            Boolean obfuscated;

            obfuscated=((dgv.CurrentCellAddress.X / 3) % 2 == 1 && (dgv.CurrentCellAddress.Y / 3) % 2 == 0) || ((dgv.CurrentCellAddress.X / 3) % 2 == 0 && (dgv.CurrentCellAddress.Y / 3) % 2 == 1);
            SudokuTable[dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y].Style.BackColor=(obfuscated? green: lightGreen);
            SudokuTable[dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y].Style.SelectionBackColor=(obfuscated? Color.DarkGreen: Color.SeaGreen);
            foreach(BaseCell cell in neighbors)
            {
                obfuscated=((cell.Row / 3) % 2 == 1 && (cell.Col / 3) % 2 == 0) || ((cell.Row / 3) % 2 == 0 && (cell.Col / 3) % 2 == 1);
                SudokuTable[cell.Row, cell.Col].Style.BackColor=(obfuscated? green: lightGreen);
                SudokuTable[cell.Row, cell.Col].Style.ForeColor=(obfuscated? textColor: Color.Black);
            }
        }

        /// <summary>
        /// Resizes the table as defined in the application's options, the actual size of the cells also depents on the application setting 
        /// 'MagnifactionFactor'
        /// </summary>
        private void ResizeTable()
        {
            int width=0;
            int height=0;
            int cellSize=(int)((float)Settings.Default.Size*Settings.Default.MagnificationFactor*Settings.Default.CellWidth*.7f);

            for(int i=0; i < SudokuSize; i++)
            {
                width+=(SudokuTable.Columns[i].Width=cellSize);
                height+=(SudokuTable.Rows[i].Height=cellSize);
            }

            Width=width+30;
            Height=height+140+(60*Settings.Default.Size);

            SudokuTable.Width=width+1;
            SudokuTable.Height=height+1;
            status.Location=new Point(status.Location.X, SudokuTable.Location.Y+height+10);
            next.Location=new Point(next.Location.X, SudokuTable.Location.Y+height+10);
            prior.Location=new Point(prior.Location.X, SudokuTable.Location.Y+height+10);
        }

        /// <summary>
        /// Sets the cell font for all cells which depends on the cell's size; the actual font name is an application setting
        /// </summary>
        private void SetCellFont()
        {
            for(int row=0; row < SudokuSize; row++)
                for(int col=0; col < SudokuSize; col++)
                    SetCellFont(row, col);
            status.Font=new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size-1])*.8f, FontStyle.Regular);
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
            SudokuTable[col, row].Style.Font=problem.Matrix.Cell(row, col).ReadOnly? boldDisplayFont: normalDisplayFont;
            SudokuTable[col, row].ReadOnly=problem.Matrix.Cell(row, col).ReadOnly;
        }

        /// <summary>
        /// Reads all values from the gui into the main problem
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
            Text=ProductName;
            SudokuTable.EndEdit();
            mouseWheelEditing=false;

            // Marshal UI grid to string[,] with minimal processing
            string[,] grid=new string[SudokuForm.SudokuSize, SudokuForm.SudokuSize];
            for(int row=0; row<SudokuForm.SudokuSize; row++)
                for(int col=0; col<SudokuForm.SudokuSize; col++)
                    grid[row, col]=SudokuTable[col, row].Value as string;

            bool ok=SyncHelper.TrySyncGrid(problem, grid, cultureInfo, autoCheck.Checked, ref incorrectTries, out var syncedProblem);
            if(!ok)
            {
                status.Text=String.Empty;

                // On error, mark cells lazily (only when failure) and optionally show message
                string firstError=null;
                for(int row=0; row<SudokuForm.SudokuSize; row++)
                    for(int col=0; col<SudokuForm.SudokuSize; col++)
                        SudokuTable[col, row].ErrorText=String.Empty;

                for(int row=0; row<SudokuForm.SudokuSize; row++)
                {
                    for(int col=0; col<SudokuForm.SudokuSize; col++)
                    {
                        string raw=grid[row, col];
                        if(raw==null) continue;
                        string value=raw.Trim();
                        if(value.Length==0) continue;

                        byte parsed;
                        bool parseOk=byte.TryParse(value, NumberStyles.Integer, cultureInfo, out parsed) && parsed>=1 && parsed<=SudokuForm.SudokuSize;
                        if(!parseOk)
                        {
                            string msg=String.Format(cultureInfo, Resources.InvalidValue, value, row+1, col+1);
                            SudokuTable[col, row].ErrorText=msg;
                            if(firstError==null) firstError=msg;
                        }
                    }
                }

                if(!silent && firstError!=null)
                {
                    hideValues=false;
                    MessageBox.Show(firstError, Resources.SudokuError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    hideValues=true;
                }
                return false;
            }

            problem=syncedProblem;
            ResetTexts();
            if(Settings.Default.ShowHints) SudokuTable.Refresh();
            return true;
        }

        /// <summary>
        /// Counts the number of filled cells in the current problem
        /// </summary>
        /// <returns>
        /// Number of filled cells in the current problem
        /// </returns>
        private int FilledCells()
        {
            int count=0;

            for(int row=0; row < SudokuSize; row++)
                for(int col=0; col < SudokuSize; col++)
                    if(SudokuTable[col, row].Value != null && (((string)SudokuTable[col, row].Value).Trim()).Length > 0)
                        count++;
            return count;
        }

        /// <summary>
        /// Displays the current status of the problem is the status line; 
        /// if requested (option 'autocheck') a check whether the problem is solvable is done.
        /// </summary>
        private void CurrentStatus(Boolean silent)
        {
            int count;
            String problemStatus;

            count=FilledCells();
            problemStatus=Resources.FilledCells+count;

            Boolean inputOK=SyncProblemWithGUI(true);
            if(autoCheck.Checked && (!inputOK || !problem.Resolvable()))
            {
                problemStatus+=(Environment.NewLine+Resources.NotResolvable);
                System.Media.SystemSounds.Hand.Play();
            }

            status.Text=problemStatus;

            if(!silent && count == TotalCellCount)
            {
                solvingTimer.Stop();

                TimeSpan ts=DateTime.Now-interactiveStart;
                hideValues=false;
                MessageBox.Show(
                    inputOK?
                        Resources.Congratulations+Environment.NewLine+Resources.ProblemSolved+Environment.NewLine+Resources.TimeNeeded+String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#}", ts.Days*24+ts.Hours, ts.Minutes, ts.Seconds)+Environment.NewLine+Resources.IncorrectTries+String.Format(cultureInfo, "{0}", incorrectTries):
                        Resources.ProblemNotSolved,
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                hideValues=true;
                sudokuStatusBarText.Text=Resources.Ready;
            }
        }

        /// <summary>
        /// Set the given value into the given cell within the given problem
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
            for(int row=0; row < SudokuSize; row++)
                for(int col=0; col < SudokuSize; col++)
                {
                    SudokuTable[col, row].Style.Font=normalDisplayFont;
                    SudokuTable[col, row].Value=String.Empty;
                    SudokuTable[col, row].ErrorText=String.Empty;
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
            for(int i=0; i < SudokuForm.SudokuSize; i++)
                for(int j=0; j < SudokuForm.SudokuSize; j++)
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
            SudokuTable[col, row].Value=(value == Values.Undefined? " ": value.ToString(cultureInfo));
            SetCellFont(row, col);
        }

        /// <summary>
        /// Calculates a string representation of the severity level of the given problem
        /// </summary>
        /// <param name="currentProblem">
        /// Problem of interest
        /// </param>
        /// <returns>
        /// Severity level of the problem in the current language
        /// </returns>
        static private String SeverityLevel(BaseProblem currentProblem)
        {
            currentProblem.SeverityLevel=float.NaN;
            return currentProblem.SeverityLevelText;
        }

        /// <summary>
        /// Calculates a numeric representation of the severity level of the given problem, not to muddle up with the internal severity level
        /// </summary>
        /// <param name="currentProblem">
        /// Problem of interest
        /// </param>
        /// <returns>
        /// Severity level of the problem (0: not defined, 1: trivial, 2: easy, 4: intermediate, 8: hard)
        /// </returns>
        static private int SeverityLevelInt(BaseProblem currentProblem)
        {
            currentProblem.SeverityLevel=float.NaN;
            return currentProblem.SeverityLevelInt;
        }

        static private String InternalSeverityLevel(BaseProblem currentProblem)
        {
            currentProblem.SeverityLevel=float.NaN;
            return currentProblem.SeverityLevel.ToString("f");
        }

        /// <summary>
        /// Sets the status text to a string stating that the current generation has been aborted by the user
        /// </summary>
        private void GenerationAborted()
        {
            status.Text =
                String.Format(cultureInfo, Resources.GenerationAborted.Replace("\\n", Environment.NewLine),
                generationParameters.GenerateBooklet? String.Format(cultureInfo, Resources.GeneratedProblems.Replace("\\n", Environment.NewLine), generationParameters.CurrentProblem, Settings.Default.BookletSizeNew)+Environment.NewLine: String.Empty,
                generationParameters.CheckedProblems, generationParameters.TotalPasses);
            status.Update();
            abortRequested=false;
            ResetDetachedProcess();
            problem=backup.Clone();
            DisplayValues(problem.Matrix);
            generationParameters=new GenerationParameters();
        }

        /// <summary>
        /// Displays the current status of the generation of a problem in the status field
        /// </summary>
        private void GenerationStatus()
        {
            // Verwende den PassCount vom aktiven Solver, falls dieser läuft
            long currentPasses = (activeSolver != null && !activeSolver.IsCompleted)? activeSolver.TotalPassCount: problem.TotalPassCounter;

            status.Text =
                (usePrecalculatedProblem ? String.Format(cultureInfo, Resources.RetrieveProblem) :
                    (generationParameters.GenerateBooklet ? String.Format(cultureInfo, Resources.GeneratedProblems, generationParameters.CurrentProblem, Settings.Default.BookletSizeNew) + Environment.NewLine : String.Empty) +
                    String.Format(cultureInfo, Resources.GeneratingStatus, generationParameters.CheckedProblems) + Environment.NewLine + String.Format(cultureInfo, Resources.CheckingStatus, generationParameters.TotalPasses + currentPasses) +
                    Environment.NewLine +
                    Resources.PreAllocatedValues + generationParameters.PreAllocatedValues.ToString(cultureInfo));
            status.Update();
        }

        /// <summary>
        /// Resets all texts to their default values
        /// </summary>
        private void ResetTexts()
        {
            DisplayValues(problem.Matrix);
            status.Text=String.Empty;
            prior.Enabled=next.Enabled=false;
            if(!solvingTimer.Enabled) sudokuStatusBarText.Text=Resources.Ready;
            Text=ProductName;
        }

        /// <summary>
        /// Clears the undo-stack
        /// </summary>
        private void ResetUndoStack()
        {
            undoStack=new Stack<CoreValue>();
            undo.Enabled=false;
            solvingTimer.Stop();
            interactiveStart=DateTime.MinValue;
            problem.Dirty=false;
        }

        // Misc functions
        /// <summary>
        /// Checks whether or not the current problem is valid an solvable
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
            if(!problem.Resolvable())
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
                hideValues=false;
                MessageBox.Show(Resources.NotFixable);
                hideValues=true;
                return;
            }
            for(int row=0; row < SudokuSize; row++)
                for(int col=0; col < SudokuSize; col++)
                    problem.Matrix.Cell(row, col).ReadOnly=(readOnly && SudokuTable[col, row].Value.ToString().Trim() != String.Empty);
            DisplayValues(problem.Matrix);
        }

        private void CheckVersion()
        {
            if(Settings.Default.LastVersion != AssemblyInfo.AssemblyVersion)
                VersionHistoryClicked(null, null);
            Settings.Default.LastVersion=AssemblyInfo.AssemblyVersion;
        }

        // Main functions
        private void GenerateProblems(int nProblems, Boolean xSudoku)
        {
            CancelSolving();
            backup = CreateNewProblem(xSudoku);
            if(!(generationParameters.GenerateBooklet=(nProblems != 1)))
            {
                hideValues=false;
                severityLevel=GetSeverity();
                hideValues=true;
            }
            else
                severityLevel=Settings.Default.SeverityLevel;

            if(severityLevel == 0) return; // the user pressed "Cancel"

            abortRequested=false;
            DisableGUI();
            trickyProblems.Clear();
            usePrecalculatedProblem=Settings.Default.UsePrecalculatedProblems;
            if(GenerateBaseProblem())
                StartDetachedProcess(DisplayGeneratingProcess, (usePrecalculatedProblem? Resources.Loading: Resources.Generating), 2, true);
            else
                GenerationAborted();
            EnableGUI();
        }

        private void SolveProblem()
        {
            CancelSolving();
            if(!PreCheck()) return;

            DisplayValues(problem.Matrix);
            backup=problem.Clone();
            ResetUndoStack();

            if(debug.Checked) problem.Matrix.CellChanged+=HandleCellChanged;

            StartDetachedProcess(DisplaySolvingProcess, Resources.Thinking, findallSolutions.Checked? UInt64.MaxValue: 1, true);
        }

        private void ShowDefiniteValues()
        {
            CancelSolving();
            if(!PreCheck()) return;

            backup=problem.Clone();
            problem.PrepareMatrix();
            DisplayValues(problem.Matrix);
            ResetUndoStack();
            SyncProblemWithGUI(true);
            status.Text=String.Format(cultureInfo, Resources.ProblemInfo.Replace("\\n", Environment.NewLine), problem.nValues-problem.nComputedValues, problem.nComputedValues, problem.nVariableValues);
            status.Update();
        }

        private void Hints()
        {
            CancelSolving();
            if(!PreCheck(true)) return;

            BaseProblem tmp=problem.Clone();
            int count;
            Color hintColor=Color.Red;

            List<BaseCell> values=problem.GetObviousCells();
            if(values.Count == 0)
            {
                hintColor=Color.Orange;
                values=problem.GetHints();
            }

            if(values.Count == 0)
            {
                hideValues=false;
                MessageBox.Show(Resources.NoHints);
                hideValues=true;
                return;
            }

            DataGridViewSelectedCellCollection cells=SudokuTable.SelectedCells;
            foreach(DataGridViewCell cell in cells) // Even though this should be only one single cell, one might never know if this changes any time...
                cell.Selected=false;

            if(values.Count <= Settings.Default.MaxHints)
                for(count=0; count < values.Count; count++)
                    ShowHint(values[count], hintColor);
            else
            {
                List<BaseCell> hints=new List<BaseCell>();
                Random rand=new Random();
                int index;
                do
                    if(!hints.Contains(values[(index=rand.Next(values.Count))]))
                        hints.Add(values[index]);
                while(hints.Count < Settings.Default.MaxHints);

                for(count=0; count < Settings.Default.MaxHints; count++)
                    ShowHint(hints[count], hintColor);
            }

            foreach(DataGridViewCell cell in cells)
                cell.Selected=true;
            SudokuTable.Update();

            problem=tmp.Clone();
        }

        private void ShowHint(BaseCell hint, Color hintColor)
        {
            Color currentColor=SudokuTable[hint.Col, hint.Row].Style.BackColor;
            SudokuTable[hint.Col, hint.Row].Style.BackColor=hintColor;
            SudokuTable.Update();
            Thread.Sleep(500);
            SudokuTable[hint.Col, hint.Row].Style.BackColor=currentColor;
        }

        private void DisplayProblemInfo()
        {
            String problemInfo;
            Boolean modified=problem.Dirty;
            Boolean problemValid=SyncProblemWithGUI(true);

            problemInfo=Resources.PreAllocatedValues+problem.nValues.ToString(cultureInfo);
            problem.PrepareMatrix();
            problemInfo+=Environment.NewLine+Resources.DefiniteCells+problem.nComputedValues.ToString(cultureInfo);
            problem.ResetMatrix();
            if(problemValid)
                problemInfo+=Environment.NewLine+Resources.ComplexityLevel+SeverityLevel(problem)+" ("+String.Format(cultureInfo, "{0:0.00}", problem.SeverityLevel)+")";
            problemInfo+=Environment.NewLine +
                (problem.ProblemSolved? String.Format(cultureInfo, Resources.CheckResult, problem is XSudokuProblem? "X-": Resources.Classic, Resources.AtLeast):
                 problem.Resolvable() && problemValid? String.Format(cultureInfo, Resources.ValidationStatus, problem is XSudokuProblem? "X-": Resources.Classic): Resources.NotResolvable);
            if(!String.IsNullOrEmpty(problem.Filename))
                problemInfo+=Environment.NewLine+Resources.Filename+Environment.NewLine+problem.Filename;
            if(!String.IsNullOrEmpty(problem.Comment))
                problemInfo+=Environment.NewLine+problem.Comment;
            hideValues=false;
            MessageBox.Show(problemInfo, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            hideValues=true;
            problem.Dirty=modified;
        }

        private void DisplayCellInfo(int row, int col)
        {
            // TODO:
            // Die Gründe für die indirekten Blocks ausgeben (pair, ...)
            String cellInfo;
            BaseCell cell=problem.Matrix.Cell(row, col);

            cellInfo=String.Format(cultureInfo, Resources.Cellinfo, row+1, col+1, (cell.ReadOnly? " ("+Resources.ReadOnly+") ": ""))+Environment.NewLine;
            if(cell.DefinitiveValue != Values.Undefined)
                cellInfo+=Environment.NewLine+String.Format(cultureInfo, Resources.DefiniteValue)+cell.DefinitiveValue.ToString();
            else
                if(cell.FixedValue)
                cellInfo+=Environment.NewLine+String.Format(cultureInfo, Resources.CellValue)+cell.CellValue.ToString();

            String directBlockedCells="";
            String indirectBlockedCells="";

            for(int i=1; i <= SudokuSize; i++)
            {
                if(i != cell.DefinitiveValue && i != cell.CellValue)
                {
                    if(cell.Blocked(i))
                        directBlockedCells+=(directBlockedCells.Length == 0? i.ToString(): ", "+i.ToString());
                    else
                        if(cell.IndirectlyBlocked(i)) indirectBlockedCells+=(indirectBlockedCells.Length == 0? i.ToString(): ", "+i.ToString());
                }
            }

            cellInfo +=
                Environment.NewLine+String.Format(cultureInfo, Resources.DirectBlocks) +
                (directBlockedCells.Length == 0? Resources.None: directBlockedCells) +
                Environment.NewLine+String.Format(cultureInfo, Resources.IndirectBlocks) +
                (indirectBlockedCells.Length == 0? Resources.None: indirectBlockedCells);

            hideValues=false;
            MessageBox.Show(cellInfo);
            hideValues=true;

            return;
        }

        private void PublishTrickyProblems()
        {
            if(trickyProblems.Empty) return;

            hideValues=false;
            if(MessageBox.Show((generationParameters.GenerateBooklet? Resources.OneOrMoreProblems: Resources.OneProblem)+Resources.Publish, ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if(trickyProblems.Publish())
                    MessageBox.Show(String.Format(Resources.PublishOK, trickyProblems.Count));
                else
                    MessageBox.Show(String.Format(Resources.PublishFailed, Settings.Default.MailAddress));
            }
            hideValues=true;
        }

        private void CheckProblem()
        {
            CancelSolving();
            if(!SyncProblemWithGUI(false)) return;
            hideValues=false;
            MessageBox.Show(problem.Resolvable()? String.Format(cultureInfo, Resources.ValidationStatus, problem is XSudokuProblem? "X-": Resources.Classic): Resources.NotResolvable, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            hideValues=true;
        }

        private void ValidateProblem()
        {
            if(!PreCheck(true)) return;

            CancelSolving();
            backup = problem.Clone();
            StartDetachedProcess(DisplayCheckingProcess, Resources.Checking, 1, true);
            solvingTimer.Start();
        }

        private void ResetProblem()
        {
            CancelSolving();
            problem = backup.Clone();
            problem.ResetSolutions();
            ResetUndoStack();
            ResetTexts();
            for(int row=0; row < SudokuSize; row++)
                for(int col=0; col < SudokuSize; col++)
                    SudokuTable[col, row].ErrorText=String.Empty;
        }

        private BaseProblem CreateNewProblem(Boolean xSudoku)
        {
            CancelSolving();

            if(xSudoku)
                problem=new XSudokuProblem();
            else
                problem=new SudokuProblem();
            backup=problem.Clone();

            UpdateGUI();
            ResetUndoStack();
            ResetTexts();
            ResetMatrix();
            incorrectTries=0;

            return problem;
        }

        private Boolean SudokuOfTheDay()
        {
            BaseProblem sudokuProblem=CreateNewProblem(Settings.Default.SudokuOfTheDay);
            if(sudokuProblem.SudokuOfTheDay())
            {
                problem=sudokuProblem.Clone();

                UpdateGUI();
                DisplayValues(problem.Matrix);
                SetCellFont();

                backup=problem.Clone();
                ResetUndoStack();
                return true;
            }
            else
                return false;
        }

        private BaseProblem LoadProblem(Boolean xSudoku)
        {
            BaseProblem sudokuProblem=CreateNewProblem(xSudoku);
            Boolean loadResult;

            hideValues=false;
            loadResult=sudokuProblem.Load();
            hideValues=true;
            if(loadResult)
                return sudokuProblem;
            else
                return null;
        }

        private BaseProblem CreateProblemFromFile(String filename)
        {
            return CreateProblemFromFile(filename, true, true, true);
        }

        private BaseProblem CreateProblemFromFile(String filename, Boolean normalSudoku, Boolean xSudoku, Boolean loadCandidates)
        {
            StreamReader sr=null;
            BaseProblem sudokuProblem=null;

            CancelSolving();
            try
            {
                Char sudokuType;

                sr=new StreamReader(filename.Replace("%20", " "), System.Text.Encoding.Default);
                sudokuType=(Char)sr.Read();
                if(sudokuType != SudokuProblem.ProblemIdentifier && sudokuType != XSudokuProblem.ProblemIdentifier)
                    throw new InvalidDataException();
                if(sudokuType == SudokuProblem.ProblemIdentifier && normalSudoku || sudokuType == XSudokuProblem.ProblemIdentifier && xSudoku)
                {
                    sudokuProblem=CreateNewProblem(sudokuType == XSudokuProblem.ProblemIdentifier);
                    sudokuProblem.ReadFromFile(sr);
                    if(loadCandidates)
                    {
                        sudokuProblem.LoadCandidates(sr, false);
                        sudokuProblem.LoadCandidates(sr, true);
                    }
                }
            }
            catch(Exception) { throw; }
            finally { sr.Close(); }
            sudokuProblem.Filename=filename;

            return sudokuProblem;
        }

        private BaseProblem RestoreProblemState()
        {
            Char sudokuType=(Char)Settings.Default.State[0];

            CancelSolving();
            if(sudokuType != SudokuProblem.ProblemIdentifier && sudokuType != XSudokuProblem.ProblemIdentifier)
                throw new InvalidDataException();
            problem=CreateNewProblem(sudokuType == XSudokuProblem.ProblemIdentifier);
            try
            {
                problem.InitProblem(Settings.Default.State.Substring(1, SudokuForm.TotalCellCount).ToCharArray(), Settings.Default.State.Substring(SudokuForm.TotalCellCount+1, 16).ToCharArray(), null);
                if(Settings.Default.State.IndexOf('\n') > 0)
                {
                    problem.LoadCandidates(Settings.Default.State.Substring(Settings.Default.State.IndexOf('\n')+1), false);
                    problem.LoadCandidates(Settings.Default.State.Substring(Settings.Default.State.LastIndexOf('\n')+1), true);
                }
            }
            catch(Exception)
            {
                ;
            }

            UpdateGUI();
            ResetUndoStack();
            ResetTexts();
            ResetMatrix();
            DisplayValues(problem.Matrix);

            return problem;
        }

        private void NextSolution()
        {
            DisplayValues(problem.Solutions[++currentSolution]);
            next.Enabled=(currentSolution < problem.Solutions.Count-1);
            prior.Enabled=(currentSolution > 0);
            Text=String.Format(cultureInfo, Resources.DisplaySolution, currentSolution+1, problem.Solutions[currentSolution].Counter);
        }

        private void PriorSolution()
        {
            DisplayValues(problem.Solutions[--currentSolution]);
            prior.Enabled=(currentSolution > 0);
            next.Enabled=(problem.Solutions.Count > 1);
            Text=String.Format(cultureInfo, Resources.DisplaySolution, currentSolution+1, problem.Solutions[currentSolution].Counter);
        }

        private Boolean GenerateBaseProblem()
        {
            if(abortRequested) return false;

            int counter =0;
            int minPreAllocations=problem.Matrix.MinimumValues;

            CancelSolving();
            problem = backup.Clone();
            if(usePrecalculatedProblem)
            {
                BaseProblem tmpProblem=LoadProblem(problem is XSudokuProblem);
                if(usePrecalculatedProblem=(tmpProblem != null))
                    problem=tmpProblem;
            }

            if(!usePrecalculatedProblem)
            {
                do
                {
                    counter++;
                    if(generationParameters.Reset)
                    {
                        SetValue(problem, generationParameters.Row, generationParameters.Col, Values.Undefined);
                        problem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly=false;
                        DisplayValue(generationParameters.Row, generationParameters.Col, Values.Undefined);
                    }

                    generationParameters.NewValue();
                    try
                    {
                        SetValue(problem, generationParameters.Row, generationParameters.Col, generationParameters.GeneratedValue);
                        problem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly=true;
                        DisplayValue(generationParameters.Row, generationParameters.Col, generationParameters.GeneratedValue);

                        if(generationParameters.PreAllocatedValues >= minPreAllocations)
                            generationParameters.CheckedProblems+=1;
                        generationParameters.PreAllocatedValues=problem.nValues-problem.nComputedValues;
                        GenerationStatus();
                        generationParameters.Reset=!problem.Resolvable();
                    }
                    catch(ArgumentException)
                    {
                        generationParameters.Reset=true;
                    }

                    if((counter % 10) == 0) Application.DoEvents();
                } while(!abortRequested && (generationParameters.Reset || problem.NumDistinctValues() < SudokuForm.SudokuSize-1 || generationParameters.PreAllocatedValues < minPreAllocations));
            }
            backup=problem.Clone();

            return !abortRequested;
        }

        private void FillCells()
        {
            problem.ResetMatrix();
            while(problem.nValues < Settings.Default.MinValues && !abortRequested)
            {
                generationParameters.NewValue();
                if(problem.GetValue(generationParameters.Row, generationParameters.Col) == Values.Undefined)
                {
                    SetValue(problem, generationParameters.Row, generationParameters.Col, problem.Solutions[0].GetValue(generationParameters.Row, generationParameters.Col));
                    problem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly=true;
                    SetCellFont(generationParameters.Row, generationParameters.Col);
                }
            }

            while((SeverityLevelInt(problem) & severityLevel) == 0 && problem.nValues < Settings.Default.MaxValues && !abortRequested)
            {
                generationParameters.NewValue();
                if(problem.GetValue(generationParameters.Row, generationParameters.Col) == Values.Undefined)
                {
                    SetValue(problem, generationParameters.Row, generationParameters.Col, problem.Solutions[0].GetValue(generationParameters.Row, generationParameters.Col));
                    problem.Matrix.Cell(generationParameters.Row, generationParameters.Col).ReadOnly=true;
                    SetCellFont(generationParameters.Row, generationParameters.Col);
                }
            }
        }

        private void tick(object sender, EventArgs e)
        {
            TimeSpan ts=DateTime.Now-computingStart;
            sudokuStatusBarText.Text=String.Format(cultureInfo, statusBarText, ts.Hours*3600+ts.Minutes*60+ts.Seconds);
            sudokuStatusBar.Update();
        }

        // Threading
        private void StartDetachedProcess(SudokuAction action, String statusBarText, UInt64 numSolutions, Boolean initStatus)
        {
            abortRequested=false;
            DisableGUI();
            CancelSolving();

            if(initStatus)
            {
                status.Text=String.Empty;
                status.Update();
            }
            this.statusBarText=statusBarText;

            solutionTimer.Dispose();
            solutionTimer=new System.Windows.Forms.Timer(components);
            solutionTimer.Tick+=new EventHandler(tick);
            solutionTimer.Interval=10;
            solutionTimer.Start();
            computingStart=DateTime.Now;

            // Erstelle und starte den Solver
            InitializeCancellationToken();
            activeSolver = new SudokuSolver(problem, findallSolutions.Checked? UInt64.MaxValue: 1, solverCts.Token);

            action();
        }

        private void InitializeCancellationToken()
        {
            // Initialisiere CancellationTokenSource für den Solver
            if(solverCts != null) solverCts.Dispose();
            solverCts = new CancellationTokenSource();
        }

        private void ResetDetachedProcess()
        {
            CancelSolving();
            sudokuStatusBarText.Text=Resources.Ready;
            solutionTimer.Stop();
            solvingTimer.Stop();
            if(debug.Checked) problem.Matrix.CellChanged-=HandleCellChanged;
            EnableGUI();
        }

        // Dialogs
        private Boolean UnsavedChanges()
        {
            Boolean rc=true;
            DialogResult dialogResult;

            if(problem.Dirty && FilledCells() != TotalCellCount)
            {
                hideValues=false;
                dialogResult=MessageBox.Show(Resources.UnsavedChanges, ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                hideValues=true;
                if(dialogResult == DialogResult.Yes)
                    rc=SaveProblem();
                else
                    rc=(dialogResult == DialogResult.No);
            }
            return rc;
        }

        private void OpenProblem()
        {
            if(UnsavedChanges())
            {
                openSudokuDialog.InitialDirectory=Settings.Default.ProblemDirectory;
                openSudokuDialog.DefaultExt="*"+Settings.Default.DefaultFileExtension;
                openSudokuDialog.Filter=String.Format(cultureInfo, Resources.FilterString, Settings.Default.DefaultFileExtension);
                hideValues=false;
                if(openSudokuDialog.ShowDialog() == DialogResult.OK)
                    LoadProblem(openSudokuDialog.FileName);
                hideValues=true;
            }
        }

        private void LoadProblem(String filename)
        {
            BaseProblem tmp=problem.Clone();

            hideValues=false;
            try
            {
                problem=CreateProblemFromFile(filename);
            }
            catch(ArgumentException)
            {
                MessageBox.Show(String.Format(cultureInfo, Resources.InvalidSudokuFile, filename), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                problem=tmp.Clone();
            }
            catch(InvalidDataException)
            {
                MessageBox.Show(Resources.InvalidSudokuIdentifier, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                problem=tmp.Clone();
            }
            catch(Exception e)
            {
                MessageBox.Show(Resources.OpenFailed+Environment.NewLine+e.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                problem=tmp.Clone();
            }
            hideValues=true;

            backup=problem.Clone();

            UpdateGUI();
            DisplayValues(problem.Matrix);
            SetCellFont();
            ResetUndoStack();
        }

        private async Task<Boolean> MinimizeAsync(int maxSeverity)
        {
            DateTime x = DateTime.Now;
            String statusbarText = sudokuStatusBarText.Text;
            BaseProblem minimalProblem = null;
            Boolean rc = false;

            backup = problem.Clone();
            sudokuStatusBarText.Text = Resources.Minimizing;
            Cursor = Cursors.WaitCursor;
            DisableGUI();
            CancelSolving();

            InitializeCancellationToken();

            // Event-Handler für UI-Updates während der Minimierung
            var progress = new Progress<BaseProblem>(minProb =>
            {
                HandleMinimizing(this, minProb);
            });

            activeSolver = new SudokuSolver(problem, maxSeverity, solverCts.Token, progress);
            minimalProblem = activeSolver.MinimalProblem;

            if(minimalProblem != null)
            {
                rc = true;
                problem = minimalProblem;
            }

            problem.ResetMatrix();

            UpdateGUI();
            DisplayValues(problem.Matrix);
            SetCellFont();
            ResetUndoStack();
            EnableGUI();
            Cursor = Cursors.Default;
            sudokuStatusBarText.Text = statusbarText;

            return rc;
        }
        private Boolean SaveProblem(String filename)
        {
            Boolean returnCode=true;
            try
            {
                if(solvingTimer.Enabled)
                    problem.SolvingTime=DateTime.Now-interactiveStart;
                problem.SaveToFile(filename);
            }
            catch(Exception e)
            {
                hideValues=false;
                MessageBox.Show(Resources.SaveFailed+Environment.NewLine+e.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                hideValues=true;
                returnCode=false;
            }
            return returnCode;
        }

        private Boolean ExportProblem(String filename)
        {
            Boolean returnCode=true;
            try
            {
                problem.SaveToHTMLFile(filename);
            }
            catch(Exception e)
            {
                hideValues=false;
                MessageBox.Show(Resources.SaveFailed+Environment.NewLine+e.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                hideValues=true;
                returnCode=false;
            }
            return returnCode;
        }

        private Boolean SaveProblem()
        {
            if(!SyncProblemWithGUI(true))
            {
                hideValues=false;
                MessageBox.Show(Resources.InvalidProblem+Environment.NewLine+Resources.SaveNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                hideValues=true;
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
                hideValues=false;
                MessageBox.Show(Resources.InvalidProblem+Environment.NewLine+Resources.ExportNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                hideValues=true;
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
                hideValues=false;
                MessageBox.Show(Resources.InvalidProblem+Environment.NewLine+Resources.TwitterNotPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                hideValues=true;
                return false;
            }

            System.Diagnostics.Process.Start(Resources.TwitterURL+String.Format(cultureInfo, Resources.TwitterText, (problem is XSudokuProblem? "X": ""), problem.Serialize(false).Substring(1, SudokuForm.TotalCellCount)));

            return true;
        }

        private DialogResult ShowSaveDialog(String Extension)
        {
            DialogResult result=DialogResult.OK;
            saveSudokuDialog.InitialDirectory=Settings.Default.ProblemDirectory;
            saveSudokuDialog.RestoreDirectory=true;
            saveSudokuDialog.DefaultExt="*"+Extension;
            saveSudokuDialog.Filter=String.Format(cultureInfo, Resources.FilterString, Extension);
            saveSudokuDialog.FileName="Problem-"+DateTime.Now.ToString("yyyy.MM.dd-hh-mm", cultureInfo);
            hideValues=false;
            result=saveSudokuDialog.ShowDialog();
            hideValues=true;
            return result;
        }

        private void PushOnUndoStack(DataGridView dgv)
        {
            CoreValue cv=new CoreValue();
            cv.Row=dgv.CurrentCell.RowIndex;
            cv.Col=dgv.CurrentCell.ColumnIndex;
            if(dgv.CurrentCell.Value != null)
                cv.UnformatedValue=(String)dgv.CurrentCell.Value;
            undoStack.Push(cv);
            undo.Enabled=true;
        }

        // Diverse Events
        private void DropProblem(object sender, DragEventArgs e)
        {
            if(UnsavedChanges())
            {
                try
                {
                    String[] droppedData=(String[])e.Data.GetData(DataFormats.FileDrop.ToString());
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
                e.Effect=DragDropEffects.Move;
        }

        private void BeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if(sender is DataGridView)
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
                    problem.SolvingTime=TimeSpan.Zero;
                    solvingTimer.Start();
                    interactiveStart=DateTime.Now-problem.SolvingTime;
                }

                DataGridView dgv=(DataGridView)sender;
                problem.SetValue(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex, Values.Undefined);
                SetCellFont(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex);
            }
            mouseWheelEditing=false;

            CurrentStatus(false);
        }

        private new void KeyUp(object sender, KeyEventArgs e)
        {
            int candidate;
            candidate=e.KeyValue-96; // Numpad
            if(candidate < 0) candidate=e.KeyValue-48;

            if(sender is DataGridView && (Control.ModifierKeys == Keys.Control || Control.ModifierKeys == (Keys.Control | Keys.Shift)) && candidate > 0 && candidate <= SudokuSize)
            {
                hideValues=false;
                if(Settings.Default.ShowHints && MessageBox.Show(Resources.CandidatesNotShown, ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                    showPossibleValues.Checked=Settings.Default.ShowHints=false;
                hideValues=true;

                DataGridView dgv=(DataGridView)sender;
                if(!problem.Cell(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex).ReadOnly)
                {
                    problem.SetCandidate(dgv.CurrentCell.RowIndex, dgv.CurrentCell.ColumnIndex, candidate, Control.ModifierKeys == (Keys.Control | Keys.Shift));
                    clearCandidates.Enabled=problem.HasCandidates();
                }

                dgv.Refresh();
            }
        }

        private void HandleSpecialChar(object sender, KeyEventArgs e)
        {
            if(sender is DataGridView && e.KeyCode == Keys.Delete)
            {
                DataGridView dgv=(DataGridView)sender;
                if(!SudokuTable[dgv.CurrentCell.ColumnIndex, dgv.CurrentCell.RowIndex].ReadOnly)
                {
                    PushOnUndoStack(dgv);
                    SudokuTable[dgv.CurrentCell.ColumnIndex, dgv.CurrentCell.RowIndex].Value="";
                    CellEndEdit(sender);
                }
            }
        }

        private void MouseWheelHandler(object sender, MouseEventArgs e)
        {
            if(sender is DataGridView)
            {
                DataGridView dgv=(DataGridView)sender;

                if(dgv.EditingControl == null && !dgv.CurrentCell.ReadOnly)
                {
                    if(!mouseWheelEditing) PushOnUndoStack(dgv);

                    try
                    {
                        int currentValue=(dgv.CurrentCell.Value == null || ((String)dgv.CurrentCell.Value).Trim().Length == 0? 0: Convert.ToInt32(dgv.CurrentCell.Value, cultureInfo));
                        currentValue+=Math.Sign(e.Delta);
                        if(currentValue > 0 && currentValue <= SudokuSize)
                            dgv.CurrentCell.Value=currentValue.ToString();
                        else if(currentValue == Values.Undefined)
                            dgv.CurrentCell.Value="";
                        else
                            System.Media.SystemSounds.Hand.Play();
                        mouseWheelEditing=true;
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
                DataGridView dgv=(DataGridView)sender;
                BaseCell[] neighbors=problem.Matrix.Cell(dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y).Neighbors;

                FormatCell(dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y);
                foreach(BaseCell cell in neighbors)
                    FormatCell(cell.Row, cell.Col);
            }
        }

        private void ShowValues()
        {
            if(!valuesVisible)
            {
                DisplayValues(problem.Matrix);
                valuesVisible=true;
                if(!solvingTimer.Enabled)
                {
                    problem.SolvingTime=TimeSpan.Zero;
                    solvingTimer.Start();
                    interactiveStart=DateTime.Now;
                }
            }
        }
        private void CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if(sender is DataGridView && problem != null && Settings.Default.MarkNeighbors)
                MarkNeighbors((DataGridView)sender);
            ShowValues();
        }

        private void HandleCellChanged(object sender, BaseCell v)
        {
            DisplayValue(v.Row, v.Col, v.CellValue);
            try { Thread.Sleep(Settings.Default.TraceFrequence); }
            catch(ThreadInterruptedException) { /* do nothing */ }
            catch(Exception) { throw; }
            Application.DoEvents();
        }

        private void HandleOnTestCell(object sender, BaseCell cell)
        {
            SudokuTable[cell.Col, cell.Row].Style.Font=strikethroughFont;
            SudokuTable[cell.Col, cell.Row].Style.BackColor=Color.Coral;
        }

        private void HandleOnResetCell(object sender, BaseCell cell)
        {
            SudokuTable[cell.Col, cell.Row].Style.Font=boldDisplayFont;
            FormatCell(cell.Col, cell.Row);
            Application.DoEvents();
        }

        private void HandleMinimizing(object sender, BaseProblem minimalProblem)
        {
            status.Text=String.Format(Resources.CurrentMinimalProblem, SeverityLevel(minimalProblem), minimalProblem.nValues, problem.nValues).Replace("\\n", Environment.NewLine);
            status.Update();
            Application.DoEvents();
        }

        private void ShowCellHints(object sender, PaintEventArgs e)
        {
            if(sender is DataGridView)
            {
                Font printFont=(Settings.Default.Size == 1? PrintParameters.SmallFont: PrintParameters.NormalFont);

                showCandidates=!Settings.Default.ShowHints;
                DataGridView dgv=(DataGridView)sender;
                float cellSize=dgv.Columns[0].Width;
                for(int row=0; row < SudokuSize; row++)
                    for(int col=0; col < SudokuSize; col++)
                        if(problem.GetValue(row, col) == Values.Undefined)
                        {
                            RectangleF rf=new RectangleF(col*cellSize, row*cellSize, cellSize, cellSize);
                            if(rf.IntersectsWith(e.ClipRectangle))
                                if(Settings.Default.UseWatchHandHints)
                                    PrintWatchHands(problem.Cell(row, col), rf, e.Graphics);
                                else
                                    PrintHints(problem.Cell(row, col), rf, e.Graphics, printFont, SudokuTable[row, col].Style.ForeColor);
                        }
            }
        }

        private void DisplayCellInfo(object sender, EventArgs e)
        {
            DataGridViewSelectedCellCollection cells=SudokuTable.SelectedCells;
            if(cells.Count == 1)
                DisplayCellInfo(cells[0].RowIndex, cells[0].ColumnIndex);
        }

        private void ActivateGrid(object sender, EventArgs e)
        {
            SudokuTable.Focus();
        }

        private void ResizeForm(object sender, EventArgs e)
        {
            Opacity=(WindowState == FormWindowState.Minimized)? 0: 100;
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
            TimeSpan ts=DateTime.Now-interactiveStart;
            sudokuStatusBarText.Text=Resources.SolutionTime+String.Format(cultureInfo, "{0:0#}:{1:0#}:{2:0#}", ts.Days*24+ts.Hours, ts.Minutes, ts.Seconds);
        }

        private void AutoPauseTick(object sender, EventArgs e)
        {
            if(WindowState != FormWindowState.Minimized) 
                PauseClick(sender, e);
        }

        private void GenerationSingleProblemFinished()
        {
            status.Text=usePrecalculatedProblem?
                Resources.ProblemRetrieved:
                String.Format(cultureInfo, Resources.NewProblemGenerated.Replace("\\n", Environment.NewLine), SeverityLevel(problem), problem.nValues, generationParameters.CheckedProblems, generationParameters.TotalPasses);
            DisplayValues(problem.Matrix);
            PublishTrickyProblems();
            ResetDetachedProcess();
            generationParameters=new GenerationParameters();
            CancelSolving();
        }

        private void GenerationBookletProblemFinished()
        {
            printParameters.Problems.Add(problem);
            if(Settings.Default.AutoSaveBooklet)
                if(!SaveProblem(generationParameters.BaseDirectory+Path.DirectorySeparatorChar+"Problem-"+(generationParameters.CurrentProblem+1).ToString(cultureInfo)+"("+SeverityLevel(problem)+") ("+InternalSeverityLevel(problem)+")"+Settings.Default.DefaultFileExtension))
                    Settings.Default.AutoSaveBooklet=false;
            if(++generationParameters.CurrentProblem < Settings.Default.BookletSizeNew)
            {
                if(optionsDialog != null && optionsDialog.Enabled) optionsDialog.MinBookletSize=generationParameters.CurrentProblem+1;

                backup=CreateNewProblem(generationParameters.NewSudokuType());
                DisplayValues(problem.Matrix);
                if(GenerateBaseProblem())
                    StartDetachedProcess(DisplayGeneratingProcess, Resources.Generating, 2, false);
                else
                    GenerationAborted();
            }
            else
            {
                status.Text=String.Format(cultureInfo, Resources.NewProblems, generationParameters.CurrentProblem);
                PrintBooklet();
                PublishTrickyProblems();
                ResetTexts();
                ResetDetachedProcess();
                problem.Dirty=false;
                generationParameters=new GenerationParameters();
            }
            CancelSolving();
        }

        private void NextTry()
        {
            if(!problem.Aborted)
            {
                if(GenerateBaseProblem())
                    StartDetachedProcess(DisplayGeneratingProcess, Resources.Generating, 2, false);
                else
                    GenerationAborted();
            }
            else
                GenerationAborted();
        }

        private async void DisplayGeneratingProcess()
        {
            // Prüfen auf activeSolverTask statt problem.SolverTask
            if(activeSolver != null && !activeSolver.IsCompleted)
            {
                if(debug.Checked) SudokuTable.Update();
                GenerationStatus();
                try { await activeSolver.Solve; } catch { }
            }
            if(activeSolver == null)
                return;

            problem=activeSolver.Problem;

            solutionTimer.Stop();
            solutionTimer.Dispose();

            // Zugriff auf Eigenschaften von activeSolver
            generationParameters.Reset = (activeSolver.NumSolutions == 0);
            generationParameters.TotalPasses += activeSolver.TotalPassCount;

            if(activeSolver.NumSolutions == 1 && !activeSolver.Aborted)
            {
                Boolean processProblem = true;
                if(Settings.Default.GenerateMinimalProblems)
                {
                    if(SeverityLevelInt(problem) <= severityLevel) processProblem = await MinimizeAsync(severityLevel);
                }
                else
                    FillCells();

                if(processProblem && (SeverityLevelInt(problem) & severityLevel) != 0)
                {
                    problem.ResetMatrix();
                    if(processProblem && (SeverityLevelInt(problem) & severityLevel) != 0)
                    {
                        if(problem.IsTricky && !usePrecalculatedProblem) trickyProblems.Add(problem);

                        if(!generationParameters.GenerateBooklet)
                            GenerationSingleProblemFinished();
                        else
                            GenerationBookletProblemFinished();
                        status.Update();
                    }
                    else
                        NextTry();
                }
                else
                    NextTry();
            }
            else
                NextTry();
        }

        private void DisplayCheckingProcess()
        {
            // Prüfen auf activeSolverTask statt problem.SolverTask
            if(activeSolver != null && !activeSolver.IsCompleted)
            {
                if(debug.Checked) SudokuTable.Update();

                // Status vom activeSolver lesen
                long totalPasses = activeSolver != null ? activeSolver.TotalPassCount : 0;

                status.Text =
                    String.Format(cultureInfo, Resources.CheckingStatus, totalPasses) + Environment.NewLine +
                    Resources.TimeElapsed + (DateTime.Now - computingStart).ToString();
                status.Update();
            }
            else
            {
                ResetDetachedProcess();
                status.Text = String.Empty;

                // Prüfen auf activeSolver.Aborted
                if(activeSolver != null && !activeSolver.Aborted)
                {
                    // Prüfen auf activeSolver.ProblemSolved
                    Boolean problemSolved = activeSolver.ProblemSolved;

                    problem = backup.Clone();
                    DisplayValues(problem.Matrix);
                    hideValues = false;
                    MessageBox.Show(String.Format(cultureInfo, Resources.CheckResult, problem is XSudokuProblem ? "X-" : Resources.Classic, problemSolved ? Resources.AtLeast : Resources.No), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    hideValues = true;
                }
            }
        }

        private void DisplaySolvingProcess()
        {
            // Prüfen auf activeSolverTask statt problem.SolverTask
            if(activeSolver != null && !activeSolver.IsCompleted)
            {
                if(debug.Checked) SudokuTable.Update();

                // Status vom activeSolver lesen
                long totalPasses = activeSolver.TotalPassCount;
                int solutionsSoFar = problem.Solutions.Count;

                status.Text =
                    (findallSolutions.Checked ? String.Format(cultureInfo, Resources.SolutionsSoFar, solutionsSoFar) + Environment.NewLine : String.Empty) +
                    String.Format(cultureInfo, Resources.CheckingStatus, totalPasses) +
                    Environment.NewLine +
                    Resources.TimeElapsed + (DateTime.Now - computingStart).ToString();
                status.Update();
            }
            else
            {
                // Task ist beendet
                if(activeSolver != null && activeSolver.ProblemSolved)
                {
                    status.Text = Resources.ProblemSolved + Environment.NewLine + Resources.NeededTime + (DateTime.Now - computingStart).ToString() + (findallSolutions.Checked ? Environment.NewLine + Resources.TotalNumberOfSolutions + problem.Solutions.Count.ToString("n0", cultureInfo) : String.Empty) + Environment.NewLine + Resources.NeededPasses + activeSolver.TotalPassCount.ToString("n0", cultureInfo);
                    currentSolution = -1;
                    NextSolution();
                }
                else if(activeSolver != null && (activeSolver.Aborted || solverCts.IsCancellationRequested))
                {
                    if(problem.Solutions.Count > 0)
                    {
                        status.Text = String.Format(cultureInfo, Resources.SolutionsFound, (activeSolver.TotalPassCount > 0 ? Resources.Plural : String.Empty)) + Environment.NewLine + Resources.NeededTime + (DateTime.Now - computingStart).ToString() + (findallSolutions.Checked ? Environment.NewLine + Resources.TotalNumberOfSolutionsSoFar + problem.Solutions.Count.ToString("n0", cultureInfo) : String.Empty) + Environment.NewLine + Resources.NeededPasses + activeSolver.TotalPassCount.ToString("n0", cultureInfo);
                        currentSolution = -1;
                        NextSolution();
                    }
                    else
                        status.Text = String.Format(cultureInfo, Resources.Interrupt.Replace("\\n", Environment.NewLine), DateTime.Now - computingStart, activeSolver.TotalPassCount);
                }
                else
                {
                    long passes = activeSolver != null ? activeSolver.TotalPassCount : 0;
                    status.Text = Resources.NotResolvable + Environment.NewLine + Resources.NeededTime + (DateTime.Now - computingStart).ToString() + Environment.NewLine + Resources.NeededPasses + passes.ToString("n0", cultureInfo);
                }

                ResetDetachedProcess();
            }
        }
        // Menu handling
        private void EnableGUI(Boolean enable)
        {
            const String disableTag="disable";
            int disableTagLength=disableTag.Length;
            String menuTag=String.Empty;

            for(int i=0; i < sudokuMenu.Items.Count; i++)
            {
                ToolStripMenuItem mi=(ToolStripMenuItem)sudokuMenu.Items[i];
                if(mi.HasDropDownItems)
                    for(int j=0; j < mi.DropDownItems.Count; j++)
                    {
                        if(mi.DropDownItems[j] is ToolStripMenuItem)
                        {
                            ToolStripMenuItem ddm=(ToolStripMenuItem)mi.DropDownItems[j];
                            if(mi.DropDownItems[j].Tag != null)
                            {
                                menuTag=mi.DropDownItems[j].Tag.ToString();
                                if(!String.IsNullOrEmpty(menuTag) && menuTag.StartsWith(disableTag))
                                    mi.DropDownItems[j].Enabled=((menuTag.Substring(disableTagLength+1, 1) == "1") == enable);
                            }
                            if(ddm.HasDropDownItems)
                                for(int k=0; k < ddm.DropDownItems.Count; k++)
                                {
                                    if(ddm.DropDownItems[k].Tag != null)
                                    {
                                        menuTag=ddm.DropDownItems[k].Tag.ToString();
                                        if(!String.IsNullOrEmpty(menuTag) && menuTag.StartsWith(disableTag))
                                            ddm.DropDownItems[k].Enabled=((menuTag.Substring(disableTagLength+1, 1) == "1") == enable);
                                    }
                                }
                        }
                    }
            }
            // special handling for the "Undo", the "Clear Candidates", and the "Reset Timer" menu items
            undo.Enabled=undoStack.Count > 0 && enable;
            resetTimer.Enabled=solvingTimer.Enabled && enable;
            clearCandidates.Enabled=problem.HasCandidates() && enable;

            if(SudokuTable.Enabled=enable)
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
            hideValues=false;
            new AboutSudoku().ShowDialog();
            hideValues=true;
        }

        static private int GetSeverity()
        {
            if(Settings.Default.SelectSeverity)
            {
                SeverityLevelDialog severityLevelDialog=new SeverityLevelDialog();

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
            problem.ResetCandidates();
            clearCandidates.Enabled=false;
            SudokuTable.Refresh();
        }

        private void NewSudokuClick(object sender, EventArgs e)
        {
            if(UnsavedChanges()) problem=CreateNewProblem(false);
        }

        private void NewXSudokuClick(object sender, EventArgs e)
        {
            if(UnsavedChanges()) problem=CreateNewProblem(true);
        }

        private void SolveClick(object sender, EventArgs e)
        {
            SolveProblem();
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
            optionsDialog=new OptionsDialog();
            optionsDialog.MinBookletSize=(generationParameters.GenerateBooklet? Math.Max(generationParameters.CurrentProblem+1, 2): 2);

            hideValues=false;
            if(optionsDialog.ShowDialog() == DialogResult.OK)
            {
                Thread.CurrentThread.CurrentUICulture=(cultureInfo=new System.Globalization.CultureInfo(Settings.Default.DisplayLanguage));
                ShowInTaskbar=!Settings.Default.HideWhenMinimized;
                usePrecalculatedProblem=Settings.Default.UsePrecalculatedProblems;

                if(generationParameters.GenerateBooklet) severityLevel=Settings.Default.SeverityLevel;

                int colorIndex=255-(int)(255f*((float)Settings.Default.Contrast / 100f));
                gray=Color.FromArgb(colorIndex, colorIndex, colorIndex);
                green=Color.FromArgb(64, colorIndex, 64);
                colorIndex=255-(int)(255f*((float)Settings.Default.Contrast / 220f));
                lightGray=Color.FromArgb(colorIndex, colorIndex, colorIndex);
                colorIndex=255-(int)(255f*((float)Settings.Default.Contrast / 1000f));
                lightGreen=Color.FromArgb(191, colorIndex, 191);
                textColor=Settings.Default.Contrast > 50? Color.White: Color.Black;
                normalDisplayFont=new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size-1]), FontStyle.Regular);
                boldDisplayFont=new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size-1]), FontStyle.Bold);
                autoPauseTimer.Interval= Convert.ToInt32(Settings.Default.AutoPauseLag)*1000;

                UpdateGUI();
            }
            hideValues=true;
        }

        private void EditCommentClicked(object sender, EventArgs e)
        {
            String oldComment=String.Empty;
            Comment commentDialog=new Comment();

            oldComment=commentDialog.SudokuComment=problem.Comment;
            if(commentDialog.ShowDialog() == DialogResult.OK)
            {
                problem.Comment=commentDialog.SudokuComment;
                problem.Dirty=problem.Comment != oldComment;
            }
        }

        private void AbortClick(object sender, EventArgs e)
        {
            lock(this) CancelSolving(true);
        }

        private void PrintClick(object sender, EventArgs e)
        {
            PrintDialog();
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
            if(undoStack.Count < 1)
                throw (new ApplicationException());

            CoreValue cv=undoStack.Pop();
            if(!SudokuTable[cv.Col, cv.Row].ReadOnly)
            {
                SudokuTable[cv.Col, cv.Row].Value=cv.UnformatedValue;
                SudokuTable.Update();
                CurrentStatus(true);
                SudokuTable[cv.Col, cv.Row].Selected=true;

                undo.Enabled=undoStack.Count > 0;
                problem.Dirty=undo.Enabled;
            }
            else
                undoStack.Push(cv);
        }

        private void DebugClick(object sender, EventArgs e)
        {
            Settings.Default.Debug=debug.Checked;
        }

        private void FindallSolutionsClick(object sender, EventArgs e)
        {
            Settings.Default.FindAllSolutions=findallSolutions.Checked;
        }

        private void ShowPossibleValuesClick(object sender, EventArgs e)
        {
            Settings.Default.ShowHints=showPossibleValues.Checked;
            UpdateGUI();
        }

        private void AutoCheckClick(object sender, EventArgs e)
        {
            Settings.Default.AutoCheck=autoCheck.Checked;
        }

        private void VisitHomepageClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Resources.Homepage);
        }

        private void ResetTimerClick(object sender, EventArgs e)
        {
            problem.SolvingTime=TimeSpan.Zero;
            solvingTimer.Stop();
            sudokuStatusBarText.Text=Resources.Ready;
        }

        private void PauseClick(object sender, EventArgs e)
        {
            DateTime pauseStart=DateTime.Now;

            solvingTimer.Stop();
            problem.SolvingTime=DateTime.Now-interactiveStart;
            sudokuStatusBarText.Text+=Resources.Paused;
            autoPauseTimer.Stop();
            HideCells();
            hideValues=false;
            MessageBox.Show("Click to continue");
            hideValues=true;
            solvingTimer.Start();
            ShowValues();
            interactiveStart+=(DateTime.Now-pauseStart);
        }

        private void HideCells()
        {
            int row, col;

            for(row=0; row < SudokuSize; row++)
                for(col=0; col < SudokuSize; col++)
                    SudokuTable[row, col].Value="";
            valuesVisible=false;
        }

        private void MarkNeighborsClicked(object sender, EventArgs e)
        {
            Settings.Default.MarkNeighbors=markNeighbors.Checked;
        }

        private async void MinimizeClick(object sender, EventArgs e)
        {
            int before = problem.nValues;
            Boolean dirty = problem.Dirty;

            if(!SyncProblemWithGUI(true))
            {
                hideValues = false;
                MessageBox.Show(Resources.MinimizationNotPossible);
                hideValues = true;
                return;
            }

            await MinimizeAsync(int.MaxValue);

            hideValues = false;
            if(before - problem.nValues == 0)
            {
                MessageBox.Show(Resources.NoMinimizationPossible, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                problem.Dirty = dirty;
            }
            else
            {
                MessageBox.Show(String.Format(Resources.Minimized, (before - problem.nValues).ToString()), ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                problem.Dirty = true;
            }
            hideValues = true;
        }

        private void FixClick(object sender, EventArgs e)
        {
            SetReadOnly(true);
        }

        private void ReleaseClick(object sender, EventArgs e)
        {
            SetReadOnly(false);
        }

        private void VersionHistoryClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Resources.VersionHistory);
        }

        private void OptionsMenuOpening(object sender, EventArgs e)
        {
            pause.Enabled=resetTimer.Enabled=solvingTimer.Enabled;
        }

        private void SudokuOfTheDayClicked(object sender, EventArgs e)
        {
            hideValues=false;
            if(SudokuOfTheDay())
                MessageBox.Show(String.Format(Resources.SudokuOfTheDayInfo, problem.SeverityLevelText));
            else
                MessageBox.Show(Resources.SudokuOfTheDayNotLoaded);
            hideValues=true;
        }

        // Exit Sudoku
        private void ExitSudoku(object sender, FormClosingEventArgs e)
        {
            CancelSolving();

            if(e.CloseReason != CloseReason.TaskManagerClosing && e.CloseReason != CloseReason.WindowsShutDown)
            {
                if(Settings.Default.AutoSaveState)
                {
                    if(solvingTimer.Enabled) problem.SolvingTime=DateTime.Now-interactiveStart;
                    Settings.Default.State=problem.Serialize();
                    Settings.Default.Save();
                }
                else
                {
                    Settings.Default.State="";
                    Settings.Default.Save();
                    if(!SyncProblemWithGUI(true))
                    {
                        hideValues=false;
                        e.Cancel=MessageBox.Show(Resources.CloseAnyway, ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.No;
                        hideValues=true;
                    }
                    else
                        e.Cancel=!UnsavedChanges();
                }
            }
            applicationExiting=!e.Cancel;
        }
    }
}