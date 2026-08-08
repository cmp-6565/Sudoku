using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Sudoku;

/// <summary>
/// About dialog showing product information and contact links.
/// </summary>
partial class AboutSudoku: Form
{
    private readonly ISudokuSettings settings;

    /// <summary>
    /// Creates a new About dialog using the provided settings to determine display language and contact info.
    /// </summary>
    /// <param name="settings">Application settings used for localization and contact information.</param>
    public AboutSudoku(ISudokuSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
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

    /// <summary>
    /// Opens the user's default mail client pre-filled with the contact email from settings.
    /// </summary>
    private void OpenContactEmail(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("mailto:" + settings.MailAddress) { UseShellExecute = true });
    }

    /// <summary>
    /// Opens the project's git repository URL in the default browser.
    /// </summary>
    private void OpenGitRepository(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(AssemblyInfo.AssemblyGitRepository) { UseShellExecute = true });

    }
}
