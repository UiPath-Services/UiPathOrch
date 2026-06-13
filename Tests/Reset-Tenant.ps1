#Requires -Modules UiPathOrch
<#
.SYNOPSIS
    Wipes a tenant clean for test runs. Preserves the current user.

.DESCRIPTION
    Deletes all deletable entities from the target tenant in dependency order.
    Intended to be run before Import-Fixture.ps1 so each test starts from a
    known empty state.

    Preserves:
    - Current authenticated user (deleting it would lock us out)
    - Personal workspaces (tied to user existence)
    - System / built-in roles (non-deletable)
    - The OAuth external app or service account used to connect

    DESTRUCTIVE. Always confirm the -TargetDrive points to a disposable tenant
    before running.

.PARAMETER TargetDrive
    Name of the target UiPathOrch PSDrive. Must already be mounted.

.PARAMETER Confirm
    Use -Confirm:$false to skip the safety prompt.

.EXAMPLE
    .\Reset-Tenant.ps1 -TargetDrive OrchTest

.EXAMPLE
    .\Reset-Tenant.ps1 -TargetDrive local -Confirm:$false
#>
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [string]$TargetDrive
)

$ErrorActionPreference = 'Stop'
Get-PSDrive -Name $TargetDrive -ErrorAction Stop | Out-Null
$root = "${TargetDrive}:\"

if (-not $PSCmdlet.ShouldProcess($root, "DELETE all deletable entities")) {
    return
}

$currentUser = (Get-OrchCurrentUser -Path $root).UserName
Write-Host "Target:       $root"
Write-Host "Preserve user: $currentUser"

# Folder-scoped: iterate every non-personal folder, blow away contents
$folders = Get-ChildItem $root -Recurse |
    Where-Object { $_.FolderType -ne 'Personal' } |
    Sort-Object { ($_.FullyQualifiedName -split '/').Count } -Descending

# Folder names can contain PowerShell wildcard metacharacters — e.g. a folder
# literally named "test [test]" or "ZZbt[1]". Resolve the per-folder target with
# -LiteralPath, NOT -Path: under -Path the "[test]" is a wildcard char-class that
# matches no real folder, so the folder-scoped cmdlets resolve to nothing and THROW
# a terminating "no folders resolved" guard. That guard is NOT suppressed by
# -ErrorAction SilentlyContinue and is fatal under this script's
# $ErrorActionPreference='Stop', so a single bracket-named folder aborted the whole
# reset (and -Path also failed to actually delete such folders in step [6/9]).
# The try/catch is a best-effort safety net for any other stray terminating error;
# every folder is deleted outright in step [6/9] regardless.
Write-Host "[1/9] Triggers + folder assignments"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    try {
        Remove-OrchTrigger        -LiteralPath $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchFolderMachine  -LiteralPath $fp -Name '*'                 -ErrorAction SilentlyContinue
        Get-OrchFolderUser -LiteralPath $fp -ErrorAction SilentlyContinue |
            Where-Object { $_.UserEntity.UserName -ne $currentUser } |
            ForEach-Object {
                Remove-OrchFolderUser -LiteralPath $fp -UserName $_.UserEntity.UserName `
                    -ErrorAction SilentlyContinue
            }
    } catch { Write-Verbose "Reset-Tenant [1/9] '$fp': $($_.Exception.Message)" }
}

# Queues must be deleted before Processes — an SLA-enabled queue references
# its Release (Process) and blocks Process deletion otherwise.
Write-Host "[2/9] Queues"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    try {
        Remove-OrchQueue -LiteralPath $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
    } catch { Write-Verbose "Reset-Tenant [2/9] '$fp': $($_.Exception.Message)" }
}

Write-Host "[3/9] Processes"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    try {
        Remove-OrchProcess -LiteralPath $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
    } catch { Write-Verbose "Reset-Tenant [3/9] '$fp': $($_.Exception.Message)" }
}

Write-Host "[4/9] Buckets + bucket items"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    try {
        foreach ($b in (Get-OrchBucket -LiteralPath $fp -ErrorAction SilentlyContinue)) {
            Remove-OrchBucketItem -LiteralPath $fp -Name $b.Name -FullPath '*' -Confirm:$false -ErrorAction SilentlyContinue
        }
        Remove-OrchBucket -LiteralPath $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
    } catch { Write-Verbose "Reset-Tenant [4/9] '$fp': $($_.Exception.Message)" }
}

Write-Host "[5/9] Assets"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    try {
        Remove-OrchAsset -LiteralPath $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
    } catch { Write-Verbose "Reset-Tenant [5/9] '$fp': $($_.Exception.Message)" }
}

Write-Host "[6/9] Folders (deepest first)"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    try {
        Remove-Item -LiteralPath $fp -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    } catch { Write-Verbose "Reset-Tenant [6/9] '$fp': $($_.Exception.Message)" }
}

Write-Host "[7/9] Tenant packages"
Get-OrchPackage -Path $root -ErrorAction SilentlyContinue |
    Remove-OrchPackage -Confirm:$false -ErrorAction SilentlyContinue

Write-Host "[8/9] Tenant machines"
Get-OrchMachine -Path $root -ErrorAction SilentlyContinue |
    Remove-OrchMachine -Confirm:$false -ErrorAction SilentlyContinue

Write-Host "[9/9] Tenant custom roles + users (preserve current + system)"
Get-OrchRole -Path $root -ErrorAction SilentlyContinue |
    Where-Object { $_.IsEditable -eq $true } |
    Remove-OrchRole -Confirm:$false -ErrorAction SilentlyContinue

Get-OrchUser -Path $root -ErrorAction SilentlyContinue |
    Where-Object { $_.UserName -and $_.UserName -ne $currentUser -and $_.UserType -ne 'Robot' } |
    Remove-OrchUser -Confirm:$false -ErrorAction SilentlyContinue

# Reset the target drive's cwd. Any folder the caller had cd'd into is now
# deleted, and a stale cwd silently breaks downstream cmdlets that resolve
# paths relative to it (notably Import-Fixture's link rows, which pipe
# Path/Link via property-name binding through resolvers that consult the
# current location for context). The PSDriveInfo object is registered in
# the runspace, so mutating CurrentLocation here persists in the caller's
# session for the next operation.
$drv = Get-PSDrive -Name $TargetDrive -ErrorAction SilentlyContinue
if ($drv -and $drv.CurrentLocation) {
    Write-Host "Resetting cwd on ${TargetDrive}: from '$($drv.CurrentLocation)' to drive root (previous folder was wiped)"
    $drv.CurrentLocation = ''
}

Write-Host "Done." -ForegroundColor Green
