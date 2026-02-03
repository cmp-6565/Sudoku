using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sudoku;

internal class SudokuFileService
{
    private readonly ISudokuSettings settings;
    private IUserInteraction ui;
    private static readonly HttpClient httpClient = new HttpClient();

    private static byte ReadOnlyOffset = 64;
    public Char SudokuTypeIdentifier { get { return Sudoku.SudokuTypeIdentifier; } }

    public BaseProblem Sudoku { get; set; }
    public BaseMatrix Matrix { get { return Sudoku.Matrix; } }
    public TimeSpan SolvingTime { get { return Sudoku.SolvingTime; } set { Sudoku.SolvingTime = value; } }
    public String Comment { get { return Sudoku.Comment; } set { Sudoku.Comment = value; } }
    public String SeverityLevelText { get { return Sudoku.SeverityLevelText; } }
    public int GetValue(int row, int col)
    {
        return Sudoku.GetValue(row, col);
    }
    public void SetValue(int row, int col, byte value)
    {
        Sudoku.SetValue(row, col, value);
    }
    public Boolean GetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        return Sudoku.GetCandidate(row, col, candidate, exclusionCandidate);
    }
    public void SetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
    {
        Sudoku.SetCandidate(row, col, candidate, exclusionCandidate);
    }
    public SudokuFileService(BaseProblem SudokuProblem, ISudokuSettings settings, IUserInteraction ui)
    {
        Sudoku = SudokuProblem;
        this.settings = settings;
        this.ui = ui;
    }
    public Boolean SaveToFile(String file)
    {
        Boolean rc = false;
        StreamWriter sw;
        try
        {
            sw = new StreamWriter(file);
            sw.Write(Serialize(true));
            sw.Close();
            Sudoku.Filename = file;
            Sudoku.Dirty = false;

            rc = true;
        }
        catch(Exception) { throw; }
        return rc;
    }
    public Action<Boolean> ReadProblem;
    private void NotifyReadProblem(Boolean xSudoku)
    {
        ReadProblem?.Invoke(xSudoku);
    }
    public void SaveToHTMLFile(String file)
    {
        StreamWriter sw;
        Char[] problem = SerializeLegacy(false).ToCharArray();
        int offset = (int)'0' + ReadOnlyOffset;

        try
        {
            sw = new StreamWriter(file);
            sw.Write(
                String.Format(Resources.HTMLFrame,
                    String.Format(
                        problem[00] == SudokuProblem.ProblemIdentifier ? Resources.HTMLTemplate : Resources.HTMLTemplateX,
                        problem[01] == '0' ? "&nbsp;" : problem[01] > '9' ? (problem[01] - offset).ToString() : problem[01].ToString(),
                        problem[02] == '0' ? "&nbsp;" : problem[02] > '9' ? (problem[02] - offset).ToString() : problem[02].ToString(),
                        problem[03] == '0' ? "&nbsp;" : problem[03] > '9' ? (problem[03] - offset).ToString() : problem[03].ToString(),
                        problem[04] == '0' ? "&nbsp;" : problem[04] > '9' ? (problem[04] - offset).ToString() : problem[04].ToString(),
                        problem[05] == '0' ? "&nbsp;" : problem[05] > '9' ? (problem[05] - offset).ToString() : problem[05].ToString(),
                        problem[06] == '0' ? "&nbsp;" : problem[06] > '9' ? (problem[06] - offset).ToString() : problem[06].ToString(),
                        problem[07] == '0' ? "&nbsp;" : problem[07] > '9' ? (problem[07] - offset).ToString() : problem[07].ToString(),
                        problem[08] == '0' ? "&nbsp;" : problem[08] > '9' ? (problem[08] - offset).ToString() : problem[08].ToString(),
                        problem[09] == '0' ? "&nbsp;" : problem[09] > '9' ? (problem[09] - offset).ToString() : problem[09].ToString(),
                        problem[10] == '0' ? "&nbsp;" : problem[10] > '9' ? (problem[10] - offset).ToString() : problem[10].ToString(),
                        problem[11] == '0' ? "&nbsp;" : problem[11] > '9' ? (problem[11] - offset).ToString() : problem[11].ToString(),
                        problem[12] == '0' ? "&nbsp;" : problem[12] > '9' ? (problem[12] - offset).ToString() : problem[12].ToString(),
                        problem[13] == '0' ? "&nbsp;" : problem[13] > '9' ? (problem[13] - offset).ToString() : problem[13].ToString(),
                        problem[14] == '0' ? "&nbsp;" : problem[14] > '9' ? (problem[14] - offset).ToString() : problem[14].ToString(),
                        problem[15] == '0' ? "&nbsp;" : problem[15] > '9' ? (problem[15] - offset).ToString() : problem[15].ToString(),
                        problem[16] == '0' ? "&nbsp;" : problem[16] > '9' ? (problem[16] - offset).ToString() : problem[16].ToString(),
                        problem[17] == '0' ? "&nbsp;" : problem[17] > '9' ? (problem[17] - offset).ToString() : problem[17].ToString(),
                        problem[18] == '0' ? "&nbsp;" : problem[18] > '9' ? (problem[18] - offset).ToString() : problem[18].ToString(),
                        problem[19] == '0' ? "&nbsp;" : problem[19] > '9' ? (problem[19] - offset).ToString() : problem[19].ToString(),
                        problem[20] == '0' ? "&nbsp;" : problem[20] > '9' ? (problem[20] - offset).ToString() : problem[20].ToString(),
                        problem[21] == '0' ? "&nbsp;" : problem[21] > '9' ? (problem[21] - offset).ToString() : problem[21].ToString(),
                        problem[22] == '0' ? "&nbsp;" : problem[22] > '9' ? (problem[22] - offset).ToString() : problem[22].ToString(),
                        problem[23] == '0' ? "&nbsp;" : problem[23] > '9' ? (problem[23] - offset).ToString() : problem[23].ToString(),
                        problem[24] == '0' ? "&nbsp;" : problem[24] > '9' ? (problem[24] - offset).ToString() : problem[24].ToString(),
                        problem[25] == '0' ? "&nbsp;" : problem[25] > '9' ? (problem[25] - offset).ToString() : problem[25].ToString(),
                        problem[26] == '0' ? "&nbsp;" : problem[26] > '9' ? (problem[26] - offset).ToString() : problem[26].ToString(),
                        problem[27] == '0' ? "&nbsp;" : problem[27] > '9' ? (problem[27] - offset).ToString() : problem[27].ToString(),
                        problem[28] == '0' ? "&nbsp;" : problem[28] > '9' ? (problem[28] - offset).ToString() : problem[28].ToString(),
                        problem[29] == '0' ? "&nbsp;" : problem[29] > '9' ? (problem[29] - offset).ToString() : problem[29].ToString(),
                        problem[30] == '0' ? "&nbsp;" : problem[30] > '9' ? (problem[30] - offset).ToString() : problem[30].ToString(),
                        problem[31] == '0' ? "&nbsp;" : problem[31] > '9' ? (problem[31] - offset).ToString() : problem[31].ToString(),
                        problem[32] == '0' ? "&nbsp;" : problem[32] > '9' ? (problem[32] - offset).ToString() : problem[32].ToString(),
                        problem[33] == '0' ? "&nbsp;" : problem[33] > '9' ? (problem[33] - offset).ToString() : problem[33].ToString(),
                        problem[34] == '0' ? "&nbsp;" : problem[34] > '9' ? (problem[34] - offset).ToString() : problem[34].ToString(),
                        problem[35] == '0' ? "&nbsp;" : problem[35] > '9' ? (problem[35] - offset).ToString() : problem[35].ToString(),
                        problem[36] == '0' ? "&nbsp;" : problem[36] > '9' ? (problem[36] - offset).ToString() : problem[36].ToString(),
                        problem[37] == '0' ? "&nbsp;" : problem[37] > '9' ? (problem[37] - offset).ToString() : problem[37].ToString(),
                        problem[38] == '0' ? "&nbsp;" : problem[38] > '9' ? (problem[38] - offset).ToString() : problem[38].ToString(),
                        problem[39] == '0' ? "&nbsp;" : problem[39] > '9' ? (problem[39] - offset).ToString() : problem[39].ToString(),
                        problem[40] == '0' ? "&nbsp;" : problem[40] > '9' ? (problem[40] - offset).ToString() : problem[40].ToString(),
                        problem[41] == '0' ? "&nbsp;" : problem[41] > '9' ? (problem[41] - offset).ToString() : problem[41].ToString(),
                        problem[42] == '0' ? "&nbsp;" : problem[42] > '9' ? (problem[42] - offset).ToString() : problem[42].ToString(),
                        problem[43] == '0' ? "&nbsp;" : problem[43] > '9' ? (problem[43] - offset).ToString() : problem[43].ToString(),
                        problem[44] == '0' ? "&nbsp;" : problem[44] > '9' ? (problem[44] - offset).ToString() : problem[44].ToString(),
                        problem[45] == '0' ? "&nbsp;" : problem[45] > '9' ? (problem[45] - offset).ToString() : problem[45].ToString(),
                        problem[46] == '0' ? "&nbsp;" : problem[46] > '9' ? (problem[46] - offset).ToString() : problem[46].ToString(),
                        problem[47] == '0' ? "&nbsp;" : problem[47] > '9' ? (problem[47] - offset).ToString() : problem[47].ToString(),
                        problem[48] == '0' ? "&nbsp;" : problem[48] > '9' ? (problem[48] - offset).ToString() : problem[48].ToString(),
                        problem[49] == '0' ? "&nbsp;" : problem[49] > '9' ? (problem[49] - offset).ToString() : problem[49].ToString(),
                        problem[50] == '0' ? "&nbsp;" : problem[50] > '9' ? (problem[50] - offset).ToString() : problem[50].ToString(),
                        problem[51] == '0' ? "&nbsp;" : problem[51] > '9' ? (problem[51] - offset).ToString() : problem[51].ToString(),
                        problem[52] == '0' ? "&nbsp;" : problem[52] > '9' ? (problem[52] - offset).ToString() : problem[52].ToString(),
                        problem[53] == '0' ? "&nbsp;" : problem[53] > '9' ? (problem[53] - offset).ToString() : problem[53].ToString(),
                        problem[54] == '0' ? "&nbsp;" : problem[54] > '9' ? (problem[54] - offset).ToString() : problem[54].ToString(),
                        problem[55] == '0' ? "&nbsp;" : problem[55] > '9' ? (problem[55] - offset).ToString() : problem[55].ToString(),
                        problem[56] == '0' ? "&nbsp;" : problem[56] > '9' ? (problem[56] - offset).ToString() : problem[56].ToString(),
                        problem[57] == '0' ? "&nbsp;" : problem[57] > '9' ? (problem[57] - offset).ToString() : problem[57].ToString(),
                        problem[58] == '0' ? "&nbsp;" : problem[58] > '9' ? (problem[58] - offset).ToString() : problem[58].ToString(),
                        problem[59] == '0' ? "&nbsp;" : problem[59] > '9' ? (problem[59] - offset).ToString() : problem[59].ToString(),
                        problem[60] == '0' ? "&nbsp;" : problem[60] > '9' ? (problem[60] - offset).ToString() : problem[60].ToString(),
                        problem[61] == '0' ? "&nbsp;" : problem[61] > '9' ? (problem[61] - offset).ToString() : problem[61].ToString(),
                        problem[62] == '0' ? "&nbsp;" : problem[62] > '9' ? (problem[62] - offset).ToString() : problem[62].ToString(),
                        problem[63] == '0' ? "&nbsp;" : problem[63] > '9' ? (problem[63] - offset).ToString() : problem[63].ToString(),
                        problem[64] == '0' ? "&nbsp;" : problem[64] > '9' ? (problem[64] - offset).ToString() : problem[64].ToString(),
                        problem[65] == '0' ? "&nbsp;" : problem[65] > '9' ? (problem[65] - offset).ToString() : problem[65].ToString(),
                        problem[66] == '0' ? "&nbsp;" : problem[66] > '9' ? (problem[66] - offset).ToString() : problem[66].ToString(),
                        problem[67] == '0' ? "&nbsp;" : problem[67] > '9' ? (problem[67] - offset).ToString() : problem[67].ToString(),
                        problem[68] == '0' ? "&nbsp;" : problem[68] > '9' ? (problem[68] - offset).ToString() : problem[68].ToString(),
                        problem[69] == '0' ? "&nbsp;" : problem[69] > '9' ? (problem[69] - offset).ToString() : problem[69].ToString(),
                        problem[70] == '0' ? "&nbsp;" : problem[70] > '9' ? (problem[70] - offset).ToString() : problem[70].ToString(),
                        problem[71] == '0' ? "&nbsp;" : problem[71] > '9' ? (problem[71] - offset).ToString() : problem[71].ToString(),
                        problem[72] == '0' ? "&nbsp;" : problem[72] > '9' ? (problem[72] - offset).ToString() : problem[72].ToString(),
                        problem[73] == '0' ? "&nbsp;" : problem[73] > '9' ? (problem[73] - offset).ToString() : problem[73].ToString(),
                        problem[74] == '0' ? "&nbsp;" : problem[74] > '9' ? (problem[74] - offset).ToString() : problem[74].ToString(),
                        problem[75] == '0' ? "&nbsp;" : problem[75] > '9' ? (problem[75] - offset).ToString() : problem[75].ToString(),
                        problem[76] == '0' ? "&nbsp;" : problem[76] > '9' ? (problem[76] - offset).ToString() : problem[76].ToString(),
                        problem[77] == '0' ? "&nbsp;" : problem[77] > '9' ? (problem[77] - offset).ToString() : problem[77].ToString(),
                        problem[78] == '0' ? "&nbsp;" : problem[78] > '9' ? (problem[78] - offset).ToString() : problem[78].ToString(),
                        problem[79] == '0' ? "&nbsp;" : problem[79] > '9' ? (problem[79] - offset).ToString() : problem[79].ToString(),
                        problem[80] == '0' ? "&nbsp;" : problem[80] > '9' ? (problem[80] - offset).ToString() : problem[80].ToString(),
                        problem[81] == '0' ? "&nbsp;" : problem[81] > '9' ? (problem[81] - offset).ToString() : problem[81].ToString()),
                    SeverityLevelText,
                    String.IsNullOrEmpty(Comment) ? "" : Comment,
                    DateTime.Now.ToString("yyyy.MM.dd", new CultureInfo(settings.DisplayLanguage)),
                    AssemblyInfo.AssemblyCopyright
                ));
            sw.Close();
        }
        catch(Exception) { throw; }
        return;
    }
    public string Serialize(Boolean includeROFlag)
    {
        var state = new SudokuSaveState
        {
            Id = Sudoku.Id,
            Type = SudokuTypeIdentifier.ToString(),
            GridData = SerializeMatrix(includeROFlag),
            Time = SolvingTime,
            Comment = Sudoku.Comment,
            Candidates = SerializeCandiates()
        };

        return System.Text.Json.JsonSerializer.Serialize(state);
    }
    public void Deserialize(string jsonState)
    {
        if(string.IsNullOrEmpty(jsonState)) return;

        try
        {
            var state = System.Text.Json.JsonSerializer.Deserialize<SudokuSaveState>(jsonState);
            NotifyReadProblem(state.Type[0] == XSudokuProblem.ProblemIdentifier);

            Sudoku.Id = state.Id;
            InitMatrix(state.GridData.ToCharArray());
            Sudoku.SolvingTime = state.Time;
            Sudoku.Comment = state.Comment;
            LoadCandidates(state.Candidates.Substring(state.Candidates.IndexOf('\n') + 1), false);
            LoadCandidates(state.Candidates.Substring(state.Candidates.LastIndexOf('\n') + 1), true);
        }
        catch
        {
            throw;
        }
        return;
    }
    public String SerializeLegacy(Boolean includeROFlag = true)
    {
        String serializedProblem;

        serializedProblem = SudokuTypeIdentifier.ToString();
        serializedProblem += SerializeMatrix(includeROFlag);
        serializedProblem += SolvingTime.ToString().PadRight(16, '0');
        serializedProblem += Comment;
        if(Matrix.HasCandidates())
            serializedProblem += (Environment.NewLine + SerializeCandiates());

        return serializedProblem;
    }
    public String SerializeMatrix(Boolean includeROFlag = true)
    {
        String serializedProblem = String.Empty;
        byte offset = (byte)'0';

        for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
            for(int j = 0; j < WinFormsSettings.SudokuSize; j++)
                serializedProblem += (char)(GetValue(i, j) + (Matrix.Cell(i, j).ReadOnly && includeROFlag ? ReadOnlyOffset : 0) + offset);

        return serializedProblem;
    }

    private String SerializeCandiates()
    {
        return SerializeCandiates(false) + Environment.NewLine + SerializeCandiates(true);
    }
    private String SerializeCandiates(Boolean exclusionCandidate)
    {
        Byte oneCandidate = 64;
        Byte bit = 0;
        String serializedCandidates = "";

        for(int row = 0; row < WinFormsSettings.SudokuSize; row++)
            for(int col = 0; col < WinFormsSettings.SudokuSize; col++)
            {
                for(int candidate = 1; candidate <= WinFormsSettings.SudokuSize; candidate++)
                {
                    if(GetCandidate(row, col, candidate, exclusionCandidate))
                        oneCandidate += (Byte)(1 << bit);
                    if(++bit > 5)
                    {
                        serializedCandidates += (Char)oneCandidate;
                        oneCandidate = 64;
                        bit = 0;
                    }
                }
            }
        serializedCandidates += (Char)oneCandidate;
        return serializedCandidates;
    }

    private void DeserializeCandidates(String candidates, Boolean exclusionCandidates)
    {
        Char oneCandidate;
        int candidate = 1;
        int row = 0;
        int col = 0;

        if(candidates == null) return;

        for(int i = 0; i < candidates.Length; i++)
        {
            oneCandidate = candidates[i];
            for(int bit = 0; bit < 6; bit++)
            {
                if((oneCandidate & (1 << bit)) > 0)
                    SetCandidate(row, col, candidate, exclusionCandidates);
                if(++candidate > WinFormsSettings.SudokuSize)
                {
                    candidate = 1;
                    if(++col >= WinFormsSettings.SudokuSize)
                    {
                        col = 0;
                        if(++row >= WinFormsSettings.SudokuSize)
                            return;
                    }
                }
            }
        }
        return;
    }

    public async Task<Boolean> SudokuOfTheDay()
    {
        return await Load("https://sudoku.pi-c-it.de/misc/PrecalculatedProblems/SudokuOfTheDay.php");
    }

    public async Task<Boolean> Load()
    {
        return await Load("https://sudoku.pi-c-it.de/misc/PrecalculatedProblems/Load.php");
    }
    public async Task<Boolean> Upload()
    {
        try
        {
            var content = new StringContent(SerializeLegacy().Substring(0, WinFormsSettings.TotalCellCount + 1), Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await httpClient.PostAsync("https://sudoku.pi-c-it.de/misc/TrickyProblems/Upload.php", content);

            String result = (await response.Content.ReadAsStringAsync()).Trim();
            if(result.IndexOf("ERROR") != 0)
                return true;
            else
                return false;
        }
        catch(Exception) { return false; }
    }
    public async Task<Boolean> Load(String URL)
    {
        Sudoku.Matrix.Init();
        SolvingTime = TimeSpan.Zero;
        try
        {
            var content = new StringContent(new String(SudokuTypeIdentifier, 1), Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await httpClient.PostAsync(URL, content);

            String sudoku = (await response.Content.ReadAsStringAsync()).Trim();
            if(sudoku.IndexOf("ERROR") != 0)
            {
                InitProblem(sudoku.ToCharArray(), "".ToCharArray(), "");
                return true;
            }
            else
                return false;
        }
        catch(Exception) { return false; }
    }

    public void LoadCandidates(StreamReader sr, Boolean exclusionCandidates)
    {
        DeserializeCandidates(sr.ReadLine(), exclusionCandidates);
    }

    public void LoadCandidates(String candidates, Boolean exclusionCandidates)
    {
        DeserializeCandidates(candidates, exclusionCandidates);
    }

    public void ReadFromFile(StreamReader sr)
    {
        Sudoku.Matrix.Init();

        try
        {
            char[] values = new char[WinFormsSettings.TotalCellCount];
            char[] elapsedTime = new char[16];

            sr.Read(values, 0, values.Length);
            sr.Read(elapsedTime, 0, elapsedTime.Length);

            InitProblem(values, elapsedTime, sr.ReadLine());
        }
        catch(Exception) { throw; }
    }
    public void InitProblem(char[] values, char[] elapsedTime, String initialComment)
    {
        try
        {
            TimeSpan ts = TimeSpan.Zero;

            if(TimeSpan.TryParse(new String(elapsedTime), out ts))
                SolvingTime = ts;
            if((Sudoku.Comment = initialComment) == null) // for compability reasons
                Sudoku.Comment = String.Empty;

            InitMatrix(values);
        }
        catch(Exception) { throw; }
    }
    private void InitMatrix(char[] values)
    {
        try
        {
            byte offset = (byte)'0';
            byte v = 0;

            Sudoku.Matrix.SetPredefinedValues = false;
            for(int i = 0; i < WinFormsSettings.SudokuSize; i++)
                for(int j = 0; j < WinFormsSettings.SudokuSize; j++)
                {
                    v = Convert.ToByte(values[i * WinFormsSettings.SudokuSize + j] - offset);
                    if(v >= ReadOnlyOffset)
                    {
                        Sudoku.Matrix.Cell(i, j).ReadOnly = (v > ReadOnlyOffset);
                        v -= ReadOnlyOffset;
                    }
                    Sudoku.SetValue(i, j, v);
                }
            Sudoku.Matrix.SetPredefinedValues = true;
        }
        catch(Exception) { throw; }
    }

    public void LoadProblem(String filename, Boolean normalSudoku, Boolean xSudoku, Boolean loadCandidates)
    {
        try
        {
            CreateProblemFromJsonFile(filename, normalSudoku, xSudoku, loadCandidates);
        }
        catch(Exception)
        {
            CreateProblemFromLegacyFile(filename, normalSudoku, xSudoku, loadCandidates);
        }
        finally { Sudoku.Filename = filename; }
    }

    public void CreateProblemFromJsonFile(String filename, Boolean normalSudoku, Boolean xSudoku, Boolean loadCandidates)
    {
        StreamReader sr = null;
        try
        {
            sr = new StreamReader(filename.Replace("%20", " "), System.Text.Encoding.Default);
            String jsonState = sr.ReadToEnd();
            sr.Close();

            Deserialize(jsonState);
        }
        catch(Exception) { throw; }
        finally { sr.Close(); }
        Sudoku.Filename = filename;
    }
    public void CreateProblemFromLegacyFile(String filename, Boolean normalSudoku, Boolean xSudoku, Boolean loadCandidates)
    {
        StreamReader sr = null;
        try
        {
            Char sudokuType;
            sr = new StreamReader(filename.Replace("%20", " "), System.Text.Encoding.Default);
            sudokuType = (Char)sr.Read();
            if(sudokuType != SudokuProblem.ProblemIdentifier && sudokuType != XSudokuProblem.ProblemIdentifier) throw new InvalidDataException();
            if(sudokuType == SudokuProblem.ProblemIdentifier && normalSudoku || sudokuType == XSudokuProblem.ProblemIdentifier && xSudoku)
            {
                NotifyReadProblem(sudokuType == XSudokuProblem.ProblemIdentifier);
                ReadFromFile(sr);
                if(loadCandidates)
                {
                    LoadCandidates(sr, false);
                    LoadCandidates(sr, true);
                }
            }
        }
        catch(Exception) { throw; }
        finally { sr.Close(); }
    }
    public void CreateBookletDirectory(GenerationParameters generationParameters)
    {
        if(settings.AutoSaveBooklet)
        {
            if(!Directory.Exists(settings.ProblemDirectory))
            {
                try
                {
                    Directory.CreateDirectory(settings.ProblemDirectory);
                }
                catch
                {
                    ui.ShowError(String.Format(Thread.CurrentThread.CurrentCulture, Resources.CreateDirectoryFailed, settings.ProblemDirectory));
                    settings.AutoSaveBooklet = false;
                }
            }
        }
        if(settings.AutoSaveBooklet)
        {
            generationParameters.BaseDirectory = settings.ProblemDirectory + Path.DirectorySeparatorChar + "Booklet-" + DateTime.Now.ToString("yyyy.MM.dd-hh-mm", Thread.CurrentThread.CurrentCulture);
            try
            {
                Directory.CreateDirectory(generationParameters.BaseDirectory);
            }
            catch
            {
                ui.ShowError(String.Format(Thread.CurrentThread.CurrentCulture, Resources.CreateDirectoryFailed, generationParameters.BaseDirectory));
                settings.AutoSaveBooklet = false;
            }
        }
    }
    public void LoadProblemFilenames(DirectoryInfo directoryInfo, List<String> filenames, CancellationToken token)
    {
        if(token.IsCancellationRequested) return;

        foreach(FileInfo fileInfo in directoryInfo.GetFiles())
            filenames.Add(fileInfo.FullName);

        foreach(DirectoryInfo di in directoryInfo.GetDirectories())
            LoadProblemFilenames(di, filenames, token);
    }
}
