<#
.SYNOPSIS
    Captures tab completion results from the current module as a baseline.
    Output: Tests\CompleterBaseline.clixml

.NOTES
    Run in a fresh PowerShell session with UiPathOrch loaded and Orch drives connected.
#>
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'CompleterBaseline.clixml')
)

Import-Module UiPathOrch -ErrorAction Stop

# Verify drive
$drive = 'Orch1'
Get-PSDrive $drive -ErrorAction Stop | Out-Null
Push-Location "${drive}:\Shared"

function Complete-Parameter {
    param([string]$InputScript)
    $result = [System.Management.Automation.CommandCompletion]::CompleteInput(
        $InputScript, $InputScript.Length, $null)
    return $result.CompletionMatches
}

function Capture-Completer {
    param([string]$Command)
    $results = Complete-Parameter $Command
    if ($null -eq $results -or $results.Count -eq 0) { return ,[PSCustomObject[]]@() }
    $list = [System.Collections.Generic.List[PSCustomObject]]::new()
    foreach ($r in $results) {
        $list.Add([PSCustomObject]@{
            CompletionText = $r.CompletionText
            ListItemText   = $r.ListItemText
            ResultType     = $r.ResultType.ToString()
            ToolTip        = $r.ToolTip
        })
    }
    return ,$list.ToArray()
}

$baseline = @{}

# FolderScoped (migrated)
$baseline['Get-OrchQueue -Name']           = Capture-Completer 'Get-OrchQueue -Name '
$baseline['Get-OrchProcess -Name']         = Capture-Completer 'Get-OrchProcess -Name '
$baseline['Get-OrchActionCatalog -Name']   = Capture-Completer 'Get-OrchActionCatalog -Name '
$baseline['Get-OrchTrigger -Name']         = Capture-Completer 'Get-OrchTrigger -Name '
$baseline['Get-OrchEventTrigger -Name']    = Capture-Completer 'Get-OrchEventTrigger -Name '
$baseline['Get-OrchApiTrigger -Name']      = Capture-Completer 'Get-OrchApiTrigger -Name '
$baseline['Start-OrchJob -Name']           = Capture-Completer 'Start-OrchJob -Name '

# DriveScoped (migrated)
$baseline['Get-OrchMachine -Name']         = Capture-Completer 'Get-OrchMachine -Name '
$baseline['Get-OrchRole -Name']            = Capture-Completer 'Get-OrchRole -Name '
$baseline['Get-OrchCalendar -Name']        = Capture-Completer 'Get-OrchCalendar -Name '
$baseline['Copy-OrchCredentialStore -Name'] = Capture-Completer 'Copy-OrchCredentialStore -Name '
$baseline['Get-OrchWebhook -Name']         = Capture-Completer 'Get-OrchWebhook -Name '

# Positional (same commands, no -Name)
$baseline['Get-OrchQueue (positional)']    = Capture-Completer 'Get-OrchQueue '
$baseline['Get-OrchMachine (positional)']  = Capture-Completer 'Get-OrchMachine '
$baseline['Get-OrchRole (positional)']     = Capture-Completer 'Get-OrchRole '

# FolderScoped with ResolvePathWithoutPersonalWorkspace (migrated)
$baseline['Get-OrchTestCase -Name']        = Capture-Completer 'Get-OrchTestCase -Name '
$baseline['Get-OrchTestDataQueue -Name']   = Capture-Completer 'Get-OrchTestDataQueue -Name '
$baseline['Get-OrchTestSetSchedule -Name'] = Capture-Completer 'Get-OrchTestSetSchedule -Name '
$baseline['Get-OrchTestSet -Name']         = Capture-Completer 'Get-OrchTestSet -Name '
$baseline['Get-OrchFolderMachine -Name']   = Capture-Completer 'Get-OrchFolderMachine -Name '

# Non-migrated (should be unchanged)
$baseline['Get-OrchAsset -Name']           = Capture-Completer 'Get-OrchAsset -Name '
$baseline['Get-OrchBucket -Name']          = Capture-Completer 'Get-OrchBucket -Name '

Pop-Location

$baseline | Export-Clixml -Path $OutputPath -Depth 5
Write-Host "Baseline saved to: $OutputPath"
Write-Host "Captured $($baseline.Keys.Count) command completions."
foreach ($key in $baseline.Keys | Sort-Object) {
    Write-Host "  $key : $($baseline[$key].Count) candidates"
}
