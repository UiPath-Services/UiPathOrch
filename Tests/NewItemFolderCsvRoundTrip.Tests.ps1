#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Folder structure round-trip: dir -ExportCsv | Import-Csv | New-Item reproduces
    the original folders with FeedType, Description, and wildcard-bearing names intact.

.DESCRIPTION
    Requires a connected, writable Orch2: drive (run Import-OrchConfig first). Creates two
    uniquely-named top-level folders (a modern + a classic one) and a small subtree under the
    modern one, then exports with `dir -ExportCsv`, wipes the subtree, re-imports with
    `Import-Csv | New-Item`, and asserts the recreated tree matches the original. Cleans up in
    AfterAll.

    Regression coverage for two traps that silently dropped data before:

    1. FeedType / Description are provider DYNAMIC parameters of New-Item, which PowerShell does
       NOT bind from the pipeline by property name. Without the provider reading them off the
       piped row (the object that lands on -Value), every recreated folder came back as
       'Processes' with no description -- a classic (FolderHierarchy) folder silently became
       modern. NewItem pulls them off the piped object as a fallback; these tests pin that.

    2. Folder names containing wildcard metacharacters ([ ] * ?). On the pipeline path the parent
       Path is matched literally (exact string), so a raw wildcard name round-trips correctly and
       must NOT be backtick-escaped on export -- escaping would make the exact-match miss and the
       child (and its subtree) would be dropped. These tests include a folder nested under a
       '[...]'-named parent to guard that exact-match path.

.NOTES
    Run with: Invoke-Pester -Path Tests\NewItemFolderCsvRoundTrip.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    $script:DrivePrefix = "${script:Drive}:\"
    $script:Prefix = "ZZPesterRT_$(Get-Random -Maximum 99999)"
    $script:Modern  = "${script:DrivePrefix}${script:Prefix}_Modern"
    $script:Classic = "${script:DrivePrefix}${script:Prefix}_Classic"

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    # Snapshot the folders under our prefix as a Rel -> {FeedType, Description} map.
    function script:Get-TreeSnapshot {
        $map = @{}
        Get-ChildItem $script:DrivePrefix -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "${script:DrivePrefix}$($script:Prefix)*" } |
            ForEach-Object {
                $rel = $_.FullName.Substring($script:DrivePrefix.Length)
                $map[$rel] = [pscustomobject]@{
                    FeedType    = "$($_.FeedType)"
                    Description = "$($_.Description)"   # coerce $null -> ''
                }
            }
        return $map
    }

    # --- Build the original tree -------------------------------------------------
    #   <Prefix>_Modern              (top-level, Processes, described)
    #     Fin[ance]                  (wildcard name, described)
    #       Child                    (nested UNDER a [..]-named parent, described)
    #     Star*Sub                   (wildcard name, no description)
    #     Q?Mark                     (wildcard name, no description)
    #   <Prefix>_Classic             (top-level, FolderHierarchy, described)
    New-Item -Path $script:DrivePrefix -Name "$($script:Prefix)_Modern"  -Description 'modern root'  | Out-Null
    New-Item -Path $script:DrivePrefix -Name "$($script:Prefix)_Classic" -FeedType FolderHierarchy -Description 'classic root' | Out-Null
    New-Item -Path $script:Modern -Name 'Fin[ance]' -Description 'bracket dept' | Out-Null
    # The parent has a literal '['; address it with a backtick-escaped -Path on this DIRECT call.
    New-Item -Path "$script:Modern\Fin``[ance``]" -Name 'Child' -Description 'nested under bracket' | Out-Null
    New-Item -Path $script:Modern -Name 'Star*Sub' | Out-Null
    New-Item -Path $script:Modern -Name 'Q?Mark'   | Out-Null

    $script:Before = script:Get-TreeSnapshot

    # --- Export with the real provider -ExportCsv --------------------------------
    $script:Csv = Join-Path $env:TEMP "NewItemFolderRT_$(Get-Random -Maximum 99999).csv"
    Get-ChildItem $script:DrivePrefix -Recurse -ExportCsv $script:Csv | Out-Null
    # Keep only the rows that belong to our tree (top folders + their descendants).
    $script:Rows = @(Import-Csv $script:Csv | Where-Object {
            $_.Name -like "$($script:Prefix)*" -or $_.Path -like "*$($script:Prefix)*"
        })

    # --- Wipe our tree -----------------------------------------------------------
    Remove-Item $script:Modern  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:Classic -Recurse -Force -ErrorAction SilentlyContinue
    $script:AfterWipe = script:Get-TreeSnapshot

    # --- Re-import the exported rows: Import-Csv | New-Item ----------------------
    # dir -Recurse exports parents before children, so New-Item sees each parent first.
    $script:Rows | New-Item -ErrorAction SilentlyContinue | Out-Null
    $script:After = script:Get-TreeSnapshot
}

AfterAll {
    Remove-Item $script:Modern  -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:Classic -Recurse -Force -ErrorAction SilentlyContinue
    if ($script:Csv -and (Test-Path $script:Csv)) { Remove-Item $script:Csv -Force -ErrorAction SilentlyContinue }
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'dir -ExportCsv | Import-Csv | New-Item folder round-trip' {

    It 'wiped the original tree before re-import (sanity)' {
        $script:AfterWipe.Keys.Count | Should -Be 0 -Because 'the export+wipe setup must clear the tree so the re-import is what recreates it'
        # And the original tree was actually built with the expected members.
        $script:Before.Keys | Should -Contain "$($script:Prefix)_Modern"
        $script:Before.Keys | Should -Contain "$($script:Prefix)_Modern\Fin[ance]\Child"
    }

    It 'reproduces every folder (structure, incl. wildcard names and nesting under a [..] parent)' {
        ($script:After.Keys | Sort-Object) | Should -Be ($script:Before.Keys | Sort-Object)
    }

    It 'preserves FeedType, including a classic (FolderHierarchy) top-level folder' {
        # The classic folder is the canary: a dropped FeedType silently turns it modern.
        $script:After["$($script:Prefix)_Classic"].FeedType | Should -Be 'FolderHierarchy'
        foreach ($rel in $script:Before.Keys) {
            $script:After[$rel].FeedType | Should -Be $script:Before[$rel].FeedType -Because "FeedType of '$rel' should round-trip"
        }
    }

    It 'preserves Description, including on a folder nested under a [..]-named parent' {
        $script:After["$($script:Prefix)_Modern\Fin[ance]\Child"].Description | Should -Be 'nested under bracket'
        foreach ($rel in $script:Before.Keys) {
            $script:After[$rel].Description | Should -Be $script:Before[$rel].Description -Because "Description of '$rel' should round-trip"
        }
    }
}
