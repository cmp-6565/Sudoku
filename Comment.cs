using System;
using System.Threading;
using System.Windows.Forms;

namespace Sudoku;

/// <summary>
/// A dialog form for entering and editing comments for Sudoku problems.
/// </summary>
public partial class Comment: Form
{
    private readonly ISudokuSettings settings;

    /// <summary>
    /// Initializes a new instance of the Comment dialog form.
    /// </summary>
    /// <param name="settings">The application settings used to set the display language.</param>
    public Comment(ISudokuSettings settings)
    {
        Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(settings.DisplayLanguage);
        InitializeComponent();
        commentTextBox.Focus();
        this.settings = settings;
    }

    /// <summary>
    /// Gets or sets the comment text entered by the user.
    /// </summary>
    public String SudokuComment
    {
        get { return commentTextBox.Text; }
        set { commentTextBox.Text = value; }
    }
}