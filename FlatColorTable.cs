using System.Drawing;
using System.Windows.Forms;

namespace Sudoku
{
    public class FlatColorTable: ProfessionalColorTable
    {
        // Modernes, flaches Hellgrau für Hintergründe
        public override Color MenuItemSelected => Color.FromArgb(230, 230, 230);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuBorder => Color.LightGray;
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(200, 200, 200);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(200, 200, 200);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(230, 230, 230);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(230, 230, 230);
        public override Color ToolStripDropDownBackground => Color.White;
        public override Color ImageMarginGradientBegin => Color.White;
        public override Color ImageMarginGradientMiddle => Color.White;
        public override Color ImageMarginGradientEnd => Color.White;
    }
}
