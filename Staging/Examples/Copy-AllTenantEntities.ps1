<# 
.SYNOPSIS
    This script copies all tenant entities from one UiPath Orchestrator drive to another, and logs the process.

.DESCRIPTION
    The script performs the following tasks:
    - Starts a transcript to log the process.
    - Copies libraries, packages, credential stores, roles, users, machines, calendars, and webhooks from the source drive to the destination drive.
    - Handles errors and ensures that the transcript is stopped even if an error occurs.

    After copying the tenant entities with this script, you can copy all folders by following these steps:
    1. In the Orchestrator web interface, start exploring all personal workspace folders that need to be copied in both the source and destination tenants.
       This operation is not supported by the API, so it must be done manually.
    2. Reflect the start of exploring personal workspace folders in your PowerShell session:
       PS> Clear-OrchCache -AllDrives
    3. Copy all folders using the following command. Please adjust the drive names according to your environment:
       PS> copy Src:\ Dst:\ -Recurse

.PARAMETER SourceDrive
    The name of the source drive where the entities will be copied from. For example, 'Src:'.

.PARAMETER DestinationDrive
    The name of the destination drive where the entities will be copied to. For example, 'Dst:'.

.EXAMPLE
    .\Copy-TenantEntities.ps1 Src: Dst:
    
.NOTES
    Version: 1.0
    Author: Yoshifumi Tsuda
    Date: 2024-09-02
#>

param (
    [Parameter(Mandatory=$true, Position=0)]
    [string]$SourceDrive,

    [Parameter(Mandatory=$true, Position=1)]
    [string]$DestinationDrive
)

Start-Transcript -Path "C:Copy-AllTenantEntities.log"

try {
    Clear-OrchCache -AllDrives

    Copy-OrchLibrary -Path "${SourceDrive}\" * * "${DestinationDrive}\"
    Copy-OrchPackage -Path "${SourceDrive}\" * * "${DestinationDrive}\"
    Copy-OrchCredentialStore -Path "${SourceDrive}\" * "${DestinationDrive}\"
    Copy-OrchRole -Path "${SourceDrive}\" * "${DestinationDrive}\"
    Copy-OrchUser -Path "${SourceDrive}\" * "${DestinationDrive}\"
    Copy-OrchMachine -Path "${SourceDrive}\" * "${DestinationDrive}\"
    Copy-OrchCalendar -Path "${SourceDrive}\" * "${DestinationDrive}\"
    Copy-OrchWebhook -Path "${SourceDrive}\" * "${DestinationDrive}\"
}
catch {
    Write-Error "An error occurred: $_.Exception.Message"
}
finally {
    Stop-Transcript
}
