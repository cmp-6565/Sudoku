using System;

namespace Sudoku;

/// <summary>
/// Factory interface to create <see cref="SudokuController"/> instances for a given <see cref="IUserInteraction"/>.
/// </summary>
internal interface ISudokuControllerFactory
{
    /// <summary>
    /// Create a new <see cref="SudokuController"/> bound to the specified <paramref name="ui"/>.
    /// </summary>
    /// <param name="ui">UI interaction implementation (usually the Form).</param>
    /// <returns>A new <see cref="SudokuController"/> instance.</returns>
    SudokuController Create(IUserInteraction ui);
}