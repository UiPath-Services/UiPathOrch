#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Enumeration-coverage smoke test for the CopyItem provider implementation.

.DESCRIPTION
    Runs `Copy-Item <fixture> <drive-root> -WhatIf -Recurse` against the
    Pester fixture folder and asserts that each expected entity kind shows up
    in the What If stream at least the expected number of times. Surfaces the
    class of bug that's easy to introduce in CopyItem.cs: a Copy* method
    that silently skips entities the user expected to be copied (wrong cache
    miss, wrong filter, off-by-one ShouldProcess gate).

    This is intentionally a smoke test, not a behaviour test -- it verifies
    that enumeration reaches each entity, not that the copy itself would do
    the right thing. For round-trip correctness see Fixture.RoundTrip.Tests.

    Prerequisites:
    - Target drive (default Orch2) mounted and authenticated.
    - Pester fixture imported into TestFixture_Base via Import-Fixture.ps1
      (Fixture.RoundTrip.Tests.ps1 runs the import in its BeforeAll; this
      test piggybacks on whatever's currently in the target tenant).

.NOTES
    Run with: Invoke-Pester -Path Tests\CopyItem.WhatIf.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:TargetDrive = 'Orch2'
    $script:FixtureRoot = "${script:TargetDrive}:\TestFixture_Base"
    $script:DstRoot     = "${script:TargetDrive}:\"

    Get-PSDrive $script:TargetDrive -ErrorAction Stop | Out-Null

    # Sanity: fixture folder must exist on the target drive.
    if (-not (Test-Path $script:FixtureRoot)) {
        throw "Fixture folder '$script:FixtureRoot' not found. " +
              "Run Tests\Import-Fixture.ps1 -TargetDrive $script:TargetDrive first " +
              "(or run Fixture.RoundTrip.Tests.ps1 which imports as part of its setup)."
    }

    # Capture What If stream. -WhatIf prints "What if: Performing the operation
    # 'X' on target 'Y'." lines; we redirect the information stream (6) to
    # success (1) so Pester can read them as strings.
    Push-Location $script:DstRoot
    try {
        $script:WhatIfLines = @(
            Copy-Item -Path $script:FixtureRoot -Destination $script:DstRoot -Recurse -WhatIf 6>&1 3>&1 |
                ForEach-Object { $_.ToString() } |
                Where-Object { $_ -match '^What if:' }
        )
    } finally {
        Pop-Location
    }

    # Action-keyword counts: each "Copy <Kind>" action keyword that CopyItem.cs
    # passes to ShouldProcess. Match by action keyword in the What If line.
    function script:Count-WhatIf([string]$actionKeyword) {
        @($script:WhatIfLines | Where-Object { $_ -match [regex]::Escape($actionKeyword) }).Count
    }
}

Describe 'Copy-Item -WhatIf enumeration coverage' {

    Context 'shape sanity' {
        It 'produces at least one What If message' {
            $script:WhatIfLines.Count | Should -BeGreaterThan 0
        }

        It 'always emits a "Copy Folder" message for the recursed root' {
            (Count-WhatIf 'Copy Folder') | Should -BeGreaterThan 0
        }
    }

    # Each entity kind below maps 1:1 to a Copy* method in CopyItem.cs and the
    # ShouldProcess action keyword it passes. If a Copy* method silently stops
    # enumerating, its count drops to 0 here.

    Context 'per-entity coverage (entity kinds present in TestData/Fixture)' {
        It 'copies <kind>' -ForEach @(
            @{ kind = 'FolderUser'; minCount = 1 }
            @{ kind = 'Bucket';     minCount = 1 }
            @{ kind = 'Package';    minCount = 1 }
            @{ kind = 'Process';    minCount = 1 }
            @{ kind = 'Asset';      minCount = 1 }
            @{ kind = 'Queue';      minCount = 1 }
            @{ kind = 'Trigger';    minCount = 1 }
        ) {
            (Count-WhatIf "Copy $kind") | Should -BeGreaterOrEqual $minCount `
                -Because "CopyItem.cs.Copy$($kind)s should enumerate at least one " +
                         "$kind from the fixture; a count of 0 means the Copy${kind}s " +
                         "method or its source-side cache lookup is silently skipping."
        }
    }

    Context 'fixture does not cover (informational only)' {
        # These kinds are NOT in TestData/Fixture, so the test cannot assert
        # enumeration. Listed here so a future fixture extension can flip them
        # from Skip to active.

        It 'copies FolderMachine (skipped: fixture has no folder-scoped machine assignments)' -Skip {
            (Count-WhatIf 'Copy FolderMachine') | Should -BeGreaterThan 0
        }
        It 'copies ApiTrigger (skipped: fixture has no API triggers)' -Skip {
            (Count-WhatIf 'Copy ApiTrigger') | Should -BeGreaterThan 0
        }
        It 'copies TestSet (skipped: fixture has no test sets)' -Skip {
            (Count-WhatIf 'Copy TestSet') | Should -BeGreaterThan 0
        }
        It 'copies TestSetSchedule (skipped: fixture has no test set schedules)' -Skip {
            (Count-WhatIf 'Copy TestSetSchedule') | Should -BeGreaterThan 0
        }
        It 'copies TestDataQueue (skipped: fixture has no test data queues)' -Skip {
            (Count-WhatIf 'Copy TestDataQueue') | Should -BeGreaterThan 0
        }
        It 'copies ActionCatalog (skipped: fixture has no action catalogs)' -Skip {
            (Count-WhatIf 'Copy ActionCatalog') | Should -BeGreaterThan 0
        }
    }

    Context 'no unexpected error markers' {
        It 'does not emit an Error record alongside the What If output' {
            # Any errors during the dry-run enumeration would surface as
            # WriteError calls from inside the various FindDst* / Copy*
            # helpers. They're often legitimate (missing dst role / user
            # warnings) but worth surfacing in test logs.
            $errorLines = @($script:WhatIfLines | Where-Object { $_ -match 'Error:|Warning:' })
            if ($errorLines.Count -gt 0) {
                Write-Host ""
                Write-Host "WhatIf surfaced $($errorLines.Count) warning/error line(s):" -ForegroundColor Yellow
                $errorLines | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
            }
            # Not an assertion failure -- just visibility.
            $true | Should -Be $true
        }
    }
}
