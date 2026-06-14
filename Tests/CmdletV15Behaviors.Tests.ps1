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
    $script:TargetDrive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
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

Describe 'v1.5.3: TestDataQueue / ActionCatalog full CSV round-trip' {
    # The contiguous round-trip the manual procedure documented, now
    # automated: create a source entity, Get -ExportCsv, Import-Csv,
    # New-Orch* under a fresh name, and assert the copy matches the
    # source on the columns the CSV carries. Live-verified on Orch1
    # (yotsuda) 2026-05-22. The individual legs are covered elsewhere;
    # this pins the whole pipeline end to end.
    It 'New-OrchTestDataQueue: Get -ExportCsv | Import-Csv | New- preserves Description + schema' {
        $src = "${script:Prefix}tdq-rt-src"
        $dst = "${script:Prefix}tdq-rt-dst"
        $csv = Join-Path $env:TEMP "${script:Prefix}tdq-rt.csv"
        try {
            New-OrchTestDataQueue -Path $script:Folder -Name $src -Description 'rt source' `
                -ContentJsonSchema '{"type":"object","properties":{"OrderId":{"type":"string"}}}' -ErrorAction Stop | Out-Null
            Get-OrchTestDataQueue -Path $script:Folder -Name $src -ExportCsv $csv | Out-Null
            $row = Import-Csv $csv | Where-Object Name -eq $src
            $row | Should -Not -BeNullOrEmpty
            $row.Name = $dst
            $row | New-OrchTestDataQueue -ErrorAction Stop | Out-Null

            $srcE = Get-OrchTestDataQueue -Path $script:Folder -Name $src
            $dstE = Get-OrchTestDataQueue -Path $script:Folder -Name $dst
            $dstE.Description | Should -Be $srcE.Description
            { $null = $dstE.ContentJsonSchema | ConvertFrom-Json } | Should -Not -Throw
        }
        finally {
            foreach ($n in $src, $dst) {
                Get-OrchTestDataQueue -Path $script:Folder -Name $n -ErrorAction SilentlyContinue |
                    Remove-OrchTestDataQueue -Confirm:$false -ErrorAction SilentlyContinue
            }
            Remove-Item $csv -ErrorAction SilentlyContinue
        }
    }

    It 'New-OrchActionCatalog: Get -ExportCsv | Import-Csv | New- preserves Description + Encrypted' {
        $src = "${script:Prefix}ac-rt-src"
        $dst = "${script:Prefix}ac-rt-dst"
        $csv = Join-Path $env:TEMP "${script:Prefix}ac-rt.csv"
        try {
            New-OrchActionCatalog -Path $script:Folder -Name $src -Description 'rt source' -Encrypted true -ErrorAction Stop | Out-Null
            Get-OrchActionCatalog -Path $script:Folder -Name $src -ExportCsv $csv | Out-Null
            $row = Import-Csv $csv | Where-Object Name -eq $src
            $row | Should -Not -BeNullOrEmpty
            $row.Name = $dst
            $row | New-OrchActionCatalog -ErrorAction Stop | Out-Null

            $srcE = Get-OrchActionCatalog -Path $script:Folder -Name $src
            $dstE = Get-OrchActionCatalog -Path $script:Folder -Name $dst
            $dstE.Description | Should -Be $srcE.Description
            $dstE.Encrypted   | Should -Be $srcE.Encrypted
        }
        finally {
            foreach ($n in $src, $dst) {
                Get-OrchActionCatalog -Path $script:Folder -Name $n -ErrorAction SilentlyContinue |
                    Remove-OrchActionCatalog -Confirm:$false -ErrorAction SilentlyContinue
            }
            Remove-Item $csv -ErrorAction SilentlyContinue
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

Describe 'v1.5.3: New-OrchWebhook + Get-OrchWebhook -ExportCsv' {
    # Webhooks are drive-scoped (no folder context). Create a uniquely-
    # named webhook against the test drive, exercise Get | Export-Csv |
    # Import-Csv | Update- round-trip, then clean up.
    It 'barebones (Name + Url) creates a webhook with SubscribeToAllEvents default true' {
        $name = "${script:Prefix}webhook"
        try {
            # -Enabled false: the URL is fake, so an *enabled* subscribe-to-all
            # webhook makes Orchestrator fire on every tenant event, fail
            # delivery, auto-disable the hook, and email the tenant admin.
            # Disabled means no delivery attempts. SubscribeToAllEvents is still
            # left to default (true), which the assertion below verifies.
            $created = New-OrchWebhook -Path "${script:TargetDrive}:" `
                -Name $name -Url 'https://example.invalid/webhook' -Enabled false `
                -ErrorAction Stop
            $created | Should -Not -BeNullOrEmpty
            $created.Name | Should -Be $name
            $created.Url  | Should -Be 'https://example.invalid/webhook'
            # Cmdlet default — server stores the literal "all events" subscription.
            $created.SubscribeToAllEvents | Should -Be $true
        }
        finally {
            Get-OrchWebhook -Path "${script:TargetDrive}:" -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchWebhook -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'Get | Export-Csv | Import-Csv | Update- round-trips Description and Enabled' {
        $name = "${script:Prefix}webhook-rt"
        $csv  = Join-Path $env:TEMP "${script:Prefix}webhook-rt.csv"
        try {
            # -SubscribeToAllEvents false + -Events <one rare event>: the
            # round-trip flips Enabled to true below; an enabled subscribe-to-all
            # webhook on a fake URL would fire on every tenant event, fail
            # delivery, auto-disable, and email the admin. Subscribing to a
            # single quiet event keeps it inert in practice. (The server now
            # rejects empty -Events when SubscribeToAllEvents=false, so we
            # must specify at least one; CSV column presence, not value, is
            # what's asserted, so any valid event works.)
            New-OrchWebhook -Path "${script:TargetDrive}:" -Name $name `
                -Url 'https://example.invalid/initial' -Description 'initial' -Enabled $false `
                -SubscribeToAllEvents false -Events 'process.created' `
                -ErrorAction Stop | Out-Null

            Get-OrchWebhook -Path "${script:TargetDrive}:" -Name $name -ExportCsv $csv | Out-Null
            $row = Import-Csv $csv | Where-Object Name -eq $name
            $row | Should -Not -BeNullOrEmpty
            # CSV columns must match Update- parameter names exactly.
            $row.PSObject.Properties.Name | Should -Contain 'Url'
            $row.PSObject.Properties.Name | Should -Contain 'Description'
            $row.PSObject.Properties.Name | Should -Contain 'Enabled'
            $row.PSObject.Properties.Name | Should -Contain 'AllowInsecureSsl'
            $row.PSObject.Properties.Name | Should -Contain 'SubscribeToAllEvents'
            $row.PSObject.Properties.Name | Should -Contain 'Events'
            $row.PSObject.Properties.Name | Should -Contain 'Secret'

            # Mutate two fields and pipe back through Update-.
            $row.Description = 'updated'
            $row.Enabled     = 'true'
            $row | Update-OrchWebhook | Out-Null

            $reread = Get-OrchWebhook -Path "${script:TargetDrive}:" -Name $name
            $reread.Description | Should -Be 'updated'
            $reread.Enabled     | Should -Be $true
        }
        finally {
            Get-OrchWebhook -Path "${script:TargetDrive}:" -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchWebhook -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item $csv -ErrorAction SilentlyContinue
        }
    }
}

Describe 'v1.5.3: Asset / Bucket / Queue Link -ExportCsv' {
    # Fixture seeds links (asset_links.csv etc.) via Import-Fixture.ps1.
    # Each link cmdlet's CSV must have Path / Name / Link columns —
    # those are the only meaningful fields per the link-cmdlet shape
    # tests; they match the Add-Orch*Link mandatory parameters.
    # Beyond the header check, assert every row has a non-empty value
    # in every column so a partially-populated row doesn't slip through.
    It 'Get-OrchAssetLink -ExportCsv emits Path / Name / Link with values populated' {
        $csv = Join-Path $env:TEMP "${script:Prefix}assetlinks.csv"
        try {
            Get-OrchAssetLink -Path "${script:TargetDrive}:\TestFixture_Base" -Recurse -ExportCsv $csv | Out-Null
            Test-Path $csv | Should -BeTrue -Because 'ExportCsv must create the file even on empty output'
            $header = (Get-Content $csv -TotalCount 1)
            $header | Should -Match '^Path,Name,Link\s*$'
            $rows = @(Import-Csv $csv)
            $rows.Count | Should -BeGreaterThan 0 -Because 'fixture seeds asset_links.csv into TestFixture_Base'
            foreach ($r in $rows) {
                $r.Path | Should -Not -BeNullOrEmpty
                $r.Name | Should -Not -BeNullOrEmpty
                $r.Link | Should -Not -BeNullOrEmpty
            }
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'Get-OrchBucketLink -ExportCsv emits Path / Name / Link with values populated' {
        $csv = Join-Path $env:TEMP "${script:Prefix}bucketlinks.csv"
        try {
            Get-OrchBucketLink -Path "${script:TargetDrive}:\TestFixture_Base" -Recurse -ExportCsv $csv | Out-Null
            Test-Path $csv | Should -BeTrue
            $header = (Get-Content $csv -TotalCount 1)
            $header | Should -Match '^Path,Name,Link\s*$'
            $rows = @(Import-Csv $csv)
            $rows.Count | Should -BeGreaterThan 0
            foreach ($r in $rows) {
                $r.Path | Should -Not -BeNullOrEmpty
                $r.Name | Should -Not -BeNullOrEmpty
                $r.Link | Should -Not -BeNullOrEmpty
            }
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'Get-OrchQueueLink -ExportCsv emits Path / Name / Link with values populated' {
        $csv = Join-Path $env:TEMP "${script:Prefix}queuelinks.csv"
        try {
            Get-OrchQueueLink -Path "${script:TargetDrive}:\TestFixture_Base" -Recurse -ExportCsv $csv | Out-Null
            Test-Path $csv | Should -BeTrue
            $header = (Get-Content $csv -TotalCount 1)
            $header | Should -Match '^Path,Name,Link\s*$'
            $rows = @(Import-Csv $csv)
            $rows.Count | Should -BeGreaterThan 0
            foreach ($r in $rows) {
                $r.Path | Should -Not -BeNullOrEmpty
                $r.Name | Should -Not -BeNullOrEmpty
                $r.Link | Should -Not -BeNullOrEmpty
            }
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }
}

Describe 'v1.5.3: -ExportCsv value-population on fixture-seeded entities' {
    # The header-presence tests above only prove "the column exists in the
    # CSV". This block asserts that the cmdlet actually emits a value in
    # the column — catches the bug class where a cmdlet writes the header
    # but populates from a server field that the LIST endpoint leaves null.
    #
    # Test target: fixture-seeded entities (Import-Fixture.ps1 11a / 11b /
    # 11c steps from v1.5.3). For each entity, fixture rows have known
    # non-empty values in known columns; CSV emission must preserve them.
    It 'Get-OrchApiTrigger -ExportCsv populates Method, Slug, CallingMode, Release' {
        $csv = Join-Path $env:TEMP "${script:Prefix}api_triggers.csv"
        try {
            Get-OrchApiTrigger -Path "${script:TargetDrive}:\TestFixture_Base" -Recurse -ExportCsv $csv | Out-Null
            $rows = @(Import-Csv $csv | Where-Object Name -in 'trig-api-1', 'trig-api-2', 'trig-api-prod')
            $rows.Count | Should -Be 3 -Because 'fixture seeds three api_triggers.csv rows'
            foreach ($r in $rows) {
                $r.Path        | Should -Not -BeNullOrEmpty
                $r.Name        | Should -Not -BeNullOrEmpty
                $r.Release     | Should -Not -BeNullOrEmpty -Because 'Release name resolved from ReleaseKey'
                $r.Method      | Should -Not -BeNullOrEmpty
                $r.Slug        | Should -Not -BeNullOrEmpty -Because 'cmdlet defaults Slug=Name when omitted, so server stores it'
                $r.CallingMode | Should -Not -BeNullOrEmpty
            }
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'Get-OrchTestDataQueue -ExportCsv populates Description and ContentJsonSchema' {
        $csv = Join-Path $env:TEMP "${script:Prefix}tdqs.csv"
        try {
            Get-OrchTestDataQueue -Path "${script:TargetDrive}:\TestFixture_Base" -Recurse -ExportCsv $csv | Out-Null
            $rows = @(Import-Csv $csv | Where-Object Name -in 'tdq-any', 'tdq-orders', 'tdq-qa')
            $rows.Count | Should -Be 3 -Because 'fixture seeds three test_data_queues.csv rows'
            foreach ($r in $rows) {
                $r.Path              | Should -Not -BeNullOrEmpty
                $r.Name              | Should -Not -BeNullOrEmpty
                $r.Description       | Should -Not -BeNullOrEmpty -Because 'fixture supplies Description on every row'
                $r.ContentJsonSchema | Should -Not -BeNullOrEmpty -Because 'server canonicalises {} but never leaves it empty'
            }
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'Get-OrchActionCatalog -ExportCsv populates Description and Encrypted' {
        $csv = Join-Path $env:TEMP "${script:Prefix}acs.csv"
        try {
            Get-OrchActionCatalog -Path "${script:TargetDrive}:\TestFixture_Base" -Recurse -ExportCsv $csv | Out-Null
            $rows = @(Import-Csv $csv | Where-Object Name -in 'ac-dev', 'ac-prod', 'ac-secure')
            $rows.Count | Should -Be 3 -Because 'fixture seeds three task_catalogs.csv rows'
            foreach ($r in $rows) {
                $r.Path        | Should -Not -BeNullOrEmpty
                $r.Name        | Should -Not -BeNullOrEmpty
                $r.Description | Should -Not -BeNullOrEmpty -Because 'fixture supplies Description on every row'
                $r.Encrypted   | Should -Not -BeNullOrEmpty -Because 'Encrypted is bool; server always populates'
            }
            # Also verify the "true" row really came back as true.
            $secure = $rows | Where-Object Name -eq 'ac-secure'
            $secure.Encrypted | Should -BeIn @('True', 'true')
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }
}

Describe 'v1.5.3: Get-OrchTestSetSchedule -ExportCsv header columns' {
    # TestSetSchedule creation is gated by a tenant feature flag (yotsuda
    # has it off), so we cannot create rows to round-trip. The CSV
    # *header* — which determines whether Import-Csv | New-/Update- will
    # bind correctly — can be verified against an empty result.
    It 'emits exactly the New-/Update- parameter names as columns' {
        $csv = Join-Path $env:TEMP "${script:Prefix}schedules.csv"
        try {
            Get-OrchTestSetSchedule -Path "${script:TargetDrive}:\TestFixture_Base" -Recurse -ExportCsv $csv | Out-Null
            Test-Path $csv | Should -BeTrue
            $header = (Get-Content $csv -TotalCount 1)
            # Order doesn't matter for downstream Import-Csv binding; verify set.
            $cols = $header -split ',' | ForEach-Object Trim
            $cols | Should -Contain 'Path'
            $cols | Should -Contain 'Name'
            $cols | Should -Contain 'Description'
            $cols | Should -Contain 'Enabled'
            $cols | Should -Contain 'TestSetName'
            $cols | Should -Contain 'CronExpression'
            $cols | Should -Contain 'TimeZoneId'
            $cols | Should -Contain 'CalendarName'
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }
}

Describe 'v1.5.x: New-OrchApiTrigger pipeline binding' {
    # Verifies ValueFromPipelineByPropertyName actually works end-to-end:
    # a PSCustomObject (the shape Import-Csv produces) piped in must bind
    # Name / Release / per-field columns and create the trigger.
    It 'binds Name / Release / Method from a piped PSCustomObject' {
        $name = "${script:Prefix}pipe"
        try {
            $row = [pscustomobject]@{
                Path        = $script:Folder
                Name        = $name
                Release     = $script:Release
                Method      = 'Post'
                CallingMode = 'FireAndForget'
                Description = 'piped'
            }
            $created = $row | New-OrchApiTrigger -ErrorAction Stop
            $created | Should -Not -BeNullOrEmpty
            $created.Name        | Should -Be $name
            $created.Method      | Should -Be 'Post'
            $created.CallingMode | Should -Be 'FireAndForget'
        }
        finally {
            Get-OrchApiTrigger -Path $script:Folder -Name $name -ErrorAction SilentlyContinue |
                Remove-OrchApiTrigger -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

Describe 'v1.5.x: completer candidates are valid input values' {
    # The contract (per maintainer): every completer candidate must be a
    # value the parameter actually accepts. Verify by driving TabExpansion2
    # against fixture-seeded entities and feeding the first candidate back
    # into the relevant cmdlet — it must resolve without error.
    It 'New-OrchApiTrigger -Release completer emits real release names' {
        $line = "New-OrchApiTrigger -Path $($script:Folder) -Release "
        $tab = TabExpansion2 $line $line.Length
        $tab.CompletionMatches.Count | Should -BeGreaterThan 0 -Because 'fixture seeds processes (releases)'
        $candidate = $tab.CompletionMatches[0].ListItemText
        # The candidate must be a real release in the folder.
        $rel = Get-OrchProcess -Path $script:Folder -Name $candidate
        $rel | Should -Not -BeNullOrEmpty -Because "completer suggested '$candidate' which must be a valid -Release value"
    }

    It 'New-OrchTestSetSchedule -TestSetName completer emits real TestSet names (or none)' {
        $line = "New-OrchTestSetSchedule -Path $($script:Folder) -Name x -TestSetName "
        $tab = TabExpansion2 $line $line.Length
        # The fixture seeds no TestSets (feature flag), so the candidate
        # list may be empty — that's acceptable. If non-empty, every
        # candidate must resolve to a real TestSet.
        if ($tab.CompletionMatches.Count -eq 0) {
            Set-ItResult -Skipped -Because 'no TestSets in fixture to complete against'
            return
        }
        $candidate = $tab.CompletionMatches[0].ListItemText
        Get-OrchTestSet -Path $script:Folder -Name $candidate | Should -Not -BeNullOrEmpty
    }

    It 'Update-OrchTestSetSchedule -Name completer emits real schedule names (or none)' {
        $line = "Update-OrchTestSetSchedule -Path $($script:Folder) -Name "
        $tab = TabExpansion2 $line $line.Length
        if ($tab.CompletionMatches.Count -eq 0) {
            Set-ItResult -Skipped -Because 'no TestSetSchedules in fixture (tenant feature flag)'
            return
        }
        $candidate = $tab.CompletionMatches[0].ListItemText
        Get-OrchTestSetSchedule -Path $script:Folder -Name $candidate | Should -Not -BeNullOrEmpty
    }

    It 'New-OrchTestSetSchedule -TimeZoneId completer emits valid TimeZoneInfo Ids' {
        $line = "New-OrchTestSetSchedule -Path $($script:Folder) -Name x -TimeZoneId Tokyo"
        $tab = TabExpansion2 $line $line.Length
        $tab.CompletionMatches.Count | Should -BeGreaterThan 0
        foreach ($m in $tab.CompletionMatches) {
            # Every emitted Id must be a real system time zone.
            { [System.TimeZoneInfo]::FindSystemTimeZoneById($m.ListItemText) } |
                Should -Not -Throw -Because "completer emitted '$($m.ListItemText)' which must be a valid TimeZoneId"
        }
    }
}

Describe 'v1.5.3: Update-OrchTestSetSchedule against tenant feature flag' {
    # As long as the wrapped server endpoint is gated by the same flag
    # as creation, success-path verification is blocked. The cmdlet's
    # job in that environment is to: build a correct payload, resolve
    # TestSetName → TestSetId / CalendarName → CalendarId, and surface
    # the server error cleanly. We verify the third part — that the
    # cmdlet doesn't silently swallow a "not found" or PUT with stale
    # state — by exercising it against an entity name that doesn't exist
    # in the target folder. Cmdlet must not throw; it must WriteError.
    It 'returns cleanly when no schedule matches the supplied -Name' {
        $err = $null
        # Wildcard guaranteed not to match anything.
        Update-OrchTestSetSchedule -Path $script:Folder -Name "${script:Prefix}-nope-*" -Enabled false `
            -ErrorAction SilentlyContinue -ErrorVariable err | Out-Null
        # No match => no PUT, no error. The expectation here is "cmdlet
        # is a no-op on empty match", same pattern as Update-OrchTrigger.
        $err | Should -BeNullOrEmpty
    }
}
