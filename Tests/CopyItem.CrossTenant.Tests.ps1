#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copy-Item CROSS-TENANT verification: copy a self-seeded source folder from
    one Orchestrator tenant to a DIFFERENT tenant and verify the per-folder
    entities -- and their name-resolved references -- reach the destination.

.DESCRIPTION
    Sister test to CopyItem.RoundTrip.Tests.ps1. That test copies WITHIN a
    single tenant to isolate "is enumeration + creation correct?". THIS test
    copies ACROSS tenants (src Orch2: -> dst Orch1:, different svc) to exercise
    the cross-tenant-only code paths the same-tenant test deliberately skips:

        - FindDstUser : resolve a source folder user against the DST tenant's
                        directory (not the source's) and re-assign it.
        - FindDstRole : resolve the role name ('Automation User', a built-in
                        role present in every tenant) on the dst side.
        - cross-drive Copy* : a Copy* method writing to a different drive/tenant
                        than the one it enumerated from.

            _xt_src_<pid>  (src tenant, Orch2:)
                  |
                  v   Copy-Item -Recurse   (across tenants)
                  v
            _xt_dst_<pid>\_xt_src_<pid>  (dst tenant, Orch1:)

    Scope is intentionally a SELF-CONTAINED entity set that resolves
    cross-tenant WITHOUT tenant-specific prerequisites in the destination:

        folders, assets, queues, buckets, folder users.

    Deliberately EXCLUDED (they'd fail for ENVIRONMENT reasons, not Copy* bugs,
    because the dst tenant lacks the prerequisite):
        - machines                          : tenant-specific; the dst tenant
                                              won't have the src's machines.
        - packages / processes / test sets / triggers : need the package
                                              uploaded into the dst feed -- heavy
                                              and it pollutes the (real) dst
                                              tenant.
    Those are covered SAME-tenant by CopyItem.RoundTrip.Tests.ps1.

    The source folder is seeded fresh in BeforeAll (NOT the shared fixture --
    cross-tenant doesn't want the fixture's machines/packages) and BOTH the
    source and destination temp folders are removed in AfterAll.

.NOTES
    Run with: Invoke-Pester -Path Tests\CopyItem.CrossTenant.Tests.ps1 -Output Detailed

    Prereqs:
    - BOTH drives (default src Orch2, dst Orch1) mounted and authenticated
      (Import-OrchConfig). The whole file SKIPS (not fails) if the dst drive is
      absent or if the two drives resolve to the same tenant -- then it would
      not be a cross-tenant test.
    - For the folder-user resolution to succeed the dst tenant's directory must
      contain the seeded user. The default seed picks an existing DirectoryUser
      of the src tenant; under a shared org directory (same org, different svc)
      that user is resolvable in the dst tenant too.

    Creates _xt_src_<pid> on the src drive and _xt_dst_<pid> on the dst drive;
    removes both in AfterAll. If the test crashes leaving them behind, clean up
    with Remove-Item -Recurse on each.
#>

BeforeAll {
    # Env-var overrides so the run can swap in any pair of connected drives
    # (Orch2 + Orch1 is the default convention used in repo docs, but Orch1
    # can be unavailable at run time -- e.g. PKCE not completed -- and any
    # two distinct OrchDrive mounts work equally well).
    $script:SrcDrive = if ($env:UIPATHORCH_TEST_DRIVE)     { $env:UIPATHORCH_TEST_DRIVE     } else { 'Orch2' }
    $script:DstDrive = if ($env:UIPATHORCH_TEST_DST_DRIVE) { $env:UIPATHORCH_TEST_DST_DRIVE } else { 'Orch1' }
    $script:SkipReason = $null

    $rnd = $PID
    $script:SrcRoot     = "${script:SrcDrive}:\_xt_src_$rnd"
    $script:DstRoot     = "${script:DstDrive}:\_xt_dst_$rnd"
    # Copy-Item creates a child folder under dst named after src; the effective
    # path of the copy is dst/<srcRootName>.
    $script:DstCopyRoot = "$($script:DstRoot)\_xt_src_$rnd"
    $script:ExportDir   = Join-Path $env:TEMP "CopyItemXT_$rnd"
    $script:SeededFolderUser = $null

    # --- generic helpers (ported verbatim from CopyItem.RoundTrip.Tests.ps1;
    #     they are drive-agnostic, so they work cross-tenant unchanged) -------
    function script:Normalize-Rows {
        param(
            [object[]]$Rows,
            [string]$RootPrefix,
            [string[]]$PathColumns = @('Path', 'Link')
        )
        if (-not $Rows -or $Rows.Count -eq 0) { return @() }
        $escaped = [regex]::Escape($RootPrefix)
        foreach ($row in $Rows) {
            foreach ($col in $PathColumns) {
                if ($row.PSObject.Properties.Match($col).Count -gt 0) {
                    $v = $row.$col
                    if ($null -ne $v) { $row.$col = ($v -replace "^$escaped", '<ROOT>') }
                }
            }
            foreach ($prop in $row.PSObject.Properties) {
                if ($null -eq $prop.Value) { $prop.Value = '' }
            }
        }
        $hasName = $Rows[0].PSObject.Properties.Match('Name').Count -gt 0
        if ($hasName) { return $Rows | Sort-Object Path, Name }
        return $Rows | Sort-Object Path
    }

    function script:Compare-CopySubset {
        param(
            [object[]]$SrcRows,
            [object[]]$DstRows,
            [string[]]$ColumnsToCheck,
            [string]$EntityLabel
        )
        $key = { param($r) "$($r.Path)|$($r.Name)" }
        $dstByKey = @{}
        foreach ($r in $DstRows) { $dstByKey[(& $key $r)] = $r }
        foreach ($e in $SrcRows) {
            $k = & $key $e
            $dstByKey.ContainsKey($k) | Should -BeTrue `
                -Because "$EntityLabel row '$k' should exist on the cross-tenant copy destination after Copy-Item -Recurse"
            $a = $dstByKey[$k]
            foreach ($col in $ColumnsToCheck) {
                $eVal = if ($e.PSObject.Properties.Match($col).Count -gt 0) { $e.$col } else { '' }
                $aVal = if ($a.PSObject.Properties.Match($col).Count -gt 0) { $a.$col } else { '' }
                $aVal | Should -Be $eVal -Because "$EntityLabel row '$k' column '$col' should round-trip through a cross-tenant Copy-Item"
            }
        }
    }

    function script:Export-AndDiff {
        param(
            [scriptblock]$Getter,
            [string]$Name,
            [string[]]$ColumnsToCheck
        )
        $srcCsv = Join-Path $script:ExportDir "src_$Name.csv"
        $dstCsv = Join-Path $script:ExportDir "dst_$Name.csv"
        & $Getter $script:SrcRoot     $srcCsv
        & $Getter $script:DstCopyRoot $dstCsv
        $srcRows = script:Normalize-Rows (Import-Csv $srcCsv) $script:SrcRoot
        $dstRows = script:Normalize-Rows (Import-Csv $dstCsv) $script:DstCopyRoot
        script:Compare-CopySubset $srcRows $dstRows $ColumnsToCheck $Name
    }

    # --- prereqs: both drives present, and they must be DIFFERENT tenants -----
    $srcDriveObj = Get-PSDrive $script:SrcDrive -ErrorAction SilentlyContinue
    $dstDriveObj = Get-PSDrive $script:DstDrive -ErrorAction SilentlyContinue
    if (-not $srcDriveObj) {
        $script:SkipReason = "Source drive '$($script:SrcDrive):' not mounted."
    }
    elseif (-not $dstDriveObj) {
        $script:SkipReason = "Destination drive '$($script:DstDrive):' not mounted (cross-tenant test needs a second tenant)."
    }
    else {
        $srcUrl = (Get-OrchPSDrive | Where-Object Name -eq $script:SrcDrive).Root
        $dstUrl = (Get-OrchPSDrive | Where-Object Name -eq $script:DstDrive).Root
        if ($srcUrl -and $dstUrl -and $srcUrl -eq $dstUrl) {
            $script:SkipReason = "Drives '$($script:SrcDrive):' and '$($script:DstDrive):' point at the same tenant ($srcUrl); not a cross-tenant test."
        }
        else {
            Write-Host "Cross-tenant copy: $srcUrl  -->  $dstUrl" -ForegroundColor Cyan
        }
    }

    if (-not $script:SkipReason) {
        New-Item -ItemType Directory -Path $script:ExportDir -Force | Out-Null

        # ---- seed the source folder in the src tenant ----
        Write-Host "Seeding source folder '$($script:SrcRoot)' ..." -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $script:SrcRoot -Force -ErrorAction Stop | Out-Null
        # a nested subfolder, to confirm the tree replicates across tenants
        New-Item -ItemType Directory -Path "$($script:SrcRoot)\Child" -Force -ErrorAction Stop | Out-Null

        Set-OrchAsset -Path $script:SrcRoot -ValueType Text -Name xtAssetA -Value 'xt-text-a' -Description 'xt asset A' -ErrorAction Stop | Out-Null
        Set-OrchAsset -Path $script:SrcRoot -ValueType Text -Name xtAssetB -Value 'xt-text-b' -Description 'xt asset B' -ErrorAction Stop | Out-Null
        New-OrchQueue  -Path $script:SrcRoot -Name xtQueue  -Description 'xt queue desc'  -ErrorAction Stop | Out-Null
        New-OrchBucket -Path $script:SrcRoot -Name xtBucket -Description 'xt bucket desc' -ErrorAction Stop | Out-Null

        # folder user: a directory user of the src tenant other than me. Under a
        # shared org directory it also resolves in the dst tenant (FindDstUser).
        $me  = (Get-OrchCurrentUser -Path "$($script:SrcDrive):\").UserName
        $alt = Get-OrchUser -Path "$($script:SrcDrive):\" -Type DirectoryUser |
                    Where-Object UserName -ne $me | Select-Object -First 1
        if ($alt) {
            $script:SeededFolderUser = if ($alt.EmailAddress) { $alt.EmailAddress } else { $alt.UserName }
            Add-OrchFolderUser -Path $script:SrcRoot -Type DirectoryUser -UserName $script:SeededFolderUser -Roles 'Automation User' -ErrorAction Stop | Out-Null
        }
        Clear-OrchCache -Path $script:SrcRoot

        # ---- cross-tenant copy ----
        Write-Host "Copy-Item -Recurse (cross-tenant) to '$($script:DstRoot)' ..." -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $script:DstRoot -Force -ErrorAction Stop | Out-Null
        Copy-Item -Path $script:SrcRoot -Destination $script:DstRoot -Recurse -ErrorAction Stop
        Clear-OrchCache -Path $script:DstRoot
    }
    else {
        Write-Host "SKIPPING cross-tenant Copy-Item tests: $($script:SkipReason)" -ForegroundColor Yellow
    }
}

AfterAll {
    if ($script:ExportDir -and (Test-Path $script:ExportDir)) {
        Remove-Item $script:ExportDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($script:DstRoot -and (Test-Path $script:DstRoot)) {
        Write-Host "Removing dst temp folder '$($script:DstRoot)' ..." -ForegroundColor Cyan
        Remove-Item $script:DstRoot -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
    }
    if ($script:SrcRoot -and (Test-Path $script:SrcRoot)) {
        Write-Host "Removing src temp folder '$($script:SrcRoot)' ..." -ForegroundColor Cyan
        Remove-Item $script:SrcRoot -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
    }
}

# ============================================================================
# Each test exports a kind from both the source folder (src tenant) and the
# copied destination folder (dst tenant), then asserts the destination contains
# every source row. A row in src but missing in dst means a Copy* method
# silently dropped it across the tenant boundary -- OR a FindDst* resolver
# failed to map a name into the dst tenant. That's the bug class this file adds
# on top of the same-tenant CopyItem.RoundTrip suite.
# ============================================================================

Describe 'Copy-Item -Recurse cross-tenant preserves per-folder entities' {

    It 'subfolder tree is replicated across tenants' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        function Get-RelativeFolders($rootPSPath) {
            $rootFolder = Get-Item $rootPSPath -ErrorAction Stop
            $rootFqn = $rootFolder.FullyQualifiedName
            Get-ChildItem $rootPSPath -Recurse -Force |
                Where-Object PSIsContainer |
                ForEach-Object {
                    if ($rootFqn) { $_.FullyQualifiedName -replace "^$([regex]::Escape($rootFqn))/", '' }
                    else { $_.FullyQualifiedName }
                } |
                Where-Object { $_ -ne '' } |
                Sort-Object
        }
        $srcFolders = @(Get-RelativeFolders $script:SrcRoot)
        $dstFolders = @(Get-RelativeFolders $script:DstCopyRoot)
        foreach ($f in $srcFolders) {
            $dstFolders | Should -Contain $f -Because "subfolder '$f' should exist after a cross-tenant Copy-Item -Recurse"
        }
    }

    It 'assets round-trip across tenants' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchAsset -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'assets' `
            -ColumnsToCheck @('Description', 'ValueType')
    }

    It 'queues round-trip across tenants' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchQueue -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'queues' `
            -ColumnsToCheck @('Description')
    }

    It 'buckets round-trip across tenants' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        Export-AndDiff `
            -Getter { param($path, $csv) Get-OrchBucket -Path $path -Recurse -ExportCsv $csv | Out-Null } `
            -Name 'buckets' `
            -ColumnsToCheck @('Description')
    }

    It 'folder users resolve and round-trip across tenants' {
        if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }
        if (-not $script:SeededFolderUser) {
            Set-ItResult -Skipped -Because 'no DirectoryUser other than the current user was available to seed'
            return
        }
        # This is THE cross-tenant-specific check: CopyFolderUsers must resolve
        # each source folder user against the DST tenant's directory (FindDstUser)
        # and the role name against the dst tenant's roles (FindDstRole), then
        # re-assign. Get-OrchFolderUser's identity column is UserName (not Name).
        function script:Get-FolderUserRows($root, $csvName) {
            $csv = Join-Path $script:ExportDir $csvName
            Get-OrchFolderUser -Path $root -Recurse -ExportCsv $csv | Out-Null
            $esc = [regex]::Escape($root)
            Import-Csv $csv | ForEach-Object {
                [pscustomobject]@{
                    Key   = (($_.Path -replace "^$esc", '<ROOT>') + '|' + $_.UserName)
                    Type  = [string]$_.Type
                    Roles = [string]$_.FolderRoles
                }
            }
        }
        $src = @(script:Get-FolderUserRows $script:SrcRoot     'src_folder_users.csv')
        $dst = @(script:Get-FolderUserRows $script:DstCopyRoot 'dst_folder_users.csv')

        # the seeded user must be among the source rows (sanity on the seed)
        ($src | Where-Object { $_.Key -like "*|$($script:SeededFolderUser)" }).Count |
            Should -BeGreaterThan 0 -Because 'the seeded folder user should appear on the source folder'

        $dstByKey = @{}
        foreach ($r in $dst) { $dstByKey[$r.Key] = $r }
        foreach ($e in $src) {
            $dstByKey.ContainsKey($e.Key) | Should -BeTrue `
                -Because "folder user '$($e.Key)' should be resolved + assigned on the cross-tenant copy destination"
            $dstByKey[$e.Key].Type  | Should -Be $e.Type
            $dstByKey[$e.Key].Roles | Should -Be $e.Roles
        }
    }
}

# ============================================================================
# Regression: a shared queue used to disappear from the destination for the
# rest of the session, because Copy-Item never invalidated the destination
# folder's cached queue list -- neither when it CREATED queues there (the
# folder loop was the one create path without the clear that Copy-OrchQueue
# always had) nor when it SHARED one in.
#
# The trigger is a destination folder that ALREADY EXISTS when the copy starts:
# the first folder's pass looks for its shared queue's counterpart there, finds
# nothing, and caches that emptiness. In the field this is the normal case --
# \Shared always exists, and every re-run finds the whole tree already there --
# so the pre-created dst folders below are the point of the setup, not scaffolding.
#
# Deliberately NO Clear-OrchCache between the copy and the assertion: the
# cleared cache is exactly what used to hide the bug. Both folders are checked
# so the test does not depend on which one the folder walk reaches first (the
# poisoned side flips with the order; before the fix one of the two was empty).
# ============================================================================

Describe 'Copy-Item -Recurse cross-tenant leaves no stale destination queue cache' {

    BeforeAll {
        $script:StaleSkip = $script:SkipReason
        if (-not $script:StaleSkip) {
            $stamp = $PID
            $script:StaleSrcRoot = "$($script:SrcDrive):\_xt_stale_src_$stamp"
            $script:StaleDstRoot = "$($script:DstDrive):\_xt_stale_dst_$stamp"
            $script:StaleSrcA    = "$($script:StaleSrcRoot)\aOwner"
            $script:StaleSrcB    = "$($script:StaleSrcRoot)\bBorrower"
            $dstCopyRoot         = "$($script:StaleDstRoot)\_xt_stale_src_$stamp"
            $script:StaleDstA    = "$dstCopyRoot\aOwner"
            $script:StaleDstB    = "$dstCopyRoot\bBorrower"

            Write-Host "Seeding shared-queue source '$($script:StaleSrcRoot)' ..." -ForegroundColor Cyan
            New-Item -ItemType Directory -Path $script:StaleSrcRoot -Force -ErrorAction Stop | Out-Null
            New-Item -ItemType Directory -Path $script:StaleSrcA    -Force -ErrorAction Stop | Out-Null
            New-Item -ItemType Directory -Path $script:StaleSrcB    -Force -ErrorAction Stop | Out-Null
            New-OrchQueue -Path $script:StaleSrcA -Name xtSharedQueue -Description 'xt shared queue' -ErrorAction Stop | Out-Null
            Add-OrchQueueLink -Path $script:StaleSrcA -Name xtSharedQueue -Link $script:StaleSrcB -ErrorAction Stop | Out-Null

            # Pre-create the destination tree so both folders exist before the copy.
            New-Item -ItemType Directory -Path $script:StaleDstRoot -Force -ErrorAction Stop | Out-Null
            New-Item -ItemType Directory -Path $dstCopyRoot         -Force -ErrorAction Stop | Out-Null
            New-Item -ItemType Directory -Path $script:StaleDstA    -Force -ErrorAction Stop | Out-Null
            New-Item -ItemType Directory -Path $script:StaleDstB    -Force -ErrorAction Stop | Out-Null

            Clear-OrchCache
            Write-Host "Copy-Item -Recurse (shared queue, pre-existing dst folders) ..." -ForegroundColor Cyan
            Copy-Item -Path $script:StaleSrcRoot -Destination $script:StaleDstRoot -Recurse -ErrorAction Stop
        }
    }

    AfterAll {
        foreach ($root in @($script:StaleDstRoot, $script:StaleSrcRoot)) {
            if ($root -and (Test-Path $root)) {
                Remove-Item $root -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
            }
        }
    }

    It 'both destination folders list the shared queue without Clear-OrchCache' {
        if ($script:StaleSkip) { Set-ItResult -Skipped -Because $script:StaleSkip; return }
        foreach ($dstFolder in @($script:StaleDstA, $script:StaleDstB)) {
            $names = @(Get-OrchQueue -Path $dstFolder * | Select-Object -ExpandProperty Name)
            $names | Should -Contain 'xtSharedQueue' `
                -Because "'$dstFolder' holds the queue on the server, so the copy must not leave a stale cached list hiding it"
        }
    }

    It 'the server agrees after Clear-OrchCache (the cached view was the only thing at issue)' {
        if ($script:StaleSkip) { Set-ItResult -Skipped -Because $script:StaleSkip; return }
        # Scoped to the destination drive on purpose: a bare Clear-OrchCache walks every
        # mounted drive, and under the Invoke-AllTests runner ($ErrorActionPreference='Stop')
        # one unreachable drive in the user's config would end the test on an unrelated error.
        Clear-OrchCache -Path "$($script:DstDrive):\"
        foreach ($dstFolder in @($script:StaleDstA, $script:StaleDstB)) {
            $names = @(Get-OrchQueue -Path $dstFolder * | Select-Object -ExpandProperty Name)
            $names | Should -Contain 'xtSharedQueue' `
                -Because "the copy itself should have put the queue in '$dstFolder'"
        }
    }

    # Positive control for the two tests above: they are only meaningful if the
    # copy actually took the SHARE path (LinkQueue -> ShareQueuesToFolders). One
    # queue id in both destination folders proves it did; two ids would mean the
    # queue was copied twice as independent queues, the link path never ran, and
    # the assertions above passed for the wrong reason.
    It 'the destination holds ONE shared queue, not two independent copies' {
        if ($script:StaleSkip) { Set-ItResult -Skipped -Because $script:StaleSkip; return }
        # Clears its own cache rather than inheriting the previous test's: this asks about
        # server state, so it must not report a stale-cache miss as "the ids differ".
        Clear-OrchCache -Path "$($script:DstDrive):\"
        $ids = foreach ($dstFolder in @($script:StaleDstA, $script:StaleDstB)) {
            (Get-OrchQueue -Path $dstFolder xtSharedQueue).Id
        }
        @($ids).Count | Should -Be 2 -Because 'both destination folders should report the queue'
        $ids[0] | Should -Be $ids[1] `
            -Because 'the cross-tenant copy reproduces the folder link, so both folders see the same queue'
    }
}
