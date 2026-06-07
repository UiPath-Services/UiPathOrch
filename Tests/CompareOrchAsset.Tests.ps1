#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration tests for Compare-OrchAsset — the cmdlet-level behavior that the C# unit
    tests (CompareAssetTests.cs) can't reach: mode dispatch, "<=" / "=>" enumeration, folder
    mirroring with -Recurse, the DifferenceName-not-found error, and the -Property warning.

.DESCRIPTION
    Requires a connected, writable Orch2: drive (run Import-OrchConfig first). Creates two
    sibling folders (…A and …B) on Orch2, populates them so every SideIndicator is exercised,
    and compares folder-to-folder within the one tenant (cross-tenant user mapping is covered
    by the unit tests). Everything is prefixed "PesterCmp_XXXX_" and removed in AfterAll.

.NOTES
    Run with: Invoke-Pester -Path Tests\CompareOrchAsset.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive  = 'Orch2'
    $script:Prefix = "PesterCmp_$(Get-Random -Maximum 9999)_"

    $script:RootA = "${script:Drive}:\${script:Prefix}A"
    $script:RootB = "${script:Drive}:\${script:Prefix}B"
    $script:SubA  = "${script:RootA}\Sub"
    $script:SubB  = "${script:RootB}\Sub"

    # Asset names (identical on both sides so name-match pairs them).
    $script:Same    = "${script:Prefix}Same"     # equal on both sides
    $script:Changed = "${script:Prefix}Changed"  # present on both sides, value differs
    $script:OnlyA   = "${script:Prefix}OnlyA"    # reference-only  => "<="
    $script:OnlyB   = "${script:Prefix}OnlyB"    # difference-only => "=>"
    $script:Sub     = "${script:Prefix}SubAsset" # under Sub\, value differs (for -Recurse)

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    $null = mkdir $script:RootA
    $null = mkdir $script:RootB
    $null = mkdir $script:SubA
    $null = mkdir $script:SubB

    # Reference side (A)
    Set-OrchAsset -Path $script:RootA -ValueType Text -Name $script:Same    -Value 'v1'
    Set-OrchAsset -Path $script:RootA -ValueType Text -Name $script:Changed -Value 'v1' -Description 'shared-desc'
    Set-OrchAsset -Path $script:RootA -ValueType Text -Name $script:OnlyA   -Value 'x'
    Set-OrchAsset -Path $script:SubA  -ValueType Text -Name $script:Sub     -Value 'a'

    # Difference side (B)
    Set-OrchAsset -Path $script:RootB -ValueType Text -Name $script:Same    -Value 'v1'
    Set-OrchAsset -Path $script:RootB -ValueType Text -Name $script:Changed -Value 'v2' -Description 'shared-desc'
    Set-OrchAsset -Path $script:RootB -ValueType Text -Name $script:OnlyB   -Value 'y'
    Set-OrchAsset -Path $script:SubB  -ValueType Text -Name $script:Sub     -Value 'b'
}

AfterAll {
    Remove-OrchAsset -Name "${script:Prefix}*" -Path $script:RootA -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchAsset -Name "${script:Prefix}*" -Path $script:RootB -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item $script:SubA  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubB  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootA -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootB -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Compare-OrchAsset name-match mode' {
    BeforeAll {
        $script:Result = Compare-OrchAsset -Path $script:RootA -DifferencePath $script:RootB
    }

    It 'reports a changed asset as "<>"' {
        ($script:Result | Where-Object Name -eq $script:Changed).SideIndicator | Should -Be '<>'
    }

    It 'reports a reference-only asset as "<="' {
        ($script:Result | Where-Object Name -eq $script:OnlyA).SideIndicator | Should -Be '<='
    }

    It 'reports a difference-only asset as "=>"' {
        ($script:Result | Where-Object Name -eq $script:OnlyB).SideIndicator | Should -Be '=>'
    }

    It 'suppresses equal assets by default' {
        ($script:Result | Where-Object Name -eq $script:Same) | Should -BeNullOrEmpty
    }

    It 'emits "==" for equal assets with -IncludeEqual' {
        $r = Compare-OrchAsset -Path $script:RootA -DifferencePath $script:RootB -IncludeEqual
        ($r | Where-Object Name -eq $script:Same).SideIndicator | Should -Be '=='
    }

    It 'carries the per-property breakdown on a "<>" row' {
        $changed = $script:Result | Where-Object Name -eq $script:Changed
        $vd = $changed.Differences | Where-Object Property -eq 'Value'
        $vd.ReferenceValue  | Should -Be 'v1'
        $vd.DifferenceValue | Should -Be 'v2'
    }

    It 'exposes single-sided Path / DifferencePath' {
        $changed = $script:Result | Where-Object Name -eq $script:Changed
        $changed.Path           | Should -BeLike "*${script:Prefix}A\*"
        $changed.DifferencePath | Should -BeLike "*${script:Prefix}B\*"

        ($script:Result | Where-Object Name -eq $script:OnlyA).DifferencePath | Should -BeNullOrEmpty
        ($script:Result | Where-Object Name -eq $script:OnlyB).Path           | Should -BeNullOrEmpty
    }

    It 'honors a -Name filter on both sides' {
        $r = Compare-OrchAsset -Path $script:RootA -DifferencePath $script:RootB -Name "${script:Prefix}Only*"
        ($r | Where-Object Name -eq $script:OnlyA).SideIndicator | Should -Be '<='
        ($r | Where-Object Name -eq $script:OnlyB).SideIndicator | Should -Be '=>'
        ($r | Where-Object Name -eq $script:Changed)             | Should -BeNullOrEmpty
    }

    It 'descends into mirrored subfolders with -Recurse' {
        $r = Compare-OrchAsset -Path $script:RootA -DifferencePath $script:RootB -Recurse
        $sub = $r | Where-Object { $_.Name -eq $script:Sub -and $_.Path -like '*\Sub\*' }
        $sub.SideIndicator | Should -Be '<>'
    }
}

Describe 'Compare-OrchAsset broadcast mode (-DifferenceName)' {
    It 'compares a reference asset to a differently named target' {
        # OnlyA (value 'x') vs Same (value 'v1') -> differ.
        $r = Compare-OrchAsset -Path $script:RootA -Name $script:OnlyA `
            -DifferencePath $script:RootB -DifferenceName $script:Same
        $r.SideIndicator | Should -Be '<>'
        $r.Name          | Should -Be $script:OnlyA
    }

    It 'reports "==" for equal values under different names (-IncludeEqual)' {
        # Changed@A (value 'v1') vs Same@B (value 'v1') -> equal values, different names.
        # Scoped to Value so the assets' unrelated Description difference doesn't intrude.
        $r = Compare-OrchAsset -Path $script:RootA -Name $script:Changed `
            -DifferencePath $script:RootB -DifferenceName $script:Same -Property 'Value' -IncludeEqual
        $r.SideIndicator | Should -Be '=='
    }

    It 'errors when the named difference asset does not exist' {
        { Compare-OrchAsset -Path $script:RootA -Name $script:OnlyA `
            -DifferencePath $script:RootB -DifferenceName "${script:Prefix}Nope" -ErrorAction Stop } |
            Should -Throw
    }
}

Describe 'Compare-OrchAsset -Property' {
    It 'warns and ignores an unrecognized property name' {
        Compare-OrchAsset -Path $script:RootA -DifferencePath $script:RootB `
            -Property 'Bogus' -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Match 'unrecognized'
    }

    It 'restricts the comparison to the named property' {
        # Changed differs only in Value; comparing on Description alone makes it equal,
        # so it drops out (no -IncludeEqual) leaving only the existence differences.
        $r = Compare-OrchAsset -Path $script:RootA -DifferencePath $script:RootB -Property 'Description'
        ($r | Where-Object Name -eq $script:Changed) | Should -BeNullOrEmpty
        ($r | Where-Object Name -eq $script:OnlyA).SideIndicator | Should -Be '<='
    }
}
