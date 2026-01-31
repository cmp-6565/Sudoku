using System;
using System.Runtime.Serialization;

namespace Sudoku;

[Serializable()]
public class InvalidSudokuValueException: Exception
{
    public InvalidSudokuValueException(String s): base(s) { }
    public InvalidSudokuValueException(String s, Exception ex): base(s, ex) { }
    public InvalidSudokuValueException(): base() { }
}

public class MaxResultsReached: Exception
{
    public MaxResultsReached(): base() { }
}