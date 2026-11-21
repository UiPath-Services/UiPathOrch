#Requires -Modules UiPathOrch
<#
.SYNOPSIS
    Runs a share (Add-Orch{Asset,Queue,Bucket}Link) again while the server still refuses a
    freshly created folder.

.DESCRIPTION
    Right after New-Item creates a folder, Orchestrator can refuse a share that involves it with
    "You don't have QueuesView permissions in folder ..." (errorCode 1017), although the user's
    role is inherited from the parent; the same call goes through some seconds later. Measured on
    Automation Cloud (2026-11-21): refused twice, accepted about 20 seconds after New-Item.

    The share APIs are all-or-nothing per call, so running the action again is safe. Any other
    error is thrown at once. The action must make errors terminating (-ErrorAction Stop).

    Dot-source it, as in the example: called with &, this script is a new script scope, and the
    action's $script: variables resolve there (null) instead of in the test file.

.PARAMETER Action
    The share to run, with -ErrorAction Stop.

.PARAMETER TimeoutSeconds
    How long to keep trying before failing. Default 60.

.EXAMPLE
    . "$PSScriptRoot\Invoke-UntilFolderPermitted.ps1" {
        Add-OrchQueueLink -Path $src -Name q -Link $dst -Confirm:$false -ErrorAction Stop
    }
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory, Position = 0)]
    [scriptblock]$Action,

    [int]$TimeoutSeconds = 60
)

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ($true) {
    try {
        & $Action
        return
    }
    catch {
        if ("$($_.Exception.Message)" -notmatch 'permissions in folder' -or (Get-Date) -ge $deadline) { throw }
        Start-Sleep -Seconds 3
    }
}
