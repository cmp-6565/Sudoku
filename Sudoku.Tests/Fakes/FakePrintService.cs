// Sudoku.Tests/Fakes/FakePrintService.cs
#nullable enable
using Sudoku.Application;
using Sudoku.Core;

namespace Sudoku.Tests;

/// <summary>
/// No-op-Implementierung von <see cref="IPrintService"/> für Tests. Zeichnet lediglich mit,
/// wie viele Probleme hinzugefügt wurden – ohne jede Abhängigkeit zu GDI+/System.Drawing.
/// </summary>
internal sealed class FakePrintService: IPrintService
{
    private readonly System.Collections.Generic.List<BaseProblem> problems = new();

    public int PrintResult => 0;
    public string PrintErrorMessage => string.Empty;
    public bool ShowCandidates { get; set; }
    public int NumberOfProblems => problems.Count;

    public void AddProblem(BaseProblem problem) => problems.Add(problem);
    public void SortProblems() { }
    public void Print() { }
    public void Dispose() { }
}