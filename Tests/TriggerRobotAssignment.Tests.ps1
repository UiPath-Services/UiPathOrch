#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    A trigger's executor-robot assignment must survive being read, exported, copied, updated and
    re-imported from its own exported row.

.DESCRIPTION
    The assignment (ExecutorRobots; what GetRobotIdsForSchedule returns and the trigger screen's
    Account column shows) was lost in two ways, found 2026-10-06:

    1. Update-OrchTrigger cleared it on EVERY update, since 1.0.0. The PUT is built from the
       trigger listing, which carries no ExecutorRobots, and Orchestrator takes a PUT without
       them as "no robots" -- so -Enabled alone wiped the assignment. Measured with 1.18.0 on
       Automation Cloud and Automation Suite 24.10.11.

    2. 1.19.0 read whole triggers from the listing with $expand=ExecutorRobots. On-premises
       servers (20.10.16, 22.4.4, 25.10.2, Automation Suite 24.10.11) accept that expansion and
       return it EMPTY, so Get-OrchTriggerDetail, -ExportCsv and Copy-Item / Copy-OrchTrigger
       lost the assignment there. Automation Cloud fills it, which is why 1.19.0 was checked
       and looked right.

    This file seeds a robot-bound trigger and checks every path. The default drive is the
    disposable Cloud tenant, which catches (1). (2) shows only on an on-premises server: point
    UIPATHORCH_TEST_DRIVE at one -- the whole file only creates and removes its own objects.

      UIPATHORCH_TEST_DRIVE   (default 'Orch2')

    The robot is a throwaway robot account (Set-PmRobotAccount -> Add-OrchUser with an
    unattended credential -> folder assignment), as UpdateOrchTriggerMachineRobots.Tests.ps1
    does; the machine is the tenant's first Standard machine (else its first machine); the
    process runs BlankProcess19, which the target tenant's feed must hold (the shared fixture
    puts it in the disposable tenant).
    The file skips when any of that cannot be set up (20.10.16, for one, has no robot accounts
    reachable this way).

.NOTES
    Tests\Invoke-AllTests.ps1 -Tenant Orch2 -Filter 'TriggerRobotAssignment*'
    $env:UIPATHORCH_TEST_DRIVE = 'AS'; Invoke-Pester Tests\TriggerRobotAssignment.Tests.ps1

    Positive control (2026-10-06): with the published 1.18.0 the Update test fails on Cloud;
    with 1.19.0 the Detail, CSV, Copy and Update tests fail on Automation Suite 24.10.11. The
    re-import test caught a slip in the first fix itself -- an unchanged -ExecutorRobots left
    the PUT without robots -- on Cloud, before it shipped.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:SkipReason = $null

    $stamp = [guid]::NewGuid().ToString('N').Substring(0, 6)
    $script:Bot     = "ZZBot_$stamp"
    $script:Src     = "$($script:Drive):\_tra_src_$stamp"
    $script:DstRoot = "$($script:Drive):\_tra_dst_$stamp"
    $script:DstCopy = "$($script:DstRoot)\_tra_src_$stamp"
    $script:Trigger = 'traTrigger'
    $script:Done    = @{}

    function script:RobotIds([string]$path, $id) {
        @((Invoke-OrchApi -Path $path -Raw -ErrorAction Stop `
            -ApiPath "/odata/ProcessSchedules/UiPath.Server.Configuration.OData.GetRobotIdsForSchedule(key=$id)" |
            ConvertFrom-Json).value | ForEach-Object { [long]$_ } | Sort-Object) -join ','
    }
    function script:TriggerId([string]$path) {
        (Get-OrchTrigger -Path $path -Name $script:Trigger -ErrorAction Stop).Id
    }

    try {
        # A Standard machine if there is one; Automation Suite installs may have only templates,
        # which bind a robot to a trigger just the same.
        $machines = @(Get-OrchMachine -Path "$($script:Drive):\" -ErrorAction Stop)
        $machine = @($machines | Where-Object Type -eq 'Standard')[0]
        if (-not $machine) { $machine = $machines[0] }
        if (-not $machine) { throw "no machine in $($script:Drive):" }
        $pkg = @(Get-OrchPackage -Path "$($script:Drive):\" -ErrorAction Stop | Where-Object Id -eq 'BlankProcess19')[0]
        if (-not $pkg) { throw "BlankProcess19 is not in the $($script:Drive): tenant feed" }

        New-Item -ItemType Directory -Path $script:Src -ErrorAction Stop | Out-Null; $script:Done.src = $true
        Add-OrchFolderMachine -Path $script:Src -Name $machine.Name -ErrorAction Stop | Out-Null
        Set-PmRobotAccount -Path "$($script:Drive):" -Name $script:Bot -GroupName 'Automation Users' -Confirm:$false -ErrorAction Stop *>$null
        $script:Done.pm = $true
        Add-OrchUser -Path "$($script:Drive):" -UserName $script:Bot -Type DirectoryRobot -MayHaveUnattendedSession $true `
            -UR_CredentialType Default -UR_UserName "localhost\$($script:Bot)" -UR_Password 'P@ssw0rd1!' -Confirm:$false -ErrorAction Stop *>$null
        $script:Done.user = $true
        # 'Automation User' from 22.4 on; 21.10 names the folder role a robot needs 'Robot'.
        $role = @('Automation User', 'Robot' | Where-Object { $_ -in (Get-OrchRole -Path "$($script:Drive):\" -ErrorAction Stop).Name })[0]
        Add-OrchFolderUser -Path $script:Src -UserName $script:Bot -Type DirectoryRobot -Roles $role -Confirm:$false -ErrorAction Stop *>$null
        Clear-OrchCache -Path "$($script:Drive):" | Out-Null

        New-OrchProcess -Path $script:Src -Id BlankProcess19 -Version $pkg.Version -Name traProc -ErrorAction Stop | Out-Null
        $mr = @{ UserName = $script:Bot; MachineName = $machine.Name } | ConvertTo-Json -Compress
        # Disabled, so nothing ever starts a job.
        New-OrchTrigger -Path $script:Src -Name $script:Trigger -ReleaseName traProc -StartProcessCron '0 0 9 ? * *' `
            -TimeZoneId 'Tokyo Standard Time' -Enabled false -MachineRobots "[$mr]" -ErrorAction Stop *>$null
        Clear-OrchCache -Path "$($script:Drive):" | Out-Null

        $script:Id = script:TriggerId $script:Src
        $script:Assigned = script:RobotIds $script:Src $script:Id
    }
    catch {
        $script:SkipReason = "setup failed on $($script:Drive): $($_.Exception.Message)"
        Write-Host "SKIPPING: $($script:SkipReason)" -ForegroundColor Yellow
    }
}

AfterAll {
    foreach ($p in @($script:DstRoot, $script:Src)) {
        if ($p -and (Test-Path $p)) { Remove-Item -Path $p -Recurse -Confirm:$false -ErrorAction SilentlyContinue }
    }
    # On-premises servers drop a deleted trigger's robot link a moment after the folder goes;
    # removing the user before that fails with "The user is assigned to trigger ...".
    Start-Sleep -Seconds 8
    Clear-OrchCache -Path "$($script:Drive):" -ErrorAction SilentlyContinue | Out-Null
    if ($script:Done.user) {
        foreach ($u in @(Get-OrchUser -Path "$($script:Drive):\" -ErrorAction SilentlyContinue | Where-Object UserName -eq $script:Bot)) {
            Remove-OrchUser -Path "$($script:Drive):" -UserName $u.UserName -Type $u.Type -Confirm:$false -ErrorAction SilentlyContinue *>$null
        }
    }
    if ($script:Done.pm) {
        Remove-PmRobotAccount -Path "$($script:Drive):" -Name $script:Bot -Confirm:$false -ErrorAction SilentlyContinue *>$null
    }
}

Describe 'A trigger keeps its executor robots' {

    It 'was created with a robot, or the test proves nothing' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $script:Assigned | Should -Not -BeNullOrEmpty -Because 'New-OrchTrigger -MachineRobots writes the robot relation'
    }

    It 'shows them in Get-OrchTriggerDetail' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $t = Get-OrchTriggerDetail -Path $script:Src -Name $script:Trigger
        (@($t.ExecutorRobots | ForEach-Object { [long]$_.Id } | Sort-Object) -join ',') | Should -Be $script:Assigned
    }

    It 'writes them to the -ExportCsv ExecutorRobots column' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $csv = Join-Path ([IO.Path]::GetTempPath()) "tra_$([guid]::NewGuid().ToString('N')).csv"
        try {
            Get-OrchTrigger -Path $script:Src -ExportCsv $csv *>$null
            $row = Import-Csv $csv | Where-Object Name -eq $script:Trigger
            $row.ExecutorRobots | Should -Not -BeNullOrEmpty -Because 're-importing an empty column would clear the assignment'
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }

    It 'carries them through Copy-Item' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        New-Item -ItemType Directory -Path $script:DstRoot -ErrorAction Stop | Out-Null
        Copy-Item -Path $script:Src -Destination $script:DstRoot -Recurse -ErrorAction Continue 3>$null
        Clear-OrchCache -Path "$($script:Drive):" | Out-Null
        $copiedId = script:TriggerId $script:DstCopy
        script:RobotIds $script:DstCopy $copiedId | Should -Be $script:Assigned `
            -Because 'same tenant, so the copy should run on the same robot'
    }

    It 'keeps them through an Update-OrchTrigger that does not touch them' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        # A rename: a real change (so the PUT is sent) that has nothing to do with robots, and
        # leaves the trigger disabled.
        Update-OrchTrigger -Path $script:Src -Name $script:Trigger -NewName "$($script:Trigger)Renamed" -Confirm:$false -ErrorAction Stop *>$null
        $script:Trigger = "$($script:Trigger)Renamed"
        script:RobotIds $script:Src $script:Id | Should -Be $script:Assigned `
            -Because 'a PUT without ExecutorRobots clears them; the update has to send the current ones'
    }

    # Last, because a failure here clears the robots the tests above read.
    It 'keeps them when the exported row is piped back into Update-OrchTrigger' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $csv = Join-Path ([IO.Path]::GetTempPath()) "tra_$([guid]::NewGuid().ToString('N')).csv"
        try {
            Get-OrchTrigger -Path $script:Src -ExportCsv $csv *>$null
            $row = Import-Csv $csv | Where-Object Name -eq $script:Trigger
            # A real change, so the PUT is sent; the robot columns are re-sent unchanged.
            $row.SpecificPriorityValue = '45'
            $row | Update-OrchTrigger -Confirm:$false -ErrorAction Stop *>$null
            script:RobotIds $script:Src $script:Id | Should -Be $script:Assigned `
                -Because 'an unchanged ExecutorRobots cell must not turn into a PUT without robots'
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }
}
