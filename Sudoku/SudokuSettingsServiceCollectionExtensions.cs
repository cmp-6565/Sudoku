using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sudoku.DependencyInjection;

/// <summary>
/// Provides extension methods for registering Sudoku settings services in the dependency injection container.
/// Enables type-safe addition of settings services to the application's service collection.
/// </summary>
public static class SudokuSettingsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the WinFormsSettings implementation as a singleton service for ISudokuSettings.
    /// </summary>
    /// <param name="services">The service collection to add settings to.</param>
    /// <returns>The same service collection instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddSudokuSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddSingleton<ISudokuSettings>(provider =>
        {
            var settings = new WinFormsSettings();
            return settings;
        });

        return services;
    }

    /// <summary>
    /// Adds the WinFormsSettings implementation as a singleton service for both ISudokuSettings and IObservableSudokuSettings.
    /// Enables subscribers to receive change notifications.
    /// </summary>
    /// <param name="services">The service collection to add settings to.</param>
    /// <returns>The same service collection instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddObservableSudokuSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddSingleton<IObservableSudokuSettings>(provider =>
        {
            var settings = new WinFormsSettings();
            return settings;
        });

        // Also register as ISudokuSettings for backward compatibility
        services.AddSingleton<ISudokuSettings>(provider =>
            provider.GetRequiredService<IObservableSudokuSettings>());

        return services;
    }

    /// <summary>
    /// Gets the registered ISudokuSettings instance from the service provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The registered ISudokuSettings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when ISudokuSettings is not registered.</exception>
    public static ISudokuSettings GetSudokuSettings(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));

        return provider.GetRequiredService<ISudokuSettings>();
    }

    /// <summary>
    /// Gets the registered IObservableSudokuSettings instance from the service provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The registered IObservableSudokuSettings instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when provider is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when IObservableSudokuSettings is not registered.</exception>
    public static IObservableSudokuSettings GetObservableSudokuSettings(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider, nameof(provider));

        return provider.GetRequiredService<IObservableSudokuSettings>();
    }
}