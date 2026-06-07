#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration test for Compare-OrchProcess, focused on -Recurse (mirrored subfolder
    comparison) plus the basic name-match differences.

.DESCRIPTION
    Requires a connected, writable Orch2: drive with at least one package in its feed (run
    Import-OrchConfig first). Creates two sibling folders, each with a subfolder, deploys a
    process from the same package into each (top-level equal, subfolder differing by
    Description), and compares them. Skips if no package is available. Everything is prefixed
    "PesterCmpP_XXXX_" and removed in AfterAll.
#>

BeforeAll {
    $script:Drive  = 'Orch2'
    $script:Prefix = "PesterCmpP_$(Get-Random -Maximum 9999)_"
    $script:RootA  = "${script:Drive}:\${script:Prefix}A"
    $script:RootB  = "${script:Drive}:\${script:Prefix}B"
    $script:SubA   = "${script:RootA}\Sub"
    $script:SubB   = "${script:RootB}\Sub"

    $script:Top = "${script:Prefix}Top"
    $script:Sub = "${script:Prefix}Sub"

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $script:PackageId = (Get-OrchPackage -Path "${script:Drive}:\" -ErrorAction SilentlyContinue | Select-Object -First 1).Id

    $null = mkdir $script:RootA
    $null = mkdir $script:RootB
    $null = mkdir $script:SubA
    $null = mkdir $script:SubB

    if ($script:PackageId) {
        # Top level: same name + same Description -> equal.
        New-OrchProcess -Id $script:PackageId -Name $script:Top -Description 'd1' -Path $script:RootA | Out-Null
        New-OrchProcess -Id $script:PackageId -Name $script:Top -Description 'd1' -Path $script:RootB | Out-Null
        # Subfolder: same name, differing Description -> "<>" only visible with -Recurse.
        New-OrchProcess -Id $script:PackageId -Name $script:Sub -Description 'd1' -Path $script:SubA | Out-Null
        New-OrchProcess -Id $script:PackageId -Name $script:Sub -Description 'd2' -Path $script:SubB | Out-Null
    }
}

AfterAll {
    Remove-OrchProcess -Name "${script:Prefix}*" -Path $script:RootA -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchProcess -Name "${script:Prefix}*" -Path $script:RootB -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item $script:SubA  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubB  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootA -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootB -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Compare-OrchProcess -Recurse' {
    It 'does not see subfolder processes without -Recurse' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchProcess -Name * -Path $script:RootA -DifferencePath $script:RootB
        ($r | Where-Object Name -eq $script:Sub) | Should -BeNullOrEmpty
    }

    It 'descends into mirrored subfolders with -Recurse' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchProcess -Name * -Path $script:RootA -DifferencePath $script:RootB -Recurse
        $sub = $r | Where-Object { $_.Name -eq $script:Sub -and $_.Path -like '*\Sub\*' }
        $sub.SideIndicator | Should -Be '<>'
        ($sub.Differences | Where-Object Property -eq 'Description').DifferenceValue | Should -Be 'd2'
    }

    It 'treats the equal top-level process as == under -Recurse -IncludeEqual' {
        if (-not $script:PackageId) { Set-ItResult -Skipped -Because 'no package on test drive'; return }
        $r = Compare-OrchProcess -Name * -Path $script:RootA -DifferencePath $script:RootB -Recurse -IncludeEqual
        ($r | Where-Object Name -eq $script:Top).SideIndicator | Should -Be '=='
    }
}
