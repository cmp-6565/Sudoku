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
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware); // oder PerMonitorV2
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (s, e) =>
        {
            MessageBox.Show(Resources.UnknownError + e.Exception.Message, Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Optional: Logging
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            // Fataler Fehler, Anwendung wird wahrscheinlich beendet
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(Resources.CriticalError + ex?.Message, Resources.Crash, MessageBoxButtons.OK, MessageBoxIcon.Stop);
        };

        Application.Run(new SudokuForm());
    }
}