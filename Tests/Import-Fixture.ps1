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

# 4b. Tenant directory users (the directory identity must already exist in
# the org — Reset-Tenant only removes the tenant assignment, not the org
# directory entry). SelfContained.Tests.ps1 requires at least one
# DirectoryUser other than the current user; we re-add them here.
Write-Host "[ 3b/11] Users (re-add to tenant)"
Import-Csv "$FixturePath\users.csv" |
    ForEach-Object { $_.Path = "${TargetDrive}:\"; $_ } |
    Add-OrchUser -ErrorAction SilentlyContinue | Out-Null

# 4c. Folder-scoped machine assignments. Requires both folders (from step 1)
# and tenant machines (from step 2) to be in place; doesn't depend on packages
# or any folder-content entity. Done here so subsequent steps can reference
# folder-machine relationships if needed.
Write-Host "[ 3c/12] Folder machine assignments"
Import-Csv "$FixturePath\folder_machines.csv" |
    ForEach-Object {
        $_.Path = $_.Path -replace '^Orch1:', "${TargetDrive}:"
        $_
    } |
    Add-OrchFolderMachine -Confirm:$false | Out-Null

# 5. Package to tenant feed. Tolerate "already exists" — some OC builds (e.g.
# 20.10/ApiVer 11) won't let Remove-OrchPackage during Reset clean the
# previous upload, but the package is otherwise the right one.
Write-Host "[ 4/11] Packages (tenant feed)"
Get-ChildItem "$FixturePath\Packages\*.nupkg" | ForEach-Object {
    Import-OrchPackage -Path "${TargetDrive}:\" -Source $_.FullName -ErrorAction Continue 2>&1 |
        Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] -or
                       $_.Exception.Message -notmatch 'already exists' } |
        Out-Null
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
Write-Host "[11/17] Triggers"
Import-Csv "$FixturePath\triggers.csv" | Remap-Path | New-OrchTrigger | Out-Null

# 12a. API triggers (depend on processes; all Enabled=false to prevent firing).
# The cmdlet defaults Tags=[], MachineRobots=[{}], Slug=Name when omitted —
# all server-required, so barebones-ish CSV rows still POST cleanly.
Write-Host "[11a/17] API triggers"
Import-Csv "$FixturePath\api_triggers.csv" | Remap-Path | New-OrchApiTrigger | Out-Null

# 12b. Test data queues. ContentJsonSchema defaults to '{}' when omitted.
Write-Host "[11b/17] Test data queues"
Import-Csv "$FixturePath\test_data_queues.csv" | Remap-Path | New-OrchTestDataQueue | Out-Null

# 12b2. Test data queue items (depend on the queues just created). Per-queue
# web-format CSVs under TestDataQueueItems\<Folder>_<Queue>.csv: the header
# row is the queue's schema property names, matching the Orchestrator web
# "Upload Items" format that Import-OrchTestDataQueueItem consumes.
Write-Host "[11b2/17] Test data queue items"
Get-ChildItem "$FixturePath\TestDataQueueItems\*.csv" | ForEach-Object {
    $parts  = $_.BaseName -split '_', 2
    $folder = $parts[0]
    $queue  = $parts[1]
    Import-OrchTestDataQueueItem `
        -Path "${TargetDrive}:\TestFixture_Base\$folder" `
        -Name $queue `
        -ImportCsv $_.FullName -Confirm:$false | Out-Null
}

# 12b3. Test set (depends on a test-automation package in Packages\, e.g.
# TestAutomation.*.nupkg, uploaded in step 4). New-OrchTestSet needs the
# package's deployed release id + test-case definition ids, so create a
# process first, then build the typed TestSetPackage[] / TestCase[] arrays.
# Test set *schedules* are intentionally NOT seeded -- their creation is
# tenant-gated (errorCode 3234 "not allowed for this tenant" on some tenants),
# so CopyItem.RoundTrip's test-set check covers the set, not the schedule.
Write-Host "[11b3/17] Test set"
$tsPkg = 'TestAutomation'
$tsVer = @(Get-OrchPackageVersion -Path $root | Where-Object Id -eq $tsPkg |
    Select-Object -ExpandProperty Version) | Sort-Object -Descending | Select-Object -First 1
if ($tsVer) {
    New-OrchProcess -Path $root -Id $tsPkg -Version $tsVer -Name $tsPkg -ErrorAction SilentlyContinue | Out-Null
    Clear-OrchCache -Path $root
    $tsRel = @(Get-OrchProcess -Path $root | Where-Object ProcessKey -eq $tsPkg) | Select-Object -First 1
    $tsCases = @(Get-OrchTestCase -Path $root -Recurse) | Select-Object -First 3
    if ($tsRel -and $tsCases) {
        $tsPackages = , ([UiPath.PowerShell.Entities.TestSetPackage]@{
                PackageIdentifier = $tsPkg; VersionMask = $tsRel.ProcessVersion
            })
        $tsTc = $tsCases | ForEach-Object {
            [UiPath.PowerShell.Entities.TestCase]@{
                DefinitionId = $_.Id; Enabled = $true
                VersionNumber = $tsRel.ProcessVersion; ReleaseId = $tsRel.Id
            }
        }
        New-OrchTestSet -Path $root -Name FixtureTestSet `
            -Description 'Copy round-trip fixture test set' `
            -Packages $tsPackages -TestCases $tsTc -ErrorAction SilentlyContinue | Out-Null
    }
}
else {
    Write-Host "  (skipped: no $tsPkg test package in $FixturePath\Packages)" -ForegroundColor DarkGray
}

# 12c. Action catalogs (TaskCatalog wire entity).
Write-Host "[11c/17] Action catalogs"
Import-Csv "$FixturePath\task_catalogs.csv" | Remap-Path | New-OrchActionCatalog | Out-Null

# 13. Asset links (depend on assets; share existing assets into additional folders).
# The CSV's Link column is also a path that needs the source-prefix remap.
Write-Host "[12/17] Asset links"
Import-Csv "$FixturePath\asset_links.csv" |
    ForEach-Object {
        $_.Path = $_.Path -replace '^Orch1:', "${TargetDrive}:"
        $_.Link = $_.Link -replace '^Orch1:', "${TargetDrive}:"
        $_
    } |
    Add-OrchAssetLink -Confirm:$false | Out-Null

# 14. Bucket links (depend on buckets; share existing buckets into additional folders).
Write-Host "[13/17] Bucket links"
Import-Csv "$FixturePath\bucket_links.csv" |
    ForEach-Object {
        $_.Path = $_.Path -replace '^Orch1:', "${TargetDrive}:"
        $_.Link = $_.Link -replace '^Orch1:', "${TargetDrive}:"
        $_
    } |
    Add-OrchBucketLink -Confirm:$false | Out-Null

# 15. Queue links (depend on queues; share existing queues into additional folders).
Write-Host "[14/17] Queue links"
Import-Csv "$FixturePath\queue_links.csv" |
    ForEach-Object {
        $_.Path = $_.Path -replace '^Orch1:', "${TargetDrive}:"
        $_.Link = $_.Link -replace '^Orch1:', "${TargetDrive}:"
        $_
    } |
    Add-OrchQueueLink -Confirm:$false | Out-Null

Write-Host "Done." -ForegroundColor Green
