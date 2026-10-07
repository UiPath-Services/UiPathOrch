#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Regression coverage for the New-/Update-OrchTrigger -MachineRobots robot
    resolution fix.

.DESCRIPTION
    The resolver used to match the supplied user name only against
    User.UnattendedRobot.UserName and read RobotId from there, so a robot
    account — or any robot whose identity lives on RobotProvision or the account
    login — failed to resolve: the trigger got the MachineId but no RobotId,
    with a "… is not configured as Unattended Robot" warning. The fix matches
    the merged robot view (UnattendedRobot ?? RobotProvision, plus the account
    login) and takes RobotId from the matched robot.

    The file seeds everything it drives, in a folder of its own: the tenant's first
    Standard machine (else its first machine) added to the folder, a throwaway robot
    account (Set-PmRobotAccount -> Add-OrchUser with a dummy Default credential, so the
    server lets it bind a trigger -> Add-OrchFolderUser), a BlankProcess19 process and a
    disabled trigger with an EMPTY MachineRobots binding. Each test sets the binding by
    the robot's account login, asserts the RobotId is written, and empties it again.
    AfterAll removes the folder, the user and the robot account.

      UIPATHORCH_TEST_DRIVE   (default 'Orch2'; any drive whose feed holds BlankProcess19)

    It used to drive a hand-made fixture on a 'local:' drive (trigger 'DispatcherTrigger'
    in 'Shared', machine 'orchestrator.local'). That drive was retired on 2026-10-07, when
    the on-premises servers got a drive each (op2010: .. op2510:), and every test here
    skipped from then on without anyone noticing -- hence the self-seeding.

    Self-skips (Set-ItResult -Skipped) when the setup cannot be built, e.g. 20.10.16,
    which has no robot accounts reachable this way.
#>

BeforeAll {
    $script:DriveName = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:Drive = "$($script:DriveName):"
    $script:SkipReason = $null

    $stamp = [guid]::NewGuid().ToString('N').Substring(0, 6)
    $script:RobotLogin = "ZZBot_$stamp"
    $script:FolderPath = "$($script:Drive)\_utm_$stamp"
    $script:Trigger    = 'utmTrigger'
    $script:Done       = @{}

    try {
        $machines = @(Get-OrchMachine -Path "$($script:Drive)\" -ErrorAction Stop)
        $machine = @($machines | Where-Object Type -eq 'Standard')[0]
        if (-not $machine) { $machine = $machines[0] }
        if (-not $machine) { throw "no machine in $($script:Drive)" }
        $script:Machine = $machine.Name
        $pkg = @(Get-OrchPackage -Path "$($script:Drive)\" -ErrorAction Stop | Where-Object Id -eq 'BlankProcess19')[0]
        if (-not $pkg) { throw "BlankProcess19 is not in the $($script:Drive) tenant feed" }

        New-Item -ItemType Directory -Path $script:FolderPath -ErrorAction Stop | Out-Null; $script:Done.folder = $true
        Add-OrchFolderMachine -Path $script:FolderPath -Name $script:Machine -ErrorAction Stop | Out-Null
        Set-PmRobotAccount -Path $script:Drive -Name $script:RobotLogin -GroupName 'Automation Users' -Confirm:$false -ErrorAction Stop *>$null
        $script:Done.pm = $true
        Add-OrchUser -Path $script:Drive -UserName $script:RobotLogin -Type DirectoryRobot -MayHaveUnattendedSession $true `
            -UR_CredentialType Default -UR_UserName "localhost\$($script:RobotLogin)" -UR_Password 'P@ssw0rd1!' -Confirm:$false -ErrorAction Stop *>$null
        $script:Done.user = $true
        # 'Automation User' from 22.4 on; 21.10 names the folder role a robot needs 'Robot'.
        $role = @('Automation User', 'Robot' | Where-Object { $_ -in (Get-OrchRole -Path "$($script:Drive)\" -ErrorAction Stop).Name })[0]
        Add-OrchFolderUser -Path $script:FolderPath -UserName $script:RobotLogin -Type DirectoryRobot -Roles $role -Confirm:$false -ErrorAction Stop *>$null
        Clear-OrchCache -Path $script:Drive | Out-Null

        New-OrchProcess -Path $script:FolderPath -Id BlankProcess19 -Version $pkg.Version -Name utmProc -ErrorAction Stop | Out-Null
        # Disabled, so nothing ever starts a job; no robots, which is what each test starts from.
        New-OrchTrigger -Path $script:FolderPath -Name $script:Trigger -ReleaseName utmProc -StartProcessCron '0 0 9 ? * *' `
            -TimeZoneId 'Tokyo Standard Time' -Enabled false -ErrorAction Stop *>$null
        Clear-OrchCache -Path $script:Drive | Out-Null

        $t = Get-OrchTriggerDetail -Path $script:FolderPath -Name $script:Trigger -ErrorAction Stop
        if (@($t.MachineRobots).Count -ne 0) { throw "the new trigger came with a robot binding" }
    }
    catch {
        $script:SkipReason = "setup failed on $($script:Drive) $($_.Exception.Message)"
        Write-Host "SKIPPING: $($script:SkipReason)" -ForegroundColor Yellow
    }

    function script:Require {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return $false }
        return $true
    }

    function script:ClearBinding {
        Update-OrchTrigger -Path $script:FolderPath -Name $script:Trigger -MachineRobots '[]' -Confirm:$false -ErrorAction SilentlyContinue *>$null
    }
}

AfterAll {
    if ($script:Done.folder -and (Test-Path $script:FolderPath)) {
        Remove-Item -Path $script:FolderPath -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    }
    # On-premises servers drop a deleted trigger's robot link a moment after the folder goes;
    # removing the user before that fails with "The user is assigned to trigger ...".
    Start-Sleep -Seconds 8
    Clear-OrchCache -Path $script:Drive -ErrorAction SilentlyContinue | Out-Null
    if ($script:Done.user) {
        foreach ($u in @(Get-OrchUser -Path "$($script:Drive)\" -ErrorAction SilentlyContinue | Where-Object UserName -eq $script:RobotLogin)) {
            Remove-OrchUser -Path $script:Drive -UserName $u.UserName -Type $u.Type -Confirm:$false -ErrorAction SilentlyContinue *>$null
        }
    }
    if ($script:Done.pm) {
        Remove-PmRobotAccount -Path $script:Drive -Name $script:RobotLogin -Confirm:$false -ErrorAction SilentlyContinue *>$null
    }
}

Describe 'Update-OrchTrigger -MachineRobots robot resolution' {
    It 'resolves an unattended robot by its account login and writes its RobotId' {
        if (-not (script:Require)) { return }
        $mr = (@{ UserName = $script:RobotLogin; MachineName = $script:Machine } | ConvertTo-Json -Compress)
        $out = Update-OrchTrigger -Path $script:FolderPath -Name $script:Trigger -MachineRobots "[$mr]" -Confirm:$false *>&1

        # The supplied login resolved — no "not configured / does not match" warning.
        ($out | Where-Object { "$_" -match 'not configured|does not match' }) | Should -BeNullOrEmpty

        # The fix: the binding carries a RobotId (previously dropped to null) plus
        # the machine, not the machine alone.
        $bound = @((Get-OrchTriggerDetail -Path $script:FolderPath -Name $script:Trigger).MachineRobots)
        $bound | Should -Not -BeNullOrEmpty
        $bound[0].MachineName | Should -Be $script:Machine
        $bound[0].RobotId | Should -Not -BeNullOrEmpty

        script:ClearBinding
    }

    It 'still warns for a genuinely unknown robot name (no over-matching)' {
        if (-not (script:Require)) { return }
        $mr = (@{ UserName = 'NoSuchRobot_ZZZ'; MachineName = $script:Machine } | ConvertTo-Json -Compress)
        $out = Update-OrchTrigger -Path $script:FolderPath -Name $script:Trigger -MachineRobots "[$mr]" -Confirm:$false *>&1

        ($out | Where-Object { "$_" -match 'does not match any' }) | Should -Not -BeNullOrEmpty

        script:ClearBinding
    }

    It 'round-trips a MachineRobots binding through Get-OrchTrigger -ExportCsv and Import-Csv | Update-OrchTrigger' {
        if (-not (script:Require)) { return }

        # Bind a robot by its login and capture the resolved RobotId.
        $mr = (@{ UserName = $script:RobotLogin; MachineName = $script:Machine } | ConvertTo-Json -Compress)
        Update-OrchTrigger -Path $script:FolderPath -Name $script:Trigger -MachineRobots "[$mr]" -Confirm:$false *>$null
        $before = @((Get-OrchTriggerDetail -Path $script:FolderPath -Name $script:Trigger).MachineRobots)[0]
        $before.RobotId | Should -Not -BeNullOrEmpty

        $csv = Join-Path ([IO.Path]::GetTempPath()) "trig_$([guid]::NewGuid().ToString('N')).csv"
        try {
            # Export: the MachineRobots column holds the serialized binding — the
            # robot's resolved, usually domain-qualified, user name (e.g. host\user).
            Get-OrchTrigger -Path $script:FolderPath -ExportCsv $csv *>$null
            $row = Import-Csv $csv | Where-Object Name -eq $script:Trigger
            $row | Should -Not -BeNullOrEmpty
            $row.MachineRobots | Should -Match 'UserName'

            # Wipe, then re-import the row. Import-Csv | Update-OrchTrigger must
            # restore the exact binding from the exported domain\user value — this
            # only works because matching is literal (a wildcard pattern would
            # treat the backslash as an escape and drop the RobotId).
            script:ClearBinding
            @((Get-OrchTriggerDetail -Path $script:FolderPath -Name $script:Trigger).MachineRobots).Count | Should -Be 0

            $reout = $row | Update-OrchTrigger -Confirm:$false *>&1
            ($reout | Where-Object { "$_" -match 'not configured|does not match' }) | Should -BeNullOrEmpty

            $after = @((Get-OrchTriggerDetail -Path $script:FolderPath -Name $script:Trigger).MachineRobots)[0]
            $after.RobotId     | Should -Be $before.RobotId
            $after.MachineName | Should -Be $before.MachineName
        }
        finally {
            Remove-Item $csv -ErrorAction SilentlyContinue
            script:ClearBinding
        }
    }
}
