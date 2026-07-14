#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration coverage for the Get-PmGroupLicense -ExportCsv ->
    Add-PmGroupLicense round trip and the -License completer.

.DESCRIPTION
    Runs against a LIVE tenant (drive from $env:UIPATHORCH_TEST_DRIVE, default
    'Orch1'). Supersedes the deferred PmLicensedGroupCsv coverage: instead of
    creating/deleting a licensed group, it discovers an EXISTING licensed group
    that has at least one available-but-unheld license. The one mutating test
    adds that single license and removes it again in a finally block, restoring
    the group's original bundle set (same self-reverting shape as
    PmLicensedUser.Tests.ps1).

    Each test self-skips (Set-ItResult -Skipped) when no drive is connected or
    no suitable group exists — the -Skip parameter can't see BeforeAll's
    $script: state because Pester evaluates it at discovery time.

    Run:  Invoke-Pester ./Tests/PmLicensedGroupCsv.Tests.ps1
#>

BeforeAll {
    $script:DriveName = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch1' }
    $script:drive     = "${script:DriveName}:"

    Import-OrchConfig | Out-Null
    $script:hasDrive = $null -ne (Get-OrchPSDrive | Where-Object Name -eq $script:DriveName)

    # Catalog friendly-name map, mirroring the module's static AvailableUserBundlesItems
    # so assertions read in human terms.
    $script:codeToName = @{
        ACCU = 'Action Center - Multiuser'; ACNU = 'Action Center - Named User'
        AKIT = 'Automation Express'; ATTUCU = 'Attended - Multiuser'; ATTUNU = 'Attended - Named User'
        CTZDEVCU = 'Citizen Developer - Multiuser'; CTZDEVNU = 'Citizen Developer - Named User'
        IDU = 'Insights Designer Users'; PMBU = 'Process Mining Business User'; PMD = 'Process Mining Developer'
        RPADEVCU = 'RPA Developer - Multiuser'; RPADEVNU = 'RPA Developer - Named User'
        RPADEVPROCU = 'Automation Developer - Multiuser'; RPADEVPRONU = 'Automation Developer - Named User'
        TSTNU = 'Tester - Named User'
    }

    function script:AvailableCodes($groupId) {
        (Invoke-OrchApi -Path $script:drive -Portal `
            -ApiPath "/api/license/accountant/UserLicense/group/?id=$groupId" -Raw |
            ConvertFrom-Json).availableUserBundles.code
    }

    $script:grp = $null
    $script:unheldCode = $null
    $script:unheldName = $null
    # A group with >=2 available-but-unheld licenses, used by the aggregation test.
    $script:grp2 = $null
    $script:unheld2Names = $null
    $script:unheld2Codes = $null
    if ($script:hasDrive) {
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        # Skip orphans: a dangling license allocation whose group was deleted from the directory.
        # It has no group to ask about, so the licensing endpoint 404s on its id ("Cannot resolve
        # groupIds=[...] in CIS") and the whole setup dies with it. Get-PmGroupLicense flags them
        # (orphan = true, empty name) and leaves them out of -ExportCsv for the same reason.
        foreach ($g in (Get-PmGroupLicense -Path $script:drive | Where-Object { -not $_.orphan })) {
            $held   = @($g.userBundleLicenses)
            $unheld = @(script:AvailableCodes $g.id |
                Where-Object { $_ -and $_ -notin $held -and $script:codeToName.ContainsKey($_) })
            if ($unheld.Count -gt 0 -and $null -eq $script:grp) {
                $script:grp        = $g
                $script:unheldCode = $unheld[0]
                $script:unheldName = $script:codeToName[$unheld[0]]
            }
            # Prefer an ASCII-named group for the case-insensitivity assertion.
            if ($unheld.Count -ge 2 -and $null -eq $script:grp2 -and $g.name -match '^[\x20-\x7e]+$') {
                $script:grp2         = $g
                $script:unheld2Codes = @($unheld[0], $unheld[1])
                $script:unheld2Names = @($script:codeToName[$unheld[0]], $script:codeToName[$unheld[1]])
            }
        }
    }
    $script:hasGroup  = $null -ne $script:grp
    $script:hasGroup2 = $null -ne $script:grp2

    function script:RequireGroup {
        if (-not $script:hasDrive) { Set-ItResult -Skipped -Because "drive '$script:DriveName' is not connected"; return $false }
        if (-not $script:hasGroup) { Set-ItResult -Skipped -Because "no licensed group with an available-but-unheld license on $script:drive"; return $false }
        return $true
    }

    function script:RequireGroup2 {
        if (-not $script:hasDrive)  { Set-ItResult -Skipped -Because "drive '$script:DriveName' is not connected"; return $false }
        if (-not $script:hasGroup2) { Set-ItResult -Skipped -Because "no ASCII-named licensed group with >=2 available-but-unheld licenses on $script:drive"; return $false }
        return $true
    }
}

Describe 'Get-PmGroupLicense -ExportCsv' {
    It 'exports Path/GroupName/License columns only' {
        if (-not (script:RequireGroup)) { return }
        $csv = Join-Path ([IO.Path]::GetTempPath()) "pmlg_$([guid]::NewGuid().ToString('N')).csv"
        try {
            Get-PmGroupLicense -Path $script:drive -ExportCsv $csv | Out-Null
            $cols = (Import-Csv $csv | Select-Object -First 1).PSObject.Properties.Name | Sort-Object
            $cols | Should -Be (@('GroupName','License','Path') | Sort-Object)
        } finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'License column holds friendly names, not raw codes' {
        if (-not (script:RequireGroup)) { return }
        $csv = Join-Path ([IO.Path]::GetTempPath()) "pmlg_$([guid]::NewGuid().ToString('N')).csv"
        try {
            Get-PmGroupLicense -Path $script:drive -ExportCsv $csv | Out-Null
            $rows = Import-Csv $csv
            ($rows | Where-Object { $script:codeToName.ContainsKey($_.License) }) | Should -BeNullOrEmpty
        } finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }
}

Describe 'Add-PmGroupLicense -License completer' {
    It 'excludes held licenses and offers the unheld one' {
        if (-not (script:RequireGroup)) { return }
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        $line = "Add-PmGroupLicense -Path $($script:drive) -GroupName '$($script:grp.name)' -License "
        $matches = (TabExpansion2 $line $line.Length).CompletionMatches |
            ForEach-Object { $_.CompletionText.Trim("'") }

        foreach ($code in @($script:grp.userBundleLicenses)) {
            if ($script:codeToName.ContainsKey($code)) {
                $matches | Should -Not -Contain $script:codeToName[$code]
            }
        }
        $matches | Should -Contain $script:unheldName
    }
}

Describe 'ExportCsv -> Add round trip' {
    It 're-importing the exported CSV is idempotent (-WhatIf makes no change)' {
        if (-not (script:RequireGroup)) { return }
        $csv = Join-Path ([IO.Path]::GetTempPath()) "pmlg_$([guid]::NewGuid().ToString('N')).csv"
        try {
            Get-PmGroupLicense -Path $script:drive -ExportCsv $csv | Out-Null
            { Import-Csv $csv | Add-PmGroupLicense -WhatIf } | Should -Not -Throw
        } finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'adds an available-but-unheld license then restores the group' {
        if (-not (script:RequireGroup)) { return }
        $before = @((Get-PmGroupLicense -Path $script:drive |
            Where-Object name -eq $script:grp.name).userBundleLicenses | Sort-Object)
        try {
            Add-PmGroupLicense -Path $script:drive -GroupName $script:grp.name `
                -License $script:unheldName -Confirm:$false | Out-Null
            Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
            $after = @((Get-PmGroupLicense -Path $script:drive |
                Where-Object name -eq $script:grp.name).userBundleLicenses)
            $after | Should -Contain $script:unheldCode
        } finally {
            Remove-PmGroupLicense -Path $script:drive -GroupName $script:grp.name `
                -License $script:unheldName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
            Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        }
        $restored = @((Get-PmGroupLicense -Path $script:drive |
            Where-Object name -eq $script:grp.name).userBundleLicenses | Sort-Object)
        $restored | Should -Be $before
    }
}

Describe 'Multi-row aggregation (same group, case-insensitive)' {
    It 'merges two CSV rows naming one group in different case into a single applied set' {
        if (-not (script:RequireGroup2)) { return }
        $name = $script:grp2.name
        $lower = $name.ToLowerInvariant()
        $upper = $name.ToUpperInvariant()
        $codeA, $codeB = $script:unheld2Codes
        $nameA, $nameB = $script:unheld2Names

        $before = @((Get-PmGroupLicense -Path $script:drive |
            Where-Object name -eq $name).userBundleLicenses | Sort-Object)
        $csv = Join-Path ([IO.Path]::GetTempPath()) "pmlg_agg_$([guid]::NewGuid().ToString('N')).csv"
        try {
            # Same group, different case per row, a different license each — these
            # must aggregate (atomic-replace API: one PUT carrying BOTH licenses).
            @(
                [pscustomobject]@{ Path = $script:drive; GroupName = $lower; License = $nameA }
                [pscustomobject]@{ Path = $script:drive; GroupName = $upper; License = $nameB }
            ) | Export-Csv $csv -NoTypeInformation -Encoding utf8

            Import-Csv $csv | Add-PmGroupLicense -Confirm:$false | Out-Null
            Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

            $after = @((Get-PmGroupLicense -Path $script:drive |
                Where-Object name -eq $name).userBundleLicenses)
            # Both licenses present — neither row clobbered the other.
            $after | Should -Contain $codeA
            $after | Should -Contain $codeB
        } finally {
            Remove-Item $csv -ErrorAction SilentlyContinue
            Remove-PmGroupLicense -Path $script:drive -GroupName $name `
                -License $nameA, $nameB -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
            Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        }
        $restored = @((Get-PmGroupLicense -Path $script:drive |
            Where-Object name -eq $name).userBundleLicenses | Sort-Object)
        $restored | Should -Be $before
    }
}
