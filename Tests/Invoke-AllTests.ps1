#Requires -Modules UiPathOrch, Pester
<#
.SYNOPSIS
    Runs the live UiPathOrch Pester suite reliably by giving every test file a
    clean, known starting state.

.DESCRIPTION
    The files under Tests\ are LIVE integration tests that share ONE mutable
    Orchestrator tenant. Each file passes in isolation, but a bare
    `Invoke-Pester -Path .` is order-dependent and tends to cascade into mass
    failure: some files wipe the tenant (Reset-Tenant) while others assume it is
    populated, so a file that runs after a wipe fails in its setup and every test
    in it is reported failed.

    This runner removes that coupling:
      * Before EACH file it resets the disposable tenant and re-imports the
        fixture, so every file starts from the same known state regardless of
        order (use -SkipReset / -SkipImport to trade safety for speed).
      * It pins the current location to the disposable tenant, so a stray
        null-path operation (e.g. a cleanup that runs after a failed setup)
        resolves to the disposable tenant and can never touch another one.
      * It silences progress bars and per-drive host warnings so the only
        thing you read is the per-file pass/fail summary.

    DRIVES
      -Tenant   DISPOSABLE tenant; WIPED and repopulated before each file.
                Default: $env:UIPATHORCH_TEST_DRIVE (required if unset).
      -RefDrive Read-only reference tenant for the two-drive tests; never written.
                Default: $env:UIPATHORCH_TEST_REF_DRIVE, else 'Orch1'.

    Known environmental limitations (failures here are NOT regressions):
      * A tenant that forbids test-set-schedule creation (e.g. some trial/grade
        tenants) will fail the schedule-creating cases. Pick a -Tenant that
        allows it, or -Exclude those files. See Tests\README.md.

.EXAMPLE
    .\Invoke-AllTests.ps1 -Tenant Orch2 -RefDrive Orch1

.EXAMPLE
    # Fast re-run of one area without re-resetting the tenant each time:
    .\Invoke-AllTests.ps1 -Tenant Orch2 -Filter 'Compare*' -SkipReset -SkipImport

.EXAMPLE
    .\Invoke-AllTests.ps1 -Tenant Orch2 -Exclude 'CopyTestDataQueueMigration.Tests.ps1'
#>
[CmdletBinding()]
param(
    [string]$Tenant   = $env:UIPATHORCH_TEST_DRIVE,
    [string]$RefDrive  = $(if ($env:UIPATHORCH_TEST_REF_DRIVE) { $env:UIPATHORCH_TEST_REF_DRIVE } else { 'Orch1' }),
    [string]$Filter    = '*',
    [string[]]$Exclude = @(),
    [switch]$SkipReset,
    [switch]$SkipImport
)

$ErrorActionPreference = 'Stop'

if (-not $Tenant) {
    throw "Specify -Tenant (or set `$env:UIPATHORCH_TEST_DRIVE). This is the DISPOSABLE test tenant; it WILL be wiped."
}

Import-Module UiPathOrch -ErrorAction Stop
Import-OrchConfig 6>$null | Out-Null
Get-PSDrive $Tenant   -ErrorAction Stop | Out-Null
Get-PSDrive $RefDrive -ErrorAction Stop | Out-Null

# Children (Invoke-Pester test blocks) read these.
$env:UIPATHORCH_TEST_DRIVE     = $Tenant
$env:UIPATHORCH_TEST_REF_DRIVE = $RefDrive

$reset  = Join-Path $PSScriptRoot 'Reset-Tenant.ps1'
$import = Join-Path $PSScriptRoot 'Import-Fixture.ps1'

$files = Get-ChildItem -Path $PSScriptRoot -Filter '*.Tests.ps1' |
    Where-Object { $_.Name -like $Filter -and $_.Name -notin $Exclude } |
    Sort-Object Name

if (-not $files) { throw "No test files matched -Filter '$Filter' (after -Exclude)." }

Write-Host ("Tenant (disposable, wiped per file): {0}:   Ref (read-only): {1}:" -f $Tenant, $RefDrive) -ForegroundColor Cyan
Write-Host ("Running {0} file(s). Reset={1} Import={2}" -f $files.Count, (-not $SkipReset), (-not $SkipImport)) -ForegroundColor Cyan
Write-Host ""

$origin = (Get-Location).Path
$progSaved = $ProgressPreference
$ProgressPreference = 'SilentlyContinue'   # kill the cmdlet progress bars
$results = [System.Collections.Generic.List[object]]::new()

try {
    foreach ($f in $files) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $errored = $null
        $r = $null
        try {
            if (-not $SkipReset)  { & $reset  -TargetDrive $Tenant -Confirm:$false *>$null }
            if (-not $SkipImport) { & $import -TargetDrive $Tenant *>$null }
            # Pin cwd to the disposable tenant: any null-path op stays contained here.
            Set-Location "${Tenant}:\"
            $r = Invoke-Pester -Path $f.FullName -Output None -PassThru 3>$null 4>$null 5>$null 6>$null
        }
        catch {
            $errored = $_.Exception.Message
        }
        $sw.Stop()

        $passed  = if ($r) { $r.PassedCount }  else { 0 }
        $failed  = if ($r) { $r.FailedCount }  else { 0 }
        $skipped = if ($r) { $r.SkippedCount } else { 0 }
        $failedNames = if ($r) { @($r.Failed | ForEach-Object { $_.ExpandedPath }) } else { @() }

        $tag = if ($errored) { 'ERROR' } elseif ($failed -eq 0) { 'PASS' } else { 'FAIL' }
        $color = switch ($tag) { 'PASS' { 'Green' } 'FAIL' { 'Red' } default { 'Magenta' } }

        $results.Add([pscustomobject]@{
            File = $f.Name; Tag = $tag
            Passed = $passed; Failed = $failed; Skipped = $skipped
            Sec = [int]$sw.Elapsed.TotalSeconds
            FailedTests = $failedNames; Error = $errored
        })

        Write-Host ("[{0,-5}] {1,-42} P={2,-3} F={3,-3} S={4,-3} {5,3}s" -f `
            $tag, $f.Name, $passed, $failed, $skipped, [int]$sw.Elapsed.TotalSeconds) -ForegroundColor $color
        if ($errored) { Write-Host "         setup/run error: $errored" -ForegroundColor Magenta }
        foreach ($ft in $failedNames) { Write-Host "         - $ft" -ForegroundColor Red }
    }
}
finally {
    Set-Location $origin
    $ProgressPreference = $progSaved
}

$filesFailed = @($results | Where-Object { $_.Tag -ne 'PASS' }).Count
$tPassed  = ($results | Measure-Object Passed  -Sum).Sum
$tFailed  = ($results | Measure-Object Failed  -Sum).Sum
$tSkipped = ($results | Measure-Object Skipped -Sum).Sum

Write-Host ""
Write-Host ("==== SUMMARY: {0} file(s), {1} not clean | tests P={2} F={3} S={4} ====" -f `
    $results.Count, $filesFailed, $tPassed, $tFailed, $tSkipped) `
    -ForegroundColor $(if ($filesFailed -eq 0) { 'Green' } else { 'Yellow' })

if ($filesFailed -gt 0) {
    Write-Host "Not clean:" -ForegroundColor Yellow
    $results | Where-Object { $_.Tag -ne 'PASS' } | ForEach-Object {
        Write-Host ("  [{0}] {1}" -f $_.Tag, $_.File) -ForegroundColor Yellow
    }
    exit 1
}
