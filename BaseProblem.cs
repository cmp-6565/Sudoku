using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Sudoku.Properties;

namespace Sudoku
{
    internal abstract class BaseProblem: EventArgs, IComparable
    {
        private Int64 totalPassCount = 0;
        private Int64 passCount = 0;
        private int nVarValues = 0;
        private Boolean findAll = false;
        protected BaseMatrix matrix;
        private List<Solution> solutions;
        private Boolean checkWellDefined = false;
        private Boolean problemSolved = false;
        private Boolean aborted = false;

        private Task solverTask = null;

        private float severityLevel = float.NaN;
        private String filename = String.Empty;
        private String comment = String.Empty;
        private Boolean dirty = false;
        private Boolean preparing = false;
        private TimeSpan solvingTime;
        private TimeSpan generationTime;
        private BaseProblem minimalProblem;

        private static byte ReadOnlyOffset = 64;
        public static Char ProblemIdentifier = ' ';
        public virtual Char SudokuTypeIdentifier { get { return ProblemIdentifier; } }

        public static CancellationTokenSource FormCTS
        {
            get
            {
                try
                {
                    if(System.Windows.Forms.Application.OpenForms.Count > 0)
                    {
                        SudokuForm mainForm = (SudokuForm)System.Windows.Forms.Application.OpenForms[0];
                        return mainForm.FormCTS;
                    }
                    else
                        return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public event EventHandler<BaseProblem> Minimizing;
        protected virtual void OnMinimizing(BaseProblem p)
        {
            EventHandler<BaseProblem> handler = Minimizing;
            if(handler != null) handler(this, p);
        }

        public event EventHandler<BaseCell> TestCell;
        protected virtual void OnTestCell(BaseCell c)
        {
            EventHandler<BaseCell> handler = TestCell;
            if(handler != null) handler(this, c);
        }

        public event EventHandler<BaseCell> ResetCell;
        protected virtual void OnResetCell(BaseCell c)
        {
            EventHandler<BaseCell> handler = ResetCell;
            if(handler != null) handler(this, c);
        }

        public BaseProblem()
        {
            createMatrix();
            solutions = new List<Solution>();
            solverTask = null;
            solvingTime = TimeSpan.Zero;
            generationTime = TimeSpan.Zero;
        }

        protected abstract void createMatrix();
        protected abstract BaseProblem CreateInstance();
        public virtual Boolean IsTricky { get { return false; } }

        public BaseMatrix Matrix { get { return matrix; } }
        public List<Solution> Solutions { get { return solutions; } }

        public int nValues { get { return Matrix.nValues; } }
        public int nVariableValues { get { return Matrix.nVariableValues; } }
        public int nComputedValues { get { return Matrix.nComputedValues; } }

        public Int64 TotalPassCounter
        {
            get { return totalPassCount; }
            set { totalPassCount = value; }
        }
        public int NumberOfSolutions { get { return Solutions.Count; } }
        public Task SolverTask
        {
            get { return solverTask; }
        }

        public Boolean ProblemSolved
        {
            get { return problemSolved; }
        }

        public Boolean Aborted
        {
            get { return aborted; }
            set { aborted = value; }
        }

        public float SeverityLevel
        {
            get
            {
                severityLevel = Matrix.SeverityLevel;
                return severityLevel;
            }
            set { severityLevel = value; }
        }

        public String SeverityLevelText
        {
            get { return float.IsNaN(SeverityLevel) ? "-" : (SeverityLevel > Settings.Default.Hard ? Resources.Hard : (SeverityLevel > Settings.Default.Intermediate ? Resources.Intermediate : (SeverityLevel > Settings.Default.Trivial ? Resources.Easy : Resources.Trivial))); }
        }

        public int SeverityLevelInt
        {
            get { return float.IsNaN(SeverityLevel) ? 0 : (SeverityLevel > Settings.Default.Hard ? 8 : (SeverityLevel > Settings.Default.Intermediate ? 4 : (SeverityLevel > Settings.Default.Trivial ? 2 : 1))); }
        }

        public String Filename { get { return filename; } set { filename = value; } }
        public String Comment { get { return comment; } set { comment = value; } }
        public Boolean Dirty { get { return dirty; } set { dirty = value; } }
        public Boolean Preparing { get { return preparing; } set { preparing = value; } }
        public TimeSpan SolvingTime { get { return solvingTime; } set { solvingTime = value; } }
        public TimeSpan GenerationTime { get { return generationTime; } set { generationTime = value; } }

        public int CompareTo(System.Object obj)
        {
            if(obj == null) return -1;
            BaseProblem tmpProblem;
            if(!((tmpProblem = (BaseProblem)obj) is BaseProblem)) throw new ArgumentException(obj.ToString());
            return SeverityLevel.CompareTo(tmpProblem.SeverityLevel);
        }

        public void ResetSolutions()
        {
            solutions = new List<Solution>();
        }

        public BaseProblem Clone()
        {
            BaseProblem dest = CreateInstance();
            dest.matrix = CloneMatrix();

            dest.ResetSolutions();
            for(int i = 0; i < NumberOfSolutions && i < Settings.Default.MaxSolutions; i++)
                dest.Solutions.Add(Solutions[i]);

            dest.severityLevel = SeverityLevel;
            dest.problemSolved = ProblemSolved;
            dest.Filename = Filename;
            dest.Comment = Comment;
            dest.Dirty = Dirty;
            dest.SolvingTime = SolvingTime;
            dest.GenerationTime = GenerationTime;

            return dest;
        }

        public BaseMatrix CloneMatrix()
        {
            return (BaseMatrix)Matrix.Clone();
        }

        public Solution CopyTo(ref Solution dest)
        {
            dest = new Solution();
            dest.Init();
            dest.Counter = passCount;

            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                    dest.SetValue(row, col, Matrix.GetValue(row, col), true);

            return dest;
        }

        public List<BaseCell> GetObviousCells()
        {
            return Matrix.GetObviousCells(true);
        }

        public List<BaseCell> GetHints()
        {
            return Matrix.GetHints(false);
        }

        public List<BaseCell> GetDeepHints()
        {
            return Matrix.GetHints(true);
        }

        private void SaveResult()
        {
            if(NumberOfSolutions < Settings.Default.MaxSolutions)
            {
                Solution solution = null;
                Solutions.Add((Solution)CopyTo(ref solution));
            }
            passCount = 0;
        }

        public void PrepareMatrix()
        {
            Matrix.Prepare();
        }

        public void ResetMatrix()
        {
            Matrix.Reset();
        }

        public void ResetCandidates()
        {
            Matrix.ResetCandidates();
        }

        public void ResetCandidates(int row, int col)
        {
            Matrix.ResetCandidates(row, col);
        }

        public Boolean GetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
        {
            return Matrix.GetCandidate(row, col, candidate, exclusionCandidate);
        }

        public void SetCandidate(int row, int col, int candidate, Boolean exclusionCandidate)
        {
            Matrix.SetCandidate(row, col, candidate, exclusionCandidate);
        }

        public Boolean HasCandidates()
        {
            return Matrix.HasCandidates();
        }

        public Boolean HasCandidate(int row, int col)
        {
            return Matrix.HasCandidate(row, col);
        }

        public BaseCell[] GetNeighbors(int row, int col)
        {
            return Matrix.Cell(row, col).Neighbors;
        }

        public void SetValue(int row, int col, byte value, Boolean fix)
        {
            if(GetValue(row, col) != value || FixedValue(row, col) != fix)
            {
                Matrix.SetValue(row, col, value, fix);
                severityLevel = float.NaN;
                problemSolved = false;
                filename = String.Empty;
            }
        }

        public void SetValue(int row, int col, byte value)
        {
            dirty = dirty || (value != GetValue(row, col));
            SetValue(row, col, value, value != Values.Undefined);
        }

        public void SetValue(BaseCell cell, byte value)
        {
            SetValue(cell.Row, cell.Col, value);
        }

        private void ResetValue(int row, int col)
        {
            float sv = severityLevel;
            dirty = dirty || (GetValue(row, col) != Values.Undefined);
            SetValue(row, col, Values.Undefined, false);
            severityLevel = sv;
        }

        private void TryValue(int row, int col, byte value)
        {
            float sv = severityLevel;
            dirty = dirty || (value != GetValue(row, col));
            SetValue(row, col, value, true);
            severityLevel = sv;
        }

        public BaseCell Cell(int row, int col)
        {
            return Matrix.Cell(row, col);
        }

        public byte GetValue(int row, int col)
        {
            return Matrix.GetValue(row, col);
        }

        public Boolean ComputedValue(int row, int col)
        {
            return Matrix.ComputedValue(row, col);
        }

        public Boolean FixedValue(int row, int col)
        {
            return Matrix.FixedValue(row, col);
        }

        public void FindSolutions(int maxSolutions, CancellationToken token)
        {
            solverTask?.Dispose();

            if(NumberOfSolutions >= maxSolutions) return;

            solverTask = FindSolutionsAsync(maxSolutions, token);
            solverTask.Wait(10);
        }

        private async Task FindSolutionsAsync(int maxSolutions, CancellationToken token)
        {
            if(aborted) return;

            preparing = true;
            findAll = (maxSolutions == int.MaxValue);
            checkWellDefined = (maxSolutions == 2);
            passCount = 0;
            totalPassCount = 0;
            problemSolved = false;
            solvingTime = TimeSpan.Zero;

            ResetSolutions();
            severityLevel = Matrix.SeverityLevel;

            try
            {
                PrepareMatrix();
            }
            catch(ArgumentException)
            {
                preparing = false;
                return;
            }
            finally
            {
                preparing = false;
            }

            if(Matrix.nVariableValues == 0)
            {
                problemSolved = true;
                SaveResult();
                return;
            }

            if(!Resolvable()) return;

            await Task.Run(() => Solve(token), token);
        }

        public void Cancel()
        {
            aborted = true;
            try
            {
                if(FormCTS != null && !FormCTS.IsCancellationRequested)
                    FormCTS.Cancel();
            }
            catch { }
        }

        private void Solve(CancellationToken token)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.DisplayLanguage);
            try
            {
                nVarValues = Matrix.nVariableValues;
                if(token.IsCancellationRequested) { aborted = true; return; }

                Solve(0, token);
            }
            catch(Exception)
            {
                ResetMatrix();
                aborted = true;
            }
        }

        private void Solve(int current, CancellationToken token)
        {
            // Strikter Abbruch-Check am Anfang jeder Rekursion
            if(aborted || token.IsCancellationRequested) { aborted = true; return; }

            BaseCell currentValue = Matrix.Get(current);
            byte value = 0;

            passCount++;
            totalPassCount++;

            const int progressInterval = 2000;

            if(passCount % progressInterval == 0)
            {
                OnProgress();
                if(aborted || token.IsCancellationRequested) { aborted = true; return; }
            }

            if(currentValue.nPossibleValues > 0)
            {
                while(!problemSolved && ++value <= SudokuForm.SudokuSize)
                {
                    if(aborted || token.IsCancellationRequested) { aborted = true; return; }

                    ResetValue(currentValue.Row, currentValue.Col);
                    if(currentValue.Enabled(value))
                    {
                        try
                        {
                            TryValue(currentValue.Row, currentValue.Col, value);
                            currentValue.ComputedValue = true;

                            if(current < nVarValues - 1) // Resolvable Check entfernen für Performance in tiefer Rekursion
                            {
                                if(Resolvable()) Solve(current + 1, token);
                            }
                            else
                            {
                                if(problemSolved = IsSolved()) SaveResult();
                                if(findAll || (checkWellDefined && NumberOfSolutions < 2)) problemSolved = false;
                            }
                        }
                        catch(ArgumentException) { }
                    }
                }
            }
            else if(currentValue.DefinitiveValue != Values.Undefined)
            {
                if(aborted || token.IsCancellationRequested) { aborted = true; return; }

                TryValue(currentValue.Row, currentValue.Col, currentValue.DefinitiveValue);
                currentValue.ComputedValue = true;

                if(current < nVarValues - 1 && Resolvable())
                    Solve(current + 1, token);
                else
                {
                    if(problemSolved = IsSolved()) SaveResult();
                    if(findAll || (checkWellDefined && NumberOfSolutions < 2)) problemSolved = false;
                }
            }

            if(!problemSolved) ResetValue(currentValue.Row, currentValue.Col);

            if((findAll || checkWellDefined) && current == 0) problemSolved = (NumberOfSolutions > 0);
        }
        public async Task<BaseProblem> Minimize(int maxSeverity, CancellationToken token)
        {
            ResetMatrix();

            minimalProblem = Clone();

            List<BaseCell> candidates=await GetCandidates(Matrix.Cells, 0, CancellationToken.None);
            if(await MinimizeRecursive(candidates, maxSeverity, token))
            {
                minimalProblem.severityLevel = float.NaN;

                await minimalProblem.FindSolutionsAsync(2, token);

                return (minimalProblem.NumberOfSolutions == 1 ? minimalProblem : null);
            }
            else
                return null;
        }

        // Async Recursive Minimize
        private async Task<Boolean> MinimizeRecursive(List<BaseCell> candidates, int maxSeverity, CancellationToken token)
        {
            if(candidates == null) return true;

            int start = 0;
            foreach(BaseCell cell in candidates)
            {
                if(aborted || token.IsCancellationRequested) return false;
                if(SeverityLevelInt > maxSeverity) return false;

                if(nValues - (candidates.Count - start) < minimalProblem.nValues)
                {
                    byte cellValue = cell.CellValue;
                    SetValue(cell, Values.Undefined);

                    ResetMatrix();
                    if(nValues < minimalProblem.nValues) minimalProblem = Clone();

                    OnMinimizing(minimalProblem);

                    var nextCandidates = await GetCandidates(candidates, ++start, token);

                    if(aborted || token.IsCancellationRequested) return false;
                    if(!await MinimizeRecursive(nextCandidates, maxSeverity, token)) return false;

                    ResetMatrix();
                    SetValue(cell, cellValue);
                }
            }
            return true;
        }

        // Private helper now async
        private async Task<List<BaseCell>> GetCandidates(List<BaseCell> source, int start, CancellationToken token)
        {
            List<BaseCell> candiates = new List<BaseCell>();

            for(int i = start; i < source.Count; i++)
            {
                if(nValues - candiates.Count - (source.Count - i) > minimalProblem.nValues) return null;

                byte cellValue = source[i].CellValue;
                if(cellValue != Values.Undefined)
                {
                    SetValue(source[i], Values.Undefined);
                    if(source[i].DefinitiveValue == cellValue)
                        candiates.Add(source[i]);
                    else
                    {
                        if(aborted || token.IsCancellationRequested) { aborted = true; return null; }

                        await FindSolutionsAsync(2, token);

                        if(NumberOfSolutions == 1) candiates.Add(source[i]);
                    }
                    ResetMatrix();
                    SetValue(source[i], cellValue);
                }

                if(aborted || token.IsCancellationRequested) { aborted = true; return null; }
            }

            return candiates;
        }

        public virtual Boolean Resolvable()
        {
            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                    if(!Check(row, col)) return false;

            for(int i = 0; i < SudokuForm.SudokuSize; i++)
                if(!BaseMatrix.Check(Matrix.Rows[i]) || !BaseMatrix.Check(Matrix.Cols[i]) || !BaseMatrix.Check(Matrix.Rectangles[i])) return false;

            return true;
        }

        public int NumDistinctValues()
        {
            int i, j;
            int count = 0;
            Boolean[] exists = new Boolean[SudokuForm.SudokuSize + 1];

            for(i = 0; i <= SudokuForm.SudokuSize; i++) exists[i] = false;
            for(i = 0; i < SudokuForm.SudokuSize; i++)
                for(j = 0; j < SudokuForm.SudokuSize; j++)
                    exists[GetValue(i, j)] = true;
            for(i = 1; i <= SudokuForm.SudokuSize; i++)
                if(exists[i]) count++;

            return count;
        }

        public event EventHandler Progress;
        protected virtual void OnProgress()
        {
            EventHandler handler = Progress;
            if(handler != null) handler(this, EventArgs.Empty);
        }

        private Boolean IsSolved()
        {
            int i, j;
            for(i = 0; i < SudokuForm.SudokuSize; i++)
                for(j = 0; j < SudokuForm.SudokuSize; j++)
                    if(GetValue(i, j) == Values.Undefined || !Check(i, j)) return false;

            return true;
        }

        private Boolean Check(int row, int col)
        {
            return !(Matrix.Cell(row, col).nPossibleValues == 0 && GetValue(row, col) == Values.Undefined && Matrix.Cell(row, col).DefinitiveValue == Values.Undefined);
        }

        public void SaveToFile(String file)
        {
            StreamWriter sw;
            try
            {
                sw = new StreamWriter(file);
                sw.Write(Serialize());
                sw.Close();
                Filename = file;
                Dirty = false;
            }
            catch(Exception) { throw; }
        }

        public void SaveToHTMLFile(String file)
        {
            StreamWriter sw;
            Char[] problem = Serialize().ToCharArray();
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
                        DateTime.Now.ToString("yyyy.MM.dd", new CultureInfo(Settings.Default.DisplayLanguage)),
                        AssemblyInfo.AssemblyCopyright
                    ));
                sw.Close();
            }
            catch(Exception) { throw; }
            return;
        }

        public String Serialize(Boolean includeROFlag = true)
        {
            String serializedProblem;
            byte offset = (byte)'0';

            serializedProblem = SudokuTypeIdentifier.ToString();
            for(int i = 0; i < SudokuForm.SudokuSize; i++)
                for(int j = 0; j < SudokuForm.SudokuSize; j++)
                    serializedProblem += (char)(GetValue(i, j) + (Matrix.Cell(i, j).ReadOnly && includeROFlag ? ReadOnlyOffset : 0) + offset);
            serializedProblem += SolvingTime.ToString().PadRight(16, '0');
            serializedProblem += Comment;
            if(matrix.HasCandidates())
                serializedProblem += (Environment.NewLine + SerializeCandiates(false) + Environment.NewLine + SerializeCandiates(true));

            return serializedProblem;
        }

        private String SerializeCandiates(Boolean exclusionCandidate)
        {
            Byte oneCandidate = 64;
            Byte bit = 0;
            String serializedCandidates = "";

            for(int row = 0; row < SudokuForm.SudokuSize; row++)
                for(int col = 0; col < SudokuForm.SudokuSize; col++)
                {
                    for(int candidate = 1; candidate <= SudokuForm.SudokuSize; candidate++)
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
                    if(++candidate > SudokuForm.SudokuSize)
                    {
                        candidate = 1;
                        if(++col >= SudokuForm.SudokuSize)
                        {
                            col = 0;
                            if(++row >= SudokuForm.SudokuSize)
                                return;
                        }
                    }
                }
            }
            return;
        }

        public Boolean SudokuOfTheDay()
        {
            return Load("https://sudoku.pi-c-it.de/misc/PrecalculatedProblems/SudokuOfTheDay.php");
        }

        public Boolean Load()
        {
            return Load("https://sudoku.pi-c-it.de/misc/PrecalculatedProblems/Load.php");
        }

        private Boolean Load(String URL)
        {
            Matrix.Init();
            WebClient client = new WebClient();
            try
            {
                client.Encoding = System.Text.Encoding.UTF8;
                String sudoku = client.UploadString(URL, "POST", new String(SudokuTypeIdentifier, 1)).Trim();
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
            Matrix.Init();

            try
            {
                char[] values = new char[SudokuForm.TotalCellCount];
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
                byte offset = (byte)'0';
                byte v = 0;

                if(!TimeSpan.TryParse(new String(elapsedTime), out solvingTime))
                    solvingTime = TimeSpan.Zero;
                if((Comment = initialComment) == null) // for compability reasons
                    Comment = String.Empty;

                Matrix.SetPredefinedValues = false;
                for(int i = 0; i < SudokuForm.SudokuSize; i++)
                    for(int j = 0; j < SudokuForm.SudokuSize; j++)
                    {
                        v = Convert.ToByte(values[i * SudokuForm.SudokuSize + j] - offset);
                        if(v >= ReadOnlyOffset)
                        {
                            Matrix.Cell(i, j).ReadOnly = (v > ReadOnlyOffset);
                            v -= ReadOnlyOffset;
                        }
                        SetValue(i, j, v);
                    }
                Matrix.SetPredefinedValues = true;
            }
            catch(Exception) { throw; }
        }
    }
}
