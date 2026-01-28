using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sudoku.Tests
{
    [TestClass]
    public class MaskTests
    {
        private ISudokuSettings settings = new WinFormsSettings();

        [TestMethod]
        public void ToggleAndGetViaCell()
        {
            var m = new SudokuMatrix(settings);
            m.Init();
            var cell = m.Cell(0, 0);

            // ensure cell initialized
            // m.Init() already calls Init on contained cells

            Assert.IsFalse(cell.GetCandidateMask(1, false));
            cell.ToggleCandidateMask(1, false);
            Assert.IsTrue(cell.GetCandidateMask(1, false));
            cell.ToggleCandidateMask(1, true);
            Assert.IsFalse(cell.GetCandidateMask(1, false));
            Assert.IsTrue(cell.GetCandidateMask(1, true));
        }

        [TestMethod]
        public void SetGetViaMatrix()
        {
            var m = new SudokuMatrix(settings);
            m.Init();
            m.SetCandidate(0, 0, 2, false);
            Assert.IsTrue(m.GetCandidate(0, 0, 2, false));
            m.SetCandidate(0, 0, 2, false);
            Assert.IsFalse(m.GetCandidate(0, 0, 2, false));
        }
    }
}
