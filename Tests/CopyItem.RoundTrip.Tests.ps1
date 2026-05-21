#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copy-Item round-trip verification: copy the fixture folder and verify
    every per-folder entity reaches the destination.

.DESCRIPTION
    Sister test to Fixture.RoundTrip.Tests.ps1. Where that test verifies
    Import-Fixture -> CSV export -> source CSV equivalence (the IMPORT
    path), this test verifies:

        TestFixture_Base (source)
              |
              v   Copy-Item -Recurse
              v
        TestFixture_RoundTrip_<pid> (destination)

    by exporting both src and dst entity sets via the corresponding Get-Orch*
    cmdlets and asserting that every src row appears in dst (with path-prefix
    stripped to align the two folder trees).

    This is the activity that catches the "Copy* method silently drops some
    entities" class of bug -- the kind of regression that wouldn't trip
    any existing unit test or the CSV roundtrip suite, but a real Copy-Item
    user would notice when their copied folder is incomplete.

    Same-tenant copy is intentional: cross-tenant exercises additional
    code paths (FindDstRole / FindDstUser / FindDstMachine name resolution)
    that the existing CopyFolder name-collision test partially covers.
    Same-tenant isolates the "is enumeration + creation correct?" question.

    Field-equivalence granularity is intentionally loose: assert each
    expected row's identity columns (Path / Name + a small set of meaningful
    fields per kind) match. Server-assigned columns (Id, timestamps) are
    excluded because they legitimately differ after copy.

.NOTES
    Run with: Invoke-Pester -Path Tests\CopyItem.RoundTrip.Tests.ps1 -Output Detailed

    Prereqs:
    - Target drive (default Orch2) mounted and authenticated.
    - Fixture imported into TestFixture_Base via Import-Fixture.ps1
      (e.g. run Fixture.RoundTrip.Tests.ps1 once first; this test does
      NOT call Reset-Tenant on its own to avoid surprise destruction).

    Creates a temp folder TestFixture_RoundTrip_<pid> on the target drive
    and removes it in AfterAll. If the test crashes leaving the folder
    behind, clean it manually with Remove-Item -Recurse.
#>

BeforeAll {
    $script:TargetDrive  = 'Orch2'
    $script:SrcRoot      = "${script:TargetDrive}:\TestFixture_Base"
    $script:DstFolderName = "TestFixture_RoundTrip_$PID"
    $script:DstRoot      = "${script:TargetDrive}:\$($script:DstFolderName)"
    # Copy-Item creates a child folder under dst named after src. Effective
    # path of the copy is dst/<srcRootName>.
    $script:DstCopyRoot  = "$($script:DstRoot)\TestFixture_Base"
    $script:ExportDir    = Join-Path $env:TEMP "CopyItemRoundTrip_$(Get-Random -Maximum 9999)"
    New-Item -ItemType Directory -Path $script:ExportDir -Force | Out-Null

    Get-PSDrive $script:TargetDrive -ErrorAction Stop | Out-Null

    if (-not (Test-Path $script:SrcRoot)) {
        throw "Fixture folder '$script:SrcRoot' not found. " +
              "Run Fixture.RoundTrip.Tests.ps1 first (it imports the fixture in its BeforeAll) " +
              "or run Tests\Import-Fixture.ps1 -TargetDrive $script:TargetDrive manually."
    }

    Write-Host "Creating destination folder '$script:DstRoot' ..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $script:DstRoot -Force -ErrorAction Stop | Out-Null

    Write-Host "Copy-Item -Recurse from '$script:SrcRoot' to '$script:DstRoot' ..." -ForegroundColor Cyan
    Copy-Item -Path $script:SrcRoot -Destination $script:DstRoot -Recurse -ErrorAction Stop

    Push-Location $script:DstRoot

    function script:Normalize-Rows {
        <#
        .SYNOPSIS
            Strip the per-folder-tree prefix so src rows and dst rows compare.
            The drive prefix is the same on both sides ($TargetDrive:), but the
            root folder name differs: TestFixture_Base on src, the dst-copy
            root path on dst.
        #>
        param(
            [object[]]$Rows,
            [string]$RootPrefix,        # e.g. "Orch2:\TestFixture_Base"
            [string[]]$PathColumns = @('Path', 'Link')
        )
        if (-not $Rows -or $Rows.Count -eq 0) { return @() }

        $escaped = [regex]::Escape($RootPrefix)
        foreach ($row in $Rows) {
            foreach ($col in $PathColumns) {
                if ($row.PSObject.Properties.Match($col).Count -gt 0) {
                    $v = $row.$col
                    if ($null -ne $v) {
                        # "<root>\Foo" -> "<ROOT>\Foo"; bare "<root>" -> "<ROOT>"
                        $row.$col = ($v -replace "^$escaped", '<ROOT>')
                    }
                }
            }
            foreach ($prop in $row.PSObject.Properties) {
                if ($null -eq $prop.Value) { $prop.Value = '' }
            }
        }

        $hasName = $Rows[0].PSObject.Properties.Match('Name').Count -gt 0
        if ($hasName) { return $Rows | Sort-Object Path, Name }
        return $Rows | Sort-Object Path
    }

    function script:Compare-CopySubset {
        <#
        .SYNOPSIS
            Verify every src row exists in dst (by Path|Name key) with matching
            values in the column list. Extra rows in dst are tolerated (the
            destination folder may legitimately inherit additional content
            -- though for a freshly-created folder this shouldn't happen).
        #>
        param(
            [object[]]$SrcRows,
            [object[]]$DstRows,
            [string[]]$ColumnsToCheck,
            [string]$EntityLabel
        )
        $key = { param($r) "$($r.Path)|$($r.Name)" }
        $dstByKey = @{}
        foreach ($r in $DstRows) {
            $dstByKey[(& $key $r)] = $r
        }

        foreach ($e in $SrcRows) {
            $k = & $key $e
            $dstByKey.ContainsKey($k) | Should -BeTrue `
                -Because "$EntityLabel row '$k' should exist on the copy destination after Copy-Item -Recurse"
            $a = $dstByKey[$k]
            foreach ($col in $ColumnsToCheck) {
                $eVal = if ($e.PSObject.Properties.Match($col).Count -gt 0) { $e.$col } else { '' }
                $aVal = if ($a.PSObject.Properties.Match($col).Count -gt 0) { $a.$col } else { '' }
                $aVal | Should -Be $eVal -Because "$EntityLabel row '$k' column '$col' should round-trip through Copy-Item"
            }
        }
    }

    function script:Export-AndDiff {
        <#
        .SYNOPSIS
            Run Get-Orch<Kind> on both src and dst-copy roots, normalize the
            two CSV outputs by their respective root prefixes, and assert
            every src entity is present in dst with matching identity columns.
        #>
        param(
            [scriptblock]$Getter,       # e.g. { param($path, $csv) Get-OrchAsset -Path $path -Recurse -ExportCsv $csv | Out-Null }
            [string]$Name,              # e.g. 'assets'
            [string[]]$ColumnsToCheck   # identity columns to compare
        )
        $srcCsv = Join-Path $script:ExportDir "src_$Name.csv"
        $dstCsv = Join-Path $script:ExportDir "dst_$Name.csv"

        & $Getter $script:SrcRoot $srcCsv
        & $Getter $script:DstCopyRoot $dstCsv

        $srcRows = script:Normalize-Rows (Import-Csv $srcCsv) $script:SrcRoot
        $dstRows = script:Normalize-Rows (Import-Csv $dstCsv) $script:DstCopyRoot

        script:Compare-CopySubset $srcRows $dstRows $ColumnsToCheck $Name
    }
}

AfterAll {
    Pop-Location -ErrorAction SilentlyContinue
    if (Test-Path $script:ExportDir) {
        Remove-Item $script:ExportDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $script:DstRoot) {
        Write-Host "Removing temp destination folder '$script:DstRoot' ..." -ForegroundColor Cyan
        Remove-Item $script:DstRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# ============================================================================
# Per-entity Copy-Item -Recurse round-trip checks. Each test exports a kind
# from both the source fixture and the copied destination, then asserts the
# destination contains every source row (by Path|Name identity, with
# additional content-field equivalence on the columns meaningful for that
# kind). A row that's in src but missing in dst means a Copy* method
# silently dropped it -- the bug class this whole test file exists to catch.
# ============================================================================

Describe 'Copy-Item -Recurse round-trip preserves per-folder entities' {

    It 'subfolder tree is replicated' {
        # Folders are not exported via -ExportCsv on a Get-Orch* cmdlet; use
        # FullyQualifiedName off the Folder object (already a clean relative
        # path) and strip the per-tree root prefix to align src vs dst.
        function Get-RelativeFolders($rootPSPath) {
            $rootFolder = Get-Item $rootPSPath -ErrorAction Stop
            $rootFqn = $rootFolder.FullyQualifiedName
            Get-ChildItem $rootPSPath -Recurse -Force |
                Where-Object PSIsContainer |
                ForEach-Object {
                    if ($rootFqn) {
                        $_.FullyQualifiedName -replace "^$([regex]::Escape($rootFqn))/", ''
                    } else {
                        $_.FullyQualifiedName
                    }
                } |
                Where-Object { $_ -ne '' } |
                Sort-Object
        }

        $srcFolders = @(Get-RelativeFolders $script:SrcRoot)
        $dstFolders = @(Get-RelativeFolders $script:DstCopyRoot)

        foreach ($f in $srcFolders) {
            $dstFolders | Should -Contain $f -Because "subfolder '$f' should exist after Copy-Item -Recurse"
        }
    }

    It 'assets round-trip' {
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchAsset -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'assets' `
            -ColumnsToCheck @('Description', 'ValueType')
    }

    It 'queues round-trip' {
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchQueue -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'queues' `
            -ColumnsToCheck @('Description')
    }

    It 'buckets round-trip' {
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchBucket -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'buckets' `
            -ColumnsToCheck @('Description')
    }

    It 'processes round-trip' {
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchProcess -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'processes' `
            -ColumnsToCheck @('Description', 'Version')
    }

    It 'triggers round-trip' {
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchTrigger -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'triggers' `
            -ColumnsToCheck @('ReleaseName')
    }
}
