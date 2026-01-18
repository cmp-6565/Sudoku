using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    public partial class OptionsDialog: Form
    {
        String[] supportedCultures;
        String[] supportedGridSizes;
        String[] supportedSolutionGridSizes;

        public OptionsDialog()
        {
            supportedCultures = Settings.Default.SupportedCultures.Split('|');
            supportedGridSizes = Settings.Default.HorizontalProblemsAlternatives.Split('|');
            supportedSolutionGridSizes = Settings.Default.HorizontalSolutionsAlternatives.Split('|');
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Settings.Default.DisplayLanguage);

            InitializeComponent();

            hard.Text = Resources.Hard;
            easy.Text = Resources.Easy;
            intermediate.Text = Resources.Intermediate;
            minValues.Maximum = Settings.Default.MaxValues;

            bookletSizeNew.Value = Settings.Default.BookletSizeNew;
            bookletSizeExisting.Value = Settings.Default.BookletSizeExisting;
            bookletSizeExisting.Enabled = !Settings.Default.BookletSizeUnlimited;
            unlimited.Checked = Settings.Default.BookletSizeUnlimited;
            minValues.Value = Settings.Default.MinValues;
            constrast.Value = Settings.Default.Contrast;
            xSudokuConstrast.Value = Settings.Default.XSudokuConstrast;
            problemDirectory.Text = Settings.Default.ProblemDirectory;
            printSolutions.Checked = Settings.Default.PrintSolution;
            printInternalSeverity.Checked = Settings.Default.PrintInternalSeverity;
            autoSaveBooklet.Checked = Settings.Default.AutoSaveBooklet;
            printHints.Checked = Settings.Default.PrintHints;
            hideWhenMinimized.Checked = Settings.Default.HideWhenMinimized;
            saveState.Checked = Settings.Default.AutoSaveState;
            generateMinimalProblems.Checked = Settings.Default.GenerateMinimalProblems;
            minValues.Enabled = !generateMinimalProblems.Checked;
            precalculatedProblems.Checked = Settings.Default.UsePrecalculatedProblems;
            autoPause.Checked = Settings.Default.AutoPause;
            autoPauseLag.Value = Settings.Default.AutoPauseLag;
            autoPauseLag.Enabled = autoPause.Checked;

            // The severity level "trivial" (1) is not handled
            if(Settings.Default.SeverityLevel == 0) Settings.Default.SeverityLevel = 15;
            hard.Checked = (Settings.Default.SeverityLevel & 8) != 0;
            intermediate.Checked = (Settings.Default.SeverityLevel & 4) != 0;
            easy.Checked = (Settings.Default.SeverityLevel & 2) != 0;
            selectSeverityLevel.Checked = Settings.Default.SelectSeverity;

            normalSudoku.Checked = Settings.Default.GenerateNormalSudoku;
            xSudoku.Checked = Settings.Default.GenerateXSudoku;

            foreach(RadioButton rb in sizeGroupBox.Controls)
                rb.Checked = (rb.Tag.ToString() == Settings.Default.Size.ToString());

            useWatchHands.Checked = Settings.Default.UseWatchHandHints;
            useDigits.Checked = !useWatchHands.Checked;
            possibleValuesExamplePicture.Image = useWatchHands.Checked ? Resources.watchHandCandidates : Resources.digitCandidates;

            int i = supportedGridSizes.Length;
            foreach(RadioButton rb in problemPrintSize.Controls)
                rb.Checked = (supportedGridSizes[--i].ToString() == Settings.Default.HorizontalProblems.ToString());

            i = supportedSolutionGridSizes.Length;
            foreach(RadioButton rb in solutionPrintSize.Controls)
                rb.Checked = (supportedSolutionGridSizes[--i].ToString() == Settings.Default.HorizontalSolutions.ToString());

            for(i = 0; i < supportedCultures.Length; i++)
                language.Items.Add(CultureInfo.GetCultureInfoByIetfLanguageTag(supportedCultures[i]).DisplayName);
            language.Text = CultureInfo.GetCultureInfoByIetfLanguageTag(Settings.Default.DisplayLanguage).DisplayName;
        }

        public int MinBookletSize
        {
            set
            {
                bookletSizeNew.Minimum = value;
            }
        }

        private void directorySelect_Click(object sender, EventArgs e)
        {
            selectProblemDirectoryDialog.SelectedPath = problemDirectory.Text;
            if(selectProblemDirectoryDialog.ShowDialog() == DialogResult.OK)
                problemDirectory.Text = selectProblemDirectoryDialog.SelectedPath;
        }

        private void ok_Click(object sender, EventArgs e)
        {
            Settings.Default.BookletSizeNew = (int)bookletSizeNew.Value;
            Settings.Default.MinValues = (int)minValues.Value;
            Settings.Default.Contrast = (int)constrast.Value;
            Settings.Default.XSudokuConstrast = (int)xSudokuConstrast.Value;
            Settings.Default.ProblemDirectory = problemDirectory.Text;
            Settings.Default.PrintSolution = printSolutions.Checked;
            Settings.Default.AutoSaveBooklet = autoSaveBooklet.Checked;
            Settings.Default.PrintHints = printHints.Checked;
            Settings.Default.PrintInternalSeverity = printInternalSeverity.Checked;
            Settings.Default.BookletSizeUnlimited = unlimited.Checked;
            Settings.Default.BookletSizeExisting = (int)bookletSizeExisting.Value;
            Settings.Default.SeverityLevel = (easy.Checked ? 2 : 0) + (intermediate.Checked ? 4 : 0) + (hard.Checked ? 8 : 0);
            Settings.Default.SelectSeverity = selectSeverityLevel.Checked;
            Settings.Default.HideWhenMinimized = hideWhenMinimized.Checked;
            Settings.Default.AutoSaveState = saveState.Checked;
            Settings.Default.UseWatchHandHints = useWatchHands.Checked;
            Settings.Default.GenerateNormalSudoku = normalSudoku.Checked;
            Settings.Default.GenerateXSudoku = xSudoku.Checked;
            Settings.Default.GenerateMinimalProblems = generateMinimalProblems.Checked;
            Settings.Default.UsePrecalculatedProblems = precalculatedProblems.Checked;
            Settings.Default.AutoPause = autoPause.Checked;
            Settings.Default.AutoPauseLag = autoPauseLag.Value;

            foreach(RadioButton rb in sizeGroupBox.Controls)
                if(rb.Checked)
                    Settings.Default.Size = Convert.ToInt32(rb.Tag.ToString());

            int i = supportedGridSizes.Length - 1;
            foreach(RadioButton rb in problemPrintSize.Controls)
            {
                if(rb.Checked)
                    Settings.Default.HorizontalProblems = Convert.ToInt32(supportedGridSizes[i].ToString());
                i--;
            }

            i = supportedSolutionGridSizes.Length - 1;
            foreach(RadioButton rb in solutionPrintSize.Controls)
            {
                if(rb.Checked)
                    Settings.Default.HorizontalSolutions = Convert.ToInt32(supportedSolutionGridSizes[i].ToString());
                i--;
            }
        }

        private void checkUI(object sender, CancelEventArgs e)
        {
            if(e.Cancel) return;

            foreach(CultureInfo ci in CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures))
            {
                Debug.Print(ci.Name + " / " + ci.DisplayName);
                if(ci.DisplayName.CompareTo(language.Text) == 0)
                    for(int i = 0; i < supportedCultures.Length; i++)
                        if(ci.IetfLanguageTag.CompareTo(supportedCultures[i]) == 0)
                        {
                            Settings.Default.DisplayLanguage = supportedCultures[i];
                            return;
                        }
            }
            MessageBox.Show(Resources.InvalidCulture, Resources.SudokuError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void unlimitedCheckedChanged(object sender, EventArgs e)
        {
            bookletSizeExisting.Enabled = !unlimited.Checked;
        }

        private void severityLevelCheckedChanged(object sender, EventArgs e)
        {
            if(!easy.Checked && !intermediate.Checked && !hard.Checked)
            {
                MessageBox.Show(Resources.SeverityLevelError, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ((CheckBox)sender).Checked = true;
            }
        }

        private void sudokuTypeCheckedChanged(object sender, EventArgs e)
        {
            if(!xSudoku.Checked && !normalSudoku.Checked)
            {
                MessageBox.Show(Resources.SudokuTypeError, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ((CheckBox)sender).Checked = true;
            }
        }

        private void exchangePicture(object sender, EventArgs e)
        {
            possibleValuesExamplePicture.Image = useWatchHands.Checked ? Resources.watchHandCandidates : Resources.digitCandidates;
        }

        private void generateMinimumProblemsChanged(object sender, EventArgs e)
        {
            minValues.Enabled = !generateMinimalProblems.Checked;
        }

        private void precalculatedCheckedChanged(object sender, EventArgs e)
        {
            if(precalculatedProblems.Checked)
                bookletSizeNew.Value = Math.Min(bookletSizeNew.Value, Settings.Default.MaxProblems);
        }

        private void bookletSizeNewChanged(object sender, EventArgs e)
        {
            precalculatedProblems.Checked = (precalculatedProblems.Checked && (bookletSizeNew.Value <= Settings.Default.MaxProblems));
        }

        private void autoPauseCheckedChanged(object sender, EventArgs e)
        {
            autoPauseLag.Enabled = autoPause.Checked;
        }
    }
}