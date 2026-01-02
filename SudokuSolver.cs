using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Sudoku.Properties;

namespace Sudoku
{
    // Klasse muss internal sein, da BaseProblem internal ist
    internal class SudokuSolver
    {
        private readonly BaseProblem _problem;
        private bool _aborted;
        private bool _problemSolved;
        private bool _findAll;
        private bool _checkWellDefined;
        private int _nVarValues;
        private int _numSolutions;
        private BaseProblem _minimalProblem;
        private Task _findSolutions;

        // Öffentliche Eigenschaften für den Status
        public bool ProblemSolved => _problemSolved;
        public bool Aborted => _aborted;
        public bool IsCompleted => _findSolutions?.IsCompleted ?? false;
        public BaseProblem MinimalProblem => _minimalProblem;
        public Task Solve => _findSolutions;
        public int NumSolutions => _numSolutions;
        public long PassCount { get; private set; }
        public long TotalPassCount { get; private set; }
        public BaseProblem Problem => _problem;

        public SudokuSolver(BaseProblem problem, UInt64 maxSolutions, CancellationToken token)
        {
            _problem = problem;
            FindSolutions(maxSolutions, token);
        }

        public SudokuSolver(BaseProblem problem, int maxSeverity, CancellationToken token, IProgress<BaseProblem> progress = null)
        {
            _problem=problem;
            _minimalProblem=Minimize(maxSeverity, token, progress);
        }

        private BaseProblem Minimize(int maxSeverity, CancellationToken token, IProgress<BaseProblem> progress = null)
        {
            return _minimalProblem = MinimizeAsync(maxSeverity, token, progress).GetAwaiter().GetResult();
        }

        private void FindSolutions(UInt64 maxSolutions, CancellationToken token)
        {
            if(_findSolutions == null || _findSolutions.IsCompleted)
            {
                _findSolutions = FindSolutionsAsync(maxSolutions, token);
            }
        }

        private async Task FindSolutionsAsync(UInt64 maxSolutions, CancellationToken token)
        {
            _findAll = (maxSolutions == UInt64.MaxValue);
            _checkWellDefined = (maxSolutions == 2);
            _numSolutions = 0;
            PassCount = 0;
            TotalPassCount = 0;
            _problemSolved = false;
            _aborted = false;

            _problem.ResetSolutions();

            // Matrix vorbereiten
            try
            {
                _problem.PrepareMatrix();
            }
            catch(ArgumentException)
            {
                return; // Nicht lösbar
            }

            // Wenn keine variablen Werte mehr vorhanden sind, ist das Problem bereits gelöst
            if(_problem.Matrix.nVariableValues == 0)
            {
                _problemSolved = true;
                SaveResult();
                return;
            }

            if(!_problem.Resolvable())
            {
                return;
            }

            // Startet den rekursiven Lösungsprozess in einem Hintergrund-Task
            await Task.Run(() =>
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.DisplayLanguage);
                try
                {
                    _nVarValues = _problem.Matrix.nVariableValues;
                    if(token.IsCancellationRequested) { _aborted = true; return; }
                    SolveInternal(0, token);
                }
                catch(Exception)
                {
                    throw;
                }
            }, token);
        }

        private void SolveInternal(int current, CancellationToken token)
        {
            // Zugriff auf die Matrix über die öffentliche Eigenschaft
            BaseCell currentValue = _problem.Matrix.Get(current);
            byte value = 0;

            PassCount++;
            TotalPassCount++;

            // Periodische Prüfung auf Abbruch (z.B. alle 1000 Durchläufe)
            if(PassCount % 1000 == 0 && token.IsCancellationRequested)
            {
                _aborted = true;
                return;
            }

            if(currentValue.nPossibleValues > 0)
            {
                while(!_problemSolved && ++value <= SudokuForm.SudokuSize)
                {
                    if(token.IsCancellationRequested) { _aborted = true; return; }

                    // Wert zurücksetzen (entspricht ResetValue in BaseProblem)
                    _problem.SetValue(currentValue.Row, currentValue.Col, Values.Undefined, false);

                    if(currentValue.Enabled(value))
                    {
                        try
                        {
                            // Wert versuchen (entspricht TryValue in BaseProblem)
                            _problem.SetValue(currentValue.Row, currentValue.Col, value, true);
                            currentValue.ComputedValue = true;

                            if(current < _nVarValues - 1 && _problem.Resolvable())
                            {
                                SolveInternal(current + 1, token);
                            }
                            else
                            {
                                if(_problemSolved = IsSolved())
                                    SaveResult();

                                if(_findAll || (_checkWellDefined && _numSolutions < 2))
                                    _problemSolved = false;
                            }
                        }
                        catch(ArgumentException)
                        {
                            // Sackgasse, Backtracking
                        }
                    }
                }
            }
            else if(currentValue.DefinitiveValue != Values.Undefined)
            {
                if(token.IsCancellationRequested) { _aborted = true; return; }

                _problem.SetValue(currentValue.Row, currentValue.Col, currentValue.DefinitiveValue, true);
                currentValue.ComputedValue = true;

                if(current < _nVarValues - 1 && _problem.Resolvable())
                {
                    SolveInternal(current + 1, token);
                }
                else
                {
                    if(_problemSolved = IsSolved())
                        SaveResult();

                    if(_findAll || (_checkWellDefined && _numSolutions < 2))
                        _problemSolved = false;
                }
            }

            if(!_problemSolved)
            {
                _problem.SetValue(currentValue.Row, currentValue.Col, Values.Undefined, false);
            }

            if((_findAll || _checkWellDefined) && current == 0)
                _problemSolved = (_numSolutions > 0);
        }

        private bool IsSolved()
        {
            // 1. Prüfen, ob alle Zellen gefüllt sind
            for(int row = 0; row < SudokuForm.SudokuSize; row++)
            {
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                {
                    if(_problem.GetValue(row, col) == Values.Undefined)
                        return false;
                }
            }

            // 2. Prüfen der Regeln über das Problem-Objekt selbst
            // Dies stellt sicher, dass auch Varianten wie X-Sudoku korrekt validiert werden
            return _problem.Resolvable();
        }

        private void SaveResult()
        {
            if(_numSolutions++ < Settings.Default.MaxSolutions)
            {
                Solution solution = null;
                // Zugriff auf die Solutions-Liste von BaseProblem
                _problem.Solutions.Add((Solution)_problem.CopyTo(ref solution));
            }
            PassCount = 0;
        }

        // --- Minimierungs-Logik ---
        private async Task<BaseProblem> MinimizeAsync(int maxSeverity, CancellationToken token, IProgress<BaseProblem> progress = null)
        {
            _problem.ResetMatrix();
            _minimalProblem = _problem.Clone();

            // Hole Kandidaten (Zellen, die entfernt werden könnten)
            var candidates = await GetCandidatesAsync(_problem.Matrix.Cells, 0, token);

            if (await MinimizeRecursiveAsync(candidates, maxSeverity, token, progress))
            {
                _minimalProblem.SeverityLevel = float.NaN; // Neuberechnung erzwingen
                
                // Prüfen, ob das minimierte Problem eindeutig lösbar ist
                var checkSolver = new SudokuSolver(_minimalProblem, 2, token);
                await checkSolver.Solve;
                
                return (checkSolver.NumSolutions == 1 ? _minimalProblem : null);
            }
            else
            {
                return null;
            }
        }

        private async Task<bool> MinimizeRecursiveAsync(List<BaseCell> candidates, int maxSeverity, CancellationToken token, IProgress<BaseProblem> progress)
        {
            if (candidates == null) return true;

            int start = 0;
            foreach (BaseCell cell in candidates)
            {
                if (token.IsCancellationRequested) { _aborted = true; return false; }
                if (_problem.SeverityLevelInt > maxSeverity) return false;

                // Wenn das Entfernen dieser Zelle zu weniger Werten führt als das bisher beste Minimum
                if (_problem.nValues - (candidates.Count - start) < _minimalProblem.nValues)
                {
                    byte cellValue = cell.CellValue;
                    _problem.SetValue(cell, Values.Undefined);

                    _problem.ResetMatrix();
                    if (_problem.nValues < _minimalProblem.nValues)
                    {
                        _minimalProblem = _problem.Clone();
                    }

                    progress?.Report(_minimalProblem);

                    // Rekursiver Aufruf mit den verbleibenden Kandidaten
                    var nextCandidates = await GetCandidatesAsync(candidates, ++start, token);
                    if (!await MinimizeRecursiveAsync(nextCandidates, maxSeverity, token, progress)) return false;

                    _problem.ResetMatrix();
                    _problem.SetValue(cell, cellValue);
                }
            }
            return true;
        }

        private async Task<List<BaseCell>> GetCandidatesAsync(List<BaseCell> source, int start, CancellationToken token)
        {
            List<BaseCell> candidates = new List<BaseCell>();

            for (int i = start; i < source.Count; i++)
            {
                // Optimierung: Wenn wir selbst bei Entfernung aller restlichen Kandidaten nicht besser werden als das aktuelle Minimum, abbrechen.
                if (_problem.nValues - candidates.Count - (source.Count - i) > _minimalProblem.nValues) return null;

                byte cellValue = source[i].CellValue;
                if (cellValue != Values.Undefined)
                {
                    _problem.SetValue(source[i], Values.Undefined);
                    
                    // Wenn der Wert eindeutig bestimmt ist (durch Logik, ohne Raten)
                    if (source[i].DefinitiveValue == cellValue)
                    {
                        candidates.Add(source[i]);
                    }
                    else
                    {
                        if (token.IsCancellationRequested) { _aborted = true; return null; }
                        
                        // Prüfen, ob das Problem ohne diesen Wert immer noch eindeutig lösbar ist
                        var checkSolver = new SudokuSolver(_problem, 2, token);
                        await checkSolver.Solve;
                        
                        if (checkSolver.NumSolutions == 1)
                        {
                            candidates.Add(source[i]);
                        }
                    }
                    _problem.ResetMatrix();
                    _problem.SetValue(source[i], cellValue);
                }

                if (token.IsCancellationRequested) { _aborted = true; return null; }
            }

            return candidates;
        }
    }
}