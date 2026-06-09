#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    End-to-end Pester coverage for Move-Orch{Asset,Bucket,Queue} added in 1.6.2.

.DESCRIPTION
    Move relocates a single shared entity between folders within one tenant
    drive via a single atomic ShareToFolders call (toAdd=[dst], toRemove=[src]).
    These tests run against a LIVE drive ($env:UIPATHORCH_TEST_DRIVE, default
    'Orch1'): they create their own throwaway folder tree, exercise the move,
    and tear the tree down in AfterAll so the tenant returns to its prior shape.

    Covered:
    - Asset move: relocates, value preserved, source emptied, -WhatIf is a
      no-op, a destination equal to the source is a no-op.
    - Queue move: the queue's items follow it to the destination.
    - Bucket move: the bucket's Identifier (its storage pointer) is unchanged.
    - -Recurse mirrors the source tree under the destination (an entity in a
      sub-folder lands in the matching sub-folder, created on demand), not
      flattened into the destination root.

    Each test self-skips (Set-ItResult -Skipped) when no drive is connected,
    because Pester evaluates -Skip at discovery before BeforeAll runs.
#>

BeforeAll {
    $script:DriveName = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch1' }
    $script:drive = "${script:DriveName}:"

    Import-OrchConfig | Out-Null
    $script:hasDrive = $null -ne (Get-OrchPSDrive | Where-Object Name -eq $script:DriveName)

    # Unique root names so parallel/re-run invocations don't collide.
    $script:tag = "ZZ_MoveTest_$([guid]::NewGuid().ToString('N').Substring(0,8))"
    $script:srcRoot = "$($script:drive)\$($script:tag)_src"
    $script:dstRoot = "$($script:drive)\$($script:tag)_dst"
    $script:srcSub = "$($script:srcRoot)\sub"

    if ($script:hasDrive) {
        # Build the throwaway tree: src, src\sub, dst.
        New-Item -Path $script:srcRoot -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        New-Item -Path $script:srcSub -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        New-Item -Path $script:dstRoot -ItemType Directory -ErrorAction SilentlyContinue | Out-Null
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        $script:ready = $null -ne (Get-Item $script:srcRoot -ErrorAction SilentlyContinue) `
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
        # Remove the whole throwaway tree (entities go with their folders).
        Remove-Item $script:srcRoot -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item $script:dstRoot -Recurse -Force -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
    }
}

Describe 'Move-OrchAsset' {
    It 'relocates an asset, preserves its value, and empties the source' {
        if (-not (script:Require)) { return }
        $n = 'MvAsset1'
        Set-OrchAsset -Path $script:srcRoot -Name $n -ValueType Text -Value 'KEEP_ME' -Confirm:$false | Out-Null

        Move-OrchAsset -Path $script:srcRoot -Name $n -Destination $script:dstRoot -Confirm:$false
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        (Get-OrchAsset -Path $script:srcRoot -Name $n -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
        $moved = Get-OrchAsset -Path $script:dstRoot -Name $n -ErrorAction SilentlyContinue
        $moved | Should -Not -BeNullOrEmpty
        $moved.Value | Should -Be 'KEEP_ME'

        # tidy for the next test
        Get-OrchAsset -Path $script:dstRoot -Name $n -ErrorAction SilentlyContinue | Remove-OrchAsset -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
    }

    It '-WhatIf previews but does not move' {
        if (-not (script:Require)) { return }
        $n = 'MvAssetWhatIf'
        Set-OrchAsset -Path $script:srcRoot -Name $n -ValueType Text -Value 'x' -Confirm:$false | Out-Null

        Move-OrchAsset -Path $script:srcRoot -Name $n -Destination $script:dstRoot -WhatIf
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        (Get-OrchAsset -Path $script:srcRoot -Name $n -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        (Get-OrchAsset -Path $script:dstRoot -Name $n -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty

        Get-OrchAsset -Path $script:srcRoot -Name $n -ErrorAction SilentlyContinue | Remove-OrchAsset -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
    }

    It 'a destination equal to the source folder is a no-op' {
        if (-not (script:Require)) { return }
        $n = 'MvAssetSame'
        Set-OrchAsset -Path $script:srcRoot -Name $n -ValueType Text -Value 'same' -Confirm:$false | Out-Null

        { Move-OrchAsset -Path $script:srcRoot -Name $n -Destination $script:srcRoot -Confirm:$false } | Should -Not -Throw
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
        (Get-OrchAsset -Path $script:srcRoot -Name $n -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty

        Get-OrchAsset -Path $script:srcRoot -Name $n -ErrorAction SilentlyContinue | Remove-OrchAsset -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
    }

    It '-Recurse mirrors the source tree under the destination (no flattening)' {
        if (-not (script:Require)) { return }
        $top = 'MvRecTop'; $sub = 'MvRecSub'
        Set-OrchAsset -Path $script:srcRoot -Name $top -ValueType Text -Value 't' -Confirm:$false | Out-Null
        Set-OrchAsset -Path $script:srcSub -Name $sub -ValueType Text -Value 's' -Confirm:$false | Out-Null
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        Move-OrchAsset -Path $script:srcRoot -Recurse -Name 'MvRec*' -Destination $script:dstRoot -Confirm:$false
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        # Top-level asset lands in the destination root.
        (Get-OrchAsset -Path $script:dstRoot -Name $top -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        # Sub-folder asset lands in the mirrored sub-folder (auto-created), NOT the root.
        (Get-OrchAsset -Path "$($script:dstRoot)\sub" -Name $sub -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        (Get-OrchAsset -Path $script:dstRoot -Name $sub -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
        # Sources emptied.
        (Get-OrchAsset -Path $script:srcRoot -Name $top -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
        (Get-OrchAsset -Path $script:srcSub -Name $sub -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
    }
}

Describe 'Move-OrchQueue' {
    It 'moves a queue together with its items' {
        if (-not (script:Require)) { return }
        $qn = 'MvQueue1'
        New-OrchQueue -Path $script:srcRoot -Name $qn -Confirm:$false | Out-Null
        $item = @{ itemData = @{ Name = $qn; Priority = 'Normal'; SpecificContent = @{ p = 'v' }; Reference = 'MV_REF' } } | ConvertTo-Json -Depth 6
        Invoke-OrchApi -Path $script:srcRoot -ApiPath '/odata/Queues/UiPathODataSvc.AddQueueItem' -Method Post -Body ([string]$item) | Out-Null
        Start-Sleep -Seconds 2
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        $q = Get-OrchQueue -Path $script:srcRoot -Name $qn
        $before = (Invoke-OrchApi -Path $script:srcRoot -ApiPath "/odata/QueueItems?`$filter=QueueDefinitionId eq $($q.Id)&`$count=true" -Raw | ConvertFrom-Json).'@odata.count'
        $before | Should -BeGreaterOrEqual 1

        Move-OrchQueue -Path $script:srcRoot -Name $qn -Destination $script:dstRoot -Confirm:$false
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        (Get-OrchQueue -Path $script:srcRoot -Name $qn -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
        (Get-OrchQueue -Path $script:dstRoot -Name $qn -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        # The dst folder-scoped QueueItems projection is updated asynchronously after the move
        # (the server re-stamps each item's folder association), so the count can momentarily read
        # 0 right after the share. Poll until it settles instead of reading once.
        $after = 0
        foreach ($i in 1..5) {
            $after = (Invoke-OrchApi -Path $script:dstRoot -ApiPath "/odata/QueueItems?`$filter=QueueDefinitionId eq $($q.Id)&`$count=true" -Raw | ConvertFrom-Json).'@odata.count'
            if ($after -ge $before) { break }
            Start-Sleep -Seconds 2
        }
        $after | Should -Be $before

        Get-OrchQueue -Path $script:dstRoot -Name $qn -ErrorAction SilentlyContinue | Remove-OrchQueue -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
    }
}

Describe 'Move-OrchBucket' {
    It 'moves a bucket and keeps its Identifier (storage pointer) unchanged' {
        if (-not (script:Require)) { return }
        $bn = 'MvBucket1'
        New-OrchBucket -Path $script:srcRoot -Name $bn -Confirm:$false | Out-Null
        $idBefore = (Get-OrchBucket -Path $script:srcRoot -Name $bn).Identifier

        Move-OrchBucket -Path $script:srcRoot -Name $bn -Destination $script:dstRoot -Confirm:$false
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null

        (Get-OrchBucket -Path $script:srcRoot -Name $bn -ErrorAction SilentlyContinue) | Should -BeNullOrEmpty
        $moved = Get-OrchBucket -Path $script:dstRoot -Name $bn -ErrorAction SilentlyContinue
        $moved | Should -Not -BeNullOrEmpty
        $moved.Identifier | Should -Be $idBefore

        Get-OrchBucket -Path $script:dstRoot -Name $bn -ErrorAction SilentlyContinue | Remove-OrchBucket -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path $script:drive -ErrorAction SilentlyContinue | Out-Null
    }
}
