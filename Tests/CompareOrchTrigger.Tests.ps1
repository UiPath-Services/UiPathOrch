#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration test for Compare-OrchTrigger — a folder-scoped noun whose entities come from the
    drive.GetTriggers accessor, exercised end-to-end through the shared FolderCompare engine
    (name-match, broadcast, -Recurse, -Property).

.DESCRIPTION
    Requires a connected, writable Orch2: drive with at least one package in its feed. Deploys a
    process into each test folder and creates time triggers referencing it, varying Enabled to
    produce differences. Skips if no package is available. Prefixed "PesterCmpTr_XXXX_" and
    removed in AfterAll.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:Prefix = "PesterCmpTr_$(Get-Random -Maximum 9999)_"
    $script:RootA  = "${script:Drive}:\${script:Prefix}A"
    $script:RootB  = "${script:Drive}:\${script:Prefix}B"
    $script:SubA   = "${script:RootA}\Sub"
    $script:SubB   = "${script:RootB}\Sub"

    $script:Proc    = "${script:Prefix}Proc"
    $script:Same    = "${script:Prefix}Same"
    $script:Changed = "${script:Prefix}Changed"
    $script:SubTrig = "${script:Prefix}SubTrig"
    $script:Cron    = '0 0 0 1/1 * ? *'

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $script:PackageId = (Get-OrchPackage -Path "${script:Drive}:\" -ErrorAction SilentlyContinue | Select-Object -First 1).Id

    $null = mkdir $script:RootA
    $null = mkdir $script:RootB
    $null = mkdir $script:SubA
    $null = mkdir $script:SubB

    if ($script:PackageId) {
        foreach ($f in $script:RootA, $script:RootB, $script:SubA, $script:SubB) {
            New-OrchProcess -Id $script:PackageId -Name $script:Proc -Path $f | Out-Null
        }
        New-OrchTrigger -Path $script:RootA -Name $script:Same    -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:RootA -Name $script:Changed -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:RootB -Name $script:Same    -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:RootB -Name $script:Changed -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled false | Out-Null
        New-OrchTrigger -Path $script:SubA  -Name $script:SubTrig -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:SubB  -Name $script:SubTrig -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled false | Out-Null
        Clear-OrchCache
    }
}

AfterAll {
    foreach ($f in $script:RootA, $script:RootB) {
        Remove-OrchTrigger -Name "${script:Prefix}*" -Path $f -Recurse -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchProcess -Name "${script:Prefix}*" -Path $f -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    }
    Remove-Item $script:SubA  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubB  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootA -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootB -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Compare-OrchTrigger' {
    It 'reports a changed trigger as "<>" with an Enabled difference' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB
        ($r | Where-Object Name -eq $script:Changed).SideIndicator | Should -Be '<>'
    }

    It 'suppresses equal triggers by default and shows them with -IncludeEqual' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB -IncludeEqual
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '=='
    }

    It 'broadcasts to a single named target with -DifferenceName' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        # Same@B (Enabled true) vs Changed@B (Enabled false) -> differ.
        $r = Compare-OrchTrigger -Path $script:RootB -Name $script:Same `
            -DifferencePath $script:RootB -DifferenceName $script:Changed
        $r.SideIndicator | Should -Be '<>'
    }

    It 'errors when the named difference trigger does not exist' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        { Compare-OrchTrigger -Path $script:RootA -Name $script:Same `
            -DifferencePath $script:RootB -DifferenceName "${script:Prefix}Nope" -ErrorAction Stop } |
            Should -Throw
    }

    It 'warns on an unrecognized -Property name' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB `
            -Property 'Bogus' -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Match 'unrecognized'
    }

    It 'descends into mirrored subfolders with -Recurse' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB -Recurse
        $sub = $r | Where-Object { $_.Name -eq $script:SubTrig -and $_.Path -like '*\Sub\*' }
        $sub.SideIndicator | Should -Be '<>'
    }

    # Only time triggers are in scope here, so the queue-trigger notice must stay quiet.
    It 'does not mention StartProcessCron when no queue trigger is in scope' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB `
            -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Not -Match 'StartProcessCron'
    }
}

# ---------------------------------------------------------------------------
# A queue trigger's StartProcessCron is not a setting anyone chose: such a trigger fires on queue
# items and the web UI offers it no cron, so the value in ProcessScheduleDto is whatever the last
# writer left. Comparing it flagged every correctly migrated queue trigger (reported from an
# MSI-to-Automation-Suite migration, 2026-08-31, source "0 0/30 * 1/1 * ? *" against destination
# "33 20/30 * * * ? *").
#
# Skipping it silently would be its own bug: an "==" row would be read as "the cron matched" when
# nothing checked it. But that misreading only matters when the value really does differ -- when
# the two crons are identical the row is equal in every sense, so the notice fires on a DIFFERENCE,
# not on a queue trigger merely being in scope. (That is where this parts company with
# Compare-OrchAsset's secret notice, which has to fire on presence: a secret's value is never
# returned, so drift is unknowable. Here both values are in hand.)
# ---------------------------------------------------------------------------
Describe 'Compare-OrchTrigger and a queue trigger cron' {
    BeforeAll {
        $script:QSkip = if ($script:PackageId) { $null } else { 'no package on test drive' }
        if ($script:QSkip) { return }

        $script:QRootA = "${script:Drive}:\${script:Prefix}QA"
        $script:QRootB = "${script:Drive}:\${script:Prefix}QB"
        $script:QName  = "${script:Prefix}Queue"
        $script:QTrig  = "${script:Prefix}QTrig"
        $script:TTrig  = "${script:Prefix}TTrig"

        $null = mkdir $script:QRootA
        $null = mkdir $script:QRootB
        foreach ($f in $script:QRootA, $script:QRootB) {
            New-OrchProcess -Id $script:PackageId -Name $script:Proc -Path $f | Out-Null
            New-OrchQueue -Path $f -Name $script:QName | Out-Null
        }

        # Same queue trigger both sides, DIFFERENT cron: the reported shape.
        New-OrchTrigger -Path $script:QRootA -Name $script:QTrig -ReleaseName $script:Proc `
            -QueueDefinitionName $script:QName -StartProcessCron '0 0/30 * 1/1 * ? *' -Enabled false | Out-Null
        New-OrchTrigger -Path $script:QRootB -Name $script:QTrig -ReleaseName $script:Proc `
            -QueueDefinitionName $script:QName -StartProcessCron '33 20/30 * * * ? *' -Enabled false | Out-Null

        # The contrast: a TIME trigger with the same two crons, which must still be reported.
        New-OrchTrigger -Path $script:QRootA -Name $script:TTrig -ReleaseName $script:Proc `
            -StartProcessCron '0 0/30 * 1/1 * ? *' -Enabled false | Out-Null
        New-OrchTrigger -Path $script:QRootB -Name $script:TTrig -ReleaseName $script:Proc `
            -StartProcessCron '0 0/45 * 1/1 * ? *' -Enabled false | Out-Null

        # A second queue trigger, SAME NAME, in a mirrored subfolder -- the shape a migration
        # produces. Two triggers differ, and they share a name, so the notice has to count and name
        # them by path: keyed on the name it would say "1" and point at one folder.
        $script:QSubA = "$($script:QRootA)\Sub"
        $script:QSubB = "$($script:QRootB)\Sub"
        foreach ($f in $script:QSubA, $script:QSubB) {
            $null = mkdir $f
            New-OrchProcess -Id $script:PackageId -Name $script:Proc -Path $f | Out-Null
            New-OrchQueue -Path $f -Name $script:QName | Out-Null
            New-OrchTrigger -Path $f -Name $script:QTrig -ReleaseName $script:Proc `
                -QueueDefinitionName $script:QName -StartProcessCron '0 0/30 * 1/1 * ? *' -Enabled false | Out-Null
        }

        # The matching-cron case is NOT built here. Automation Cloud assigns a queue trigger's cron
        # itself (measured 2026-09-01: posting "13 7/29 * 1/1 * ? *" reads back "39 3/30 * * * ? *",
        # and the next one "40 3/30 * * * ? *"), so two queue triggers created separately can never
        # carry the same cron -- the fixture is unbuildable, and an attempt at it passed only
        # because Orchestrator refused the second trigger on that queue (409, errorCode 1607: one
        # trigger per queue) and left nothing to compare. That case is covered deterministically by
        # CompareTriggerComparatorTests.QueueTrigger_SameCron_HidesNothing.
        Clear-OrchCache
    }

    AfterAll {
        foreach ($f in $script:QSubA, $script:QSubB, $script:QRootA, $script:QRootB) {
            if (-not $f) { continue }
            Remove-OrchTrigger -Name "${script:Prefix}*" -Path $f -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchProcess -Name "${script:Prefix}*" -Path $f -Confirm:$false -ErrorAction SilentlyContinue
            Remove-Item $f -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # The two sides are identical apart from whatever cron the server gave each, so the only thing
    # that could produce a row here is the cron -- and it must not.
    It 'does not report a queue trigger, whatever cron the server gave each side' {
        if ($script:QSkip) { Set-ItResult -Skipped -Because $script:QSkip; return }
        $r = @(Compare-OrchTrigger $script:QTrig $script:QRootB -Path $script:QRootA -WarningAction SilentlyContinue)
        $r | Should -BeNullOrEmpty -Because 'nobody chose that value, so the difference is not one anyone can act on'
    }

    # Whether two queue triggers' crons differ is NOT something a test can arrange: the server
    # assigns the value and refuses to take one, at create and at update alike (measured on
    # Automation Cloud 26.3, 2026-09-01 -- New-OrchTrigger and a raw POST are both overridden, and
    # Update-OrchTrigger -StartProcessCron leaves it unchanged). Two triggers created moments apart
    # may get the same expression or different ones depending on the clock. So "warns when it
    # differs", "warns once for several", "counts by path rather than by name" and "stays quiet
    # when they agree" are pinned deterministically in CompareMigrationNoiseTests instead
    # (AddCronDiff / ComposeCronNotice / HidesACronDifference). What stays here is what a server
    # does reproduce.
    It 'still reports a TIME trigger whose cron differs' {
        if ($script:QSkip) { Set-ItResult -Skipped -Because $script:QSkip; return }
        $r = @(Compare-OrchTrigger $script:TTrig $script:QRootB -Path $script:QRootA -WarningAction SilentlyContinue)
        $r.SideIndicator | Should -Be '<>' -Because 'a time trigger cron IS a setting someone chose'
        ($r.Differences -join ' ') | Should -Match 'StartProcessCron'
    }

    # -Property narrows the comparison to one name; over a queue trigger that name compares
    # nothing, so the row comes back "==" with an empty Differences whether or not the underlying
    # crons agree. (That the notice fires when they do differ is pinned in the unit tests, for the
    # reason above.)
    It 'reports "==" when -Property asks for StartProcessCron alone on a queue trigger' {
        if ($script:QSkip) { Set-ItResult -Skipped -Because $script:QSkip; return }
        $r = @(Compare-OrchTrigger $script:QTrig $script:QRootB -Path $script:QRootA -Property StartProcessCron `
            -IncludeEqual -WarningAction SilentlyContinue)
        $r.SideIndicator | Should -Be '==' -Because 'the only compared property was suppressed'
        $r.Differences | Should -BeNullOrEmpty
    }
}
