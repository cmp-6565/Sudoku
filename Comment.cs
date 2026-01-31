using System;
using System.Threading;
using System.Windows.Forms;

namespace Sudoku;

public partial class Comment: Form
{
    private readonly ISudokuSettings settings;
    public Comment(ISudokuSettings settings)
    {
        Thread.CurrentThread.CurrentUICulture=new System.Globalization.CultureInfo(settings.DisplayLanguage);
        InitializeComponent();
        commentTextBox.Focus();
        this.settings=settings;
    }

    public String SudokuComment
    {
        get { return commentTextBox.Text; }
        set { commentTextBox.Text=value; }
    }
}