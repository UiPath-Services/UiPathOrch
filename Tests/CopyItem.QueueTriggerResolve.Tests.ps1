#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copy-Item must not skip a QUEUE TRIGGER because the destination folder's queue list is stale.

.DESCRIPTION
    The other half of the failure 1.15.1 fixed. CopyItem.CrossTenant.Tests.ps1 covers the visible
    half -- a shared queue missing from Get-OrchQueue at the destination. This covers the half that
    made a migration silently incomplete: the trigger stage resolves its queue BY NAME against that
    same per-folder list (FindDstQueue in CopyItem.Resolve.cs reads drive.Queues.Get(dstFolder)),
    so a stale empty list makes it decide the queue does not exist and skip the trigger with

        Copying trigger ...: <folder> does not have queue with Name = '<queue>'.

    a WARNING, not an error -- the run looks successful while the trigger never arrived. Reported
    from an MSI-to-Automation-Suite migration (2026-08-31).

    The setup primes the destination folder's queue cache with an empty list before the copy, which
    is what the old code did implicitly: on a folder that already exists at the destination, the
    first folder's pass looked for a shared queue's counterpart there, found nothing, and cached
    that. Copy-Item's folder loop was then the one create path that never invalidated the list
    (CopyItem.Recurse.cs, the queue stage). Priming it here reproduces that state directly, so the
    test does not depend on the multi-folder ordering that produced it in the field.

    DELIBERATELY no Clear-OrchCache between the copy and the assertions: the cleared cache is
    exactly what used to hide this.

.NOTES
    Run through the shared runner, which wipes and re-seeds the disposable tenant first:
        Tests\Invoke-AllTests.ps1 -Tenant Orch2 -Filter 'CopyItem.QueueTriggerResolve*'

    Same-tenant on purpose: the trigger needs a Release, and a Release needs its package in the
    destination feed, which is why CopyItem.CrossTenant.Tests.ps1 excludes triggers entirely.
    The queue is created in the source folder by this file (the shared fixture seeds no queue
    trigger), and both temp folders are removed in AfterAll.

    Positive control (2026-09-01): with `dstDrive.Queues.ClearCache(newFolder)` removed from the
    queue stage in CopyItem.Recurse.cs, this file fails -- the warning fires and the trigger is
    absent at the destination. Restored, it passes.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:SkipReason = $null

    $stamp = $PID
    $script:SrcRoot = "$($script:Drive):\_qtr_src_$stamp"
    $script:DstRoot = "$($script:Drive):\_qtr_dst_$stamp"
    $script:DstCopy = "$($script:DstRoot)\_qtr_src_$stamp"
    $script:QueueName   = 'qtrQueue'
    $script:ProcessName = 'qtrProc'
    $script:TriggerName = 'qtrQueueTrigger'
    $script:PackageId   = 'BlankProcess19'
    $script:PackageVer  = '1.0.3'

    try {
        New-Item -ItemType Directory -Path $script:SrcRoot -Force -ErrorAction Stop | Out-Null
        New-OrchQueue -Path $script:SrcRoot -Name $script:QueueName -Description 'queue trigger target' -ErrorAction Stop | Out-Null
        New-OrchProcess -Path $script:SrcRoot -Id $script:PackageId -Version $script:PackageVer `
            -Name $script:ProcessName -ErrorAction Stop | Out-Null
        # Enabled=false so the copy never starts a job; the binding is what matters.
        New-OrchTrigger -Path $script:SrcRoot -Name $script:TriggerName -ReleaseName $script:ProcessName `
            -QueueDefinitionName $script:QueueName -Enabled false -ErrorAction Stop | Out-Null

        # The destination folder EXISTS before the copy -- the field condition. \Shared always
        # exists, and any re-run finds the whole tree already there.
        New-Item -ItemType Directory -Path $script:DstRoot -Force -ErrorAction Stop | Out-Null
        New-Item -ItemType Directory -Path $script:DstCopy -Force -ErrorAction Stop | Out-Null

        Clear-OrchCache

        # THE POSITIVE CONTROL. Reading an empty destination folder caches an empty queue list;
        # unless the copy invalidates it when it creates the queue there, the trigger stage below
        # resolves against this and skips.
        $script:PrimedQueues = @(Get-OrchQueue -Path $script:DstCopy * | Select-Object -ExpandProperty Name)

        Write-Host "Copy-Item -Recurse (queue trigger, pre-existing dst folder, primed cache) ..." -ForegroundColor Cyan
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

Describe 'Copy-Item resolves a queue trigger against a destination folder whose queue list was cached empty' {

    It 'primed the destination queue cache empty, or the test proves nothing' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $script:PrimedQueues | Should -BeNullOrEmpty `
            -Because 'the whole point is that the copy has to invalidate a list cached as empty'
    }

    It 'does not warn that the destination has no such queue' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $offending = @($script:CopyWarnings | Where-Object { "$_" -like '*does not have queue with Name*' })
        $offending | Should -BeNullOrEmpty `
            -Because 'the queue is created in that folder by the same copy, so the trigger stage must see it'
    }

    It 'creates the queue trigger at the destination, still bound to its queue' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $copied = @(Get-OrchTrigger -Path $script:DstCopy * | Where-Object Name -eq $script:TriggerName)
        $copied.Count | Should -Be 1 -Because 'a skipped trigger is the silent half of this failure'
        $copied[0].QueueDefinitionName | Should -Be $script:QueueName `
            -Because 'the trigger is worthless at the destination if it lost its queue'
    }

    It 'lists the queue in the destination folder without Clear-OrchCache' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $names = @(Get-OrchQueue -Path $script:DstCopy * | Select-Object -ExpandProperty Name)
        $names | Should -Contain $script:QueueName `
            -Because 'the same stale list is what Get-OrchQueue reads, and it under-reported in the field'
    }
}
