using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace Sudoku;

internal partial class OptionsDialog: Form
{
    private readonly ISudokuSettings settings;
    private IUserInteraction ui;

    String[] supportedCultures;
    String[] supportedGridSizes;
    String[] supportedSolutionGridSizes;

    public OptionsDialog(ISudokuSettings settings, IUserInteraction ui)
    {
        supportedCultures=settings.SupportedCultures.Split('|');
        supportedGridSizes=settings.HorizontalProblemsAlternatives.Split('|');
        supportedSolutionGridSizes=settings.HorizontalSolutionsAlternatives.Split('|');
        Thread.CurrentThread.CurrentUICulture=new System.Globalization.CultureInfo(settings.DisplayLanguage);

        InitializeComponent();

        this.settings=settings;
        this.ui=ui;

        hard.Text=Resources.Hard;
        easy.Text=Resources.Easy;
        intermediate.Text=Resources.Intermediate;
        minValues.Maximum=settings.MaxValues;

        bookletSizeNew.Value=settings.BookletSizeNew;
        bookletSizeExisting.Value=settings.BookletSizeExisting;
        bookletSizeExisting.Enabled=!settings.BookletSizeUnlimited;
        unlimited.Checked=settings.BookletSizeUnlimited;
        minValues.Value=settings.MinValues;
        constrast.Value=settings.Contrast;
        xSudokuConstrast.Value=settings.XSudokuConstrast;
        problemDirectory.Text=settings.ProblemDirectory;
        printSolutions.Checked=settings.PrintSolution;
        printInternalSeverity.Checked=settings.PrintInternalSeverity;
        autoSaveBooklet.Checked=settings.AutoSaveBooklet;
        printHints.Checked=settings.PrintHints;
        hideWhenMinimized.Checked=settings.HideWhenMinimized;
        saveState.Checked=settings.AutoSaveState;
        generateMinimalProblems.Checked=settings.GenerateMinimalProblems;
        minValues.Enabled=!generateMinimalProblems.Checked;
        precalculatedProblems.Checked=settings.UsePrecalculatedProblems;
        autoPause.Checked=settings.AutoPause;
        autoPauseLag.Value=settings.AutoPauseLag;
        autoPauseLag.Enabled=autoPause.Checked;

        // The severity level "trivial" (1) is not handled
        if(settings.SeverityLevel == 0) settings.SeverityLevel=15;
        hard.Checked=(settings.SeverityLevel & 8) != 0;
        intermediate.Checked=(settings.SeverityLevel & 4) != 0;
        easy.Checked=(settings.SeverityLevel & 2) != 0;
        selectSeverityLevel.Checked=settings.SelectSeverity;

        normalSudoku.Checked=settings.GenerateNormalSudoku;
        xSudoku.Checked=settings.GenerateXSudoku;

        foreach(RadioButton rb in sizeGroupBox.Controls)
            rb.Checked=(rb.Tag.ToString() == settings.Size.ToString());

        useWatchHands.Checked=settings.UseWatchHandHints;
        useDigits.Checked=!useWatchHands.Checked;
        possibleValuesExamplePicture.Image=useWatchHands.Checked? Resources.watchHandCandidates: Resources.digitCandidates;

        int i=supportedGridSizes.Length;
        foreach(RadioButton rb in problemPrintSize.Controls)
            rb.Checked=(supportedGridSizes[--i].ToString() == settings.HorizontalProblems.ToString());

        i=supportedSolutionGridSizes.Length;
        foreach(RadioButton rb in solutionPrintSize.Controls)
            rb.Checked=(supportedSolutionGridSizes[--i].ToString() == settings.HorizontalSolutions.ToString());

        for(i=0; i < supportedCultures.Length; i++)
            language.Items.Add(CultureInfo.GetCultureInfoByIetfLanguageTag(supportedCultures[i]).DisplayName);
        language.Text=CultureInfo.GetCultureInfoByIetfLanguageTag(settings.DisplayLanguage).DisplayName;
    }

    public int MinBookletSize
    {
        set
        {
            bookletSizeNew.Minimum=value;
        }
    }

    private void directorySelect_Click(object sender, EventArgs e)
    {
        selectProblemDirectoryDialog.SelectedPath=problemDirectory.Text;
        if(selectProblemDirectoryDialog.ShowDialog() == DialogResult.OK)
            problemDirectory.Text=selectProblemDirectoryDialog.SelectedPath;
    }

    private void ok_Click(object sender, EventArgs e)
    {
        settings.BookletSizeNew=(int)bookletSizeNew.Value;
        settings.MinValues=(int)minValues.Value;
        settings.Contrast=(int)constrast.Value;
        settings.XSudokuConstrast=(int)xSudokuConstrast.Value;
        settings.ProblemDirectory=problemDirectory.Text;
        settings.PrintSolution=printSolutions.Checked;
        settings.AutoSaveBooklet=autoSaveBooklet.Checked;
        settings.PrintHints=printHints.Checked;
        settings.PrintInternalSeverity=printInternalSeverity.Checked;
        settings.BookletSizeUnlimited=unlimited.Checked;
        settings.BookletSizeExisting=(int)bookletSizeExisting.Value;
        settings.SeverityLevel=(easy.Checked? 2: 0) + (intermediate.Checked? 4: 0) + (hard.Checked? 8: 0);
        settings.SelectSeverity=selectSeverityLevel.Checked;
        settings.HideWhenMinimized=hideWhenMinimized.Checked;
        settings.AutoSaveState=saveState.Checked;
        settings.UseWatchHandHints=useWatchHands.Checked;
        settings.GenerateNormalSudoku=normalSudoku.Checked;
        settings.GenerateXSudoku=xSudoku.Checked;
        settings.GenerateMinimalProblems=generateMinimalProblems.Checked;
        settings.UsePrecalculatedProblems=precalculatedProblems.Checked;
        settings.AutoPause=autoPause.Checked;
        settings.AutoPauseLag=autoPauseLag.Value;

        foreach(RadioButton rb in sizeGroupBox.Controls)
            if(rb.Checked)
                settings.Size=Convert.ToInt32(rb.Tag.ToString());

        int i=supportedGridSizes.Length - 1;
        foreach(RadioButton rb in problemPrintSize.Controls)
        {
            if(rb.Checked)
                settings.HorizontalProblems=Convert.ToInt32(supportedGridSizes[i].ToString());
            i--;
        }

        i=supportedSolutionGridSizes.Length - 1;
        foreach(RadioButton rb in solutionPrintSize.Controls)
        {
            if(rb.Checked)
                settings.HorizontalSolutions=Convert.ToInt32(supportedSolutionGridSizes[i].ToString());
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
                for(int i=0; i < supportedCultures.Length; i++)
                    if(ci.IetfLanguageTag.CompareTo(supportedCultures[i]) == 0)
                    {
                        settings.DisplayLanguage=supportedCultures[i];
                        return;
                    }
        }
        ui.ShowError(Resources.InvalidCulture);
    }

    private void unlimitedCheckedChanged(object sender, EventArgs e)
    {
        bookletSizeExisting.Enabled=!unlimited.Checked;
    }

    private void severityLevelCheckedChanged(object sender, EventArgs e)
    {
        if(!easy.Checked && !intermediate.Checked && !hard.Checked)
        {
            ui.ShowError(Resources.SeverityLevelError);
            ((CheckBox)sender).Checked=true;
        }
    }

    private void sudokuTypeCheckedChanged(object sender, EventArgs e)
    {
        if(!xSudoku.Checked && !normalSudoku.Checked)
        {
            ui.ShowError(Resources.SudokuTypeError);
            ((CheckBox)sender).Checked=true;
        }
    }

    private void exchangePicture(object sender, EventArgs e)
    {
        possibleValuesExamplePicture.Image=useWatchHands.Checked? Resources.watchHandCandidates: Resources.digitCandidates;
    }

    private void generateMinimumProblemsChanged(object sender, EventArgs e)
    {
        minValues.Enabled=!generateMinimalProblems.Checked;
    }

    private void precalculatedCheckedChanged(object sender, EventArgs e)
    {
        if(precalculatedProblems.Checked)
            bookletSizeNew.Value=Math.Min(bookletSizeNew.Value, settings.MaxProblems);
    }

    private void bookletSizeNewChanged(object sender, EventArgs e)
    {
        precalculatedProblems.Checked=(precalculatedProblems.Checked && (bookletSizeNew.Value <= settings.MaxProblems));
    }

    private void autoPauseCheckedChanged(object sender, EventArgs e)
    {
        autoPauseLag.Enabled=autoPause.Checked;
    }
}