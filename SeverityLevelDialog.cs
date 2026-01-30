using System.Windows.Forms;

namespace Sudoku;

public partial class SeverityLevelDialog: Form
{
    public SeverityLevelDialog()
    {
        InitializeComponent();

        hard.Text = Resources.Hard;
        easy.Text = Resources.Easy;
        intermediate.Text = Resources.Intermediate;

        easy.Checked = true;
    }

    public int SeverityLevel
    {
        get { return (easy.Checked ? 2 : 0) + (intermediate.Checked ? 4 : 0) + (hard.Checked ? 8 : 0); }
    }
}