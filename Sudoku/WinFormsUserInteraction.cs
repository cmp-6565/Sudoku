// Sudoku/WinFormsUserInteraction.cs (neu)
using System;
using System.Windows.Forms;

using Sudoku.Application;

namespace Sudoku;

internal sealed class WinFormsUserInteraction: IUserInteraction
{
    public WinFormsUserInteraction() { }
    public void ShowError(string message) => MessageBox.Show(message, "Sudoku", MessageBoxButtons.OK, MessageBoxIcon.Error);
    public void ShowInfo(string message) => MessageBox.Show(message, "Sudoku", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public ConfirmResult Confirm(string message, ConfirmOptions options = ConfirmOptions.YesNo)
    {
        var buttons = options switch
        {
            ConfirmOptions.YesNoCancel => MessageBoxButtons.YesNoCancel,
            ConfirmOptions.OkCancel => MessageBoxButtons.OKCancel,
            _ => MessageBoxButtons.YesNo
        };
        return MessageBox.Show(message, "Sudoku", buttons) switch
        {
            DialogResult.Yes or DialogResult.OK => ConfirmResult.Yes,
            DialogResult.No => ConfirmResult.No,
            _ => ConfirmResult.Cancel
        };
    }

    public int GetSeverity() { /* bisherige SaveFileDialog-Logik */ throw new NotImplementedException(); }
    public string AskForFilename(string defaultExt) { /* bisherige SaveFileDialog-Logik */ throw new NotImplementedException(); }
}