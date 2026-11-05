#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    The task cmdlets in a modern folder: Get-OrchTask lists the folder's tasks, Get-OrchTaskData
    reads a task's data, Set-OrchTask edits it and Remove-OrchTask deletes it.

.DESCRIPTION
    Get-OrchTask used /odata/Tasks, which answers for classic folders only, and Remove-OrchTask
    sent a DELETE the API does not have; both are covered here end to end.

    Creates a folder "zzTK_<pid>" on $env:UIPATHORCH_TEST_DRIVE (default Orch2), an external task
    in it (through Invoke-OrchApi, as no cmdlet creates tasks), and removes both in AfterAll.

.NOTES
    Run with: Invoke-Pester -Path Tests\OrchTask.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $script:Folder = "$($script:Drive):\zzTK_$($PID)"
    $script:Title = "zzTK_$($PID)_task"
    New-Item $script:Folder -ItemType Folder -ErrorAction Stop 3>$null | Out-Null

    $created = Invoke-OrchApi -Path $script:Folder -Method Post -Uri 'tasks/GenericTasks/CreateTask' -Body @{
        title = $script:Title; type = 'ExternalTask'; priority = 'Low'
        data  = @{ Customer = 'ACME'; Amount = 1200; Items = @('a', 'b'); Nested = @{ ok = $true } }
    } -ErrorAction Stop
    $script:TaskId = [long]$created.id

    function script:Get-Fresh { Clear-OrchCache -Path "$($script:Drive):" 3>$null; Get-OrchTask -Path $script:Folder 3>$null }
}

AfterAll {
    if ($script:TaskId) { Remove-OrchTask -Path $script:Folder -Id $script:TaskId -Confirm:$false -ErrorAction SilentlyContinue 3>$null }
    Remove-Item $script:Folder -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue 3>$null
}

Describe 'Task cmdlets in a modern folder' {
    It 'Get-OrchTask lists the task' {
        $t = @(Get-Fresh)
        $t.Count | Should -Be 1
        $t[0].Id | Should -Be $script:TaskId
        $t[0].Title | Should -BeExactly $script:Title
        $t[0].Path | Should -Be $script:Folder
    }

    It 'Get-OrchTaskAcrossFolder lists it too' {
        @(Get-OrchTaskAcrossFolder -Path "$($script:Drive):" 3>$null | Where-Object Id -eq $script:TaskId).Count | Should -Be 1
    }

    It 'Get-OrchTaskData by -Id reads the data as native values' {
        $d = Get-OrchTaskData -Path $script:Folder -Id $script:TaskId 3>$null
        $d.Id | Should -Be $script:TaskId
        $d.Type | Should -BeExactly 'ExternalTask'
        $d.Status | Should -BeExactly 'Unassigned'
        $d.Path | Should -Be $script:Folder
        $d.Data.Customer | Should -BeExactly 'ACME'
        $d.Data.Amount | Should -Be 1200
        $d.Data.Items | Should -Be @('a', 'b')
        $d.Data.Nested.ok | Should -BeTrue
    }

    It 'Get-OrchTaskData takes the task from the pipeline' {
        $d = @(Get-Fresh | Get-OrchTaskData 3>$null)
        $d.Count | Should -Be 1
        $d[0].Data.Customer | Should -BeExactly 'ACME'
    }

    It 'Get-OrchTaskData reports a task that does not exist' {
        Get-OrchTaskData -Path $script:Folder -Id 1 -ErrorVariable err -ErrorAction SilentlyContinue 3>$null | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        $err[0].FullyQualifiedErrorId | Should -Match '^GetTaskDataError'
    }

    It 'Set-OrchTask changes the priority' {
        Get-Fresh | Set-OrchTask -Priority High -Confirm:$false 3>$null
        (Get-Fresh).Priority | Should -BeExactly 'High'
    }

    It 'Remove-OrchTask removes it, and it is no longer listed' {
        Remove-OrchTask -Path $script:Folder -Id $script:TaskId -Confirm:$false -ErrorAction Stop 3>$null
        @(Get-Fresh).Count | Should -Be 0
        @(Get-OrchTaskAcrossFolder -Path "$($script:Drive):" 3>$null | Where-Object Id -eq $script:TaskId).Count | Should -Be 0
        $script:TaskId = $null
    }
}
