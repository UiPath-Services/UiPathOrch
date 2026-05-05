#Requires -Modules UiPathOrch
<#
.SYNOPSIS
    Imports the test fixture data from TestData/Fixture into a clean tenant.

.DESCRIPTION
    Loads the curated test fixture (folders, processes, queues, assets, buckets,
    triggers, machines, roles, package) into the specified target Orchestrator
    tenant. Assumes the target tenant is empty; conflicts will surface as errors.

    The fixture was exported from Orch1:\TestFixture_Base. This script remaps
    the Orch1: prefix in CSV Path columns to the target drive.

.PARAMETER TargetDrive
    Name of the target UiPathOrch PSDrive (e.g., 'OrchTest').
    The drive must already be mounted via Import-OrchConfig.

.PARAMETER FixturePath
    Path to the Fixture directory. Defaults to ../TestData/Fixture.

.PARAMETER CredentialPassword
    Password to populate empty CredentialPassword columns. Default: 'TestPassw0rd!'.

.EXAMPLE
    .\Import-Fixture.ps1 -TargetDrive OrchTest

.EXAMPLE
    .\Import-Fixture.ps1 -TargetDrive local -CredentialPassword 'MyP@ss123'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$TargetDrive,

    [string]$FixturePath = (Join-Path $PSScriptRoot '..\TestData\Fixture'),

    [string]$CredentialPassword = 'TestPassw0rd!'
)

$ErrorActionPreference = 'Stop'

$FixturePath = (Resolve-Path $FixturePath).Path
Write-Host "Fixture path: $FixturePath"

Get-PSDrive -Name $TargetDrive -ErrorAction Stop | Out-Null
$root = "${TargetDrive}:\TestFixture_Base"
Write-Host "Target root:  $root"

function Remap-Path {
    process {
        if ($_.Path) {
            $_.Path = $_.Path -replace '^Orch1:', "${script:TargetDrive}:"
        }
        $_
    }
}

# 1. Root folder
if (-not (Test-Path $root)) {
    New-Item -Path $root -ItemType Directory | Out-Null
}

# 2. Subfolders (children of TestFixture_Base, hierarchy preserved by Path column)
Write-Host "[ 1/11] Folders"
Import-Csv "$FixturePath\folders.csv" | Remap-Path | New-Item | Out-Null

# 3. Tenant machines
Write-Host "[ 2/11] Machines"
Import-Csv "$FixturePath\machines.csv" |
    ForEach-Object { $_.Path = "${TargetDrive}:\"; $_ } |
    New-OrchMachine | Out-Null

# 4. Tenant role
Write-Host "[ 3/11] Roles"
Import-Csv "$FixturePath\roles.csv" |
    ForEach-Object { $_.Path = "${TargetDrive}:\"; $_ } |
    Set-OrchRole | Out-Null

# 5. Package to tenant feed
Write-Host "[ 4/11] Packages (tenant feed)"
Get-ChildItem "$FixturePath\Packages\*.nupkg" | ForEach-Object {
    Import-OrchPackage -Path "${TargetDrive}:\" -Source $_.FullName | Out-Null
}

# Clear cache so subsequent New-OrchProcess calls see the freshly-uploaded
# package's entry-point IDs instead of stale ones from a previous import.
Clear-OrchCache -Path "${TargetDrive}:\"

# 6. Processes (depend on package + folders)
Write-Host "[ 5/11] Processes"
Import-Csv "$FixturePath\processes.csv" | Remap-Path | New-OrchProcess | Out-Null

# 7. Queues (queue-orders has SLA + Release reference, requires process)
Write-Host "[ 6/11] Queues"
Import-Csv "$FixturePath\queues.csv" | Remap-Path | New-OrchQueue | Out-Null

# 8. Standard assets
Write-Host "[ 7/11] Assets"
Import-Csv "$FixturePath\assets.csv" | Remap-Path | Set-OrchAsset | Out-Null

# 9. Credential assets (passwords are blank in CSV; fill from parameter)
Write-Host "[ 8/11] Credential assets"
Import-Csv "$FixturePath\credentials.csv" |
    ForEach-Object {
        $_.Path = $_.Path -replace '^Orch1:', "${TargetDrive}:"
        $_.CredentialPassword = $CredentialPassword
        $_
    } |
    Set-OrchCredentialAsset | Out-Null

# 10. Buckets
Write-Host "[ 9/11] Buckets"
Import-Csv "$FixturePath\buckets.csv" | Remap-Path | New-OrchBucket | Out-Null

# 11. Bucket items (directory naming: <FolderName>_<BucketName>)
Write-Host "[10/11] Bucket items"
Get-ChildItem "$FixturePath\BucketItems" -Directory | ForEach-Object {
    $parts  = $_.Name -split '_', 2
    $folder = $parts[0]
    $bucket = $parts[1]
    Import-OrchBucketItem `
        -Path "${TargetDrive}:\TestFixture_Base\$folder" `
        -Name $bucket `
        -Source "$($_.FullName)\*" | Out-Null
}

# 12. Triggers (depend on processes; all are Enabled=false to prevent firing)
Write-Host "[11/11] Triggers"
Import-Csv "$FixturePath\triggers.csv" | Remap-Path | New-OrchTrigger | Out-Null

Write-Host "Done." -ForegroundColor Green
