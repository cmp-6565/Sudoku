using System.Windows.Forms;

namespace Sudoku
{
    public class FlatRenderer: ToolStripProfessionalRenderer
    {
        public FlatRenderer() : base(new FlatColorTable()) { }
    }
}
