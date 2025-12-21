using System;

namespace Sudoku
{
    [Serializable]
    internal class CoreValue
    {
        private Byte content=Values.Undefined;
        private int row=0;
        private int col=0;
        private String unformated="";

        public String UnformatedValue
        {
            get { return unformated; }
            set { unformated=value; }
        }

        public Byte CellValue
        {
            get { return this.content; }
            set { this.content=value; }
        }

        public int Row
        {
            get { return row; }
            set { row=value; }
        }

        public int Col
        {
            get { return col; }
            set { col=value; }
        }
    }
}
