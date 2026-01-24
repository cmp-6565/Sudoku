using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku
{
    public enum MinimizationUpdateType { Status, TestCell, ResetCell }
    internal class MinimizationUpdate
    {
        public MinimizationUpdateType Type { get; set; }
        public BaseCell Cell { get; set; }
        public BaseProblem Problem { get; set; } // Für Status-Updates
    }
}
