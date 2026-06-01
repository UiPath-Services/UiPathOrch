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
    machine and at least one unattended robot whose account login differs from
    its UnattendedRobot.UserName (the case the old code missed) — which an
    on-prem Orchestrator with a directory/robot account reproduces. It drives an
    existing trigger that currently has an EMPTY MachineRobots binding, sets the
    binding by the robot's account login, asserts the RobotId is written, and
    restores the trigger to empty in AfterAll. Everything is overridable by
    environment variable; defaults target the on-prem verification setup.

      UIPATHORCH_TEST_DRIVE         (default 'local')
      UIPATHORCH_TEST_TRIGGER_FOLDER(default 'Shared')
      UIPATHORCH_TEST_TRIGGER       (default 'DispatcherTrigger')  # must start empty
      UIPATHORCH_TEST_MACHINE       (default 'orchestrator.local')
      UIPATHORCH_TEST_ROBOT_LOGIN   (default 'User1')              # account login

    Self-skips (Set-ItResult -Skipped) when the drive isn't connected or the
    fixture (trigger/machine, empty binding) isn't present, since Pester
    evaluates -Skip at discovery before BeforeAll runs.
#>

BeforeAll {
    $script:DriveName = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'local' }
    $script:Folder = if ($env:UIPATHORCH_TEST_TRIGGER_FOLDER) { $env:UIPATHORCH_TEST_TRIGGER_FOLDER } else { 'Shared' }
    $script:Trigger = if ($env:UIPATHORCH_TEST_TRIGGER) { $env:UIPATHORCH_TEST_TRIGGER } else { 'DispatcherTrigger' }
    $script:Machine = if ($env:UIPATHORCH_TEST_MACHINE) { $env:UIPATHORCH_TEST_MACHINE } else { 'orchestrator.local' }
    $script:RobotLogin = if ($env:UIPATHORCH_TEST_ROBOT_LOGIN) { $env:UIPATHORCH_TEST_ROBOT_LOGIN } else { 'User1' }
    $script:FolderPath = "$($script:DriveName):\$($script:Folder)"

    Import-OrchConfig | Out-Null
    $script:hasDrive = $null -ne (Get-OrchPSDrive | Where-Object Name -eq $script:DriveName)

    if ($script:hasDrive) {
        $t = Get-OrchTriggerDetail -Path $script:FolderPath -Name $script:Trigger -ErrorAction SilentlyContinue
        $m = Get-OrchFolderMachine -Path $script:FolderPath -ErrorAction SilentlyContinue |
            Where-Object Name -eq $script:Machine
        # Only operate on a trigger that currently has no robot binding, so the
        # toggle-and-restore can't clobber a real one.
        $origEmpty = ($null -ne $t) -and (@($t.MachineRobots).Count -eq 0)
        $script:ready = ($null -ne $t) -and ($null -ne $m) -and $origEmpty
    }

    function script:Require {
        if (-not $script:hasDrive) { Set-ItResult -Skipped -Because "drive '$script:DriveName' is not connected"; return $false }
        if (-not $script:ready) { Set-ItResult -Skipped -Because "fixture not present (need trigger '$script:Trigger' with empty MachineRobots + machine '$script:Machine' in $script:FolderPath)"; return $false }
        return $true
    }

    function script:ClearBinding {
        Update-OrchTrigger -Path $script:FolderPath -Name $script:Trigger -MachineRobots '[]' -Confirm:$false -ErrorAction SilentlyContinue *>$null
    }
}

AfterAll {
    if ($script:hasDrive -and $script:ready) { script:ClearBinding }
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
}
