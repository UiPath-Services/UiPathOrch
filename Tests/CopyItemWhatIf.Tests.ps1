#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    -WhatIf / -Confirm message coverage for the copy operations (the 1.7.3
    improvements): recursive folder preview, tenant-entity counts, the
    "folders skipped without -Recurse" warning, the bucket destination, and
    personal-workspace guidance instead of an error.

.DESCRIPTION
    ShouldProcess -WhatIf text is written to the host, not to any pipeline /
    stream, so it can't be captured with 2>&1 or *>&1 — these tests capture it
    with a transcript (Get-WhatIfLines). Warnings and errors ARE on their
    streams and are captured with -WarningVariable / -ErrorVariable.

    The single-tenant cases (subfolder preview, bucket destination) seed their
    own fixture on $env:UIPATHORCH_TEST_DRIVE (default Orch2). The root-to-root
    cases (counts, folder-skip warning, personal-workspace handling) need a
    second, distinct tenant ($env:UIPATHORCH_TEST_DST_DRIVE, default Orch1) and
    SKIP if it isn't mounted or resolves to the same tenant.

.NOTES
    Run with: Invoke-Pester -Path Tests\CopyItemWhatIf.Tests.ps1 -Output Detailed
    Self-cleaning: removes the seeded folders in AfterAll. Everything the tests
    run is -WhatIf, so the destination tenant is never written to.
#>

# ---- discovery-time drive detection (decides -Skip on the root Describe) -------
$WiDrive    = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
$WiDstDrive = if ($env:UIPATHORCH_TEST_DST_DRIVE) { $env:UIPATHORCH_TEST_DST_DRIVE } else { 'Orch1' }
$WiTwoDrive = [bool](Get-PSDrive $WiDstDrive -ErrorAction SilentlyContinue) -and ($WiDstDrive -ne $WiDrive)

BeforeAll {
    $script:Drive    = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:DstDrive = if ($env:UIPATHORCH_TEST_DST_DRIVE) { $env:UIPATHORCH_TEST_DST_DRIVE } else { 'Orch1' }
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    $rnd = $PID
    $script:Src = "$($script:Drive):\zzWI_src_$rnd"
    $script:Sub = "$($script:Src)\Sub1"
    $script:Dst = "$($script:Drive):\zzWI_dst_$rnd"
    $script:Bkt = "zzWIBucket_$rnd"

    # Capture host-only -WhatIf lines (they never reach a pipeline / stream).
    function script:Get-WhatIfLines {
        param([scriptblock]$Action)
        $tf = Join-Path $env:TEMP "wi_$([guid]::NewGuid().ToString('N')).txt"
        Start-Transcript -Path $tf -Force | Out-Null
        try { & $Action } finally { Stop-Transcript | Out-Null }
        $lines = @(Get-Content $tf | Where-Object { $_ -match 'What if:' })
        Remove-Item $tf -ErrorAction SilentlyContinue
        return $lines
    }

    function script:Remove-WiFixture {
        foreach ($p in $script:Src, $script:Dst) {
            if ($p -and (Test-Path $p)) { Remove-Item $p -Recurse -Confirm:$false -ErrorAction SilentlyContinue }
        }
    }

    Remove-WiFixture
    New-Item -ItemType Directory -Path $script:Src -Force -ErrorAction Stop | Out-Null
    New-Item -ItemType Directory -Path $script:Sub -Force -ErrorAction Stop | Out-Null
    New-Item -ItemType Directory -Path $script:Dst -Force -ErrorAction Stop | Out-Null
    New-OrchBucket -Path $script:Src -Name $script:Bkt | Out-Null
    Clear-OrchCache "$($script:Drive):" | Out-Null
}

Describe 'Copy -WhatIf messages (single tenant)' {
    It '-Recurse -WhatIf previews the subfolder, not just the named folder' {
        $lines = Get-WhatIfLines { Copy-Item $script:Src $script:Dst -Recurse -WhatIf }
        $eSub = [regex]::Escape($script:Sub)
        ($lines | Where-Object { $_ -match "Copy Folder.+'$eSub'" }).Count |
            Should -BeGreaterThan 0 -Because 'the subfolder should get its own -WhatIf line under -Recurse'
    }

    It 'a bucket copy -WhatIf names the destination' {
        $lines = Get-WhatIfLines { Copy-OrchBucket $script:Bkt $script:Dst -Path $script:Src -WhatIf }
        ($lines | Where-Object { $_ -match 'Copy Bucket.+Destination:' }).Count |
            Should -BeGreaterThan 0 -Because 'a bucket -WhatIf line should name where it would be copied'
    }
}

Describe 'Copy root -WhatIf (cross-tenant)' -Skip:(-not $WiTwoDrive) {
    It 'tenant-entity lines include the source count' {
        $lines = Get-WhatIfLines { Copy-Item "$($script:Drive):\" "$($script:DstDrive):\" -WhatIf }
        ($lines | Where-Object { $_ -match '\* \(\d+\)' }).Count |
            Should -BeGreaterThan 0 -Because 'each tenant-entity -WhatIf line should show how many would be copied'
    }

    It 'a root copy without -Recurse warns that folders are skipped' {
        Copy-Item "$($script:Drive):\" "$($script:DstDrive):\" -WhatIf `
            -WarningVariable wv -WarningAction SilentlyContinue *> $null
        ($wv | Where-Object { $_.Message -match 'tenant-level entities only.+without -Recurse' }).Count |
            Should -BeGreaterThan 0
    }

    It 'a recursive root -WhatIf never errors on a personal workspace (warns with guidance instead)' {
        Copy-Item "$($script:Drive):\" "$($script:DstDrive):\" -Recurse -WhatIf `
            -ErrorVariable ev -WarningVariable wv -ErrorAction SilentlyContinue -WarningAction SilentlyContinue *> $null
        @($ev | Where-Object { "$_" -match 'personal workspace' }).Count |
            Should -Be 0 -Because 'a personal workspace with no destination counterpart is guidance, not an error'
        # When the tenant pair does surface orphan personal workspaces, the warning
        # must be the manual-exploration guidance (no-op when there are none).
        foreach ($w in @($wv | Where-Object { $_.Message -match 'personal workspace' })) {
            $w.Message | Should -Match 'start exploring'
        }
    }
}

AfterAll {
    if (Get-Command Remove-WiFixture -ErrorAction SilentlyContinue) { Remove-WiFixture }
}
