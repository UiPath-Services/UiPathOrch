#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copy-Item must not skip a TRIGGER because the destination folder's process list is stale, and
    must leave the destination's process and trigger lists current.

.DESCRIPTION
    The process-side twin of CopyItem.QueueTriggerResolve.Tests.ps1. The trigger stage resolves each
    trigger's process BY NAME against the destination folder's cached process list (FindDstRelease
    in CopyItem.Resolve.cs reads drive.Releases.Get(dstFolder)). A list cached before the processes
    were created makes it decide the process does not exist and skip the trigger with

        Copying trigger ...: <folder> does not have process with Name = '<process>'.

    a WARNING, not an error -- the run looks successful while the trigger never arrived. The field
    condition is a destination folder that already exists: a re-run, or a retry after a failed
    stage, whose earlier pass cached the folder's process list (empty, if the processes could not
    be created then). Copy-Item pre-cleared the PARENT folder's list instead of the folder it copies
    into, and the process stage did not refresh the list after creating processes.

    The setup reproduces that state directly: the destination folder exists, and its process and
    trigger lists are read (cached empty) before the copy.

    DELIBERATELY no Clear-OrchCache between the copy and the assertions: the cleared cache is
    exactly what used to hide this.

.NOTES
    Run through the shared runner, which wipes and re-seeds the disposable tenant first:
        Tests\Invoke-AllTests.ps1 -Tenant Orch2 -Filter 'CopyItem.ProcessTriggerResolve*'

    Same-tenant on purpose, as in CopyItem.QueueTriggerResolve.Tests.ps1: the trigger needs a
    Release, and a Release needs its package in the destination feed. A time trigger (disabled),
    so that no queue is involved and only the process lookup is tested.

    What each assertion guards (c7a3aed3):
    - 'does not warn' and 'creates the trigger': the pre-clear of the folder copied INTO
      (CopyItem.Recurse.cs) and the refresh after creating processes (CopyProcesses in
      CopyItem.Entities.cs). Either one alone makes these pass, because the primed list is read
      before the copy; reverting both makes them fail.
    - 'lists the trigger': the refresh after the trigger stage. The pre-clear does not touch the
      trigger list, so this one fails on its own when that refresh is removed.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:SkipReason = $null

    $stamp = $PID
    $script:SrcRoot = "$($script:Drive):\_ptr_src_$stamp"
    $script:DstRoot = "$($script:Drive):\_ptr_dst_$stamp"
    $script:DstCopy = "$($script:DstRoot)\_ptr_src_$stamp"
    $script:ProcessName = 'ptrProc'
    $script:TriggerName = 'ptrTimeTrigger'
    $script:PackageId   = 'BlankProcess19'
    $script:PackageVer  = '1.0.3'
    $script:Cron        = '0 0 0/1 1/1 * ? *'

    try {
        New-Item -ItemType Directory -Path $script:SrcRoot -Force -ErrorAction Stop | Out-Null
        New-OrchProcess -Path $script:SrcRoot -Id $script:PackageId -Version $script:PackageVer `
            -Name $script:ProcessName -ErrorAction Stop | Out-Null
        # Enabled=false so the copy never starts a job; the binding is what matters.
        New-OrchTrigger -Path $script:SrcRoot -Name $script:TriggerName -ReleaseName $script:ProcessName `
            -StartProcessCron $script:Cron -Enabled false -ErrorAction Stop | Out-Null

        # The destination folder EXISTS before the copy -- the field condition.
        New-Item -ItemType Directory -Path $script:DstRoot -Force -ErrorAction Stop | Out-Null
        New-Item -ItemType Directory -Path $script:DstCopy -Force -ErrorAction Stop | Out-Null

        Clear-OrchCache

        # THE POSITIVE CONTROL. Reading the empty destination folder caches empty lists; unless
        # the copy refreshes them, the trigger stage and the Get-* calls below read these.
        $script:PrimedProcesses = @(Get-OrchProcess -Path $script:DstCopy * | Select-Object -ExpandProperty Name)
        $script:PrimedTriggers  = @(Get-OrchTrigger -Path $script:DstCopy * | Select-Object -ExpandProperty Name)

        Write-Host "Copy-Item -Recurse (time trigger, pre-existing dst folder, primed cache) ..." -ForegroundColor Cyan
        $script:CopyWarnings = @()
        Copy-Item -Path $script:SrcRoot -Destination $script:DstRoot -Recurse `
            -WarningVariable copyWarnings -ErrorAction Stop
        $script:CopyWarnings = @($copyWarnings)
    }
    catch {
        $script:SkipReason = "setup failed: $($_.Exception.Message)"
        Write-Host "SKIPPING: $($script:SkipReason)" -ForegroundColor Yellow
    }
}

AfterAll {
    foreach ($p in @($script:DstRoot, $script:SrcRoot)) {
        if ($p) { Remove-Item -Path $p -Recurse -ErrorAction SilentlyContinue }
    }
}

Describe 'Copy-Item resolves a trigger against a destination folder whose process list was cached empty' {

    It 'primed the destination process and trigger caches empty, or the test proves nothing' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $script:PrimedProcesses | Should -BeNullOrEmpty `
            -Because 'the copy has to refresh a process list cached as empty'
        $script:PrimedTriggers | Should -BeNullOrEmpty `
            -Because 'the copy has to refresh a trigger list cached as empty'
    }

    It 'does not warn that the destination has no such process' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $offending = @($script:CopyWarnings | Where-Object { "$_" -like '*does not have process with Name*' })
        $offending | Should -BeNullOrEmpty `
            -Because 'the process is created in that folder by the same copy, so the trigger stage must see it'
    }

    It 'lists the process in the destination folder without Clear-OrchCache' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $names = @(Get-OrchProcess -Path $script:DstCopy * | Select-Object -ExpandProperty Name)
        $names | Should -Contain $script:ProcessName `
            -Because 'Get-OrchProcess reads the same list the trigger stage resolves against'
    }

    It 'creates the trigger at the destination, bound to its process, listed without Clear-OrchCache' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $copied = @(Get-OrchTrigger -Path $script:DstCopy * | Where-Object Name -eq $script:TriggerName)
        $copied.Count | Should -Be 1 `
            -Because 'a skipped trigger, or a trigger list left stale after the copy, both show as missing here'
        $copied[0].ReleaseName | Should -Be $script:ProcessName `
            -Because 'the trigger is worthless at the destination if it lost its process'
    }
}
