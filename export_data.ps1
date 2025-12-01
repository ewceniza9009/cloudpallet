$server = "localhost"
$database = "WmsColdStorageDb"
$outputDir = "x:\wms\wms-frontend\src\assets\mock-data"

$tables = @(
    "Companies",
    "Warehouses",
    "Locations",
    "Materials",
    "UnitsOfMeasure",
    "Pallets",
    "Docks",
    "YardSpots",
    "MaterialCategories"
)

# Ensure output directory exists
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir
}

foreach ($table in $tables) {
    Write-Host "Exporting $table..."
    
    $query = "SET NOCOUNT ON; SELECT * FROM [$table] FOR JSON PATH"
    
    # Use sqlcmd to get the JSON string. 
    # -y 0 is important to prevent truncation of large outputs (0 = unlimited)
    # We cannot use -h -1 with -y 0, so we will strip headers manually.
    $output = sqlcmd -S $server -d $database -Q $query -y 0
    
    if ($output) {
        # The output will contain the column name (JSON_...) and dashes.
        # We assume the JSON starts with '['.
        # We join the array into a string.
        $fullText = $output -join ""
        
        # Find the start of the JSON array
        $startIndex = $fullText.IndexOf("[")
        if ($startIndex -ge 0) {
            $json = $fullText.Substring($startIndex)
            $json | Out-File -FilePath "$outputDir\$table.json" -Encoding utf8
            Write-Host "Exported $table to $outputDir\$table.json"
        }
        else {
            Write-Warning "No JSON array found in output for $table."
            "[]" | Out-File -FilePath "$outputDir\$table.json" -Encoding utf8
        }
    }
    else {
        Write-Warning "No data found for $table or error occurred."
        "[]" | Out-File -FilePath "$outputDir\$table.json" -Encoding utf8
    }
}

Write-Host "Export complete."
