<#
.SYNOPSIS
    This script reads UserNames from a CSV file and completely removes those users from the tenant.

.DESCRIPTION
    The script reads a CSV file containing a list of users with `Path` and `UserName` columns and executes multiple cmdlets 
    to remove users and their associated workspaces from UiPath Orchestrator. It also logs all actions performed 
    during the execution.

.PARAMETER csvFilePath
    The path to the CSV file containing user data. The file must include `Path` and `UserName` columns.

.EXAMPLE
    .\Remove-TenantUsersFromCsv.ps1 "C:\path\to\your\file.csv"

    This command executes the script with the specified CSV file.

.NOTES
    - Ensure that the specified file path is correct and accessible.
    - The script logs all actions to a transcript file.
    - Add `-Encoding` option to `Import-Csv` if needed, for example: `Import-Csv $csvFilePath -Encoding utf8`.

#>

param (
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$csvFilePath
)

if (-not (Test-Path $csvFilePath)) {
    Write-Error "The specified CSV file does not exist: $csvFilePath"
    exit 1
}

try {
    Start-Transcript

    $data = Import-Csv $csvFilePath

    # Execute the cmdlets with the imported data
    try {
        $data | ForEach-Object {
            $username = $_.UserName
            $path = $_.Path
            $matchingWorkspace = Get-OrchPersonalWorkspace -Path $path | Where-Object { $_.OwnerName -eq $username }

            if ($matchingWorkspace) {
                $wsname = $matchingWorkspace | Select-Object -ExpandProperty Name
                Remove-OrchUser -Path $path -UserName $username -Verbose
                Remove-OrchPersonalWorkspace -Path $path -Name $wsname -Verbose
            } else {
                Remove-OrchUser -UserName $username -Verbose
            }
        }
        Write-Output "Remove-OrchUser and Remove-OrchPersonalWorkspace executed successfully."
        Write-Output ""

        $data | Remove-PmGroupMember -GroupName * -Verbose
        Write-Output "Remove-PmGroupMember executed successfully."
        Write-Output ""

        $data | Remove-PmUser -Verbose
        Write-Output "Remove-PmUser executed successfully."
        Write-Output ""
    } catch {
        Write-Error "An error occurred while executing cmdlets: $_"
        exit 1
    }
} catch {
    Write-Error "Failed to import CSV file or execute cmdlets: $_"
    exit 1
} finally {
    Write-Output "All cmdlets executed successfully and logging completed."
    $transcript = Stop-Transcript
    Write-Output $transcript
}
