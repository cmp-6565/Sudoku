using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    internal class SudokuBoard: DataGridView
    {
        public int RectSize;
        public int SudokuSize;
        public int TotalCellCount;

        private SudokuController controller;

        private Boolean mouseWheelEditing = false;
        public Boolean InSync { get; private set; }

        public event EventHandler<Boolean> UndoAvailableChanged;
        public event EventHandler<Boolean> CandidatesAvailableChanged;
        public event EventHandler<Boolean> UpdateStatus;
        public event EventHandler<Boolean> UpdateHints;
        public event EventHandler<string> StatusTextChanged;

        // Kontextmenü Variable
        private ContextMenuStrip cellContextMenu;

        private Color highlightColor = Color.Cyan;
        private List<Point> highlightedCells = new List<Point>();

        private Font normalDisplayFont;
        private Font boldDisplayFont;
        private Font strikethroughFont;
        private String[] fontSizes;
        private Boolean valuesVisible = true;

        Color gray;
        Color lightGray;
        Color green;
        Color lightGreen;
        Color textColor;

        public SudokuBoard() { }

        internal void Initialize(int rectSize)
        {
            RectSize = rectSize;
            SudokuSize = RectSize * RectSize;
            TotalCellCount = SudokuSize * SudokuSize;

            DoubleBuffered = true;

            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeColumns = false;
            AllowUserToResizeRows = false;

            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.Sunken;
            BackgroundColor = Color.White;
            GridColor = Color.Gainsboro;

            ColumnHeadersVisible = false;
            RowHeadersVisible = false;

            MultiSelect = false;
            SelectionMode = DataGridViewSelectionMode.CellSelect;
            StandardTab = true; // Tab springt zur nächsten Zelle

            DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 210, 255);
            DefaultCellStyle.SelectionForeColor = Color.Black;

            MouseWheel += new MouseEventHandler(MouseWheelHandler);
            Rows.Add(SudokuSize);

            UpdateFonts();

            ResetMatrix();
            DisplayValues();

            InitializeCellContextMenu();
            InitializeEvents();
        }

        internal SudokuController Controller
        {
            get { return controller; }
            set
            {
                if(controller != null)
                {
                    controller.MatrixChanged -= OnMatrixChanged;
                    controller.SolutionFound -= OnSolutionFound;
                }

                controller = value;

                if(controller != null)
                {
                    controller.MatrixChanged += OnMatrixChanged;
                    controller.SolutionFound += OnSolutionFound;

                    if(controller.CurrentProblem != null) DisplayValues(controller.CurrentProblem.Matrix);
                }
            }
        }

        private void OnMatrixChanged(object s, EventArgs e)
        {
            if(InvokeRequired)
            {
                Invoke(new EventHandler(OnMatrixChanged), s, e);
                return;
            }

            if(Controller.CurrentProblem != null)
            {
                DisplayValues(Controller.CurrentProblem.Matrix);
            }

            Refresh();
        }

        private void OnSolutionFound(object s, EventArgs e)
        {
            if(InvokeRequired)
            {
                Invoke(new EventHandler(OnSolutionFound), s, e);
                return;
            }
            Refresh();
        }

        private void InitializeEvents()
        {
            CellBeginEdit += new DataGridViewCellCancelEventHandler(HandleBeginEdit);
            CellEndEdit += new DataGridViewCellEventHandler(HandleEndEdit);
            CellEnter += new DataGridViewCellEventHandler(HandleCellEnter);
            CellLeave += new DataGridViewCellEventHandler(HandleCellLeave);
            Paint += new PaintEventHandler(ShowCellHints);
            KeyDown += new KeyEventHandler(HandleSpecialChar);
        }

        private void InitializeCellContextMenu()
        {
            cellContextMenu = new ContextMenuStrip();

            var itemClear = cellContextMenu.Items.Add(Resources.ClearContent);
            itemClear.Enabled = true;
            itemClear.Click += (s, e) =>
            {
                if(CurrentCell != null && !CurrentCell.ReadOnly)
                {
                    PushOnUndoStack(this);
                    CurrentCell.Value = "";
                    HandleCellEndEdit(this);
                }
            };

            var itemCandidate = cellContextMenu.Items.Add(Resources.ClearCandidates);
            itemCandidate.Enabled = true;
            itemCandidate.Click += (s, e) =>
            {
                if(Controller.CurrentProblem != null)
                {
                    Controller.CurrentProblem.ResetCandidates(CurrentCell.RowIndex, CurrentCell.ColumnIndex);
                    CandidatesAvailableChanged?.Invoke(this, Controller.CurrentProblem.HasCandidates());
                    Refresh();
                }
            };

            ContextMenuStrip = cellContextMenu;
            CellMouseDown += HandleCellMouseDown;
        }

        private void HandleCellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                HandleRightMouseButton(e.RowIndex, e.ColumnIndex);
            }
        }

        private void MouseWheelHandler(object sender, MouseEventArgs e)
        {
            if(sender is DataGridView)
            {
                if(EditingControl == null && !CurrentCell.ReadOnly)
                {
                    if(!mouseWheelEditing) PushOnUndoStack(this);

                    try
                    {
                        int currentValue = (CurrentCell.Value == null || ((String)CurrentCell.Value).Trim().Length == 0 ? 0 : Convert.ToInt32(CurrentCell.Value));
                        currentValue += Math.Sign(e.Delta);
                        if(currentValue > 0 && currentValue <= SudokuSize)
                            CurrentCell.Value = currentValue.ToString();
                        else if(currentValue == Values.Undefined)
                            CurrentCell.Value = "";
                        else
                            System.Media.SystemSounds.Hand.Play();
                        mouseWheelEditing = true;
                    }
                    catch(FormatException) { }
                }
            }
        }

        public void FormatCell(int row, int col)
        {
            if(this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.BackColor == highlightColor) return;

            Boolean obfuscated = ((row / 3) % 2 == 1 && (col / 3) % 2 == 0) || ((row / 3) % 2 == 0 && (col / 3) % 2 == 1);
            this[row, col].Style.BackColor = (obfuscated ? gray : ((Controller.CurrentProblem is XSudokuProblem) && (row == col || row + col == SudokuSize - 1) ? lightGray : Color.White));
            this[row, col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
            this[row, col].Style.SelectionBackColor = SystemColors.AppWorkspace;
        }

        public void MarkNeighbors()
        {
            BaseCell[] neighbors = Controller.GetNeighbors(CurrentCellAddress.X, CurrentCellAddress.Y);
            Boolean obfuscated;

            if(this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.BackColor == highlightColor) return;

            obfuscated = ((CurrentCellAddress.X / 3) % 2 == 1 && (CurrentCellAddress.Y / 3) % 2 == 0) || ((CurrentCellAddress.X / 3) % 2 == 0 && (CurrentCellAddress.Y / 3) % 2 == 1);
            this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.BackColor = (obfuscated ? green : lightGreen);
            this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.SelectionBackColor = (obfuscated ? Color.DarkGreen : Color.SeaGreen);
            foreach(BaseCell cell in neighbors)
            {
                obfuscated = ((cell.Row / 3) % 2 == 1 && (cell.Col / 3) % 2 == 0) || ((cell.Row / 3) % 2 == 0 && (cell.Col / 3) % 2 == 1);
                this[cell.Row, cell.Col].Style.BackColor = (obfuscated ? green : lightGreen);
                this[cell.Row, cell.Col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
            }
        }

        public void PushOnUndoStack(DataGridView dgv)
        {
            CoreValue cv = new CoreValue();
            cv.Row = CurrentCell.RowIndex;
            cv.Col = CurrentCell.ColumnIndex;
            if(CurrentCell.Value != null)
                cv.UnformatedValue = (String)CurrentCell.Value;
            Controller.PushUndo(cv);
            UndoAvailableChanged?.Invoke(this, true);
        }

        public int ResizeBoard()
        {
            int width = 0;
            int height = 0;
            int cellSize = (int)((float)Settings.Default.Size * Settings.Default.MagnificationFactor * Settings.Default.CellWidth * .7f);

            for(int i = 0; i < SudokuSize; i++)
            {
                width += (Columns[i].Width = cellSize);
                height += (Rows[i].Height = cellSize);
            }

            Width = width + 1;
            Height = height + 1;

            return height;
        }

        private void SetValue(int row, int col, byte value)
        {
            Controller.CurrentProblem.SetValue(row, col, value);
        }

        public void ResetMatrix()
        {
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                {
                    this[col, row].Style.Font = normalDisplayFont;
                    this[col, row].Value = String.Empty;
                    this[col, row].ErrorText = String.Empty;
                }
        }

        public void DisplayValues(Values values = null)
        {
            if(values == null) values = Controller?.CurrentProblem?.Matrix;
            if(values == null) return;

            for(int i = 0; i < SudokuForm.SudokuSize; i++)
                for(int j = 0; j < SudokuForm.SudokuSize; j++)
                    DisplayValue(i, j, values.GetValue(i, j));
        }

        public void DisplayValue(int row, int col, byte value)
        {
            this[col, row].Value = (value == Values.Undefined ? " " : value.ToString());
            SetCellFont(row, col);
        }

        public void SetCellFont()
        {
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    SetCellFont(row, col);
        }

        public void SetCellFont(int row, int col)
        {
            if(Controller == null) return;
            this[col, row].Style.Font = Controller.IsCellReadOnly(row, col) ? boldDisplayFont : normalDisplayFont;
            this[col, row].ReadOnly = Controller.IsCellReadOnly(row, col);
        }

        public Boolean SyncProblemWithGUI(Boolean silent, Boolean autocheck)
        {
            EndEdit();
            mouseWheelEditing = false;

            string[,] grid = new string[SudokuForm.SudokuSize, SudokuForm.SudokuSize];
            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                {
                    this[col, row].ErrorText = String.Empty;
                    grid[row, col] = this[col, row].Value as string;
                }

            ValidationResult result = Controller.ParseAndSync(grid);
            InSync = result.IsValid;

            if(autocheck)
            {
                foreach(var error in result.Errors)
                {
                    this[error.Col, error.Row].ErrorText = error.Message;
                }

                if(!silent && !result.IsValid)
                {
                    MessageBox.Show(result.Errors[0].Message, Resources.SudokuError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            if(Settings.Default.ShowHints) Refresh();
            return result.IsValid;
        }

        public void FormatBoard()
        {
            SetCellFont();
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    FormatCell(row, col);
            if(Settings.Default.MarkNeighbors)
                MarkNeighbors();
        }

        public int FilledCells
        {
            get
            {
                int count = 0;

                for(int row = 0; row < SudokuSize; row++)
                    for(int col = 0; col < SudokuSize; col++)
                        if(this[col, row].Value != null && (((string)this[col, row].Value).Trim()).Length > 0)
                            count++;
                return count;
            }
        }

        public void SetReadOnly(Boolean readOnly)
        {
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    Controller.SetCellReadOnly(row, col, (readOnly && this[col, row].Value.ToString().Trim() != String.Empty));
            DisplayValues();
        }

        private void HandleBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if(sender is DataGridView)
                if(!((DataGridView)sender).CurrentCell.ReadOnly)
                    PushOnUndoStack((DataGridView)sender);
        }

        private void HandleEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            HandleCellEndEdit(sender);
        }

        private void HandleCellEndEdit(object sender)
        {
            if(sender is DataGridView)
            {
                SetValue(CurrentCell.RowIndex, CurrentCell.ColumnIndex, Values.Undefined);
                SetCellFont(CurrentCell.RowIndex, CurrentCell.ColumnIndex);
            }
            mouseWheelEditing = false;

            UpdateStatus?.Invoke(this, false);
        }

        private void HandleSpecialChar(object sender, KeyEventArgs e)
        {
            if(sender is DataGridView)
            {
                if(e.KeyCode == Keys.Delete || e.KeyCode == Keys.Apps || e.KeyCode == Keys.Back)
                {
                    if(e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                    {
                        if(!this[CurrentCell.ColumnIndex, CurrentCell.RowIndex].ReadOnly)
                        {
                            PushOnUndoStack(this);
                            this[CurrentCell.ColumnIndex, CurrentCell.RowIndex].Value = "";
                            HandleCellEndEdit(sender);
                        }
                    }
                    else
                    {
                        HandleRightMouseButton(CurrentCell.RowIndex, CurrentCell.ColumnIndex);
                    }
                }
                else
                {
                    // if(!pencilMode.Checked || e.Control || e.Alt) return; // TODO: pencilMode prüfen

                    int value = -1;
                    if(e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) value = e.KeyCode - Keys.D0;
                    else if(e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) value = e.KeyCode - Keys.NumPad0;

                    if(value > 0)
                    {
                        DataGridViewCell current = CurrentCell;
                        if(current != null && !current.ReadOnly)
                        {
                            Controller.CurrentProblem.SetCandidate(CurrentCell.RowIndex, CurrentCell.ColumnIndex, value, false);
                            CandidatesAvailableChanged?.Invoke(this, Controller.CurrentProblem.HasCandidates());

                            e.Handled = true;
                            e.SuppressKeyPress = true;

                            InvalidateCell(current.ColumnIndex, current.RowIndex);
                            Refresh();
                        }
                    }
                }
            }
        }

        private void HandleCellLeave(object sender, DataGridViewCellEventArgs e)
        {
            if(mouseWheelEditing) HandleCellEndEdit(sender);

            if(Settings.Default.MarkNeighbors) FormatBoard();
        }

        private void HandleCellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if(Settings.Default.HighlightSameValues) UpdateHighligts();
            if(Settings.Default.MarkNeighbors) FormatBoard();
            ShowValues();
        }

        internal void HandleOnTestCell(object sender, BaseCell cell)
        {
            if(InvokeRequired)
            {
                Invoke(new Action<object, BaseCell>(HandleOnTestCell), sender, cell);
                return;
            }
            this[cell.Col, cell.Row].Style.Font = strikethroughFont;
            this[cell.Col, cell.Row].Style.BackColor = Color.Coral;
        }

        internal void HandleOnResetCell(object sender, BaseCell cell)
        {
            if(InvokeRequired)
            {
                Invoke(new Action<object, BaseCell>(HandleOnResetCell), sender, cell);
                return;
            }
            this[cell.Col, cell.Row].Style.Font = boldDisplayFont;
            FormatCell(cell.Col, cell.Row);
        }

        public void UpdateHighligts()
        {
            ClearHighlights();

            if(CurrentCell == null || CurrentCell.Value == null || String.IsNullOrWhiteSpace(CurrentCell.Value.ToString())) return;

            highlightedCells = GetSameValueCells(CurrentCell.Value);

            foreach(Point p in highlightedCells)
                this[p.X, p.Y].Style.BackColor = highlightColor;
        }

        public void ClearHighlights()
        {
            foreach(Point p in highlightedCells)
                FormatCell(p.X, p.Y);
            highlightedCells.Clear();
        }

        private List<Point> GetSameValueCells(object value)
        {
            List<Point> cells = new List<Point>();
            for(int row = 0; row < SudokuSize; row++)
                for(int col = 0; col < SudokuSize; col++)
                    if(this[col, row].Value != null && this[col, row].Value.Equals(value))
                        cells.Add(new Point(col, row));
            return cells;
        }

        public void HideCells()
        {
            int row, col;

            for(row = 0; row < SudokuSize; row++)
                for(col = 0; col < SudokuSize; col++)
                    this[row, col].Value = "";
            valuesVisible = false;
        }
        public void ShowValues()
        {
            if(!valuesVisible)
            {
                DisplayValues(Controller.CurrentProblem.Matrix);
                valuesVisible = true;
            }
        }

        private void ShowCellHints(object sender, PaintEventArgs e)
        {
            if(sender is DataGridView && Controller?.CurrentProblem != null)
            {
                Font printFont = (Settings.Default.Size == 1 ? PrintParameters.SmallFont : PrintParameters.NormalFont);
                bool showCandidatesMode = !Settings.Default.ShowHints;

                if(showCandidatesMode && !Controller.CurrentProblem.HasCandidates()) return;

                float cellSize = Columns[0].Width;

                int startRow = Math.Max(0, (int)(e.ClipRectangle.Top / cellSize));
                int endRow = Math.Min(SudokuSize, (int)(e.ClipRectangle.Bottom / cellSize) + 1);
                int startCol = Math.Max(0, (int)(e.ClipRectangle.Left / cellSize));
                int endCol = Math.Min(SudokuSize, (int)(e.ClipRectangle.Right / cellSize) + 1);

                for(int row = startRow; row < endRow; row++)
                {
                    for(int col = startCol; col < endCol; col++)
                    {
                        if(Controller.CurrentProblem.GetValue(row, col) == Values.Undefined && (!showCandidatesMode || Controller.CurrentProblem.HasCandidate(row, col)))
                        {
                            RectangleF rf = new RectangleF(col * cellSize, row * cellSize, cellSize, cellSize);

                            if(Settings.Default.UseWatchHandHints)
                                SudokuRenderer.DrawWatchHands(Controller.CurrentProblem.Cell(row, col), rf, e.Graphics, showCandidatesMode);
                            else
                                SudokuRenderer.DrawHints(Controller.CurrentProblem.Cell(row, col), rf, e.Graphics, printFont, this[col, row].Style.ForeColor, showCandidatesMode);
                        }
                    }
                }
            }
        }
        private void HandleRightMouseButton(int row, int col)
        {
            cellContextMenu.Items[0].Enabled = CurrentCell.Value.ToString().Trim().Length != 0;
            cellContextMenu.Items[1].Enabled = Controller.CurrentProblem.HasCandidate(row, col);
        }

        public void CreateNewProblem(Boolean xSudoku)
        {
            Controller.CreateNewProblem(xSudoku);
            Controller.BackupProblem();
            InSync = true;
        }

        public void ResetUndo()
        {
            Controller.ClearUndo();
            UndoAvailableChanged?.Invoke(this, false);
        }
        public void UpdateFonts()
        {
            int colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 100f));
            gray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
            green = Color.FromArgb(64, colorIndex, 64);
            colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 220f));
            lightGray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
            colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 1000f));
            lightGreen = Color.FromArgb(191, colorIndex, 191);

            fontSizes = Settings.Default.FontSizes.Split('|');
            normalDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Regular);
            boldDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Bold);
            strikethroughFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Bold | FontStyle.Strikeout);

            textColor = Color.FromArgb(255-colorIndex, 255-colorIndex, 255-colorIndex);
        }
        public void UpdateProblemState(GenerationProgressState state)
        {
            if(state.Value != Values.Undefined)
                DisplayValue(state.Row, state.Col, state.Value);
            else
                this[state.Col, state.Row].Value = "";

            if(state.ReadOnly) SetCellFont(state.Row, state.Col);
        }
        public async Task AnimateHint(int row, int col, bool isSingle)
        {
            Color originalColor = this[col, row].Style.BackColor;
            this[col, row].Style.BackColor = isSingle ? Color.Red : Color.Orange;

            Refresh();

            await Task.Delay(500);

            this[col, row].Style.BackColor = originalColor;
        }
    }
}