#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Live reproductions of bugs fixed after v1.9.1, exercised through the real cmdlets.

.DESCRIPTION
    Because these drive the cmdlets by name (no compile-time reference to the module's internals),
    the SAME file runs against a v1.9.1-deployed module AND a HEAD-deployed module. Each test FAILS
    on v1.9.1 (reproduces the bug) and PASSES on HEAD (fix confirmed):

      - 2.5  Get-Item -LiteralPath on a backtick-named folder (GetItem wrongly unescaped it).
      - C3   New-Item -FeedType <invalid> fell through to CreateFolder -> two errors instead of one.
      - A1   Get-OrchWebhook | Update-OrchWebhook coerced WebhookEvent[] -> garbage -> wiped events.
      - A2   Get-OrchQueue   | Update-OrchQueue   coerced Tag[]          -> garbage -> corrupted tags.
      - B2   Copy-OrchQueue dropped RetryAbandonedItems (omitted from the copied queue DTO).

    Runs against a LIVE drive ($env:UIPATHORCH_TEST_DRIVE, default 'Orch2'); creates throwaway
    folders/entities and tears them down. The test webhook is created Enabled=false so it never delivers.
#>

BeforeAll {
    $script:DriveName = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:d = "${script:DriveName}:"
    Import-OrchConfig | Out-Null
    $script:hasDrive = $null -ne (Get-OrchPSDrive | Where-Object Name -eq $script:DriveName)

    $script:tag = "ZZreg_$([guid]::NewGuid().ToString('N').Substring(0,8))"
    $script:src = "$($script:d)\$($script:tag)_src"
    $script:dst = "$($script:d)\$($script:tag)_dst"
    $script:wh = "$($script:tag)_wh"
    $script:cal = "$($script:tag)_cal"

    if ($script:hasDrive) {
        New-Item -Path $script:src -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        New-Item -Path $script:dst -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
    }

    function script:Require {
        if (-not $script:hasDrive) { Set-ItResult -Skipped -Because "drive '$script:DriveName' is not connected"; return $false }
        return $true
    }
}

AfterAll {
    if ($script:hasDrive) {
        Remove-OrchWebhook -Path $script:d -Name $script:wh -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchCalendar -Path $script:d -Name $script:cal -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item -Path $script:src -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -Path $script:dst -Recurse -Force -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
    }
}

Describe 'Regression: post-1.9.1 fixes (fail on v1.9.1, pass on HEAD)' {

    It '2.5 Get-Item -LiteralPath resolves a backtick-named folder' {
        if (-not (script:Require)) { return }
        $bt = "$($script:src)\" + 'ZZbt`[1`]'
        New-Item -Path $bt -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
        (Get-Item -LiteralPath $bt -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
    }

    It 'C3 New-Item with an invalid -FeedType emits one error and creates nothing' {
        if (-not (script:Require)) { return }
        $ft = "$($script:src)\ZZft"
        $e = $null
        New-Item -Path $ft -ItemType Directory -FeedType 'Bogus' -ErrorVariable e -ErrorAction SilentlyContinue | Out-Null
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
        $e.Count | Should -Be 1
        (Get-Item -LiteralPath $ft -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
    }

    It 'A1 Get-OrchWebhook | Update-OrchWebhook re-binds events cleanly (no error, events kept)' {
        if (-not (script:Require)) { return }
        $evts = @(Get-OrchWebhookEventType -Path $script:d -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty Name -First 2)
        if ($evts.Count -lt 2) { Set-ItResult -Skipped -Because 'need >= 2 webhook event types'; return }

        New-OrchWebhook -Path $script:d -Name $script:wh -Url 'https://example.invalid/zz' `
            -Enabled 'false' -Events $evts -Confirm:$false | Out-Null
        $before = @((Get-OrchWebhook -Path $script:d -Name $script:wh).Events.EventType | Sort-Object)

        # v1.9.1: the piped WebhookEvent[] coerces to the type name, the resolver matches nothing,
        # and the empty-events PATCH is rejected by the server -> Update errors. HEAD: events re-bind
        # to themselves -> no error, events unchanged.
        $e = $null
        Get-OrchWebhook -Path $script:d -Name $script:wh |
            Update-OrchWebhook -Confirm:$false -ErrorVariable e -ErrorAction SilentlyContinue

        $after = @((Get-OrchWebhook -Path $script:d -Name $script:wh).Events.EventType | Sort-Object)
        $e | Should -BeNullOrEmpty
        $after | Should -Be $before
    }

    It 'A2 Get-OrchQueue | Update-OrchQueue preserves tags' {
        if (-not (script:Require)) { return }
        $q = 'ZZtagq'
        New-OrchQueue -Path $script:src -Name $q -Tags 'env=test', 'team=zz' -Confirm:$false | Out-Null
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
        $before = @((Get-OrchQueue -Path $script:src -Name $q).Tags.DisplayName | Sort-Object)

        Get-OrchQueue -Path $script:src -Name $q | Update-OrchQueue -Confirm:$false
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null

        $after = @((Get-OrchQueue -Path $script:src -Name $q).Tags.DisplayName | Sort-Object)
        $after | Should -Be $before
        $before | Should -Contain 'env'   # guard: the source actually had tags
    }

    It 'B2 Copy-OrchQueue carries RetryAbandonedItems to the copy' {
        if (-not (script:Require)) { return }
        $q = 'ZZcopyq'
        New-OrchQueue -Path $script:src -Name $q -RetryAbandonedItems $true -Confirm:$false | Out-Null
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
        Copy-OrchQueue -Path $script:src -Name $q -Destination $script:dst -Confirm:$false
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
        (Get-OrchQueue -Path $script:dst -Name $q).RetryAbandonedItems | Should -Be $true
    }

    It 'D1 Add-OrchCalendarDate re-adding an already-excluded date is a no-op (no duplicate)' {
        if (-not (script:Require)) { return }
        $when = (Get-Date).Date.AddDays(30)
        # Add-OrchCalendarDate upserts: first call creates the calendar with this date.
        Add-OrchCalendarDate -Path $script:d -Name $script:cal -ExcludedDate $when -Confirm:$false
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
        $before = @(Get-OrchCalendarDate -Path $script:d -Name $script:cal).Count

        # Re-add the SAME date. Pre-fix: existing dates come back local-kind while the input is UTC,
        # so Distinct()/the no-op guard treat the same day as two instants -> a needless PUT carrying a
        # duplicated date. HEAD: the existing dates are normalized to UTC, so it is a true no-op.
        Add-OrchCalendarDate -Path $script:d -Name $script:cal -ExcludedDate $when -Confirm:$false
        Clear-OrchCache -Path $script:d -ErrorAction SilentlyContinue | Out-Null
        $after = @(Get-OrchCalendarDate -Path $script:d -Name $script:cal).Count

        $before | Should -Be 1          # guard: the date was actually added
        $after | Should -Be $before     # no duplicate after re-add
    }

    It 'A Get-OrchApiTrigger fills Release.Name from ReleaseKey (the triggers endpoint rejects expand=Release)' {
        if (-not (script:Require)) { return }
        if (-not (Get-Item "$($script:d)\TestFixture_Base" -ErrorAction SilentlyContinue)) {
            Set-ItResult -Skipped -Because 'TestFixture_Base not imported (Tests\Import-Fixture.ps1 -TargetDrive Orch2)'
            return
        }
        $t = Get-OrchApiTrigger -Path "$($script:d)\TestFixture_Base" -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.ReleaseKey } | Select-Object -First 1
        if (-not $t) { Set-ItResult -Skipped -Because 'fixture has no API trigger bound to a release'; return }
        # expand=Release is rejected by the endpoint, so Release.Name is resolved client-side from
        # ReleaseKey; a trigger that carries a ReleaseKey must therefore show a non-empty Release.Name.
        $t.Release.Name | Should -Not -BeNullOrEmpty
    }
}
