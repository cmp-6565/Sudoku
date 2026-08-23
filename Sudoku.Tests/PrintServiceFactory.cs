using Sudoku;
using Sudoku.Application;
using Sudoku.Core;

namespace Sudoku.Sudoku.Tests;
internal sealed class PrintServiceFactory: IPrintServiceFactory
{
    private readonly ISudokuSettings settings;
    public PrintServiceFactory(ISudokuSettings settings) => this.settings = settings;
    public IPrintService Create() => new SudokuPrinterService(SudokuGrid.SudokuSize, settings);
}