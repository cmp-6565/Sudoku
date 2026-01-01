using System;
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

        // Öffentliche Eigenschaften für den Status
        public bool ProblemSolved => _problemSolved;
        public bool Aborted => _aborted;
        public int NumSolutions => _numSolutions;
        public long PassCount { get; private set; }
        public long TotalPassCount { get; private set; }
        public BaseProblem Problem => _problem;

        public SudokuSolver(BaseProblem problem)
        {
            _problem = problem;
        }

        public async Task FindSolutionsAsync(UInt64 maxSolutions, CancellationToken token)
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
                    Solve(0, token);
                }
                catch(Exception)
                {
                    throw;
                }
            }, token);
        }

        private void Solve(int current, CancellationToken token)
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
                                Solve(current + 1, token);
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
                    Solve(current + 1, token);
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
    }
}