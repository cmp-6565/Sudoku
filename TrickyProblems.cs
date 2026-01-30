using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace Sudoku
{
    internal class TrickyProblems
    {
        private readonly ISudokuSettings settings;
        private IUserInteraction ui;

        private List<BaseProblem> problems;

        public TrickyProblems(ISudokuSettings settings, IUserInteraction ui)
        {
            problems = new List<BaseProblem>();
            this.settings = settings;
            this.ui = ui;
        }

        public void Add(BaseProblem problem)
        {
            problems.Add(problem);
        }

        public void Clear()
        {
            problems.Clear();
        }
        public async Task<Boolean> Publish()
        {
            if(Empty) return true;

            try
            {
                foreach(BaseProblem problem in problems)
                {
                    SudokuFileService fileService = new SudokuFileService(problem, settings, ui);
                    return await fileService.Upload();
                }
            }
            catch(Exception) { return false; }

            return true;
        }

        public Boolean Empty { get { return problems.Count == 0; } }
        public int Count { get { return problems.Count; } }
    }
}