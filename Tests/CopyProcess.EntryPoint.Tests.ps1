#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copying the same process twice in one session must give BOTH copies the source's entry point.

.DESCRIPTION
    CopyProcesses (behind Copy-Item and Copy-OrchProcess) maps the source entry point to the
    destination's: it looks the source EntryPointId up in the source package to get the workflow
    path, then takes the destination package's id for that path. It used to write that id onto the
    CACHED source release, so the destination's id outlived the copy. The next copy of the same
    process then looked a foreign id up in the source package, warned

        source entry point id <n> not found in package '<id> <version>'; copying the process
        without an entry point.

    and created the process with no entry point -- which the server fills with the package's main
    workflow. Get-OrchProcessDetail on the source showed the foreign id too. Present from the first
    release until 2026-10-05.

    Nothing caught it because it needs three things at once, and no test had all three:
      - the same process copied twice in one session;
      - entry point ids that differ between source and destination. Ids belong to a package version
        IN A FEED, so a copy within one feed rewrote the id to the value it already had;
      - an entry point other than the main workflow, or the server's fallback hides the loss.
        The shared fixture's processes all run BlankProcess19's Main.xaml.

    So this file seeds its own: a package with three entry points (Main.xaml, Sub.xaml, Sub2.xaml)
    in the tenant feed for the source, and in two destination folders that each have their OWN
    feed (FeedType FolderHierarchy), which gives each copy different ids. The source process runs
    Sub.xaml. Everything stays inside the disposable tenant.

    DELIBERATELY no Clear-OrchCache between the copies or before the assertions: the cache is
    where the damage lived.

.NOTES
    Run through the shared runner, which wipes and re-seeds the disposable tenant first:
        Tests\Invoke-AllTests.ps1 -Tenant Orch2 -Filter 'CopyProcess.EntryPoint*'

    The package is TestData\EntryPoints\, not the shared fixture's Packages\: Import-Fixture
    uploads everything in that folder for every test file, and other files assert on what the
    tenant feed and the fixture folders hold. This file uploads it itself and removes it, the
    folders and the processes in AfterAll.

    Positive control (2026-10-05): with the ShallowClone() after ReleasesDetailed.Get removed from
    CopyProcesses (CopyItem.Entities.cs), three of the four tests fail -- the warning fires, the
    second destination loses Sub.xaml, and the source is left with no entry point id. Only
    the precondition (the ids differ) passes. Restored, all four pass.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:SkipReason = $null

    $stamp = $PID
    $script:SrcRoot     = "$($script:Drive):\_cpep_src_$stamp"
    $script:DstRoots    = @("$($script:Drive):\_cpep_dst1_$stamp", "$($script:Drive):\_cpep_dst2_$stamp")
    $script:ProcessName = 'cpepProc'
    $script:PackageId   = '複数のエントリポイント'
    $script:PackageVer  = '1.0.5'
    $script:EntryPoint  = 'Sub.xaml'
    $script:PackageFile = Join-Path $PSScriptRoot "..\TestData\EntryPoints\$($script:PackageId).$($script:PackageVer).nupkg"
    $script:UploadedToTenant = $false

    try {
        if (-not (Test-Path -LiteralPath $script:PackageFile)) { throw "package not found: $($script:PackageFile)" }

        # Source: the tenant feed. Upload only if the tenant does not already hold it, so AfterAll
        # never removes a package this file did not put there.
        if (-not (Get-OrchPackage -Path "$($script:Drive):\" | Where-Object Id -eq $script:PackageId)) {
            Import-OrchPackage -Path "$($script:Drive):\" -Source $script:PackageFile -ErrorAction Stop | Out-Null
            $script:UploadedToTenant = $true
        }
        New-Item -ItemType Directory -Path $script:SrcRoot -Force -ErrorAction Stop | Out-Null
        New-OrchProcess -Path $script:SrcRoot -Id $script:PackageId -Version $script:PackageVer `
            -Name $script:ProcessName -EntryPoint $script:EntryPoint -ErrorAction Stop | Out-Null

        # Destinations: one feed each, so each copy maps to ids of its own.
        foreach ($dst in $script:DstRoots) {
            New-Item -Path "$($script:Drive):\" -Name (Split-Path $dst -Leaf) -ItemType Directory `
                -FeedType FolderHierarchy -ErrorAction Stop | Out-Null
            Import-OrchPackage -Path $dst -Source $script:PackageFile -ErrorAction Stop | Out-Null
        }

        # From the LISTING, not Get-OrchProcessDetail. Reading the detail first fills EntryPointPath
        # on the cached release the copy then posts, and the server takes that path when the id is
        # missing -- which hid the lost entry point from the destination check (seen on the
        # positive-control run). Nothing in the field reads the detail before a copy.
        $before = Get-OrchProcess -Path $script:SrcRoot -Name $script:ProcessName -ErrorAction Stop
        $script:SrcEntryPointIdBefore = $before.EntryPointId

        Write-Host "Copy-OrchProcess twice in one session ..." -ForegroundColor Cyan
        $script:CopyWarnings = @()
        foreach ($dst in $script:DstRoots) {
            Copy-OrchProcess -Path $script:SrcRoot -Name $script:ProcessName -Destination $dst `
                -Confirm:$false -WarningVariable copyWarnings -ErrorAction Stop
            $script:CopyWarnings += @($copyWarnings)
        }
    }
    catch {
        $script:SkipReason = "setup failed: $($_.Exception.Message)"
        Write-Host "SKIPPING: $($script:SkipReason)" -ForegroundColor Yellow
    }
}

AfterAll {
    foreach ($p in @($script:DstRoots) + @($script:SrcRoot)) {
        if ($p -and (Test-Path $p)) { Remove-Item -Path $p -Recurse -Confirm:$false -ErrorAction SilentlyContinue }
    }
    if ($script:UploadedToTenant) {
        Remove-OrchPackage -Path "$($script:Drive):\" -Id $script:PackageId -Confirm:$false -ErrorAction SilentlyContinue
    }
}

Describe 'Copying the same process twice keeps its entry point' {

    It 'gives the entry point a different id in each destination feed, or the test proves nothing' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $ids = foreach ($dst in $script:DstRoots) {
            (Get-OrchProcessDetail -Path $dst -Name $script:ProcessName).EntryPointId
        }
        $ids | Should -Not -Contain $script:SrcEntryPointIdBefore `
            -Because 'with the same id on both sides the old rewrite changed nothing and could not show'
    }

    It 'does not warn that the source entry point is missing from the source package' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $offending = @($script:CopyWarnings | Where-Object { "$_" -like '*entry point id*not found in package*' })
        $offending | Should -BeNullOrEmpty `
            -Because 'the second copy used to look the first destination''s id up in the source package'
    }

    It 'runs the source entry point in both destinations' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        foreach ($dst in $script:DstRoots) {
            (Get-OrchProcessDetail -Path $dst -Name $script:ProcessName).EntryPointPath | Should -Be $script:EntryPoint `
                -Because "the copy in '$dst' lost its entry point and fell back to Main.xaml"
        }
    }

    It 'leaves the source process reporting its own entry point' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        $after = Get-OrchProcessDetail -Path $script:SrcRoot -Name $script:ProcessName
        $after.EntryPointId | Should -Be $script:SrcEntryPointIdBefore `
            -Because 'the copy must not rewrite the cached source release'
        $after.EntryPointPath | Should -Be $script:EntryPoint
    }
}
