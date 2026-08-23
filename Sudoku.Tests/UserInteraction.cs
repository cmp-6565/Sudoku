// Sudoku/WinFormsUserInteraction.cs (neu)
using System;
using System.Windows.Forms;

using Sudoku.Application;

namespace Sudoku.Sudoku.Tests;

internal sealed class UserInteraction: IUserInteraction
{
    public UserInteraction() { }
    public void ShowError(string message) { throw new NotImplementedException(); }
    public void ShowInfo(string message) { throw new NotImplementedException(); }
    public ConfirmResult Confirm(string message, ConfirmOptions options = ConfirmOptions.YesNo) { throw new NotImplementedException(); }
    public int GetSeverity() { throw new NotImplementedException(); }
    public string AskForFilename(string defaultExt) { throw new NotImplementedException(); }
}