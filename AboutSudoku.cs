using System;
using System.Diagnostics;
using System.Security.Policy;
using System.Windows.Forms;

namespace Sudoku;

partial class AboutSudoku: Form
{
    private readonly ISudokuSettings settings;
    public AboutSudoku(ISudokuSettings settings)
    {
        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(settings.DisplayLanguage);
        InitializeComponent();

        //  Initialize the AboutBox to display the product information from the assembly information.
        //  Change assembly information settings for your application through either:
        // -Project->Properties->Application->Assembly Information
        // -AssemblyInfo.cs
        this.Text = String.Format(System.Threading.Thread.CurrentThread.CurrentUICulture, "About {0}", AssemblyInfo.AssemblyTitle);
        this.labelProductName.Text = AssemblyInfo.AssemblyProduct;
        this.labelVersion.Text = String.Format(System.Threading.Thread.CurrentThread.CurrentUICulture, "Version {0} ({1})", AssemblyInfo.AssemblyVersion, AssemblyInfo.AssemblyDate);
        this.labelCopyright.Text = AssemblyInfo.AssemblyCopyright;
        this.labelCompanyName.Text = Resources.picit;
        this.textBoxDescription.Text = AssemblyInfo.AssemblyDescription.Replace("\\n", Environment.NewLine);
        this.logoPictureBox.Image = Resources.SudokuProblem;
        this.settings = settings;
    }

    private void OpenContactEmail(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("mailto:" + settings.MailAddress) { UseShellExecute = true });
    }

    private void OpenGitRepository(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(AssemblyInfo.AssemblyGitRepository) { UseShellExecute = true });

    }
}
