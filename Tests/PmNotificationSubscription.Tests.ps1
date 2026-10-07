#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    End-to-end Pester coverage for the PmNotificationSubscription cmdlet family:
    Get-/Set-/Copy-PmNotificationSubscription, their argument completers, and the
    -ExportCsv | Import-Csv | Set round-trip.

.DESCRIPTION
    Exercises the live notification service (notificationservice_/usersubscription
    service) against a real org. Subscriptions are per connected user, so the tests
    toggle one non-mandatory (topic, mode) and restore it in AfterAll.

    Requirements:
    - $env:UIPATHORCH_TEST_DRIVE (Automation Cloud Pm drive). Defaults to 'Orch2'.
    - The org must expose at least one non-mandatory notification topic (every AC
      org does; the tests pick one dynamically).

.NOTES
    Run with: Invoke-Pester -Path Tests\PmNotificationSubscription.Tests.ps1 -Output Detailed
    Mutating but self-restoring; safe on a normal drive.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:DrivePath = "${script:Drive}:"

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    # A standalone server has no notification service (Get-PmNotificationSubscription says so);
    # Automation Cloud and Automation Suite do. Every test then skips.
    $script:NoNotifications = if ((Get-OrchPSDrive | Where-Object Name -eq $script:Drive).Edition -eq 'OnPremises') {
        "${script:Drive}: is a standalone server, which has no notification service"
    }
    function script:SkipWithoutNotifications {
        if ($script:NoNotifications) { Set-ItResult -Skipped -Because $script:NoNotifications; return $true }
        return $false
    }
    if ($script:NoNotifications) { return }

    # Pick a non-mandatory (topic, mode) to toggle, and remember its current state.
    $script:Subject = Get-PmNotificationSubscription -Path $script:DrivePath |
        Where-Object { -not $_.IsMandatory -and $_.Topic -and $_.Mode } |
        Select-Object -First 1
    if (-not $script:Subject) { throw "No non-mandatory notification topic found on '$script:DrivePath'." }
    $script:Topic = $script:Subject.Topic
    $script:Mode = $script:Subject.Mode
    $script:Original = [bool]$script:Subject.IsSubscribed

    function Get-State {
        param([string]$Topic, [string]$Mode)
        [bool]((Get-PmNotificationSubscription -Path $script:DrivePath |
            Where-Object { $_.Topic -eq $Topic -and $_.Mode -eq $Mode }).IsSubscribed)
    }

    function Complete-Parameter {
        param([string]$InputScript)
        ([System.Management.Automation.CommandCompletion]::CompleteInput(
            $InputScript, $InputScript.Length, $null)).CompletionMatches
    }
}

Describe 'Get-PmNotificationSubscription' {
    It 'returns rows with the expected shape' {
        if (script:SkipWithoutNotifications) { return }
        $rows = @(Get-PmNotificationSubscription -Path $script:DrivePath)
        $rows.Count | Should -BeGreaterThan 0
        $names = $rows[0].PSObject.Properties.Name
        $names | Should -Contain 'Topic'
        $names | Should -Contain 'Mode'
        $names | Should -Contain 'IsSubscribed'
        $rows[0].Mode | Should -BeIn @('InApp', 'Email')
    }

    It 'filters by -Mode' {

        if (script:SkipWithoutNotifications) { return }
        $rows = @(Get-PmNotificationSubscription -Path $script:DrivePath -Mode Email)
        $rows | Where-Object { $_.Mode -ne 'Email' } | Should -BeNullOrEmpty
    }

    It 'filters by -Publisher (wildcard)' {

        if (script:SkipWithoutNotifications) { return }
        $rows = @(Get-PmNotificationSubscription -Path $script:DrivePath -Publisher 'App*')
        $rows.Count | Should -BeGreaterThan 0
        $rows | Where-Object { $_.Publisher -notlike 'App*' } | Should -BeNullOrEmpty
    }
}

Describe 'Set-PmNotificationSubscription' {
    It 'toggles a subscription and reads it back' {
        if (script:SkipWithoutNotifications) { return }
        $target = -not $script:Original
        Set-PmNotificationSubscription $script:Topic $script:Mode "$target" -Path $script:DrivePath -Confirm:$false | Out-Null
        Get-State $script:Topic $script:Mode | Should -Be $target
    }

    It 'accepts the string "false"/"true" (CSV style)' {

        if (script:SkipWithoutNotifications) { return }
        Set-PmNotificationSubscription $script:Topic $script:Mode 'false' -Path $script:DrivePath -Confirm:$false | Out-Null
        Get-State $script:Topic $script:Mode | Should -BeFalse
        Set-PmNotificationSubscription $script:Topic $script:Mode 'true' -Path $script:DrivePath -Confirm:$false | Out-Null
        Get-State $script:Topic $script:Mode | Should -BeTrue
    }

    It 'errors on an invalid -Subscribed value' {

        if (script:SkipWithoutNotifications) { return }
        { Set-PmNotificationSubscription $script:Topic $script:Mode 'maybe' -Path $script:DrivePath -Confirm:$false -ErrorAction Stop } |
            Should -Throw
    }

    It 'errors on an unknown topic' {

        if (script:SkipWithoutNotifications) { return }
        { Set-PmNotificationSubscription 'No.Such.Topic' Email true -Path $script:DrivePath -Confirm:$false -ErrorAction Stop } |
            Should -Throw
    }
}

Describe 'CSV round-trip (-ExportCsv | Import-Csv | Set)' {
    It 'exports the binding columns and re-applies via Import-Csv | Set' {
        if (script:SkipWithoutNotifications) { return }
        $csv = Join-Path ([System.IO.Path]::GetTempPath()) "pmnotif_$PID.csv"
        try {
            Get-PmNotificationSubscription -Path $script:DrivePath -Publisher 'App*' -ExportCsv $csv
            Test-Path $csv | Should -BeTrue

            $imported = @(Import-Csv $csv)
            $imported.Count | Should -BeGreaterThan 0
            $cols = $imported[0].PSObject.Properties.Name
            'Path', 'Topic', 'Mode', 'IsSubscribed' | ForEach-Object { $cols | Should -Contain $_ }
            $imported[0].IsSubscribed | Should -BeIn @('True', 'False')

            $applied = @($imported | Set-PmNotificationSubscription -Path $script:DrivePath -Confirm:$false)
            $applied.Count | Should -Be $imported.Count
        }
        finally {
            Remove-Item $csv -ErrorAction SilentlyContinue
        }
    }
}

Describe 'Argument completers' {
    It '-Publisher completes publisher names' {
        if (script:SkipWithoutNotifications) { return }
        $c = Complete-Parameter "Get-PmNotificationSubscription -Path $script:DrivePath -Publisher "
        @($c.CompletionText) | Should -Contain 'Apps'
    }

    It '-Publisher excludes an already-entered wildcard value' {

        if (script:SkipWithoutNotifications) { return }
        $c = Complete-Parameter "Get-PmNotificationSubscription -Path $script:DrivePath -Publisher Apps,"
        @($c.CompletionText) | Should -Not -Contain 'Apps'
    }

    It '-Mode completes InApp / Email' {

        if (script:SkipWithoutNotifications) { return }
        $c = Complete-Parameter "Set-PmNotificationSubscription -Path $script:DrivePath -Topic x -Mode "
        @($c.CompletionText) | Should -Contain 'InApp'
        @($c.CompletionText) | Should -Contain 'Email'
    }

    It '-Topic completes topic names' {

        if (script:SkipWithoutNotifications) { return }
        $c = Complete-Parameter "Set-PmNotificationSubscription -Path $script:DrivePath -Topic Apps."
        @($c.CompletionText) | Should -Contain 'Apps.Shared'
    }

    It '-Subscribed completes true / false' {

        if (script:SkipWithoutNotifications) { return }
        $c = Complete-Parameter "Set-PmNotificationSubscription -Path $script:DrivePath -Topic x -Mode Email -Subscribed "
        @($c.CompletionText) | Should -Contain 'True'
        @($c.CompletionText) | Should -Contain 'False'
    }
}

Describe 'Copy-PmNotificationSubscription' {
    It 'is a no-op when source and destination are the same organization' {
        if (script:SkipWithoutNotifications) { return }
        # Same partition -> nothing copied, no error.
        $r = Copy-PmNotificationSubscription $script:DrivePath -Path $script:DrivePath -Confirm:$false 2>&1
        $r | Should -BeNullOrEmpty
    }
}

AfterAll {
    if ($script:Topic) {
        Set-PmNotificationSubscription $script:Topic $script:Mode "$($script:Original)" `
            -Path $script:DrivePath -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
    }
}
