namespace Sudoku
{
    partial class SudokuForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components=null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support-do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SudokuForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            // ALT: this.SudokuTable = new System.Windows.Forms.DataGridView();
            this.SudokuTable = new SudokuBoard(); // NEU
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.status = new System.Windows.Forms.Label();
            this.next = new System.Windows.Forms.Button();
            this.prior = new System.Windows.Forms.Button();
            this.solutionTimer = new System.Windows.Forms.Timer(this.components);
            this.sudokuMenu = new System.Windows.Forms.MenuStrip();
            this.file = new System.Windows.Forms.ToolStripMenuItem();
            this.newItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newNormalSudoku = new System.Windows.Forms.ToolStripMenuItem();
            this.newXSudoku = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            this.sudokuOfTheDayToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.open = new System.Windows.Forms.ToolStripMenuItem();
            this.save = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.twitter = new System.Windows.Forms.ToolStripMenuItem();
            this.export = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.print = new System.Windows.Forms.ToolStripMenuItem();
            this.generateBooklet = new System.Windows.Forms.ToolStripMenuItem();
            this.newBooklet = new System.Windows.Forms.ToolStripMenuItem();
            this.existingBooklet = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.exit = new System.Windows.Forms.ToolStripMenuItem();
            this.sudokuProblem = new System.Windows.Forms.ToolStripMenuItem();
            this.generate = new System.Windows.Forms.ToolStripMenuItem();
            this.generateNormalSudoku = new System.Windows.Forms.ToolStripMenuItem();
            this.generateXSudoku = new System.Windows.Forms.ToolStripMenuItem();
            this.solve = new System.Windows.Forms.ToolStripMenuItem();
            this.mimimalAllocation = new System.Windows.Forms.ToolStripMenuItem();
            this.definiteValues = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.validate = new System.Windows.Forms.ToolStripMenuItem();
            this.check = new System.Windows.Forms.ToolStripMenuItem();
            this.info = new System.Windows.Forms.ToolStripMenuItem();
            this.EditComment = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            this.startSolveProblem = new System.Windows.Forms.ToolStripMenuItem();
            this.fixCurrentValues = new System.Windows.Forms.ToolStripMenuItem();
            this.releaseCurrentValues = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            this.reset = new System.Windows.Forms.ToolStripMenuItem();
            this.clearCandidates = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.abort = new System.Windows.Forms.ToolStripMenuItem();
            this.options = new System.Windows.Forms.ToolStripMenuItem();
            this.undo = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.debug = new System.Windows.Forms.ToolStripMenuItem();
            this.findallSolutions = new System.Windows.Forms.ToolStripMenuItem();
            this.showPossibleValues = new System.Windows.Forms.ToolStripMenuItem();
            this.autoCheck = new System.Windows.Forms.ToolStripMenuItem();
            this.pencilMode = new System.Windows.Forms.ToolStripMenuItem();
            this.highlightSameValues = new System.Windows.Forms.ToolStripMenuItem();
            this.markNeighbors = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            this.pause = new System.Windows.Forms.ToolStripMenuItem();
            this.resetTimer = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.sudokuOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.help = new System.Windows.Forms.ToolStripMenuItem();
            this.showCellInfo = new System.Windows.Forms.ToolStripMenuItem();
            this.showHints = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.visitHomepage = new System.Windows.Forms.ToolStripMenuItem();
            this.versionHistory = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutSudoku = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSudokuDialog = new System.Windows.Forms.SaveFileDialog();
            this.openSudokuDialog = new System.Windows.Forms.OpenFileDialog();
            this.sudokuStatusBar = new System.Windows.Forms.StatusStrip();
            this.sudokuStatusBarText = new System.Windows.Forms.ToolStripStatusLabel();
            this.printSudokuDialog = new System.Windows.Forms.PrintDialog();
            this.printSudoku = new System.Drawing.Printing.PrintDocument();
            this.selectBookletDirectory = new System.Windows.Forms.FolderBrowserDialog();
            this.solvingTimer = new System.Windows.Forms.Timer(this.components);
            this.autoPauseTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.SudokuTable)).BeginInit();
            this.sudokuMenu.SuspendLayout();
            this.sudokuStatusBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // SudokuTable
            // 
            this.SudokuTable.AllowUserToAddRows = false;
            this.SudokuTable.AllowUserToDeleteRows = false;
            this.SudokuTable.AllowUserToResizeColumns = false;
            this.SudokuTable.AllowUserToResizeRows = false;
            this.SudokuTable.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.SudokuTable.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.SudokuTable.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            resources.ApplyResources(this.SudokuTable, "SudokuTable");
            this.SudokuTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.SudokuTable.ColumnHeadersVisible = false;
            this.SudokuTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2,
            this.Column3,
            this.Column4,
            this.Column5,
            this.Column6,
            this.Column7,
            this.Column8,
            this.Column9});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Comic Sans MS", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle2.NullValue = null;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.ButtonShadow;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.SudokuTable.DefaultCellStyle = dataGridViewCellStyle2;
            this.SudokuTable.MultiSelect = false;
            this.SudokuTable.Name = "SudokuTable";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.SudokuTable.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.SudokuTable.RowHeadersVisible = false;
            this.SudokuTable.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            dataGridViewCellStyle4.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.SudokuTable.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.SudokuTable.RowTemplate.Height = 30;
            this.SudokuTable.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.SudokuTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.SudokuTable.StandardTab = true;
            this.SudokuTable.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.BeginEdit);
            this.SudokuTable.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.EndEdit);
            this.SudokuTable.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.CellEnter);
            this.SudokuTable.CellLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.CellLeave);
            this.SudokuTable.Paint += new System.Windows.Forms.PaintEventHandler(this.ShowCellHints);
            this.SudokuTable.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HandleSpecialChar);
            this.SudokuTable.KeyUp += new System.Windows.Forms.KeyEventHandler(this.KeyUp);
            // 
            // Column1
            // 
            this.Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column1, "Column1");
            this.Column1.Name = "Column1";
            this.Column1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column2
            // 
            this.Column2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column2, "Column2");
            this.Column2.Name = "Column2";
            this.Column2.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column3
            // 
            this.Column3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column3, "Column3");
            this.Column3.Name = "Column3";
            this.Column3.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column4
            // 
            this.Column4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column4, "Column4");
            this.Column4.Name = "Column4";
            this.Column4.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column5
            // 
            this.Column5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column5, "Column5");
            this.Column5.Name = "Column5";
            this.Column5.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column6
            // 
            this.Column6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column6, "Column6");
            this.Column6.Name = "Column6";
            this.Column6.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column7
            // 
            this.Column7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column7, "Column7");
            this.Column7.Name = "Column7";
            this.Column7.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column8
            // 
            this.Column8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column8, "Column8");
            this.Column8.Name = "Column8";
            this.Column8.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column8.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column9
            // 
            this.Column9.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(this.Column9, "Column9");
            this.Column9.Name = "Column9";
            this.Column9.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column9.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // status
            // 
            resources.ApplyResources(this.status, "status");
            this.status.Name = "status";
            // 
            // next
            // 
            resources.ApplyResources(this.next, "next");
            this.next.Name = "next";
            this.next.Click += new System.EventHandler(this.NextClick);
            // 
            // prior
            // 
            resources.ApplyResources(this.prior, "prior");
            this.prior.Name = "prior";
            this.prior.Click += new System.EventHandler(this.PriorClick);
            // 
            // sudokuMenu
            // 
            this.sudokuMenu.BackColor = System.Drawing.SystemColors.MenuBar;
            this.sudokuMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.file,
            this.sudokuProblem,
            this.options,
            this.help});
            resources.ApplyResources(this.sudokuMenu, "sudokuMenu");
            this.sudokuMenu.Name = "sudokuMenu";
            // 
            // file
            // 
            this.file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newItem,
            this.open,
            this.save,
            this.toolStripSeparator2,
            this.twitter,
            this.export,
            this.toolStripSeparator1,
            this.print,
            this.generateBooklet,
            this.toolStripSeparator5,
            this.exit});
            this.file.Name = "file";
            resources.ApplyResources(this.file, "file");
            // 
            // newItem
            // 
            this.newItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newNormalSudoku,
            this.newXSudoku,
            this.toolStripSeparator13,
            this.sudokuOfTheDayToolStripMenuItem1});
            this.newItem.Name = "newItem";
            resources.ApplyResources(this.newItem, "newItem");
            // 
            // newNormalSudoku
            // 
            this.newNormalSudoku.Name = "newNormalSudoku";
            resources.ApplyResources(this.newNormalSudoku, "newNormalSudoku");
            this.newNormalSudoku.Tag = "disable=1";
            this.newNormalSudoku.Click += new System.EventHandler(this.NewSudokuClick);
            // 
            // newXSudoku
            // 
            this.newXSudoku.Name = "newXSudoku";
            resources.ApplyResources(this.newXSudoku, "newXSudoku");
            this.newXSudoku.Tag = "disable=1";
            this.newXSudoku.Click += new System.EventHandler(this.NewXSudokuClick);
            // 
            // toolStripSeparator13
            // 
            this.toolStripSeparator13.Name = "toolStripSeparator13";
            resources.ApplyResources(this.toolStripSeparator13, "toolStripSeparator13");
            // 
            // sudokuOfTheDayToolStripMenuItem1
            // 
            this.sudokuOfTheDayToolStripMenuItem1.Name = "sudokuOfTheDayToolStripMenuItem1";
            resources.ApplyResources(this.sudokuOfTheDayToolStripMenuItem1, "sudokuOfTheDayToolStripMenuItem1");
            this.sudokuOfTheDayToolStripMenuItem1.Tag = "disable=1";
            this.sudokuOfTheDayToolStripMenuItem1.Click += new System.EventHandler(this.SudokuOfTheDayClicked);
            // 
            // open
            // 
            this.open.Name = "open";
            resources.ApplyResources(this.open, "open");
            this.open.Tag = "disable=1";
            this.open.Click += new System.EventHandler(this.OpenClick);
            // 
            // save
            // 
            this.save.Name = "save";
            resources.ApplyResources(this.save, "save");
            this.save.Tag = "disable=1";
            this.save.Click += new System.EventHandler(this.SaveClick);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // twitter
            // 
            this.twitter.Name = "twitter";
            resources.ApplyResources(this.twitter, "twitter");
            this.twitter.Tag = "disable=1";
            this.twitter.Click += new System.EventHandler(this.TwitterProblemClick);
            // 
            // export
            // 
            this.export.Name = "export";
            resources.ApplyResources(this.export, "export");
            this.export.Tag = "disable=1";
            this.export.Click += new System.EventHandler(this.ExportClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // print
            // 
            this.print.Name = "print";
            resources.ApplyResources(this.print, "print");
            this.print.Tag = "disable=1";
            this.print.Click += new System.EventHandler(this.PrintClick);
            // 
            // generateBooklet
            // 
            this.generateBooklet.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newBooklet,
            this.existingBooklet});
            this.generateBooklet.Name = "generateBooklet";
            resources.ApplyResources(this.generateBooklet, "generateBooklet");
            // 
            // newBooklet
            // 
            this.newBooklet.Name = "newBooklet";
            resources.ApplyResources(this.newBooklet, "newBooklet");
            this.newBooklet.Tag = "disable=1";
            this.newBooklet.Click += new System.EventHandler(this.PrintBookletClick);
            // 
            // existingBooklet
            // 
            this.existingBooklet.Name = "existingBooklet";
            resources.ApplyResources(this.existingBooklet, "existingBooklet");
            this.existingBooklet.Tag = "disable=1";
            this.existingBooklet.Click += new System.EventHandler(this.LoadBookletClick);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            resources.ApplyResources(this.toolStripSeparator5, "toolStripSeparator5");
            // 
            // exit
            // 
            this.exit.Name = "exit";
            resources.ApplyResources(this.exit, "exit");
            this.exit.Click += new System.EventHandler(this.ExitClick);
            // 
            // sudokuProblem
            // 
            this.sudokuProblem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.generate,
            this.solve,
            this.mimimalAllocation,
            this.definiteValues,
            this.toolStripSeparator3,
            this.validate,
            this.check,
            this.info,
            this.EditComment,
            this.toolStripSeparator11,
            this.startSolveProblem,
            this.fixCurrentValues,
            this.releaseCurrentValues,
            this.toolStripSeparator9,
            this.reset,
            this.clearCandidates,
            this.toolStripSeparator4,
            this.abort});
            this.sudokuProblem.Name = "sudokuProblem";
            resources.ApplyResources(this.sudokuProblem, "sudokuProblem");
            // 
            // generate
            // 
            this.generate.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.generateNormalSudoku,
            this.generateXSudoku});
            this.generate.Name = "generate";
            resources.ApplyResources(this.generate, "generate");
            this.generate.Tag = "disable=1";
            // 
            // generateNormalSudoku
            // 
            this.generateNormalSudoku.Name = "generateNormalSudoku";
            resources.ApplyResources(this.generateNormalSudoku, "generateNormalSudoku");
            this.generateNormalSudoku.Click += new System.EventHandler(this.GenerateSudokuClick);
            // 
            // generateXSudoku
            // 
            this.generateXSudoku.Name = "generateXSudoku";
            resources.ApplyResources(this.generateXSudoku, "generateXSudoku");
            this.generateXSudoku.Click += new System.EventHandler(this.GenerateXSudokuClick);
            // 
            // solve
            // 
            this.solve.Name = "solve";
            resources.ApplyResources(this.solve, "solve");
            this.solve.Tag = "disable=1";
            this.solve.Click += new System.EventHandler(this.SolveClick);
            // 
            // mimimalAllocation
            // 
            this.mimimalAllocation.Name = "mimimalAllocation";
            resources.ApplyResources(this.mimimalAllocation, "mimimalAllocation");
            this.mimimalAllocation.Tag = "disable=1";
            this.mimimalAllocation.Click += new System.EventHandler(this.MinimizeClick);
            // 
            // definiteValues
            // 
            this.definiteValues.Name = "definiteValues";
            resources.ApplyResources(this.definiteValues, "definiteValues");
            this.definiteValues.Tag = "disable=1";
            this.definiteValues.Click += new System.EventHandler(this.DefiniteClick);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // validate
            // 
            this.validate.Name = "validate";
            resources.ApplyResources(this.validate, "validate");
            this.validate.Tag = "disable=1";
            this.validate.Click += new System.EventHandler(this.ValidateClick);
            // 
            // check
            // 
            this.check.Name = "check";
            resources.ApplyResources(this.check, "check");
            this.check.Tag = "disable=1";
            this.check.Click += new System.EventHandler(this.CheckClick);
            // 
            // info
            // 
            this.info.Name = "info";
            resources.ApplyResources(this.info, "info");
            this.info.Tag = "disable=1";
            this.info.Click += new System.EventHandler(this.InfoClick);
            // 
            // EditComment
            // 
            this.EditComment.Name = "EditComment";
            resources.ApplyResources(this.EditComment, "EditComment");
            this.EditComment.Tag = "disable=1";
            this.EditComment.Click += new System.EventHandler(this.EditCommentClicked);
            // 
            // toolStripSeparator11
            // 
            this.toolStripSeparator11.Name = "toolStripSeparator11";
            resources.ApplyResources(this.toolStripSeparator11, "toolStripSeparator11");
            // 
            // startSolveProblem
            // 
            this.startSolveProblem.Name = "startSolveProblem";
            resources.ApplyResources(this.startSolveProblem, "startSolveProblem");
            this.startSolveProblem.Tag = "disable=1";
            // 
            // fixCurrentValues
            // 
            this.fixCurrentValues.Name = "fixCurrentValues";
            resources.ApplyResources(this.fixCurrentValues, "fixCurrentValues");
            this.fixCurrentValues.Tag = "disable=1";
            this.fixCurrentValues.Click += new System.EventHandler(this.FixClick);
            // 
            // releaseCurrentValues
            // 
            this.releaseCurrentValues.Name = "releaseCurrentValues";
            resources.ApplyResources(this.releaseCurrentValues, "releaseCurrentValues");
            this.releaseCurrentValues.Tag = "disable=1";
            this.releaseCurrentValues.Click += new System.EventHandler(this.ReleaseClick);
            // 
            // toolStripSeparator9
            // 
            this.toolStripSeparator9.Name = "toolStripSeparator9";
            resources.ApplyResources(this.toolStripSeparator9, "toolStripSeparator9");
            // 
            // reset
            // 
            this.reset.Name = "reset";
            resources.ApplyResources(this.reset, "reset");
            this.reset.Tag = "disable=1";
            this.reset.Click += new System.EventHandler(this.ResetClick);
            // 
            // clearCandidates
            // 
            this.clearCandidates.Name = "clearCandidates";
            resources.ApplyResources(this.clearCandidates, "clearCandidates");
            this.clearCandidates.Tag = "disable=1";
            this.clearCandidates.Click += new System.EventHandler(this.ClearCandidatesClick);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // abort
            // 
            this.abort.Name = "abort";
            resources.ApplyResources(this.abort, "abort");
            this.abort.Tag = "disable=0";
            this.abort.Click += new System.EventHandler(this.AbortClick);
            // 
            // options
            // 
            this.options.CheckOnClick = true;
            this.options.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undo,
            this.toolStripSeparator8,
            this.debug,
            this.findallSolutions,
            this.showPossibleValues,
            this.autoCheck,
            this.pencilMode,
            this.highlightSameValues,
            this.markNeighbors,
            this.toolStripSeparator10,
            this.pause,
            this.resetTimer,
            this.toolStripSeparator6,
            this.sudokuOptionsToolStripMenuItem});
            this.options.Name = "options";
            resources.ApplyResources(this.options, "options");
            this.options.DropDownOpening += new System.EventHandler(this.OptionsMenuOpening);
            // 
            // undo
            // 
            this.undo.Name = "undo";
            resources.ApplyResources(this.undo, "undo");
            this.undo.Tag = "disable=1";
            this.undo.Click += new System.EventHandler(this.UndoClick);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            resources.ApplyResources(this.toolStripSeparator8, "toolStripSeparator8");
            // 
            // debug
            // 
            this.debug.CheckOnClick = true;
            this.debug.Name = "debug";
            resources.ApplyResources(this.debug, "debug");
            this.debug.Tag = "disable=1";
            this.debug.Click += new System.EventHandler(this.DebugClick);
            // 
            // findallSolutions
            // 
            this.findallSolutions.CheckOnClick = true;
            this.findallSolutions.Name = "findallSolutions";
            resources.ApplyResources(this.findallSolutions, "findallSolutions");
            this.findallSolutions.Tag = "disable=1";
            this.findallSolutions.Click += new System.EventHandler(this.FindallSolutionsClick);
            // 
            // showPossibleValues
            // 
            this.showPossibleValues.CheckOnClick = true;
            this.showPossibleValues.Name = "showPossibleValues";
            resources.ApplyResources(this.showPossibleValues, "showPossibleValues");
            this.showPossibleValues.Tag = "disable=1";
            this.showPossibleValues.Click += new System.EventHandler(this.ShowPossibleValuesClick);
            // 
            // autoCheck
            // 
            this.autoCheck.CheckOnClick = true;
            this.autoCheck.Name = "autoCheck";
            resources.ApplyResources(this.autoCheck, "autoCheck");
            this.autoCheck.Tag = "disable=1";
            this.autoCheck.Click += new System.EventHandler(this.AutoCheckClick);
            // 
            // pencilMode
            // 
            this.pencilMode.Name = "pencilMode";
            resources.ApplyResources(this.pencilMode, "pencilMode");
            this.pencilMode.Tag = "disable=1";
            this.pencilMode.Click += new System.EventHandler(this.TogglePencilModeClick);
            // 
            // highlightSameValues
            // 
            this.highlightSameValues.Name = "highlightSameValues";
            resources.ApplyResources(this.highlightSameValues, "highlightSameValues");
            this.highlightSameValues.Click += new System.EventHandler(this.ToggleHighlightSameValuesClicked);
            // 
            // markNeighbors
            // 
            this.markNeighbors.CheckOnClick = true;
            this.markNeighbors.Name = "markNeighbors";
            resources.ApplyResources(this.markNeighbors, "markNeighbors");
            this.markNeighbors.Tag = "disable=1";
            this.markNeighbors.Click += new System.EventHandler(this.MarkNeighborsClicked);
            // 
            // toolStripSeparator10
            // 
            this.toolStripSeparator10.Name = "toolStripSeparator10";
            resources.ApplyResources(this.toolStripSeparator10, "toolStripSeparator10");
            // 
            // pause
            // 
            this.pause.Name = "pause";
            resources.ApplyResources(this.pause, "pause");
            this.pause.Tag = "disable=1";
            this.pause.Click += new System.EventHandler(this.PauseClick);
            // 
            // resetTimer
            // 
            this.resetTimer.Name = "resetTimer";
            resources.ApplyResources(this.resetTimer, "resetTimer");
            this.resetTimer.Tag = "disable=1";
            this.resetTimer.Click += new System.EventHandler(this.ResetTimerClick);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
            // 
            // sudokuOptionsToolStripMenuItem
            // 
            this.sudokuOptionsToolStripMenuItem.Name = "sudokuOptionsToolStripMenuItem";
            resources.ApplyResources(this.sudokuOptionsToolStripMenuItem, "sudokuOptionsToolStripMenuItem");
            this.sudokuOptionsToolStripMenuItem.Click += new System.EventHandler(this.OptionsClick);
            // 
            // help
            // 
            this.help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showCellInfo,
            this.showHints,
            this.toolStripSeparator7,
            this.visitHomepage,
            this.versionHistory,
            this.aboutSudoku});
            this.help.Name = "help";
            resources.ApplyResources(this.help, "help");
            // 
            // showCellInfo
            // 
            this.showCellInfo.Name = "showCellInfo";
            resources.ApplyResources(this.showCellInfo, "showCellInfo");
            this.showCellInfo.Tag = "disable=1";
            this.showCellInfo.Click += new System.EventHandler(this.DisplayCellInfo);
            // 
            // showHints
            // 
            this.showHints.Name = "showHints";
            resources.ApplyResources(this.showHints, "showHints");
            this.showHints.Tag = "disable=1";
            this.showHints.Click += new System.EventHandler(this.ShowHintsClick);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // visitHomepage
            // 
            this.visitHomepage.Name = "visitHomepage";
            resources.ApplyResources(this.visitHomepage, "visitHomepage");
            this.visitHomepage.Click += new System.EventHandler(this.VisitHomepageClick);
            // 
            // versionHistory
            // 
            this.versionHistory.Name = "versionHistory";
            resources.ApplyResources(this.versionHistory, "versionHistory");
            this.versionHistory.Click += new System.EventHandler(this.VersionHistoryClicked);
            // 
            // aboutSudoku
            // 
            this.aboutSudoku.Name = "aboutSudoku";
            resources.ApplyResources(this.aboutSudoku, "aboutSudoku");
            this.aboutSudoku.Click += new System.EventHandler(this.AboutSudokuClick);
            // 
            // saveSudokuDialog
            // 
            this.saveSudokuDialog.DefaultExt = "*.sudoku";
            resources.ApplyResources(this.saveSudokuDialog, "saveSudokuDialog");
            this.saveSudokuDialog.OverwritePrompt = false;
            this.saveSudokuDialog.RestoreDirectory = true;
            this.saveSudokuDialog.SupportMultiDottedExtensions = true;
            // 
            // openSudokuDialog
            // 
            this.openSudokuDialog.DefaultExt = "*.sudoku";
            resources.ApplyResources(this.openSudokuDialog, "openSudokuDialog");
            this.openSudokuDialog.RestoreDirectory = true;
            this.openSudokuDialog.ShowReadOnly = true;
            this.openSudokuDialog.SupportMultiDottedExtensions = true;
            // 
            // sudokuStatusBar
            // 
            this.sudokuStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sudokuStatusBarText});
            resources.ApplyResources(this.sudokuStatusBar, "sudokuStatusBar");
            this.sudokuStatusBar.Name = "sudokuStatusBar";
            this.sudokuStatusBar.SizingGrip = false;
            // 
            // sudokuStatusBarText
            // 
            this.sudokuStatusBarText.Name = "sudokuStatusBarText";
            resources.ApplyResources(this.sudokuStatusBarText, "sudokuStatusBarText");
            // 
            // printSudokuDialog
            // 
            this.printSudokuDialog.AllowSelection = true;
            this.printSudokuDialog.AllowSomePages = true;
            this.printSudokuDialog.Document = this.printSudoku;
            // 
            // printSudoku
            // 
            this.printSudoku.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.PrintSudokuEvent);
            // 
            // selectBookletDirectory
            // 
            resources.ApplyResources(this.selectBookletDirectory, "selectBookletDirectory");
            // 
            // solvingTimer
            // 
            this.solvingTimer.Tick += new System.EventHandler(this.SolvingTimerTick);
            // 
            // autoPauseTimer
            // 
            this.autoPauseTimer.Interval = 1000;
            this.autoPauseTimer.Tick += new System.EventHandler(this.AutoPauseTick);
            // 
            // SudokuForm
            // 
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.prior);
            this.Controls.Add(this.next);
            this.Controls.Add(this.status);
            this.Controls.Add(this.SudokuTable);
            this.Controls.Add(this.sudokuStatusBar);
            this.Controls.Add(this.sudokuMenu);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.MainMenuStrip = this.sudokuMenu;
            this.MaximizeBox = false;
            this.Name = "SudokuForm";
            this.Opacity = 0D;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ExitSudoku);
            this.MenuComplete += new System.EventHandler(this.ActivateGrid);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.DropProblem);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.DragOverForm);
            this.Resize += new System.EventHandler(this.ResizeForm);
            ((System.ComponentModel.ISupportInitialize)(this.SudokuTable)).EndInit();
            this.sudokuMenu.ResumeLayout(false);
            this.sudokuMenu.PerformLayout();
            this.sudokuStatusBar.ResumeLayout(false);
            this.sudokuStatusBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label status;
        private System.Windows.Forms.Button next;
        private System.Windows.Forms.Button prior;
        private System.Windows.Forms.Timer solutionTimer;
        private System.Windows.Forms.MenuStrip sudokuMenu;
        private System.Windows.Forms.ToolStripMenuItem file;
        private System.Windows.Forms.ToolStripMenuItem sudokuProblem;
        private System.Windows.Forms.ToolStripMenuItem help;
        private System.Windows.Forms.ToolStripMenuItem aboutSudoku;
        private System.Windows.Forms.ToolStripMenuItem open;
        private System.Windows.Forms.ToolStripMenuItem save;
        private System.Windows.Forms.ToolStripMenuItem exit;
        private System.Windows.Forms.ToolStripMenuItem generate;
        private System.Windows.Forms.ToolStripMenuItem solve;
        private System.Windows.Forms.ToolStripMenuItem reset;
        private System.Windows.Forms.ToolStripMenuItem options;
        private System.Windows.Forms.ToolStripMenuItem debug;
        private System.Windows.Forms.ToolStripMenuItem findallSolutions;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.SaveFileDialog saveSudokuDialog;
        private System.Windows.Forms.OpenFileDialog openSudokuDialog;
        private System.Windows.Forms.StatusStrip sudokuStatusBar;
        private System.Windows.Forms.ToolStripStatusLabel sudokuStatusBarText;
        private System.Windows.Forms.ToolStripMenuItem check;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem validate;
        private System.Windows.Forms.ToolStripMenuItem abort;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem print;
        private System.Windows.Forms.PrintDialog printSudokuDialog;
        /*
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
         */
        private System.Windows.Forms.ToolStripMenuItem generateBooklet;
        private System.Windows.Forms.ToolStripMenuItem info;
        private System.Windows.Forms.ToolStripMenuItem newBooklet;
        private System.Windows.Forms.ToolStripMenuItem existingBooklet;
        private System.Windows.Forms.FolderBrowserDialog selectBookletDirectory;
        private System.Windows.Forms.ToolStripMenuItem sudokuOptionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem autoCheck;
        private System.Windows.Forms.ToolStripMenuItem definiteValues;
        private System.Windows.Forms.ToolStripMenuItem showHints;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column4;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column5;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column6;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column7;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column8;
        private System.Windows.Forms.DataGridViewTextBoxColumn Column9;
        private System.Windows.Forms.ToolStripMenuItem undo;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem showPossibleValues;
        private System.Windows.Forms.Timer solvingTimer;
        private System.Windows.Forms.ToolStripMenuItem visitHomepage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem pause;
        private System.Windows.Forms.ToolStripMenuItem resetTimer;
        private System.Windows.Forms.ToolStripMenuItem EditComment;
        private SudokuBoard SudokuTable;
        private System.Windows.Forms.ToolStripMenuItem newItem;
        private System.Windows.Forms.ToolStripMenuItem newNormalSudoku;
        private System.Windows.Forms.ToolStripMenuItem newXSudoku;
        private System.Windows.Forms.ToolStripMenuItem generateNormalSudoku;
        private System.Windows.Forms.ToolStripMenuItem generateXSudoku;
        private System.Windows.Forms.ToolStripMenuItem versionHistory;
        private System.Windows.Forms.ToolStripMenuItem clearCandidates;
        private System.Windows.Forms.ToolStripMenuItem showCellInfo;
        private System.Drawing.Printing.PrintDocument printSudoku;
        private System.Windows.Forms.ToolStripMenuItem export;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem mimimalAllocation;
        private System.Windows.Forms.ToolStripMenuItem markNeighbors;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator11;
        private System.Windows.Forms.ToolStripMenuItem fixCurrentValues;
        private System.Windows.Forms.ToolStripMenuItem releaseCurrentValues;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator13;
        private System.Windows.Forms.ToolStripMenuItem sudokuOfTheDayToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem startSolveProblem;
        private System.Windows.Forms.ToolStripMenuItem twitter;
        private System.Windows.Forms.Timer autoPauseTimer;
        private System.Windows.Forms.ToolStripMenuItem pencilMode;
        private System.Windows.Forms.ToolStripMenuItem highlightSameValues;
    }
}