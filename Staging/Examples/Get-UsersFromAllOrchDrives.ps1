# Retrieve all tenant users from all mounted UiPathOrch drives and save
# them to a single CSV file, flattening RolesList into a semicolon-
# delimited column.
#
# Drive names cannot be wildcarded in -Path, so this script enumerates
# all mounted UiPathOrch drives and passes the comma-separated list.


$OutputDir = "c:\tmp"
$CsvEncoding = "utf-8"
$OutputCsv = "$OutputDir\all-users.csv"

# Create the output directory if it does not exist
if (-not (Test-Path -Path $OutputDir)) {
    mkdir $OutputDir
}

# Retrieve a list of UiPathOrch provider drive names and store them in a variable
$drivePaths = Get-PSDrive -PSProvider UiPathOrch | ForEach-Object { "$($_.Name):\" }

# Retrieve users from each drive, display them in the console, and store in a variable
Get-OrchUser -Path $drivePaths |
    Select-Object *, @{Name = 'RolesListExpanded'; Expression = { $_.RolesList -join ';' }} |
    Tee-Object -Variable output

# Export the contents of the array to a CSV file
$output | Export-Csv -Path $OutputCsv -Encoding $CsvEncoding -NoTypeInformation

# Open the CSV in the default application (typically Excel)
Invoke-Item $OutputCsv
