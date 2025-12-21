using System;
using System.Threading;
using System.Windows.Forms;

using Sudoku.Properties;

namespace Sudoku
{
    public partial class Comment: Form
    {
        public Comment()
        {
            Thread.CurrentThread.CurrentUICulture=new System.Globalization.CultureInfo(Settings.Default.DisplayLanguage);
            InitializeComponent();
            commentTextBox.Focus();
        }

        public String SudokuComment
        {
            get { return commentTextBox.Text; }
            set { commentTextBox.Text=value; }
        }
    }
}