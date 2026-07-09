using System;

namespace Sudoku;

/// <summary>
/// Default implementation of <see cref="ISudokuControllerFactory"/>.
/// </summary>
internal class SudokuControllerFactory : ISudokuControllerFactory
{
    private readonly ISudokuSettings settings;

    /// <summary>
    /// Creates the factory using application <paramref name="settings"/>.
    /// </summary>
    /// <param name="settings">Application settings used to construct controllers.</param>
    public SudokuControllerFactory(ISudokuSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc/>
    public SudokuController Create(IUserInteraction ui)
    {
        if (ui == null) throw new ArgumentNullException(nameof(ui));
        return new SudokuController(settings, ui);
    }
}