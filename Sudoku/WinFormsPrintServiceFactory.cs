// Sudoku/WinFormsPrintServiceFactory.cs (neu)
using Sudoku;
using Sudoku.Application;
using Sudoku.Core;

namespace Sudoku;

internal sealed class WinFormsPrintServiceFactory: IPrintServiceFactory
{
    private readonly ISudokuSettings settings;
    public WinFormsPrintServiceFactory(ISudokuSettings settings) => this.settings = settings;
    public IPrintService Create() => new SudokuPrinterService(SudokuGrid.SudokuSize, settings);
}