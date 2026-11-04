#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Clear-OrchCache on the drive root clears the folders' caches too when given -Recurse or -Depth.

.DESCRIPTION
    `Clear-OrchCache Orch2:\` clears the tenant cache. With -Recurse or -Depth it ignored them and
    returned, so the folder caches the caller meant to discard silently survived (fixed in
    c7a3aed3). A cache's contents cannot be inspected from outside, so the test reads what the
    cmdlet decided to clear: every clear goes through ShouldProcess, and -Verbose turns each
    decision into a "Performing the operation "<action>" on target "<path>"" record.

.NOTES
    Run through the shared runner:
        Tests\Invoke-AllTests.ps1 -Tenant Orch2 -Filter 'ClearOrchCache*'

    Creates _cc_<pid>\Sub on the target drive and removes it in AfterAll. Writes nothing else; the
    cache clears themselves are harmless.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:Root  = "$($script:Drive):\"
    $script:Top   = "_cc_$PID"
    $script:TopPath = "$($script:Drive):\$($script:Top)"
    $script:SubPath = "$($script:TopPath)\Sub"

    New-Item -ItemType Directory -Path $script:TopPath -Force -ErrorAction Stop | Out-Null
    New-Item -ItemType Directory -Path $script:SubPath -Force -ErrorAction Stop | Out-Null
    # The new folders must be in the drive's folder list for the walk to reach them.
    Clear-OrchCache "$($script:Drive):"

    function script:Get-ClearTargets {
        param([string]$Action, [hashtable]$Arguments)
        $records = @(Clear-OrchCache @Arguments -Verbose 4>&1 | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] })
        @($records | ForEach-Object {
            if ($_.Message -match "^Performing the operation `"$([regex]::Escape($Action))`" on target `"(.*)`"\.$") { $Matches[1] }
        })
    }
}

AfterAll {
    if ($script:TopPath) { Remove-Item -Path $script:TopPath -Recurse -ErrorAction SilentlyContinue }
}

Describe 'Clear-OrchCache on the drive root' {

    It 'without -Recurse clears the tenant cache only' {
        $tenant  = script:Get-ClearTargets 'Clear Tenant Cache' @{ Path = $script:Root }
        $folders = script:Get-ClearTargets 'Clear Folder Cache' @{ Path = $script:Root }
        $tenant.Count | Should -Be 1
        $folders | Should -BeNullOrEmpty -Because 'the drive-root form alone is the tenant scope'
    }

    It 'with -Recurse clears the tenant cache and every folder below' {
        $tenant  = script:Get-ClearTargets 'Clear Tenant Cache' @{ Path = $script:Root; Recurse = $true }
        $folders = script:Get-ClearTargets 'Clear Folder Cache' @{ Path = $script:Root; Recurse = $true }
        $tenant.Count | Should -Be 1 -Because 'the root is still cleared as the tenant scope'
        $folders | Should -Contain $script:TopPath -Because '-Recurse on the root used to be ignored'
        $folders | Should -Contain $script:SubPath
    }

    It 'with -Depth 1 clears the top-level folders only' {
        $folders = script:Get-ClearTargets 'Clear Folder Cache' @{ Path = $script:Root; Depth = 1 }
        $folders | Should -Contain $script:TopPath
        $folders | Should -Not -Contain $script:SubPath -Because 'Sub is two levels below the root'
    }
}
