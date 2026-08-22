#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sudoku;

public class FlatRenderer: ToolStripProfessionalRenderer
{
    public FlatRenderer() : base(new FlatColorTable()) { }
}
