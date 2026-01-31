using System.Windows.Forms;

namespace Sudoku;

internal interface IUserInteraction
{
    void ShowError(string message);
    void ShowInfo(string message);
    DialogResult Confirm(string message, MessageBoxButtons buttons=MessageBoxButtons.YesNo);
    int GetSeverity();
    string AskForFilename(string defaultExt);
}
