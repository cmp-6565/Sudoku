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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SudokuForm));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            SudokuGrid = new SudokuBoard();
            Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            Column9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            status = new System.Windows.Forms.Label();
            next = new System.Windows.Forms.Button();
            prior = new System.Windows.Forms.Button();
            sudokuMenu = new System.Windows.Forms.MenuStrip();
            file = new System.Windows.Forms.ToolStripMenuItem();
            newItem = new System.Windows.Forms.ToolStripMenuItem();
            newNormalSudoku = new System.Windows.Forms.ToolStripMenuItem();
            newXSudoku = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator13 = new System.Windows.Forms.ToolStripSeparator();
            sudokuOfTheDayToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            open = new System.Windows.Forms.ToolStripMenuItem();
            save = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            twitter = new System.Windows.Forms.ToolStripMenuItem();
            export = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            print = new System.Windows.Forms.ToolStripMenuItem();
            generateBooklet = new System.Windows.Forms.ToolStripMenuItem();
            newBooklet = new System.Windows.Forms.ToolStripMenuItem();
            existingBooklet = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            exit = new System.Windows.Forms.ToolStripMenuItem();
            SudokuProblem = new System.Windows.Forms.ToolStripMenuItem();
            generate = new System.Windows.Forms.ToolStripMenuItem();
            generateNormalSudoku = new System.Windows.Forms.ToolStripMenuItem();
            generateXSudoku = new System.Windows.Forms.ToolStripMenuItem();
            solve = new System.Windows.Forms.ToolStripMenuItem();
            mimimalAllocation = new System.Windows.Forms.ToolStripMenuItem();
            definiteValues = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            validate = new System.Windows.Forms.ToolStripMenuItem();
            check = new System.Windows.Forms.ToolStripMenuItem();
            info = new System.Windows.Forms.ToolStripMenuItem();
            EditComment = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator11 = new System.Windows.Forms.ToolStripSeparator();
            startSolveProblem = new System.Windows.Forms.ToolStripMenuItem();
            fixCurrentValues = new System.Windows.Forms.ToolStripMenuItem();
            releaseCurrentValues = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator9 = new System.Windows.Forms.ToolStripSeparator();
            reset = new System.Windows.Forms.ToolStripMenuItem();
            clearCandidates = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            abort = new System.Windows.Forms.ToolStripMenuItem();
            options = new System.Windows.Forms.ToolStripMenuItem();
            undo = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            traceMode = new System.Windows.Forms.ToolStripMenuItem();
            findallSolutions = new System.Windows.Forms.ToolStripMenuItem();
            showPossibleValues = new System.Windows.Forms.ToolStripMenuItem();
            autoCheck = new System.Windows.Forms.ToolStripMenuItem();
            pencilMode = new System.Windows.Forms.ToolStripMenuItem();
            highlightSameValues = new System.Windows.Forms.ToolStripMenuItem();
            markNeighbors = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
            pause = new System.Windows.Forms.ToolStripMenuItem();
            resetTimer = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            sudokuOptionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            help = new System.Windows.Forms.ToolStripMenuItem();
            showCellInfo = new System.Windows.Forms.ToolStripMenuItem();
            showHints = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            visitHomepage = new System.Windows.Forms.ToolStripMenuItem();
            versionHistory = new System.Windows.Forms.ToolStripMenuItem();
            aboutSudoku = new System.Windows.Forms.ToolStripMenuItem();
            saveSudokuDialog = new System.Windows.Forms.SaveFileDialog();
            openSudokuDialog = new System.Windows.Forms.OpenFileDialog();
            sudokuStatusBar = new System.Windows.Forms.StatusStrip();
            sudokuStatusBarText = new System.Windows.Forms.ToolStripStatusLabel();
            selectBookletDirectory = new System.Windows.Forms.FolderBrowserDialog();
            ((System.ComponentModel.ISupportInitialize)SudokuGrid).BeginInit();
            sudokuMenu.SuspendLayout();
            sudokuStatusBar.SuspendLayout();
            SuspendLayout();
            // 
            // SudokuGrid
            // 
            SudokuGrid.AllowUserToAddRows = false;
            SudokuGrid.AllowUserToDeleteRows = false;
            SudokuGrid.AllowUserToResizeColumns = false;
            SudokuGrid.AllowUserToResizeRows = false;
            SudokuGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            SudokuGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Sunken;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            SudokuGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            resources.ApplyResources(SudokuGrid, "SudokuGrid");
            SudokuGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            SudokuGrid.ColumnHeadersVisible = false;
            SudokuGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { Column1, Column2, Column3, Column4, Column5, Column6, Column7, Column8, Column9 });
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Comic Sans MS", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle2.NullValue = null;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.ButtonShadow;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            SudokuGrid.DefaultCellStyle = dataGridViewCellStyle2;
            SudokuGrid.MultiSelect = false;
            SudokuGrid.Name = "SudokuGrid";
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            SudokuGrid.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            SudokuGrid.RowHeadersVisible = false;
            SudokuGrid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            dataGridViewCellStyle4.FormatProvider = new System.Globalization.CultureInfo("de-DE");
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            SudokuGrid.RowsDefaultCellStyle = dataGridViewCellStyle4;
            SudokuGrid.RowTemplate.Height = 30;
            SudokuGrid.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            SudokuGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            SudokuGrid.StandardTab = true;
            // 
            // Column1
            // 
            Column1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column1, "Column1");
            Column1.Name = "Column1";
            Column1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column2
            // 
            Column2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column2, "Column2");
            Column2.Name = "Column2";
            Column2.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column3
            // 
            Column3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column3, "Column3");
            Column3.Name = "Column3";
            Column3.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column4
            // 
            Column4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column4, "Column4");
            Column4.Name = "Column4";
            Column4.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column5
            // 
            Column5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column5, "Column5");
            Column5.Name = "Column5";
            Column5.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column6
            // 
            Column6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column6, "Column6");
            Column6.Name = "Column6";
            Column6.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column7
            // 
            Column7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column7, "Column7");
            Column7.Name = "Column7";
            Column7.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column8
            // 
            Column8.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column8, "Column8");
            Column8.Name = "Column8";
            Column8.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column8.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Column9
            // 
            Column9.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            resources.ApplyResources(Column9, "Column9");
            Column9.Name = "Column9";
            Column9.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            Column9.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // status
            // 
            resources.ApplyResources(status, "status");
            status.Name = "status";
            // 
            // next
            // 
            resources.ApplyResources(next, "next");
            next.Name = "next";
            next.Click += NextClick;
            // 
            // prior
            // 
            resources.ApplyResources(prior, "prior");
            prior.Name = "prior";
            prior.Click += PriorClick;
            // 
            // sudokuMenu
            // 
            sudokuMenu.BackColor = System.Drawing.SystemColors.MenuBar;
            sudokuMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { file, SudokuProblem, options, help });
            resources.ApplyResources(sudokuMenu, "sudokuMenu");
            sudokuMenu.Name = "sudokuMenu";
            // 
            // file
            // 
            file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newItem, open, save, toolStripSeparator2, twitter, export, toolStripSeparator1, print, generateBooklet, toolStripSeparator5, exit });
            file.Name = "file";
            resources.ApplyResources(file, "file");
            // 
            // newItem
            // 
            newItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newNormalSudoku, newXSudoku, toolStripSeparator13, sudokuOfTheDayToolStripMenuItem1 });
            newItem.Name = "newItem";
            resources.ApplyResources(newItem, "newItem");
            // 
            // newNormalSudoku
            // 
            newNormalSudoku.Name = "newNormalSudoku";
            resources.ApplyResources(newNormalSudoku, "newNormalSudoku");
            newNormalSudoku.Tag = "disable=1";
            newNormalSudoku.Click += NewSudokuClick;
            // 
            // newXSudoku
            // 
            newXSudoku.Name = "newXSudoku";
            resources.ApplyResources(newXSudoku, "newXSudoku");
            newXSudoku.Tag = "disable=1";
            newXSudoku.Click += NewXSudokuClick;
            // 
            // toolStripSeparator13
            // 
            toolStripSeparator13.Name = "toolStripSeparator13";
            resources.ApplyResources(toolStripSeparator13, "toolStripSeparator13");
            // 
            // sudokuOfTheDayToolStripMenuItem1
            // 
            sudokuOfTheDayToolStripMenuItem1.Name = "sudokuOfTheDayToolStripMenuItem1";
            resources.ApplyResources(sudokuOfTheDayToolStripMenuItem1, "sudokuOfTheDayToolStripMenuItem1");
            sudokuOfTheDayToolStripMenuItem1.Tag = "disable=1";
            sudokuOfTheDayToolStripMenuItem1.Click += SudokuOfTheDayClicked;
            // 
            // open
            // 
            open.Name = "open";
            resources.ApplyResources(open, "open");
            open.Tag = "disable=1";
            open.Click += OpenClick;
            // 
            // save
            // 
            save.Name = "save";
            resources.ApplyResources(save, "save");
            save.Tag = "disable=1";
            save.Click += SaveClick;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(toolStripSeparator2, "toolStripSeparator2");
            // 
            // twitter
            // 
            twitter.Name = "twitter";
            resources.ApplyResources(twitter, "twitter");
            twitter.Tag = "disable=1";
            twitter.Click += TwitterProblemClick;
            // 
            // export
            // 
            export.Name = "export";
            resources.ApplyResources(export, "export");
            export.Tag = "disable=1";
            export.Click += ExportClick;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(toolStripSeparator1, "toolStripSeparator1");
            // 
            // print
            // 
            print.Name = "print";
            resources.ApplyResources(print, "print");
            print.Tag = "disable=1";
            print.Click += PrintClick;
            // 
            // generateBooklet
            // 
            generateBooklet.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newBooklet, existingBooklet });
            generateBooklet.Name = "generateBooklet";
            resources.ApplyResources(generateBooklet, "generateBooklet");
            // 
            // newBooklet
            // 
            newBooklet.Name = "newBooklet";
            resources.ApplyResources(newBooklet, "newBooklet");
            newBooklet.Tag = "disable=1";
            newBooklet.Click += PrintBookletClick;
            // 
            // existingBooklet
            // 
            existingBooklet.Name = "existingBooklet";
            resources.ApplyResources(existingBooklet, "existingBooklet");
            existingBooklet.Tag = "disable=1";
            existingBooklet.Click += LoadBookletClick;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            resources.ApplyResources(toolStripSeparator5, "toolStripSeparator5");
            // 
            // exit
            // 
            exit.Name = "exit";
            resources.ApplyResources(exit, "exit");
            exit.Click += ExitClick;
            // 
            // SudokuProblem
            // 
            SudokuProblem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { generate, solve, mimimalAllocation, definiteValues, toolStripSeparator3, validate, check, info, EditComment, toolStripSeparator11, startSolveProblem, fixCurrentValues, releaseCurrentValues, toolStripSeparator9, reset, clearCandidates, toolStripSeparator4, abort });
            SudokuProblem.Name = "SudokuProblem";
            resources.ApplyResources(SudokuProblem, "SudokuProblem");
            // 
            // generate
            // 
            generate.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { generateNormalSudoku, generateXSudoku });
            generate.Name = "generate";
            resources.ApplyResources(generate, "generate");
            generate.Tag = "disable=1";
            // 
            // generateNormalSudoku
            // 
            generateNormalSudoku.Name = "generateNormalSudoku";
            resources.ApplyResources(generateNormalSudoku, "generateNormalSudoku");
            generateNormalSudoku.Click += GenerateSudokuClick;
            // 
            // generateXSudoku
            // 
            generateXSudoku.Name = "generateXSudoku";
            resources.ApplyResources(generateXSudoku, "generateXSudoku");
            generateXSudoku.Click += GenerateXSudokuClick;
            // 
            // solve
            // 
            solve.Name = "solve";
            resources.ApplyResources(solve, "solve");
            solve.Tag = "disable=1";
            solve.Click += SolveClick;
            // 
            // mimimalAllocation
            // 
            mimimalAllocation.Name = "mimimalAllocation";
            resources.ApplyResources(mimimalAllocation, "mimimalAllocation");
            mimimalAllocation.Tag = "disable=1";
            mimimalAllocation.Click += MinimizeClick;
            // 
            // definiteValues
            // 
            definiteValues.Name = "definiteValues";
            resources.ApplyResources(definiteValues, "definiteValues");
            definiteValues.Tag = "disable=1";
            definiteValues.Click += DefiniteClick;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(toolStripSeparator3, "toolStripSeparator3");
            // 
            // validate
            // 
            validate.Name = "validate";
            resources.ApplyResources(validate, "validate");
            validate.Tag = "disable=1";
            validate.Click += ValidateClick;
            // 
            // check
            // 
            check.Name = "check";
            resources.ApplyResources(check, "check");
            check.Tag = "disable=1";
            check.Click += CheckClick;
            // 
            // info
            // 
            info.Name = "info";
            resources.ApplyResources(info, "info");
            info.Tag = "disable=1";
            info.Click += InfoClick;
            // 
            // EditComment
            // 
            EditComment.Name = "EditComment";
            resources.ApplyResources(EditComment, "EditComment");
            EditComment.Tag = "disable=1";
            EditComment.Click += EditCommentClicked;
            // 
            // toolStripSeparator11
            // 
            toolStripSeparator11.Name = "toolStripSeparator11";
            resources.ApplyResources(toolStripSeparator11, "toolStripSeparator11");
            // 
            // startSolveProblem
            // 
            startSolveProblem.Name = "startSolveProblem";
            resources.ApplyResources(startSolveProblem, "startSolveProblem");
            startSolveProblem.Tag = "disable=1";
            startSolveProblem.Click += StartGameClick;
            // 
            // fixCurrentValues
            // 
            fixCurrentValues.Name = "fixCurrentValues";
            resources.ApplyResources(fixCurrentValues, "fixCurrentValues");
            fixCurrentValues.Tag = "disable=1";
            fixCurrentValues.Click += FixClick;
            // 
            // releaseCurrentValues
            // 
            releaseCurrentValues.Name = "releaseCurrentValues";
            resources.ApplyResources(releaseCurrentValues, "releaseCurrentValues");
            releaseCurrentValues.Tag = "disable=1";
            releaseCurrentValues.Click += ReleaseClick;
            // 
            // toolStripSeparator9
            // 
            toolStripSeparator9.Name = "toolStripSeparator9";
            resources.ApplyResources(toolStripSeparator9, "toolStripSeparator9");
            // 
            // reset
            // 
            reset.Name = "reset";
            resources.ApplyResources(reset, "reset");
            reset.Tag = "disable=1";
            reset.Click += ResetClick;
            // 
            // clearCandidates
            // 
            clearCandidates.Name = "clearCandidates";
            resources.ApplyResources(clearCandidates, "clearCandidates");
            clearCandidates.Tag = "disable=1";
            clearCandidates.Click += ClearCandidatesClick;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(toolStripSeparator4, "toolStripSeparator4");
            // 
            // abort
            // 
            abort.Name = "abort";
            resources.ApplyResources(abort, "abort");
            abort.Tag = "disable=0";
            abort.Click += AbortClick;
            // 
            // options
            // 
            options.CheckOnClick = true;
            options.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { undo, toolStripSeparator8, traceMode, findallSolutions, showPossibleValues, autoCheck, pencilMode, highlightSameValues, markNeighbors, toolStripSeparator10, pause, resetTimer, toolStripSeparator6, sudokuOptionsToolStripMenuItem });
            options.Name = "options";
            resources.ApplyResources(options, "options");
            options.DropDownOpening += OptionsMenuOpening;
            // 
            // undo
            // 
            undo.Name = "undo";
            resources.ApplyResources(undo, "undo");
            undo.Tag = "disable=1";
            undo.Click += UndoClick;
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            resources.ApplyResources(toolStripSeparator8, "toolStripSeparator8");
            // 
            // debug
            // 
            traceMode.CheckOnClick = true;
            traceMode.Name = "debug";
            resources.ApplyResources(traceMode, "debug");
            traceMode.Tag = "disable=1";
            traceMode.Click += DebugClick;
            // 
            // findallSolutions
            // 
            findallSolutions.CheckOnClick = true;
            findallSolutions.Name = "findallSolutions";
            resources.ApplyResources(findallSolutions, "findallSolutions");
            findallSolutions.Tag = "disable=1";
            findallSolutions.Click += FindallSolutionsClick;
            // 
            // showPossibleValues
            // 
            showPossibleValues.CheckOnClick = true;
            showPossibleValues.Name = "showPossibleValues";
            resources.ApplyResources(showPossibleValues, "showPossibleValues");
            showPossibleValues.Tag = "disable=1";
            showPossibleValues.Click += ShowPossibleValuesClick;
            // 
            // autoCheck
            // 
            autoCheck.CheckOnClick = true;
            autoCheck.Name = "autoCheck";
            resources.ApplyResources(autoCheck, "autoCheck");
            autoCheck.Tag = "disable=1";
            autoCheck.Click += AutoCheckClick;
            // 
            // pencilMode
            // 
            pencilMode.Name = "pencilMode";
            resources.ApplyResources(pencilMode, "pencilMode");
            pencilMode.Tag = "disable=1";
            pencilMode.Click += TogglePencilModeClick;
            // 
            // highlightSameValues
            // 
            highlightSameValues.Name = "highlightSameValues";
            resources.ApplyResources(highlightSameValues, "highlightSameValues");
            highlightSameValues.Tag = "disable=1";
            highlightSameValues.Click += ToggleHighlightSameValuesClicked;
            // 
            // markNeighbors
            // 
            markNeighbors.CheckOnClick = true;
            markNeighbors.Name = "markNeighbors";
            resources.ApplyResources(markNeighbors, "markNeighbors");
            markNeighbors.Tag = "disable=1";
            markNeighbors.Click += MarkNeighborsClicked;
            // 
            // toolStripSeparator10
            // 
            toolStripSeparator10.Name = "toolStripSeparator10";
            resources.ApplyResources(toolStripSeparator10, "toolStripSeparator10");
            // 
            // pause
            // 
            pause.Name = "pause";
            resources.ApplyResources(pause, "pause");
            pause.Tag = "disable=1";
            pause.Click += PauseClick;
            // 
            // resetTimer
            // 
            resetTimer.Name = "resetTimer";
            resources.ApplyResources(resetTimer, "resetTimer");
            resetTimer.Tag = "disable=1";
            resetTimer.Click += ResetTimerClick;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            resources.ApplyResources(toolStripSeparator6, "toolStripSeparator6");
            // 
            // sudokuOptionsToolStripMenuItem
            // 
            sudokuOptionsToolStripMenuItem.Name = "sudokuOptionsToolStripMenuItem";
            resources.ApplyResources(sudokuOptionsToolStripMenuItem, "sudokuOptionsToolStripMenuItem");
            sudokuOptionsToolStripMenuItem.Click += OptionsClick;
            // 
            // help
            // 
            help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { showCellInfo, showHints, toolStripSeparator7, visitHomepage, versionHistory, aboutSudoku });
            help.Name = "help";
            resources.ApplyResources(help, "help");
            // 
            // showCellInfo
            // 
            showCellInfo.Name = "showCellInfo";
            resources.ApplyResources(showCellInfo, "showCellInfo");
            showCellInfo.Tag = "disable=1";
            showCellInfo.Click += DisplayCellInfo;
            // 
            // showHints
            // 
            showHints.Name = "showHints";
            resources.ApplyResources(showHints, "showHints");
            showHints.Tag = "disable=1";
            showHints.Click += ShowHintsClick;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            resources.ApplyResources(toolStripSeparator7, "toolStripSeparator7");
            // 
            // visitHomepage
            // 
            visitHomepage.Name = "visitHomepage";
            resources.ApplyResources(visitHomepage, "visitHomepage");
            visitHomepage.Click += VisitHomepageClick;
            // 
            // versionHistory
            // 
            versionHistory.Name = "versionHistory";
            resources.ApplyResources(versionHistory, "versionHistory");
            versionHistory.Click += VersionHistoryClicked;
            // 
            // aboutSudoku
            // 
            aboutSudoku.Name = "aboutSudoku";
            resources.ApplyResources(aboutSudoku, "aboutSudoku");
            aboutSudoku.Click += AboutSudokuClick;
            // 
            // saveSudokuDialog
            // 
            saveSudokuDialog.DefaultExt = "*.sudoku";
            resources.ApplyResources(saveSudokuDialog, "saveSudokuDialog");
            saveSudokuDialog.OverwritePrompt = false;
            saveSudokuDialog.RestoreDirectory = true;
            saveSudokuDialog.SupportMultiDottedExtensions = true;
            // 
            // openSudokuDialog
            // 
            openSudokuDialog.DefaultExt = "*.sudoku";
            resources.ApplyResources(openSudokuDialog, "openSudokuDialog");
            openSudokuDialog.RestoreDirectory = true;
            openSudokuDialog.ShowReadOnly = true;
            openSudokuDialog.SupportMultiDottedExtensions = true;
            // 
            // sudokuStatusBar
            // 
            sudokuStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { sudokuStatusBarText });
            resources.ApplyResources(sudokuStatusBar, "sudokuStatusBar");
            sudokuStatusBar.Name = "sudokuStatusBar";
            sudokuStatusBar.SizingGrip = false;
            // 
            // sudokuStatusBarText
            // 
            sudokuStatusBarText.Name = "sudokuStatusBarText";
            resources.ApplyResources(sudokuStatusBarText, "sudokuStatusBarText");
            // 
            // selectBookletDirectory
            // 
            resources.ApplyResources(selectBookletDirectory, "selectBookletDirectory");
            // 
            // SudokuForm
            // 
            AllowDrop = true;
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            Controls.Add(prior);
            Controls.Add(next);
            Controls.Add(status);
            Controls.Add(SudokuGrid);
            Controls.Add(sudokuStatusBar);
            Controls.Add(sudokuMenu);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            MainMenuStrip = sudokuMenu;
            MaximizeBox = false;
            Name = "SudokuForm";
            Opacity = 0D;
            FormClosing += ExitSudoku;
            MenuComplete += ActivateGrid;
            DragDrop += DropProblem;
            DragOver += DragOverForm;
            Resize += ResizeForm;
            ((System.ComponentModel.ISupportInitialize)SudokuGrid).EndInit();
            sudokuMenu.ResumeLayout(false);
            sudokuMenu.PerformLayout();
            sudokuStatusBar.ResumeLayout(false);
            sudokuStatusBar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label status;
        private System.Windows.Forms.Button next;
        private System.Windows.Forms.Button prior;
        private System.Windows.Forms.MenuStrip sudokuMenu;
        private System.Windows.Forms.ToolStripMenuItem file;
        private System.Windows.Forms.ToolStripMenuItem SudokuProblem;
        private System.Windows.Forms.ToolStripMenuItem help;
        private System.Windows.Forms.ToolStripMenuItem aboutSudoku;
        private System.Windows.Forms.ToolStripMenuItem open;
        private System.Windows.Forms.ToolStripMenuItem save;
        private System.Windows.Forms.ToolStripMenuItem exit;
        private System.Windows.Forms.ToolStripMenuItem generate;
        private System.Windows.Forms.ToolStripMenuItem solve;
        private System.Windows.Forms.ToolStripMenuItem reset;
        private System.Windows.Forms.ToolStripMenuItem options;
        private System.Windows.Forms.ToolStripMenuItem traceMode;
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
        private System.Windows.Forms.ToolStripMenuItem visitHomepage;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator9;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator10;
        private System.Windows.Forms.ToolStripMenuItem pause;
        private System.Windows.Forms.ToolStripMenuItem resetTimer;
        private System.Windows.Forms.ToolStripMenuItem EditComment;
        private SudokuBoard SudokuGrid;
        private System.Windows.Forms.ToolStripMenuItem newItem;
        private System.Windows.Forms.ToolStripMenuItem newNormalSudoku;
        private System.Windows.Forms.ToolStripMenuItem newXSudoku;
        private System.Windows.Forms.ToolStripMenuItem generateNormalSudoku;
        private System.Windows.Forms.ToolStripMenuItem generateXSudoku;
        private System.Windows.Forms.ToolStripMenuItem versionHistory;
        private System.Windows.Forms.ToolStripMenuItem clearCandidates;
        private System.Windows.Forms.ToolStripMenuItem showCellInfo;
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
        private System.Windows.Forms.ToolStripMenuItem pencilMode;
        private System.Windows.Forms.ToolStripMenuItem highlightSameValues;
    }
}