#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration test for Compare-OrchCalendar — the tenant-scoped path through the shared
    TenantCompare engine (mode dispatch, "<=" / "=>" / "<>" / "==", broadcast, -Property),
    end-to-end through the real provider.

.DESCRIPTION
    Requires connected Orch2: (writable test target) and Orch1: (a different, read-only tenant
    used only to demonstrate "<=" / "=>" — nothing is created there). Calendars are created on
    Orch2 via Add-OrchCalendarDate under a random "PesterCmpCal_XXXX_" prefix and removed in
    AfterAll. The random prefix guarantees Orch1 has no matching calendar.
#>

BeforeAll {
    $script:Drive = 'Orch2'
    $script:Other = 'Orch1'
    $script:Prefix = "PesterCmpCal_$(Get-Random -Maximum 9999)_"

    $script:Same    = "${script:Prefix}Same"
    $script:Changed = "${script:Prefix}Changed"
    $script:Other2  = "${script:Prefix}Other"

    $script:D1 = [datetime]'2030-01-01'
    $script:D2 = [datetime]'2030-02-02'

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    Get-PSDrive $script:Other -ErrorAction Stop | Out-Null

    Add-OrchCalendarDate -Path "${script:Drive}:" -Name $script:Same    -ExcludedDate $script:D1 | Out-Null
    Add-OrchCalendarDate -Path "${script:Drive}:" -Name $script:Changed -ExcludedDate $script:D1 | Out-Null
    Add-OrchCalendarDate -Path "${script:Drive}:" -Name $script:Other2  -ExcludedDate $script:D2 | Out-Null
    Clear-OrchCache
}

AfterAll {
    Remove-OrchCalendar -Path "${script:Drive}:" -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Compare-OrchCalendar (tenant-scoped)' {
    It 'reports "==" for a calendar compared to itself (name-match)' {
        $r = Compare-OrchCalendar -Name "${script:Prefix}*" -Path "${script:Drive}:" -DifferencePath "${script:Drive}:" -IncludeEqual
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '=='
    }

    It 'reports "<=" for a calendar present only on the reference drive' {
        $r = Compare-OrchCalendar -Name "${script:Prefix}*" -Path "${script:Drive}:" -DifferencePath "${script:Other}:"
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '<='
    }

    It 'reports "=>" for a calendar present only on the difference drive' {
        $r = Compare-OrchCalendar -Name "${script:Prefix}*" -Path "${script:Other}:" -DifferencePath "${script:Drive}:"
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '=>'
    }

    It 'reports "<>" with an ExcludedDates difference (broadcast)' {
        $r = Compare-OrchCalendar -Path "${script:Drive}:" -Name $script:Changed `
            -DifferencePath "${script:Drive}:" -DifferenceName $script:Other2 -Property ExcludedDates
        $r.SideIndicator | Should -Be '<>'
        (($r.Differences | Where-Object Property -eq 'ExcludedDates') | Measure-Object).Count | Should -Be 1
    }

    It 'reports "==" for equal dates under different names (broadcast)' {
        $r = Compare-OrchCalendar -Path "${script:Drive}:" -Name $script:Same `
            -DifferencePath "${script:Drive}:" -DifferenceName $script:Changed -Property ExcludedDates -IncludeEqual
        $r.SideIndicator | Should -Be '=='
    }

    It 'errors when the named difference calendar does not exist' {
        { Compare-OrchCalendar -Path "${script:Drive}:" -Name $script:Same `
            -DifferencePath "${script:Drive}:" -DifferenceName "${script:Prefix}Nope" -ErrorAction Stop } |
            Should -Throw
    }

    It 'warns on an unrecognized -Property name' {
        Compare-OrchCalendar -Name "${script:Prefix}*" -Path "${script:Drive}:" -DifferencePath "${script:Drive}:" `
            -Property 'Bogus' -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Match 'unrecognized'
    }
}
