using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sudoku;

internal class SudokuBoard: DataGridView, IDisposable
{
    private ISudokuSettings settings;
    private IUserInteraction ui;

    private SudokuController controller;
    private Boolean debugMode = false;

    private Boolean mouseWheelEditing = false;
    public Boolean InSync { get; private set; } = true;

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
    private Font hintFontSmall;
    private Font hintFontNormal;
    private String[] fontSizes;
    private Boolean valuesVisible = true;

    Color gray;
    Color lightGray;
    Color green;
    Color lightGreen;
    Color textColor;
    public SudokuBoard() { }

    internal void Initialize(ISudokuSettings settings, IUserInteraction ui)
    {
        DoubleBuffered = true;
        ShowCellToolTips = false;

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
        Rows.Add(WinFormsSettings.SudokuSize);

        CurrentCell = this[0, 0];
        this.settings = settings;
        this.ui = ui;
        UpdateFonts();

        ResetMatrix();
        DisplayValues();

        InitializeCellContextMenu();
        InitializeInputValidation();
        InitializeEvents();
    }
    public new void Dispose()
    {
        base.Dispose();
        normalDisplayFont?.Dispose(); normalDisplayFont = null;
        boldDisplayFont?.Dispose(); boldDisplayFont = null;
        strikethroughFont?.Dispose(); strikethroughFont = null;
        hintFontSmall?.Dispose(); hintFontSmall = null;
        hintFontNormal?.Dispose(); hintFontNormal = null;
        cellContextMenu?.Dispose(); cellContextMenu = null;
    }
    internal SudokuController Controller
    {
        get { return controller; }
        set
        {
            if(controller != null)
            {
                controller.MatrixChanged -= OnMatrixChanged;
                if(controller.CurrentProblem != null)
                {
                    controller.MinimizedFailed -= OnMinimizedFailed;
                    controller.CurrentProblem.SolutionFound -= OnSolutionFound;
                    controller.CurrentProblem.Matrix.CellChanged -= OnCellChanged;
                }
            }

            controller = value;

            if(controller != null)
            {
                controller.MatrixChanged += OnMatrixChanged;
                if(controller.CurrentProblem != null)
                {
                    controller.MinimizedFailed += OnMinimizedFailed;
                    controller.CurrentProblem.SolutionFound += OnSolutionFound;
                    DisplayValues(controller.CurrentProblem.Matrix);
                    if(debugMode)
                        controller.CurrentProblem.Matrix.CellChanged += OnCellChanged;
                }
            }
        }
    }

    private void OnMinimizedFailed(object s)
    {
        if(InvokeRequired)
        {
            Invoke(new Action<object>(OnMinimizedFailed), s);
            return;
        }
        ResetMatrix();
        DisplayValues();
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
        FormatBoard();

        Refresh();
    }
    private void OnCellChanged(object sender, BaseCell v)
    {
        if(InvokeRequired)
        {
            Invoke(new Action(() =>
            {
                DisplayValue(v.Row, v.Col, v.CellValue);
                Update();
            }));

            if(settings.TraceFrequence > 0)
            {
                try { Thread.Sleep(settings.TraceFrequence); } catch { }
            }
            return;
        }

        DisplayValue(v.Row, v.Col, v.CellValue);
        Update();
    }

    private void OnSolutionFound(object s, EventArgs e)
    {
        if(InvokeRequired)
        {
            Invoke(new EventHandler(OnSolutionFound), s, e);
            return;
        }
        DisplayValues(Controller.CurrentProblem.Solutions[controller.CurrentProblem.NumberOfSolutions - 1]);
        Refresh();
    }

    private void InitializeInputValidation()
    {
        EditingControlShowing += CellEditingControl;
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

    private void CellEditingControl(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if(e.Control is TextBox textBox)
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
                    if(currentValue > 0 && currentValue <= WinFormsSettings.SudokuSize)
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
    public void FormatCell(int row, int col, Boolean clearHighlight = false)
    {
        if(!clearHighlight && this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.BackColor == highlightColor) return;

        Boolean xSudoku = (Controller != null && Controller.CurrentProblem is XSudokuProblem);

        Boolean obfuscated = ((row / 3) % 2 == 1 && (col / 3) % 2 == 0) || ((row / 3) % 2 == 0 && (col / 3) % 2 == 1);
        this[row, col].Style.BackColor = (obfuscated ? gray : ((xSudoku && (row == col || row + col == WinFormsSettings.SudokuSize - 1)) ? lightGray : Color.White));
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
        int cellSize = (int)((float)settings.Size * settings.MagnificationFactor * settings.CellWidth * .7f);

        for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
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
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                this[col, row].Style.Font = normalDisplayFont;
                this[col, row].Value = String.Empty;
                this[col, row].ErrorText = String.Empty;
                FormatCell(row, col, true);
            }
        ClearHighlights();
        ClearErrorMessages();
    }

    public Boolean IsCompleted { get { return FilledCells == WinFormsSettings.TotalCellCount; } }
    public void DisplayValues(Values values = null)
    {
        if(values == null) values = Controller?.CurrentProblem?.Matrix;
        if(values == null) return;

        for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
            for(int j = 0; j < WinFormsSettings.SudokuSize; j++)
                DisplayValue(i, j, values.GetValue(i, j));
    }

    public void DisplayValue(int row, int col, byte value)
    {
        this[col, row].Value = (value == Values.Undefined ? " " : value.ToString());
        SetCellFont(row, col);
    }

    public void SetCellFont()
    {
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
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

        string[,] grid = new string[WinFormsSettings.SudokuSize, WinFormsSettings.SudokuSize];
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
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
                ui.ShowInfo(result.Errors[0].Message);
            }
        }
        if(settings.ShowHints) Refresh();
        return result.IsValid;
    }

    public void FormatBoard(Boolean newProblem = false)
    {
        if(newProblem) ResetMatrix();
        SetCellFont();
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                FormatCell(row, col);
        if(settings.MarkNeighbors)
            MarkNeighbors();
    }

    public int FilledCells
    {
        get
        {
            int count = 0;

            for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
                for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                    if(this[col, row].Value != null && (((string)this[col, row].Value).Trim()).Length > 0)
                        count++;
            return count;
        }
    }

    public void SetReadOnly(Boolean readOnly)
    {
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
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
        SetValue(CurrentCell.RowIndex, CurrentCell.ColumnIndex, Values.Undefined);
        SetCellFont(CurrentCell.RowIndex, CurrentCell.ColumnIndex);
        mouseWheelEditing = false;

        UpdateStatus?.Invoke(this, false);
    }
    private static readonly IReadOnlyDictionary<Keys, int> ShiftNumPadMap = new Dictionary<Keys, int>
    {
        { Keys.End, -1 }, { Keys.Down, -2 }, { Keys.PageDown, -3 },
        { Keys.Left, -4 }, { Keys.Clear, -5 }, { Keys.Right, -6 },
        { Keys.Home, -7 }, { Keys.Up, -8 }, { Keys.PageUp, -9 }
    };

    public void HandleDeleteAndMenuKeys(object sender, KeyEventArgs e)
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
    private void HandleSpecialChar(object sender, KeyEventArgs e)
    {
        if(e.KeyCode == Keys.Delete || e.KeyCode == Keys.Apps || e.KeyCode == Keys.Back)
        {
            HandleDeleteAndMenuKeys(sender, e);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if(!e.Modifiers.HasFlag(Keys.Shift) && !e.Modifiers.HasFlag(Keys.Control)) return;

        int value = 0;
        if(e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) value = e.KeyCode - Keys.D0;
        else if(e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) value = e.KeyCode - Keys.NumPad0;
        else if(e.Modifiers.HasFlag(Keys.Control) && !ShiftNumPadMap.TryGetValue(e.KeyCode, out value)) value=0;

        if(value == 0) return;

        if(CurrentCell?.ReadOnly == true) return;

        ProcessCandidate(Math.Abs(value), e.Modifiers.HasFlag(Keys.Shift) || value < 0); // that means it's a shift numpad key, so we set the candidate in shift mode
        e.Handled = true;
        e.SuppressKeyPress = true;

        Refresh();
    }
    private void ProcessCandidate(int value, bool shiftMode)
    {
        Controller.CurrentProblem.SetCandidate(CurrentCell.RowIndex, CurrentCell.ColumnIndex, (byte)value, shiftMode);
        CandidatesAvailableChanged?.Invoke(this, Controller.CurrentProblem.HasCandidates());
        InvalidateCell(CurrentCell.ColumnIndex, CurrentCell.RowIndex);
    }

    private void HandleCellLeave(object sender, DataGridViewCellEventArgs e)
    {
        if(mouseWheelEditing) HandleCellEndEdit(sender);

        if(settings.MarkNeighbors) FormatBoard();
    }

    private void HandleCellEnter(object sender, DataGridViewCellEventArgs e)
    {
        if(settings.HighlightSameValues) UpdateHighligts();
        if(settings.MarkNeighbors) FormatBoard();
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

    internal void ResetCellVisuals(object sender, BaseCell cell)
    {
        if(InvokeRequired)
        {
            Invoke(new Action<object, BaseCell>(ResetCellVisuals), sender, cell);
            return;
        }
        this[cell.Col, cell.Row].Style.Font = boldDisplayFont;
        FormatCell(cell.Col, cell.Row);
    }

    public void ClearErrorMessages()
    {
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                this[col, row].ErrorText = String.Empty;
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
            FormatCell(p.X, p.Y, true);
        highlightedCells.Clear();
    }

    private List<Point> GetSameValueCells(object value)
    {
        List<Point> cells = new List<Point>();
        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
                if(this[col, row].Value != null && this[col, row].Value.Equals(value))
                    cells.Add(new Point(col, row));
        return cells;
    }

    public void HideCells()
    {
        int row, col;

        for(row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(col = 0; col < WinFormsSettings.SudokuSize; col++)
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
        var currentProblem = Controller?.CurrentProblem;
        if(sender is not DataGridView || currentProblem == null) return;

        bool showCandidatesMode = !settings.ShowHints;
        if(showCandidatesMode && !currentProblem.HasCandidates()) return;

        EnsureHintFonts();
        Font hintFont = settings.Size == 1 ? hintFontSmall : hintFontNormal;

        float cellSize = Columns[0].Width;
        Rectangle clip = e.ClipRectangle;

        int startRow = Math.Max(0, (int)(clip.Top / cellSize));
        int endRow = Math.Min(WinFormsSettings.SudokuSize, (int)(clip.Bottom / cellSize) + 1);
        int startCol = Math.Max(0, (int)(clip.Left / cellSize));
        int endCol = Math.Min(WinFormsSettings.SudokuSize, (int)(clip.Right / cellSize) + 1);

        for(int row = startRow; row < endRow; row++)
        {
            for(int col = startCol; col < endCol; col++)
            {
                if(currentProblem.GetValue(row, col) != Values.Undefined) continue;
                if(showCandidatesMode && !currentProblem.HasCandidate(row, col)) continue;

                RectangleF cellBounds = new RectangleF(col * cellSize, row * cellSize, cellSize, cellSize);
                BaseCell cell = currentProblem.Cell(row, col);

                if(settings.UseWatchHandHints)
                    SudokuRenderer.DrawWatchHands(cell, cellBounds, e.Graphics, showCandidatesMode);
                else
                    SudokuRenderer.DrawHints(cell, cellBounds, e.Graphics, hintFont, this[col, row].Style.ForeColor, showCandidatesMode);
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
        ResetMatrix();
        SetDebugMode(debugMode);
        InSync = true;
    }

    public void ResetUndo()
    {
        Controller.ClearUndo();
        UndoAvailableChanged?.Invoke(this, false);
    }
    public void UpdateFonts()
    {
        int colorIndex = 255 - (int)(255f * ((float)settings.Contrast / 100f));
        gray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
        green = Color.FromArgb(64, colorIndex, 64);
        colorIndex = 255 - (int)(255f * ((float)settings.Contrast / 220f));
        lightGray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
        colorIndex = 255 - (int)(255f * ((float)settings.Contrast / 1000f));
        lightGreen = Color.FromArgb(191, colorIndex, 191);

        var printParameters = new PrintParameters(settings);

        fontSizes = settings.FontSizes.Split('|');
        normalDisplayFont?.Dispose();
        boldDisplayFont?.Dispose();
        strikethroughFont?.Dispose();
        normalDisplayFont = new Font(settings.TableFont, Convert.ToInt32(fontSizes[settings.Size - 1]), FontStyle.Regular);
        boldDisplayFont = new Font(settings.TableFont, Convert.ToInt32(fontSizes[settings.Size - 1]), FontStyle.Bold);
        strikethroughFont = new Font(settings.TableFont, Convert.ToInt32(fontSizes[settings.Size - 1]), FontStyle.Bold | FontStyle.Strikeout);

        hintFontSmall?.Dispose();
        hintFontNormal?.Dispose();
        hintFontSmall = (Font)printParameters.SmallFont.Clone();
        hintFontNormal = (Font)printParameters.NormalFont.Clone();

        textColor = Color.FromArgb(255 - colorIndex, 255 - colorIndex, 255 - colorIndex);
    }
    private void EnsureHintFonts()
    {
        if(hintFontSmall != null && hintFontNormal != null) return;

        var printParameters = new PrintParameters(settings);
        hintFontSmall ??= (Font)printParameters.SmallFont.Clone();
        hintFontNormal ??= (Font)printParameters.NormalFont.Clone();
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
    public void SetDebugMode(Boolean debug)
    {
        debugMode = debug;
        if(debug)
            controller.CurrentProblem.Matrix.CellChanged += OnCellChanged;
        else
            controller.CurrentProblem.Matrix.CellChanged -= OnCellChanged;
    }
    public async Task VisualizeHints(List<BaseCell> hints)
    {
        var selectedPositions = new List<Point>();
        foreach(DataGridViewCell cell in SelectedCells)
            selectedPositions.Add(new Point(cell.ColumnIndex, cell.RowIndex));

        ClearSelection();

        foreach(var hint in hints)
        {
            await AnimateHint(hint.Row, hint.Col, hint.nPossibleValues == 1);
        }

        foreach(var pos in selectedPositions)
            this[pos.X, pos.Y].Selected = true;

        this.Update();
    }
}