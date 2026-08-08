#nullable enable
namespace Sudoku.Properties;

/// <summary>
/// Provides event handlers for application settings changes and saves.
/// </summary>
// This class allows you to handle specific events on the settings class:
//  The SettingChanging event is raised before a setting's value is changed.
//  The PropertyChanged event is raised after a setting's value is changed.
//  The SettingsLoaded event is raised after the setting values are loaded.
//  The SettingsSaving event is raised before the setting values are saved.
internal sealed partial class Settings
{
    /// <summary>
    /// Initializes a new instance of the Settings class.
    /// </summary>
    public Settings()
    {
        // // To add event handlers for saving and changing settings, uncomment the lines below:
        //
        // this.SettingChanging+=this.SettingChangingEventHandler;
        //
        // this.SettingsSaving+=this.SettingsSavingEventHandler;
        //
    }

    /// <summary>
    /// Handles the SettingChanging event when a setting value is about to be changed.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The SettingChangingEventArgs that contains the event data.</param>
    private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
    {
        // ToDo
    }

    /// <summary>
    /// Handles the SettingsSaving event when setting values are about to be saved.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The CancelEventArgs that contains the event data.</param>
    private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Add code to handle the SettingsSaving event here.
    }
}
