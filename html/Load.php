<?php
// load.php - Lädt ein zufälliges Sudoku
// Nachbildung der Logik von load.aspx.cs

// Identifiers wie im C# Code definiert
const SUDOKU_IDENTIFIER = '9';
const X_SUDOKU_IDENTIFIER = 'X';

// Dateinamen
const NORMAL_SUDOKUS_FILE = "NormalSudokus.sudoku";
const X_SUDOKUS_FILE = "XSudokus.sudoku";

// Feste Längen
const LENGTH = 81;        // Reines Sudoku
const RECORD_LENGTH = 83; // Sudoku + CR + LF (81 + 2)

try {
    // 1. Request Body lesen (Exakt 1 Byte erwartet)
    $input = file_get_contents("php://input");
    
    if (strlen($input) !== 1) {
        throw new Exception("Invalid Request: " . strlen($input));
    }

    $type = $input[0];
    $filename = "";

    // 2. Dateinamen basierend auf dem Typ bestimmen
    if ($type === SUDOKU_IDENTIFIER) {
        $filename = NORMAL_SUDOKUS_FILE;
    } elseif ($type === X_SUDOKU_IDENTIFIER) {
        $filename = X_SUDOKUS_FILE;
    } else {
        throw new Exception("Invalid Sudoku type: " . $type);
    }

    // Pfad zur Datei (im selben Verzeichnis wie das Skript)
    $filepath = __DIR__ . DIRECTORY_SEPARATOR . $filename;

    if (!file_exists($filepath)) {
        throw new Exception("Data file not found: " . $filename);
    }

    $filesize = filesize($filepath);
    
    // 3. Anzahl der verfügbaren Probleme berechnen
    // C# Logik: (int)(fi.Length/(length+2))-1
    $numProblems = floor($filesize / RECORD_LENGTH) - 1;

    if ($numProblems < 1) {
        throw new Exception("Not enough problems in file.");
    }

    // 4. Zufälligen Index wählen
    // C#: new Random().Next(...)
    $randomIndex = rand(0, $numProblems - 1); // rand ist inklusiv, daher -1

    // Offset berechnen
    $offset = $randomIndex * RECORD_LENGTH;

    // 5. Datei lesen
    $fp = fopen($filepath, 'r');
    if (!$fp) {
        throw new Exception("Could not open file.");
    }

    // Zum berechneten Offset springen
    if (fseek($fp, $offset) !== 0) {
        fclose($fp);
        throw new Exception("Seek failed.");
    }

    // 81 Bytes lesen (das eigentliche Sudoku)
    $sudokuData = fread($fp, LENGTH);
    fclose($fp);

    if ($sudokuData === false || strlen($sudokuData) !== LENGTH) {
        throw new Exception("Error reading sudoku data.");
    }

    // 6. Rückgabe
    echo $sudokuData;

} catch (Exception $ex) {
    // Fehlerbehandlung analog zum C# catch-Block
    echo "ERROR: " . $ex->getMessage();
}
?>