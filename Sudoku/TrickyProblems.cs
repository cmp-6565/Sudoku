using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

using Sudoku.Core;

namespace Sudoku;

/// <summary>
/// Manages a collection of "tricky" or difficult Sudoku problems for publishing.
/// Provides functionality to add, clear, and publish problems to a remote server.
/// </summary>
internal class TrickyProblems
{
    private readonly ISudokuSettings settings;
    private IUserInteraction ui;

    private List<BaseProblem> problems;

    /// <summary>
    /// Initializes a new instance of the TrickyProblems class.
    /// </summary>
    /// <param name="settings">The application settings.</param>
    /// <param name="ui">The user interaction interface for messaging.</param>
    public TrickyProblems(ISudokuSettings settings, IUserInteraction ui)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(ui);
        problems = new List<BaseProblem>();
        this.settings = settings;
        this.ui = ui;
    }

    /// <summary>
    /// Adds a problem to the collection.
    /// </summary>
    /// <param name="problem">The Sudoku problem to add.</param>
    public void Add(BaseProblem problem)
    {
        ArgumentNullException.ThrowIfNull(problem);
        problems.Add(problem);
    }

    /// <summary>
    /// Removes all problems from the collection.
    /// </summary>
    public void Clear()
    {
        problems.Clear();
    }

    /// <summary>
    /// Asynchronously publishes all problems in the collection to a remote server.
    /// </summary>
    /// <returns>True if publication was successful; false otherwise.</returns>
    public async Task<Boolean> Publish()
    {
        if(Empty) return true;

        try
        {
            foreach(BaseProblem problem in problems)
            {
                SudokuFileService fileService = new SudokuFileService(problem, settings, ui);
                return await fileService.Upload();
            }
        }
        catch(HttpRequestException)
        {
            return false;
        }
        catch(Exception)
        {
            // Unknown exception: propagate to caller or return false conservatively
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a value indicating whether the collection is empty.
    /// </summary>
    public Boolean Empty { get { return problems.Count == 0; } }

    /// <summary>
    /// Gets the number of problems in the collection.
    /// </summary>
    public int Count { get { return problems.Count; } }
}