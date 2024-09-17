# Retrieve all jobs information from all mounted UiPathOrch drives
# and save it to a single CSV file.


$OutputDir = "c:\tmp"
$CsvEncoding = "utf-8"
$OutputCsv = "$OutputDir\all-folderusers.csv"

# Create the output directory if it does not exist
if (-not (Test-Path -Path $OutputDir)) {
    mkdir $OutputDir
}

# Retrieve a list of UiPathOrch provider drive names and store them in a variable
$drivePaths = (Get-PSDrive -PSProvider UiPathOrch) | % { "$($_.Name):\" }

# Retrieve users from each drive, display them in the console, and store in a variable
Get-OrchFolderUser -Recurse -Path $drivePaths | 
    Select-Object Path, 
                 Id, 
                 @{Name='UserName'; Expression={$_.UserEntity.UserName}}, 
                 @{Name='FullName'; Expression={$_.UserEntity.FullName}}, 
                 HasAlertsEnabled, 
                 @{Name='MayHaveAttended'; Expression={$_.UserEntity.MayHaveAttended}}, 
                 @{Name='MayHaveUnattended'; Expression={$_.UserEntity.MayHaveUnattended}} | 
    Tee-Object -Variable output

# Export the contents of the array to a CSV file
$output | Export-Csv -Path $OutputCsv -Encoding $CsvEncoding -NoTypeInformation

# Invoke Excel
ii $OutputCsv
