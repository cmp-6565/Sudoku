namespace Sudoku
{
	partial class OptionsDialog
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
			if (disposing && (components != null))
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsDialog));
            this.cancel = new System.Windows.Forms.Button();
            this.ok = new System.Windows.Forms.Button();
            this.bookletSizeNew = new System.Windows.Forms.NumericUpDown();
            this.labelBookletSizeNew = new System.Windows.Forms.Label();
            this.printSolutions = new System.Windows.Forms.CheckBox();
            this.minValues = new System.Windows.Forms.NumericUpDown();
            this.labelMinValues = new System.Windows.Forms.Label();
            this.autoSaveBooklet = new System.Windows.Forms.CheckBox();
            this.labelBaseDirectory = new System.Windows.Forms.Label();
            this.problemDirectory = new System.Windows.Forms.TextBox();
            this.directorySelect = new System.Windows.Forms.Button();
            this.selectProblemDirectoryDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.printHints = new System.Windows.Forms.CheckBox();
            this.optionsTab = new System.Windows.Forms.TabControl();
            this.layoutPage = new System.Windows.Forms.TabPage();
            this.autoPauseLag = new System.Windows.Forms.NumericUpDown();
            this.autoPause = new System.Windows.Forms.CheckBox();
            this.saveState = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.possibleValuesExamplePicture = new System.Windows.Forms.PictureBox();
            this.useDigits = new System.Windows.Forms.RadioButton();
            this.useWatchHands = new System.Windows.Forms.RadioButton();
            this.hideWhenMinimized = new System.Windows.Forms.CheckBox();
            this.sizeGroupBox = new System.Windows.Forms.GroupBox();
            this.largeRB = new System.Windows.Forms.RadioButton();
            this.mediumRB = new System.Windows.Forms.RadioButton();
            this.smallRB = new System.Windows.Forms.RadioButton();
            this.labelLanguage = new System.Windows.Forms.Label();
            this.constrast = new System.Windows.Forms.NumericUpDown();
            this.labelConstrast = new System.Windows.Forms.Label();
            this.language = new System.Windows.Forms.ComboBox();
            this.printPage = new System.Windows.Forms.TabPage();
            this.printInternalSeverity = new System.Windows.Forms.CheckBox();
            this.xSudokuConstrast = new System.Windows.Forms.NumericUpDown();
            this.xSudokuConstrastLabel = new System.Windows.Forms.Label();
            this.solutionPrintSize = new System.Windows.Forms.GroupBox();
            this.solutionSmall = new System.Windows.Forms.RadioButton();
            this.solutionNormal = new System.Windows.Forms.RadioButton();
            this.problemPrintSize = new System.Windows.Forms.GroupBox();
            this.tiny = new System.Windows.Forms.RadioButton();
            this.small = new System.Windows.Forms.RadioButton();
            this.normal = new System.Windows.Forms.RadioButton();
            this.large = new System.Windows.Forms.RadioButton();
            this.generatePage = new System.Windows.Forms.TabPage();
            this.precalculatedProblems = new System.Windows.Forms.CheckBox();
            this.generateMinimalProblems = new System.Windows.Forms.CheckBox();
            this.selectLevelBox = new System.Windows.Forms.GroupBox();
            this.selectSeverityLevel = new System.Windows.Forms.CheckBox();
            this.type = new System.Windows.Forms.GroupBox();
            this.xSudoku = new System.Windows.Forms.CheckBox();
            this.normalSudoku = new System.Windows.Forms.CheckBox();
            this.severity = new System.Windows.Forms.GroupBox();
            this.hard = new System.Windows.Forms.CheckBox();
            this.intermediate = new System.Windows.Forms.CheckBox();
            this.easy = new System.Windows.Forms.CheckBox();
            this.bookletSize = new System.Windows.Forms.GroupBox();
            this.unlimited = new System.Windows.Forms.CheckBox();
            this.bookletSizeExisting = new System.Windows.Forms.NumericUpDown();
            this.labelBookletSizeExisting = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.bookletSizeNew)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.minValues)).BeginInit();
            this.optionsTab.SuspendLayout();
            this.layoutPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.autoPauseLag)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.possibleValuesExamplePicture)).BeginInit();
            this.sizeGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.constrast)).BeginInit();
            this.printPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.xSudokuConstrast)).BeginInit();
            this.solutionPrintSize.SuspendLayout();
            this.problemPrintSize.SuspendLayout();
            this.generatePage.SuspendLayout();
            this.selectLevelBox.SuspendLayout();
            this.type.SuspendLayout();
            this.severity.SuspendLayout();
            this.bookletSize.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bookletSizeExisting)).BeginInit();
            this.SuspendLayout();
            // 
            // cancel
            // 
            resources.ApplyResources(this.cancel, "cancel");
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Name = "cancel";
            // 
            // ok
            // 
            resources.ApplyResources(this.ok, "ok");
            this.ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ok.Name = "ok";
            this.ok.Click += new System.EventHandler(this.ok_Click);
            // 
            // bookletSizeNew
            // 
            resources.ApplyResources(this.bookletSizeNew, "bookletSizeNew");
            this.bookletSizeNew.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.bookletSizeNew.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.bookletSizeNew.Name = "bookletSizeNew";
            this.bookletSizeNew.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.bookletSizeNew.ValueChanged += new System.EventHandler(this.bookletSizeNewChanged);
            // 
            // labelBookletSizeNew
            // 
            resources.ApplyResources(this.labelBookletSizeNew, "labelBookletSizeNew");
            this.labelBookletSizeNew.Name = "labelBookletSizeNew";
            // 
            // printSolutions
            // 
            resources.ApplyResources(this.printSolutions, "printSolutions");
            this.printSolutions.Name = "printSolutions";
            // 
            // minValues
            // 
            resources.ApplyResources(this.minValues, "minValues");
            this.minValues.Maximum = new decimal(new int[] {
            35,
            0,
            0,
            0});
            this.minValues.Minimum = new decimal(new int[] {
            17,
            0,
            0,
            0});
            this.minValues.Name = "minValues";
            this.minValues.Value = new decimal(new int[] {
            17,
            0,
            0,
            0});
            // 
            // labelMinValues
            // 
            resources.ApplyResources(this.labelMinValues, "labelMinValues");
            this.labelMinValues.Name = "labelMinValues";
            // 
            // autoSaveBooklet
            // 
            resources.ApplyResources(this.autoSaveBooklet, "autoSaveBooklet");
            this.autoSaveBooklet.Name = "autoSaveBooklet";
            // 
            // labelBaseDirectory
            // 
            resources.ApplyResources(this.labelBaseDirectory, "labelBaseDirectory");
            this.labelBaseDirectory.Name = "labelBaseDirectory";
            // 
            // problemDirectory
            // 
            resources.ApplyResources(this.problemDirectory, "problemDirectory");
            this.problemDirectory.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.problemDirectory.Name = "problemDirectory";
            this.problemDirectory.ReadOnly = true;
            this.problemDirectory.TabStop = false;
            // 
            // directorySelect
            // 
            resources.ApplyResources(this.directorySelect, "directorySelect");
            this.directorySelect.Name = "directorySelect";
            this.directorySelect.Click += new System.EventHandler(this.directorySelect_Click);
            // 
            // selectProblemDirectoryDialog
            // 
            resources.ApplyResources(this.selectProblemDirectoryDialog, "selectProblemDirectoryDialog");
            // 
            // printHints
            // 
            resources.ApplyResources(this.printHints, "printHints");
            this.printHints.Name = "printHints";
            // 
            // optionsTab
            // 
            resources.ApplyResources(this.optionsTab, "optionsTab");
            this.optionsTab.Controls.Add(this.layoutPage);
            this.optionsTab.Controls.Add(this.printPage);
            this.optionsTab.Controls.Add(this.generatePage);
            this.optionsTab.Name = "optionsTab";
            this.optionsTab.SelectedIndex = 0;
            // 
            // layoutPage
            // 
            resources.ApplyResources(this.layoutPage, "layoutPage");
            this.layoutPage.BackColor = System.Drawing.SystemColors.ControlLight;
            this.layoutPage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.layoutPage.Controls.Add(this.autoPauseLag);
            this.layoutPage.Controls.Add(this.autoPause);
            this.layoutPage.Controls.Add(this.saveState);
            this.layoutPage.Controls.Add(this.groupBox1);
            this.layoutPage.Controls.Add(this.hideWhenMinimized);
            this.layoutPage.Controls.Add(this.sizeGroupBox);
            this.layoutPage.Controls.Add(this.labelLanguage);
            this.layoutPage.Controls.Add(this.constrast);
            this.layoutPage.Controls.Add(this.labelConstrast);
            this.layoutPage.Controls.Add(this.language);
            this.layoutPage.ForeColor = System.Drawing.SystemColors.ControlText;
            this.layoutPage.Name = "layoutPage";
            // 
            // autoPauseLag
            // 
            resources.ApplyResources(this.autoPauseLag, "autoPauseLag");
            this.autoPauseLag.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.autoPauseLag.Name = "autoPauseLag";
            this.autoPauseLag.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // autoPause
            // 
            resources.ApplyResources(this.autoPause, "autoPause");
            this.autoPause.Name = "autoPause";
            this.autoPause.UseVisualStyleBackColor = true;
            this.autoPause.CheckedChanged += new System.EventHandler(this.autoPauseCheckedChanged);
            // 
            // saveState
            // 
            resources.ApplyResources(this.saveState, "saveState");
            this.saveState.Name = "saveState";
            this.saveState.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.possibleValuesExamplePicture);
            this.groupBox1.Controls.Add(this.useDigits);
            this.groupBox1.Controls.Add(this.useWatchHands);
            this.groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // possibleValuesExamplePicture
            // 
            resources.ApplyResources(this.possibleValuesExamplePicture, "possibleValuesExamplePicture");
            this.possibleValuesExamplePicture.Name = "possibleValuesExamplePicture";
            this.possibleValuesExamplePicture.TabStop = false;
            // 
            // useDigits
            // 
            resources.ApplyResources(this.useDigits, "useDigits");
            this.useDigits.Name = "useDigits";
            this.useDigits.TabStop = true;
            this.useDigits.UseVisualStyleBackColor = true;
            // 
            // useWatchHands
            // 
            resources.ApplyResources(this.useWatchHands, "useWatchHands");
            this.useWatchHands.Name = "useWatchHands";
            this.useWatchHands.TabStop = true;
            this.useWatchHands.UseVisualStyleBackColor = true;
            this.useWatchHands.CheckedChanged += new System.EventHandler(this.exchangePicture);
            // 
            // hideWhenMinimized
            // 
            resources.ApplyResources(this.hideWhenMinimized, "hideWhenMinimized");
            this.hideWhenMinimized.Name = "hideWhenMinimized";
            this.hideWhenMinimized.UseVisualStyleBackColor = true;
            // 
            // sizeGroupBox
            // 
            resources.ApplyResources(this.sizeGroupBox, "sizeGroupBox");
            this.sizeGroupBox.Controls.Add(this.largeRB);
            this.sizeGroupBox.Controls.Add(this.mediumRB);
            this.sizeGroupBox.Controls.Add(this.smallRB);
            this.sizeGroupBox.ForeColor = System.Drawing.SystemColors.ControlText;
            this.sizeGroupBox.Name = "sizeGroupBox";
            this.sizeGroupBox.TabStop = false;
            // 
            // largeRB
            // 
            resources.ApplyResources(this.largeRB, "largeRB");
            this.largeRB.Name = "largeRB";
            this.largeRB.Tag = "3";
            // 
            // mediumRB
            // 
            resources.ApplyResources(this.mediumRB, "mediumRB");
            this.mediumRB.Name = "mediumRB";
            this.mediumRB.Tag = "2";
            // 
            // smallRB
            // 
            resources.ApplyResources(this.smallRB, "smallRB");
            this.smallRB.ForeColor = System.Drawing.SystemColors.ControlText;
            this.smallRB.Name = "smallRB";
            this.smallRB.Tag = "1";
            // 
            // labelLanguage
            // 
            resources.ApplyResources(this.labelLanguage, "labelLanguage");
            this.labelLanguage.Name = "labelLanguage";
            // 
            // constrast
            // 
            resources.ApplyResources(this.constrast, "constrast");
            this.constrast.Name = "constrast";
            // 
            // labelConstrast
            // 
            resources.ApplyResources(this.labelConstrast, "labelConstrast");
            this.labelConstrast.Name = "labelConstrast";
            // 
            // language
            // 
            resources.ApplyResources(this.language, "language");
            this.language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.language.Name = "language";
            this.language.Validating += new System.ComponentModel.CancelEventHandler(this.checkUI);
            // 
            // printPage
            // 
            resources.ApplyResources(this.printPage, "printPage");
            this.printPage.BackColor = System.Drawing.SystemColors.ControlLight;
            this.printPage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.printPage.Controls.Add(this.printInternalSeverity);
            this.printPage.Controls.Add(this.xSudokuConstrast);
            this.printPage.Controls.Add(this.xSudokuConstrastLabel);
            this.printPage.Controls.Add(this.solutionPrintSize);
            this.printPage.Controls.Add(this.printSolutions);
            this.printPage.Controls.Add(this.printHints);
            this.printPage.Controls.Add(this.problemPrintSize);
            this.printPage.Name = "printPage";
            // 
            // printInternalSeverity
            // 
            resources.ApplyResources(this.printInternalSeverity, "printInternalSeverity");
            this.printInternalSeverity.Name = "printInternalSeverity";
            this.printInternalSeverity.UseVisualStyleBackColor = true;
            // 
            // xSudokuConstrast
            // 
            resources.ApplyResources(this.xSudokuConstrast, "xSudokuConstrast");
            this.xSudokuConstrast.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.xSudokuConstrast.Name = "xSudokuConstrast";
            // 
            // xSudokuConstrastLabel
            // 
            resources.ApplyResources(this.xSudokuConstrastLabel, "xSudokuConstrastLabel");
            this.xSudokuConstrastLabel.Name = "xSudokuConstrastLabel";
            // 
            // solutionPrintSize
            // 
            resources.ApplyResources(this.solutionPrintSize, "solutionPrintSize");
            this.solutionPrintSize.Controls.Add(this.solutionSmall);
            this.solutionPrintSize.Controls.Add(this.solutionNormal);
            this.solutionPrintSize.ForeColor = System.Drawing.SystemColors.ControlText;
            this.solutionPrintSize.Name = "solutionPrintSize";
            this.solutionPrintSize.TabStop = false;
            // 
            // solutionSmall
            // 
            resources.ApplyResources(this.solutionSmall, "solutionSmall");
            this.solutionSmall.Name = "solutionSmall";
            this.solutionSmall.TabStop = true;
            this.solutionSmall.Tag = "50";
            this.solutionSmall.UseVisualStyleBackColor = true;
            // 
            // solutionNormal
            // 
            resources.ApplyResources(this.solutionNormal, "solutionNormal");
            this.solutionNormal.Name = "solutionNormal";
            this.solutionNormal.TabStop = true;
            this.solutionNormal.Tag = "40";
            this.solutionNormal.UseVisualStyleBackColor = true;
            // 
            // problemPrintSize
            // 
            resources.ApplyResources(this.problemPrintSize, "problemPrintSize");
            this.problemPrintSize.Controls.Add(this.tiny);
            this.problemPrintSize.Controls.Add(this.small);
            this.problemPrintSize.Controls.Add(this.normal);
            this.problemPrintSize.Controls.Add(this.large);
            this.problemPrintSize.ForeColor = System.Drawing.SystemColors.ControlText;
            this.problemPrintSize.Name = "problemPrintSize";
            this.problemPrintSize.TabStop = false;
            // 
            // tiny
            // 
            resources.ApplyResources(this.tiny, "tiny");
            this.tiny.Name = "tiny";
            this.tiny.TabStop = true;
            this.tiny.Tag = "40";
            this.tiny.UseVisualStyleBackColor = true;
            // 
            // small
            // 
            resources.ApplyResources(this.small, "small");
            this.small.Name = "small";
            this.small.TabStop = true;
            this.small.Tag = "30";
            this.small.UseVisualStyleBackColor = true;
            // 
            // normal
            // 
            resources.ApplyResources(this.normal, "normal");
            this.normal.Name = "normal";
            this.normal.TabStop = true;
            this.normal.Tag = "20";
            this.normal.UseVisualStyleBackColor = true;
            // 
            // large
            // 
            resources.ApplyResources(this.large, "large");
            this.large.Name = "large";
            this.large.TabStop = true;
            this.large.Tag = "10";
            this.large.UseVisualStyleBackColor = true;
            // 
            // generatePage
            // 
            resources.ApplyResources(this.generatePage, "generatePage");
            this.generatePage.BackColor = System.Drawing.SystemColors.ControlLight;
            this.generatePage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.generatePage.Controls.Add(this.precalculatedProblems);
            this.generatePage.Controls.Add(this.generateMinimalProblems);
            this.generatePage.Controls.Add(this.selectLevelBox);
            this.generatePage.Controls.Add(this.type);
            this.generatePage.Controls.Add(this.severity);
            this.generatePage.Controls.Add(this.labelMinValues);
            this.generatePage.Controls.Add(this.directorySelect);
            this.generatePage.Controls.Add(this.problemDirectory);
            this.generatePage.Controls.Add(this.labelBaseDirectory);
            this.generatePage.Controls.Add(this.minValues);
            this.generatePage.Controls.Add(this.autoSaveBooklet);
            this.generatePage.Controls.Add(this.bookletSize);
            this.generatePage.Name = "generatePage";
            // 
            // precalculatedProblems
            // 
            resources.ApplyResources(this.precalculatedProblems, "precalculatedProblems");
            this.precalculatedProblems.Name = "precalculatedProblems";
            this.precalculatedProblems.UseVisualStyleBackColor = true;
            this.precalculatedProblems.CheckedChanged += new System.EventHandler(this.precalculatedCheckedChanged);
            // 
            // generateMinimalProblems
            // 
            resources.ApplyResources(this.generateMinimalProblems, "generateMinimalProblems");
            this.generateMinimalProblems.Name = "generateMinimalProblems";
            this.generateMinimalProblems.UseVisualStyleBackColor = true;
            this.generateMinimalProblems.CheckedChanged += new System.EventHandler(this.generateMinimumProblemsChanged);
            // 
            // selectLevelBox
            // 
            resources.ApplyResources(this.selectLevelBox, "selectLevelBox");
            this.selectLevelBox.Controls.Add(this.selectSeverityLevel);
            this.selectLevelBox.Name = "selectLevelBox";
            this.selectLevelBox.TabStop = false;
            // 
            // selectSeverityLevel
            // 
            resources.ApplyResources(this.selectSeverityLevel, "selectSeverityLevel");
            this.selectSeverityLevel.Name = "selectSeverityLevel";
            this.selectSeverityLevel.UseVisualStyleBackColor = true;
            // 
            // type
            // 
            resources.ApplyResources(this.type, "type");
            this.type.Controls.Add(this.xSudoku);
            this.type.Controls.Add(this.normalSudoku);
            this.type.ForeColor = System.Drawing.SystemColors.ControlText;
            this.type.Name = "type";
            this.type.TabStop = false;
            // 
            // xSudoku
            // 
            resources.ApplyResources(this.xSudoku, "xSudoku");
            this.xSudoku.Name = "xSudoku";
            this.xSudoku.UseVisualStyleBackColor = true;
            this.xSudoku.CheckedChanged += new System.EventHandler(this.sudokuTypeCheckedChanged);
            // 
            // normalSudoku
            // 
            resources.ApplyResources(this.normalSudoku, "normalSudoku");
            this.normalSudoku.Name = "normalSudoku";
            this.normalSudoku.UseVisualStyleBackColor = true;
            this.normalSudoku.CheckedChanged += new System.EventHandler(this.sudokuTypeCheckedChanged);
            // 
            // severity
            // 
            resources.ApplyResources(this.severity, "severity");
            this.severity.Controls.Add(this.hard);
            this.severity.Controls.Add(this.intermediate);
            this.severity.Controls.Add(this.easy);
            this.severity.ForeColor = System.Drawing.SystemColors.ControlText;
            this.severity.Name = "severity";
            this.severity.TabStop = false;
            // 
            // hard
            // 
            resources.ApplyResources(this.hard, "hard");
            this.hard.Name = "hard";
            this.hard.UseVisualStyleBackColor = true;
            this.hard.CheckedChanged += new System.EventHandler(this.severityLevelCheckedChanged);
            // 
            // intermediate
            // 
            resources.ApplyResources(this.intermediate, "intermediate");
            this.intermediate.Name = "intermediate";
            this.intermediate.UseVisualStyleBackColor = true;
            this.intermediate.CheckedChanged += new System.EventHandler(this.severityLevelCheckedChanged);
            // 
            // easy
            // 
            resources.ApplyResources(this.easy, "easy");
            this.easy.Name = "easy";
            this.easy.UseVisualStyleBackColor = true;
            this.easy.CheckedChanged += new System.EventHandler(this.severityLevelCheckedChanged);
            // 
            // bookletSize
            // 
            resources.ApplyResources(this.bookletSize, "bookletSize");
            this.bookletSize.Controls.Add(this.unlimited);
            this.bookletSize.Controls.Add(this.bookletSizeExisting);
            this.bookletSize.Controls.Add(this.labelBookletSizeNew);
            this.bookletSize.Controls.Add(this.labelBookletSizeExisting);
            this.bookletSize.Controls.Add(this.bookletSizeNew);
            this.bookletSize.ForeColor = System.Drawing.SystemColors.ControlText;
            this.bookletSize.Name = "bookletSize";
            this.bookletSize.TabStop = false;
            // 
            // unlimited
            // 
            resources.ApplyResources(this.unlimited, "unlimited");
            this.unlimited.Checked = true;
            this.unlimited.CheckState = System.Windows.Forms.CheckState.Checked;
            this.unlimited.Name = "unlimited";
            this.unlimited.UseVisualStyleBackColor = true;
            this.unlimited.CheckedChanged += new System.EventHandler(this.unlimitedCheckedChanged);
            // 
            // bookletSizeExisting
            // 
            resources.ApplyResources(this.bookletSizeExisting, "bookletSizeExisting");
            this.bookletSizeExisting.Maximum = new decimal(new int[] {
            99999999,
            0,
            0,
            0});
            this.bookletSizeExisting.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.bookletSizeExisting.Name = "bookletSizeExisting";
            this.bookletSizeExisting.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // labelBookletSizeExisting
            // 
            resources.ApplyResources(this.labelBookletSizeExisting, "labelBookletSizeExisting");
            this.labelBookletSizeExisting.Name = "labelBookletSizeExisting";
            // 
            // OptionsDialog
            // 
            this.AcceptButton = this.ok;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.CancelButton = this.cancel;
            this.ControlBox = false;
            this.Controls.Add(this.optionsTab);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.cancel);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionsDialog";
            ((System.ComponentModel.ISupportInitialize)(this.bookletSizeNew)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.minValues)).EndInit();
            this.optionsTab.ResumeLayout(false);
            this.layoutPage.ResumeLayout(false);
            this.layoutPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.autoPauseLag)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.possibleValuesExamplePicture)).EndInit();
            this.sizeGroupBox.ResumeLayout(false);
            this.sizeGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.constrast)).EndInit();
            this.printPage.ResumeLayout(false);
            this.printPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.xSudokuConstrast)).EndInit();
            this.solutionPrintSize.ResumeLayout(false);
            this.solutionPrintSize.PerformLayout();
            this.problemPrintSize.ResumeLayout(false);
            this.problemPrintSize.PerformLayout();
            this.generatePage.ResumeLayout(false);
            this.generatePage.PerformLayout();
            this.selectLevelBox.ResumeLayout(false);
            this.selectLevelBox.PerformLayout();
            this.type.ResumeLayout(false);
            this.type.PerformLayout();
            this.severity.ResumeLayout(false);
            this.severity.PerformLayout();
            this.bookletSize.ResumeLayout(false);
            this.bookletSize.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bookletSizeExisting)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button cancel;
		private System.Windows.Forms.Button ok;
		private System.Windows.Forms.NumericUpDown bookletSizeNew;
		private System.Windows.Forms.Label labelBookletSizeNew;
		private System.Windows.Forms.CheckBox printSolutions;
		private System.Windows.Forms.NumericUpDown minValues;
		private System.Windows.Forms.Label labelMinValues;
		private System.Windows.Forms.CheckBox autoSaveBooklet;
		private System.Windows.Forms.Label labelBaseDirectory;
		private System.Windows.Forms.TextBox problemDirectory;
        private System.Windows.Forms.Button directorySelect;
        private System.Windows.Forms.FolderBrowserDialog selectProblemDirectoryDialog;
        private System.Windows.Forms.CheckBox printHints;
        private System.Windows.Forms.TabControl optionsTab;
        private System.Windows.Forms.TabPage layoutPage;
        private System.Windows.Forms.GroupBox sizeGroupBox;
        private System.Windows.Forms.RadioButton largeRB;
        private System.Windows.Forms.RadioButton mediumRB;
        private System.Windows.Forms.RadioButton smallRB;
        private System.Windows.Forms.Label labelLanguage;
        private System.Windows.Forms.NumericUpDown constrast;
        private System.Windows.Forms.Label labelConstrast;
        private System.Windows.Forms.ComboBox language;
        private System.Windows.Forms.TabPage printPage;
        private System.Windows.Forms.TabPage generatePage;
        private System.Windows.Forms.GroupBox problemPrintSize;
        private System.Windows.Forms.RadioButton large;
        private System.Windows.Forms.GroupBox solutionPrintSize;
        private System.Windows.Forms.RadioButton solutionSmall;
        private System.Windows.Forms.RadioButton solutionNormal;
        private System.Windows.Forms.RadioButton tiny;
        private System.Windows.Forms.RadioButton small;
        private System.Windows.Forms.RadioButton normal;
        private System.Windows.Forms.NumericUpDown bookletSizeExisting;
        private System.Windows.Forms.Label labelBookletSizeExisting;
        private System.Windows.Forms.CheckBox unlimited;
        private System.Windows.Forms.GroupBox bookletSize;
        private System.Windows.Forms.GroupBox severity;
        private System.Windows.Forms.CheckBox easy;
        private System.Windows.Forms.CheckBox hard;
        private System.Windows.Forms.CheckBox intermediate;
        private System.Windows.Forms.CheckBox hideWhenMinimized;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton useDigits;
        private System.Windows.Forms.RadioButton useWatchHands;
        private System.Windows.Forms.PictureBox possibleValuesExamplePicture;
        private System.Windows.Forms.GroupBox type;
        private System.Windows.Forms.CheckBox xSudoku;
        private System.Windows.Forms.CheckBox normalSudoku;
        private System.Windows.Forms.GroupBox selectLevelBox;
        private System.Windows.Forms.CheckBox selectSeverityLevel;
        private System.Windows.Forms.NumericUpDown xSudokuConstrast;
        private System.Windows.Forms.Label xSudokuConstrastLabel;
        private System.Windows.Forms.CheckBox saveState;
        private System.Windows.Forms.CheckBox generateMinimalProblems;
        private System.Windows.Forms.CheckBox precalculatedProblems;
        private System.Windows.Forms.CheckBox printInternalSeverity;
        private System.Windows.Forms.CheckBox autoPause;
        private System.Windows.Forms.NumericUpDown autoPauseLag;
    }
}