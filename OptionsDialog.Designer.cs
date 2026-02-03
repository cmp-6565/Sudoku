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
            cancel = new System.Windows.Forms.Button();
            ok = new System.Windows.Forms.Button();
            bookletSizeNew = new System.Windows.Forms.NumericUpDown();
            labelBookletSizeNew = new System.Windows.Forms.Label();
            printSolutions = new System.Windows.Forms.CheckBox();
            minValues = new System.Windows.Forms.NumericUpDown();
            labelMinValues = new System.Windows.Forms.Label();
            autoSaveBooklet = new System.Windows.Forms.CheckBox();
            labelBaseDirectory = new System.Windows.Forms.Label();
            problemDirectory = new System.Windows.Forms.TextBox();
            directorySelect = new System.Windows.Forms.Button();
            selectProblemDirectoryDialog = new System.Windows.Forms.FolderBrowserDialog();
            printHints = new System.Windows.Forms.CheckBox();
            optionsTab = new System.Windows.Forms.TabControl();
            layoutPage = new System.Windows.Forms.TabPage();
            autoPauseLag = new System.Windows.Forms.NumericUpDown();
            autoPause = new System.Windows.Forms.CheckBox();
            saveState = new System.Windows.Forms.CheckBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            possibleValuesExamplePicture = new System.Windows.Forms.PictureBox();
            useDigits = new System.Windows.Forms.RadioButton();
            useWatchHands = new System.Windows.Forms.RadioButton();
            hideWhenMinimized = new System.Windows.Forms.CheckBox();
            sizeGroupBox = new System.Windows.Forms.GroupBox();
            largeRB = new System.Windows.Forms.RadioButton();
            mediumRB = new System.Windows.Forms.RadioButton();
            smallRB = new System.Windows.Forms.RadioButton();
            labelLanguage = new System.Windows.Forms.Label();
            constrast = new System.Windows.Forms.NumericUpDown();
            labelConstrast = new System.Windows.Forms.Label();
            language = new System.Windows.Forms.ComboBox();
            printPage = new System.Windows.Forms.TabPage();
            printInternalSeverity = new System.Windows.Forms.CheckBox();
            xSudokuConstrast = new System.Windows.Forms.NumericUpDown();
            xSudokuConstrastLabel = new System.Windows.Forms.Label();
            solutionPrintSize = new System.Windows.Forms.GroupBox();
            solutionSmall = new System.Windows.Forms.RadioButton();
            solutionNormal = new System.Windows.Forms.RadioButton();
            problemPrintSize = new System.Windows.Forms.GroupBox();
            tiny = new System.Windows.Forms.RadioButton();
            small = new System.Windows.Forms.RadioButton();
            normal = new System.Windows.Forms.RadioButton();
            large = new System.Windows.Forms.RadioButton();
            generatePage = new System.Windows.Forms.TabPage();
            precalculatedProblems = new System.Windows.Forms.CheckBox();
            generateMinimalProblems = new System.Windows.Forms.CheckBox();
            selectLevelBox = new System.Windows.Forms.GroupBox();
            selectSeverityLevel = new System.Windows.Forms.CheckBox();
            type = new System.Windows.Forms.GroupBox();
            xSudoku = new System.Windows.Forms.CheckBox();
            normalSudoku = new System.Windows.Forms.CheckBox();
            severity = new System.Windows.Forms.GroupBox();
            hard = new System.Windows.Forms.CheckBox();
            intermediate = new System.Windows.Forms.CheckBox();
            easy = new System.Windows.Forms.CheckBox();
            bookletSize = new System.Windows.Forms.GroupBox();
            unlimited = new System.Windows.Forms.CheckBox();
            bookletSizeExisting = new System.Windows.Forms.NumericUpDown();
            labelBookletSizeExisting = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)bookletSizeNew).BeginInit();
            ((System.ComponentModel.ISupportInitialize)minValues).BeginInit();
            optionsTab.SuspendLayout();
            layoutPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)autoPauseLag).BeginInit();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)possibleValuesExamplePicture).BeginInit();
            sizeGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)constrast).BeginInit();
            printPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)xSudokuConstrast).BeginInit();
            solutionPrintSize.SuspendLayout();
            problemPrintSize.SuspendLayout();
            generatePage.SuspendLayout();
            selectLevelBox.SuspendLayout();
            type.SuspendLayout();
            severity.SuspendLayout();
            bookletSize.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)bookletSizeExisting).BeginInit();
            SuspendLayout();
            // 
            // cancel
            // 
            resources.ApplyResources(cancel, "cancel");
            cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancel.Name = "cancel";
            // 
            // ok
            // 
            resources.ApplyResources(ok, "ok");
            ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            ok.Name = "ok";
            ok.Click += ok_Click;
            // 
            // bookletSizeNew
            // 
            resources.ApplyResources(bookletSizeNew, "bookletSizeNew");
            bookletSizeNew.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            bookletSizeNew.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            bookletSizeNew.Name = "bookletSizeNew";
            bookletSizeNew.Value = new decimal(new int[] { 2, 0, 0, 0 });
            bookletSizeNew.ValueChanged += bookletSizeNewChanged;
            // 
            // labelBookletSizeNew
            // 
            resources.ApplyResources(labelBookletSizeNew, "labelBookletSizeNew");
            labelBookletSizeNew.Name = "labelBookletSizeNew";
            // 
            // printSolutions
            // 
            resources.ApplyResources(printSolutions, "printSolutions");
            printSolutions.Name = "printSolutions";
            // 
            // minValues
            // 
            resources.ApplyResources(minValues, "minValues");
            minValues.Maximum = new decimal(new int[] { 35, 0, 0, 0 });
            minValues.Minimum = new decimal(new int[] { 17, 0, 0, 0 });
            minValues.Name = "minValues";
            minValues.Value = new decimal(new int[] { 17, 0, 0, 0 });
            // 
            // labelMinValues
            // 
            resources.ApplyResources(labelMinValues, "labelMinValues");
            labelMinValues.Name = "labelMinValues";
            // 
            // autoSaveBooklet
            // 
            resources.ApplyResources(autoSaveBooklet, "autoSaveBooklet");
            autoSaveBooklet.Name = "autoSaveBooklet";
            // 
            // labelBaseDirectory
            // 
            resources.ApplyResources(labelBaseDirectory, "labelBaseDirectory");
            labelBaseDirectory.Name = "labelBaseDirectory";
            // 
            // problemDirectory
            // 
            resources.ApplyResources(problemDirectory, "problemDirectory");
            problemDirectory.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            problemDirectory.Name = "problemDirectory";
            problemDirectory.ReadOnly = true;
            problemDirectory.TabStop = false;
            // 
            // directorySelect
            // 
            resources.ApplyResources(directorySelect, "directorySelect");
            directorySelect.Name = "directorySelect";
            directorySelect.Click += directorySelect_Click;
            // 
            // selectProblemDirectoryDialog
            // 
            resources.ApplyResources(selectProblemDirectoryDialog, "selectProblemDirectoryDialog");
            // 
            // printHints
            // 
            resources.ApplyResources(printHints, "printHints");
            printHints.Name = "printHints";
            // 
            // optionsTab
            // 
            resources.ApplyResources(optionsTab, "optionsTab");
            optionsTab.Controls.Add(layoutPage);
            optionsTab.Controls.Add(printPage);
            optionsTab.Controls.Add(generatePage);
            optionsTab.Name = "optionsTab";
            optionsTab.SelectedIndex = 0;
            // 
            // layoutPage
            // 
            resources.ApplyResources(layoutPage, "layoutPage");
            layoutPage.BackColor = System.Drawing.SystemColors.ControlLight;
            layoutPage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            layoutPage.Controls.Add(autoPauseLag);
            layoutPage.Controls.Add(autoPause);
            layoutPage.Controls.Add(saveState);
            layoutPage.Controls.Add(groupBox1);
            layoutPage.Controls.Add(hideWhenMinimized);
            layoutPage.Controls.Add(sizeGroupBox);
            layoutPage.Controls.Add(labelLanguage);
            layoutPage.Controls.Add(constrast);
            layoutPage.Controls.Add(labelConstrast);
            layoutPage.Controls.Add(language);
            layoutPage.ForeColor = System.Drawing.SystemColors.ControlText;
            layoutPage.Name = "layoutPage";
            // 
            // autoPauseLag
            // 
            resources.ApplyResources(autoPauseLag, "autoPauseLag");
            autoPauseLag.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            autoPauseLag.Name = "autoPauseLag";
            autoPauseLag.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // autoPause
            // 
            resources.ApplyResources(autoPause, "autoPause");
            autoPause.Name = "autoPause";
            autoPause.UseVisualStyleBackColor = true;
            autoPause.CheckedChanged += autoPauseCheckedChanged;
            // 
            // saveState
            // 
            resources.ApplyResources(saveState, "saveState");
            saveState.Name = "saveState";
            saveState.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(groupBox1, "groupBox1");
            groupBox1.Controls.Add(possibleValuesExamplePicture);
            groupBox1.Controls.Add(useDigits);
            groupBox1.Controls.Add(useWatchHands);
            groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            groupBox1.Name = "groupBox1";
            groupBox1.TabStop = false;
            // 
            // possibleValuesExamplePicture
            // 
            resources.ApplyResources(possibleValuesExamplePicture, "possibleValuesExamplePicture");
            possibleValuesExamplePicture.Name = "possibleValuesExamplePicture";
            possibleValuesExamplePicture.TabStop = false;
            // 
            // useDigits
            // 
            resources.ApplyResources(useDigits, "useDigits");
            useDigits.Name = "useDigits";
            useDigits.TabStop = true;
            useDigits.UseVisualStyleBackColor = true;
            // 
            // useWatchHands
            // 
            resources.ApplyResources(useWatchHands, "useWatchHands");
            useWatchHands.Name = "useWatchHands";
            useWatchHands.TabStop = true;
            useWatchHands.UseVisualStyleBackColor = true;
            useWatchHands.CheckedChanged += exchangePicture;
            // 
            // hideWhenMinimized
            // 
            resources.ApplyResources(hideWhenMinimized, "hideWhenMinimized");
            hideWhenMinimized.Name = "hideWhenMinimized";
            hideWhenMinimized.UseVisualStyleBackColor = true;
            // 
            // sizeGroupBox
            // 
            resources.ApplyResources(sizeGroupBox, "sizeGroupBox");
            sizeGroupBox.Controls.Add(largeRB);
            sizeGroupBox.Controls.Add(mediumRB);
            sizeGroupBox.Controls.Add(smallRB);
            sizeGroupBox.ForeColor = System.Drawing.SystemColors.ControlText;
            sizeGroupBox.Name = "sizeGroupBox";
            sizeGroupBox.TabStop = false;
            // 
            // largeRB
            // 
            resources.ApplyResources(largeRB, "largeRB");
            largeRB.Name = "largeRB";
            largeRB.Tag = "3";
            // 
            // mediumRB
            // 
            resources.ApplyResources(mediumRB, "mediumRB");
            mediumRB.Name = "mediumRB";
            mediumRB.Tag = "2";
            // 
            // smallRB
            // 
            resources.ApplyResources(smallRB, "smallRB");
            smallRB.ForeColor = System.Drawing.SystemColors.ControlText;
            smallRB.Name = "smallRB";
            smallRB.Tag = "1";
            // 
            // labelLanguage
            // 
            resources.ApplyResources(labelLanguage, "labelLanguage");
            labelLanguage.Name = "labelLanguage";
            // 
            // constrast
            // 
            resources.ApplyResources(constrast, "constrast");
            constrast.Name = "constrast";
            // 
            // labelConstrast
            // 
            resources.ApplyResources(labelConstrast, "labelConstrast");
            labelConstrast.Name = "labelConstrast";
            // 
            // language
            // 
            resources.ApplyResources(language, "language");
            language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            language.Name = "language";
            language.Validating += checkUI;
            // 
            // printPage
            // 
            resources.ApplyResources(printPage, "printPage");
            printPage.BackColor = System.Drawing.SystemColors.ControlLight;
            printPage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            printPage.Controls.Add(printInternalSeverity);
            printPage.Controls.Add(xSudokuConstrast);
            printPage.Controls.Add(xSudokuConstrastLabel);
            printPage.Controls.Add(solutionPrintSize);
            printPage.Controls.Add(printSolutions);
            printPage.Controls.Add(printHints);
            printPage.Controls.Add(problemPrintSize);
            printPage.Name = "printPage";
            // 
            // printInternalSeverity
            // 
            resources.ApplyResources(printInternalSeverity, "printInternalSeverity");
            printInternalSeverity.Name = "printInternalSeverity";
            printInternalSeverity.UseVisualStyleBackColor = true;
            // 
            // xSudokuConstrast
            // 
            resources.ApplyResources(xSudokuConstrast, "xSudokuConstrast");
            xSudokuConstrast.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            xSudokuConstrast.Name = "xSudokuConstrast";
            // 
            // xSudokuConstrastLabel
            // 
            resources.ApplyResources(xSudokuConstrastLabel, "xSudokuConstrastLabel");
            xSudokuConstrastLabel.Name = "xSudokuConstrastLabel";
            // 
            // solutionPrintSize
            // 
            resources.ApplyResources(solutionPrintSize, "solutionPrintSize");
            solutionPrintSize.Controls.Add(solutionSmall);
            solutionPrintSize.Controls.Add(solutionNormal);
            solutionPrintSize.ForeColor = System.Drawing.SystemColors.ControlText;
            solutionPrintSize.Name = "solutionPrintSize";
            solutionPrintSize.TabStop = false;
            // 
            // solutionSmall
            // 
            resources.ApplyResources(solutionSmall, "solutionSmall");
            solutionSmall.Name = "solutionSmall";
            solutionSmall.TabStop = true;
            solutionSmall.Tag = "50";
            solutionSmall.UseVisualStyleBackColor = true;
            // 
            // solutionNormal
            // 
            resources.ApplyResources(solutionNormal, "solutionNormal");
            solutionNormal.Name = "solutionNormal";
            solutionNormal.TabStop = true;
            solutionNormal.Tag = "40";
            solutionNormal.UseVisualStyleBackColor = true;
            // 
            // problemPrintSize
            // 
            resources.ApplyResources(problemPrintSize, "problemPrintSize");
            problemPrintSize.Controls.Add(tiny);
            problemPrintSize.Controls.Add(small);
            problemPrintSize.Controls.Add(normal);
            problemPrintSize.Controls.Add(large);
            problemPrintSize.ForeColor = System.Drawing.SystemColors.ControlText;
            problemPrintSize.Name = "problemPrintSize";
            problemPrintSize.TabStop = false;
            // 
            // tiny
            // 
            resources.ApplyResources(tiny, "tiny");
            tiny.Name = "tiny";
            tiny.TabStop = true;
            tiny.Tag = "40";
            tiny.UseVisualStyleBackColor = true;
            // 
            // small
            // 
            resources.ApplyResources(small, "small");
            small.Name = "small";
            small.TabStop = true;
            small.Tag = "30";
            small.UseVisualStyleBackColor = true;
            // 
            // normal
            // 
            resources.ApplyResources(normal, "normal");
            normal.Name = "normal";
            normal.TabStop = true;
            normal.Tag = "20";
            normal.UseVisualStyleBackColor = true;
            // 
            // large
            // 
            resources.ApplyResources(large, "large");
            large.Name = "large";
            large.TabStop = true;
            large.Tag = "10";
            large.UseVisualStyleBackColor = true;
            // 
            // generatePage
            // 
            resources.ApplyResources(generatePage, "generatePage");
            generatePage.BackColor = System.Drawing.SystemColors.ControlLight;
            generatePage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            generatePage.Controls.Add(precalculatedProblems);
            generatePage.Controls.Add(generateMinimalProblems);
            generatePage.Controls.Add(selectLevelBox);
            generatePage.Controls.Add(type);
            generatePage.Controls.Add(severity);
            generatePage.Controls.Add(labelMinValues);
            generatePage.Controls.Add(directorySelect);
            generatePage.Controls.Add(problemDirectory);
            generatePage.Controls.Add(labelBaseDirectory);
            generatePage.Controls.Add(minValues);
            generatePage.Controls.Add(autoSaveBooklet);
            generatePage.Controls.Add(bookletSize);
            generatePage.Name = "generatePage";
            // 
            // precalculatedProblems
            // 
            resources.ApplyResources(precalculatedProblems, "precalculatedProblems");
            precalculatedProblems.Name = "precalculatedProblems";
            precalculatedProblems.UseVisualStyleBackColor = true;
            precalculatedProblems.CheckedChanged += precalculatedCheckedChanged;
            // 
            // generateMinimalProblems
            // 
            resources.ApplyResources(generateMinimalProblems, "generateMinimalProblems");
            generateMinimalProblems.Name = "generateMinimalProblems";
            generateMinimalProblems.UseVisualStyleBackColor = true;
            generateMinimalProblems.CheckedChanged += generateMinimumProblemsChanged;
            // 
            // selectLevelBox
            // 
            resources.ApplyResources(selectLevelBox, "selectLevelBox");
            selectLevelBox.Controls.Add(selectSeverityLevel);
            selectLevelBox.Name = "selectLevelBox";
            selectLevelBox.TabStop = false;
            // 
            // selectSeverityLevel
            // 
            resources.ApplyResources(selectSeverityLevel, "selectSeverityLevel");
            selectSeverityLevel.Name = "selectSeverityLevel";
            selectSeverityLevel.UseVisualStyleBackColor = true;
            // 
            // type
            // 
            resources.ApplyResources(type, "type");
            type.Controls.Add(xSudoku);
            type.Controls.Add(normalSudoku);
            type.ForeColor = System.Drawing.SystemColors.ControlText;
            type.Name = "type";
            type.TabStop = false;
            // 
            // xSudoku
            // 
            resources.ApplyResources(xSudoku, "xSudoku");
            xSudoku.Name = "xSudoku";
            xSudoku.UseVisualStyleBackColor = true;
            xSudoku.CheckedChanged += sudokuTypeCheckedChanged;
            // 
            // normalSudoku
            // 
            resources.ApplyResources(normalSudoku, "normalSudoku");
            normalSudoku.Name = "normalSudoku";
            normalSudoku.UseVisualStyleBackColor = true;
            normalSudoku.CheckedChanged += sudokuTypeCheckedChanged;
            // 
            // severity
            // 
            resources.ApplyResources(severity, "severity");
            severity.Controls.Add(hard);
            severity.Controls.Add(intermediate);
            severity.Controls.Add(easy);
            severity.ForeColor = System.Drawing.SystemColors.ControlText;
            severity.Name = "severity";
            severity.TabStop = false;
            // 
            // hard
            // 
            resources.ApplyResources(hard, "hard");
            hard.Name = "hard";
            hard.UseVisualStyleBackColor = true;
            hard.CheckedChanged += severityLevelCheckedChanged;
            // 
            // intermediate
            // 
            resources.ApplyResources(intermediate, "intermediate");
            intermediate.Name = "intermediate";
            intermediate.UseVisualStyleBackColor = true;
            intermediate.CheckedChanged += severityLevelCheckedChanged;
            // 
            // easy
            // 
            resources.ApplyResources(easy, "easy");
            easy.Name = "easy";
            easy.UseVisualStyleBackColor = true;
            easy.CheckedChanged += severityLevelCheckedChanged;
            // 
            // bookletSize
            // 
            resources.ApplyResources(bookletSize, "bookletSize");
            bookletSize.Controls.Add(unlimited);
            bookletSize.Controls.Add(bookletSizeExisting);
            bookletSize.Controls.Add(labelBookletSizeNew);
            bookletSize.Controls.Add(labelBookletSizeExisting);
            bookletSize.Controls.Add(bookletSizeNew);
            bookletSize.ForeColor = System.Drawing.SystemColors.ControlText;
            bookletSize.Name = "bookletSize";
            bookletSize.TabStop = false;
            // 
            // unlimited
            // 
            resources.ApplyResources(unlimited, "unlimited");
            unlimited.Checked = true;
            unlimited.CheckState = System.Windows.Forms.CheckState.Checked;
            unlimited.Name = "unlimited";
            unlimited.UseVisualStyleBackColor = true;
            unlimited.CheckedChanged += unlimitedCheckedChanged;
            // 
            // bookletSizeExisting
            // 
            resources.ApplyResources(bookletSizeExisting, "bookletSizeExisting");
            bookletSizeExisting.Maximum = new decimal(new int[] { 99999999, 0, 0, 0 });
            bookletSizeExisting.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            bookletSizeExisting.Name = "bookletSizeExisting";
            bookletSizeExisting.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // labelBookletSizeExisting
            // 
            resources.ApplyResources(labelBookletSizeExisting, "labelBookletSizeExisting");
            labelBookletSizeExisting.Name = "labelBookletSizeExisting";
            // 
            // OptionsDialog
            // 
            AcceptButton = ok;
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            CancelButton = cancel;
            ControlBox = false;
            Controls.Add(optionsTab);
            Controls.Add(ok);
            Controls.Add(cancel);
            ForeColor = System.Drawing.SystemColors.ControlText;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OptionsDialog";
            ((System.ComponentModel.ISupportInitialize)bookletSizeNew).EndInit();
            ((System.ComponentModel.ISupportInitialize)minValues).EndInit();
            optionsTab.ResumeLayout(false);
            layoutPage.ResumeLayout(false);
            layoutPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)autoPauseLag).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)possibleValuesExamplePicture).EndInit();
            sizeGroupBox.ResumeLayout(false);
            sizeGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)constrast).EndInit();
            printPage.ResumeLayout(false);
            printPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)xSudokuConstrast).EndInit();
            solutionPrintSize.ResumeLayout(false);
            solutionPrintSize.PerformLayout();
            problemPrintSize.ResumeLayout(false);
            problemPrintSize.PerformLayout();
            generatePage.ResumeLayout(false);
            generatePage.PerformLayout();
            selectLevelBox.ResumeLayout(false);
            selectLevelBox.PerformLayout();
            type.ResumeLayout(false);
            type.PerformLayout();
            severity.ResumeLayout(false);
            severity.PerformLayout();
            bookletSize.ResumeLayout(false);
            bookletSize.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)bookletSizeExisting).EndInit();
            ResumeLayout(false);

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