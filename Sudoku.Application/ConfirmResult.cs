// Sudoku.Application/ConfirmResult.cs
using System;

namespace Sudoku.Application;

public enum ConfirmResult: int { Yes = 0, No = 1, Cancel = 2 }
public enum ConfirmOptions: int { YesNo = 0, YesNoCancel = 1, OkCancel = 2 }