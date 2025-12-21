using System;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    partial class AboutSudoku: Form
    {
        public AboutSudoku()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture=new System.Globalization.CultureInfo(Settings.Default.DisplayLanguage);
            InitializeComponent();

            //  Initialize the AboutBox to display the product information from the assembly information.
            //  Change assembly information settings for your application through either:
            // -Project->Properties->Application->Assembly Information
            // -AssemblyInfo.cs
            this.Text=String.Format(System.Threading.Thread.CurrentThread.CurrentUICulture, "About {0}", AssemblyInfo.AssemblyTitle);
            this.labelProductName.Text=AssemblyInfo.AssemblyProduct;
            this.labelVersion.Text=String.Format(System.Threading.Thread.CurrentThread.CurrentUICulture, "Version {0} ({1})", AssemblyInfo.AssemblyVersion, AssemblyInfo.AssemblyDate);
            this.labelCopyright.Text=AssemblyInfo.AssemblyCopyright;
            this.labelCompanyName.Text=Resources.picit;
            this.textBoxDescription.Text=Resources.Description.Replace("\\n", Environment.NewLine);
            this.logoPictureBox.Image=Resources.SudokuProblem;
        }

        private void contact_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:"+Settings.Default.MailAddress);
        }
    }
}
