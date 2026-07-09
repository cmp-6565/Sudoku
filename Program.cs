using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sudoku.DependencyInjection;

[assembly: CLSCompliant(true)]
namespace Sudoku;

/// <summary>
/// Main application entry point and startup class for the Sudoku application.
/// </summary>
static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// Initializes the application, configures error handling, and starts the main form.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (s, e) =>
        {
            MessageBox.Show(Resources.UnknownError + e.Exception.Message, Resources.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(Resources.CriticalError + ex?.Message, Resources.Crash, MessageBoxButtons.OK, MessageBoxIcon.Stop);
        };

        // Configure DI
        var services = new ServiceCollection();

        // add settings and helper services
        services.AddSudokuSettings();

        // register controller factory
        services.AddSingleton<SudokuControllerFactory>();

        // register the form so DI can construct it with injected settings and factory
        services.AddTransient<SudokuForm>(sp =>
            new SudokuForm(sp.GetRequiredService<ISudokuSettings>(), sp.GetRequiredService<SudokuControllerFactory>()));

        using var provider = services.BuildServiceProvider();

        var mainForm = provider.GetRequiredService<SudokuForm>();
        Application.Run(mainForm);
    }
}