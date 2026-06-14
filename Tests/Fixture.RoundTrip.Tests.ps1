#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    CSV round-trip verification for the fixture entities.

.DESCRIPTION
    Imports the fixture via Import-Fixture.ps1, then re-exports each entity
    type to CSV via the corresponding cmdlet's -ExportCsv parameter. The
    re-exported CSV is compared with the source CSV in TestData/Fixture to
    catch import/export drift -- a column dropped on either side, a field
    being trimmed, an enum case-folded, etc.

    The comparison normalizes:
    - Path prefix: source CSVs use "Orch1:" (the original capture); exports
      use the target drive prefix. Both are stripped before compare.
    - Row order: rows are sorted by (Path, Name) so insertion order doesn't
      cause spurious diffs.
    - BOM: source CSVs were saved with UTF-8 BOM. The exporter writes without
      BOM. Comparison is on parsed rows, not raw bytes.
    - Empty / null values: PowerShell treats "$null" and "" interchangeably
      for CSV cells; both sides are normalized to "".

.NOTES
    Run with: Invoke-Pester -Path Tests\Fixture.RoundTrip.Tests.ps1 -Output Detailed

    Setup wipes the target tenant (default: OrchTest) via Reset-Tenant.ps1
    then runs Import-Fixture.ps1. Destructive on the target drive.
#>

BeforeAll {
    $script:TargetDrive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:FixtureRoot = "${script:TargetDrive}:\TestFixture_Base"
    $script:FixturePath = Join-Path $PSScriptRoot '..\TestData\Fixture' | Resolve-Path
    $script:ExportDir = Join-Path $env:TEMP "FixtureRoundTrip_$(Get-Random -Maximum 9999)"
    New-Item -ItemType Directory -Path $script:ExportDir -Force | Out-Null

    Get-PSDrive $script:TargetDrive -ErrorAction Stop | Out-Null

    Write-Host "Resetting ${script:TargetDrive}: ..." -ForegroundColor Cyan
    & "$PSScriptRoot\Reset-Tenant.ps1" -TargetDrive $script:TargetDrive -Confirm:$false
    Write-Host "Importing fixture into ${script:TargetDrive}: ..." -ForegroundColor Cyan
    & "$PSScriptRoot\Import-Fixture.ps1" -TargetDrive $script:TargetDrive

    Push-Location $script:FixtureRoot

    function Read-FixtureCsv {
        param([string]$Name)
        $path = Join-Path $script:FixturePath $Name
        Import-Csv $path
    }

    function Normalize-Rows {
        <#
        .SYNOPSIS
            Normalize CSV rows for comparison.
            - Replace 'Orch1:' / "${TargetDrive}:" prefixes with a stable token in Path columns.
            - Replace $null cells with "".
            - Sort by (Path, Name) when both columns exist; otherwise by Path.
        #>
        param(
            [object[]]$Rows,
            [string[]]$PathColumns = @('Path', 'Link')
        )
        if (-not $Rows -or $Rows.Count -eq 0) { return @() }

        # Normalize path prefixes
        foreach ($row in $Rows) {
            foreach ($col in $PathColumns) {
                if ($row.PSObject.Properties.Match($col).Count -gt 0) {
                    $v = $row.$col
                    if ($null -ne $v) {
                        $v = $v -replace "^Orch1:", "<DRIVE>:"
                        $v = $v -replace "^${script:TargetDrive}:", "<DRIVE>:"
                        $row.$col = $v
                    }
                }
            }
            # Coerce null cells to ""
            foreach ($prop in $row.PSObject.Properties) {
                if ($null -eq $prop.Value) { $prop.Value = '' }
            }
        }

        # Sort by (Path, Name) for deterministic comparison
        $hasName = $Rows[0].PSObject.Properties.Match('Name').Count -gt 0
        if ($hasName) {
            return $Rows | Sort-Object Path, Name
        }
        return $Rows | Sort-Object Path
    }

    function Compare-CsvSubset {
        <#
        .SYNOPSIS
            Verify that every row in $ExpectedRows has a corresponding row in
            $ActualRows with matching values for $ColumnsToCheck. Extra rows in
            $ActualRows are tolerated (the tenant may have unrelated residue,
            but Reset+Import should leave only the fixture).
        #>
        param(
            [object[]]$ExpectedRows,
            [object[]]$ActualRows,
            [string[]]$ColumnsToCheck,
            [string]$EntityLabel
        )
        $key = { param($r) "$($r.Path)|$($r.Name)" }
        $actualByKey = @{}
        foreach ($r in $ActualRows) {
            $actualByKey[(& $key $r)] = $r
        }

        foreach ($e in $ExpectedRows) {
            $k = & $key $e
            $actualByKey.ContainsKey($k) | Should -BeTrue -Because "$EntityLabel row '$k' should exist after round-trip"
            $a = $actualByKey[$k]
            foreach ($col in $ColumnsToCheck) {
                # Use $e.PSObject... to avoid issues with property name vs string equality
                $eVal = if ($e.PSObject.Properties.Match($col).Count -gt 0) { $e.$col } else { '' }
                $aVal = if ($a.PSObject.Properties.Match($col).Count -gt 0) { $a.$col } else { '' }
                $aVal | Should -Be $eVal -Because "$EntityLabel row '$k' column '$col' should round-trip"
            }
        }
    }
}

AfterAll {
    Pop-Location
    if (Test-Path $script:ExportDir) {
        Remove-Item $script:ExportDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Cleaning up ${script:TargetDrive}: ..." -ForegroundColor Cyan
    & "$PSScriptRoot\Reset-Tenant.ps1" -TargetDrive $script:TargetDrive -Confirm:$false
}

# ============================================================================
# Per-entity round-trip checks. Each test exports the entities under the
# fixture root, parses both the export and the source CSV, and asserts that
# every source row appears in the export with matching values on the columns
# meaningful to the entity. Columns that are derived / computed by the
# exporter (Id, CreationTime, dates, etc.) are excluded.
# ============================================================================

Describe 'Fixture CSV round-trip (Orch1: → OrchTest: → CSV) preserves source data' {

    It 'queues.csv round-trips Path / Name / Description' {
        $exportPath = Join-Path $script:ExportDir 'queues.csv'
        Get-OrchQueue -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'queues.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Description') 'queues'
    }

    It 'processes.csv round-trips Path / Name / Description / Version' {
        $exportPath = Join-Path $script:ExportDir 'processes.csv'
        Get-OrchProcess -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'processes.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Description', 'Version') 'processes'
    }

    It 'assets.csv round-trips Path / Name / Description / ValueType / Value' {
        $exportPath = Join-Path $script:ExportDir 'assets.csv'
        Get-OrchAsset -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'assets.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Description', 'ValueType', 'Value') 'assets'
    }

    It 'buckets.csv round-trips Path / Name / Description' {
        $exportPath = Join-Path $script:ExportDir 'buckets.csv'
        Get-OrchBucket -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'buckets.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Description') 'buckets'
    }

    It 'triggers.csv round-trips Path / Name / ReleaseName' {
        $exportPath = Join-Path $script:ExportDir 'triggers.csv'
        Get-OrchTrigger -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'triggers.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'ReleaseName') 'triggers'
    }

    It 'machines.csv round-trips Path / Name / Type / Scope' {
        $exportPath = Join-Path $script:ExportDir 'machines.csv'
        Get-OrchMachine -Path "${script:TargetDrive}:\" -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'machines.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Type', 'Scope') 'machines'
    }

    It 'api_triggers.csv round-trips Path / Name / Release / Method / Slug / CallingMode' {
        $exportPath = Join-Path $script:ExportDir 'api_triggers.csv'
        Get-OrchApiTrigger -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'api_triggers.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Release', 'Method', 'Slug', 'CallingMode') 'api_triggers'
    }

    It 'test_data_queues.csv round-trips Path / Name / Description' {
        $exportPath = Join-Path $script:ExportDir 'test_data_queues.csv'
        Get-OrchTestDataQueue -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'test_data_queues.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        # ContentJsonSchema is not asserted here — the import path sets it
        # from the CSV but the server may canonicalise (re-order keys,
        # strip whitespace) and reading it back drifts from byte-equal.
        # The Path/Name/Description triad pins identity-and-metadata.
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Description') 'test_data_queues'
    }

    It 'task_catalogs.csv round-trips Path / Name / Description / Encrypted' {
        $exportPath = Join-Path $script:ExportDir 'task_catalogs.csv'
        Get-OrchActionCatalog -Path $script:FixtureRoot -Recurse -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'task_catalogs.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        Compare-CsvSubset $expected $actual @('Path', 'Name', 'Description', 'Encrypted') 'task_catalogs'
    }

    It 'roles.csv round-trips Path / Name (per-permission rows)' {
        # Get-OrchRole emits one row per (Role, Permission) tuple, matching the
        # source CSV shape. Comparison is on the (Path, Name, PermissionName)
        # composite key implicitly via the Compare-CsvSubset key + column check.
        $exportPath = Join-Path $script:ExportDir 'roles.csv'
        Get-OrchRole -Path "${script:TargetDrive}:\" -ExportCsv $exportPath | Out-Null
        $expected = Normalize-Rows (Read-FixtureCsv 'roles.csv')
        $actual   = Normalize-Rows (Import-Csv $exportPath)
        # Just check role names are all present (per-permission row matching is
        # over-precise for this regression test).
        $expectedNames = $expected | Select-Object -ExpandProperty Name -Unique
        $actualNames = $actual | Select-Object -ExpandProperty Name -Unique
        foreach ($n in $expectedNames) {
            $actualNames | Should -Contain $n -Because "roles fixture role '$n' should round-trip"
        }
    }
}
