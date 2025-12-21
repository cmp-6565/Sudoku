using System;
using System.Collections.Generic;
using System.Drawing;

using Sudoku.Properties;

namespace Sudoku
{
    internal class PrintParameters
    {
        private List<BaseProblem> problems;
        private int currentProblem=0;
        private int currentSolution=0;
        private int currentPage=0;

        private float pageWidthDots=0;
        private float pageHeightDots=0;
        private float cellWidthDots=0;
        private float cellHeightDots=0;
        private float smallCellWidthDots=0;
        private float smallCellHeightDots=0;

        private int printResult=0;

        static private Pen thickSolidLine=new Pen(Color.Black, 2.5f);
        static private Pen thinSolidLine=new Pen(Color.Black, 2.0f);
        static private Pen tinySolidLine=new Pen(Color.Black, 0.5f);
        static private Pen redTinySolidLine=new Pen(Color.Red, 0.5f);
        static private Pen greenTinySolidLine=new Pen(Color.Green, 0.5f);
        static private Brush solidBrush=new SolidBrush(Color.Black);
        static private Brush lightGraySolidBrush;
        static private Brush greenSolidBrush=new SolidBrush(Color.Green);
        static private Brush redSolidBrush=new SolidBrush(Color.Red);
        static private Font titleFont=new Font(Settings.Default.TableFont, 12, FontStyle.Bold);
        static private Font headerFont=new Font(Settings.Default.TableFont, 24, FontStyle.Bold);
        static private Font largeFont=new Font(Settings.Default.PrintFont, 14, FontStyle.Regular);
        static private Font normalFont=new Font(Settings.Default.PrintFont, 10, FontStyle.Regular);
        static private Font normalBoldFont=new Font(Settings.Default.PrintFont, 10, FontStyle.Bold);
        static private Font smallFont=new Font(Settings.Default.PrintFont, 6, FontStyle.Regular);
        static private Font smallBoldFont=new Font(Settings.Default.PrintFont, 6, FontStyle.Bold);
        static private Font smallFixedFont=new Font(Settings.Default.FixedFont, 6, FontStyle.Regular);

        static private StringFormat centered=new StringFormat();
        static private StringFormat vertical=new StringFormat();
        static private StringFormat leftBounded=new StringFormat();
        static private StringFormat rightBounded=new StringFormat();

        public PrintParameters()
        {
            problems=new List<BaseProblem>();

            int colorIndex=255-(int)(255f*((float)Settings.Default.XSudokuConstrast/100f));
            lightGraySolidBrush=new SolidBrush(Color.FromArgb(colorIndex, colorIndex, colorIndex));

            centered.FormatFlags=StringFormatFlags.NoWrap;
            centered.Alignment=StringAlignment.Center;
            centered.LineAlignment=StringAlignment.Center;

            vertical.FormatFlags=StringFormatFlags.NoWrap|StringFormatFlags.DirectionVertical;
            vertical.Alignment=StringAlignment.Center;
            vertical.LineAlignment=StringAlignment.Center;

            leftBounded.FormatFlags=StringFormatFlags.NoWrap;
            leftBounded.Alignment=StringAlignment.Near;
            leftBounded.LineAlignment=StringAlignment.Far;

            rightBounded.FormatFlags=StringFormatFlags.NoWrap;
            rightBounded.Alignment=StringAlignment.Far;
            rightBounded.LineAlignment=StringAlignment.Far;
        }

        public static String PrintError(int errorCode)
        {
            String[] errors={ Resources.InvalidSize, Resources.UnknownError };

            if(errorCode<1||errorCode>errors.Length)
                throw new ArgumentException(errorCode.ToString());

            return errors[errorCode-1];
        }

        public int CurrentProblem
        {
            get { return currentProblem; }
            set { currentProblem=value; }
        }

        public int CurrentSolution
        {
            get { return currentSolution; }
            set { currentSolution=value; }
        }

        public float PageWidthDots
        {
            get { return pageWidthDots; }
            set { pageWidthDots=value; }
        }

        public float PageHeightDots
        {
            get { return pageHeightDots; }
            set { pageHeightDots=value; }
        }

        public float CellWidthDots
        {
            get { return cellWidthDots; }
            set { cellWidthDots=value; }
        }

        public float CellHeightDots
        {
            get { return cellHeightDots; }
            set { cellHeightDots=value; }
        }

        public float SmallCellWidthDots
        {
            get { return smallCellWidthDots; }
            set { smallCellWidthDots=value; }
        }

        public float SmallCellHeightDots
        {
            get { return smallCellHeightDots; }
            set { smallCellHeightDots=value; }
        }

        public int PrintResult
        {
            get { return printResult; }
            set { printResult=value; }
        }

        public List<BaseProblem> Problems
        {
            get { return problems; }
        }

        public List<Solution> Solutions(int problem)
        {
            return problems[problem].Solutions;
        }

        static public Pen ThickSolidLine
        {
            get { return thickSolidLine; }
        }

        static public Pen ThinSolidLine
        {
            get { return thinSolidLine; }
        }

        public static Pen TinySolidLine
        {
            get { return PrintParameters.tinySolidLine; }
        }

        public static Pen RedTinySolidLine
        {
            get { return PrintParameters.redTinySolidLine; }
        }

        public static Pen GreenTinySolidLine
        {
            get { return PrintParameters.greenTinySolidLine; }
        }

        static public Brush SolidBrush
        {
            get { return solidBrush; }
        }

        public static Brush LightGraySolidBrush
        {
            get { return PrintParameters.lightGraySolidBrush; }
        }

        static public Brush RedSolidBrush
        {
            get { return PrintParameters.redSolidBrush; }
        }

        static public Brush GreenSolidBrush
        {
            get { return PrintParameters.greenSolidBrush; }
        }

        static public Font TitleFont
        {
            get { return titleFont; }
        }

        static public Font HeaderFont
        {
            get { return headerFont; }
        }

        static public Font LargeFont
        {
            get { return largeFont; }
        }

        static public Font NormalFont
        {
            get { return normalFont; }
        }

        static public Font NormalBoldFont
        {
            get { return normalBoldFont; }
        }

        static public Font SmallFont
        {
            get { return smallFont; }
        }

        static public Font SmallBoldFont
        {
            get { return smallBoldFont; }
        }

        static public Font SmallFixedFont
        {
            get { return smallFixedFont; }
        }

        static public StringFormat Centered
        {
            get { return centered; }
        }

        static public StringFormat Vertical
        {
            get { return vertical; }
        }

        static public StringFormat LeftBounded
        {
            get { return leftBounded; }
        }

        static public StringFormat RightBounded
        {
            get { return rightBounded; }
        }

        public int CurrentPage
        {
            get { return currentPage; }
            set { currentPage=value; }
        }
    }
}