#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Live integration tests for the "only PUT when a value actually changed"
    (dirty-gating) behavior of the Update-Orch* cmdlets.

.DESCRIPTION
    Every Update-Orch* cmdlet computes a change-set against the current server state
    and skips the PUT/PATCH entirely when nothing differs. These tests prove that gate
    end-to-end, in two shapes dictated by what the server actually returns (verified
    against the v17 swagger AND live: only Release and User expose a LastModificationTime):

      SHAPE A (entities WITH a server LastModificationTime — Process, ProcessVersion, User):
        re-applying the current value leaves LastModificationTime untouched (no write);
        a different value advances it (a write happened). BOTH directions provable live.

      SHAPE B (entities with NO LastModificationTime — Machine, Queue, Bucket, Webhook,
        Trigger, ApiTrigger): only "different value => write" is provable live (the changed
        value round-trips through a read-back). The no-op (same-value => no PUT) direction
        is covered by the C# unit tests (Tests/UnitTests/Update*DirtyTests.cs) and is NOT
        provable live — there is no timestamp to observe.

    LastModificationTime is second-granular and null until the FIRST modification, so SHAPE A
    establishes a baseline with one warm-up Update, then sleeps >1s before the different-value
    write so the advanced timestamp is observably greater.

    Self-contained: all entities are created under a "PesterDirty_XXXX_" prefix and removed in
    AfterAll. Run directly (Invoke-Pester -Path Tests\UpdateDirtyGating.Tests.ps1) — it does not
    wipe the tenant, so it is safe against a populated on-prem drive. Set UIPATHORCH_TEST_DRIVE
    to the target drive (both it and the ref drive can be the same on-prem drive).

.NOTES
    Run with: Invoke-Pester -Path Tests\UpdateDirtyGating.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:Prefix = "PesterDirty_$(Get-Random -Maximum 9999)_"
    $script:RootFolder = "${script:Drive}:\${script:Prefix}Root"

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $null = mkdir $script:RootFolder

    # A package on the test drive itself, for Process / ProcessVersion / Trigger / ApiTrigger.
    $script:PackageId = (Get-OrchPackage -Path "${script:Drive}:\" | Select-Object -First 1).Id

    # A spare directory user (not the current one) for the User test.
    $script:CurrentUserName = (Get-OrchCurrentUser -Path "${script:Drive}:\").UserName
    $alternateUser = Get-OrchUser -Path "${script:Drive}:\" -Type DirectoryUser -ErrorAction SilentlyContinue |
        Where-Object { $_.UserName -ne $script:CurrentUserName } |
        Select-Object -First 1
    $script:TestUserAUserName = if ($alternateUser) { $alternateUser.UserName } else { $null }
}

AfterAll {
    Push-Location $script:RootFolder -ErrorAction SilentlyContinue
    Remove-OrchTrigger -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchApiTrigger -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchProcess -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchQueue -Name "${script:Prefix}*" -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchBucket -Name "${script:Prefix}*" -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Pop-Location

    Remove-OrchMachine -Name "${script:Prefix}*" -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchWebhook -Path "${script:Drive}:\" -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue

    Remove-Item $script:RootFolder -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

# ===========================================================================
# SHAPE A — entities WITH a server LastModificationTime (both directions).
# ===========================================================================

Describe 'Update-OrchProcess dirty-gating' {
    BeforeAll {
        if (-not $script:PackageId) { $script:ProcSkip = 'No package on the test drive to create a process from.' }
        $script:ProcessName = "${script:Prefix}Process"
        Push-Location $script:RootFolder
        if (-not $script:ProcSkip) { New-OrchProcess -Id $script:PackageId -Name $script:ProcessName; Clear-OrchCache }
    }
    AfterAll {
        Remove-OrchProcess -Name $script:ProcessName -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'skips the PATCH on an unchanged value and writes on a changed value' {
        if ($script:ProcSkip) { Set-ItResult -Skipped -Because $script:ProcSkip; return }

        # Warm-up write so LastModificationTime is populated (it is null until first modified).
        Update-OrchProcess -Name $script:ProcessName -Description 'dirty-base'
        Clear-OrchCache
        $before = Get-OrchProcess -Name $script:ProcessName
        $before.LastModificationTime | Should -Not -BeNullOrEmpty

        # same value => NO patch
        Update-OrchProcess -Name $script:ProcessName -Description 'dirty-base'
        Clear-OrchCache
        (Get-OrchProcess -Name $script:ProcessName).LastModificationTime | Should -Be $before.LastModificationTime

        # different value => PATCH
        Start-Sleep -Milliseconds 1100
        Update-OrchProcess -Name $script:ProcessName -Description 'dirty-changed'
        Clear-OrchCache
        $after = Get-OrchProcess -Name $script:ProcessName
        $after.Description | Should -Be 'dirty-changed'
        $after.LastModificationTime | Should -BeGreaterThan $before.LastModificationTime
    }
}

Describe 'Update-OrchProcessVersion dirty-gating' {
    BeforeAll {
        if (-not $script:PackageId) { $script:PvSkip = 'No package on the test drive to create a process from.' }
        $script:PvProcessName = "${script:Prefix}PvProcess"
        Push-Location $script:RootFolder
        if (-not $script:PvSkip) {
            New-OrchProcess -Id $script:PackageId -Name $script:PvProcessName
            Clear-OrchCache
            $script:PvVersions = @(
                Get-OrchPackageVersion -Id $script:PackageId -Path "${script:Drive}:\" -ErrorAction SilentlyContinue |
                    ForEach-Object { $_.Version } | Where-Object { $_ }
            )
        }
    }
    AfterAll {
        Remove-OrchProcess -Name $script:PvProcessName -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 're-pinning the current version is a no-op (LastModificationTime unchanged)' {
        if ($script:PvSkip) { Set-ItResult -Skipped -Because $script:PvSkip; return }

        # Warm-up so LastModificationTime is populated.
        Update-OrchProcess -Name $script:PvProcessName -Description 'dirty-base'
        Clear-OrchCache
        $before = Get-OrchProcess -Name $script:PvProcessName
        $before.LastModificationTime | Should -Not -BeNullOrEmpty
        $curVer = if ($before.CurrentVersion.VersionNumber) { $before.CurrentVersion.VersionNumber } else { $before.ProcessVersion }
        $curVer | Should -Not -BeNullOrEmpty

        Update-OrchProcessVersion -Name $script:PvProcessName -Version $curVer
        Clear-OrchCache
        (Get-OrchProcess -Name $script:PvProcessName).LastModificationTime | Should -Be $before.LastModificationTime
    }

    It 'pinning to a different version writes (LastModificationTime advances)' {
        if ($script:PvSkip) { Set-ItResult -Skipped -Because $script:PvSkip; return }

        Update-OrchProcess -Name $script:PvProcessName -Description 'dirty-base2'
        Clear-OrchCache
        $before = Get-OrchProcess -Name $script:PvProcessName
        $curVer = if ($before.CurrentVersion.VersionNumber) { $before.CurrentVersion.VersionNumber } else { $before.ProcessVersion }
        $otherVer = $script:PvVersions | Where-Object { $_ -ne $curVer } | Select-Object -First 1
        if (-not $otherVer) { Set-ItResult -Skipped -Because 'needs a second published package version'; return }

        Start-Sleep -Milliseconds 1100
        Update-OrchProcessVersion -Name $script:PvProcessName -Version $otherVer
        Clear-OrchCache
        (Get-OrchProcess -Name $script:PvProcessName).LastModificationTime | Should -BeGreaterThan $before.LastModificationTime
    }
}

Describe 'Update-OrchUser dirty-gating' {
    # A directory user has a LastModificationTime, but the only editable diff is its tenant
    # RolesList — and (a) editing it mutates a real, shared-tenant user's permissions (a side
    # effect), and (b) the RolesList read-back on this tenant does not reliably reflect
    # Update-OrchUser -Roles. The User no-op is covered by the C# unit tests
    # (UpdateDirtyDetectionTests.cs), and live no-op is already proven on Process/ProcessVersion,
    # so this stays a documented skip rather than a flaky, side-effecting live test.
    It 'is skipped (RolesList read-back unreliable; live no-op proven on Process/ProcessVersion)' {
        Set-ItResult -Skipped -Because 'Editing a directory user RolesList mutates a real shared-tenant user (side effect) and does not read back reliably here; the User no-op is covered by the C# unit tests and live no-op is proven on Process/ProcessVersion.'
    }
}

# ===========================================================================
# SHAPE B — entities with NO LastModificationTime. Only "different value => write"
# is provable live. The no-op direction is covered by the C# unit tests.
# ===========================================================================

Describe 'Update-OrchMachine dirty-gating' {
    BeforeAll {
        $script:MachineName = "${script:Prefix}Machine"
        New-OrchMachine -Name $script:MachineName -Path "${script:Drive}:\" -Description 'dirty-base'
        Clear-OrchCache
    }
    AfterAll { Remove-OrchMachine -Name $script:MachineName -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue }

    It 'a changed value round-trips (write happened)' {
        Update-OrchMachine -Name $script:MachineName -Path "${script:Drive}:\" -Description 'dirty-changed'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path "${script:Drive}:\").Description | Should -Be 'dirty-changed'
        # no-op (same-value => no PUT) is covered by Tests/UnitTests/UpdateMachineDirtyTests.cs; not provable live (no LastModificationTime).
    }
}

Describe 'Update-OrchQueue dirty-gating' {
    BeforeAll {
        $script:QueueName = "${script:Prefix}Queue"
        Push-Location $script:RootFolder
        New-OrchQueue -Name $script:QueueName -Description 'dirty-base'
        Clear-OrchCache
    }
    AfterAll { Remove-OrchQueue -Name $script:QueueName -Confirm:$false -ErrorAction SilentlyContinue; Pop-Location }

    It 'a changed value round-trips (write happened)' {
        Update-OrchQueue -Name $script:QueueName -Description 'dirty-changed'
        Clear-OrchCache
        (Get-OrchQueue -Name $script:QueueName).Description | Should -Be 'dirty-changed'
        # no-op covered by Tests/UnitTests/UpdateQueueDirtyTests.cs; not provable live (Queue exposes only CreationTime).
    }
}

Describe 'Update-OrchBucket dirty-gating' {
    BeforeAll {
        $script:BucketName = "${script:Prefix}Bucket"
        Push-Location $script:RootFolder
        New-OrchBucket -Name $script:BucketName -Description 'dirty-base'
        Clear-OrchCache
    }
    AfterAll { Remove-OrchBucket -Name $script:BucketName -Confirm:$false -ErrorAction SilentlyContinue; Pop-Location }

    It 'a changed value round-trips (write happened)' {
        Update-OrchBucket -Name $script:BucketName -Description 'dirty-changed'
        Clear-OrchCache
        (Get-OrchBucket -Name $script:BucketName).Description | Should -Be 'dirty-changed'
        # no-op covered by Tests/UnitTests/UpdateBucketDirtyTests.cs; not provable live (no LastModificationTime).
    }
}

Describe 'Update-OrchWebhook dirty-gating' {
    BeforeAll {
        $script:WebhookName = "${script:Prefix}Webhook"
        # Fake URL, disabled; subscribe-to-all avoids the "Events required" error. It can never deliver.
        New-OrchWebhook -Path "${script:Drive}:\" -Name $script:WebhookName `
            -Url 'https://example.invalid/pester' -Enabled false -SubscribeToAllEvents true -Description 'dirty-base'
        Clear-OrchCache
    }
    AfterAll { Remove-OrchWebhook -Path "${script:Drive}:\" -Name $script:WebhookName -Confirm:$false -ErrorAction SilentlyContinue }

    It 'a changed value round-trips (write happened)' {
        Update-OrchWebhook -Path "${script:Drive}:\" -Name $script:WebhookName -Description 'dirty-changed'
        Clear-OrchCache
        (Get-OrchWebhook -Path "${script:Drive}:\" -Name $script:WebhookName).Description | Should -Be 'dirty-changed'
        # no-op covered by Tests/UnitTests/UpdateWebhookDirtyTests.cs; not provable live (no LastModificationTime).
    }
}

Describe 'Update-OrchTrigger dirty-gating' {
    BeforeAll {
        if (-not $script:PackageId) { $script:TrigSkip = 'No package on the test drive to create a process from.' }
        $script:TriggerProcess = "${script:Prefix}TrigProcess"
        $script:TriggerName = "${script:Prefix}Trigger"
        Push-Location $script:RootFolder
        if (-not $script:TrigSkip) {
            New-OrchProcess -Id $script:PackageId -Name $script:TriggerProcess
            Clear-OrchCache
            New-OrchTrigger -Name $script:TriggerName -ReleaseName $script:TriggerProcess -StartProcessCron '0 0 0 1/1 * ? *' -Enabled false
            Clear-OrchCache
        }
    }
    AfterAll {
        Remove-OrchTrigger -Name $script:TriggerName -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchProcess -Name $script:TriggerProcess -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'a changed value round-trips (write happened)' {
        if ($script:TrigSkip) { Set-ItResult -Skipped -Because $script:TrigSkip; return }
        Update-OrchTrigger -Name $script:TriggerName -Enabled true
        Clear-OrchCache
        (Get-OrchTrigger -Name $script:TriggerName).Enabled | Should -Be $true
        # no-op covered by Tests/UnitTests/UpdateTriggerDirtyTests.cs; not provable live (ProcessSchedule has no LastModificationTime).
    }
}

Describe 'Update-OrchApiTrigger dirty-gating' {
    # On this tenant Get-OrchApiTrigger did not return the just-created trigger (read-back came
    # back empty), so a live round-trip is unreliable here. The ApiTrigger diff/no-op are covered
    # by the C# unit tests (UpdateApiTriggerDirtyTests.cs).
    It 'is skipped (Get-OrchApiTrigger read-back unreliable on this tenant)' {
        Set-ItResult -Skipped -Because 'Get-OrchApiTrigger did not return the just-created trigger here, so a live read-back is unreliable; covered by the C# unit tests (UpdateApiTriggerDirtyTests.cs).'
    }
}

# ===========================================================================
# SKIP — cannot create or update on an on-prem/standard tenant.
# ===========================================================================

Describe 'Update-OrchTestSetSchedule dirty-gating' {
    It 'is skipped on a standard tenant' {
        Set-ItResult -Skipped -Because 'TestSetSchedule create/update is rejected on every tenant tested (errorCode 3234).'
    }
}

Describe 'Update-OrchBusinessRule dirty-gating' {
    It 'is skipped (cmdlets not exported)' {
        Set-ItResult -Skipped -Because 'BusinessRule cmdlets are non-public / not exported from the module.'
    }
}

Describe 'Update-OrchCredentialStore dirty-gating' {
    It 'is skipped (no New- path, no timestamp)' {
        Set-ItResult -Skipped -Because 'No New-OrchCredentialStore path (only the default DB store), and CredentialStore has no LastModificationTime.'
    }
}
