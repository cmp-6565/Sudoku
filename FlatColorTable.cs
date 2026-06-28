using System.Drawing;
using System.Windows.Forms;

namespace Sudoku;

/// <summary>
/// Provides a modern flat-design color scheme for menu and toolbar rendering.
/// Uses light gray colors for a contemporary application appearance.
/// </summary>
public class FlatColorTable: ProfessionalColorTable
{
    /// <summary>
    /// Gets the color for selected menu items.
    /// </summary>
    public override Color MenuItemSelected => Color.FromArgb(230, 230, 230);

    /// <summary>
    /// Gets the color for menu item borders.
    /// </summary>
    public override Color MenuItemBorder => Color.Transparent;

    /// <summary>
    /// Gets the color for menu borders.
    /// </summary>
    public override Color MenuBorder => Color.LightGray;

    /// <summary>
    /// Gets the starting gradient color for pressed menu items.
    /// </summary>
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(200, 200, 200);

    /// <summary>
    /// Gets the ending gradient color for pressed menu items.
    /// </summary>
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(200, 200, 200);

    /// <summary>
    /// Gets the starting gradient color for selected menu items.
    /// </summary>
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(230, 230, 230);

    /// <summary>
    /// Gets the ending gradient color for selected menu items.
    /// </summary>
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(230, 230, 230);

    /// <summary>
    /// Gets the background color for dropdown menus.
    /// </summary>
    public override Color ToolStripDropDownBackground => Color.White;

    /// <summary>
    /// Gets the starting gradient color for the image margin.
    /// </summary>
    public override Color ImageMarginGradientBegin => Color.White;

    /// <summary>
    /// Gets the middle gradient color for the image margin.
    /// </summary>
    public override Color ImageMarginGradientMiddle => Color.White;

    /// <summary>
    /// Gets the ending gradient color for the image margin.
    /// </summary>
    public override Color ImageMarginGradientEnd => Color.White;
}
