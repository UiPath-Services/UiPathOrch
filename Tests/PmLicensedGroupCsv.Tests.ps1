#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration coverage for the Get-PmLicensedGroup -ExportCsv ->
    Add-PmLicenseToPmLicensedGroup round trip and the -License completer.

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
    if ($script:hasDrive) {
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        foreach ($g in (Get-PmLicensedGroup -Path $script:drive)) {
            $held   = @($g.userBundleLicenses)
            $unheld = @(script:AvailableCodes $g.id |
                Where-Object { $_ -and $_ -notin $held -and $script:codeToName.ContainsKey($_) })
            if ($unheld.Count -gt 0) {
                $script:grp        = $g
                $script:unheldCode = $unheld[0]
                $script:unheldName = $script:codeToName[$unheld[0]]
                break
            }
        }
    }
    $script:hasGroup = $null -ne $script:grp

    function script:RequireGroup {
        if (-not $script:hasDrive) { Set-ItResult -Skipped -Because "drive '$script:DriveName' is not connected"; return $false }
        if (-not $script:hasGroup) { Set-ItResult -Skipped -Because "no licensed group with an available-but-unheld license on $script:drive"; return $false }
        return $true
    }
}

Describe 'Get-PmLicensedGroup -ExportCsv' {
    It 'exports Path/GroupName/License columns only' {
        if (-not (script:RequireGroup)) { return }
        $csv = Join-Path ([IO.Path]::GetTempPath()) "pmlg_$([guid]::NewGuid().ToString('N')).csv"
        try {
            Get-PmLicensedGroup -Path $script:drive -ExportCsv $csv | Out-Null
            $cols = (Import-Csv $csv | Select-Object -First 1).PSObject.Properties.Name | Sort-Object
            $cols | Should -Be (@('GroupName','License','Path') | Sort-Object)
        } finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'License column holds friendly names, not raw codes' {
        if (-not (script:RequireGroup)) { return }
        $csv = Join-Path ([IO.Path]::GetTempPath()) "pmlg_$([guid]::NewGuid().ToString('N')).csv"
        try {
            Get-PmLicensedGroup -Path $script:drive -ExportCsv $csv | Out-Null
            $rows = Import-Csv $csv
            ($rows | Where-Object { $script:codeToName.ContainsKey($_.License) }) | Should -BeNullOrEmpty
        } finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }
}

Describe 'Add-PmLicenseToPmLicensedGroup -License completer' {
    It 'excludes held licenses and offers the unheld one' {
        if (-not (script:RequireGroup)) { return }
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        $line = "Add-PmLicenseToPmLicensedGroup -Path $($script:drive) -GroupName '$($script:grp.name)' -License "
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
            Get-PmLicensedGroup -Path $script:drive -ExportCsv $csv | Out-Null
            { Import-Csv $csv | Add-PmLicenseToPmLicensedGroup -WhatIf } | Should -Not -Throw
        } finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'adds an available-but-unheld license then restores the group' {
        if (-not (script:RequireGroup)) { return }
        $before = @((Get-PmLicensedGroup -Path $script:drive |
            Where-Object name -eq $script:grp.name).userBundleLicenses | Sort-Object)
        try {
            Add-PmLicenseToPmLicensedGroup -Path $script:drive -GroupName $script:grp.name `
                -License $script:unheldName -Confirm:$false | Out-Null
            Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
            $after = @((Get-PmLicensedGroup -Path $script:drive |
                Where-Object name -eq $script:grp.name).userBundleLicenses)
            $after | Should -Contain $script:unheldCode
        } finally {
            Remove-PmLicenseFromPmLicensedGroup -Path $script:drive -GroupName $script:grp.name `
                -License $script:unheldName -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
            Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        }
        $restored = @((Get-PmLicensedGroup -Path $script:drive |
            Where-Object name -eq $script:grp.name).userBundleLicenses | Sort-Object)
        $restored | Should -Be $before
    }
}
