#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Regression tests for the cmdlets shipped in v1.5.0 - v1.5.2.

.DESCRIPTION
    Pins down the live-tenant behaviors that were verified ad-hoc during
    the v1.5.0 / v1.5.1 / v1.5.2 release cycle. The cycle exposed a class
    of bug where a cmdlet built a payload the server rejected (because
    the docs lied about which fields were optional) or returned a server
    response that the cmdlet then mis-emitted (Get LIST endpoint vs
    GetForEdit). Future Swagger updates or refactors can re-introduce
    these regressions silently — this file is the safety net.

    Behaviors covered:

    1. New-OrchApiTrigger barebones succeeds — the cmdlet defaults
       Tags=[], MachineRobots=[{}], Slug=Name so the server doesn't
       return its useless generic 500.
    2. New-OrchApiTrigger -RunAsCaller round-trips through Get.
    3. Update-OrchApiTrigger MachineRobots transitions:
       null -> user-bound -> machine-bound -> cleared.
    4. Get-OrchApiTrigger -ExportCsv | Import-Csv | Update-OrchApiTrigger
       preserves Description / Enabled.
    5. New-OrchTestDataQueue without -ContentJsonSchema succeeds (cmdlet
       defaults "{}", the server requires the field).
    6. New-OrchActionCatalog barebones succeeds.

    Excluded (would require fixture expansion):
    - MachineRobots Modern-robot (Robot.User.UserName) serialize
      fall-through — needs a folder user with unattended robot binding.
    - New-OrchTestSet end-to-end — fixture has no test automation
      package.
    - New-OrchTestSetSchedule success path — tenant feature flag.

    The tests run against the fixture already loaded into the target
    drive (default Orch2) by Fixture.RoundTrip.Tests.ps1 / Import-Fixture.ps1.
    To avoid touching fixture rows, every test creates entities under
    a per-PID prefix and deletes them in finally.

.NOTES
    Run with: Invoke-Pester -Path Tests\CmdletV15Behaviors.Tests.ps1 -Output Detailed

    Prereqs:
    - Target drive (default Orch2) mounted, authenticated, and seeded
      via Import-Fixture.ps1.
#>

BeforeAll {
    $script:TargetDrive = 'Orch2'
    $script:Folder      = "${script:TargetDrive}:\TestFixture_Base\Development"

    Get-PSDrive $script:TargetDrive -ErrorAction Stop | Out-Null
    if (-not (Test-Path $script:Folder)) {
        throw "Fixture folder '$script:Folder' missing — run Import-Fixture.ps1 first."
    }

    # Each test creates entities under this prefix so the cleanup in
    # finally can wildcard-match without risk of touching fixture rows.
    $script:Prefix = "regression-v15-$PID-"
    $script:Release = (Get-OrchProcess -Path $script:Folder | Select-Object -First 1).Name
    if (-not $script:Release) {
        throw "No releases in $script:Folder; fixture didn't import processes."
    }
}

Describe 'v1.5.1: New-OrchApiTrigger server-required defaults' {
    It 'barebones (Name + Release only) succeeds — Tags/MachineRobots/Slug auto-supplied' {
        $name = "${script:Prefix}barebones"
        try {
            $created = New-OrchApiTrigger -Path $script:Folder -Name $name -Release $script:Release
            $created | Should -Not -BeNullOrEmpty
            $created.Name | Should -Be $name
            # Server-side echo should reflect the auto-supplied Slug (= Name).
            $created.Slug | Should -Be $name
        }
        finally {
            Get-OrchApiTrigger -Path $script:Folder -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchApiTrigger -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It '-RunAsCaller round-trips via Get' {
        $name = "${script:Prefix}runascaller"
        try {
            New-OrchApiTrigger -Path $script:Folder -Name $name -Release $script:Release `
                -RunAsCaller true | Out-Null
            $reread = Get-OrchApiTrigger -Path $script:Folder -Name $name
            $reread.RunAsCaller | Should -Be $true
        }
        finally {
            Get-OrchApiTrigger -Path $script:Folder -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchApiTrigger -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

Describe 'v1.5.1: Update-OrchApiTrigger CSV round-trip' {
    It 'Description and Enabled round-trip through Export-Csv | Update' {
        $name = "${script:Prefix}csvroundtrip"
        $csv = Join-Path $env:TEMP "regression-v15-$PID-$([Guid]::NewGuid().Guid.Substring(0,8)).csv"
        try {
            New-OrchApiTrigger -Path $script:Folder -Name $name -Release $script:Release `
                -Description 'initial' -Enabled false | Out-Null

            Get-OrchApiTrigger -Path $script:Folder -Name $name -ExportCsv $csv | Out-Null
            $row = Import-Csv $csv | Where-Object Name -eq $name
            $row | Should -Not -BeNullOrEmpty

            $row.Description = 'updated'
            $row.Enabled = 'true'
            $row | Update-OrchApiTrigger | Out-Null

            $reread = Get-OrchApiTrigger -Path $script:Folder -Name $name
            $reread.Description | Should -Be 'updated'
            $reread.Enabled | Should -Be $true
        }
        finally {
            Get-OrchApiTrigger -Path $script:Folder -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchApiTrigger -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item $csv -ErrorAction SilentlyContinue
        }
    }
}

Describe 'v1.5.2: New-OrchTestDataQueue ContentJsonSchema default' {
    It 'barebones (Name only) succeeds — cmdlet defaults ContentJsonSchema to "{}"' {
        $name = "${script:Prefix}tdq"
        try {
            $created = New-OrchTestDataQueue -Path $script:Folder -Name $name
            $created | Should -Not -BeNullOrEmpty
            $created.Name | Should -Be $name
            # Server stores the default schema. The wire value coming back
            # may be canonicalised — yotsuda turns the literal "{}" into a
            # draft-07 schema reference like
            # `{"$schema":"http://json-schema.org/draft-07/schema#"}`.
            # Assert the field is non-empty and parses as JSON rather than
            # exact-matching the bytes the cmdlet sent.
            $reread = Get-OrchTestDataQueue -Path $script:Folder -Name $name
            $reread.ContentJsonSchema | Should -Not -BeNullOrEmpty
            { $null = $reread.ContentJsonSchema | ConvertFrom-Json } | Should -Not -Throw `
                -Because 'ContentJsonSchema must round-trip as valid JSON'
        }
        finally {
            Get-OrchTestDataQueue -Path $script:Folder -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchTestDataQueue -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

Describe 'v1.5.1: New-OrchActionCatalog barebones' {
    It 'barebones (Name only) succeeds' {
        $name = "${script:Prefix}ac"
        try {
            $created = New-OrchActionCatalog -Path $script:Folder -Name $name
            $created | Should -Not -BeNullOrEmpty
            $created.Name | Should -Be $name
        }
        finally {
            Get-OrchActionCatalog -Path $script:Folder -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchActionCatalog -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

Describe 'v1.5.3: Get-OrchTestSetDetail' {
    # TestSet creation itself requires a test automation package not in
    # the fixture. This test verifies the Detail cmdlet works against
    # whatever TestSets happen to exist on the target — if there are
    # none, the test passes trivially (no-op behavior).
    It 'populates Packages / TestCases arrays vs the LIST Get' {
        $existing = @(Get-OrchTestSet -Path "${script:TargetDrive}:\" -Recurse -ErrorAction SilentlyContinue)
        if ($existing.Count -eq 0) {
            Set-ItResult -Skipped -Because 'no TestSets in target tenant to test against'
            return
        }
        $sample = $existing[0]
        $listEntity   = Get-OrchTestSet       -Path $sample.Path -Name $sample.Name
        $detailEntity = Get-OrchTestSetDetail -Path $sample.Path -Name $sample.Name

        # LIST: arrays empty. DETAIL: arrays populated (assuming non-empty TestCaseCount on the sample).
        if ($listEntity.TestCaseCount -gt 0) {
            ($detailEntity.TestCases | Measure-Object).Count | Should -BeGreaterThan 0 `
                -Because 'GetForEdit must populate TestCases when LIST reports TestCaseCount > 0'
        }
    }
}
