<#
.SYNOPSIS
    This script imports data from a CSV file and sequentially executes cmdlets to add users and groups to UiPath Orchestrator.

.DESCRIPTION
    The script takes a CSV file containing information about Process Member Groups, Users, and Folder Users. It reads each row and uses the provided data to:
    1. Add a member to a Process Member Group using `Add-OrchPmMemberToPmGroup`.
    2. Add a user using `Add-OrchUser`.
    3. Add a user to a folder using `Add-OrchFolderUser`.

.PARAMETER CsvFilePath
    The path to the CSV file containing the data.

.PARAMETER Encoding
    The encoding of the CSV file. Defaults to "utf8". Other valid encodings include "utf7", "unicode", "bigendianunicode", "utf32", "ascii", "default", "oem".

.EXAMPLE
    .\Add-TenantUser.ps1 "C:\data\users.csv" "utf8"
    This example reads the CSV file located at "C:\data\users.csv" using UTF-8 encoding and processes each row to add members, users, and folder users.

.EXAMPLE
    .\Add-TenantUser.ps1 "C:\data\users.csv" "unicode"
    This example reads the CSV file located at "C:\data\users.csv" using Unicode encoding.

.NOTES
    Ensure the CSV file has the columns: PmGroupName, MemberName, UserName, FolderName.
    This script assumes the necessary cmdlets (Add-OrchPmMemberToPmGroup, Add-OrchUser, Add-OrchFolderUser) are available and properly configured.

#>

param (
    [string]$CsvFilePath,    # Positional parameter 0: Path to the CSV file
    [string]$Encoding = "utf8" # Positional parameter 1: Encoding of the CSV file (default is utf8)
)

# Import the CSV file
$csvData = Import-Csv -Path $CsvFilePath -Encoding $Encoding

# Process each row sequentially
foreach ($row in $csvData) {
    # Retrieve columns from the CSV data
    $path = $row.Path
    $groupName = $row.GroupName
    $objectType = $row.ObjectType
    $name = $row.Name

    # Execute Add-OrchPmMemberToPmGroup cmdlet
    Add-OrchPmMemberToPmGroup -Path $path -GroupName $groupName -ObjectType $objectType -Name $name

    # Execute Add-OrchUser cmdlet
    Add-OrchUser -UserName $userName

    # Execute Add-OrchFolderUser cmdlet
    Add-OrchFolderUser -FolderName $folderName -UserName $userName
}
