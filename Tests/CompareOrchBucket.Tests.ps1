#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration test for Compare-OrchBucket — a folder-scoped non-Asset noun through the shared
    FolderCompare engine (mode dispatch, "<=" / "=>" / "<>" / "==", broadcast, -Recurse,
    -Property), end-to-end through the real provider.

.DESCRIPTION
    Requires a connected, writable Orch2: drive. Creates two sibling folders (and subfolders)
    with bucket definitions whose Description differs. Prefixed "PesterCmpBk_XXXX_" and removed
    in AfterAll.
#>

BeforeAll {
    $script:Drive  = 'Orch2'
    $script:Prefix = "PesterCmpBk_$(Get-Random -Maximum 9999)_"
    $script:RootA  = "${script:Drive}:\${script:Prefix}A"
    $script:RootB  = "${script:Drive}:\${script:Prefix}B"
    $script:SubA   = "${script:RootA}\Sub"
    $script:SubB   = "${script:RootB}\Sub"

    $script:Same    = "${script:Prefix}Same"
    $script:Changed = "${script:Prefix}Changed"
    $script:OnlyA   = "${script:Prefix}OnlyA"
    $script:OnlyB   = "${script:Prefix}OnlyB"
    $script:SubBkt  = "${script:Prefix}SubBkt"

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $null = mkdir $script:RootA
    $null = mkdir $script:RootB
    $null = mkdir $script:SubA
    $null = mkdir $script:SubB

    New-OrchBucket -Path $script:RootA -Name $script:Same    -Description 'd1' | Out-Null
    New-OrchBucket -Path $script:RootA -Name $script:Changed -Description 'd1' | Out-Null
    New-OrchBucket -Path $script:RootA -Name $script:OnlyA   -Description 'x'  | Out-Null
    New-OrchBucket -Path $script:SubA  -Name $script:SubBkt  -Description 'd1' | Out-Null

    New-OrchBucket -Path $script:RootB -Name $script:Same    -Description 'd1' | Out-Null
    New-OrchBucket -Path $script:RootB -Name $script:Changed -Description 'd2' | Out-Null
    New-OrchBucket -Path $script:RootB -Name $script:OnlyB   -Description 'y'  | Out-Null
    New-OrchBucket -Path $script:SubB  -Name $script:SubBkt  -Description 'd2' | Out-Null

    Clear-OrchCache
}

AfterAll {
    Remove-OrchBucket -Name "${script:Prefix}*" -Path $script:RootA -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchBucket -Name "${script:Prefix}*" -Path $script:RootB -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item $script:SubA  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubB  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootA -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootB -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Compare-OrchBucket' {
    BeforeAll {
        $script:Result = Compare-OrchBucket -Name * -Path $script:RootA -DifferencePath $script:RootB
    }

    It 'reports a changed bucket as "<>" with a Description difference' {
        $changed = $script:Result | Where-Object Name -eq $script:Changed
        $changed.SideIndicator | Should -Be '<>'
        ($changed.Differences | Where-Object Property -eq 'Description').DifferenceValue | Should -Be 'd2'
    }

    It 'reports reference-only "<=" and difference-only "=>"' {
        ($script:Result | Where-Object Name -eq $script:OnlyA).SideIndicator | Should -Be '<='
        ($script:Result | Where-Object Name -eq $script:OnlyB).SideIndicator | Should -Be '=>'
    }

    It 'suppresses equal buckets by default and shows them with -IncludeEqual' {
        ($script:Result | Where-Object Name -eq $script:Same) | Should -BeNullOrEmpty
        $r = Compare-OrchBucket -Name * -Path $script:RootA -DifferencePath $script:RootB -IncludeEqual
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '=='
    }

    It 'broadcasts to a single named target with -DifferenceName' {
        $r = Compare-OrchBucket -Path $script:RootA -Name $script:OnlyA `
            -DifferencePath $script:RootB -DifferenceName $script:Same
        $r.SideIndicator | Should -Be '<>'
        $r.Name          | Should -Be $script:OnlyA
    }

    It 'errors when the named difference bucket does not exist' {
        { Compare-OrchBucket -Path $script:RootA -Name $script:OnlyA `
            -DifferencePath $script:RootB -DifferenceName "${script:Prefix}Nope" -ErrorAction Stop } |
            Should -Throw
    }

    It 'warns on an unrecognized -Property name' {
        Compare-OrchBucket -Name * -Path $script:RootA -DifferencePath $script:RootB `
            -Property 'Bogus' -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Match 'unrecognized'
    }

    It 'does not see subfolder buckets without -Recurse' {
        ($script:Result | Where-Object Name -eq $script:SubBkt) | Should -BeNullOrEmpty
    }

    It 'descends into mirrored subfolders with -Recurse' {
        $r = Compare-OrchBucket -Name * -Path $script:RootA -DifferencePath $script:RootB -Recurse
        $sub = $r | Where-Object { $_.Name -eq $script:SubBkt -and $_.Path -like '*\Sub\*' }
        $sub.SideIndicator | Should -Be '<>'
    }
}
