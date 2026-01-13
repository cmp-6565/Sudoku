using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sudoku
{
    public class FlatRenderer: ToolStripProfessionalRenderer
    {
        public FlatRenderer() : base(new FlatColorTable()) { }
    }
}
