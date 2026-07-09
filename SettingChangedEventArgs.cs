using System;

namespace Sudoku;

/// <summary>
/// Provides event data for setting change notifications.
/// Captures the property name and both old and new values for change tracking.
/// </summary>
public class SettingChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the SettingChangedEventArgs class.
    /// </summary>
    /// <param name="settingName">The name of the setting that was changed.</param>
    /// <param name="oldValue">The previous value of the setting.</param>
    /// <param name="newValue">The new value of the setting.</param>
    public SettingChangedEventArgs(string settingName, object? oldValue, object? newValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(settingName, nameof(settingName));

        SettingName = settingName;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the name of the setting that was changed.
    /// </summary>
    public string SettingName { get; }

    /// <summary>
    /// Gets the previous value of the setting before the change.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the new value of the setting after the change.
    /// </summary>
    public object? NewValue { get; }

    /// <summary>
    /// Gets the UTC timestamp when the change occurred.
    /// </summary>
    public DateTime ChangedAt { get; }

    /// <summary>
    /// Gets a value indicating whether the value actually changed.
    /// </summary>
    public bool HasChanged => !Equals(OldValue, NewValue);

    /// <summary>
    /// Returns a string representation of this setting change event.
    /// </summary>
    public override string ToString()
    {
        return $"Setting '{SettingName}' changed from '{OldValue}' to '{NewValue}' at {ChangedAt:O}";
    }
}