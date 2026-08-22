#nullable enable
using System;

namespace Sudoku;

/// <summary>
/// Extended interface for observable Sudoku settings that supports event-driven change notifications.
/// Extends ISudokuSettings with change tracking capabilities for reactive UI updates.
/// </summary>
public interface IObservableSudokuSettings : ISudokuSettings
{
    /// <summary>
    /// Occurs when any setting value changes.
    /// Subscribers can track which settings changed and their old/new values.
    /// </summary>
    event EventHandler<SettingChangedEventArgs> SettingChanged;
}