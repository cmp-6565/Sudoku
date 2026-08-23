// Sudoku.Application/IPrintService.cs
using System;

using Sudoku.Core;

namespace Sudoku.Application;

public interface IPrintService: IDisposable
{
    int PrintResult { get; }
    string PrintErrorMessage { get; }
    bool ShowCandidates { get; set; }
    int NumberOfProblems { get; }

    void AddProblem(BaseProblem problem);
    void SortProblems();
    void Print();
}