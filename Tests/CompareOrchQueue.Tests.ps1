#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration test for Compare-OrchQueue — one non-Asset folder-scoped cmdlet exercised
    end-to-end through the real provider, proving the shared FolderCompare engine works for a
    noun other than Asset (mode dispatch, "<=" / "=>" / "<>" / "==", broadcast, -Property).

.DESCRIPTION
    Requires a connected, writable Orch2: drive (run Import-OrchConfig first). Creates two
    sibling folders with queue definitions whose Description differs, and compares them.
    Everything is prefixed "PesterCmpQ_XXXX_" and removed in AfterAll.
#>

BeforeAll {
    $script:Drive  = 'Orch2'
    $script:Prefix = "PesterCmpQ_$(Get-Random -Maximum 9999)_"
    $script:RootA  = "${script:Drive}:\${script:Prefix}A"
    $script:RootB  = "${script:Drive}:\${script:Prefix}B"
    $script:SubA   = "${script:RootA}\Sub"
    $script:SubB   = "${script:RootB}\Sub"

    $script:Same    = "${script:Prefix}Same"
    $script:Changed = "${script:Prefix}Changed"
    $script:OnlyA   = "${script:Prefix}OnlyA"
    $script:OnlyB   = "${script:Prefix}OnlyB"
    $script:SubQ    = "${script:Prefix}SubQ"

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $null = mkdir $script:RootA
    $null = mkdir $script:RootB

    New-OrchQueue -Path $script:RootA -Name $script:Same    -Description 'd1' | Out-Null
    New-OrchQueue -Path $script:RootA -Name $script:Changed -Description 'd1' | Out-Null
    New-OrchQueue -Path $script:RootA -Name $script:OnlyA   -Description 'x'  | Out-Null

    New-OrchQueue -Path $script:RootB -Name $script:Same    -Description 'd1' | Out-Null
    New-OrchQueue -Path $script:RootB -Name $script:Changed -Description 'd2' | Out-Null
    New-OrchQueue -Path $script:RootB -Name $script:OnlyB   -Description 'y'  | Out-Null

    # Mirrored subfolders for the -Recurse test: same-named queue, differing Description.
    $null = mkdir $script:SubA
    $null = mkdir $script:SubB
    New-OrchQueue -Path $script:SubA -Name $script:SubQ -Description 'd1' | Out-Null
    New-OrchQueue -Path $script:SubB -Name $script:SubQ -Description 'd2' | Out-Null
}

AfterAll {
    Remove-OrchQueue -Name "${script:Prefix}*" -Path $script:RootA -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchQueue -Name "${script:Prefix}*" -Path $script:RootB -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item $script:SubA  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubB  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootA -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootB -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Compare-OrchQueue' {
    BeforeAll {
        $script:Result = Compare-OrchQueue -Name * -Path $script:RootA -DifferencePath $script:RootB
    }

    It 'reports a changed queue as "<>" with a Description difference' {
        $changed = $script:Result | Where-Object Name -eq $script:Changed
        $changed.SideIndicator | Should -Be '<>'
        ($changed.Differences | Where-Object Property -eq 'Description').DifferenceValue | Should -Be 'd2'
    }

    It 'reports reference-only "<=" and difference-only "=>"' {
        ($script:Result | Where-Object Name -eq $script:OnlyA).SideIndicator | Should -Be '<='
        ($script:Result | Where-Object Name -eq $script:OnlyB).SideIndicator | Should -Be '=>'
    }

    It 'suppresses equal queues by default and shows them with -IncludeEqual' {
        ($script:Result | Where-Object Name -eq $script:Same) | Should -BeNullOrEmpty
        $r = Compare-OrchQueue -Name * -Path $script:RootA -DifferencePath $script:RootB -IncludeEqual
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '=='
    }

    It 'broadcasts to a single named target with -DifferenceName' {
        # OnlyA (Description 'x') vs Same (Description 'd1') -> differ.
        $r = Compare-OrchQueue -Path $script:RootA -Name $script:OnlyA `
            -DifferencePath $script:RootB -DifferenceName $script:Same
        $r.SideIndicator | Should -Be '<>'
        $r.Name          | Should -Be $script:OnlyA
    }

    It 'errors when the named difference queue does not exist' {
        { Compare-OrchQueue -Path $script:RootA -Name $script:OnlyA `
            -DifferencePath $script:RootB -DifferenceName "${script:Prefix}Nope" -ErrorAction Stop } |
            Should -Throw
    }

    It 'warns on an unrecognized -Property name' {
        Compare-OrchQueue -Name * -Path $script:RootA -DifferencePath $script:RootB `
            -Property 'Bogus' -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Match 'unrecognized'
    }

    It 'does not see subfolder queues without -Recurse' {
        ($script:Result | Where-Object Name -eq $script:SubQ) | Should -BeNullOrEmpty
    }

    It 'descends into mirrored subfolders with -Recurse' {
        $r = Compare-OrchQueue -Name * -Path $script:RootA -DifferencePath $script:RootB -Recurse
        $sub = $r | Where-Object { $_.Name -eq $script:SubQ -and $_.Path -like '*\Sub\*' }
        $sub.SideIndicator | Should -Be '<>'
        ($sub.Differences | Where-Object Property -eq 'Description').DifferenceValue | Should -Be 'd2'
    }
}
