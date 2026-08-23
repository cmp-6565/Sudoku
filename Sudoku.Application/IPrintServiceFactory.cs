// Sudoku.Application/IPrintServiceFactory.cs
namespace Sudoku.Application;

/// <summary>
/// Erzeugt Instanzen von <see cref="IPrintService"/>. Als Factory abstrahiert, weil
/// SudokuController an mehreren Stellen frische Instanzen benötigt (z. B. für separate Druckvorgänge).
/// </summary>
public interface IPrintServiceFactory
{
    IPrintService Create();
}