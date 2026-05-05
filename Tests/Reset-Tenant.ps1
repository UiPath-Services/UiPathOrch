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

Write-Host "[1/9] Triggers + folder assignments"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    Remove-OrchTrigger        -Path $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchFolderMachine  -Path $fp -Name '*'                 -ErrorAction SilentlyContinue
    Get-OrchFolderUser -Path $fp -ErrorAction SilentlyContinue |
        Where-Object { $_.UserEntity.UserName -ne $currentUser } |
        ForEach-Object {
            Remove-OrchFolderUser -Path $fp -UserName $_.UserEntity.UserName `
                -ErrorAction SilentlyContinue
        }
}

# Queues must be deleted before Processes — an SLA-enabled queue references
# its Release (Process) and blocks Process deletion otherwise.
Write-Host "[2/9] Queues"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    Remove-OrchQueue -Path $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host "[3/9] Processes"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    Remove-OrchProcess -Path $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host "[4/9] Buckets + bucket items"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    foreach ($b in (Get-OrchBucket -Path $fp -ErrorAction SilentlyContinue)) {
        Remove-OrchBucketItem -Path $fp -Name $b.Name -FullPath '*' -Confirm:$false -ErrorAction SilentlyContinue
    }
    Remove-OrchBucket -Path $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host "[5/9] Assets"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    Remove-OrchAsset -Path $fp -Name '*' -Confirm:$false -ErrorAction SilentlyContinue
}

Write-Host "[6/9] Folders (deepest first)"
foreach ($f in $folders) {
    $fp = "${TargetDrive}:\$($f.FullyQualifiedName)"
    Remove-Item -Path $fp -Recurse -Confirm:$false -ErrorAction SilentlyContinue
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

Write-Host "Done." -ForegroundColor Green
