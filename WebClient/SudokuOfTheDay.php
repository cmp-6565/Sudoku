<?php
// sudokuOfTheDay.php

// Identifiers wie im C# Code definiert (SudokuProblem='9', XSudokuProblem='X')
const SUDOKU_IDENTIFIER='9';
const X_SUDOKU_IDENTIFIER='X';

const NORMAL_SUDOKUS_FILE="NormalSudokus.sudoku";
const X_SUDOKUS_FILE="XSudokus.sudoku";
const LENGTH=81; // Länge eines Sudokus ohne CRLF
const RECORD_LENGTH=83; // 81 Zeichen + CR + LF (entspricht length+2 im C#-Code)

// Startdatum aus der Original-Logik (1. Juni 2009)
$firstProblemDate=new DateTime('2009-06-01');

try {
    // 1. Request Body lesen (es wird genau 1 Byte erwartet)
    $input=file_get_contents("php://input");
    
    if (strlen($input) !== 1) {
        throw new Exception("Invalid Request: " . strlen($input));
    }

    $type=$input[0];
    $filename="";

    // 2. Dateinamen basierend auf dem Typ bestimmen
    if ($type === SUDOKU_IDENTIFIER) {
        $filename=NORMAL_SUDOKUS_FILE;
    } elseif ($type === X_SUDOKU_IDENTIFIER) {
        $filename=X_SUDOKUS_FILE;
    } else {
        throw new Exception("Invalid Sudoku type: " . $type);
    }

    // Pfad zur Datei (im selben Verzeichnis)
    $filepath=__DIR__ . DIRECTORY_SEPARATOR . $filename;

    if (!file_exists($filepath)) {
        throw new Exception("Data file not found: " . $filename);
    }

    $filesize=filesize($filepath);
    
    // 3. Berechnung des Tages-Index (Logik aus C# übernommen)
    // C#: ((int)(fi.Length/(length+2))-1)
    $numProblems=floor($filesize / RECORD_LENGTH) - 1;

    if ($numProblems < 1) {
        throw new Exception("Not enough problems in file.");
    }

    // Differenz in Tagen berechnen
    $now=new DateTime();
    // Reset time to midnight to match C# DateTime.Now.Date logic purely based on days
    $now->setTime(0, 0, 0); 
    
    $interval=$firstProblemDate->diff($now);
    $daysPassed=$interval->days;
    // diff() ist absolut, Sicherheitshalber prüfen ob wir in der Zukunft sind
    if ($now < $firstProblemDate) {
        $daysPassed=0;
    }

    // Modulo Operation um den Index zu bestimmen
    $index=$daysPassed % $numProblems;
    
    // Offset berechnen
    $offset=$index * RECORD_LENGTH;

    // 4. Datei lesen
    $fp=fopen($filepath, 'r');
    if (!$fp) {
        throw new Exception("Could not open file.");
    }

    // Zum berechneten Offset springen
    if (fseek($fp, $offset) !== 0) {
        fclose($fp);
        throw new Exception("Seek failed.");
    }

    // 81 Bytes lesen (das eigentliche Sudoku)
    $sudokuData=fread($fp, LENGTH);
    fclose($fp);

    if ($sudokuData === false || strlen($sudokuData) !== LENGTH) {
        throw new Exception("Error reading sudoku data.");
    }

    // 5. Ergebnis ausgeben (kein Header, nur der String)
    echo $sudokuData;

    // Optional: Logging (wie im Original)
    // $logEntry=date('Y-m-d H:i:s') . ": " . $_SERVER['REMOTE_ADDR'] . ", " . $filename . "\n";
    // file_put_contents(__DIR__ . "/SudokuOfTheDay.log", $logEntry, FILE_APPEND);

} catch (Exception $ex) {
    // Fehler werden vom Client erwartet, wenn der String mit "ERROR" beginnt
    echo "ERROR: " . $ex->getMessage();
}
?>