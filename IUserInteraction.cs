#nullable enable
using System.Windows.Forms;

namespace Sudoku;

/// <summary>
/// Defines the interface for user interaction and messaging in the Sudoku application.
/// Provides methods for showing errors, information, and getting user input.
/// </summary>
internal interface IUserInteraction
{
    /// <summary>
    /// Displays an error message to the user.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    void ShowError(string message);

    /// <summary>
    /// Displays an informational message to the user.
    /// </summary>
    /// <param name="message">The information message to display.</param>
    void ShowInfo(string message);

    /// <summary>
    /// Displays a confirmation dialog to the user.
    /// </summary>
    /// <param name="message">The confirmation message to display.</param>
    /// <param name="buttons">The buttons to show in the dialog (default is YesNo).</param>
    /// <returns>The user's choice as a DialogResult.</returns>
    DialogResult Confirm(string message, MessageBoxButtons buttons = MessageBoxButtons.YesNo);

    /// <summary>
    /// Prompts the user to select a severity level for problem generation.
    /// </summary>
    /// <returns>The selected severity level.</returns>
    int GetSeverity();

    /// <summary>
    /// Prompts the user to select or enter a filename for saving.
    /// </summary>
    /// <param name="defaultExt">The default file extension to use.</param>
    /// <returns>The filename selected by the user, or null if canceled.</returns>
    string AskForFilename(string defaultExt);
}
