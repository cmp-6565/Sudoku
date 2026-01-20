using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

using Sudoku.Properties;

using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Sudoku.ValidationResult;

namespace Sudoku
{
    public class SudokuBoard : DataGridView
    {
        public const int RectSize = 3;
        public const int SudokuSize = RectSize * RectSize;
        public const int TotalCellCount = SudokuSize * SudokuSize;

        private SudokuController controller;

        private Boolean mouseWheelEditing = false;
        private Boolean inSync = false;

        private Color highlightColor = Color.Cyan;
        private List<Point> highlightedCells = new List<Point>();

        private Font normalDisplayFont;
        private Font boldDisplayFont;
        private Font strikethroughFont;
        private String[] fontSizes;

        Color gray;
        Color lightGray;
        Color green;
        Color lightGreen;
        Color textColor;

        public SudokuBoard()
        {
            DoubleBuffered = true;

            // Grundlegende UI-Einstellungen (ausgelagert aus dem Form-Designer/Constructor)
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

            int colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 100f));
            gray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
            green = Color.FromArgb(127, colorIndex, 127);
            colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 220f));
            lightGray = Color.FromArgb(colorIndex, colorIndex, colorIndex);
            colorIndex = 255 - (int)(255f * ((float)Settings.Default.Contrast / 1000f));
            lightGreen = Color.FromArgb(200, colorIndex, 200);
            textColor = Settings.Default.Contrast > 50 ? Color.White : Color.Black;

            fontSizes = Settings.Default.FontSizes.Split('|');
            normalDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Regular);
            boldDisplayFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Bold);
            strikethroughFont = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]), FontStyle.Bold | FontStyle.Strikeout);

            InitializeController();
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

            Refresh();
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
            Refresh();
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
                        int currentValue = (dgv.CurrentCell.Value == null || ((String)dgv.CurrentCell.Value).Trim().Length == 0 ? 0 : Convert.ToInt32(dgv.CurrentCell.Value));
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
        public void FormatCell(int row, int col)
        {
            Boolean obfuscated = ((row / 3) % 2 == 1 && (col / 3) % 2 == 0) || ((row / 3) % 2 == 0 && (col / 3) % 2 == 1);
            this[row, col].Style.BackColor = (obfuscated ? gray : ((controller.CurrentProblem is XSudokuProblem) && (row == col || row + col == SudokuSize - 1) ? lightGray : Color.White));
            this[row, col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
            this[row, col].Style.SelectionBackColor = System.Drawing.SystemColors.AppWorkspace;
        }
        public void MarkNeighbors(DataGridView dgv)
        {
            BaseCell[] neighbors = controller.GetNeighbors(dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y);
            Boolean obfuscated;

            obfuscated = ((dgv.CurrentCellAddress.X / 3) % 2 == 1 && (dgv.CurrentCellAddress.Y / 3) % 2 == 0) || ((dgv.CurrentCellAddress.X / 3) % 2 == 0 && (dgv.CurrentCellAddress.Y / 3) % 2 == 1);
            this[dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y].Style.BackColor = (obfuscated ? green : lightGreen);
            this[dgv.CurrentCellAddress.X, dgv.CurrentCellAddress.Y].Style.SelectionBackColor = (obfuscated ? Color.DarkGreen : Color.SeaGreen);
            foreach(BaseCell cell in neighbors)
            {
                obfuscated = ((cell.Row / 3) % 2 == 1 && (cell.Col / 3) % 2 == 0) || ((cell.Row / 3) % 2 == 0 && (cell.Col / 3) % 2 == 1);
                this[cell.Row, cell.Col].Style.BackColor = (obfuscated ? green : lightGreen);
                this[cell.Row, cell.Col].Style.ForeColor = (obfuscated ? textColor : Color.Black);
            }
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

            Width = width + 30;
            Height = height + 140 + (60 * Settings.Default.Size);

            Width = width + 1;
            Height = height + 1;

            return height;
        }
        private void SetValue(int row, int col, byte value)
        {
            controller.CurrentProblem.SetValue(row, col, value);
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
        public void DisplayValues(Values values=null)
        {
            if(values == null) values=controller.CurrentProblem.Matrix;
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
            // status.Font = new Font(Settings.Default.TableFont, Convert.ToInt32(fontSizes[Settings.Default.Size - 1]) * .8f, FontStyle.Regular);
        }
        private void SetCellFont(int row, int col)
        {
            this[col, row].Style.Font = controller.IsCellReadOnly(row, col) ? boldDisplayFont : normalDisplayFont;
            this[col, row].ReadOnly = controller.IsCellReadOnly(row, col);
        }
        public Boolean SyncProblemWithGUI(Boolean silent, Boolean autocheck)
        {
            Text = ProductName;
            EndEdit();
            mouseWheelEditing = false;

            // Marshal UI grid to string[,] with minimal processing
            string[,] grid = new string[SudokuForm.SudokuSize, SudokuForm.SudokuSize];
            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                {
                    this[col, row].ErrorText = String.Empty;
                    grid[row, col] = this[col, row].Value as string;
                }

            ValidationResult result = controller.ParseAndSync(grid);
            inSync = result.IsValid;

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
            int row, col;

            SetCellFont();
            for(row = 0; row < SudokuSize; row++)
                for(col = 0; col < SudokuSize; col++)
                    FormatCell(row, col);
            if(Settings.Default.MarkNeighbors)
                MarkNeighbors(this);
        }


        // Hier können später Methoden wie FormatCell, MarkNeighbors etc. einziehen
    }
}