using System;
using System.Collections.Generic;
using System.Net;

namespace Sudoku
{
    internal class TrickyProblems
    {
        private List<BaseProblem> problems;

        public TrickyProblems()
        {
            problems = new List<BaseProblem>();
        }

        public void Add(BaseProblem problem)
        {
            problems.Add(problem);
        }

        public void Clear()
        {
            problems.Clear();
        }

        public Boolean Publish()
        {
            if(Empty) return true;

            WebClient client = new WebClient();
            try
            {
                foreach(BaseProblem problem in problems)
                {
                    String sudoku = problem.Serialize().Substring(0, SudokuForm.TotalCellCount + 1);
                    if(client.UploadString("http://sudoku.pi-c-it.de/misc/Hard%20Games/Original/Upload/upload.aspx", sudoku).Trim() != sudoku)
                        return false;
                }
            }
            catch(Exception) { return false; }

            return true;
        }

        public Boolean Empty { get { return problems.Count == 0; } }
        public int Count { get { return problems.Count; } }
    }
}