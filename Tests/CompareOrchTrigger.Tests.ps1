#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration test for Compare-OrchTrigger — a folder-scoped noun whose entities come from the
    drive.GetTriggers accessor, exercised end-to-end through the shared FolderCompare engine
    (name-match, broadcast, -Recurse, -Property).

.DESCRIPTION
    Requires a connected, writable Orch2: drive with at least one package in its feed. Deploys a
    process into each test folder and creates time triggers referencing it, varying Enabled to
    produce differences. Skips if no package is available. Prefixed "PesterCmpTr_XXXX_" and
    removed in AfterAll.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:Prefix = "PesterCmpTr_$(Get-Random -Maximum 9999)_"
    $script:RootA  = "${script:Drive}:\${script:Prefix}A"
    $script:RootB  = "${script:Drive}:\${script:Prefix}B"
    $script:SubA   = "${script:RootA}\Sub"
    $script:SubB   = "${script:RootB}\Sub"

    $script:Proc    = "${script:Prefix}Proc"
    $script:Same    = "${script:Prefix}Same"
    $script:Changed = "${script:Prefix}Changed"
    $script:SubTrig = "${script:Prefix}SubTrig"
    $script:Cron    = '0 0 0 1/1 * ? *'

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $script:PackageId = (Get-OrchPackage -Path "${script:Drive}:\" -ErrorAction SilentlyContinue | Select-Object -First 1).Id

    $null = mkdir $script:RootA
    $null = mkdir $script:RootB
    $null = mkdir $script:SubA
    $null = mkdir $script:SubB

    if ($script:PackageId) {
        foreach ($f in $script:RootA, $script:RootB, $script:SubA, $script:SubB) {
            New-OrchProcess -Id $script:PackageId -Name $script:Proc -Path $f | Out-Null
        }
        New-OrchTrigger -Path $script:RootA -Name $script:Same    -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:RootA -Name $script:Changed -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:RootB -Name $script:Same    -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:RootB -Name $script:Changed -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled false | Out-Null
        New-OrchTrigger -Path $script:SubA  -Name $script:SubTrig -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled true  | Out-Null
        New-OrchTrigger -Path $script:SubB  -Name $script:SubTrig -ReleaseName $script:Proc -StartProcessCron $script:Cron -Enabled false | Out-Null
        Clear-OrchCache
    }
}

AfterAll {
    foreach ($f in $script:RootA, $script:RootB) {
        Remove-OrchTrigger -Name "${script:Prefix}*" -Path $f -Recurse -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchProcess -Name "${script:Prefix}*" -Path $f -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    }
    Remove-Item $script:SubA  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubB  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootA -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootB -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Compare-OrchTrigger' {
    It 'reports a changed trigger as "<>" with an Enabled difference' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB
        ($r | Where-Object Name -eq $script:Changed).SideIndicator | Should -Be '<>'
    }

    It 'suppresses equal triggers by default and shows them with -IncludeEqual' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB -IncludeEqual
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '=='
    }

    It 'broadcasts to a single named target with -DifferenceName' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        # Same@B (Enabled true) vs Changed@B (Enabled false) -> differ.
        $r = Compare-OrchTrigger -Path $script:RootB -Name $script:Same `
            -DifferencePath $script:RootB -DifferenceName $script:Changed
        $r.SideIndicator | Should -Be '<>'
    }

    It 'errors when the named difference trigger does not exist' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        { Compare-OrchTrigger -Path $script:RootA -Name $script:Same `
            -DifferencePath $script:RootB -DifferenceName "${script:Prefix}Nope" -ErrorAction Stop } |
            Should -Throw
    }

    It 'warns on an unrecognized -Property name' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB `
            -Property 'Bogus' -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Match 'unrecognized'
    }

    It 'descends into mirrored subfolders with -Recurse' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchTrigger -Name * -Path $script:RootA -DifferencePath $script:RootB -Recurse
        $sub = $r | Where-Object { $_.Name -eq $script:SubTrig -and $_.Path -like '*\Sub\*' }
        $sub.SideIndicator | Should -Be '<>'
    }
}
