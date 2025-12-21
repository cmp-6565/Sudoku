using System;
using System.Web;
using System.IO;
using System.Globalization;
using System.Net.Mail;

public partial class _Default: System.Web.UI.Page
{
    public static String result="";

    protected void Page_Load(object sender, EventArgs e)
    {
        const int SudokuLength=82;
        const char SudokuIdentifier='9';
        const char XSudokuIdentifier='X';

        StreamWriter sw=null;
        String filename="";
        result="";

        try
        {
            filename=Request.PhysicalPath.Substring(0, Request.PhysicalPath.LastIndexOf('\\')+1)+DateTime.Now.ToString("yyyy-MM-dd\\THHmmss\\(ffff\\)", DateTimeFormatInfo.InvariantInfo)+".sudoku";
            sw=new StreamWriter(filename, false);

            Byte[] sudoku=new Byte[SudokuLength];

            if(Request.TotalBytes != SudokuLength)
                throw new ArgumentException("Invalid Sudoku: "+Request.ContentLength.ToString());

            sudoku=Request.BinaryRead(SudokuLength);
            if((char)sudoku[0] != SudokuIdentifier && (char)sudoku[0] != XSudokuIdentifier)
                throw new ArgumentException("Invalid Sudoku type: "+(char)sudoku[0]);

            foreach(Byte x in sudoku)
                result+=((Char)x);

            sw.Write(result);
            SendMail(Request, filename.Substring(filename.LastIndexOf('\\')+1));
        }
        catch(Exception ex)
        {
            result=ex.Message;
        }
        finally
        {
            if(sw != null)
            {
                sw.Close();
                FileInfo fi=new FileInfo(filename);
                if(fi.Length < 82)
                    fi.Delete();
            }
        }
    }

    private void SendMail(HttpRequest request, String filename)
    {
        MailMessage message=new MailMessage("kontakt@pi-c-it.de", "kontakt@pi-c-it.de");
        SmtpClient smtp=new SmtpClient("localhost", 25);

        message.Body="Request Header: "+Environment.NewLine;
        for(int i=0; i < request.Headers.Count; i++)
            message.Body+=request.Headers.GetKey(i)+": "+request.Headers.Get(i)+Environment.NewLine;

        message.Body+="Request UserHostAdress: "+request.UserHostAddress+Environment.NewLine;
        message.Body+="Request UserHostName: "+request.UserHostName+Environment.NewLine;
        message.Subject="New tricky Sudoku uploaded: "+filename;
        smtp.Send(message);
    }
}
