# Retrieve all jobs created after midnight (the start of today) from all
# mounted UiPathOrch drives and save them to a single CSV file.
#
# Drive names cannot be wildcarded in -Path, so this script enumerates
# all mounted UiPathOrch drives and passes the comma-separated list.

$OutputDir = "c:\tmp"
$CsvEncoding = "utf-8"
$OutputCsv = "$OutputDir\all-jobs.csv"

# Create the output directory if it does not exist
if (-not (Test-Path -Path $OutputDir)) {
    mkdir $OutputDir
}

# Retrieve a list of UiPathOrch provider drive names and store them in a variable
$drivePaths = (Get-PSDrive -PSProvider UiPathOrch) | ForEach-Object { "$($_.Name):\" }

# Retrieve jobs from each drive, display them in the console, and store in a variable
Get-OrchJob -Path $drivePaths -Recurse -CreationTimeAfter ([DateTime]::Today) | Tee-Object -Variable output

if ($null -ne $output -and $output.Count -gt 0) {
    $output | Export-Csv -Path $OutputCsv -Encoding $CsvEncoding -NoTypeInformation
    Invoke-Item $OutputCsv
}
else {
    Write-Host 'No jobs found to export.'
}
