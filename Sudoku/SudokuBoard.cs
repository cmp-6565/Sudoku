#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Sudoku.Core;

namespace Sudoku;

/// <summary>
/// Represents the interactive Sudoku grid control used by the WinForms UI.
/// Handles cell rendering, validation, hint display, undo support, and board synchronization
/// with the underlying <see cref="SudokuController"/> state.
/// </summary>
internal class SudokuBoard : DataGridView, IDisposable
{
    private ISudokuSettings settings = default!;
    private IUserInteraction ui = default!;
    private SudokuController controller = default!;
    private bool debugMode = false;
    private bool mouseWheelEditing = false;

    /// <summary>
    /// Gets a value indicating whether the board currently matches the underlying puzzle state.
    /// </summary>
    public bool InSync { get; private set; } = true;

    /// <summary>
    /// Occurs when the availability of undo operations changes.
    /// </summary>
    public event EventHandler<bool>? UndoAvailableChanged;

    /// <summary>
    /// Occurs when the availability of candidate data changes.
    /// </summary>
    public event EventHandler<bool>? CandidatesAvailableChanged;

    /// <summary>
    /// Occurs when the board status should be refreshed.
    /// </summary>
    public event EventHandler<bool>? UpdateStatus;

    /// <summary>
    /// Occurs when the hint visualization state changes.
    /// </summary>
    public event EventHandler<bool>? UpdateHints;

    /// <summary>
    /// Occurs when the status text displayed by the UI should be updated.
    /// </summary>
    public event EventHandler<string>? StatusTextChanged;

    private ContextMenuStrip? cellContextMenu;
    private Color highlightColor = Color.Cyan;
    private List<Point> highlightedCells = new List<Point>();
    private Font? normalDisplayFont;
    private Font? boldDisplayFont;
    private Font? strikethroughFont;
    private Font? hintFontSmall;
    private Font? hintFontNormal;
    private string[]? fontSizes;
    private bool valuesVisible = true;

    private Color gray;
    private Color lightGray;
    private Color green;
    private Color lightGreen;
    private Color textColor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SudokuBoard"/> class.
    /// </summary>
    public SudokuBoard()
    {
    }

    /// <summary>
    /// Initializes the board with the application settings and UI interaction layer.
    /// </summary>
    /// <param name="settings">The Sudoku settings used by the board.</param>
    /// <param name="ui">The UI abstraction used for notifications and dialogs.</param>
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
        StandardTab = true;

        DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 210, 255);
        DefaultCellStyle.SelectionForeColor = Color.Black;

        MouseWheel += MouseWheelHandler;
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

    /// <summary>
    /// Releases resources used by the board and its associated fonts.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the controller responsible for the current Sudoku problem state.
    /// </summary>
    internal SudokuController Controller
    {
        get => controller!;
        set
        {
            if (controller != null)
            {
                controller.MatrixChanged -= OnMatrixChanged;
                if (controller.CurrentProblem != null)
                {
                    controller.MinimizedFailed -= OnMinimizedFailed;
                    controller.CurrentProblem.SolutionFound -= OnSolutionFound;
                    controller.CurrentProblem.Matrix.CellChanged -= OnCellChanged;
                }
            }

            controller = value;

            if (controller != null)
            {
                controller.MatrixChanged += OnMatrixChanged;
                if (controller.CurrentProblem != null)
                {
                    controller.MinimizedFailed += OnMinimizedFailed;
                    controller.CurrentProblem.SolutionFound += OnSolutionFound;
                    DisplayValues(controller.CurrentProblem.Matrix);
                    if (debugMode)
                        controller.CurrentProblem.Matrix.CellChanged += OnCellChanged;
                }
            }
        }
    }

    /// <summary>
    /// Handles a failed minimization attempt by resetting the board and refreshing the current view.
    /// </summary>
    /// <param name="s">The event source or related context object.</param>
    private void OnMinimizedFailed(object s)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<object>(OnMinimizedFailed), s);
            return;
        }

        ResetMatrix();
        DisplayValues();
    }

    /// <summary>
    /// Refreshes the board display whenever the underlying puzzle matrix changes.
    /// </summary>
    /// <param name="s">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void OnMatrixChanged(object? s, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new EventHandler(OnMatrixChanged), s, e);
            return;
        }

        if (Controller?.CurrentProblem != null)
        {
            DisplayValues(Controller.CurrentProblem.Matrix);
        }

        FormatBoard();
        Refresh();
    }

    /// <summary>
    /// Updates the visual value of a single cell when the model reports a cell change.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="v">The changed cell data.</param>
    private void OnCellChanged(object? sender, BaseCell v)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() =>
            {
                DisplayValue(v.Row, v.Col, v.CellValue);
                Update();
            }));

            var tf = settings?.TraceFrequency ?? 0;
            if (tf > 0)
            {
                try { Thread.Sleep(tf); }
                catch { }
            }

            return;
        }

        DisplayValue(v.Row, v.Col, v.CellValue);
        Update();
    }

    /// <summary>
    /// Re-renders the grid after a solution has been found.
    /// </summary>
    /// <param name="s">The event source.</param>
    /// <param name="e">The event arguments.</param>
    private void OnSolutionFound(object? s, EventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new EventHandler(OnSolutionFound), s, e);
            return;
        }

        if (Controller?.CurrentProblem != null)
        {
            DisplayValues(Controller.CurrentProblem.Solutions[Controller.CurrentProblem.NumberOfSolutions - 1]);
        }

        Refresh();
    }

    /// <summary>
    /// Publishes the current hint-visibility status to subscribers.
    /// </summary>
    /// <param name="showHints">A value indicating whether hints are visible.</param>
    private void OnUpdateHints(bool showHints)
    {
        UpdateHints?.Invoke(this, showHints);
    }

    /// <summary>
    /// Publishes a status text update to the UI.
    /// </summary>
    /// <param name="text">The status text to display.</param>
    private void OnStatusTextChanged(string text)
    {
        StatusTextChanged?.Invoke(this, text);
    }

    /// <summary>
    /// Registers input validation for the active cell editor.
    /// </summary>
    private void InitializeInputValidation()
    {
        EditingControlShowing += CellEditingControl;
    }

    /// <summary>
    /// Subscribes to the grid events used by the board logic.
    /// </summary>
    private void InitializeEvents()
    {
        CellBeginEdit += new DataGridViewCellCancelEventHandler(HandleBeginEdit);
        CellEndEdit += new DataGridViewCellEventHandler(HandleEndEdit);
        CellEnter += new DataGridViewCellEventHandler(HandleCellEnter);
        CellLeave += new DataGridViewCellEventHandler(HandleCellLeave);
        Paint += new PaintEventHandler(ShowCellHints);
        KeyDown += new KeyEventHandler(HandleSpecialChar);
    }

    /// <summary>
    /// Restricts the active cell editor to valid Sudoku input characters.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The editing-control event arguments.</param>
    private void CellEditingControl(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (e.Control is TextBox textBox)
        {
            textBox.KeyPress -= CellKeyPressValidation;
            textBox.KeyPress += CellKeyPressValidation;
        }
    }

    /// <summary>
    /// Rejects invalid key presses during cell editing.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The key press event arguments.</param>
    private void CellKeyPressValidation(object? sender, KeyPressEventArgs e)
    {
        var isValidDigit = char.IsDigit(e.KeyChar) && e.KeyChar != '0';
        var isControl = char.IsControl(e.KeyChar);

        if (!isValidDigit && !isControl)
        {
            e.Handled = true;
            System.Media.SystemSounds.Beep.Play();
        }
    }

    /// <summary>
    /// Creates the context menu used for clearing cells and clearing candidates.
    /// </summary>
    private void InitializeCellContextMenu()
    {
        cellContextMenu = new ContextMenuStrip();

        var itemClear = cellContextMenu.Items.Add(Resources.ClearContent);
        itemClear.Enabled = true;
        itemClear.Click += (s, e) =>
        {
            if (CurrentCell != null && !CurrentCell.ReadOnly)
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
            if (Controller?.CurrentProblem != null && CurrentCell != null)
            {
                Controller.CurrentProblem.ResetCandidates(CurrentCell.RowIndex, CurrentCell.ColumnIndex);
                CandidatesAvailableChanged?.Invoke(this, Controller.CurrentProblem.HasCandidates());
                Refresh();
            }
        };

        ContextMenuStrip = cellContextMenu;
        CellMouseDown += HandleCellMouseDown;
    }

    /// <summary>
    /// Handles right-click actions on a board cell.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void HandleCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
        {
            HandleRightMouseButton(e.RowIndex, e.ColumnIndex);
        }
    }

    /// <summary>
    /// Handles mouse-wheel value changes for the currently selected cell.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The mouse event arguments.</param>
    private void MouseWheelHandler(object? sender, MouseEventArgs e)
    {
        if (sender is DataGridView)
        {
            if (EditingControl == null && CurrentCell != null && !CurrentCell.ReadOnly)
            {
                if (!mouseWheelEditing) PushOnUndoStack(this);

                try
                {
                    int currentValue = (CurrentCell.Value == null || ((string)CurrentCell.Value).Trim().Length == 0
                        ? 0
                        : Convert.ToInt32(CurrentCell.Value));
                    currentValue += Math.Sign(e.Delta);

                    if (currentValue > 0 && currentValue <= WinFormsSettings.SudokuSize)
                        CurrentCell.Value = currentValue.ToString();
                    else if (currentValue == Values.Undefined)
                        CurrentCell.Value = "";
                    else
                        System.Media.SystemSounds.Hand.Play();

                    mouseWheelEditing = true;
                }
                catch (FormatException) { }
            }
        }
    }

    /// <summary>
    /// Formats a single board cell using the active puzzle and visual style.
    /// </summary>
    /// <param name="row">The row index of the cell.</param>
    /// <param name="col">The column index of the cell.</param>
    /// <param name="clearHighlight">A value indicating whether the highlight should be cleared.</param>
    public void FormatCell(int row, int col, bool clearHighlight = false)
    {
        if (!clearHighlight && this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.BackColor == highlightColor) return;
        bool xSudoku = (Controller?.CurrentProblem) is XSudokuProblem;

        bool obfuscated = ((row / 3) % 2 == 1 && (col / 3) % 2 == 0) || ((row / 3) % 2 == 0 && (col / 3) % 2 == 1);
        this[row, col].Style.BackColor = (obfuscated ? gray : ((xSudoku && (row == col || row + col == WinFormsSettings.SudokuSize - 1)) ? lightGray : Color.White));
        this[row, col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
        this[row, col].Style.SelectionBackColor = SystemColors.AppWorkspace;
    }

    /// <summary>
    /// Highlights the neighboring cells of the currently selected cell.
    /// </summary>
    public void MarkNeighbors()
    {
        BaseCell[] neighbors = Controller?.GetNeighbors(CurrentCellAddress.X, CurrentCellAddress.Y) ?? Array.Empty<BaseCell>();
        bool obfuscated;

        if (this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.BackColor == highlightColor) return;

        obfuscated = ((CurrentCellAddress.X / 3) % 2 == 1 && (CurrentCellAddress.Y / 3) % 2 == 0) || ((CurrentCellAddress.X / 3) % 2 == 0 && (CurrentCellAddress.Y / 3) % 2 == 1);
        this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.BackColor = (obfuscated ? green : lightGreen);
        this[CurrentCellAddress.X, CurrentCellAddress.Y].Style.SelectionBackColor = (obfuscated ? Color.DarkGreen : Color.SeaGreen);

        foreach (BaseCell cell in neighbors)
        {
            obfuscated = ((cell.Row / 3) % 2 == 1 && (cell.Col / 3) % 2 == 0) || ((cell.Row / 3) % 2 == 0 && (cell.Col / 3) % 2 == 1);
            this[cell.Row, cell.Col].Style.BackColor = (obfuscated ? green : lightGreen);
            this[cell.Row, cell.Col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
        }
    }

    /// <summary>
    /// Saves the current cell state to the undo stack before a change is applied.
    /// </summary>
    /// <param name="dgv">The grid instance from which the undo snapshot should be created.</param>
    public void PushOnUndoStack(DataGridView dgv)
    {
        CoreValue cv = new CoreValue();
        if (CurrentCell == null) return;

        cv.Row = CurrentCell.RowIndex;
        cv.Col = CurrentCell.ColumnIndex;

        if (CurrentCell.Value != null)
            cv.UnformatedValue = (string)CurrentCell.Value;

        Controller?.PushUndo(cv);
        UndoAvailableChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Resizes the board to match the current Sudoku configuration and DPI settings.
    /// </summary>
    /// <returns>The board height after resizing.</returns>
    public int ResizeBoard()
    {
        int width = 0;
        int height = 0;
        int cellSize = (int)((float)(settings?.Size ?? 1) * (settings?.MagnificationFactor ?? 1f) * (settings?.CellWidth ?? 10) * 0.7f * (float)this.DeviceDpi / 96f);

        for (int i = 0; i < WinFormsSettings.SudokuSize; i++)
        {
            width += (Columns[i].Width = cellSize);
            height += (Rows[i].Height = cellSize);
        }

        Width = width + 1;
        Height = height + 1;

        return height;
    }

    /// <summary>
    /// Sets the underlying model value for the specified cell.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    /// <param name="value">The value to set.</param>
    private void SetValue(int row, int col, byte value)
    {
        if (Controller?.CurrentProblem != null)
            Controller.CurrentProblem.SetValue(row, col, value);
    }

    /// <summary>
    /// Clears all values from the visual board while preserving the configured formatting.
    /// </summary>
    public void ResetMatrix()
    {
        for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                this[col, row].Style.Font = normalDisplayFont;
                this[col, row].Value = string.Empty;
                this[col, row].ErrorText = string.Empty;
                FormatCell(row, col, true);
            }

        ClearHighlights();
        ClearErrorMessages();
    }

    /// <summary>
    /// Gets a value indicating whether the puzzle has been filled completely.
    /// </summary>
    public bool IsCompleted => FilledCells == WinFormsSettings.TotalCellCount;

    /// <summary>
    /// Displays the provided values in the board grid.
    /// </summary>
    /// <param name="values">The matrix to render, or null to use the current controller state.</param>
    public void DisplayValues(Values? values = null)
    {
        if (values == null)
        {
            values = Controller?.CurrentProblem?.Matrix;
        }

        if (values == null) return;

        for (int i = 0; i < WinFormsSettings.SudokuSize; i++)
            for (int j = 0; j < WinFormsSettings.SudokuSize; j++)
                DisplayValue(i, j, values.GetValue(i, j));
    }

    /// <summary>
    /// Displays a single cell value and applies the appropriate font based on its read-only state.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    /// <param name="value">The value to display.</param>
    public void DisplayValue(int row, int col, byte value)
    {
        this[col, row].Value = (value == Values.Undefined ? " " : value.ToString());
        SetCellFont(row, col);
    }

    /// <summary>
    /// Applies the current formatting and font settings to all cells.
    /// </summary>
    public void SetCellFont()
    {
        for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
                SetCellFont(row, col);
    }

    /// <summary>
    /// Applies the correct font and read-only state to a specific cell.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    public void SetCellFont(int row, int col)
    {
        if (Controller == null) return;
        this[col, row].Style.Font = Controller.IsCellReadOnly(row, col) ? (boldDisplayFont ?? DefaultCellStyle.Font) : (normalDisplayFont ?? DefaultCellStyle.Font);
        this[col, row].ReadOnly = Controller.IsCellReadOnly(row, col);
    }

    /// <summary>
    /// Synchronizes the model state from the current grid contents.
    /// </summary>
    /// <param name="silent">A value indicating whether validation messages should be suppressed.</param>
    /// <param name="autocheck">A value indicating whether the board should display validation errors.</param>
    /// <returns><c>true</c> if the current grid is valid; otherwise, <c>false</c>.</returns>
    public bool SyncProblemWithGUI(bool silent, bool autocheck)
    {
        EndEdit();
        mouseWheelEditing = false;

        string[,] grid = new string[WinFormsSettings.SudokuSize, WinFormsSettings.SudokuSize];
        for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                this[col, row].ErrorText = string.Empty;
                grid[row, col] = this[col, row].Value as string ?? string.Empty;
            }

        ValidationResult result = Controller!.ParseAndSync(grid);
        InSync = result.IsValid;

        if (autocheck)
        {
            foreach (var error in result.Errors)
            {
                this[error.Col, error.Row].ErrorText = error.Message;
            }

            if (!silent && !result.IsValid)
            {
                ui?.ShowInfo(result.Errors[0].Message);
            }
        }

        if (settings?.ShowHints == true) Refresh();
        return result.IsValid;
    }

    /// <summary>
    /// Applies board formatting and visual styles for the current challenge state.
    /// </summary>
    /// <param name="newProblem">A value indicating whether the board should be reset before formatting.</param>
    public void FormatBoard(bool newProblem = false)
    {
        if (newProblem) ResetMatrix();
        SetCellFont();

        for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
                FormatCell(row, col);

        if (settings.MarkNeighbors)
            MarkNeighbors();
    }

    /// <summary>
    /// Counts the number of non-empty cells currently displayed on the board.
    /// </summary>
    public int FilledCells
    {
        get
        {
            int count = 0;

            for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
                for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
                    if ((this[col, row].Value?.ToString()?.Trim() ?? string.Empty).Length > 0)
                        count++;

            return count;
        }
    }

    /// <summary>
    /// Sets the read-only state for all cells that currently contain a value.
    /// </summary>
    /// <param name="readOnly">A value indicating whether the board should be locked.</param>
    public void SetReadOnly(bool readOnly)
    {
        for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
                Controller!.SetCellReadOnly(row, col, (readOnly && ((this[col, row].Value?.ToString()?.Trim() ?? string.Empty) != string.Empty)));

        DisplayValues();
    }

    /// <summary>
    /// Handles the begin-edit phase and pushes the previous cell state to the undo stack.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The cell event arguments.</param>
    private void HandleBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (sender is DataGridView)
            if (!((DataGridView)sender).CurrentCell.ReadOnly)
                PushOnUndoStack((DataGridView)sender);
    }

    /// <summary>
    /// Handles the end-edit phase for a cell value update.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The cell event arguments.</param>
    private void HandleEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        HandleCellEndEdit(sender);
    }

    /// <summary>
    /// Finalizes a cell edit and refreshes the visual and model state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    private void HandleCellEndEdit(object? sender)
    {
        SetValue(CurrentCell!.RowIndex, CurrentCell.ColumnIndex, Values.Undefined);
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

    /// <summary>
    /// Handles delete and context-menu key actions for a cell.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The key event arguments.</param>
    public void HandleDeleteAndMenuKeys(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
        {
            if (!this[CurrentCell!.ColumnIndex, CurrentCell.RowIndex].ReadOnly)
            {
                PushOnUndoStack(this);
                this[CurrentCell.ColumnIndex, CurrentCell.RowIndex].Value = "";
                HandleCellEndEdit(sender);
            }
        }
        else
        {
            HandleRightMouseButton(CurrentCell!.RowIndex, CurrentCell.ColumnIndex);
        }
    }

    /// <summary>
    /// Processes special key input such as delete, context menu, and candidate operations.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The key event arguments.</param>
    private void HandleSpecialChar(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Apps || e.KeyCode == Keys.Back)
        {
            HandleDeleteAndMenuKeys(sender, e);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (!e.Modifiers.HasFlag(Keys.Shift) && !e.Modifiers.HasFlag(Keys.Control)) return;

        int value = 0;
        if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) value = e.KeyCode - Keys.D0;
        else if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) value = e.KeyCode - Keys.NumPad0;
        else if (e.Modifiers.HasFlag(Keys.Control) && !ShiftNumPadMap.TryGetValue(e.KeyCode, out value)) value = 0;

        if (value == 0) return;

        if (CurrentCell?.ReadOnly == true) return;

        ProcessCandidate(Math.Abs(value), e.Modifiers.HasFlag(Keys.Shift) || value < 0);
        e.Handled = true;
        e.SuppressKeyPress = true;

        Refresh();
    }

    /// <summary>
    /// Adds or removes a candidate value for the current cell.
    /// </summary>
    /// <param name="value">The candidate value.</param>
    /// <param name="shiftMode">A value indicating whether candidate mode is active.</param>
    private void ProcessCandidate(int value, bool shiftMode)
    {
        Controller!.CurrentProblem.SetCandidate(CurrentCell!.RowIndex, CurrentCell.ColumnIndex, (byte)value, shiftMode);
        CandidatesAvailableChanged?.Invoke(this, Controller.CurrentProblem.HasCandidates());
        InvalidateCell(CurrentCell.ColumnIndex, CurrentCell.RowIndex);
    }

    /// <summary>
    /// Handles cell leave events and optionally refreshes neighbor highlighting.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The cell event arguments.</param>
    private void HandleCellLeave(object? sender, DataGridViewCellEventArgs e)
    {
        if (mouseWheelEditing) HandleCellEndEdit(sender);

        if (settings?.MarkNeighbors == true) FormatBoard();
    }

    /// <summary>
    /// Handles cell enter events and refreshes same-value highlighting and hint visibility.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The cell event arguments.</param>
    private void HandleCellEnter(object? sender, DataGridViewCellEventArgs e)
    {
        if (settings?.HighlightSameValues == true) UpdateHighligts();
        if (settings?.MarkNeighbors == true) FormatBoard();
        ShowValues();
    }

    /// <summary>
    /// Applies a visual strike-through to cells that fail validation during testing.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="cell">The cell that failed validation.</param>
    internal void HandleOnTestCell(object? sender, BaseCell cell)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<object, BaseCell>(HandleOnTestCell), sender, cell);
            return;
        }

        this[cell.Col, cell.Row].Style.Font = strikethroughFont;
        this[cell.Col, cell.Row].Style.BackColor = Color.Coral;
    }

    /// <summary>
    /// Resets the visual state of a cell after a validation test completes.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="cell">The cell whose visuals should be reset.</param>
    internal void ResetCellVisuals(object? sender, BaseCell cell)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<object, BaseCell>(ResetCellVisuals), sender, cell);
            return;
        }

        this[cell.Col, cell.Row].Style.Font = boldDisplayFont;
        FormatCell(cell.Col, cell.Row);
    }

    /// <summary>
    /// Clears all validation error messages from the board.
    /// </summary>
    public void ClearErrorMessages()
    {
        for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
                this[col, row].ErrorText = string.Empty;
    }

    /// <summary>
    /// Highlights all cells with the same value as the currently selected cell.
    /// </summary>
    public void UpdateHighligts()
    {
        ClearHighlights();

        if (CurrentCell == null || CurrentCell.Value == null || string.IsNullOrWhiteSpace(CurrentCell.Value.ToString())) return;

        highlightedCells = GetSameValueCells(CurrentCell.Value);

        foreach (Point p in highlightedCells)
            this[p.X, p.Y].Style.BackColor = highlightColor;
    }

    /// <summary>
    /// Clears all active highlights from the board.
    /// </summary>
    public void ClearHighlights()
    {
        foreach (Point p in highlightedCells)
            FormatCell(p.X, p.Y, true);

        highlightedCells.Clear();
    }

    /// <summary>
    /// Finds all cells whose displayed value matches the specified value.
    /// </summary>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A list of matching cell coordinates.</returns>
    private List<Point> GetSameValueCells(object value)
    {
        List<Point> cells = new List<Point>();
        for (int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (int col = 0; col < WinFormsSettings.SudokuSize; col++)
                if (this[col, row].Value != null && this[col, row].Value.Equals(value))
                    cells.Add(new Point(col, row));

        return cells;
    }

    /// <summary>
    /// Hides all cell values and keeps the board in a hidden-value state.
    /// </summary>
    public void HideCells()
    {
        int row, col;

        for (row = 0; row < WinFormsSettings.SudokuSize; row++)
            for (col = 0; col < WinFormsSettings.SudokuSize; col++)
                this[row, col].Value = "";

        valuesVisible = false;
    }

    /// <summary>
    /// Restores the displayed values when they were temporarily hidden.
    /// </summary>
    public void ShowValues()
    {
        if (!valuesVisible)
        {
            DisplayValues(Controller!.CurrentProblem.Matrix);
            valuesVisible = true;
        }
    }

    /// <summary>
    /// Renders candidate hints or watch-hand hints into the visible board area.
    /// </summary>
    /// <param name="sender">The source of the paint event.</param>
    /// <param name="e">The paint event arguments.</param>
    private void ShowCellHints(object? sender, PaintEventArgs e)
    {
        var currentProblem = Controller?.CurrentProblem;
        if (sender is not DataGridView || currentProblem == null) return;

        bool showCandidatesMode = !(settings?.ShowHints ?? false);
        if (showCandidatesMode && !currentProblem.HasCandidates()) return;

        EnsureHintFonts();
        Font hintFont = (settings?.Size ?? 1) == 1 ? (hintFontSmall ?? DefaultCellStyle.Font) : (hintFontNormal ?? DefaultCellStyle.Font);

        float cellSize = Columns[0].Width;
        Rectangle clip = e.ClipRectangle;

        int startRow = Math.Max(0, (int)(clip.Top / cellSize));
        int endRow = Math.Min(WinFormsSettings.SudokuSize, (int)(clip.Bottom / cellSize) + 1);
        int startCol = Math.Max(0, (int)(clip.Left / cellSize));
        int endCol = Math.Min(WinFormsSettings.SudokuSize, (int)(clip.Right / cellSize) + 1);

        for (int row = startRow; row < endRow; row++)
        {
            for (int col = startCol; col < endCol; col++)
            {
                if (currentProblem.GetValue(row, col) != Values.Undefined) continue;
                if (showCandidatesMode && !currentProblem.HasCandidate(row, col)) continue;

                RectangleF cellBounds = new RectangleF(col * cellSize, row * cellSize, cellSize, cellSize);
                BaseCell cell = currentProblem.Cell(row, col);

                if (settings?.UseWatchHandHints == true)
                    SudokuRenderer.DrawWatchHands(cell, cellBounds, e.Graphics, showCandidatesMode);
                else
                    SudokuRenderer.DrawHints(cell, cellBounds, e.Graphics, hintFont, this[col, row].Style.ForeColor, showCandidatesMode);
            }
        }
    }

    /// <summary>
    /// Enables or disables the context menu for the current cell.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    private void HandleRightMouseButton(int row, int col)
    {
        if (cellContextMenu != null)
        {
            var val = CurrentCell?.Value?.ToString() ?? string.Empty;
            cellContextMenu.Items[0].Enabled = val.Trim().Length != 0;
        }

        if (cellContextMenu != null)
            cellContextMenu.Items[1].Enabled = Controller!.CurrentProblem.HasCandidate(row, col);
    }

    /// <summary>
    /// Creates a new Sudoku problem and resets the visual board state.
    /// </summary>
    /// <param name="xSudoku">A value indicating whether an X-Sudoku should be created.</param>
    public void CreateNewProblem(bool xSudoku)
    {
        Controller!.CreateNewProblem(xSudoku);
        ResetMatrix();
        SetDebugMode(debugMode);
        InSync = true;
    }

    /// <summary>
    /// Clears the undo stack and notifies listeners that no undo operations are available.
    /// </summary>
    public void ResetUndo()
    {
        Controller!.ClearUndo();
        UndoAvailableChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Updates the font and color settings used to render the board.
    /// </summary>
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

    /// <summary>
    /// Ensures the hint fonts are initialized before drawing candidate hints.
    /// </summary>
    private void EnsureHintFonts()
    {
        if (hintFontSmall != null && hintFontNormal != null) return;

        var printParameters = new PrintParameters(settings);
        hintFontSmall ??= (Font)printParameters.SmallFont.Clone();
        hintFontNormal ??= (Font)printParameters.NormalFont.Clone();
    }

    /// <summary>
    /// Updates the visual representation of a generation progress state.
    /// </summary>
    /// <param name="state">The current generation state.</param>
    public void UpdateProblemState(GenerationProgressState state)
    {
        if (state.Value != Values.Undefined)
            DisplayValue(state.Row, state.Col, state.Value);
        else
            this[state.Col, state.Row].Value = "";

        if (state.ReadOnly) SetCellFont(state.Row, state.Col);
    }

    /// <summary>
    /// Animates a hint cell by briefly changing its background color.
    /// </summary>
    /// <param name="row">The row index of the cell.</param>
    /// <param name="col">The column index of the cell.</param>
    /// <param name="isSingle">A value indicating whether the hint is a single candidate.</param>
    public async Task AnimateHint(int row, int col, bool isSingle)
    {
        Color originalColor = this[col, row].Style.BackColor;
        this[col, row].Style.BackColor = isSingle ? Color.Red : Color.Orange;

        Refresh();

        await Task.Delay(500);

        this[col, row].Style.BackColor = originalColor;
    }

    /// <summary>
    /// Enables or disables debug mode for the board and attaches the cell-change listener accordingly.
    /// </summary>
    /// <param name="debug">A value indicating whether debug mode should be enabled.</param>
    public void SetDebugMode(bool debug)
    {
        debugMode = debug;
        if (debug)
            controller.CurrentProblem.Matrix.CellChanged += OnCellChanged;
        else
            controller.CurrentProblem.Matrix.CellChanged -= OnCellChanged;
    }

    /// <summary>
    /// Animates the hint cells in the currently selected set.
    /// </summary>
    /// <param name="hints">The hints to visualize.</param>
    public async Task VisualizeHints(List<BaseCell> hints)
    {
        var selectedPositions = new List<Point>();
        foreach (DataGridViewCell cell in SelectedCells)
            selectedPositions.Add(new Point(cell.ColumnIndex, cell.RowIndex));

        ClearSelection();

        foreach (var hint in hints)
        {
            await AnimateHint(hint.Row, hint.Col, hint.nPossibleValues == 1);
        }

        foreach (var pos in selectedPositions)
            this[pos.X, pos.Y].Selected = true;

        Update();
    }
}