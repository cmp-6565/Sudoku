using System;
using System.Diagnostics;

namespace Sudoku;

/// <summary>
/// Example helper class demonstrating how to listen to and react to settings changes.
/// Can be used in ViewModels or UI components to stay in sync with settings.
/// </summary>
public class SettingsChangeListener
{
    private readonly IObservableSudokuSettings _settings;
    private readonly Action<SettingChangedEventArgs> _onChanged;

    /// <summary>
    /// Initializes a new instance of the SettingsChangeListener class.
    /// </summary>
    /// <param name="settings">The observable settings instance to listen to.</param>
    /// <param name="onChanged">Action to invoke when any setting changes.</param>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
    public SettingsChangeListener(IObservableSudokuSettings settings, Action<SettingChangedEventArgs> onChanged)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));
        ArgumentNullException.ThrowIfNull(onChanged, nameof(onChanged));

        _settings = settings;
        _onChanged = onChanged;

        // Subscribe to all setting changes
        _settings.SettingChanged += OnSettingChanged;
    }

    /// <summary>
    /// Unsubscribes from setting change notifications.
    /// Should be called during cleanup/disposal.
    /// </summary>
    public void Dispose()
    {
        _settings.SettingChanged -= OnSettingChanged;
        Debug.WriteLine("[TRACE] SettingsChangeListener disposed and unsubscribed from SettingChanged event.");
    }

    private void OnSettingChanged(object? sender, SettingChangedEventArgs e)
    {
        Debug.WriteLine($"[INFO] {e}");
        _onChanged(e);
    }

    /// <summary>
    /// Creates a listener for a specific setting.
    /// </summary>
    public static SettingsChangeListener ForSetting(IObservableSudokuSettings settings, 
        string settingName, 
        Action<object?> onChanged)
    {
        ArgumentException.ThrowIfNullOrEmpty(settingName, nameof(settingName));
        ArgumentNullException.ThrowIfNull(onChanged, nameof(onChanged));

        return new SettingsChangeListener(settings, e =>
        {
            if (e.SettingName == settingName && e.HasChanged)
            {
                onChanged(e.NewValue);
            }
        });
    }
}