#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Import-OrchTestDataQueueItem uploads in batches and, when a batch is rejected for
    a content-schema violation (409 Conflict), falls back to one item at a time so the
    valid rows still land and only the bad rows are reported.

.DESCRIPTION
    Creates a queue with a `required`, typed schema (n: integer, s: string), then
    imports a CSV whose last row has a non-numeric `n`. The cmdlet coerces that cell
    to a string, so the bulk add is rejected (409, errorCode 3219) and the per-item
    fallback kicks in: the two valid rows are inserted and the bad row surfaces as a
    single non-terminating error. A second case imports a fully valid CSV in one bulk
    call with no errors.

    This is the live end-to-end check for the per-item fallback shared by
    Import-OrchTestDataQueueItem and Copy-OrchTestDataQueue (see
    TestDataQueueUploadPolicy / TestDataQueueUploadPolicyTests for the unit-level
    classification).

    Requirements:
    - $env:UIPATHORCH_TEST_DRIVE (Automation Cloud drive, ApiVersion >= 18). Defaults to 'Orch2'.
    - At least one non-personal folder on that drive.

.NOTES
    Run with: Invoke-Pester -Path Tests\ImportTestDataQueueItem.Tests.ps1 -Output Detailed
    Creates/deletes a temp queue; self-cleaning.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:DriveColon = "$($script:Drive):"
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    $folder = @(
        Get-ChildItem "$($script:Drive):\" |
            Where-Object { $_.PSIsContainer -and $_.PSChildName -notlike '*workspace*' } |
            Select-Object -First 1 -ExpandProperty PSChildName)
    if ($folder.Count -lt 1) { throw "Need at least one non-personal folder on '$($script:DriveColon)'." }
    $script:Folder = "$($script:Drive):\$($folder[0])"
    $script:Qn = 'zzPesterImpTDQ'
    # Required + typed: a non-numeric `n` cell becomes a string and violates the schema.
    $script:Schema = '{"type":"object","properties":{"n":{"type":"integer"},"s":{"type":"string"}},"required":["n","s"]}'

    function Remove-TestQueue {
        Get-OrchTestDataQueue -Path $script:Folder | Where-Object Name -eq $script:Qn |
            Remove-OrchTestDataQueue -Confirm:$false -ErrorAction SilentlyContinue
    }

    # CSV with two valid rows and one whose `n` is not an integer (last row).
    $script:MixedCsv = Join-Path $TestDrive 'mixed.csv'
    "n,s`r`n1,alpha`r`n2,bravo`r`nabc,charlie" | Set-Content -Path $script:MixedCsv -Encoding utf8
    # CSV with only valid rows.
    $script:GoodCsv = Join-Path $TestDrive 'good.csv'
    "n,s`r`n10,ten`r`n20,twenty" | Set-Content -Path $script:GoodCsv -Encoding utf8

    function Get-ItemNs {
        # The `n` value of every item currently in the queue, sorted.
        @(Get-OrchTestDataQueueItem -Path $script:Folder -Name $script:Qn |
            ForEach-Object { ($_.ContentJson | ConvertFrom-Json).n }) | Sort-Object
    }
}

Describe 'Import-OrchTestDataQueueItem (batch upload with per-item fallback)' {
    BeforeEach {
        Remove-TestQueue
        New-OrchTestDataQueue -Path $script:Folder -Name $script:Qn -ContentJsonSchema $script:Schema | Out-Null
        Clear-OrchCache $script:DriveColon | Out-Null
    }

    It 'imports the valid rows and reports the schema-violating row' {
        Import-OrchTestDataQueueItem -Name $script:Qn -ImportCsv $script:MixedCsv -Path $script:Folder `
            -Confirm:$false -ErrorVariable impErr -ErrorAction SilentlyContinue
        Clear-OrchCache $script:DriveColon | Out-Null

        # The two valid rows landed despite the batch being rejected -> per-item fallback.
        Get-ItemNs | Should -Be @(1, 2)
        # The bad row was reported as a single non-terminating error, not swallowed.
        @($impErr).Count | Should -BeGreaterOrEqual 1
        "$impErr" | Should -Match 'rejected'
    }

    It 'imports a fully valid CSV with no errors' {
        Import-OrchTestDataQueueItem -Name $script:Qn -ImportCsv $script:GoodCsv -Path $script:Folder `
            -Confirm:$false -ErrorVariable impErr -ErrorAction SilentlyContinue
        Clear-OrchCache $script:DriveColon | Out-Null

        Get-ItemNs | Should -Be @(10, 20)
        @($impErr).Count | Should -Be 0
    }
}

AfterAll {
    Get-OrchTestDataQueue -Path $script:Folder | Where-Object Name -eq $script:Qn |
        Remove-OrchTestDataQueue -Confirm:$false -ErrorAction SilentlyContinue
}
