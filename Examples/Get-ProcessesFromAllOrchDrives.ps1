# Retrieve all processes information from all mounted UiPathOrch drives
# and save it to a single CSV file.


$OutputDir = "c:\tmp"
$CsvEncoding = "utf-8"
$OutputCsv = "$OutputDir\all-processes.csv"

# Create the output directory if it does not exist
if (-not (Test-Path -Path $OutputDir)) {
    mkdir $OutputDir
}

# Retrieve a list of UiPathOrch provider drive names and store them in a variable
$drivePaths = (Get-PSDrive -PSProvider UiPathOrch) | ForEach-Object { "$($_.Name):\" }

# Retrieve processes from each drive, display them in the console, and store in a variable
Get-OrchProcess -Recurse -Path $drivePaths | Tee-Object -Variable output

# Export the contents of the array to a CSV file
$output | Export-Csv -Path $OutputCsv -Encoding $CsvEncoding -NoTypeInformation

# Invoke Excel
ii $OutputCsv
