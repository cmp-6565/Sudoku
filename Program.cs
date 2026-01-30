using System;
using System.Windows.Forms;

[assembly: CLSCompliant(true)]
namespace Sudoku;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new SudokuForm());
    }
}