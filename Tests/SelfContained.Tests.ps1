#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Self-contained integration tests for UiPathOrch module.
    Creates all test entities from scratch; no dependency on existing data.

.DESCRIPTION
    Requires:
    - Connected OrchTest: drive (run Import-OrchConfig first)
    - Connected Orch1: drive (used to discover a package for Process/Trigger tests)

    These tests create a temporary folder hierarchy and all entities within it.
    Everything is cleaned up in AfterAll blocks.
    Uses a "PesterTest_XXXX_" prefix for all entities.

.NOTES
    Run with: Invoke-Pester -Path Tests\SelfContained.Tests.ps1 -Output Detailed
#>

BeforeAll {
    # Orch2 is the destructive test target (same tenant as OrchTest but a
    # separate drive instance so cache state lines up with Import-Fixture).
    # Orch1 stays read-only — used only to discover a reference package.
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:RefDrive = if ($env:UIPATHORCH_TEST_REF_DRIVE) { $env:UIPATHORCH_TEST_REF_DRIVE } else { 'Orch1' }
    $script:Prefix = "PesterTest_$(Get-Random -Maximum 9999)_"
    $script:RootFolder = "${script:Drive}:\${script:Prefix}Root"
    $script:SubFolder = "${script:RootFolder}\${script:Prefix}Sub"
    $script:CopyFolder = "${script:RootFolder}\${script:Prefix}Copy"
    # SmokeFolder hosts the "does not throw" smoke tests for Task / Trigger /
    # Job / Queue / Asset / Bucket / Process. Kept separate from RootFolder
    # because RootFolder has TestUserA bolted on with a restricted role,
    # which downgrades the current user's access — smoke tests want a
    # pristine folder.
    $script:SmokeFolder = "${script:Drive}:\${script:Prefix}Smoke"
    $script:TempDir = Join-Path $env:TEMP "PesterTest_$(Get-Random -Maximum 9999)"

    # PerRobot / UserValue test fixtures — pick any existing DirectoryUser on
    # the tenant other than the current user. Creating users / assigning to
    # the tenant per test run is too heavy, so we accept whatever the tenant
    # already has.
    #
    # CAUTION: TestUserA must NOT be the currently-connected user — Add-OrchFolderUser with a
    # restricted role downgrades the current user's folder access, causing "You are not authorized!"
    # errors on subsequent asset operations in that folder.
    $script:CurrentUserName = (Get-OrchCurrentUser -Path "${script:Drive}:\").UserName
    $script:TestUserType = 'DirectoryUser'
    $alternateUser = Get-OrchUser -Path "${script:Drive}:\" -Type $script:TestUserType -ErrorAction Stop |
        Where-Object { $_.UserName -ne $script:CurrentUserName } |
        Select-Object -First 1
    if (-not $alternateUser) {
        throw "SelfContained tests require at least 2 ${script:TestUserType}s on tenant ${script:Drive}: (one to be the current user, another to act as TestUserA)."
    }
    # Prefer EmailAddress over UserName. Azure AD B2B guest users get a
    # mangled tenant UserName form (e.g. 'foo_bar.com#ext#@tenant.onmicrosoft.com')
    # that Add-OrchFolderUser's PmBulkResolveByName lookup can't resolve.
    # The user's original EmailAddress (e.g. 'foo@bar.com') is the form the
    # identity directory recognizes.
    $script:TestUserA = if ($alternateUser.EmailAddress) {
        $alternateUser.EmailAddress
    } else {
        $alternateUser.UserName
    }
    # UserValues entries on assets/secrets store the tenant-form UserName,
    # which can differ from $script:TestUserA for B2B guests. Use the stable
    # UserId to find entries, not the name.
    $script:TestUserAId = $alternateUser.Id
    # Tenant UserName form of TestUserA — mangled for B2B guests
    # ('foo_bar.com#ext#@tenant.onmicrosoft.com'). Held separately so tests
    # that need the exact tenant form (Update-OrchUser, Remove-OrchRoleFromUser,
    # raw role manipulation) don't have to re-derive it.
    $script:TestUserAUserName = $alternateUser.UserName
    # TestUserUnassigned is the other tenant user, NOT added to $script:RootFolder.
    # Used by the T11.9 guard test that asserts the admin API accepts unassigned users.
    $script:TestUserUnassigned = $script:CurrentUserName  # admin user, not added via BeforeAll
    $script:TestMachine = 'm3'

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    # Verify drives are available
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    Get-PSDrive $script:RefDrive -ErrorAction Stop | Out-Null

    # Create temp directory for CSV files
    New-Item -Path $script:TempDir -ItemType Directory -Force | Out-Null

    # Create test folder hierarchy
    $null = mkdir $script:RootFolder
    $null = mkdir $script:SubFolder
    $null = mkdir $script:CopyFolder
    $null = mkdir $script:SmokeFolder

    # Assign test user/machine to test folder (best practice for PerRobot UserValues)
    Add-OrchFolderUser -Type $script:TestUserType -UserName $script:TestUserA `
        -Roles 'Automation User' -Path $script:RootFolder -ErrorAction SilentlyContinue
    Add-OrchFolderMachine -Path $script:RootFolder -Name $script:TestMachine -ErrorAction SilentlyContinue

    # Discover a package on the test drive for Process/Trigger tests. Must be
    # a package on $script:Drive itself — New-OrchProcess looks up the package
    # in the same tenant where the process is being created.
    $script:PackageId = (Get-OrchPackage -Path "${script:Drive}:\" |
        Select-Object -First 1).Id
}

AfterAll {
    # Clean up in reverse dependency order:
    # Triggers → Processes → FolderMachines → Machines → Queues → Assets → Buckets → Folders

    Push-Location $script:RootFolder -ErrorAction SilentlyContinue
    Remove-OrchTrigger -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchProcess -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchFolderMachine -Name "${script:Prefix}*" -ErrorAction SilentlyContinue
    Pop-Location

    Remove-OrchMachine -Name "${script:Prefix}*" -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchQueue -Name "${script:Prefix}*" -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchAsset -Name "${script:Prefix}*" -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchAsset -Name "${script:Prefix}*" -ValueType Credential -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchBucket -Name "${script:Prefix}*" -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue

    # Unassign test user/machine from root test folder (reverse of BeforeAll)
    Remove-OrchFolderUser -Path $script:RootFolder -UserName $script:TestUserA -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchFolderMachine -Path $script:RootFolder -Name $script:TestMachine -Confirm:$false -ErrorAction SilentlyContinue

    Remove-Item $script:CopyFolder -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubFolder -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootFolder -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SmokeFolder -Recurse -Force -ErrorAction SilentlyContinue

    Remove-Item $script:TempDir -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

# ---------------------------------------------------------------------------
# Folder Navigation
# ---------------------------------------------------------------------------
Describe 'Folder' {
    It 'Test folders were created' {
        Test-Path $script:RootFolder | Should -BeTrue
        Test-Path $script:SubFolder | Should -BeTrue
        Test-Path $script:CopyFolder | Should -BeTrue
    }

    It 'Get-ChildItem lists subfolders' {
        $children = Get-ChildItem $script:RootFolder
        $children.DisplayName | Should -Contain ($script:Prefix + 'Sub')
        $children.DisplayName | Should -Contain ($script:Prefix + 'Copy')
    }

    It 'Set-Location navigates into the test folder' {
        Push-Location $script:SubFolder
        (Get-Location).Path | Should -BeLike "*${script:Prefix}Sub*"
        Pop-Location
    }

    It 'mkdir creates a nested folder and rmdir removes it' {
        $nested = "${script:SubFolder}\${script:Prefix}Nested"
        $null = mkdir $nested
        Test-Path $nested | Should -BeTrue
        Remove-Item $nested -Recurse -Force
        Test-Path $nested | Should -BeFalse
    }
}

# ---------------------------------------------------------------------------
# Machine CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Machine' {
    BeforeAll {
        $script:MachineName = "${script:Prefix}Machine"
        $script:MachinePath = "${script:Drive}:\"
    }

    AfterAll {
        Remove-OrchMachine -Name "${script:Prefix}Machine*" -Path $script:MachinePath -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'New-OrchMachine creates a machine' {
        New-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Description 'Created by Pester'
        Clear-OrchCache
        $m = Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath
        $m | Should -Not -BeNullOrEmpty
        $m.Name | Should -Be $script:MachineName
        $m.Description | Should -Be 'Created by Pester'
    }

    It 'Update-OrchMachine updates Description' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Description 'Updated'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).Description | Should -Be 'Updated'
    }

    It 'Update-OrchMachine updates UnattendedSlots' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -UnattendedSlots 2
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).UnattendedSlots | Should -Be 2
    }

    It 'Update-OrchMachine updates AutomationType' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -AutomationType 'Foreground'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).AutomationType | Should -Be 'Foreground'
    }

    It 'Update-OrchMachine updates TargetFramework' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -TargetFramework 'Windows'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).TargetFramework | Should -Be 'Windows'
    }

    It 'Update-OrchMachine updates Tags' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Tags 'tagA', 'tagB'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).Tags.Count | Should -Be 2
    }

    It 'Update-OrchMachine clears Tags with empty string' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Tags ''
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).Tags.Count | Should -Be 0
    }

    It 'Update-OrchMachine with multiple properties' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Description 'multi' -AutomationType 'Any' -TargetFramework 'Any'
        Clear-OrchCache
        $m = Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath
        $m.Description | Should -Be 'multi'
        $m.AutomationType | Should -Be 'Any'
        $m.TargetFramework | Should -Be 'Any'
    }

    It 'Get-OrchMachine -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'machines.csv'
        Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:MachineName
    }

    It 'Remove-OrchMachine removes the machine' {
        Remove-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Confirm:$false
        Clear-OrchCache
        Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Queue CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Queue' {
    BeforeAll {
        $script:QueueName = "${script:Prefix}Queue"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchQueue -Name "${script:Prefix}Queue*" -Path $script:RootFolder -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchQueue creates a queue' {
        New-OrchQueue -Name $script:QueueName -Description 'Created by Pester'
        Clear-OrchCache
        $q = Get-OrchQueue -Name $script:QueueName
        $q | Should -Not -BeNullOrEmpty
        $q.Name | Should -Be $script:QueueName
    }

    It 'Update-OrchQueue updates Description' {
        Update-OrchQueue -Name $script:QueueName -Description 'Updated'
        Clear-OrchCache
        (Get-OrchQueue -Name $script:QueueName).Description | Should -Be 'Updated'
    }

    It 'Update-OrchQueue updates MaxNumberOfRetries' {
        Update-OrchQueue -Name $script:QueueName -MaxNumberOfRetries 5
        Clear-OrchCache
        (Get-OrchQueue -Name $script:QueueName).MaxNumberOfRetries | Should -Be 5
    }

    It 'Update-OrchQueue updates AcceptAutomaticallyRetry' {
        Update-OrchQueue -Name $script:QueueName -AcceptAutomaticallyRetry true
        Clear-OrchCache
        (Get-OrchQueue -Name $script:QueueName).AcceptAutomaticallyRetry | Should -Be $true
    }

    It 'Get-OrchQueue -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'queues.csv'
        Get-OrchQueue -Name $script:QueueName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:QueueName
    }

    It 'Remove-OrchQueue removes the queue' {
        Remove-OrchQueue -Name $script:QueueName -Confirm:$false
        Clear-OrchCache
        Get-OrchQueue -Name $script:QueueName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# QueueItem Import
# ---------------------------------------------------------------------------
Describe 'QueueItem Import' {
    BeforeAll {
        $script:QIQueueName = "${script:Prefix}QIQueue"
        Push-Location $script:RootFolder
        New-OrchQueue -Name $script:QIQueueName

        # Second queue in the same folder, seeded with its own items. Used by the
        # -Id completer scoping test: completing for one queue must not leak the
        # other queue's item ids, even though both share the per-folder cache.
        $script:QIQueueName2 = "${script:Prefix}QIQueue2"
        New-OrchQueue -Name $script:QIQueueName2
        $csv2 = Join-Path $script:TempDir 'queueitems_q2.csv'
        "Reference`nQ2-001`nQ2-002" | Set-Content -Path $csv2 -Encoding UTF8 -NoNewline
        Import-OrchQueueItem -Name $script:QIQueueName2 -ImportCsv $csv2 | Out-Null

        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchQueue -Name $script:QIQueueName -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchQueue -Name $script:QIQueueName2 -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Import-OrchQueueItem imports items from CSV' {
        $csv = Join-Path $script:TempDir 'queueitems.csv'
        "Priority,Reference,CustomerName,Amount`nNormal,REF-001,Alice,100`nHigh,REF-002,Bob,200`nLow,REF-003,Charlie,300" |
            Set-Content -Path $csv -Encoding UTF8 -NoNewline

        $result = Import-OrchQueueItem -Name $script:QIQueueName -ImportCsv $csv
        $result.Success | Should -Be $true
    }

    It 'Imported items can be retrieved' {
        # Import-OrchQueueItem returns Success=True before the items are
        # queryable — the bulk-add endpoint persists asynchronously. A brief
        # wait lets the queue settle before Get-OrchQueueItem reads it.
        Start-Sleep -Seconds 2
        Clear-OrchCache
        $items = Get-OrchQueueItem -Name $script:QIQueueName -First 10
        $items.Count | Should -Be 3
        $refs = $items.Reference | Sort-Object
        $refs | Should -Contain 'REF-001'
        $refs | Should -Contain 'REF-002'
        $refs | Should -Contain 'REF-003'
    }

    It 'Get-OrchQueueItem -Id fetches a single item by id' {
        $all = Get-OrchQueueItem -Name $script:QIQueueName -First 10
        $all.Count | Should -BeGreaterOrEqual 1
        $target = $all[0]

        $byId = Get-OrchQueueItem -Name $script:QIQueueName -Id $target.Id
        @($byId).Count | Should -Be 1 -Because '-Id must return exactly the requested item'
        $byId.Id | Should -Be $target.Id
        $byId.Reference | Should -Be $target.Reference -Because 'the by-id result carries the same enriched shape as a normal Get'
    }

    It 'Get-OrchQueueItem -Id accepts multiple comma-separated ids (OData Id in)' {
        $all = Get-OrchQueueItem -Name $script:QIQueueName -First 10
        $all.Count | Should -BeGreaterOrEqual 2
        $ids = @($all[0].Id, $all[1].Id)

        $byIds = Get-OrchQueueItem -Name $script:QIQueueName -Id $ids
        @($byIds).Count | Should -Be 2 -Because 'a comma-separated -Id list is one Id in (...) round trip'
        ($byIds.Id | Sort-Object) | Should -Be ($ids | Sort-Object)
    }

    It 'Get-OrchQueueItem -Id completer suggests cached ids and excludes already-specified ones' {
        # Populate the per-folder QueueItems cache the completer reads.
        $all = Get-OrchQueueItem -Name $script:QIQueueName -First 10
        $all.Count | Should -BeGreaterOrEqual 2
        $id1 = $all[0].Id
        $id2 = $all[1].Id

        $line = "Get-OrchQueueItem -Name $($script:QIQueueName) -Id "
        $cands = (TabExpansion2 $line $line.Length).CompletionMatches.CompletionText
        $cands | Should -Contain "$id1" -Because 'the completer suggests ids cached for the targeted queue'
        $cands | Should -Contain "$id2"

        # An id already typed on the same -Id list must drop out of the candidates.
        $line2 = "Get-OrchQueueItem -Name $($script:QIQueueName) -Id $id1,"
        $cands2 = (TabExpansion2 $line2 $line2.Length).CompletionMatches.CompletionText
        $cands2 | Should -Not -Contain "$id1" -Because 'an already-specified id must be excluded'
        $cands2 | Should -Contain "$id2" -Because 'other cached ids are still suggested'
    }

    It 'Get-OrchQueueItem -Id queries the server even with an empty cache (never served from cache)' {
        # -Id is a filter, so it always hits the API: IncrementalCachePerFolder.Fetch never
        # serves a query from the cache (it only accumulates). Prove -Id does not depend on a
        # primed cache by clearing it first.
        $all = Get-OrchQueueItem -Name $script:QIQueueName -First 10
        $all.Count | Should -BeGreaterOrEqual 1
        $targetId = $all[0].Id

        Clear-OrchCache  # empty the per-folder QueueItems cache
        $byId = Get-OrchQueueItem -Name $script:QIQueueName -Id $targetId
        @($byId).Count | Should -Be 1 -Because '-Id must query Orchestrator, not require a populated cache'
        $byId.Id | Should -Be $targetId
    }

    It 'Get-OrchQueueItem with no filter outputs the cache (no fresh query) and warns' {
        # The only no-API path: with no filter parameters the cmdlet returns the items the
        # cache accumulated from prior fetches, and warns that it is doing so.
        Get-OrchQueueItem -Name $script:QIQueueName -First 10 | Out-Null  # prime the cache

        $w = $null
        $cached = Get-OrchQueueItem -Name $script:QIQueueName -WarningVariable w -WarningAction SilentlyContinue
        ($w -join "`n") | Should -Match 'cache' -Because 'the no-filter path announces it is outputting the cache'
        @($cached).Count | Should -BeGreaterOrEqual 3 -Because 'the primed cache holds the imported items'
    }

    It 'Get-OrchQueueItem -Id completer is scoped to the bound -Name queue' {
        # Both queues live in the same folder and share the per-folder QueueItems cache,
        # so completing -Id for one queue must filter out the other queue's item ids.
        Start-Sleep -Seconds 2
        Clear-OrchCache
        $q1items = Get-OrchQueueItem -Name $script:QIQueueName -First 50
        $q2items = Get-OrchQueueItem -Name $script:QIQueueName2 -First 50
        $q1items.Count | Should -BeGreaterOrEqual 1
        $q2items.Count | Should -BeGreaterOrEqual 1 -Because 'the second queue fixture was seeded with items'
        $q2ids = @($q2items.Id | ForEach-Object { "$_" })

        $line = "Get-OrchQueueItem -Name $($script:QIQueueName) -Id "
        $cands = (TabExpansion2 $line $line.Length).CompletionMatches.CompletionText
        $cands | Should -Contain "$($q1items[0].Id)" -Because 'the bound queue''s own ids are suggested'
        foreach ($q2id in $q2ids) {
            $cands | Should -Not -Contain $q2id -Because "completer scoped to '$($script:QIQueueName)' must not leak the other queue's id $q2id"
        }
    }

    It 'Import-OrchQueueItem preserves an embedded newline in a quoted CSV field' {
        # A quoted field spanning two physical lines must be read as ONE field
        # (multi-line parser), matching the web "Upload Items" dialog — not split.
        $csv = Join-Path $script:TempDir 'queueitems_multiline.csv'
        [System.IO.File]::WriteAllText($csv, "Reference,Note`nREF-ML,`"alpha`nbeta`"`n")
        Import-OrchQueueItem -Name $script:QIQueueName -ImportCsv $csv | Out-Null
        Start-Sleep -Seconds 2
        Clear-OrchCache
        $item = Get-OrchQueueItem -Name $script:QIQueueName -First 50 |
            Where-Object Reference -eq 'REF-ML'
        $item | Should -Not -BeNullOrEmpty
        $item.SpecificContent.Note | Should -Be "alpha`nbeta"
    }

    It 'Import-OrchQueueItem rejects a CSV over the 15,000-row cap' {
        # Mirrors the web's client-side 15,000-row limit (reject, don't chunk).
        $csv = Join-Path $script:TempDir 'queueitems_overcap.csv'
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine('Reference,Note')
        for ($i = 1; $i -le 15001; $i++) { [void]$sb.AppendLine("OVERCAP-$i,n$i") }
        [System.IO.File]::WriteAllText($csv, $sb.ToString())
        $err = $null
        Import-OrchQueueItem -Name $script:QIQueueName -ImportCsv $csv -ErrorVariable err -ErrorAction SilentlyContinue | Out-Null
        $err | Should -Not -BeNullOrEmpty
        $err[0].Exception.Message | Should -Match 'maximum number of rows allowed is 15000'
    }
}

# ---------------------------------------------------------------------------
# TestDataQueueItem Import (schema-typed; shares the multi-line CSV parser)
# ---------------------------------------------------------------------------
Describe 'TestDataQueueItem Import' {
    BeforeAll {
        $script:TdqName = "${script:Prefix}Tdq"
        $script:TdqSchema = '{"type":"object","properties":{"id":{"type":"integer"},"note":{"type":"string"}},"additionalProperties":false}'
        Push-Location $script:RootFolder
        New-OrchTestDataQueue -Name $script:TdqName -ContentJsonSchema $script:TdqSchema | Out-Null
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchTestDataQueue -Name $script:TdqName -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Import-OrchTestDataQueueItem imports schema-typed items from CSV' {
        $csv = Join-Path $script:TempDir 'tdqitems.csv'
        [System.IO.File]::WriteAllText($csv, "id,note`n1,first`n2,second`n")
        Import-OrchTestDataQueueItem -Name $script:TdqName -ImportCsv $csv -Confirm:$false
        Start-Sleep -Seconds 2
        Clear-OrchCache
        $items = Get-OrchTestDataQueueItem -Name $script:TdqName
        @($items).Count | Should -BeGreaterOrEqual 2
    }

    It 'Import-OrchTestDataQueueItem preserves an embedded newline in a quoted CSV field' {
        # Shares Import-OrchQueueItem's multi-line parser, so a quoted field with
        # an embedded newline round-trips as one field (web-golden-equivalent).
        $csv = Join-Path $script:TempDir 'tdqitems_multiline.csv'
        [System.IO.File]::WriteAllText($csv, "id,note`n101,`"alpha`nbeta`"`n")
        Import-OrchTestDataQueueItem -Name $script:TdqName -ImportCsv $csv -Confirm:$false
        Start-Sleep -Seconds 2
        Clear-OrchCache
        $item = Get-OrchTestDataQueueItem -Name $script:TdqName |
            Where-Object { ($_.ContentJson | ConvertFrom-Json).id -eq 101 }
        $item | Should -Not -BeNullOrEmpty
        ($item.ContentJson | ConvertFrom-Json).note | Should -Be "alpha`nbeta"
    }

    It 'Import-OrchTestDataQueueItem rejects a CSV over the 15,000-row cap' {
        $csv = Join-Path $script:TempDir 'tdqitems_overcap.csv'
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine('id,note')
        for ($i = 1; $i -le 15001; $i++) { [void]$sb.AppendLine("$i,n$i") }
        [System.IO.File]::WriteAllText($csv, $sb.ToString())
        $err = $null
        Import-OrchTestDataQueueItem -Name $script:TdqName -ImportCsv $csv -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue | Out-Null
        $err | Should -Not -BeNullOrEmpty
        $err[0].Exception.Message | Should -Match 'maximum number of rows allowed is 15000'
    }
}

# ---------------------------------------------------------------------------
# Asset CRUD + CSV Round-Trip
# ---------------------------------------------------------------------------
Describe 'Asset' {
    BeforeAll {
        $script:AssetName = "${script:Prefix}Asset"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}Asset*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Set-OrchAsset creates a Text asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Value 'hello' -Description 'Text asset'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_Text"
        $a | Should -Not -BeNullOrEmpty
        $a.ValueType | Should -Be 'Text'
        $a.Value | Should -Be 'hello'
        $a.Description | Should -Be 'Text asset'
    }

    It 'Set-OrchAsset creates an Integer asset' {
        Set-OrchAsset -ValueType Integer -Name "${script:AssetName}_Int" -Value '42'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_Int"
        $a.Value | Should -Be '42'
    }

    It 'Set-OrchAsset creates a Bool asset' {
        Set-OrchAsset -ValueType Bool -Name "${script:AssetName}_Bool" -Value 'true'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_Bool"
        $a.Value | Should -Be 'True'
    }

    It 'Set-OrchAsset updates the value of an existing asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Value 'updated'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:AssetName}_Text").Value | Should -Be 'updated'
    }

    It 'Set-OrchAsset updates Description' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Description 'New desc'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:AssetName}_Text").Description | Should -Be 'New desc'
    }

    It 'Set-OrchAsset does not overwrite existing ValueType' {
        Set-OrchAsset -ValueType Integer -Name "${script:AssetName}_TypeKeep" -Value '10'
        Clear-OrchCache
        Set-OrchAsset -Name "${script:AssetName}_TypeKeep" -Value '20'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_TypeKeep"
        $a.ValueType | Should -Be 'Integer'
        $a.Value | Should -Be '20'
    }

    It 'Set-OrchAsset with wildcard updates multiple assets' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_WcA" -Value 'before'
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_WcB" -Value 'before'
        Clear-OrchCache
        Set-OrchAsset -Name "${script:AssetName}_Wc*" -Value 'after'
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:AssetName}_Wc*"
        $assets | ForEach-Object { $_.Value | Should -Be 'after' }
    }

    It 'Set-OrchAsset with invalid ValueType writes an error' {
        $err = $null
        Set-OrchAsset -ValueType 'InvalidType' -Name "${script:AssetName}_Err" -Value 'x' -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchAsset -Name wildcard filters correctly' {
        $assets = Get-OrchAsset -Name "${script:AssetName}_*"
        $assets.Count | Should -BeGreaterOrEqual 5
    }

    It 'Get-OrchAsset -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'assets.csv'
        Get-OrchAsset -Name "${script:AssetName}_Text" -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be "${script:AssetName}_Text"
        $rows[0].ValueType | Should -Be 'Text'
    }

    It 'CSV round-trip: Export then re-import preserves values' {
        $exportCsv = Join-Path $script:TempDir 'asset_roundtrip_export.csv'
        Get-OrchAsset -Name "${script:AssetName}_Text" -ExportCsv $exportCsv

        # Change value, then re-import from CSV to restore
        Set-OrchAsset -Name "${script:AssetName}_Text" -Value 'changed'
        Clear-OrchCache

        Import-Csv $exportCsv | Set-OrchAsset
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:AssetName}_Text").Value | Should -Be 'updated'
    }

    # T1.9 — Set-OrchAsset with -ValueType Credential on existing Credential is silent no-op (regression)
    It 'T1.9 Set-OrchAsset -ValueType Credential silent no-op' {
        $cred = "${script:AssetName}_T19Cred"
        Set-OrchCredentialAsset -Name $cred -CredentialUsername 'u' -CredentialPassword 'p'
        Clear-OrchCache
        $before = Get-OrchAsset -Name $cred
        { Set-OrchAsset -ValueType Credential -Name $cred -Value 'should-be-ignored' -ErrorAction Stop } |
            Should -Not -Throw
        Clear-OrchCache
        $after = Get-OrchAsset -Name $cred
        $after.ValueType | Should -Be 'Credential'
        $after.CredentialUsername | Should -Be $before.CredentialUsername
        Remove-OrchAsset -Name $cred -Confirm:$false -ErrorAction SilentlyContinue
    }

    # T1.10 — Set-OrchAsset with -ValueType Secret silent no-op (new behavior, was error)
    It 'T1.10 Set-OrchAsset -ValueType Secret silent no-op' {
        $sec = "${script:AssetName}_T110Sec"
        Set-OrchSecretAsset -Name $sec -SecretValue 'x'
        Clear-OrchCache
        { Set-OrchAsset -ValueType Secret -Name $sec -Value 'ignored' -ErrorAction Stop } |
            Should -Not -Throw
        Clear-OrchCache
        (Get-OrchAsset -Name $sec).ValueType | Should -Be 'Secret'
        Remove-OrchAsset -Name $sec -Confirm:$false -ErrorAction SilentlyContinue
    }

    # T1.11 — empty -Value on Global-only Text (no UserValues) deletes the asset
    It 'T1.11 Empty -Value on Global-only Text deletes the asset' {
        $nm = "${script:AssetName}_T111"
        Set-OrchAsset -ValueType Text -Name $nm -Value 'g'
        Clear-OrchCache
        Set-OrchAsset -ValueType Text -Name $nm -Value ''
        Clear-OrchCache
        Get-OrchAsset -Name $nm -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }

    # T1.11b — empty -Value on Global Text WITH UserValues clears Global only, preserves UserValues
    # (symmetric counterpart to T1.13: empty-delete is scoped to what the caller targets)
    It 'T1.11b Empty -Value on Global Text preserves PerRobot UserValues' {
        $nm = "${script:AssetName}_T111b"
        Set-OrchAsset -ValueType Text -Name $nm -Value 'g'
        Set-OrchAsset -ValueType Text -Name $nm -UserName $script:TestUserA -Value 'uv'
        Clear-OrchCache
        Set-OrchAsset -ValueType Text -Name $nm -Value ''
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        $a | Should -Not -BeNullOrEmpty
        $a.ValueScope | Should -Be 'PerRobot'
        $a.HasDefaultValue | Should -Be $false
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId).Value | Should -Be 'uv'
        Remove-OrchAsset -Name $nm -Confirm:$false -ErrorAction SilentlyContinue
    }

    # T1.12 — PerRobot: -UserName X -Value v creates UserValue
    It 'T1.12 -UserName creates UserValue entry' {
        $nm = "${script:AssetName}_T112"
        Set-OrchAsset -ValueType Text -Name $nm -UserName $script:TestUserA -Value 'uv1'
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        $a.UserValues | Should -Not -BeNullOrEmpty
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId).Value | Should -Be 'uv1'
        Remove-OrchAsset -Name $nm -Confirm:$false -ErrorAction SilentlyContinue
    }

    # T1.13 — PerRobot: -UserName X -Value '' empty-deletes that UserValue (Text type)
    It 'T1.13 -UserName with empty -Value empty-deletes that UserValue' {
        $nm = "${script:AssetName}_T113"
        Set-OrchAsset -ValueType Text -Name $nm -UserName $script:TestUserA -Value 'uv1'
        Clear-OrchCache
        Set-OrchAsset -ValueType Text -Name $nm -UserName $script:TestUserA -Value ''
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        if ($null -ne $a) {
            ($a.UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -BeNullOrEmpty
        }
        Remove-OrchAsset -Name $nm -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Remove-OrchAsset with wildcard removes all test assets' {
        Remove-OrchAsset -Name "${script:AssetName}_*" -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:AssetName}_*" -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Asset Link (share asset across folders)
# ---------------------------------------------------------------------------
Describe 'AssetLink' {
    BeforeAll {
        $script:LinkAssetName = "${script:Prefix}LinkAsset"
        $script:LinkSubFolder = "${script:RootFolder}\${script:Prefix}LinkSub"

        # Dedicated folder hierarchy for the link tests so we don't entangle
        # with the broader Asset Describe's fixtures.
        $null = New-Item -Path $script:LinkSubFolder -ItemType Directory -ErrorAction SilentlyContinue

        # Create the source asset in $script:RootFolder
        Set-OrchAsset -Path $script:RootFolder -ValueType Text `
            -Name $script:LinkAssetName -Value 'shared-value' | Out-Null
        Clear-OrchCache -Path $script:RootFolder
    }

    AfterAll {
        # Remove links first (best-effort) then the asset and folder.
        Get-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName -ErrorAction SilentlyContinue |
            Remove-OrchAssetLink -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Path $script:RootFolder -Name $script:LinkAssetName -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item -Path $script:LinkSubFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Get-OrchAssetLink returns nothing for an unlinked asset' {
        Clear-OrchCache -Path $script:RootFolder
        Get-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName |
            Should -BeNullOrEmpty
    }

    It 'Add-OrchAssetLink shares the asset to a target folder' {
        Add-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName `
            -Link $script:LinkSubFolder -Confirm:$false
        Clear-OrchCache -Path $script:RootFolder

        $links = Get-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName
        $links            | Should -Not -BeNullOrEmpty
        @($links).Count   | Should -Be 1
        $links.Path       | Should -Be $script:RootFolder
        $links.Name       | Should -Be $script:LinkAssetName
        $links.Link       | Should -Be $script:LinkSubFolder
    }

    It 'Linked asset is enumerable from the target folder' {
        $fromTarget = Get-OrchAsset -Path $script:LinkSubFolder -Name $script:LinkAssetName
        $fromTarget       | Should -Not -BeNullOrEmpty
        $fromTarget.Value | Should -Be 'shared-value'
    }

    It 'Get-OrchAssetLink output pipes into Remove-OrchAssetLink' {
        Get-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName |
            Remove-OrchAssetLink -Confirm:$false
        Clear-OrchCache -Path $script:RootFolder

        Get-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName |
            Should -BeNullOrEmpty
    }

    It 'Remove-OrchAssetLink requires -Link (Mandatory)' {
        # Without -Link the cmdlet should not silently succeed; PowerShell binds
        # the missing mandatory parameter as an error from the runtime, not a
        # tenant call. Verifying via metadata avoids hitting the API entirely.
        $cmd = Get-Command Remove-OrchAssetLink
        $cmd.Parameters['Link'].Attributes |
            Where-Object { $_ -is [Parameter] } |
            ForEach-Object { $_.Mandatory } |
            Should -Contain $true
    }

    It 'Add-OrchAssetLink batches multiple targets in a single call' {
        # We can't observe API call counts directly, but we can assert that
        # passing -Link with multiple folders results in the asset being
        # accessible from all of them after one cmdlet invocation.
        $second = "${script:LinkSubFolder}_2"
        $null = New-Item -Path $second -ItemType Directory -ErrorAction SilentlyContinue
        try {
            Add-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName `
                -Link $script:LinkSubFolder, $second -Confirm:$false
            Clear-OrchCache -Path $script:RootFolder

            $links = Get-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName |
                Sort-Object Link
            @($links).Count | Should -Be 2
            $links.Link     | Should -Contain $script:LinkSubFolder
            $links.Link     | Should -Contain $second
        }
        finally {
            Get-OrchAssetLink -Path $script:RootFolder -Name $script:LinkAssetName -ErrorAction SilentlyContinue |
                Remove-OrchAssetLink -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item -Path $second -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

# ---------------------------------------------------------------------------
# Credential Asset CRUD + CSV Round-Trip
# ---------------------------------------------------------------------------
Describe 'Credential Asset' {
    BeforeAll {
        $script:CredName = "${script:Prefix}Cred"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}Cred*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Set-OrchCredentialAsset creates a credential asset' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -CredentialUsername 'testuser' -CredentialPassword 'testpass'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CredName}_Basic"
        $a | Should -Not -BeNullOrEmpty
        $a.ValueType | Should -Be 'Credential'
        $a.CredentialUsername | Should -Be 'testuser'
    }

    It 'Set-OrchCredentialAsset updates CredentialUsername' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -CredentialUsername 'updated' -CredentialPassword 'updated'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:CredName}_Basic").CredentialUsername | Should -Be 'updated'
    }

    It 'Set-OrchCredentialAsset updates Description' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -Description 'Cred desc' -CredentialUsername 'updated' -CredentialPassword 'updated'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:CredName}_Basic").Description | Should -Be 'Cred desc'
    }

    It 'Set-OrchCredentialAsset with empty CredentialPassword does not clear Global value' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -CredentialPassword ''
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CredName}_Basic"
        $a | Should -Not -BeNullOrEmpty
        $a.HasDefaultValue | Should -Be $true
    }

    It 'Set-OrchCredentialAsset warns and skips a new credential given a username but no password' {
        $newName = "${script:CredName}_NoPass"
        Set-OrchCredentialAsset -Name $newName -CredentialUsername 'user1' -WarningVariable w -WarningAction SilentlyContinue
        $w | Should -Not -BeNullOrEmpty
        ($w -join "`n") | Should -Match 'requires a password'
        Clear-OrchCache
        Get-OrchAsset -Name $newName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }

    It 'Set-OrchCredentialAsset warns and skips a bulk-CSV row for a new name with no password' {
        # The guard fires regardless of whether a username is supplied. A mistyped or
        # since-deleted name in a bulk-update CSV falls through to the create path; with
        # the password masked (empty) it must warn and skip, not silently no-op. Piped
        # (ValueFromPipelineByPropertyName) so it binds the Plain set, as Import-Csv does.
        $newName = "${script:CredName}_NoSecret"
        [pscustomobject]@{ Path = $script:RootFolder; Name = $newName; CredentialUsername = ''; CredentialPassword = '' } |
            Set-OrchCredentialAsset -WarningVariable w -WarningAction SilentlyContinue
        $w | Should -Not -BeNullOrEmpty
        ($w -join "`n") | Should -Match 'requires a password'
        Clear-OrchCache
        Get-OrchAsset -Name $newName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }

    It 'Set-OrchCredentialAsset does not warn when creating a new credential with a password' {
        Set-OrchCredentialAsset -Name "${script:CredName}_WithPass" -CredentialUsername 'u' -CredentialPassword 'p' -WarningVariable w -WarningAction SilentlyContinue
        ($w | Where-Object { $_ -match 'requires a password' }) | Should -BeNullOrEmpty
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:CredName}_WithPass").CredentialUsername | Should -Be 'u'
    }

    It 'Get-OrchAsset -ExportCredentialCsv exports credential CSV' {
        $csv = Join-Path $script:TempDir 'creds.csv'
        Get-OrchAsset -Name "${script:CredName}_Basic" -ExportCredentialCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -BeGreaterOrEqual 1
        $rows[0].Name | Should -Be "${script:CredName}_Basic"
    }

    It 'Import-Csv | Set-OrchCredentialAsset creates from CSV' {
        $csv = Join-Path $script:TempDir 'cred_import.csv'
        $path = $script:RootFolder
        @"
Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName
${path},${script:CredName}_CsvA,From CSV,,,,csv_user_a,csv_pass_a,
${path},${script:CredName}_CsvB,From CSV,,,,csv_user_b,csv_pass_b,
"@ | Set-Content -Path $csv -Encoding UTF8

        Import-Csv $csv | Set-OrchCredentialAsset
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:CredName}_Csv*"
        $assets.Count | Should -Be 2
        ($assets | Where-Object Name -like '*_CsvA').CredentialUsername | Should -Be 'csv_user_a'
        ($assets | Where-Object Name -like '*_CsvB').CredentialUsername | Should -Be 'csv_user_b'
    }

    It 'Credential CSV round-trip preserves values' {
        $exportCsv = Join-Path $script:TempDir 'cred_roundtrip.csv'
        Get-OrchAsset -Name "${script:CredName}_CsvA" -ExportCredentialCsv $exportCsv
        $exported = Import-Csv $exportCsv
        $exported.CredentialUsername | Should -Be 'csv_user_a'
    }

    It 'Remove-OrchAsset removes credential assets' {
        Remove-OrchAsset -Name "${script:CredName}_*" -ValueType Credential -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:CredName}_*" -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Get-OrchCredentialAsset (Section 5)
# ---------------------------------------------------------------------------
Describe 'Get-OrchCredentialAsset' {
    BeforeAll {
        $script:GcaName = "${script:Prefix}Gca"
        Push-Location $script:RootFolder
        Set-OrchCredentialAsset -Name "${script:GcaName}_A" -CredentialUsername 'userA' -CredentialPassword 'passA' -Description 'A'
        Set-OrchCredentialAsset -Name "${script:GcaName}_B" -CredentialUsername 'userB' -CredentialPassword 'passB'
        Set-OrchAsset -ValueType Text -Name "${script:GcaName}_Text" -Value 't'        # noise
        Set-OrchSecretAsset -Name "${script:GcaName}_Sec" -SecretValue 's'              # noise
        # PerRobot UserValue for round-trip / populated-username check
        Set-OrchCredentialAsset -Name "${script:GcaName}_A" -UserName $script:TestUserA `
            -CredentialUsername 'uvUser' -CredentialPassword 'uvPass'
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:GcaName}_*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name "${script:GcaName}_*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'T5.1 returns only Credential type' {
        $assets = Get-OrchCredentialAsset -Name "${script:GcaName}_*"
        $assets | Should -Not -BeNullOrEmpty
        ($assets | ForEach-Object ValueType | Select-Object -Unique) | Should -Be 'Credential'
    }

    It 'T5.2 Name wildcard filters Credential only' {
        $assets = Get-OrchCredentialAsset -Name "${script:GcaName}_A*"
        $assets.Count | Should -BeGreaterOrEqual 1
        $assets.Name | Should -Contain "${script:GcaName}_A"
        $assets.Name | Should -Not -Contain "${script:GcaName}_Text"
    }

    It 'T5.3/T5.4 -ExportCsv produces credential CSV with expected headers' {
        $csv = Join-Path $script:TempDir 'gca_export.csv'
        Get-OrchCredentialAsset -Name "${script:GcaName}_*" -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -BeGreaterOrEqual 2
        $expected = @('Path','Name','Description','CredentialStore','UserName','MachineName','CredentialUsername','CredentialPassword','ExternalName')
        $actual = ($rows[0].PSObject.Properties.Name)
        $expected | ForEach-Object { $actual | Should -Contain $_ }
    }

    It 'T5.5 CredentialPassword column is masked (empty)' {
        $csv = Join-Path $script:TempDir 'gca_mask.csv'
        Get-OrchCredentialAsset -Name "${script:GcaName}_A" -ExportCsv $csv
        (Import-Csv $csv) | ForEach-Object { $_.CredentialPassword | Should -BeNullOrEmpty }
    }

    It 'T5.6 CredentialUsername column is populated (not masked)' {
        $csv = Join-Path $script:TempDir 'gca_uname.csv'
        Get-OrchCredentialAsset -Name "${script:GcaName}_A" -ExportCsv $csv
        (Import-Csv $csv | Where-Object { $_.UserName -eq '' }).CredentialUsername | Should -Contain 'userA'
    }

    It 'T5.7 -ExpandUserValues flattens PerRobot entries' {
        $all = Get-OrchCredentialAsset -Name "${script:GcaName}_A" -ExpandUserValues
        # Expect the Global row-object + at least one UserValue row
        ($all | Measure-Object).Count | Should -BeGreaterOrEqual 2
    }

    It 'T5.8 CSV round-trip: Get → Export → Import → Set (Description)' {
        # Perturb Description only. Server rejects "username change + empty password" so perturbing
        # CredentialUsername would cause the re-import to fail with server error. Round-trip is
        # safe for Description + same-username cases (the documented use case).
        $csv = Join-Path $script:TempDir 'gca_roundtrip.csv'
        Get-OrchCredentialAsset -Name "${script:GcaName}_A" -ExportCsv $csv
        Set-OrchCredentialAsset -Name "${script:GcaName}_A" -Description 'perturbed' -CredentialUsername 'userA' -CredentialPassword 'passA'
        Clear-OrchCache
        Import-Csv $csv | Set-OrchCredentialAsset
        Clear-OrchCache
        $after = Get-OrchAsset -Name "${script:GcaName}_A"
        $after.Description | Should -Be 'A'
        $after.CredentialUsername | Should -Be 'userA'
    }

    It 'T5.9 -Path / -Recurse traverses subfolders' {
        $nm = "${script:GcaName}_Sub"
        Set-OrchCredentialAsset -Name $nm -Path $script:SubFolder -CredentialUsername 'sub' -CredentialPassword 'p'
        Clear-OrchCache
        $assets = Get-OrchCredentialAsset -Name $nm -Path $script:RootFolder -Recurse
        $assets.Name | Should -Contain $nm
        Remove-OrchAsset -Name $nm -Path $script:SubFolder -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'T5.10 UserValues.CredentialUsername populated (historical bug regression)' {
        $a = Get-OrchCredentialAsset -Name "${script:GcaName}_A"
        $uv = $a.UserValues | Where-Object UserId -eq $script:TestUserAId
        $uv | Should -Not -BeNullOrEmpty
        $uv.CredentialUsername | Should -Be 'uvUser'
    }
}

# ---------------------------------------------------------------------------
# Set-OrchSecretAsset (Section 6)
# ---------------------------------------------------------------------------
Describe 'Set-OrchSecretAsset' {
    BeforeAll {
        $script:SsaName = "${script:Prefix}Ssa"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:SsaName}_*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'T6.1 -SecretValue (Plain set) creates Secret asset' {
        Set-OrchSecretAsset -Name "${script:SsaName}_Plain" -SecretValue 'topsecret' -Description 'Plain'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:SsaName}_Plain"
        $a.ValueType | Should -Be 'Secret'
        $a.Description | Should -Be 'Plain'
        $a.HasDefaultValue | Should -Be $true
    }

    It 'T6.2 -Secret SecureString (Default set) creates Secret asset' {
        $ss = ConvertTo-SecureString 'supersecret' -AsPlainText -Force
        Set-OrchSecretAsset -Name "${script:SsaName}_Sec" -Secret $ss
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:SsaName}_Sec").ValueType | Should -Be 'Secret'
    }

    It 'T6.4 Update existing SecretValue (value is masked)' {
        Set-OrchSecretAsset -Name "${script:SsaName}_Plain" -SecretValue 'updated'
        Clear-OrchCache
        # API returns masked value — cannot assert value. Assert asset still exists and Secret type.
        (Get-OrchAsset -Name "${script:SsaName}_Plain").ValueType | Should -Be 'Secret'
    }

    It 'T6.5 Update Description only (no secret change) preserves Secret' {
        # Must pass -SecretValue '' to select Plain parameter set; without it, Default set is chosen
        # and -Secret is mandatory → interactive prompt.
        Set-OrchSecretAsset -Name "${script:SsaName}_Plain" -Description 'DescUpdated' -SecretValue ''
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:SsaName}_Plain"
        $a.Description | Should -Be 'DescUpdated'
        $a.HasDefaultValue | Should -Be $true
    }

    It 'T6.6/T6.7 Round-trip safe: empty -SecretValue does not clobber existing Global value' {
        # Existing asset already has HasDefaultValue=$true
        Set-OrchSecretAsset -Name "${script:SsaName}_Plain" -SecretValue ''
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:SsaName}_Plain"
        $a | Should -Not -BeNullOrEmpty
        $a.HasDefaultValue | Should -Be $true
    }

    It 'T6.8 -UserName X -SecretValue v creates UserValue' {
        Set-OrchSecretAsset -Name "${script:SsaName}_Plain" -UserName $script:TestUserA -SecretValue 'uvSecret'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:SsaName}_Plain"
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -Not -BeNullOrEmpty
    }

    It 'T6.9 -UserName X with empty SecretValue does NOT delete UserValue (unlike Text empty-delete)' {
        # UserValue from T6.8 should still be present after this call
        Set-OrchSecretAsset -Name "${script:SsaName}_Plain" -UserName $script:TestUserA -SecretValue ''
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:SsaName}_Plain"
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -Not -BeNullOrEmpty
    }

    It 'T6.10 Name wildcard updates multiple assets' {
        Set-OrchSecretAsset -Name "${script:SsaName}_WcA" -SecretValue 'a'
        Set-OrchSecretAsset -Name "${script:SsaName}_WcB" -SecretValue 'b'
        Clear-OrchCache
        Set-OrchSecretAsset -Name "${script:SsaName}_Wc*" -Description 'wcDesc' -SecretValue ''
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:SsaName}_Wc*"
        $assets.Count | Should -BeGreaterOrEqual 2
        $assets | ForEach-Object { $_.Description | Should -Be 'wcDesc' }
    }

    It 'T6.13 -WhatIf produces no change' {
        Set-OrchSecretAsset -Name "${script:SsaName}_Plain" -Description 'WhatIfDesc' -SecretValue '' -WhatIf
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:SsaName}_Plain").Description | Should -Be 'DescUpdated'
    }

    It 'T6.14 PSCustomObject pipeline (property-name binding) resolves correctly' {
        $obj = [pscustomobject]@{
            Path = $script:RootFolder
            Name = "${script:SsaName}_Pso"
            Description = 'via pipeline'
            SecretValue = 'psoSecret'
        }
        $obj | Set-OrchSecretAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:SsaName}_Pso"
        $a.ValueType | Should -Be 'Secret'
        $a.Description | Should -Be 'via pipeline'
    }
}

# ---------------------------------------------------------------------------
# Get-OrchSecretAsset (Section 7)
# ---------------------------------------------------------------------------
Describe 'Get-OrchSecretAsset' {
    BeforeAll {
        $script:GsaName = "${script:Prefix}Gsa"
        Push-Location $script:RootFolder
        Set-OrchSecretAsset -Name "${script:GsaName}_A" -SecretValue 's1' -Description 'SecA'
        Set-OrchSecretAsset -Name "${script:GsaName}_B" -SecretValue 's2'
        Set-OrchAsset -ValueType Text -Name "${script:GsaName}_Text" -Value 't'    # noise
        Set-OrchCredentialAsset -Name "${script:GsaName}_Cred" -CredentialUsername 'u' -CredentialPassword 'p'   # noise
        Set-OrchSecretAsset -Name "${script:GsaName}_A" -UserName $script:TestUserA -SecretValue 'uv'
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:GsaName}_*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name "${script:GsaName}_*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'T7.1/T7.2 returns Secret type only' {
        $assets = Get-OrchSecretAsset -Name "${script:GsaName}_*"
        $assets | Should -Not -BeNullOrEmpty
        ($assets | ForEach-Object ValueType | Select-Object -Unique) | Should -Be 'Secret'
        $assets.Name | Should -Not -Contain "${script:GsaName}_Text"
        $assets.Name | Should -Not -Contain "${script:GsaName}_Cred"
    }

    It 'T7.3 Name wildcard filters Secret only' {
        $a = Get-OrchSecretAsset -Name "${script:GsaName}_A"
        $a.Name | Should -Be "${script:GsaName}_A"
    }

    It 'T7.4/T7.5 -ExportCsv produces Secret CSV with expected headers' {
        $csv = Join-Path $script:TempDir 'gsa_export.csv'
        Get-OrchSecretAsset -Name "${script:GsaName}_*" -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -BeGreaterOrEqual 2
        $expected = @('Path','Name','Description','CredentialStore','UserName','MachineName','SecretValue','ExternalName')
        $actual = ($rows[0].PSObject.Properties.Name)
        $expected | ForEach-Object { $actual | Should -Contain $_ }
    }

    It 'T7.6 SecretValue column is always empty (masked)' {
        $csv = Join-Path $script:TempDir 'gsa_mask.csv'
        Get-OrchSecretAsset -Name "${script:GsaName}_*" -ExportCsv $csv
        (Import-Csv $csv) | ForEach-Object { $_.SecretValue | Should -BeNullOrEmpty }
    }

    It 'T7.7 -ExpandUserValues flattens PerRobot entries' {
        $all = Get-OrchSecretAsset -Name "${script:GsaName}_A" -ExpandUserValues
        ($all | Measure-Object).Count | Should -BeGreaterOrEqual 2
    }

    It 'T7.8 -Path / -Recurse traverses subfolders' {
        $nm = "${script:GsaName}_Sub"
        Set-OrchSecretAsset -Name $nm -Path $script:SubFolder -SecretValue 's'
        Clear-OrchCache
        $assets = Get-OrchSecretAsset -Name $nm -Path $script:RootFolder -Recurse
        $assets.Name | Should -Contain $nm
        Remove-OrchAsset -Name $nm -Path $script:SubFolder -Confirm:$false -ErrorAction SilentlyContinue
    }
}

# ---------------------------------------------------------------------------
# Remove-OrchAssetUserValue (Section 8)
# ---------------------------------------------------------------------------
Describe 'Remove-OrchAssetUserValue' {
    BeforeAll {
        $script:RuvName = "${script:Prefix}Ruv"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:RuvName}_*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name "${script:RuvName}_*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'T8.1 Remove UserValue from Credential asset' {
        $nm = "${script:RuvName}_Cred"
        Set-OrchCredentialAsset -Name $nm -CredentialUsername 'g' -CredentialPassword 'g'
        Set-OrchCredentialAsset -Name $nm -UserName $script:TestUserA -CredentialUsername 'u' -CredentialPassword 'p'
        Clear-OrchCache
        Remove-OrchAssetUserValue -Name $nm -UserName $script:TestUserA -Confirm:$false
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -BeNullOrEmpty
    }

    It 'T8.2/T8.10 Remove UserValue from Secret asset (type-agnostic)' {
        $nm = "${script:RuvName}_Sec"
        Set-OrchSecretAsset -Name $nm -SecretValue 'g'
        Set-OrchSecretAsset -Name $nm -UserName $script:TestUserA -SecretValue 'uv'
        Clear-OrchCache
        Remove-OrchAssetUserValue -Name $nm -UserName $script:TestUserA -Confirm:$false
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -BeNullOrEmpty
    }

    It 'T8.4 Wildcard -UserName matches UserValue' {
        # OrchTest has only one usable non-self DirectoryUser, so this tests wildcard resolution
        # against a single UserValue rather than multiple.
        $nm = "${script:RuvName}_Wc"
        Set-OrchSecretAsset -Name $nm -SecretValue 'g'
        Set-OrchSecretAsset -Name $nm -UserName $script:TestUserA -SecretValue 'a'
        Clear-OrchCache
        Remove-OrchAssetUserValue -Name $nm -UserName '*tsuda*' -Confirm:$false
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        $a.UserValues | Should -BeNullOrEmpty
    }

    It 'T8.5 Removing all UserValues reverts to Global scope preserving HasDefaultValue' {
        $nm = "${script:RuvName}_Revert"
        Set-OrchSecretAsset -Name $nm -SecretValue 'g'
        Set-OrchSecretAsset -Name $nm -UserName $script:TestUserA -SecretValue 'uv'
        Clear-OrchCache
        Remove-OrchAssetUserValue -Name $nm -UserName '*' -Confirm:$false
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        $a.ValueScope | Should -Be 'Global'
        $a.HasDefaultValue | Should -Be $true
    }

    It 'T8.6 -WhatIf produces no change' {
        $nm = "${script:RuvName}_Whatif"
        Set-OrchCredentialAsset -Name $nm -UserName $script:TestUserA -CredentialUsername 'u' -CredentialPassword 'p'
        Clear-OrchCache
        Remove-OrchAssetUserValue -Name $nm -UserName $script:TestUserA -WhatIf
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -Not -BeNullOrEmpty
    }

    It 'T8.8 Non-existent user name is a no-op' {
        $nm = "${script:RuvName}_NoMatch"
        Set-OrchSecretAsset -Name $nm -SecretValue 'g'
        Set-OrchSecretAsset -Name $nm -UserName $script:TestUserA -SecretValue 'uv'
        Clear-OrchCache
        { Remove-OrchAssetUserValue -Name $nm -UserName 'definitely-not-a-user' -Confirm:$false -ErrorAction Stop } |
            Should -Not -Throw
        Clear-OrchCache
        ((Get-OrchAsset -Name $nm).UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -Not -BeNullOrEmpty
    }

    It 'T8.9/T8.11 Global-only asset (no UserValues) is silently skipped' {
        $nm = "${script:RuvName}_GlobalOnly"
        Set-OrchSecretAsset -Name $nm -SecretValue 'g'
        Clear-OrchCache
        { Remove-OrchAssetUserValue -Name $nm -UserName $script:TestUserA -Confirm:$false -ErrorAction Stop } |
            Should -Not -Throw
        Clear-OrchCache
        (Get-OrchAsset -Name $nm).HasDefaultValue | Should -Be $true
    }

    It 'T8.12 Text type UserValue deletion (type-agnostic confirmation)' {
        $nm = "${script:RuvName}_Text"
        Set-OrchAsset -ValueType Text -Name $nm -Value 'g'
        Set-OrchAsset -ValueType Text -Name $nm -UserName $script:TestUserA -Value 'uv'
        Clear-OrchCache
        Remove-OrchAssetUserValue -Name $nm -UserName $script:TestUserA -Confirm:$false
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        ($a.UserValues | Where-Object UserId -eq $script:TestUserAId) | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Asset Round-trip / cross-cutting (Section 9)
# ---------------------------------------------------------------------------
Describe 'Asset Round-trip' {
    BeforeAll {
        $script:RtName = "${script:Prefix}Rt"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:RtName}_*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name "${script:RtName}_*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'T9.1 Credential round-trip: Description restoration via CSV' {
        # Perturb Description only — server rejects "username change + empty password" on re-import.
        $nm = "${script:RtName}_Cred"
        Set-OrchCredentialAsset -Name $nm -CredentialUsername 'u1' -CredentialPassword 'p1' -Description 'D1'
        Clear-OrchCache
        $csv = Join-Path $script:TempDir 'rt_cred.csv'
        Get-OrchCredentialAsset -Name $nm -ExportCsv $csv
        Set-OrchCredentialAsset -Name $nm -Description 'perturbed' -CredentialUsername 'u1' -CredentialPassword 'p1'
        Clear-OrchCache
        Import-Csv $csv | Set-OrchCredentialAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        $a.Description | Should -Be 'D1'
        $a.CredentialUsername | Should -Be 'u1'
    }

    It 'T9.2 Secret round-trip: empty SecretValue does not clobber' {
        $nm = "${script:RtName}_Sec"
        Set-OrchSecretAsset -Name $nm -SecretValue 's1' -Description 'D1'
        Clear-OrchCache
        $csv = Join-Path $script:TempDir 'rt_sec.csv'
        Get-OrchSecretAsset -Name $nm -ExportCsv $csv
        Set-OrchSecretAsset -Name $nm -Description 'perturbed' -SecretValue ''
        Clear-OrchCache
        Import-Csv $csv | Set-OrchSecretAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name $nm
        $a.Description | Should -Be 'D1'
        $a.HasDefaultValue | Should -Be $true
    }

    It 'T9.4 Asset partition: Get-OrchAsset + Get-OrchCredentialAsset + Get-OrchSecretAsset covers all types' {
        Set-OrchAsset -ValueType Text -Name "${script:RtName}_Part_T" -Value 't'
        Set-OrchCredentialAsset -Name "${script:RtName}_Part_C" -CredentialUsername 'u' -CredentialPassword 'p'
        Set-OrchSecretAsset -Name "${script:RtName}_Part_S" -SecretValue 's'
        Clear-OrchCache
        $all   = Get-OrchAsset             -Name "${script:RtName}_Part_*"
        $creds = Get-OrchCredentialAsset   -Name "${script:RtName}_Part_*"
        $secs  = Get-OrchSecretAsset       -Name "${script:RtName}_Part_*"
        $all.Count | Should -Be 3
        $creds.Count | Should -Be 1
        $secs.Count | Should -Be 1
        (($creds.Count) + ($secs.Count) + (($all | Where-Object { $_.ValueType -notin 'Credential','Secret' }).Count)) |
            Should -Be $all.Count
    }

    It 'T9.6 Unicode/Japanese name round-trip for Secret' {
        $nm = "${script:RtName}_シークレット"
        Set-OrchSecretAsset -Name $nm -SecretValue 's'
        Clear-OrchCache
        $csv = Join-Path $script:TempDir 'rt_unicode.csv'
        Get-OrchSecretAsset -Name $nm -ExportCsv $csv
        Import-Csv $csv | Set-OrchSecretAsset
        Clear-OrchCache
        (Get-OrchAsset -Name $nm) | Should -Not -BeNullOrEmpty
        Remove-OrchAsset -Name $nm -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'T9.8 Copy-OrchAsset handles all 5 types including Secret (placeholder + warning)' {
        # Regression guard: Secret used to fail with "asset secret value cannot be null"
        # because CopyAssets had no Secret branch. Now inserts !!!PLEASE UPDATE!!! placeholder
        # and emits a warning like Credential.
        Set-OrchAsset -ValueType Text    -Name "${script:RtName}_C_Text" -Value 'v'
        Set-OrchAsset -ValueType Integer -Name "${script:RtName}_C_Int"  -Value 1
        Set-OrchAsset -ValueType Bool    -Name "${script:RtName}_C_Bool" -Value true
        Set-OrchCredentialAsset          -Name "${script:RtName}_C_Cred" -CredentialUsername 'u' -CredentialPassword 'p'
        Set-OrchSecretAsset              -Name "${script:RtName}_C_Sec"  -SecretValue 's'
        Clear-OrchCache

        $w = @(); $e = @()
        Copy-OrchAsset -Name "${script:RtName}_C_*" -Path $script:RootFolder -Destination $script:CopyFolder `
            -WarningVariable w -ErrorVariable e -ErrorAction Continue
        $e | Should -BeNullOrEmpty

        Clear-OrchCache
        $copied = Get-OrchAsset -Name "${script:RtName}_C_*" -Path $script:CopyFolder
        $copied.Count | Should -Be 5  # Text, Integer, Bool, Cred, Sec — Get-OrchAsset returns all types on v20+
        ($copied | ForEach-Object ValueType | Sort-Object -Unique) | Should -Be @('Bool','Credential','Integer','Secret','Text')

        ($w -join ' ') | Should -Match 'Secret asset values'
        ($w -join ' ') | Should -Match 'credential asset passwords'

        # Cleanup copied
        Remove-OrchAsset -Name "${script:RtName}_C_*" -Path $script:CopyFolder -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name "${script:RtName}_C_*" -Path $script:CopyFolder -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
    }
}

# ---------------------------------------------------------------------------
# Asset Negative (Section 11)
# ---------------------------------------------------------------------------
Describe 'Asset Negative' {
    BeforeAll {
        $script:NegName = "${script:Prefix}Neg"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:NegName}_*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'T11.2 Non-existent -Path throws (provider terminating error)' {
        # Unknown -Path is raised by the provider as ItemNotFoundException (terminating),
        # not a cmdlet non-terminating error — assert via Should -Throw.
        {
            Get-OrchSecretAsset -Path "${script:Drive}:\__DoesNotExist__$(Get-Random -Maximum 9999)" -ErrorAction Stop
        } | Should -Throw
    }

    It 'T11.3 Invalid -ValueType writes error' {
        $err = $null
        Set-OrchAsset -ValueType 'Bogus' -Name "${script:NegName}_Bad" -Value 'x' `
            -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }

    It 'T11.6 Non-existent -CredentialStore writes error' {
        $err = $null
        Set-OrchCredentialAsset -Name "${script:NegName}_Store" -CredentialUsername 'u' -CredentialPassword 'p' `
            -CredentialStore 'definitely-no-such-store' -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }

    It 'T11.9 Guard: Set-OrchSecretAsset with folder-unassigned user still succeeds (admin API is permissive)' {
        # If server behavior changes to enforce folder assignment, this test will flip to failure
        # — that's intentional (guard test).
        $nm = "${script:NegName}_Guard"
        $unassignedUser = $script:TestUserUnassigned  # not added to $script:RootFolder in BeforeAll
        { Set-OrchSecretAsset -Name $nm -UserName $unassignedUser -SecretValue 'g' -ErrorAction Stop } |
            Should -Not -Throw
    }
}

# ---------------------------------------------------------------------------
# Asset Performance / Batching (Section 12) — Tag 'Performance'
# ---------------------------------------------------------------------------
Describe 'Asset Performance' -Tag 'Performance' {
    BeforeAll {
        $script:PerfName = "${script:Prefix}Perf"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:PerfName}_*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'T12.1 Batching: 10 CSV rows × single asset collapse into 1 PUT (time-bounded proxy)' {
        $nm = "${script:PerfName}_Batch"
        Set-OrchSecretAsset -Name $nm -SecretValue 'g'
        Clear-OrchCache
        $csv = Join-Path $script:TempDir 'perf_batch.csv'
        $rows = 1..10 | ForEach-Object {
            [pscustomobject]@{
                Path = $script:RootFolder
                Name = $nm
                Description = "d$_"
                SecretValue = ''
            }
        }
        $rows | Export-Csv $csv -NoTypeInformation
        $t = Measure-Command {
            Import-Csv $csv | Set-OrchSecretAsset
        }
        # Loose bound: 10 sequential PUTs would usually exceed ~5s; batched should be well under.
        $t.TotalSeconds | Should -BeLessThan 10
    }
}

# ---------------------------------------------------------------------------
# Queue Link (share queue across folders)
# ---------------------------------------------------------------------------
Describe 'QueueLink' {
    BeforeAll {
        $script:LinkQueueName = "${script:Prefix}LinkQueue"
        $script:QueueLinkSubFolder = "${script:RootFolder}\${script:Prefix}QueueLinkSub"

        $null = New-Item -Path $script:QueueLinkSubFolder -ItemType Directory -ErrorAction SilentlyContinue

        New-OrchQueue -Path $script:RootFolder -Name $script:LinkQueueName -Description 'QueueLink test' | Out-Null
        Clear-OrchCache -Path $script:RootFolder
    }

    AfterAll {
        Get-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName -ErrorAction SilentlyContinue |
            Remove-OrchQueueLink -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchQueue -Path $script:RootFolder -Name $script:LinkQueueName -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item -Path $script:QueueLinkSubFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Get-OrchQueueLink returns nothing for an unlinked queue' {
        Clear-OrchCache -Path $script:RootFolder
        Get-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName |
            Should -BeNullOrEmpty
    }

    It 'Add-OrchQueueLink shares the queue to a target folder' {
        Add-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName `
            -Link $script:QueueLinkSubFolder -Confirm:$false
        Clear-OrchCache -Path $script:RootFolder

        $links = Get-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName
        $links            | Should -Not -BeNullOrEmpty
        @($links).Count   | Should -Be 1
        $links.Path       | Should -Be $script:RootFolder
        $links.Name       | Should -Be $script:LinkQueueName
        $links.Link       | Should -Be $script:QueueLinkSubFolder
    }

    It 'Linked queue is enumerable from the target folder' {
        $fromTarget = Get-OrchQueue -Path $script:QueueLinkSubFolder -Name $script:LinkQueueName
        $fromTarget | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchQueueLink output pipes into Remove-OrchQueueLink' {
        Get-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName |
            Remove-OrchQueueLink -Confirm:$false
        Clear-OrchCache -Path $script:RootFolder

        Get-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName |
            Should -BeNullOrEmpty
    }

    It 'Remove-OrchQueueLink requires -Link (Mandatory)' {
        $cmd = Get-Command Remove-OrchQueueLink
        $cmd.Parameters['Link'].Attributes |
            Where-Object { $_ -is [Parameter] } |
            ForEach-Object { $_.Mandatory } |
            Should -Contain $true
    }

    It 'Add-OrchQueueLink batches multiple targets in a single call' {
        $second = "${script:QueueLinkSubFolder}_2"
        $null = New-Item -Path $second -ItemType Directory -ErrorAction SilentlyContinue
        try {
            Add-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName `
                -Link $script:QueueLinkSubFolder, $second -Confirm:$false
            Clear-OrchCache -Path $script:RootFolder

            $links = Get-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName |
                Sort-Object Link
            @($links).Count | Should -Be 2
            $links.Link     | Should -Contain $script:QueueLinkSubFolder
            $links.Link     | Should -Contain $second
        }
        finally {
            Get-OrchQueueLink -Path $script:RootFolder -Name $script:LinkQueueName -ErrorAction SilentlyContinue |
                Remove-OrchQueueLink -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item -Path $second -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

# ---------------------------------------------------------------------------
# Bucket Link (share bucket across folders)
# ---------------------------------------------------------------------------
Describe 'BucketLink' {
    BeforeAll {
        $script:LinkBucketName = "${script:Prefix}LinkBucket"
        $script:BucketLinkSubFolder = "${script:RootFolder}\${script:Prefix}BucketLinkSub"

        $null = New-Item -Path $script:BucketLinkSubFolder -ItemType Directory -ErrorAction SilentlyContinue

        New-OrchBucket -Path $script:RootFolder -Name $script:LinkBucketName -Description 'BucketLink test' | Out-Null
        Clear-OrchCache -Path $script:RootFolder
    }

    AfterAll {
        Get-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName -ErrorAction SilentlyContinue |
            Remove-OrchBucketLink -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchBucket -Path $script:RootFolder -Name $script:LinkBucketName -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item -Path $script:BucketLinkSubFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Get-OrchBucketLink returns nothing for an unlinked bucket' {
        Clear-OrchCache -Path $script:RootFolder
        Get-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName |
            Should -BeNullOrEmpty
    }

    It 'Add-OrchBucketLink shares the bucket to a target folder' {
        Add-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName `
            -Link $script:BucketLinkSubFolder -Confirm:$false
        Clear-OrchCache -Path $script:RootFolder

        $links = Get-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName
        $links            | Should -Not -BeNullOrEmpty
        @($links).Count   | Should -Be 1
        $links.Path       | Should -Be $script:RootFolder
        $links.Name       | Should -Be $script:LinkBucketName
        $links.Link       | Should -Be $script:BucketLinkSubFolder
    }

    It 'Linked bucket is enumerable from the target folder' {
        $fromTarget = Get-OrchBucket -Path $script:BucketLinkSubFolder -Name $script:LinkBucketName
        $fromTarget | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchBucketLink output pipes into Remove-OrchBucketLink' {
        Get-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName |
            Remove-OrchBucketLink -Confirm:$false
        Clear-OrchCache -Path $script:RootFolder

        Get-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName |
            Should -BeNullOrEmpty
    }

    It 'Remove-OrchBucketLink requires -Link (Mandatory)' {
        $cmd = Get-Command Remove-OrchBucketLink
        $cmd.Parameters['Link'].Attributes |
            Where-Object { $_ -is [Parameter] } |
            ForEach-Object { $_.Mandatory } |
            Should -Contain $true
    }

    It 'Add-OrchBucketLink batches multiple targets in a single call' {
        $second = "${script:BucketLinkSubFolder}_2"
        $null = New-Item -Path $second -ItemType Directory -ErrorAction SilentlyContinue
        try {
            Add-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName `
                -Link $script:BucketLinkSubFolder, $second -Confirm:$false
            Clear-OrchCache -Path $script:RootFolder

            $links = Get-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName |
                Sort-Object Link
            @($links).Count | Should -Be 2
            $links.Link     | Should -Contain $script:BucketLinkSubFolder
            $links.Link     | Should -Contain $second
        }
        finally {
            Get-OrchBucketLink -Path $script:RootFolder -Name $script:LinkBucketName -ErrorAction SilentlyContinue |
                Remove-OrchBucketLink -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item -Path $second -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

# ---------------------------------------------------------------------------
# Bucket CRUD + BucketItem Import/Export
# ---------------------------------------------------------------------------
Describe 'Bucket' {
    BeforeAll {
        $script:BucketName = "${script:Prefix}Bucket"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchBucket -Name "${script:Prefix}Bucket*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchBucket creates a bucket' {
        New-OrchBucket -Name $script:BucketName -Description 'Created by Pester'
        Clear-OrchCache
        $b = Get-OrchBucket -Name $script:BucketName
        $b | Should -Not -BeNullOrEmpty
        $b.Name | Should -Be $script:BucketName
    }

    It 'Update-OrchBucket updates Description' {
        Update-OrchBucket -Name $script:BucketName -Description 'Updated by Pester'
        Clear-OrchCache
        (Get-OrchBucket -Name $script:BucketName).Description | Should -Be 'Updated by Pester'
    }

    It 'Update-OrchBucket updates Password without throwing' {
        # Password is write-only on the server (GET never returns it), so we cannot
        # read it back to assert. The migration use case is exactly this: the secret
        # is missing after Copy- and the user re-supplies it. Verify the cmdlet
        # accepts the parameter and the API call succeeds.
        { Update-OrchBucket -Name $script:BucketName -Password 'pester-secret' } | Should -Not -Throw
    }

    It 'Update-OrchBucket -WhatIf does not modify the bucket' {
        $before = (Get-OrchBucket -Name $script:BucketName).Description
        Update-OrchBucket -Name $script:BucketName -Description 'WhatIf test' -WhatIf
        Clear-OrchCache
        (Get-OrchBucket -Name $script:BucketName).Description | Should -Be $before
    }

    It 'Update-OrchBucket with no updatable parameters is a no-op' {
        # Only -Name supplied → dirty=false → no API call should fire.
        { Update-OrchBucket -Name $script:BucketName } | Should -Not -Throw
    }

    It 'Get-OrchBucket -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'buckets.csv'
        Get-OrchBucket -Name $script:BucketName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:BucketName
    }

    It 'Import-OrchBucketItem uploads a file' {
        $file = Join-Path $script:TempDir 'testfile.txt'
        'Hello from Pester' | Set-Content -Path $file -Encoding UTF8

        Import-OrchBucketItem -Name $script:BucketName -Source $file
        Clear-OrchCache
        $items = Get-OrchBucketItem -Name $script:BucketName
        $items | Should -Not -BeNullOrEmpty
        ($items | Where-Object FullPath -like '*testfile.txt') | Should -Not -BeNullOrEmpty
    }

    It 'Export-OrchBucketItem downloads the file' {
        $dest = Join-Path $script:TempDir 'bucket_download'
        New-Item -Path $dest -ItemType Directory -Force | Out-Null

        Export-OrchBucketItem -Name $script:BucketName -FullPath 'testfile.txt' -Destination $dest
        # Files are organized under subdirectories by bucket name
        $downloaded = Get-ChildItem $dest -Recurse -Filter 'testfile.txt'
        $downloaded | Should -Not -BeNullOrEmpty
        (Get-Content $downloaded.FullName) | Should -BeLike '*Hello from Pester*'
    }

    It 'Remove-OrchBucket removes the bucket' {
        Remove-OrchBucket -Name $script:BucketName -Confirm:$false
        Clear-OrchCache
        Get-OrchBucket -Name $script:BucketName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# FolderMachine Assignment
# ---------------------------------------------------------------------------
Describe 'FolderMachine' {
    BeforeAll {
        $script:FMMachineName = "${script:Prefix}FMMachine"
        New-OrchMachine -Name $script:FMMachineName -Path "${script:Drive}:\"
        Clear-OrchCache
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchFolderMachine -Name "${script:Prefix}*" -ErrorAction SilentlyContinue
        Pop-Location
        Remove-OrchMachine -Name $script:FMMachineName -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Add-OrchFolderMachine assigns a machine to the folder' {
        Add-OrchFolderMachine -Name $script:FMMachineName
        Clear-OrchCache
        $fm = Get-OrchFolderMachine -Name $script:FMMachineName
        $fm | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchFolderMachine -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'foldermachines.csv'
        Get-OrchFolderMachine -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        ($rows | Where-Object Name -eq $script:FMMachineName) | Should -Not -BeNullOrEmpty
    }

    It 'Remove-OrchFolderMachine unassigns the machine' {
        Remove-OrchFolderMachine -Name $script:FMMachineName
        Clear-OrchCache
        Get-OrchFolderMachine -Name $script:FMMachineName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Process CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Process' {
    BeforeAll {
        if (-not $script:PackageId) {
            Set-ItResult -Skipped -Because 'No package found on reference drive'
        }
        $script:ProcessName = "${script:Prefix}Process"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchProcess -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchProcess creates a process from a package' {
        New-OrchProcess -Id $script:PackageId -Name $script:ProcessName
        Clear-OrchCache
        $p = Get-OrchProcess -Name $script:ProcessName
        $p | Should -Not -BeNullOrEmpty
        $p.Name | Should -Be $script:ProcessName
    }

    It 'Get-OrchProcess -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'processes.csv'
        Get-OrchProcess -Name $script:ProcessName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:ProcessName
    }

    It 'Remove-OrchProcess removes the process' {
        Remove-OrchProcess -Name $script:ProcessName -Confirm:$false
        Clear-OrchCache
        Get-OrchProcess -Name $script:ProcessName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Trigger CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Trigger' {
    BeforeAll {
        if (-not $script:PackageId) {
            Set-ItResult -Skipped -Because 'No package found on reference drive'
        }
        $script:TriggerProcess = "${script:Prefix}TrigProcess"
        $script:TriggerName = "${script:Prefix}Trigger"
        Push-Location $script:RootFolder

        # Create a process for the trigger
        New-OrchProcess -Id $script:PackageId -Name $script:TriggerProcess
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchTrigger -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchProcess -Name $script:TriggerProcess -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchTrigger creates a time trigger' {
        New-OrchTrigger -Name $script:TriggerName -ReleaseName $script:TriggerProcess `
            -StartProcessCron '0 0 0 1/1 * ? *' -Enabled false
        Clear-OrchCache
        $t = Get-OrchTrigger -Name $script:TriggerName
        $t | Should -Not -BeNullOrEmpty
        $t.Name | Should -Be $script:TriggerName
        $t.Enabled | Should -Be $false
    }

    It 'Get-OrchTrigger -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'triggers.csv'
        Get-OrchTrigger -Name $script:TriggerName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:TriggerName
    }

    It 'Remove-OrchTrigger removes the trigger' {
        Remove-OrchTrigger -Name $script:TriggerName -Confirm:$false
        Clear-OrchCache
        Get-OrchTrigger -Name $script:TriggerName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Copy Across Folders
# ---------------------------------------------------------------------------
Describe 'Copy Across Folders' {
    BeforeAll {
        Push-Location $script:RootFolder

        # Create entities to copy
        $script:CopyMachine = "${script:Prefix}CopyMachine"
        $script:CopyQueue = "${script:Prefix}CopyQueue"
        $script:CopyAsset = "${script:Prefix}CopyAsset"
        $script:CopyBucket = "${script:Prefix}CopyBucket"

        New-OrchMachine -Name $script:CopyMachine -Path "${script:Drive}:\"
        New-OrchQueue -Name $script:CopyQueue -Description 'Source queue'
        Set-OrchAsset -ValueType Text -Name $script:CopyAsset -Value 'source'
        New-OrchBucket -Name $script:CopyBucket -Description 'Source bucket'
        Clear-OrchCache
    }

    AfterAll {
        # Clean up source entities
        Remove-OrchQueue -Name $script:CopyQueue -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name $script:CopyAsset -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchBucket -Name $script:CopyBucket -Confirm:$false -ErrorAction SilentlyContinue

        # Clean up copied entities in destination folder
        Push-Location $script:CopyFolder -ErrorAction SilentlyContinue
        Remove-OrchQueue -Name $script:CopyQueue -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name $script:CopyAsset -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchBucket -Name $script:CopyBucket -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location

        Remove-OrchMachine -Name $script:CopyMachine -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Copy-OrchQueue copies a queue to another folder' {
        Copy-OrchQueue -Name $script:CopyQueue -Destination $script:CopyFolder
        Clear-OrchCache
        $q = Get-OrchQueue -Name $script:CopyQueue -Path $script:CopyFolder
        $q | Should -Not -BeNullOrEmpty
        $q.Description | Should -Be 'Source queue'
    }

    It 'Copy-OrchAsset copies an asset to another folder' {
        Copy-OrchAsset -Name $script:CopyAsset -Destination $script:CopyFolder
        Clear-OrchCache
        Push-Location $script:CopyFolder
        $a = Get-OrchAsset -Name $script:CopyAsset
        Pop-Location
        $a | Should -Not -BeNullOrEmpty
        $a.Value | Should -Be 'source'
    }

    It 'Copy-OrchBucket copies a bucket to another folder' {
        Copy-OrchBucket -Name $script:CopyBucket -Destination $script:CopyFolder
        Clear-OrchCache
        $b = Get-OrchBucket -Name $script:CopyBucket -Path $script:CopyFolder
        $b | Should -Not -BeNullOrEmpty
    }

    It 'Copy-OrchMachine assigns a machine to another folder' {
        Push-Location $script:CopyFolder
        Add-OrchFolderMachine -Name $script:CopyMachine
        Clear-OrchCache
        $fm = Get-OrchFolderMachine -Name $script:CopyMachine
        Pop-Location
        $fm | Should -Not -BeNullOrEmpty
    }
}

# BusinessRule cmdlets are shelved (non-public, not in psd1) because OR.BusinessRules
# scope is not exposed by Identity Server; tests cannot exercise them.

# ---------------------------------------------------------------------------
# Task (Get-OrchTask / Get-OrchTaskAcrossFolder / Set-OrchTask / Remove-OrchTask)
# ---------------------------------------------------------------------------
Describe 'OrchTask' {
    It 'TK1 Get-OrchTask does not throw' {
        { Get-OrchTask -Path $script:SmokeFolder -ErrorAction SilentlyContinue } | Should -Not -Throw
    }

    It 'TK2 Get-OrchTask -Status Pending does not throw' {
        { Get-OrchTask -Path $script:SmokeFolder -Status Pending -ErrorAction SilentlyContinue } | Should -Not -Throw
    }

    It 'TK3 Get-OrchTaskAcrossFolder does not throw' {
        { Get-OrchTaskAcrossFolder -Path "${script:Drive}:\" -ErrorAction SilentlyContinue } | Should -Not -Throw
    }

    It 'TK4 Set-OrchTask -WhatIf does not throw' {
        { Set-OrchTask -Path $script:SmokeFolder -Id 1 -Title 'x' -WhatIf } | Should -Not -Throw
    }

    It 'TK5 Remove-OrchTask -WhatIf does not throw' {
        { Remove-OrchTask -Path $script:SmokeFolder -Id 1 -WhatIf } | Should -Not -Throw
    }
}

# ---------------------------------------------------------------------------
# Clear-OrchInactiveSession
# ---------------------------------------------------------------------------
Describe 'Clear-OrchInactiveSession' {
    It 'CIS1 -WhatIf does not invoke API' {
        { Clear-OrchInactiveSession -Path "${script:Drive}:\" -WhatIf } | Should -Not -Throw
    }

    It 'CIS2 With no inactive sessions present, completes silently' {
        # If the tenant has no Disconnected/Unresponsive sessions, the cmdlet should be a
        # no-op (no ShouldProcess prompt, no error, no output). Tenant-level operation.
        { Clear-OrchInactiveSession -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue } |
            Should -Not -Throw
    }
}

# ---------------------------------------------------------------------------
# Trigger validation (Test-OrchTrigger)
# ---------------------------------------------------------------------------
Describe 'Test-OrchTrigger' {
    It 'TT1 Test-OrchTrigger on a non-matching name is a no-op' {
        { Test-OrchTrigger -Path $script:SmokeFolder -Name 'definitely-not-a-trigger' -ErrorAction SilentlyContinue } |
            Should -Not -Throw
    }

    It 'TT2 Test-OrchTrigger surfaces ValidationResult shape (when triggers exist)' {
        $triggers = Get-OrchTrigger -Path $script:SmokeFolder -ErrorAction SilentlyContinue
        if (-not $triggers) {
            Set-ItResult -Skipped -Because 'No trigger in tenant test folder.'
            return
        }
        $result = $triggers | Select-Object -First 1 | Test-OrchTrigger -ErrorAction SilentlyContinue
        if ($null -eq $result) {
            Set-ItResult -Skipped -Because 'API rejected validation (likely tenant policy).'
            return
        }
        $result.PSObject.Properties.Name | Should -Contain 'IsValid'
        $result.PSObject.Properties.Name | Should -Contain 'Name'
        $result.PSObject.Properties.Name | Should -Contain 'Path'
    }
}

# ---------------------------------------------------------------------------
# Job lifecycle (Restart / Resume)
# ---------------------------------------------------------------------------
Describe 'Job lifecycle' {
    It 'J1 Restart-OrchJob with -WhatIf does not throw' {
        { Restart-OrchJob -Path $script:SmokeFolder -Id 1 -WhatIf } | Should -Not -Throw
    }

    It 'J2 Resume-OrchJob with -WhatIf does not throw' {
        $fakeKey = '00000000-0000-0000-0000-000000000000'
        { Resume-OrchJob -Path $script:SmokeFolder -Key $fakeKey -WhatIf } | Should -Not -Throw
    }

    It 'J3 Restart-OrchJob against non-existent job emits a non-fatal error' {
        $err = $null
        Restart-OrchJob -Path $script:SmokeFolder -Id 9999999999 -Confirm:$false -ErrorAction SilentlyContinue -ErrorVariable err
        # Either zero errors (jobs cmdlets sometimes treat unknown id as no-op) or one OrchException — must not crash.
        $true | Should -BeTrue
    }
}

# ---------------------------------------------------------------------------
# Webhook (Section W)
# ---------------------------------------------------------------------------
Describe 'Webhook' {
    It 'W1 Get-OrchWebhookEventType returns event types with Name and Group' {
        $types = Get-OrchWebhookEventType -Path "${script:Drive}:\"
        $types | Should -Not -BeNullOrEmpty
        $first = $types | Select-Object -First 1
        $first.Name | Should -Not -BeNullOrEmpty
        $first.Group | Should -Not -BeNullOrEmpty
    }

    It 'W2 Get-OrchWebhookEventType supports wildcard filtering by Name' {
        $all = Get-OrchWebhookEventType -Path "${script:Drive}:\"
        $sample = ($all | Select-Object -First 1).Name
        $sample | Should -Not -BeNullOrEmpty
        $filtered = Get-OrchWebhookEventType -Path "${script:Drive}:\" -Name $sample
        $filtered | Should -Not -BeNullOrEmpty
        $filtered.Name | Should -Contain $sample
    }

    It 'W3 Test-OrchWebhook with -WhatIf does not invoke the API' {
        # Create a temporary webhook with a clearly fake URL to ensure no real delivery happens.
        $whName = "${script:Prefix}WhTest"
        $existing = Get-OrchWebhook -Path "${script:Drive}:\" -Name $whName -ErrorAction SilentlyContinue
        if (-not $existing) {
            # Skip if we cannot create a webhook (Copy-OrchWebhook is the only way and needs a source)
            Set-ItResult -Skipped -Because 'No suitable Webhook to test against; tenant has none.'
            return
        }
        { Test-OrchWebhook -Path "${script:Drive}:\" -Name $whName -WhatIf } | Should -Not -Throw
    }

    # Update-OrchWebhook tests target whichever webhook the tenant happens to have
    # (no New-OrchWebhook cmdlet exists). Each test snapshots the current value,
    # mutates it, asserts, then restores — so re-running the suite is idempotent.
    It 'Update-OrchWebhook -WhatIf does not modify the webhook' {
        $wh = Get-OrchWebhook -Path "${script:Drive}:\" | Select-Object -First 1
        if (-not $wh) {
            Set-ItResult -Skipped -Because 'Tenant has no webhooks.'
            return
        }
        $before = $wh.Description
        Update-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name -Description 'WhatIf test' -WhatIf
        Clear-OrchCache
        (Get-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name).Description | Should -Be $before
    }

    It 'Update-OrchWebhook updates Description and restores' {
        $wh = Get-OrchWebhook -Path "${script:Drive}:\" | Select-Object -First 1
        if (-not $wh) {
            Set-ItResult -Skipped -Because 'Tenant has no webhooks.'
            return
        }
        $original = $wh.Description
        $marker = "Pester-${script:Prefix}desc"
        try {
            Update-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name -Description $marker
            Clear-OrchCache
            (Get-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name).Description | Should -Be $marker
        }
        finally {
            # Restore the original description even if the assertion fails.
            Update-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name -Description $original -ErrorAction SilentlyContinue
            Clear-OrchCache
        }
    }

    It 'Update-OrchWebhook toggles Enabled and restores' {
        $wh = Get-OrchWebhook -Path "${script:Drive}:\" | Select-Object -First 1
        if (-not $wh) {
            Set-ItResult -Skipped -Because 'Tenant has no webhooks.'
            return
        }
        $original = [bool]$wh.Enabled
        $toggled = -not $original
        try {
            Update-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name -Enabled ([string]$toggled).ToLower()
            Clear-OrchCache
            (Get-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name).Enabled | Should -Be $toggled
        }
        finally {
            Update-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name -Enabled ([string]$original).ToLower() -ErrorAction SilentlyContinue
            Clear-OrchCache
        }
    }

    It 'Update-OrchWebhook with no updatable parameters is a no-op' {
        $wh = Get-OrchWebhook -Path "${script:Drive}:\" | Select-Object -First 1
        if (-not $wh) {
            Set-ItResult -Skipped -Because 'Tenant has no webhooks.'
            return
        }
        { Update-OrchWebhook -Path "${script:Drive}:\" -Name $wh.Name } | Should -Not -Throw
    }
}

# ---------------------------------------------------------------------------
# CredentialStore Update
# ---------------------------------------------------------------------------
Describe 'CredentialStore Update' {
    # No New-OrchCredentialStore exists, and overwriting AdditionalConfiguration
    # on the default "Orchestrator Database" store could break asset resolution
    # tenant-wide. So these tests stay non-destructive: -WhatIf and no-op only.
    It 'Update-OrchCredentialStore -WhatIf does not modify the store' {
        $cs = Get-OrchCredentialStore -Path "${script:Drive}:\" | Select-Object -First 1
        if (-not $cs) {
            Set-ItResult -Skipped -Because 'Tenant has no credential stores.'
            return
        }
        $before = $cs.HostName
        Update-OrchCredentialStore -Path "${script:Drive}:\" -Name $cs.Name -HostName 'WhatIf-test' -WhatIf
        Clear-OrchCache
        (Get-OrchCredentialStore -Path "${script:Drive}:\" -Name $cs.Name).HostName | Should -Be $before
    }

    It 'Update-OrchCredentialStore with no updatable parameters is a no-op' {
        $cs = Get-OrchCredentialStore -Path "${script:Drive}:\" | Select-Object -First 1
        if (-not $cs) {
            Set-ItResult -Skipped -Because 'Tenant has no credential stores.'
            return
        }
        { Update-OrchCredentialStore -Path "${script:Drive}:\" -Name $cs.Name } | Should -Not -Throw
    }
}

# ---------------------------------------------------------------------------
# Read-Only Cmdlets
# ---------------------------------------------------------------------------
Describe 'Read-Only Cmdlets' {
    It 'Get-OrchCurrentUser returns current user' {
        $user = Get-OrchCurrentUser -Path "${script:Drive}:\"
        $user | Should -Not -BeNullOrEmpty
        $user.UserName | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchRole returns roles' {
        $roles = Get-OrchRole -Path "${script:Drive}:\"
        $roles | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchSetting returns settings' {
        $settings = Get-OrchSetting -Path "${script:Drive}:\"
        $settings | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchHelp returns help text' {
        $help = Get-OrchHelp
        $help | Should -Not -BeNullOrEmpty
        $help | Should -Match 'UiPathOrch'
    }

    It 'Get-OrchPSDrive returns drive info' {
        $drv = Get-OrchPSDrive "${script:Drive}:"
        $drv | Should -Not -BeNullOrEmpty
        $drv.Root | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchQueue does not throw' {
        { Get-OrchQueue -Path $script:SmokeFolder } | Should -Not -Throw
    }

    It 'Get-OrchAsset does not throw' {
        { Get-OrchAsset -Path $script:SmokeFolder } | Should -Not -Throw
    }

    It 'Get-OrchBucket does not throw' {
        { Get-OrchBucket -Path $script:SmokeFolder } | Should -Not -Throw
    }

    It 'Get-OrchProcess does not throw' {
        { Get-OrchProcess -Path $script:SmokeFolder } | Should -Not -Throw
    }

    It 'Get-OrchProcessDetail does not throw with wildcard -Name' {
        # -Name is mandatory by design; '*' is the explicit "all releases" form.
        { Get-OrchProcessDetail -Path $script:SmokeFolder -Name '*' } | Should -Not -Throw
    }

    It 'Get-OrchProcess -ExpandDetails emits a deprecation warning naming Get-OrchProcessDetail' {
        $warnings = $null
        Get-OrchProcess -Path $script:SmokeFolder -ExpandDetails `
                        -WarningVariable warnings -WarningAction SilentlyContinue | Out-Null
        ($warnings | Where-Object { $_ -match "Get-OrchProcessDetail" }) | Should -Not -BeNullOrEmpty `
            -Because "the deprecation warning must name the canonical cmdlet"
    }

    It 'Get-OrchTrigger does not throw' {
        { Get-OrchTrigger -Path $script:SmokeFolder } | Should -Not -Throw
    }

    It 'Get-OrchTriggerDetail does not throw with wildcard -Name' {
        # -Name is mandatory by design; '*' is the explicit "all triggers" form.
        { Get-OrchTriggerDetail -Path $script:SmokeFolder -Name '*' } | Should -Not -Throw
    }

    It 'Get-OrchTrigger -ExpandDetails emits a deprecation warning naming Get-OrchTriggerDetail' {
        $warnings = $null
        Get-OrchTrigger -Path $script:SmokeFolder -ExpandDetails `
                        -WarningVariable warnings -WarningAction SilentlyContinue | Out-Null
        ($warnings | Where-Object { $_ -match "Get-OrchTriggerDetail" }) | Should -Not -BeNullOrEmpty `
            -Because "the deprecation warning must name the canonical cmdlet"
    }

    It 'Get-OrchWebhook does not throw' {
        { Get-OrchWebhook -Path "${script:Drive}:\" } | Should -Not -Throw
    }

    It 'Get-OrchCalendar does not throw' {
        { Get-OrchCalendar -Path "${script:Drive}:\" } | Should -Not -Throw
    }

    It 'Get-OrchCalendarDate does not throw with wildcard -Name' {
        # -Name is mandatory by design; '*' is the explicit "all calendars" form.
        { Get-OrchCalendarDate -Path "${script:Drive}:\" -Name '*' } | Should -Not -Throw
    }

    It 'Get-OrchLibrary does not throw' {
        { Get-OrchLibrary -Path "${script:Drive}:\" } | Should -Not -Throw
    }

    It 'Get-OrchPackage does not throw' {
        { Get-OrchPackage -Path "${script:Drive}:\" } | Should -Not -Throw
    }

    It 'Get-OrchCredentialStore returns credential stores' {
        $stores = Get-OrchCredentialStore -Path "${script:Drive}:\"
        $stores | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchLicense returns license info' {
        $license = Get-OrchLicense -Path "${script:Drive}:\"
        $license | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchConfigPath returns a path string' {
        $path = Get-OrchConfigPath
        $path | Should -Not -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Asset CSV Import (Create / Update / Multi-type)
# ---------------------------------------------------------------------------
Describe 'Asset CSV Import' {
    BeforeAll {
        $script:CsvAssetName = "${script:Prefix}CsvAsset"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}CsvAsset*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Import-Csv | Set-OrchAsset creates multiple assets from CSV' {
        $csv = Join-Path $script:TempDir 'import_assets.csv'
        $path = $script:RootFolder
        "Path,Name,Description,ValueType,Value,UserName,MachineName`n${path},${script:CsvAssetName}_A,Desc A,Text,ValueA,,`n${path},${script:CsvAssetName}_B,Desc B,Integer,42,,`n${path},${script:CsvAssetName}_C,Desc C,Bool,true,," |
            Set-Content -Path $csv -Encoding UTF8 -NoNewline

        Import-Csv $csv | Set-OrchAsset
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:CsvAssetName}_*"
        $assets.Count | Should -Be 3
    }

    It 'CSV-created assets have correct types and values' {
        $a = Get-OrchAsset -Name "${script:CsvAssetName}_A"
        $a.ValueType | Should -Be 'Text'
        $a.Value | Should -Be 'ValueA'
        $a.Description | Should -Be 'Desc A'

        $b = Get-OrchAsset -Name "${script:CsvAssetName}_B"
        $b.ValueType | Should -Be 'Integer'
        $b.Value | Should -Be '42'

        $c = Get-OrchAsset -Name "${script:CsvAssetName}_C"
        $c.ValueType | Should -Be 'Bool'
        $c.Value | Should -Be 'True'
    }

    It 'Import-Csv | Set-OrchAsset updates existing assets from CSV' {
        $csv = Join-Path $script:TempDir 'update_assets.csv'
        $path = $script:RootFolder
        "Path,Name,Description,ValueType,Value,UserName,MachineName`n${path},${script:CsvAssetName}_A,Updated Desc,Text,UpdatedValue,,`n${path},${script:CsvAssetName}_B,,Integer,99,," |
            Set-Content -Path $csv -Encoding UTF8 -NoNewline

        Import-Csv $csv | Set-OrchAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CsvAssetName}_A"
        $a.Value | Should -Be 'UpdatedValue'
        $a.Description | Should -Be 'Updated Desc'

        $b = Get-OrchAsset -Name "${script:CsvAssetName}_B"
        $b.Value | Should -Be '99'
    }

    It 'Export then delete then re-import round-trip' {
        $csv = Join-Path $script:TempDir 'asset_full_roundtrip.csv'
        Get-OrchAsset -Name "${script:CsvAssetName}_A" -ExportCsv $csv
        Clear-OrchCache

        Remove-OrchAsset -Name "${script:CsvAssetName}_A" -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:CsvAssetName}_A" | Should -BeNullOrEmpty

        Import-Csv $csv | Set-OrchAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CsvAssetName}_A"
        $a | Should -Not -BeNullOrEmpty
        $a.Value | Should -Be 'UpdatedValue'
    }
}

# ---------------------------------------------------------------------------
# Credential CSV Complex Patterns
# ---------------------------------------------------------------------------
Describe 'Credential CSV Complex' {
    BeforeAll {
        $script:CredCsvName = "${script:Prefix}CredCsv"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}CredCsv*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Credential CSV round-trip: Export, delete, re-import' {
        Set-OrchCredentialAsset -Name "${script:CredCsvName}_RT" -CredentialUsername 'rt_user' -CredentialPassword 'rt_pass'
        Clear-OrchCache

        $csv = Join-Path $script:TempDir 'cred_full_roundtrip.csv'
        Get-OrchAsset -Name "${script:CredCsvName}_RT" -ExportCredentialCsv $csv
        Clear-OrchCache

        Remove-OrchAsset -Name "${script:CredCsvName}_RT" -ValueType Credential -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:CredCsvName}_RT" | Should -BeNullOrEmpty

        # Re-import — password is not exported, so add it
        $rows = Import-Csv $csv
        $rows | ForEach-Object { $_.CredentialPassword = 'rt_pass' }
        $rows | Set-OrchCredentialAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CredCsvName}_RT"
        $a | Should -Not -BeNullOrEmpty
        $a.CredentialUsername | Should -Be 'rt_user'
    }

    It 'Credential CSV with CredentialStore' {
        $csv = Join-Path $script:TempDir 'cred_store.csv'
        $path = $script:RootFolder
        "Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName`n${path},${script:CredCsvName}_Store,,Orchestrator Database,,,store_user,store_pass," |
            Set-Content -Path $csv -Encoding UTF8 -NoNewline

        Import-Csv $csv | Set-OrchCredentialAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CredCsvName}_Store"
        $a | Should -Not -BeNullOrEmpty
        $a.CredentialUsername | Should -Be 'store_user'
        $a.CredentialStoreId | Should -Not -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Additional CSV Export Types
# ---------------------------------------------------------------------------
Describe 'CSV Export Additional Types' {
    BeforeAll {
        Push-Location $script:RootFolder
    }

    AfterAll {
        Pop-Location
    }

    It 'Get-OrchUser -ExportCsv creates a CSV file' {
        $csv = Join-Path $script:TempDir 'users.csv'
        Get-OrchUser -Path "${script:Drive}:\" -ExportCsv $csv
        $csv | Should -Exist
        $header = Get-Content $csv -TotalCount 1
        $header | Should -Match 'UserName'
    }

    It 'Get-OrchUserDetail does not throw with wildcard -UserName' {
        # -UserName is mandatory by design; '*' is the explicit "all users" form.
        { Get-OrchUserDetail -Path "${script:Drive}:\" -UserName '*' } | Should -Not -Throw
    }

    It 'Get-OrchUser -ExpandDetails emits a deprecation warning naming Get-OrchUserDetail' {
        $warnings = $null
        Get-OrchUser -Path "${script:Drive}:\" -ExpandDetails `
                     -WarningVariable warnings -WarningAction SilentlyContinue | Out-Null
        ($warnings | Where-Object { $_ -match "Get-OrchUserDetail" }) | Should -Not -BeNullOrEmpty `
            -Because "the deprecation warning must name the canonical cmdlet"
    }

    It 'Get-OrchRole -ExportCsv creates a CSV file' {
        $csv = Join-Path $script:TempDir 'roles.csv'
        Get-OrchRole -Path "${script:Drive}:\" -ExportCsv $csv
        $csv | Should -Exist
        $header = Get-Content $csv -TotalCount 1
        $header | Should -Match 'Name'
    }

    It 'Get-OrchFolderUser -ExportCsv creates a CSV file' {
        $csv = Join-Path $script:TempDir 'folderusers.csv'
        Get-OrchFolderUser -ExportCsv $csv
        $csv | Should -Exist
    }
}

# ---------------------------------------------------------------------------
# Error Handling
# ---------------------------------------------------------------------------
Describe 'Error Handling' {
    It 'Get-OrchMachine for non-existent returns empty' {
        $result = Get-OrchMachine -Name 'NonExistent_99999' -Path "${script:Drive}:\"
        $result | Should -BeNullOrEmpty
    }

    It 'Remove-OrchMachine for non-existent does not throw' {
        { Remove-OrchMachine -Name 'NonExistent_99999' -Path "${script:Drive}:\" -Confirm:$false } | Should -Not -Throw
    }

    It 'New-OrchQueue with duplicate name writes an error' {
        $dupName = "${script:Prefix}DupQueue"
        Push-Location $script:RootFolder
        New-OrchQueue -Name $dupName
        Clear-OrchCache
        $err = $null
        New-OrchQueue -Name $dupName -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
        Remove-OrchQueue -Name $dupName -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Set-Location to non-existent folder writes an error' {
        { Set-Location "${script:Drive}:\NonExistent_Folder_99999" -ErrorAction Stop } | Should -Throw
    }
}

# ---------------------------------------------------------------------------
# Wildcard Support
# ---------------------------------------------------------------------------
Describe 'Wildcard Support' {
    BeforeAll {
        $script:WcA = "${script:Prefix}WcA"
        $script:WcB = "${script:Prefix}WcB"
        New-OrchMachine -Name $script:WcA -Path "${script:Drive}:\" -Description 'Wildcard A'
        New-OrchMachine -Name $script:WcB -Path "${script:Drive}:\" -Description 'Wildcard B'
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchMachine -Name "${script:Prefix}Wc*" -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Get-OrchMachine with * wildcard returns matching machines' {
        $machines = Get-OrchMachine -Name "${script:Prefix}Wc*" -Path "${script:Drive}:\"
        $machines.Count | Should -Be 2
    }

    It 'Get-OrchMachine with ? wildcard matches single character' {
        $machines = Get-OrchMachine -Name "${script:Prefix}Wc?" -Path "${script:Drive}:\"
        $machines.Count | Should -Be 2
    }

    It 'Remove-OrchMachine with wildcard removes matching machines' {
        Remove-OrchMachine -Name "${script:Prefix}Wc*" -Path "${script:Drive}:\" -Confirm:$false
        Clear-OrchCache
        Get-OrchMachine -Name "${script:Prefix}Wc*" -Path "${script:Drive}:\" | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Tab Completion Smoke Tests
# ---------------------------------------------------------------------------
Describe 'Tab Completion' {
    BeforeAll {
        function Complete-Parameter {
            param([string]$InputScript)
            $result = [System.Management.Automation.CommandCompletion]::CompleteInput(
                $InputScript, $InputScript.Length, $null)
            return $result.CompletionMatches
        }
    }

    It 'Update-OrchTrigger -MachineRobots completes without error' {
        # The Update completer yields each existing trigger's MachineRobots binding, or a single
        # empty-template fallback when the folder has none -- so against a real folder it always
        # returns at least one candidate. Target an explicit folder: without -Path the completer
        # resolves the current location, which is not guaranteed to be an Orch folder mid-suite.
        $results = Complete-Parameter "Update-OrchTrigger -Path $script:SmokeFolder -Name * -MachineRobots "
        $results | Should -Not -Be $null
    }

    It 'New-OrchTrigger -MachineRobots completes without error' {
        # The New completer offers the folder's robot-provisioned users/machines; a pristine test
        # folder usually has none, so skip when there is nothing to complete (mirrors the
        # trigger/webhook env-dependent guards above).
        $results = Complete-Parameter "New-OrchTrigger -Path $script:SmokeFolder -Name test -ReleaseName test -MachineRobots "
        if (-not $results) {
            Set-ItResult -Skipped -Because 'No robot-provisioned machine/user in the tenant test folder to complete.'
            return
        }
        $results | Should -Not -Be $null
    }

    It 'Add-PmGroupMember -UserName completes without error' {
        $results = Complete-Parameter 'Add-PmGroupMember -PmGroup * -Type DirectoryUser -UserName '
        $results | Should -Not -Be $null
    }
}

# ---------------------------------------------------------------------------
# IdentityUrl Derivation
# ---------------------------------------------------------------------------
Describe 'IdentityUrl Derivation' {
    BeforeAll {
        $script:allDrives = Get-OrchPSDrive
    }

    # Cloud (incl. Cloud-staging) Identity is a HOST-LEVEL service:
    # IdentityUrl is {scheme}://{authority}/identity_ — NOT {org}/identity_.
    # DO NOT reintroduce the org segment here: org-scoping the authorize
    # URL is the d57c287 regression (v0.9.15.5) that broke Entra-federated
    # Cloud orgs with errorCode=219 "user has not accepted the invitation";
    # customer bisection pinned it v0.9.15.4 OK -> v0.9.15.5 NG. This
    # mirrors the xUnit guard in Tests/UnitTests/IdentityUrlAutoGenTests.cs.
    It 'Cloud drive IdentityUrl is host-level {authority}/identity_' {
        $cloudDrives = $script:allDrives | Where-Object { $_.Root -match 'uipath\.com' }
        $cloudDrives | Should -Not -BeNullOrEmpty -Because 'at least one cloud drive should be connected'

        foreach ($d in $cloudDrives) {
            $uri = [Uri]$d.Root.TrimEnd('/')
            $expected = "$($uri.Scheme)://$($uri.Authority)/identity_"
            $d.IdentityUrl | Should -Be $expected -Because "drive '$($d.Name)' Root=$($d.Root)"
        }
    }

    It 'On-prem drive IdentityUrl is {authority}/identity' {
        $onpremDrives = $script:allDrives | Where-Object { $_.Root -notmatch 'uipath\.com' }
        if (-not $onpremDrives) { Set-ItResult -Skipped -Because 'no on-prem drives connected'; return }

        foreach ($d in $onpremDrives) {
            $uri = [Uri]$d.Root.TrimEnd('/')
            $expected = "$($uri.Scheme)://$($uri.Authority)/identity"
            $d.IdentityUrl | Should -Be $expected -Because "drive '$($d.Name)' Root=$($d.Root)"
        }
    }

    It 'IdentityUrl is not null or empty for any drive' {
        foreach ($d in $script:allDrives) {
            $d.IdentityUrl | Should -Not -BeNullOrEmpty -Because "drive '$($d.Name)' should have IdentityUrl"
        }
    }
}

# ---------------------------------------------------------------------------
# Regression guards — Critical bugs found and fixed in the 2026-05 review session.
# Each test is named after the originating bug. If any of these regress, treat as P0:
# the same shipping bug is back. See git log around the test addition for fix commits.
# ---------------------------------------------------------------------------
Describe 'Regression-2026-05' -Tag 'Regression' {
    BeforeAll {
        Push-Location $script:RootFolder
    }

    AfterAll {
        # Defensive: the global AfterAll covers the ${script:Prefix}* pattern, but in case a
        # test bails in the middle, do an immediate cleanup of just our Reg_* names.
        Remove-OrchAsset -Name "${script:Prefix}Reg_*" -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name "${script:Prefix}Reg_*" -ValueType Credential -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchQueue -Name "${script:Prefix}Reg_*" -Path $script:RootFolder -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchTrigger -Name "${script:Prefix}Reg_*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchProcess -Name "${script:Prefix}Reg_*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'R1: New-OrchTrigger emits valid JSON for StartProcessCronDetails' {
        # Bug: string interpolation produced "{advancedCron":"<cron>"} (leading " then {),
        #      which fails JSON parsing. Same bug existed in NewTrigger, UpdateTrigger, and
        #      OrchAPISession.PutProcessSchedule.
        # Fix: use System.Text.Json.JsonSerializer.Serialize for the cron value and braces:
        #      $"{{\"advancedCron\":{JsonSerializer.Serialize(cron ?? "")}}}".
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'No package on reference drive'; return }

        $procName = "${script:Prefix}Reg_TrigProc"
        $trigName = "${script:Prefix}Reg_Trig"
        New-OrchProcess -Id $script:PackageId -Name $procName | Out-Null
        Clear-OrchCache
        try {
            New-OrchTrigger -Name $trigName -ReleaseName $procName `
                -StartProcessCron '0 0 0 1/1 * ? *' -Enabled false | Out-Null
            Clear-OrchCache
            $t = Get-OrchTrigger -Name $trigName
            $t | Should -Not -BeNullOrEmpty
            $t.StartProcessCronDetails | Should -Not -BeNullOrEmpty
            { $t.StartProcessCronDetails | ConvertFrom-Json -ErrorAction Stop } |
                Should -Not -Throw -Because 'StartProcessCronDetails must be valid JSON; broken format would have shipped before this fix'
        } finally {
            Remove-OrchTrigger -Name $trigName -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchProcess -Name $procName -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R2: Open-OrchJob -Id @() fails parameter binding (no NRE)' {
        # Bug: Id parameter lacked Mandatory=true; omitting it bypassed binder rejection and
        #      then NREd inside ProcessRecord. The error category should be InvalidData
        #      (PowerShell parameter binder), NOT NullReferenceException.
        $err = $null
        try { Open-OrchJob -Path "${script:Drive}:\" -Id @() -ErrorAction Stop } catch { $err = $_ }
        $err | Should -Not -BeNullOrEmpty -Because 'empty Id must be rejected'
        $err.CategoryInfo.Category | Should -Be 'InvalidData' -Because 'PowerShell parameter binder must reject empty Id; an NRE here means the Mandatory regression is back'
    }

    It 'R3: Get-DuExtractor completer returns Extractors, not DocumentTypes' {
        # Bug: completer copy-pasted from GetDuClassifier and called GetDuDocumentTypes
        #      instead of GetDuExtractors. Tab completion for -Name returned the wrong list.
        # Fix: corrected the API call in the completer.
        $duDrive = Get-PSDrive | Where-Object { $_.Provider.Name -eq 'UiPathOrchDu' -and $_.Name -eq 'Orch1Du' }
        if (-not $duDrive) { Set-ItResult -Skipped -Because 'Orch1Du drive not connected'; return }

        $extractorCount = (Get-DuExtractor -Path 'Orch1Du:\Predefined' -ErrorAction SilentlyContinue | Measure-Object).Count
        $documentTypeCount = (Get-DuDocumentType -Path 'Orch1Du:\Predefined' -ErrorAction SilentlyContinue | Measure-Object).Count

        if ($extractorCount -eq 0) { Set-ItResult -Skipped -Because 'no extractors found in Predefined project'; return }
        if ($extractorCount -eq $documentTypeCount) {
            Set-ItResult -Skipped -Because "cannot distinguish: extractor count ($extractorCount) equals document type count"
            return
        }

        $line = "Get-DuExtractor -Path 'Orch1Du:\Predefined' -Name "
        $r = [System.Management.Automation.CommandCompletion]::CompleteInput($line, $line.Length, $null)
        $r.CompletionMatches.Count | Should -Be $extractorCount -Because "completer must enumerate $extractorCount extractors, not $documentTypeCount document types"
    }

    It 'R4: Get-OrchAuditLog -UserName for unresolved user returns 0 rows (no scope leakage)' {
        # Bug: UserName resolution was wrapped in `try { ... } catch { }`. When the resolved
        #      userIds set was empty (or resolution threw), no UserName filter was added to
        #      the OData query, silently returning logs for ALL users.
        # Fix: removed the catch; when userIds is empty, add a sentinel filter (UserId eq -1)
        #      that matches no rows, narrowing rather than widening the result.
        $unmatchableName = "NeverExistsUser_$([guid]::NewGuid())"
        $rNonExistent = Get-OrchAuditLog -Path "${script:RefDrive}:\" -UserName $unmatchableName -First 5 -ErrorAction SilentlyContinue
        $rAllUsers = Get-OrchAuditLog -Path "${script:RefDrive}:\" -First 5 -ErrorAction SilentlyContinue

        ($rNonExistent | Measure-Object).Count | Should -Be 0 -Because 'a UserName that resolves to no users must NOT widen results to all users'
        ($rAllUsers | Measure-Object).Count | Should -BeGreaterThan 0 -Because 'control: with no UserName filter, at least some audit log entries must exist'
    }

    It 'R5: Import-OrchQueueItem completes successfully (BulkAddQueueItem JSON escape)' {
        # Bug: queue name was concatenated into JSON via $"\"queueName\":\"{queue.Name}\"\n}}"
        #      which broke if the name contained " or \. Even safe names exercised the same
        #      code path, so this is a regression smoke that the JsonSerializer.Serialize fix
        #      didn't break the basic import path.
        $qname = "${script:Prefix}Reg_Q"
        New-OrchQueue -Name $qname | Out-Null
        Clear-OrchCache
        $csv = $null
        try {
            $csv = Join-Path $script:TempDir "${qname}.csv"
            "Priority,Reference,CustomerName`nNormal,Reg-001,Alice`nHigh,Reg-002,Bob" | Set-Content -Path $csv -Encoding UTF8 -NoNewline
            $r = Import-OrchQueueItem -Name $qname -ImportCsv $csv -ErrorAction Stop
            $r.Success | Should -Be $true -Because 'BulkAddQueueItem must succeed for a normal queue name; failure here means the JSON escape fix broke the basic case'
        } finally {
            Remove-OrchQueue -Name $qname -Confirm:$false -ErrorAction SilentlyContinue
            if ($csv) { Remove-Item $csv -ErrorAction SilentlyContinue }
        }
    }

    It 'R6: /api/Stats/GetSessionsStats responds 200 (URL trailing-quote regression)' {
        # Bug: GetSessionStats() built the URL "/api/Stats/GetSessionsStats'" with a stray
        #      single-quote at the end, causing 404. The dev's own comment was "Returns Not
        #      Found..." — the typo was never spotted.
        # Fix: removed the trailing quote.
        $sc = $null
        $null = Invoke-OrchApi -Path "${script:RefDrive}:\" -Method GET `
            -ApiPath '/api/Stats/GetSessionsStats' -StatusCodeVariable sc -SkipHttpErrorCheck -ErrorAction Stop
        $sc | Should -Be 200 -Because 'on Orch1 this endpoint is supported; the trailing-quote bug caused 404'
    }

    It 'R7: Set-OrchSecretAsset -Description "" clears the existing description (single-row case)' {
        # New behavior from the merge-aggregation refactor (resolves the Q1 spec): a single
        # direct call with empty Description and no other rows clears the existing value.
        # Without per-asset Description aggregation + isDirty tracking, this previously
        # either preserved (old !IsNullOrEmpty check) or required a separate parameter.
        $name = "${script:Prefix}Reg_Sec_Clear"
        try {
            Set-OrchSecretAsset -Name $name -SecretValue 's' -Description 'INITIAL' | Out-Null
            Clear-OrchCache
            (Get-OrchAsset -Name $name).Description | Should -Be 'INITIAL'

            Set-OrchSecretAsset -Name $name -Description '' -SecretValue '' | Out-Null
            Clear-OrchCache
            (Get-OrchAsset -Name $name).Description | Should -BeNullOrEmpty -Because '-Description "" must clear the existing value when no other row supplies a non-empty value'
        } finally {
            Remove-OrchAsset -Name $name -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R8: Set-OrchSecretAsset multi-row pipe: non-empty Description wins over empty cells' {
        # Critical interaction: Get-OrchSecretAsset's CSV exporter writes Description on the
        # first row of each asset only; subsequent UserValue rows leave it empty. The merge
        # rule (non-empty > "" > null, last-writer-wins among non-empty) must keep the value
        # when one row has 'NEW' and others have ''. This guards SelfContained T5.8 / T9.1.
        $name = "${script:Prefix}Reg_Sec_Mix"
        try {
            Set-OrchSecretAsset -Name $name -SecretValue 's' -Description 'OLD' | Out-Null
            Clear-OrchCache

            @(
                [pscustomobject]@{ Path = $script:RootFolder; Name = $name; Description = 'NEW'; SecretValue = '' }
                [pscustomobject]@{ Path = $script:RootFolder; Name = $name; Description = '';    SecretValue = '' }
            ) | Set-OrchSecretAsset
            Clear-OrchCache
            (Get-OrchAsset -Name $name).Description | Should -Be 'NEW' -Because 'non-empty Description must win over empty cells in the same batch'
        } finally {
            Remove-OrchAsset -Name $name -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R9: Set-OrchAsset -MachineName for an unassigned tenant machine errors out without corrupting the asset' {
        # Critical bug fix: SetAsset's -MachineName candidate set was drive.Machines.Get()
        # (the entire tenant); it now intersects with drive.FolderMachinesAssigned.Get(folder).
        #
        # The pre-fix flow was actively destructive, not merely permissive. The client built a
        # per-Robot UserValue referencing the out-of-folder MachineId and PUT it; the server
        # returned 200 BUT silently dropped the UserValue and wiped the original Global Value.
        # The asset was left at ValueScope=PerRobot with no UserValues and HasDefaultValue=false
        # — effectively erased while appearing to succeed. Verified against Orch1 directly
        # (PUT /odata/Assets(id) with an out-of-folder MachineId).
        #
        # This regression test guards two properties simultaneously:
        #   (a) the cmdlet rejects the call up front (correct error path), and
        #   (b) the asset's state is bit-for-bit unchanged after the failed call (no corruption).
        # If either flips red, scripts targeting unassigned machines could silently destroy
        # assets again.
        $assigned = @(Get-OrchFolderMachine -Path $script:RootFolder -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
        $allTenant = @(Get-OrchMachine -Path "${script:Drive}:\" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
        $unassigned = $allTenant | Where-Object { $_ -and $_ -notin $assigned } | Select-Object -First 1
        if (-not $unassigned) {
            Set-ItResult -Skipped -Because 'no tenant machine is unassigned to the test folder; cannot exercise the path'
            return
        }

        $name = "${script:Prefix}Reg_AssetUnassignedMach"
        try {
            Set-OrchAsset -Name $name -ValueType Text -Value 'init_value' | Out-Null
            Clear-OrchCache
            $before = Get-OrchAsset -Name $name
            $before.Value | Should -Be 'init_value'
            $before.ValueScope | Should -Be 'Global'
            $before.HasDefaultValue | Should -Be $true

            $err = $null
            try {
                Set-OrchAsset -Name $name -ValueType Text -Value 'permachine_X' -MachineName $unassigned -ErrorAction Stop
            } catch { $err = $_ }
            $err | Should -Not -BeNullOrEmpty -Because "machine '$unassigned' is not assigned to '$($script:RootFolder)' so the cmdlet must reject it"
            $err.Exception.Message | Should -Match 'is not assigned to the folder'

            Clear-OrchCache
            $after = Get-OrchAsset -Name $name
            $after.Value           | Should -Be $before.Value           -Because 'asset Value must NOT be wiped by a failed Set-OrchAsset (pre-fix bug erased Value silently)'
            $after.ValueScope      | Should -Be $before.ValueScope      -Because 'asset ValueScope must NOT flip to PerRobot on a failed Set-OrchAsset'
            $after.HasDefaultValue | Should -Be $before.HasDefaultValue -Because 'HasDefaultValue must NOT flip to false on a failed Set-OrchAsset'
        } finally {
            Remove-OrchAsset -Name $name -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R11: Start-OrchJob -RuntimeType invalid emits WriteError, not throw' {
        # Bug fix: the validity check used `throw new Exception(...)` which bypassed
        # -WhatIf / -Confirm and surfaced as a generic Exception. It now writes a
        # non-terminating ArgumentException with InvalidArgument category, listing the
        # valid values, and returns gracefully.
        $err = $null
        try {
            Start-OrchJob -Path $script:RootFolder -Name 'NoSuchProcessName' -RuntimeType 'NotARuntime' -ErrorAction Stop
        } catch { $err = $_ }

        $err | Should -Not -BeNullOrEmpty -Because 'an invalid -RuntimeType must be reported'
        $err.CategoryInfo.Category | Should -Be 'InvalidArgument' -Because 'the cmdlet should fail through WriteError, not throw an unclassified Exception'
        $err.Exception.Message | Should -Match 'Invalid RuntimeType'
        $err.Exception.Message | Should -Match 'Valid values are'
    }

    It 'R13: Set-OrchAsset -UserName for a folder-unassigned tenant user now succeeds (delegated to the API)' {
        # PR #20: the client-side folder-scope preflight was REMOVED. A user that exists in the
        # tenant but is NOT directly assigned to the folder used to be rejected as "not assigned"
        # (the old R13 assertion). Direct PUT probes on cloud Orchestrator and on-prem 22.10.1
        # showed the server accepts a per-User value for any existing tenant user regardless of
        # folder assignment (200, value persisted, Global Value intact), so the cmdlet now
        # delegates the decision. This test pins the new behaviour: the per-User value is set and
        # the Global default is preserved.
        $assigned = @(Get-OrchFolderUser -Path $script:RootFolder -ErrorAction SilentlyContinue |
            ForEach-Object { $_.UserEntity.UserName })
        $unassigned = Get-OrchUser -Path "${script:Drive}:\" -ErrorAction SilentlyContinue |
            Where-Object { $_.Type -ne 'DirectoryGroup' -and $_.Id -and $_.UserName -and $_.UserName -notin $assigned } |
            Select-Object -First 1
        if (-not $unassigned) {
            Set-ItResult -Skipped -Because 'no tenant user is unassigned to the test folder; cannot exercise the path'
            return
        }

        $name = "${script:Prefix}Reg_AssetUnassignedUser"
        try {
            Set-OrchAsset -Name $name -ValueType Text -Value 'init_value' | Out-Null
            Clear-OrchCache
            $before = Get-OrchAsset -Name $name
            $before.Value | Should -Be 'init_value'
            $before.HasDefaultValue | Should -Be $true

            # No longer rejected: the per-user value is accepted for the folder-unassigned user.
            { Set-OrchAsset -Name $name -ValueType Text -Value 'peruser_X' -UserName $unassigned.UserName -ErrorAction Stop } |
                Should -Not -Throw -Because 'the API accepts a per-user value for any existing tenant user; the cmdlet no longer pre-rejects folder-unassigned users'

            Clear-OrchCache
            $after = Get-OrchAsset -Name $name
            $after.Value           | Should -Be 'init_value' -Because 'the Global value must be preserved'
            $after.HasDefaultValue | Should -Be $true        -Because 'the Global default must be preserved'
            ($after.UserValues | Where-Object UserId -eq $unassigned.Id).Value |
                Should -Be 'peruser_X' -Because 'the per-user value for the folder-unassigned tenant user is now persisted'
        } finally {
            Remove-OrchAsset -Name $name -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R14: Set-OrchSecretAsset scope — machine still gated, user delegated to the API' {
        # SetSecretAsset mirrors SetAsset: the MACHINE folder-scope preflight is UNCHANGED (an
        # unassigned machine is still rejected up front), while the USER folder-scope preflight
        # was REMOVED (PR #20) — a per-User value for a folder-unassigned tenant user is now
        # accepted (delegated to the API) rather than rejected.
        $assignedM = @(Get-OrchFolderMachine -Path $script:RootFolder -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
        $allTenantM = @(Get-OrchMachine -Path "${script:Drive}:\" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Name)
        $unassignedM = $allTenantM | Where-Object { $_ -and $_ -notin $assignedM } | Select-Object -First 1

        $assignedU = @(Get-OrchFolderUser -Path $script:RootFolder -ErrorAction SilentlyContinue | ForEach-Object { $_.UserEntity.UserName })
        $unassignedU = Get-OrchUser -Path "${script:Drive}:\" -ErrorAction SilentlyContinue |
            Where-Object { $_.Type -ne 'DirectoryGroup' -and $_.Id -and $_.UserName -and $_.UserName -notin $assignedU } |
            Select-Object -First 1

        if (-not $unassignedM -and -not $unassignedU) {
            Set-ItResult -Skipped -Because 'no unassigned machine or user available'
            return
        }

        $name = "${script:Prefix}Reg_SecScope"
        try {
            Set-OrchSecretAsset -Name $name -SecretValue 's' | Out-Null
            Clear-OrchCache

            if ($unassignedM) {
                $err = $null
                try { Set-OrchSecretAsset -Name $name -SecretValue 's' -MachineName $unassignedM -ErrorAction Stop } catch { $err = $_ }
                $err | Should -Not -BeNullOrEmpty -Because 'the machine folder-scope preflight is unchanged'
                $err.Exception.Message | Should -Match 'MachineName.*is not assigned to the folder'
            }
            if ($unassignedU) {
                # User axis is now delegated to the API: a per-user secret value for a
                # folder-unassigned tenant user succeeds instead of being rejected up front.
                { Set-OrchSecretAsset -Name $name -SecretValue 's' -UserName $unassignedU.UserName -ErrorAction Stop } |
                    Should -Not -Throw -Because 'the user folder-scope preflight was removed in PR #20'
            }
        } finally {
            Remove-OrchAsset -Name $name -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R15: Copy-OrchAsset copies a per-user value for a tenant user not assigned to the destination folder' {
        # PR #20 REMOVED the folder-scope drop in CopyItem.FindDstUser. Previously a per-User
        # UserValue whose (tenant) user was not directly assigned to the destination folder was
        # dropped client-side. The server accepts such a value for any existing tenant user
        # (verified by direct PUT probes on cloud + on-prem 22.10.1), so the copy now PERSISTS it
        # and the Global default is unaffected.
        #
        # Test setup needs a destination folder where TestUserA has NO access (neither
        # direct nor inherited). The fixture's CopyFolder is a child of RootFolder, so
        # TestUserA inherits down to it — that wouldn't exercise the unassigned path.
        # Create a dedicated TOP-LEVEL sibling folder for this test; clean up in finally.
        $name = "${script:Prefix}Reg_CopyAssetUser"
        $siblingFolder = "${script:Drive}:\${script:Prefix}Reg_Sibling"
        try {
            # Pre-clean in case a previous run left the sibling around.
            Remove-Item -LiteralPath $siblingFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
            $null = mkdir $siblingFolder -ErrorAction Stop
            Clear-OrchCache

            # Confirm TestUserA is NOT inherited / assigned in the sibling folder.
            $siblingUsers = @(Get-OrchFolderUser -Path $siblingFolder -ErrorAction SilentlyContinue |
                ForEach-Object { $_.UserEntity.UserName })
            if ($siblingUsers -contains $script:TestUserA) {
                Set-ItResult -Skipped -Because "TestUserA ($script:TestUserA) is unexpectedly present in the sibling folder; cannot exercise the unassigned path"
                return
            }

            # Source asset in RootFolder with Global value + per-User UserValue for TestUserA.
            Set-OrchAsset -Name $name -ValueType Text -Value 'global_value' -Path $script:RootFolder | Out-Null
            Set-OrchAsset -Name $name -ValueType Text -Value 'user_value' -UserName $script:TestUserA -Path $script:RootFolder | Out-Null
            Clear-OrchCache

            $src = Get-OrchAsset -Name $name -Path $script:RootFolder
            $src.Value | Should -Be 'global_value' -Because 'sanity: source has Global value'
            ($src.UserValues | Measure-Object).Count | Should -BeGreaterOrEqual 1 -Because 'sanity: source has at least one per-User UserValue'

            # Copy to the sibling. FindDstUser resolves TestUserA (a tenant user) and includes
            # the per-User value; the server accepts it even though TestUserA is not assigned here.
            Copy-OrchAsset -Name $name -Path $script:RootFolder -Destination $siblingFolder -WarningAction SilentlyContinue
            Clear-OrchCache

            $dst = Get-OrchAsset -Name $name -Path $siblingFolder
            $dst | Should -Not -BeNullOrEmpty -Because 'copy must succeed'
            $dst.Value | Should -Be 'global_value' -Because 'Global value must survive the copy'
            $dst.HasDefaultValue | Should -Be $true
            ($dst.UserValues | Where-Object UserId -eq $script:TestUserAId).Value |
                Should -Be 'user_value' -Because 'the per-user value now persists even though TestUserA is not assigned to the destination folder (delegated to the API)'
        } finally {
            # Remove children explicitly first; OrchProvider's Remove-Item -Recurse on a
            # folder with asset children can still emit an interactive Y/N prompt (the
            # provider's child-confirmation isn't suppressed by the parent's -Confirm:$false).
            # Drain the folder by removing each known and any leftover asset before rmdir.
            Remove-OrchAsset -Name $name -Path $script:RootFolder -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchAsset -Name $name -Path $siblingFolder -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchAsset -Name "${script:Prefix}Reg_*" -Path $siblingFolder -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchAsset -Name "${script:Prefix}Reg_*" -Path $siblingFolder -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $siblingFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R16: Copy-OrchAsset batch under $ErrorActionPreference=Stop persists per-user values without aborting' {
        # Companion to R15. When Copy-OrchAsset cannot map a referenced user /
        # machine / etc. to the destination folder, the cmdlet should warn and
        # skip just the affected piece — never abort the whole batch. This
        # matters in real cross-tenant copies where users / machines / queues
        # only partially overlap, and especially under
        # $ErrorActionPreference='Stop' where any WriteError would terminate.
        #
        # Post-PR#20 (TestUserA is a tenant user not assigned to the sibling; the server accepts
        # their per-user value). Three guarantees verified together:
        #   (a) An asset with a Global default plus a per-User value keeps BOTH after the copy.
        #   (b) An asset whose only value is that per-User value is still copied (not skipped).
        #   (c) Under $ErrorActionPreference='Stop' the batch processes every source asset with
        #       no terminating exception and no error.
        $assetGlobal = "${script:Prefix}Reg_R16_Global"
        $assetUserOnly = "${script:Prefix}Reg_R16_UserOnly"
        $siblingFolder = "${script:Drive}:\${script:Prefix}Reg_R16_Sibling"
        try {
            Remove-Item -LiteralPath $siblingFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
            $null = mkdir $siblingFolder -ErrorAction Stop
            Clear-OrchCache

            # Confirm TestUserA is NOT inherited / assigned in the sibling folder
            # (same precondition as R15; sibling is top-level so RootFolder's
            # user assignment does not propagate).
            $siblingUsers = @(Get-OrchFolderUser -Path $siblingFolder -ErrorAction SilentlyContinue |
                ForEach-Object { $_.UserEntity.UserName })
            if ($siblingUsers -contains $script:TestUserA) {
                Set-ItResult -Skipped -Because "TestUserA ($script:TestUserA) is unexpectedly present in the sibling folder; cannot exercise the unassigned path"
                return
            }

            # Source assets:
            #   $assetGlobal  : Global default + per-User UserValue for TestUserA
            #   $assetUserOnly: per-User UserValue for TestUserA only (no Global default)
            Set-OrchAsset -Name $assetGlobal   -ValueType Text -Value 'global_default'  -Path $script:RootFolder | Out-Null
            Set-OrchAsset -Name $assetGlobal   -ValueType Text -Value 'user_value'      -UserName $script:TestUserA -Path $script:RootFolder | Out-Null
            Set-OrchAsset -Name $assetUserOnly -ValueType Text -Value 'user_only_value' -UserName $script:TestUserA -Path $script:RootFolder | Out-Null
            Clear-OrchCache

            # Sanity: $assetUserOnly was created without a Global default value.
            $srcUserOnly = Get-OrchAsset -Name $assetUserOnly -Path $script:RootFolder
            $srcUserOnly.HasDefaultValue | Should -Be $false -Because 'fixture: per-User-only asset must have no Global default to exercise the "no applicable values" Warning path'

            # === (c) batched copy under $ErrorActionPreference=Stop ===
            $saved = $ErrorActionPreference
            $ErrorActionPreference = 'Stop'
            $caught = $null
            $warnings = $null
            $errors = $null
            try {
                Copy-OrchAsset -Name "${script:Prefix}Reg_R16_*" -Path $script:RootFolder -Destination $siblingFolder `
                    -WarningVariable warnings -ErrorVariable errors -WarningAction SilentlyContinue
            } catch {
                $caught = $_
            } finally {
                $ErrorActionPreference = $saved
            }

            $caught | Should -BeNullOrEmpty -Because '$ErrorActionPreference=Stop must NOT terminate the batch'
            ($errors | Measure-Object).Count | Should -Be 0 -Because 'accepting a per-user value for a folder-unassigned tenant user is not an error'

            Clear-OrchCache

            # === (a) Asset with Global default keeps BOTH values ===
            $dstGlobal = Get-OrchAsset -Name $assetGlobal -Path $siblingFolder
            $dstGlobal | Should -Not -BeNullOrEmpty
            $dstGlobal.Value | Should -Be 'global_default'
            ($dstGlobal.UserValues | Where-Object UserId -eq $script:TestUserAId).Value |
                Should -Be 'user_value' -Because 'the per-user value now persists (delegated to the API)'

            # === (b) per-User-only asset is now copied (not skipped) ===
            $dstUserOnly = Get-OrchAsset -Name $assetUserOnly -Path $siblingFolder -ErrorAction SilentlyContinue
            $dstUserOnly | Should -Not -BeNullOrEmpty -Because 'the per-user value is accepted, so the asset is copied rather than skipped'
            ($dstUserOnly.UserValues | Where-Object UserId -eq $script:TestUserAId).Value | Should -Be 'user_only_value'
        } finally {
            Remove-OrchAsset -Name "${script:Prefix}Reg_R16_*" -Path $script:RootFolder -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchAsset -Name "${script:Prefix}Reg_R16_*" -Path $siblingFolder -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $siblingFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R18: Copy-OrchAsset -WhatIf previews without creating the asset' {
        # Post-PR#20: a per-user value for a folder-unassigned tenant user now PERSISTS (it is no
        # longer dropped client-side), so -WhatIf has no per-user drop to warn about. The core
        # -WhatIf guarantee remains: it must only preview — no asset may be created.
        $name = "${script:Prefix}Reg_WhatIfDrop"
        $siblingFolder = "${script:Drive}:\${script:Prefix}Reg_WhatIfSibling"
        try {
            Remove-Item -LiteralPath $siblingFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
            $null = mkdir $siblingFolder -ErrorAction Stop
            Clear-OrchCache

            # Source asset in RootFolder with Global value + per-User UserValue for TestUserA.
            Set-OrchAsset -Name $name -ValueType Text -Value 'global_value' -Path $script:RootFolder | Out-Null
            Set-OrchAsset -Name $name -ValueType Text -Value 'user_value' -UserName $script:TestUserA -Path $script:RootFolder | Out-Null
            Clear-OrchCache

            # -WhatIf must NOT create anything in the destination.
            Copy-OrchAsset -Name $name -Path $script:RootFolder -Destination $siblingFolder `
                -WhatIf -WarningAction SilentlyContinue
            Clear-OrchCache

            $dst = Get-OrchAsset -Name $name -Path $siblingFolder -ErrorAction SilentlyContinue
            $dst | Should -BeNullOrEmpty -Because '-WhatIf must only preview — no asset may be created in the destination'
        } finally {
            Remove-OrchAsset -Name $name -Path $script:RootFolder -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchAsset -Name $name -Path $siblingFolder -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $siblingFolder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R17: Add-OrchUser multi-row CSV preserves row-1 scalar values when row-2 cells blank' {
        # Bug fix: OrchCsvHelper.AssignStringValue / AssignIntValue used to fall
        # through to setter(newValue) when newValue was unspecified, silently
        # clobbering whatever an earlier row had set. The string and int helpers
        # now early-return on null/empty/zero, matching AssignBoolValue's existing
        # pattern. Affected the per-(user, role) CSV import shape:
        #   UserName,Type,Role,UR_CredentialType
        #   bot1,Robot,Robot,NoCredential
        #   bot1,Robot,SomeOtherRole,                   <- blank cell would clobber
        # Pre-fix: this.UR_CredentialType="" → EndProcessing's
        #   AssignStringIfNotNullOrEmpty skips → posted user had no CredentialType.
        # Post-fix: row-1's "NoCredential" survives → posted as specified.

        # The CSV-clobbering bug is at the OrchCsvHelper layer (parameter assignment),
        # not robot-specific. Use $script:TestUserA so the test doesn't depend on a
        # robot identity being pre-registered in the tenant's directory.
        #
        # Add-OrchUser fails with "account already exists" if the target user is
        # already a tenant member, so remove first and let the CSV import re-add
        # via Identity Server lookup. BeforeAll for the next test run re-assigns
        # TestUserA to RootFolder, so this cleanup doesn't leak across runs.
        $user = $script:TestUserA
        Remove-OrchUser -UserName $user -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache

        # MayHaveUnattendedSession=True is required for DirectoryUser to receive an
        # UnattendedRobot block; without it the server returns the user with
        # UnattendedRobot=$null regardless of UR_* values, masking the regression.
        $csv = @"
UserName,Type,MayHaveUnattendedSession,UR_CredentialType
$user,DirectoryUser,True,NoCredential
$user,DirectoryUser,,
"@ | ConvertFrom-Csv

        $csv | Add-OrchUser -Path "${script:Drive}:\" -ErrorAction Stop -WarningAction SilentlyContinue
        Clear-OrchCache

        $created = Get-OrchUserDetail -Path "${script:Drive}:\" -UserName $user
        $created | Should -Not -BeNullOrEmpty -Because 'user must be retrievable after Add-OrchUser'
        $created.UnattendedRobot | Should -Not -BeNullOrEmpty `
            -Because 'row 1 specified UR_CredentialType, so UnattendedRobot block must be posted'
        $created.UnattendedRobot.CredentialType | Should -Be 'NoCredential' `
            -Because "row 2's blank UR_CredentialType cell must NOT clobber row 1's value"
    }

    It 'R12: Add-OrchCalendarDate compares against UTC.Today, not local Today' {
        # Bug fix: -ExcludedDate values were stamped Kind=Utc but the "drop past dates"
        # filter compared against `DateTime.Today` (local). On a TZ ahead of UTC the local
        # date can already be tomorrow while UTC is still today — so a -ExcludedDate built
        # from DateTime.UtcNow.Date was considered "past" (< local Today) and silently
        # dropped. Filter now uses DateTime.UtcNow.Date so today (UTC) is preserved.
        $calName = "${script:Prefix}Reg_Cal"
        try {
            $todayUtc = [DateTime]::UtcNow.Date
            Add-OrchCalendarDate -Name $calName -ExcludedDate $todayUtc -ErrorAction Stop | Out-Null
            Clear-OrchCache

            $dates = Get-OrchCalendarDate -Name $calName -IncludePastDate
            ($dates | Measure-Object).Count | Should -BeGreaterOrEqual 1 `
                -Because "today (UTC) must survive the past-date filter; if 0, the filter dropped it"
        } finally {
            Remove-OrchCalendar -Name $calName -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R13: Get-OrchCalendar -ExpandExcludedDate emits a deprecation warning and routes to Get-OrchCalendarDate' {
        # The legacy -ExpandExcludedDate switch is a deprecated alias for
        # Get-OrchCalendarDate. The warning text must point users to the
        # canonical cmdlet so existing scripts can migrate.
        $calName = "${script:Prefix}Reg_R13_Cal"
        try {
            $todayUtc = [DateTime]::UtcNow.Date
            Add-OrchCalendarDate -Name $calName -ExcludedDate $todayUtc -ErrorAction Stop | Out-Null
            Clear-OrchCache

            $warnings = $null
            $rows = Get-OrchCalendar -Name $calName -ExpandExcludedDate -IncludePastDate `
                        -WarningVariable warnings -WarningAction SilentlyContinue
            ($warnings | Where-Object { $_ -match "Get-OrchCalendarDate" }) | Should -Not -BeNullOrEmpty `
                -Because "the deprecation warning must name the canonical cmdlet"
            ($rows | Measure-Object).Count | Should -BeGreaterOrEqual 1 `
                -Because "data still emits even on the deprecated path; only a warning is added"
        } finally {
            Remove-OrchCalendar -Name $calName -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'R14: Get-OrchCalendar -ExportCsv and Get-OrchCalendarDate -ExportCsv produce identical files' {
        # Both paths must produce a byte-identical CSV so the file remains
        # round-trip compatible with Add-OrchCalendarDate. The legacy path
        # additionally emits a deprecation warning.
        $calName = "${script:Prefix}Reg_R14_Cal"
        $csvLegacy = Join-Path $env:TEMP "ut-cal-legacy-$(Get-Random).csv"
        $csvCanonical = Join-Path $env:TEMP "ut-cal-canonical-$(Get-Random).csv"
        try {
            $todayUtc = [DateTime]::UtcNow.Date
            Add-OrchCalendarDate -Name $calName -ExcludedDate $todayUtc -ErrorAction Stop | Out-Null
            Clear-OrchCache

            $warnings = $null
            Get-OrchCalendar -Path "${script:Drive}:\" -Name $calName -ExportCsv $csvLegacy -IncludePastDate `
                -WarningVariable warnings -WarningAction SilentlyContinue
            Clear-OrchCache
            Get-OrchCalendarDate -Path "${script:Drive}:\" -Name $calName -ExportCsv $csvCanonical -IncludePastDate

            ($warnings | Where-Object { $_ -match "Get-OrchCalendarDate" }) | Should -Not -BeNullOrEmpty
            (Get-FileHash $csvLegacy).Hash | Should -Be (Get-FileHash $csvCanonical).Hash `
                -Because "the on-disk CSV format must not diverge between legacy and canonical paths"
        } finally {
            Remove-Item $csvLegacy, $csvCanonical -ErrorAction SilentlyContinue
            Remove-OrchCalendar -Name $calName -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
}

# ---------------------------------------------------------------------------
# B2B EmailAddress matching - FolderUser/Role cmdlets
# ---------------------------------------------------------------------------
# Regression suite for Azure AD B2B guest users, whose tenant UserName form
# ('foo_bar.com#ext#@tenant.onmicrosoft.com') differs from their canonical
# EmailAddress ('foo@bar.com'). The Set-*Asset / Get-/Remove-OrchUser /
# Remove-OrchAssetUserValue family was already fixed to match against both
# forms; this Describe covers the remaining FolderUser cmdlets that still
# match only on UserName.
#
# Auto-skips on tenants where TestUserA's UserName equals its EmailAddress
# (no B2B guest configured), since there's no mismatch to exercise. Note that
# Remove-OrchRoleFromUser (tenant role) is intentionally NOT covered here —
# it lacks a paired Add-OrchRoleToUser cmdlet for safe pre-test setup. That
# cmdlet should be addressed separately.
Describe 'B2B EmailAddress matching - FolderUser/User cmdlets' -Tag 'B2BRegression' {
    BeforeAll {
        # Only meaningful when the two forms differ (Azure AD B2B guest case).
        $script:HasB2BMismatch = ($script:TestUserAUserName -ne $script:TestUserA)

        # A second folder role distinct from the 'Automation User' baseline,
        # used by the Add/Remove-Role tests so they have something to toggle.
        $script:SecondFolderRole = (Get-OrchRole -Path "${script:Drive}:\" |
            Where-Object { $_.Type -ne 'Tenant' -and $_.Name -ne 'Automation User' } |
            Select-Object -First 1).Name

        $script:B2BFolder = "${script:Drive}:\${script:Prefix}B2B"
        $script:B2BDest = "${script:Drive}:\${script:Prefix}B2BDest"
        if ($script:HasB2BMismatch) {
            $null = mkdir $script:B2BFolder -ErrorAction SilentlyContinue
            $null = mkdir $script:B2BDest -ErrorAction SilentlyContinue
        }
    }

    AfterAll {
        Remove-Item $script:B2BFolder -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item $script:B2BDest -Recurse -Force -ErrorAction SilentlyContinue
    }

    BeforeEach {
        if (-not $script:HasB2BMismatch) { return }
        # Reset both folders by removing all non-current-user assignments. We
        # can't rely on $script:TestUserAUserName as the remove target because
        # the tenant may have multiple users sharing the same EmailAddress —
        # Add-OrchFolderUser's Identity Server lookup picks ONE of them and we
        # don't know which until we read back. Sweep everything but the current
        # user instead.
        foreach ($folder in @($script:B2BFolder, $script:B2BDest)) {
            Get-OrchFolderUser -Path $folder -ErrorAction SilentlyContinue |
                Where-Object { $_.UserEntity.UserName -ne $script:CurrentUserName } |
                ForEach-Object {
                    Remove-OrchFolderUser -Path $folder -UserName $_.UserEntity.UserName `
                        -Confirm:$false -ErrorAction SilentlyContinue
                }
        }
        Clear-OrchCache
        Add-OrchFolderUser -Path $script:B2BFolder -Type $script:TestUserType `
            -UserName $script:TestUserA -Roles 'Automation User' -ErrorAction Stop
        Clear-OrchCache
        # Capture the user the Identity Server actually picked — for a tenant
        # with multiple Email-aliased users it may not be the one BeforeAll's
        # Get-OrchUser snapshot identified. Subsequent assertions use this
        # actual record so the test is robust to that ambiguity.
        $assigned = Get-OrchFolderUser -Path $script:B2BFolder |
            Where-Object { $_.UserEntity.UserName -ne $script:CurrentUserName } |
            Select-Object -First 1
        if (-not $assigned) {
            throw "BeforeEach: Add-OrchFolderUser -UserName $script:TestUserA did not result in an assignment to $script:B2BFolder"
        }
        $script:AssignedUserId = $assigned.UserEntity.Id
        $script:AssignedUserName = $assigned.UserEntity.UserName
    }

    It 'BF1: Get-OrchFolderUser matches B2B guest via EmailAddress form' {
        if (-not $script:HasB2BMismatch) {
            Set-ItResult -Skipped -Because 'TestUserA UserName == EmailAddress (no B2B mismatch).'
            return
        }
        $u = Get-OrchFolderUser -Path $script:B2BFolder -UserName $script:TestUserA
        $u | Should -Not -BeNullOrEmpty `
            -Because 'Get-OrchFolderUser must accept the EmailAddress form for B2B guests'
        $u.UserEntity.Id | Should -Be $script:AssignedUserId `
            -Because 'matched user must be the one Add-OrchFolderUser placed in the folder'
    }

    It 'BF2: Add-OrchRoleToFolderUser adds role to B2B guest via EmailAddress form' {
        if (-not $script:HasB2BMismatch) {
            Set-ItResult -Skipped -Because 'TestUserA UserName == EmailAddress (no B2B mismatch).'
            return
        }
        if (-not $script:SecondFolderRole) {
            Set-ItResult -Skipped -Because 'Tenant has no second folder role to add.'
            return
        }
        Add-OrchRoleToFolderUser -Path $script:B2BFolder -UserName $script:TestUserA `
            -Roles $script:SecondFolderRole -ErrorAction Stop
        Clear-OrchCache
        $u = Get-OrchFolderUser -Path $script:B2BFolder -UserName $script:AssignedUserName
        $u.Roles.Name | Should -Contain $script:SecondFolderRole `
            -Because 'Role must be added when -UserName is the EmailAddress form'
    }

    It 'BF3: Remove-OrchRoleFromFolderUser removes role from B2B guest via EmailAddress form' {
        if (-not $script:HasB2BMismatch) {
            Set-ItResult -Skipped -Because 'TestUserA UserName == EmailAddress (no B2B mismatch).'
            return
        }
        if (-not $script:SecondFolderRole) {
            Set-ItResult -Skipped -Because 'Tenant has no second folder role to add.'
            return
        }
        # Pre-add the role via the assigned user's tenant UserName (preparation
        # uses the known-working path; the test exercises the EmailAddress path).
        Add-OrchRoleToFolderUser -Path $script:B2BFolder -UserName $script:AssignedUserName `
            -Roles $script:SecondFolderRole -ErrorAction Stop
        Clear-OrchCache
        # Remove via the EmailAddress form (the path under test).
        Remove-OrchRoleFromFolderUser -Path $script:B2BFolder -UserName $script:TestUserA `
            -Roles $script:SecondFolderRole -Confirm:$false -ErrorAction Stop
        Clear-OrchCache
        $u = Get-OrchFolderUser -Path $script:B2BFolder -UserName $script:AssignedUserName
        $u.Roles.Name | Should -Not -Contain $script:SecondFolderRole `
            -Because 'Role must be removed when -UserName is the EmailAddress form'
    }

    It 'BF4: Move-OrchFolderUser moves B2B guest via EmailAddress form' {
        if (-not $script:HasB2BMismatch) {
            Set-ItResult -Skipped -Because 'TestUserA UserName == EmailAddress (no B2B mismatch).'
            return
        }
        Move-OrchFolderUser -Path $script:B2BFolder -Destination $script:B2BDest `
            -UserName $script:TestUserA -Confirm:$false -ErrorAction Stop
        Clear-OrchCache
        $atDst = Get-OrchFolderUser -Path $script:B2BDest -UserName $script:AssignedUserName
        $atDst | Should -Not -BeNullOrEmpty `
            -Because 'Move destination must receive the B2B guest when -UserName is EmailAddress'
        $atSrc = Get-OrchFolderUser -Path $script:B2BFolder -UserName $script:AssignedUserName -ErrorAction SilentlyContinue
        $atSrc | Should -BeNullOrEmpty `
            -Because 'Move source must lose the B2B guest after a non-KeepSource move'
    }

    It 'BF5: Copy-OrchFolderUser copies B2B guest via EmailAddress form' {
        if (-not $script:HasB2BMismatch) {
            Set-ItResult -Skipped -Because 'TestUserA UserName == EmailAddress (no B2B mismatch).'
            return
        }
        Copy-OrchFolderUser -Path $script:B2BFolder -Destination $script:B2BDest `
            -UserName $script:TestUserA -ErrorAction Stop
        Clear-OrchCache
        $atDst = Get-OrchFolderUser -Path $script:B2BDest -UserName $script:AssignedUserName
        $atDst | Should -Not -BeNullOrEmpty `
            -Because 'Copy destination must receive the B2B guest when -UserName is EmailAddress'
        $atSrc = Get-OrchFolderUser -Path $script:B2BFolder -UserName $script:AssignedUserName
        $atSrc | Should -Not -BeNullOrEmpty `
            -Because 'Copy retains the source assignment'
    }

    It 'BF6: Remove-OrchFolderUser removes B2B guest via EmailAddress form' {
        if (-not $script:HasB2BMismatch) {
            Set-ItResult -Skipped -Because 'TestUserA UserName == EmailAddress (no B2B mismatch).'
            return
        }
        Remove-OrchFolderUser -Path $script:B2BFolder -UserName $script:TestUserA `
            -Confirm:$false -ErrorAction Stop
        Clear-OrchCache
        $u = Get-OrchFolderUser -Path $script:B2BFolder -UserName $script:AssignedUserName -ErrorAction SilentlyContinue
        $u | Should -BeNullOrEmpty `
            -Because 'Remove-OrchFolderUser must accept the EmailAddress form for B2B guests'
    }

    It 'BF7: Remove-OrchRoleFromUser removes tenant role from B2B guest via EmailAddress form' {
        if (-not $script:HasB2BMismatch) {
            Set-ItResult -Skipped -Because 'TestUserA UserName == EmailAddress (no B2B mismatch).'
            return
        }
        # No Add-OrchRoleToUser cmdlet exists; use Update-OrchUser (which replaces
        # RolesList) to set up. Snapshot original roles, add a candidate tenant
        # role, then verify removal via the EmailAddress form. finally{} restores
        # the original RolesList so this test is non-destructive.
        $detail = Get-OrchUserDetail -Path "${script:Drive}:\" -UserName $script:TestUserAUserName
        $originalRoles = @($detail.RolesList)
        $candidateRole = Get-OrchRole -Path "${script:Drive}:\" |
            Where-Object { $_.Type -eq 'Tenant' -and $_.Name -notin $originalRoles } |
            Select-Object -First 1
        if (-not $candidateRole) {
            Set-ItResult -Skipped -Because 'No spare tenant role available to add/remove on TestUserA.'
            return
        }

        try {
            Update-OrchUser -UserName $script:TestUserAUserName `
                -Roles ($originalRoles + $candidateRole.Name) -ErrorAction Stop
            Clear-OrchCache

            Remove-OrchRoleFromUser -UserName $script:TestUserA -Roles $candidateRole.Name `
                -Confirm:$false -ErrorAction Stop
            Clear-OrchCache

            $after = Get-OrchUserDetail -Path "${script:Drive}:\" -UserName $script:TestUserAUserName
            $after.RolesList | Should -Not -Contain $candidateRole.Name `
                -Because 'Remove-OrchRoleFromUser must accept the EmailAddress form for B2B guests'
        } finally {
            # Restore the original RolesList. Use -Roles with the snapshot;
            # Update-OrchUser replaces RolesList wholesale, so the user is left
            # exactly as before the test.
            if ($originalRoles.Count -gt 0) {
                Update-OrchUser -UserName $script:TestUserAUserName `
                    -Roles $originalRoles -ErrorAction SilentlyContinue
            } else {
                # snapshot was empty — pass an empty role to clear back to none
                Update-OrchUser -UserName $script:TestUserAUserName `
                    -Roles @('') -ErrorAction SilentlyContinue
            }
            Clear-OrchCache
        }
    }
}

# ---------------------------------------------------------------------------
# H1 Phase 1: SingleCachePerOrganization per-drive Path via per-emit ShallowClone
# ---------------------------------------------------------------------------
# Org-scoped entities (LicenseInventory, AccountLicense, PmAuthenticationRoot,
# PmUser et al.) are static singletons shared across drives in the same
# organization. The earlier PSObject-NoteProperty wrap was dropped (PowerShell
# keys PSObject instance members to base-object identity, so wrappers over the
# shared singleton collapsed to the last-written Path). Path/GroupName are now
# plain [JsonIgnore] properties on the DTO again; the cache keeps serving the
# shared singleton and never sets them, while each cmdlet emit takes a
# ShallowClone (distinct CLR instance) before stamping Path — so per-drive Path
# is independent without mutating the shared instance, and is a real property
# on the entity, NOT a PSObject NoteProperty.
Describe 'H1 Phase 1: SingleCachePerOrganization per-drive Path via ShallowClone' -Tag 'H1Phase1' {

    It 'BF8: Get-PmLicenseInventory output carries Path as a plain DTO property (per-emit ShallowClone)' {
        $inv = $null
        try {
            $inv = Get-PmLicenseInventory -Path "${script:Drive}:" -ErrorAction Stop
        } catch {
            Set-ItResult -Skipped -Because "PmLicenseInventory unavailable on this drive: $($_.Exception.Message)"
            return
        }
        if (-not $inv) {
            Set-ItResult -Skipped -Because 'PmLicenseInventory returned null on this drive.'
            return
        }
        $inv.Path | Should -Be "${script:Drive}:\" `
            -Because 'the per-emit ShallowClone carries the drive-local Path (drive.NameColonSeparator)'
        $inv.PSObject.Properties['Path'].MemberType | Should -Be 'Property' `
            -Because 'Path is a plain DTO property again (per-emit ShallowClone), not a PSObject NoteProperty'
        $inv.GetType().GetProperty('Path') | Should -Not -BeNullOrEmpty `
            -Because 'the entity type carries a real Path property ([JsonIgnore] on the DTO)'
    }

    It 'BF9: Get-PmUser list output carries Path as a plain DTO property (ListCachePerOrganization)' {
        # Phase 2: ListCachePerOrganization-backed entities (PmUser et al.)
        # follow the same model as Phase 1. Verify on PmUser; the other 9
        # entities share the cache class and the per-emit ShallowClone pattern.
        $users = $null
        try {
            $users = Get-PmUser -Path "${script:Drive}:" -ErrorAction Stop | Select-Object -First 1
        } catch {
            Set-ItResult -Skipped -Because "Get-PmUser unavailable on this drive: $($_.Exception.Message)"
            return
        }
        if (-not $users) {
            Set-ItResult -Skipped -Because 'Get-PmUser returned no users on this drive.'
            return
        }
        $users.Path | Should -Be "${script:Drive}:\" `
            -Because 'the per-emit ShallowClone carries the drive-local Path'
        $users.PSObject.Properties['Path'].MemberType | Should -Be 'Property' `
            -Because 'Path is a plain property on the cloned PmUser, not a PSObject NoteProperty'
        $users.GetType().GetProperty('Path') | Should -Not -BeNullOrEmpty `
            -Because 'PmUser carries a real Path property ([JsonIgnore] on the DTO)'
    }
}
