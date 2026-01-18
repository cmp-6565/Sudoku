using System;
using System.Runtime.Serialization;

namespace Sudoku
{
    [Serializable()]
    public class InvalidSudokuValueException: Exception
    {
        public InvalidSudokuValueException(String s) : base(s) { }
        public InvalidSudokuValueException(String s, Exception ex) : base(s, ex) { }
        public InvalidSudokuValueException() : base() { }
        protected InvalidSudokuValueException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}