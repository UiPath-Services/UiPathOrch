# Retrieve all job information created after midnight (the start of today)
# from all mounted UiPathOrch drives and save it to a single CSV file.

$OutputDir = "c:\tmp"
$CsvEncoding = "utf-8"
$OutputCsv = "$OutputDir\all-jobs.csv"

# Create the output directory if it does not exist
if (-not (Test-Path -Path $OutputDir)) {
    mkdir $OutputDir
}

# Retrieve a list of UiPathOrch provider drive names and store them in a variable
$drivePaths = (Get-PSDrive -PSProvider UiPathOrch) | ForEach-Object { "$($_.Name):\" }

# Retrieve processes from each drive, display them in the console, and store in a variable
Get-OrchJob -Path $drivePaths -Recurse -CreationTimeAfter $([DateTime]::Today) | Tee-Object -Variable output

if ($output -ne $null -and $output.Count -gt 0) {
    $output | Export-Csv -Path $OutputCsv -Encoding $CsvEncoding -NoTypeInformation
    ii $OutputCsv
}
else {
    Write-Host "No jobs found to export."
}
