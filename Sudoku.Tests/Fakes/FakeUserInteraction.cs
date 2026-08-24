// Sudoku.Tests/Fakes/FakeUserInteraction.cs
#nullable enable
using Sudoku.Application;

namespace Sudoku.Tests;

/// <summary>
/// Konfigurierbare <see cref="IUserInteraction"/>-Fake-Implementierung für Tests.
/// Standardverhalten: alles wird bestätigt, keine echten Dialoge, keine WinForms-Abhängigkeit.
/// Über die Properties kann jeder Test das Verhalten gezielt steuern.
/// </summary>
internal sealed class FakeUserInteraction: IUserInteraction
{
    public System.Collections.Generic.List<string> Errors { get; } = new();
    public System.Collections.Generic.List<string> InfoMessages { get; } = new();

    /// <summary>Antwort, die <see cref="Confirm"/> zurückgibt. Default: immer "Ja".</summary>
    public ConfirmResult ConfirmResponse { get; set; } = ConfirmResult.Yes;

    /// <summary>Wert, den <see cref="GetSeverity"/> zurückgibt.</summary>
    public int SeverityResponse { get; set; } = int.MaxValue;

    /// <summary>Wert, den <see cref="AskForFilename"/> zurückgibt. Default: kein Dateiname (Abbruch simulieren).</summary>
    public string? FilenameResponse { get; set; }

    public void ShowError(string message) => Errors.Add(message);
    public void ShowInfo(string message) => InfoMessages.Add(message);
    public ConfirmResult Confirm(string message, ConfirmOptions options = ConfirmOptions.YesNo) => ConfirmResponse;
    public int GetSeverity() => SeverityResponse;
    public string AskForFilename(string defaultExt) => FilenameResponse ?? string.Empty;
}