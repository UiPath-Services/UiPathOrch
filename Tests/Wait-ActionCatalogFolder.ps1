#Requires -Modules UiPathOrch
<#
.SYNOPSIS
    Waits until the action catalog service accepts freshly created folders.

.DESCRIPTION
    Action catalogs are served by the Actions service, which learns of a folder some seconds after
    Orchestrator creates it; until then reading the folder's catalogs fails with errorCode 1100
    ("Folder does not exist or the user does not have access to the folder"). Measured on
    Automation Cloud (2026-11-04): 2 to 11 seconds after New-Item.

    A test that creates a source folder and copies it at once can hit that window: Copy-Item reads
    the SOURCE folder's catalogs, which no retry covers (Copy-Item retries only the create in the
    destination, see CopyItem.Entities.cs), and the action catalog stage fails. Call this after
    creating the source folders and before the copy.

.PARAMETER Path
    The folders to wait for.

.PARAMETER TimeoutSeconds
    How long to wait for each folder before failing. Default 30.

.EXAMPLE
    & "$PSScriptRoot\Wait-ActionCatalogFolder.ps1" -Path $script:SrcRoot, "$($script:SrcRoot)\Child"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string[]]$Path,

    [int]$TimeoutSeconds = 30
)

foreach ($p in $Path) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ($true) {
        try {
            Get-OrchActionCatalog -LiteralPath $p -ErrorAction Stop | Out-Null
            break
        }
        catch {
            if ((Get-Date) -ge $deadline) {
                throw "'$p' was still refused by the action catalog service after $TimeoutSeconds seconds: $($_.Exception.Message)"
            }
            # A refused read caches nothing, but clear the folder anyway so the next read is fresh.
            Clear-OrchCache -LiteralPath $p
            Start-Sleep -Milliseconds 500
        }
    }
}
