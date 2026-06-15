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

    This is inherently an environment fixture test: it needs a folder with a
    machine and an unattended robot. It drives an existing trigger that currently
    has an EMPTY MachineRobots binding, sets the binding by the robot's account
    login, asserts the RobotId is written, and restores the trigger to empty.

    The robot is provisioned by the test itself when no UIPATHORCH_TEST_ROBOT_LOGIN
    is given: a throwaway robot account is created (Set-PmRobotAccount) and
    assigned to the tenant (Add-OrchUser, with a dummy Default credential so the
    server lets it bind an interactive trigger) and the folder (Add-OrchFolderUser),
    then torn down in AfterAll. Everything is overridable by environment variable.

      UIPATHORCH_TEST_DRIVE         (default 'local')
      UIPATHORCH_TEST_TRIGGER_FOLDER(default 'Shared')
      UIPATHORCH_TEST_TRIGGER       (default 'DispatcherTrigger')  # must start empty
      UIPATHORCH_TEST_MACHINE       (default 'orchestrator.local')
      UIPATHORCH_TEST_ROBOT_LOGIN   (optional; a throwaway robot is provisioned if unset)

    Self-skips (Set-ItResult -Skipped) when the drive isn't connected or the
    fixture (trigger/machine, empty binding) isn't present, since Pester
    evaluates -Skip at discovery before BeforeAll runs.
#>

BeforeAll {
    $script:DriveName = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'local' }
    $script:Folder = if ($env:UIPATHORCH_TEST_TRIGGER_FOLDER) { $env:UIPATHORCH_TEST_TRIGGER_FOLDER } else { 'Shared' }
    $script:Trigger = if ($env:UIPATHORCH_TEST_TRIGGER) { $env:UIPATHORCH_TEST_TRIGGER } else { 'DispatcherTrigger' }
    $script:Machine = if ($env:UIPATHORCH_TEST_MACHINE) { $env:UIPATHORCH_TEST_MACHINE } else { 'orchestrator.local' }
    $script:Drive = "$($script:DriveName):"
    $script:FolderPath = "$($script:DriveName):\$($script:Folder)"

    Import-OrchConfig | Out-Null
    $script:hasDrive = $null -ne (Get-OrchPSDrive | Where-Object Name -eq $script:DriveName)

    # The fixture (folder + trigger + machine) is environment-specific. If the folder
    # isn't even present, skip cleanly: probing a non-existent path throws (terminating,
    # not suppressed by -ErrorAction SilentlyContinue), which would abort BeforeAll and
    # FAIL every test instead of skipping it.
    $script:folderExists = $false
    if ($script:hasDrive) {
        try { $script:folderExists = [bool](Test-Path $script:FolderPath) } catch { $script:folderExists = $false }
    }

    # Robot: use a caller-supplied one if given (UIPATHORCH_TEST_ROBOT_LOGIN);
    # otherwise provision a throwaway robot account end to end — org
    # (Set-PmRobotAccount) -> tenant (Add-OrchUser, with a dummy Default
    # credential so the server lets it bind an interactive trigger) -> folder
    # (Add-OrchFolderUser) — and tear it down in AfterAll.
    $script:Provisioned = $false
    $script:ProvisionFailed = $false
    if ($env:UIPATHORCH_TEST_ROBOT_LOGIN) {
        $script:RobotLogin = $env:UIPATHORCH_TEST_ROBOT_LOGIN
    }
    elseif ($script:folderExists) {
        $script:RobotLogin = "ZZBot_$([guid]::NewGuid().ToString('N').Substring(0,8))"
        try {
            Set-PmRobotAccount -Path $script:Drive -UserName $script:RobotLogin -GroupName 'Automation Users' -Confirm:$false -ErrorAction Stop *>$null
            Add-OrchUser -Path $script:Drive -UserName $script:RobotLogin -Type DirectoryRobot `
                -MayHaveUnattendedSession $true -UR_CredentialType Default `
                -UR_UserName "localhost\$($script:RobotLogin)" -UR_Password 'P@ssw0rd1!' -Confirm:$false -ErrorAction Stop *>$null
            Add-OrchFolderUser -Path $script:FolderPath -UserName $script:RobotLogin -Type DirectoryRobot -Roles 'Automation User' -Confirm:$false -ErrorAction SilentlyContinue *>$null
            Clear-OrchCache -Path $script:Drive -ErrorAction SilentlyContinue | Out-Null
            $script:Provisioned = $true
        }
        catch {
            $script:ProvisionFailed = $true
            $script:ProvError = "$($_.Exception.Message)"
        }
    }

    if ($script:folderExists) {
        $t = Get-OrchTriggerDetail -Path $script:FolderPath -Name $script:Trigger -ErrorAction SilentlyContinue
        $m = Get-OrchFolderMachine -Path $script:FolderPath -ErrorAction SilentlyContinue |
            Where-Object Name -eq $script:Machine
        $robot = Get-OrchUser -Path $script:Drive -ErrorAction SilentlyContinue | Where-Object UserName -eq $script:RobotLogin
        # Only operate on a trigger that currently has no robot binding, so the
        # toggle-and-restore can't clobber a real one.
        $origEmpty = ($null -ne $t) -and (@($t.MachineRobots).Count -eq 0)
        $script:ready = ($null -ne $t) -and ($null -ne $m) -and ($null -ne $robot) -and $origEmpty
    }

    function script:Require {
        if (-not $script:hasDrive) { Set-ItResult -Skipped -Because "drive '$script:DriveName' is not connected"; return $false }
        if ($script:ProvisionFailed) { Set-ItResult -Skipped -Because "could not provision a test robot account: $($script:ProvError)"; return $false }
        if (-not $script:ready) { Set-ItResult -Skipped -Because "fixture not present (need trigger '$script:Trigger' with empty MachineRobots, machine '$script:Machine', and robot '$script:RobotLogin' in $script:FolderPath)"; return $false }
        return $true
    }

    function script:ClearBinding {
        Update-OrchTrigger -Path $script:FolderPath -Name $script:Trigger -MachineRobots '[]' -Confirm:$false -ErrorAction SilentlyContinue *>$null
    }
}

AfterAll {
    if ($script:hasDrive) {
        if ($script:ready) { script:ClearBinding }
        if ($script:Provisioned -eq $true) {
            Remove-OrchFolderUser -Path $script:FolderPath -UserName $script:RobotLogin -Confirm:$false -ErrorAction SilentlyContinue *>$null
            Remove-OrchUser -Path $script:Drive -UserName $script:RobotLogin -Type DirectoryRobot -Confirm:$false -ErrorAction SilentlyContinue *>$null
            Remove-PmRobotAccount -Path $script:Drive -Name $script:RobotLogin -Confirm:$false -ErrorAction SilentlyContinue *>$null
            Clear-OrchCache -Path $script:Drive -ErrorAction SilentlyContinue | Out-Null
        }
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
