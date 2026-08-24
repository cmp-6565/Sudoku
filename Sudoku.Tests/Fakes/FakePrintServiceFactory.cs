// Sudoku.Tests/Fakes/FakePrintServiceFactory.cs
#nullable enable
using Sudoku.Application;

namespace Sudoku.Tests;
internal sealed class FakePrintServiceFactory: IPrintServiceFactory
{
    public IPrintService Create() => new FakePrintService();
}