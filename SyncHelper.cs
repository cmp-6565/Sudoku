using System;
using System.Globalization;

namespace Sudoku
{
    internal static class SyncHelper
    {
        /// <summary>
        /// Lightweight, UI-free synchronization of a 2D string grid into a BaseProblem clone.
        /// Returns false if any parse/validation error occurs; on success, outputs the cloned, updated problem.
        /// </summary>
        internal static bool TrySyncGrid(BaseProblem currentProblem, string[,] grid, CultureInfo cultureInfo, bool autoCheck, ref int incorrectTries, out BaseProblem syncedProblem)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (grid.GetLength(0) != SudokuForm.SudokuSize || grid.GetLength(1) != SudokuForm.SudokuSize)
                throw new ArgumentException("grid must be SudokuSize x SudokuSize", nameof(grid));

            bool error = false;
            BaseProblem tmp = currentProblem.Clone();

            for (int row = 0; row < SudokuForm.SudokuSize; row++)
            {
                for (int col = 0; col < SudokuForm.SudokuSize; col++)
                {
                    string raw = grid[row, col];
                    if (string.IsNullOrEmpty(raw)) continue;

                    string value = raw.Trim();
                    if (value.Length == 0)
                    {
                        tmp.SetValue(row, col, Values.Undefined);
                        continue;
                    }

                    if (!byte.TryParse(value, NumberStyles.Integer, cultureInfo, out byte parsed))
                    {
                        error = true;
                        if (autoCheck) incorrectTries++;
                        continue;
                    }

                    if (parsed < 1 || parsed > SudokuForm.SudokuSize)
                    {
                        error = true;
                        if (autoCheck) incorrectTries++;
                        continue;
                    }

                    try
                    {
                        tmp.SetValue(row, col, parsed);
                    }
                    catch (ArgumentException)
                    {
                        error = true;
                        if (autoCheck) incorrectTries++;
                    }
                }
            }

            if (error)
            {
                syncedProblem = currentProblem;
                return false;
            }

            syncedProblem = tmp;
            return true;
        }
    }
}
