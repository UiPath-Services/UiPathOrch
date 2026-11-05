#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Live Pester coverage for copying Test Manager projects: Copy-Item Tm:\PRJ Tm:\NEW and
    Copy-TmTestCase / Copy-TmRequirement / Copy-TmTestSet (TmCopy.cs, scratch/design-copy-tm.md).

.DESCRIPTION
    Builds a source project with the cases the matching rules have to get right: names that
    differ only in case, duplicate names, foreign references, steps, labels, requirements and
    test sets with assignments, and project settings. Then copies it and checks the result
    item by item, by (name, foreign reference), not by count alone.

    Every project it creates has the prefix "PT<nnnn>" and is removed in AfterAll; nothing
    else in the Test Manager drive is touched.

    Requirements:
    - $env:UIPATHORCH_TEST_TM_DRIVE: a Test Manager drive (UiPathOrchTm) with permission to
      create and delete projects. Defaults to 'yotsudatestTm'. Every test skips when the drive
      is missing or has no Test Manager service.

.NOTES
    Run with: Invoke-Pester -Path Tests\TmCopy.Tests.ps1 -Output Detailed
    Takes a few minutes: each Copy-Item copies the whole project.
#>

BeforeAll {
    $script:Tm = if ($env:UIPATHORCH_TEST_TM_DRIVE) { $env:UIPATHORCH_TEST_TM_DRIVE } else { 'yotsudatestTm' }

    $script:NoTm = $null
    if (-not (Get-PSDrive $script:Tm -PSProvider UiPathOrchTm -ErrorAction SilentlyContinue)) {
        $script:NoTm = "no Test Manager drive '$($script:Tm):' (set UIPATHORCH_TEST_TM_DRIVE)"
    } else {
        try { $null = Get-ChildItem "$($script:Tm):\" -ErrorAction Stop }
        catch { $script:NoTm = "$($script:Tm): has no Test Manager service: $($_.Exception.Message)" }
    }
    function script:SkipWithoutTm {
        if ($script:NoTm) { Set-ItResult -Skipped -Because $script:NoTm; return $true }
        return $false
    }
    if ($script:NoTm) { return }

    $script:Stem = 'PT' + (Get-Random -Minimum 1000 -Maximum 9999)
    $script:Created = [System.Collections.Generic.List[string]]::new()

    function script:P([string]$suffix) { "$($script:Tm):\$($script:Stem)$suffix" }

    function script:New-Project([string]$suffix) {
        $null = New-Item (P $suffix) -ErrorAction Stop
        $script:Created.Add($suffix)
        (Get-Item (P $suffix)).id
    }

    # Test Manager API on a project (the cmdlets cannot set a foreign reference, nor tell
    # 'Login' from 'login' when addressing by name).
    function script:Api([string]$suffix, [string]$rel, [string]$method = 'Get', $body = $null) {
        $path = P $suffix
        $id = (Get-Item $path).id
        $a = @{ Path = $path; ApiPath = "/testmanager_/api/v2/$id/$rel"; Method = $method; Raw = $true; ErrorAction = 'Stop' }
        if ($null -ne $body) { $a.Body = ConvertTo-Json -InputObject $body -Depth 5 }
        $r = Invoke-OrchApi @a
        if ($r) { $r | ConvertFrom-Json }
    }

    # A project's content as sorted strings, so two projects compare with Should -Be. Test
    # cases are "name|foreignReference|step;step", assignments "container|name|foreignReference".
    function script:Get-Shape([string]$suffix) {
        $path = P $suffix
        $tcs = @(Get-TmTestCase -Path $path *)
        [pscustomobject]@{
            TestCases    = @($tcs | ForEach-Object {
                    $steps = (@(Api $suffix "teststeps/testcase/$($_.id)") | ForEach-Object { $_ } | ForEach-Object description) -join ';'
                    "$($_.name)|$($_.foreignReference)|$steps" } | Sort-Object -CaseSensitive)
            Requirements = @(Get-TmRequirement -Path $path * | ForEach-Object { "$($_.name)|$($_.foreignReference)" } | Sort-Object -CaseSensitive)
            TestSets     = @(Get-TmTestSet -Path $path * | ForEach-Object { $_.name } | Sort-Object -CaseSensitive)
            InSets       = @(Get-TmTestSetTestCase -Path $path | ForEach-Object { "$($_.TestSet)|$($_.TestCase)|$($_.foreignReference)" } | Sort-Object -CaseSensitive)
            InReqs       = @(Get-TmRequirementTestCase -Path $path | ForEach-Object { "$($_.Requirement)|$($_.TestCase)|$($_.foreignReference)" } | Sort-Object -CaseSensitive)
            Labels       = @($tcs | ForEach-Object {
                    $tc = $_
                    @(Api $suffix "objectlabels/testCase/$($tc.id)/all") | ForEach-Object { $_ } |
                        Where-Object name -ne 'manual' | ForEach-Object { "$($tc.name)|$($tc.foreignReference)|$($_.name)" } } | Sort-Object -CaseSensitive)
        }
    }

    # ---- the source project ------------------------------------------------------------------
    $srcId = New-Project 'S'
    function script:NewTc([string]$name, [string]$fr) {
        (Api 'S' 'testcases' Post @{ projectId = $srcId; name = $name; foreignReference = $fr }).id
    }
    function script:NewStep([string]$tc, [string]$text) {
        $null = Api 'S' 'teststeps' Post @{ testCaseId = $tc; orderNo = -1; description = $text; expectedResult = 'ok' }
    }
    $tcUpper = NewTc 'Login' $null; NewStep $tcUpper 'upper step'
    $tcLower = NewTc 'login' $null; NewStep $tcLower 'lower step'
    $tcJ1 = NewTc 'LOGIN' 'J-1'
    $tcDup1 = NewTc 'Dup' $null; NewStep $tcDup1 'dup step'
    $tcDup2 = NewTc 'Dup' $null
    $tcDupJ2 = NewTc 'Dup' 'J-2'
    $null = NewTc 'Logout' $null  # in no container; the last test gives it two namesakes at a destination

    $reqU = (Api 'S' 'requirements' Post @{ projectId = $srcId; name = 'Req'; foreignReference = $null }).id
    $reqL = (Api 'S' 'requirements' Post @{ projectId = $srcId; name = 'req'; foreignReference = $null }).id
    $reqA = (Api 'S' 'requirements' Post @{ projectId = $srcId; name = 'REQ-A'; foreignReference = 'J-R1' }).id
    $null = Api 'S' "requirements/$reqU/assigntestcases" Post @{ testCaseIds = @($tcUpper) }
    $null = Api 'S' "requirements/$reqL/assigntestcases" Post @{ testCaseIds = @($tcLower) }
    $null = Api 'S' "requirements/$reqA/assigntestcases" Post @{ testCaseIds = @($tcDupJ2) }

    $tsU = (Api 'S' 'testsets' Post @{ projectId = $srcId; name = 'Nightly'; enableCoverage = $false }).id
    $tsL = (Api 'S' 'testsets' Post @{ projectId = $srcId; name = 'nightly'; enableCoverage = $false }).id
    $null = Api 'S' "testsets/$tsU/assigntestcases" Post @($tcUpper, $tcDup1)
    $null = Api 'S' "testsets/$tsL/assigntestcases" Post @($tcLower, $tcJ1)

    $null = Api 'S' 'objectlabels/testCase/addlabels' Post @{ removeOtherLabels = $false; objectIds = @($tcLower); labels = @('lbl-tc'); projectId = $srcId; objectType = 'testCase' }

    $null = Api 'S' 'projectsettings' Put @{ projectTimeZone = 'Asia/Tokyo' }
    $null = Api 'S' 'projectsettings' Patch @(@{ op = 'replace'; path = '/executionNameFormat'; value = '{TestSetName} pester' })

    $script:Source = Get-Shape 'S'
}

AfterAll {
    if ($script:NoTm) { return }
    foreach ($suffix in $script:Created) {
        Remove-Item (P $suffix) -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    }
}

Describe 'The source project' {
    It 'has the case variants, duplicates and references the tests rely on' {
        if (script:SkipWithoutTm) { return }
        $script:Source.TestCases.Count | Should -Be 7
        $script:Source.InSets.Count | Should -Be 4
        $script:Source.InReqs.Count | Should -Be 3
    }
}

Describe 'Copy-Item Tm:\PRJ Tm:\NEW' {
    BeforeAll {
        if ($script:NoTm) { return }
        $script:CopyWarnings = $null
        Copy-Item (P 'S') (P 'D') -Confirm:$false -WarningVariable script:CopyWarnings -WarningAction SilentlyContinue
        $script:Created.Add('D')
        $script:Copy = Get-Shape 'D'
    }

    It 'creates the destination project' {
        if (script:SkipWithoutTm) { return }
        Get-Item (P 'D') | Should -Not -BeNullOrEmpty
    }

    It 'copies every test case with its foreign reference and steps, telling case apart' {
        if (script:SkipWithoutTm) { return }
        $script:Copy.TestCases | Should -BeExactly $script:Source.TestCases
    }

    It 'copies the requirements and test sets, telling case apart' {
        if (script:SkipWithoutTm) { return }
        $script:Copy.Requirements | Should -BeExactly $script:Source.Requirements
        $script:Copy.TestSets | Should -BeExactly $script:Source.TestSets
    }

    It 'assigns the same test cases to the same test sets and requirements' {
        if (script:SkipWithoutTm) { return }
        $script:Copy.InSets | Should -BeExactly $script:Source.InSets
        $script:Copy.InReqs | Should -BeExactly $script:Source.InReqs
    }

    It 'copies the labels' {
        if (script:SkipWithoutTm) { return }
        $script:Copy.Labels | Should -BeExactly $script:Source.Labels
        $script:Copy.Labels | Should -Contain 'login||lbl-tc'
    }

    It 'copies the time zone and the execution name format' {
        if (script:SkipWithoutTm) { return }
        $s = Get-TmProjectSetting (P 'D')
        $s.projectTimeZone | Should -Be 'Asia/Tokyo'
        $s.executionNameFormat | Should -Be '{TestSetName} pester'
    }

    It 'warns about nothing: duplicates with nothing at the destination are simply created' {
        if (script:SkipWithoutTm) { return }
        $script:CopyWarnings | Should -BeNullOrEmpty
    }

    It 'adds nothing when run again, and warns once about the duplicate key' {
        if (script:SkipWithoutTm) { return }
        $w = $null
        Copy-Item (P 'S') (P 'D') -Confirm:$false -WarningVariable w -WarningAction SilentlyContinue
        Get-Shape 'D' | ConvertTo-Json | Should -BeExactly ($script:Copy | ConvertTo-Json)
        @($w).Count | Should -Be 1
        "$($w[0])" | Should -Match "name 'Dup'"
        "$($w[0])" | Should -Match "\($($script:Stem)S:\d+, $($script:Stem)S:\d+\)" -Because 'the warning names the duplicates by objKey'
    }

    It 'restores, on a re-run, the steps and the assignment of a copy that stopped partway' {
        if (script:SkipWithoutTm) { return }
        $lower = Get-TmTestCase -Path (P 'D') * | Where-Object name -ceq 'login'
        foreach ($step in @(Api 'D' "teststeps/testcase/$($lower.id)")) { $null = Api 'D' "teststeps/$($step.id)" Delete }
        $ts = Get-TmTestSet -Path (P 'D') * | Where-Object name -ceq 'nightly'
        $null = Api 'D' "testsets/$($ts.id)/unassigntestcases" Post @($lower.id)
        (Get-Shape 'D').TestCases | Should -Contain 'login||' -Because 'the steps are gone before the re-run'

        Copy-Item (P 'S') (P 'D') -Confirm:$false -WarningAction SilentlyContinue
        Get-Shape 'D' | ConvertTo-Json | Should -BeExactly ($script:Copy | ConvertTo-Json)
    }

    It 'keeps a project setting changed at the destination' {
        if (script:SkipWithoutTm) { return }
        $null = Api 'D' 'projectsettings' Put @{ projectTimeZone = 'Europe/Paris' }
        Copy-Item (P 'S') (P 'D') -Confirm:$false -WarningAction SilentlyContinue
        (Get-TmProjectSetting (P 'D')).projectTimeZone | Should -Be 'Europe/Paris'
    }

    It 'with -Force creates the duplicates it cannot pair, and still reuses the rest' {
        if (script:SkipWithoutTm) { return }
        Copy-Item (P 'S') (P 'D') -Force -Confirm:$false -WarningAction SilentlyContinue
        $after = Get-Shape 'D'
        @($after.TestCases | Where-Object { $_ -like 'Dup||*' }).Count | Should -Be 4
        @($after.TestCases | Where-Object { $_ -notlike 'Dup||*' }) | Should -BeExactly @($script:Source.TestCases | Where-Object { $_ -notlike 'Dup||*' })
    }

    It 'with -WhatIf creates nothing' {
        if (script:SkipWithoutTm) { return }
        Copy-Item (P 'S') (P 'W') -WhatIf
        Get-Item (P 'W') -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

Describe 'Copy-TmTestCase, Copy-TmRequirement, Copy-TmTestSet' {
    BeforeAll {
        if ($script:NoTm) { return }
        $null = New-Project 'E'
    }

    It 'with -WhatIf writes nothing' {
        if (script:SkipWithoutTm) { return }
        Copy-TmTestCase -Path (P 'S') * (P 'E') -WhatIf
        @(Get-TmTestCase -Path (P 'E') *) | Should -BeNullOrEmpty
    }

    It 'test sets first: their assignments wait for the test cases, with a warning naming them' {
        if (script:SkipWithoutTm) { return }
        $w = $null
        Copy-TmTestSet -Path (P 'S') * (P 'E') -Confirm:$false -WarningVariable w -WarningAction SilentlyContinue
        (Get-Shape 'E').TestSets | Should -BeExactly $script:Source.TestSets
        @(Get-TmTestSetTestCase -Path (P 'E')) | Should -BeNullOrEmpty
        ($w | Out-String) | Should -Match 'Login \(' -Because 'the warning lists the missing test cases with their objKeys'
    }

    It 'then the test cases: they are copied and join the test sets' {
        if (script:SkipWithoutTm) { return }
        Copy-TmTestCase -Path (P 'S') * (P 'E') -Confirm:$false -WarningAction SilentlyContinue
        $shape = Get-Shape 'E'
        $shape.TestCases | Should -BeExactly $script:Source.TestCases
        $shape.InSets | Should -BeExactly $script:Source.InSets
    }

    It 'then the requirements: they get the test cases already there' {
        if (script:SkipWithoutTm) { return }
        Copy-TmRequirement -Path (P 'S') * (P 'E') -Confirm:$false -WarningAction SilentlyContinue
        $shape = Get-Shape 'E'
        $shape.Requirements | Should -BeExactly $script:Source.Requirements
        $shape.InReqs | Should -BeExactly $script:Source.InReqs
    }

    It 'does not take a destination test case of another case for the source''s' {
        if (script:SkipWithoutTm) { return }
        $null = New-Project 'G'
        $null = New-TmTestCase -Path (P 'G') -Name 'LOGIN'
        Copy-TmTestCase -Path (P 'S') Login (P 'G') -Confirm:$false -WarningAction SilentlyContinue
        @(Get-TmTestCase -Path (P 'G') * | ForEach-Object { "$($_.name)|$($_.foreignReference)" } | Sort-Object -CaseSensitive) |
            Should -BeExactly @('LOGIN|', 'LOGIN|J-1', 'Login|', 'login|' | Sort-Object -CaseSensitive) -Because '-Name ignores case, matching does not'
    }

    It 'skips a test case that matches several at the destination, naming both sides by objKey' {
        if (script:SkipWithoutTm) { return }
        $null = New-TmTestCase -Path (P 'G') -Name 'Logout'
        $null = New-TmTestCase -Path (P 'G') -Name 'Logout'
        $w = $null
        Copy-TmTestCase -Path (P 'S') Logout (P 'G') -Confirm:$false -WarningVariable w -WarningAction SilentlyContinue
        @(Get-TmTestCase -Path (P 'G') Logout).Count | Should -Be 2
        @($w).Count | Should -Be 1
        "$($w[0])" | Should -Match "$($script:Stem)S:\d+\): 2 test cases at .*\($($script:Stem)G:\d+, $($script:Stem)G:\d+\)"
    }
}
