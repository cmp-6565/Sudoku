using System;
using System.IO;
using System.Text;

public partial class _Default: System.Web.UI.Page
{
    public static String result="";

    protected void Page_Load(object sender, EventArgs e)
    {
        const char SudokuIdentifier='9';
        const char XSudokuIdentifier='X';
        const String NormalSudokus="NormalSudokus";
        const String XSudokus="XSudokus";

        result="";
        Byte[] sudokuType=new Byte[1];

        try
        {
            if(Request.TotalBytes != 1)
                throw new ArgumentException("Invalid Request: "+Request.ContentLength.ToString());

            sudokuType=Request.BinaryRead(1);
            if((char)sudokuType[0] != SudokuIdentifier && (char)sudokuType[0] != XSudokuIdentifier)
                throw new ArgumentException("Invalid Sudoku type: "+(char)sudokuType[0]);

            result=RandomProblem(Request.PhysicalPath.Substring(0, Request.PhysicalPath.LastIndexOf('\\')+1)+((char)sudokuType[0] == SudokuIdentifier? NormalSudokus: XSudokus)+".sudoku");
        }
        catch(Exception ex)
        {
            result="ERROR: "+ex.Message;
        }
    }

    private String RandomProblem(String Filename)
    {
        const int length=81;
        Byte[] sudoku=new Byte[length];

        FileInfo fi=new FileInfo(Filename);
        BinaryReader Sudokus=new BinaryReader(File.Open(Filename, FileMode.Open));
        Sudokus.BaseStream.Seek(new Random().Next((int)(fi.Length/(length+2))-1)*(length+2), SeekOrigin.Begin);
        Sudokus.Read(sudoku, 0, length);
        Sudokus.Close();
        return new String(Encoding.ASCII.GetChars(sudoku));
    }
}
