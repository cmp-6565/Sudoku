using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sudoku;

namespace Sudoku.Tests
{
    [TestClass]
    public class SudokuSolverTests
    {
        // Ein einfaches, lösbares Sudoku (0 steht für leere Zellen)
        private readonly int[] _simplePuzzle = new int[]
        {
            5, 3, 0, 0, 7, 0, 0, 0, 0,
            6, 0, 0, 1, 9, 5, 0, 0, 0,
            0, 9, 8, 0, 0, 0, 0, 6, 0,
            8, 0, 0, 0, 6, 0, 0, 0, 3,
            4, 0, 0, 8, 0, 3, 0, 0, 1,
            7, 0, 0, 0, 2, 0, 0, 0, 6,
            0, 6, 0, 0, 0, 0, 2, 8, 0,
            0, 0, 0, 4, 1, 9, 0, 0, 5,
            0, 0, 0, 0, 8, 0, 0, 7, 9
        };

        // Ein vollständig gelöstes Sudoku (gültig)
        private readonly int[] _solvedPuzzle = new int[]
        {
            5, 3, 4, 6, 7, 8, 9, 1, 2,
            6, 7, 2, 1, 9, 5, 3, 4, 8,
            1, 9, 8, 3, 4, 2, 5, 6, 7,
            8, 5, 9, 7, 6, 1, 4, 2, 3,
            4, 2, 6, 8, 5, 3, 7, 9, 1,
            7, 1, 3, 9, 2, 4, 8, 5, 6,
            9, 6, 1, 5, 3, 7, 2, 8, 4,
            2, 8, 7, 4, 1, 9, 6, 3, 5,
            3, 4, 5, 2, 8, 6, 1, 7, 9
        };

        [TestMethod]
        public async Task FindSolutionsAsync_ShouldFindSolution_ForValidPuzzle()
        {
            // Arrange
            var problem = CreateProblemFromArray(_simplePuzzle);
            var solver = new SudokuSolver(problem);
            var cts = new CancellationTokenSource();

            // Act
            await solver.FindSolutionsAsync(1, cts.Token);

            // Assert
            Assert.IsTrue(solver.ProblemSolved, "Der Solver sollte das Problem als gelöst markieren.");
            Assert.AreEqual(1, solver.NumSolutions, "Es sollte genau eine Lösung gefunden werden.");
            Assert.IsTrue(problem.Solutions.Count > 0, "Das Problem-Objekt sollte eine Lösung enthalten.");
        }

        [TestMethod]
        public void IsSolved_ShouldReturnTrue_ForValidFullGrid()
        {
            // Arrange
            var problem = CreateProblemFromArray(_solvedPuzzle);
            var solver = new SudokuSolver(problem);

            // Act
            // Zugriff auf die private Methode 'IsSolved' via Reflection
            MethodInfo isSolvedMethod = typeof(SudokuSolver).GetMethod("IsSolved", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = (bool)isSolvedMethod.Invoke(solver, null);

            // Assert
            Assert.IsTrue(result, "IsSolved sollte für ein korrekt gelöstes Gitter true zurückgeben.");
        }

        [TestMethod]
        public void IsSolved_ShouldReturnFalse_ForInvalidFullGrid()
        {
            // Arrange
            var problem = CreateProblemFromArray(_solvedPuzzle);
            
            // Wir manipulieren das Gitter, um es ungültig zu machen (Duplikat in der ersten Zeile)
            // Setze Zelle (0, 1) auf den Wert von Zelle (0, 0)
            byte val = problem.GetValue(0, 0);
            problem.SetValue(0, 1, val); 

            var solver = new SudokuSolver(problem);

            // Act
            MethodInfo isSolvedMethod = typeof(SudokuSolver).GetMethod("IsSolved", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = (bool)isSolvedMethod.Invoke(solver, null);

            // Assert
            Assert.IsFalse(result, "IsSolved sollte für ein ungültiges Gitter false zurückgeben.");
        }

        [TestMethod]
        public void IsSolved_ShouldReturnFalse_ForIncompleteGrid()
        {
            // Arrange
            var problem = CreateProblemFromArray(_solvedPuzzle);
            
            // Wir entfernen einen Wert (setzen ihn auf Undefined/0)
            problem.SetValue(0, 0, 0); // 0 entspricht Values.Undefined

            var solver = new SudokuSolver(problem);

            // Act
            MethodInfo isSolvedMethod = typeof(SudokuSolver).GetMethod("IsSolved", BindingFlags.NonPublic | BindingFlags.Instance);
            bool result = (bool)isSolvedMethod.Invoke(solver, null);

            // Assert
            Assert.IsFalse(result, "IsSolved sollte für ein unvollständiges Gitter false zurückgeben.");
        }

        // Hilfsmethode zum Erstellen eines Problems aus einem Array
        private SudokuProblem CreateProblemFromArray(int[] arr)
        {
            var prob = new SudokuProblem();
            int size = SudokuForm.SudokuSize; // Zugriff auf Konstante (9)
            
            // Initialisierung wie im Benchmark-Code
            prob.Matrix.Init();
            prob.Matrix.SetPredefinedValues = false;
            for (int i = 0; i < arr.Length; i++)
            {
                int v = arr[i];
                if (v != 0) prob.SetValue(i / size, i % size, (byte)v);
            }
            prob.Matrix.SetPredefinedValues = true;
            return prob;
        }
    }
}