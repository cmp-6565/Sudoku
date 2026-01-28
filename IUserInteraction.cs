using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sudoku
{
    internal interface IUserInteraction
    {
        void ShowError(string message);
        void ShowInfo(string message);
        DialogResult Confirm(string message, MessageBoxButtons buttons=MessageBoxButtons.YesNo);
        Boolean ShowPrintDialog(PrintDocument printDocument);
        int GetSeverity();
        string AskForFilename(string defaultExt);
    }
}
