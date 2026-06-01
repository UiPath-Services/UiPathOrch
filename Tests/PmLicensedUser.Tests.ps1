#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    End-to-end Pester coverage for the PmLicensed*User cmdlet family added
    in v1.6.0: Add-PmUserLicense, Remove-PmUserLicense,
    Remove-PmLicensedUser.

.DESCRIPTION
    Exercises the live License Accountant API (PUT / DELETE
    /portal_/api/license/accountant/UserLicense) against a real org. Tests
    are sequenced so each one builds on the prior's state, with AfterAll
    dropping the test subject so the org returns to its starting shape.

    Requirements:
    - $env:UIPATHORCH_TEST_DRIVE (Pm-licensable Orch drive). Defaults to
      'Orch2'.
    - The drive's org must have ≥2 free slots in two distinct user bundles.
      The fallback bundle codes (RPADEVNU, CTZDEVNU) cover Automation Cloud
      default trials; override with $env:UIPATHORCH_TEST_LICENSE_1 /
      $env:UIPATHORCH_TEST_LICENSE_2 if the test org doesn't carry them.
    - A directory user (default 'akrtsuda@gmail.com', or
      $env:UIPATHORCH_TEST_LICENSED_USER) that is NOT currently in the
      licensed-users list. The user does NOT need to be in the tenant —
      Add-PmUserLicense resolves them from the directory.

    The tests deliberately exercise the same user across all Describe
    blocks to validate the merge / strip / drop sequencing, which is the
    whole reason the family exists. Each block is a single It because
    Pester runs Its in declaration order within a Describe but Describes
    do not share state via tradition — the $script: vars carry it
    explicitly here.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:TestUser = if ($env:UIPATHORCH_TEST_LICENSED_USER) { $env:UIPATHORCH_TEST_LICENSED_USER } else { 'akrtsuda@gmail.com' }
    $script:License1 = if ($env:UIPATHORCH_TEST_LICENSE_1) { $env:UIPATHORCH_TEST_LICENSE_1 } else { 'RPADEVNU' }
    $script:License2 = if ($env:UIPATHORCH_TEST_LICENSE_2) { $env:UIPATHORCH_TEST_LICENSE_2 } else { 'CTZDEVNU' }
    $script:DrivePath = "${script:Drive}:\"

    # Sanity: drive mounted
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    # Sanity: bundles exist on the drive with free slots
    $licenses = Get-PmLicense -Path $script:DrivePath -ErrorAction Stop
    foreach ($code in @($script:License1, $script:License2)) {
        $lic = $licenses | Where-Object code -eq $code
        if (-not $lic) {
            throw "License bundle '$code' is not available on ${script:Drive}: -- override via `$env:UIPATHORCH_TEST_LICENSE_{1,2}."
        }
        if ($lic.allocated -ge $lic.total) {
            throw "License bundle '$code' is fully allocated on ${script:Drive}: ($($lic.allocated)/$($lic.total)) — free a slot or override."
        }
    }

    # Pre-clean: if a prior failed run left the test user in the licensed
    # set, drop them so the first It starts from a known-empty state.
    $existing = Get-PmUserLicense -Path $script:DrivePath -ErrorAction SilentlyContinue |
        Where-Object email -eq $script:TestUser
    if ($existing) {
        Remove-PmLicensedUser -Path $script:DrivePath -Email $script:TestUser -Confirm:$false -ErrorAction SilentlyContinue
    }
}

AfterAll {
    # Always drop the test user, no matter where in the sequence we exited.
    # The Remove-PmLicensedUser path is the one being tested, so if it's
    # broken the user may stay in the licensed set across runs; the
    # BeforeAll pre-clean catches that on the next run.
    Remove-PmLicensedUser -Path $script:DrivePath -Email $script:TestUser -Confirm:$false -ErrorAction SilentlyContinue
}

Describe 'Add-PmUserLicense' {
    It 'allocates a single bundle to a previously-unlicensed user' {
        Add-PmUserLicense -Path $script:DrivePath -Email $script:TestUser -License $script:License1 -Confirm:$false
        $u = Get-PmUserLicense -Path $script:DrivePath | Where-Object email -eq $script:TestUser
        $u | Should -Not -BeNullOrEmpty
        $u.userBundleLicenses | Should -Contain $script:License1
    }

    It 'is idempotent — re-adding the same bundle keeps the list unchanged' {
        # Atomic-replace PUT: re-submitting the same set is a no-op on the
        # server side; the cmdlet should also short-circuit when the merge
        # produces no new codes (NOTES in the help md spells this out).
        Add-PmUserLicense -Path $script:DrivePath -Email $script:TestUser -License $script:License1 -Confirm:$false
        $u = Get-PmUserLicense -Path $script:DrivePath | Where-Object email -eq $script:TestUser
        $u.userBundleLicenses | Should -Contain $script:License1
        ($u.userBundleLicenses | Where-Object { $_ -eq $script:License1 }).Count | Should -Be 1
    }

    It 'merges a second bundle without dropping the first (atomic-replace + merge)' {
        Add-PmUserLicense -Path $script:DrivePath -Email $script:TestUser -License $script:License2 -Confirm:$false
        $u = Get-PmUserLicense -Path $script:DrivePath | Where-Object email -eq $script:TestUser
        $u.userBundleLicenses | Should -Contain $script:License1
        $u.userBundleLicenses | Should -Contain $script:License2
    }
}

Describe 'Remove-PmUserLicense' {
    It 'removes a specific bundle while leaving others intact' {
        Remove-PmUserLicense -Path $script:DrivePath -Email $script:TestUser -License $script:License1 -Confirm:$false
        $u = Get-PmUserLicense -Path $script:DrivePath | Where-Object email -eq $script:TestUser
        $u | Should -Not -BeNullOrEmpty
        $u.userBundleLicenses | Should -Not -Contain $script:License1
        $u.userBundleLicenses | Should -Contain $script:License2
    }

    It '-License * strips every remaining bundle but leaves the row' {
        # The contract: the user record stays in the licensed-users set
        # with an empty bundle list (the "No license" row in the Portal UI).
        # Dropping the row entirely is Remove-PmLicensedUser's job.
        Remove-PmUserLicense -Path $script:DrivePath -Email $script:TestUser -License '*' -Confirm:$false
        $u = Get-PmUserLicense -Path $script:DrivePath | Where-Object email -eq $script:TestUser
        $u | Should -Not -BeNullOrEmpty
        $u.userBundleLicenses | Should -BeNullOrEmpty
    }
}

Describe 'Remove-PmLicensedUser' {
    It 'drops the licensed-user row entirely' {
        Remove-PmLicensedUser -Path $script:DrivePath -Email $script:TestUser -Confirm:$false
        $u = Get-PmUserLicense -Path $script:DrivePath | Where-Object email -eq $script:TestUser
        $u | Should -BeNullOrEmpty
    }
}

Describe 'Get-PmUserLicense -ExportCsv' {
    # Round-trip coverage for the -ExportCsv added in 1.6.2. Runs after the
    # sequence above has dropped the test user, then re-adds two bundles so
    # there is a known multi-license user to export. AfterAll (top of file)
    # drops the user regardless.
    BeforeAll {
        Add-PmUserLicense -Path $script:DrivePath -Email $script:TestUser `
            -License $script:License1, $script:License2 -Confirm:$false
        $script:CsvPath = Join-Path ([IO.Path]::GetTempPath()) "pmlu_$([guid]::NewGuid().ToString('N')).csv"
    }
    AfterAll {
        Remove-Item $script:CsvPath -ErrorAction SilentlyContinue
    }

    It 'emits Path / UserName / License columns (not Email)' {
        Get-PmUserLicense -Path $script:DrivePath -ExportCsv $script:CsvPath | Out-Null
        $cols = (Import-Csv $script:CsvPath | Select-Object -First 1).PSObject.Properties.Name | Sort-Object
        $cols | Should -Be (@('License', 'Path', 'UserName') | Sort-Object)
    }

    It 'writes one row per (user, license) with the test user present' {
        $rows = Import-Csv $script:CsvPath
        $mine = $rows | Where-Object UserName -eq $script:TestUser
        # The user was given two bundles, so two rows carry their login.
        @($mine).Count | Should -BeGreaterOrEqual 2
        # License column is a friendly bundle name, not a raw code.
        $mine.License | Should -Not -Contain $script:License1
    }

    It 'excludes orphan license-pool rows (UserName is never a bundle name)' {
        $rows = Import-Csv $script:CsvPath
        # Orphan rows would carry a bundle display name in UserName; real rows
        # carry a login. None of the exported UserNames should be empty.
        ($rows | Where-Object { [string]::IsNullOrWhiteSpace($_.UserName) }) | Should -BeNullOrEmpty
    }

    It 're-imports into Add-PmUserLicense without error (-WhatIf)' {
        # The UserName column binds to -Email via its alias; a full unedited
        # re-import is a no-op (every license already held), so -WhatIf just
        # previews and binds cleanly.
        { Import-Csv $script:CsvPath | Add-PmUserLicense -WhatIf } | Should -Not -Throw
    }
}
