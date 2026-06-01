#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    End-to-end Pester coverage for Copy-Orch* on-demand destination-folder
    creation added in 1.7.1.

.DESCRIPTION
    With -Recurse, Copy-Orch* mirrors the source folder tree under the
    destination, creating missing destination sub-folders on demand (plain
    modern folders, no package feed) instead of erroring "… does not exist"
    and skipping them. Two boundaries are intentional and covered here:
      - the destination ROOT must still exist (no auto-create), and
      - folders directly under the tenant root are NOT auto-created, because a
        top-level folder's package-feed setting can't be inferred.

    Runs against a LIVE drive ($env:UIPATHORCH_TEST_DRIVE, default 'Orch1'):
    each run builds its own throwaway folder tree (src, src\<tag>_sub, dst) and
    tears it down in AfterAll. The mirrored sub-folder name carries the unique
    tag so the top-level-guard test can never collide with a real top-level
    folder. Exercised through Copy-OrchAsset (the shared GetRelativeDstFolder
    helper backs every folder-scoped Copy-Orch* cmdlet).

    Each test self-skips (Set-ItResult -Skipped) when no drive is connected,
    because Pester evaluates -Skip at discovery before BeforeAll runs.
#>

BeforeAll {
    $script:DriveName = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch1' }
    $script:drive = "${script:DriveName}:"

    Import-OrchConfig | Out-Null
    $script:hasDrive = $null -ne (Get-OrchPSDrive | Where-Object Name -eq $script:DriveName)

    # Unique names so parallel/re-run invocations don't collide, and so the
    # mirrored sub-folder name is guaranteed not to exist at the tenant root.
    $script:tag = "ZZ_CopyTest_$([guid]::NewGuid().ToString('N').Substring(0,8))"
    $script:srcRoot = "$($script:drive)\$($script:tag)_src"
    $script:dstRoot = "$($script:drive)\$($script:tag)_dst"
    $script:subName = "$($script:tag)_sub"
    $script:srcSub = "$($script:srcRoot)\$($script:subName)"
    $script:dstSub = "$($script:dstRoot)\$($script:subName)"
    $script:rootSub = "$($script:drive)\$($script:subName)"   # top-level mirror target (must never be created)

    if ($script:hasDrive) {
        # Build the throwaway tree: src, src\<tag>_sub, dst (dst exists; its
        # mirrored sub-folder does NOT — that's what -Recurse must create).
        New-Item -Path $script:srcRoot -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        New-Item -Path $script:srcSub  -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        New-Item -Path $script:dstRoot -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        $script:ready = $null -ne (Get-Item $script:srcRoot -ErrorAction SilentlyContinue) `
            -and $null -ne (Get-Item $script:srcSub -ErrorAction SilentlyContinue) `
            -and $null -ne (Get-Item $script:dstRoot -ErrorAction SilentlyContinue)
    }

    function script:Require {
        if (-not $script:hasDrive) { Set-ItResult -Skipped -Because "drive '$script:DriveName' is not connected"; return $false }
        if (-not $script:ready) { Set-ItResult -Skipped -Because "could not create the test folder tree on $script:drive"; return $false }
        return $true
    }
}

AfterAll {
    if ($script:hasDrive) {
        Remove-Item $script:srcRoot -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item $script:dstRoot -Recurse -Force -ErrorAction SilentlyContinue
        # Defensive: the guard test must never create this, but tidy it anyway.
        Remove-Item $script:rootSub -Recurse -Force -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
    }
}

Describe 'Copy-OrchAsset -Recurse destination-folder creation' {
    It 'creates the missing mirrored sub-folder under an existing destination root and copies into it' {
        if (-not (script:Require)) { return }
        Set-OrchAsset -Path $script:srcSub -Name 'CpRecSub' -ValueType Text -Value 's' -Confirm:$false | Out-Null
        # The mirrored sub-folder does not exist at the destination yet.
        Remove-Item $script:dstSub -Recurse -Force -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        Copy-OrchAsset -Path $script:srcRoot -Recurse -Name 'CpRecSub' -Destination $script:dstRoot -Confirm:$false
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        # The mirrored sub-folder was created on demand, and the asset landed in
        # it (NOT flattened into the destination root).
        (Get-Item $script:dstSub -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        (Get-OrchAsset -Path $script:dstSub -Name 'CpRecSub' -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        (Get-OrchAsset -Path $script:dstRoot -Name 'CpRecSub' -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
        # Copy, not move: the source still has it.
        (Get-OrchAsset -Path $script:srcSub -Name 'CpRecSub' -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
    }

    It '-WhatIf previews the New Folder but creates nothing' {
        if (-not (script:Require)) { return }
        Set-OrchAsset -Path $script:srcSub -Name 'CpWhatIfSub' -ValueType Text -Value 'w' -Confirm:$false | Out-Null
        # Start from a destination with no mirrored sub-folder.
        Remove-Item $script:dstSub -Recurse -Force -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        Copy-OrchAsset -Path $script:srcRoot -Recurse -Name 'CpWhatIfSub' -Destination $script:dstRoot -WhatIf
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        # The folder was only previewed, never created.
        (Get-Item $script:dstSub -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
    }

    It 'does not auto-create a folder directly under the tenant root (top-level guard)' {
        if (-not (script:Require)) { return }
        Set-OrchAsset -Path $script:srcSub -Name 'CpGuardSub' -ValueType Text -Value 'g' -Confirm:$false | Out-Null
        Remove-Item $script:rootSub -Recurse -Force -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        # Destination is the tenant root, so the mirror of <tag>_sub would be a
        # top-level folder — whose feed can't be inferred, so it must be blocked.
        Copy-OrchAsset -Path $script:srcRoot -Recurse -Name 'CpGuardSub' -Destination "$($script:drive)\" -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        (Get-Item $script:rootSub -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
    }

    It 'errors when the destination root does not exist (the root is not auto-created)' {
        if (-not (script:Require)) { return }
        $missing = "$($script:drive)\$($script:tag)_nope"
        Set-OrchAsset -Path $script:srcRoot -Name 'CpNoRoot' -ValueType Text -Value 'n' -Confirm:$false | Out-Null

        { Copy-OrchAsset -Path $script:srcRoot -Name 'CpNoRoot' -Destination $missing -Confirm:$false -ErrorAction Stop } | Should -Throw
        (Get-Item $missing -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
    }
}
