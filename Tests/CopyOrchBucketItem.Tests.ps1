#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copy-OrchBucketItem copies bucket FILES directly between folders/drives (the
    bucket-file counterpart of Copy-OrchQueueItem), and Remove-OrchBucketItem now
    requires an explicit selector. This verifies the new cmdlet end to end on a
    single drive (two scratch folders) plus the mandatory-parameter contract on
    both cmdlets.

.DESCRIPTION
    Seeds a source bucket with files, then exercises: same-named copy, positional
    FullPath filter + -DestinationBucket rename, the -Recurse/-DestinationBucket
    guard, -WhatIf, the missing-destination-bucket warning, pipeline emission, and
    content fidelity. Copy-OrchBucketItem works same-drive cross-folder (src and
    dst folders only have to differ), so the whole surface is covered without a
    second tenant. Self-contained and self-cleaning.

    Requirements:
    - $env:UIPATHORCH_TEST_DRIVE (Automation Cloud drive). Defaults to 'Orch2'.
    - Creates its own two scratch folders; no tenant prerequisites.

.NOTES
    Run with: Invoke-Pester -Path Tests\CopyOrchBucketItem.Tests.ps1 -Output Detailed
    Creates/deletes buckets in two folders; self-cleaning.
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:DriveColon = "$($script:Drive):"
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    $suffix = [guid]::NewGuid().ToString('N').Substring(0, 8)
    $script:Src = "$($script:Drive):\ZZ_CBI_src_$suffix"
    $script:Dst = "$($script:Drive):\ZZ_CBI_dst_$suffix"
    New-Item -Path $script:Src -ItemType Directory -ErrorAction Stop | Out-Null
    New-Item -Path $script:Dst -ItemType Directory -ErrorAction Stop | Out-Null

    $script:Bucket = 'zzCBI'
    $script:RenameBucket = 'zzCBIrenamed'

    # Local files to seed the source bucket.
    $script:Stage = Join-Path $env:TEMP "CBI_$suffix"
    New-Item -Path $script:Stage -ItemType Directory -Force | Out-Null
    Set-Content -Path (Join-Path $script:Stage 'meta.json')  -Value '{"k":"v","n":42}'        -Encoding UTF8 -NoNewline
    Set-Content -Path (Join-Path $script:Stage 'readme.txt') -Value 'hello copy-orchbucketitem' -Encoding UTF8 -NoNewline

    # Source bucket + files.
    New-OrchBucket -Path $script:Src -Name $script:Bucket -Description 'CBI source' | Out-Null
    Import-OrchBucketItem -Path $script:Src -Name $script:Bucket -Source (Join-Path $script:Stage 'meta.json')
    Import-OrchBucketItem -Path $script:Src -Name $script:Bucket -Source (Join-Path $script:Stage 'readme.txt')

    # Destination buckets (same name + a rename target) -- they must pre-exist.
    New-OrchBucket -Path $script:Dst -Name $script:Bucket       -Description 'CBI dest'        | Out-Null
    New-OrchBucket -Path $script:Dst -Name $script:RenameBucket -Description 'CBI rename dest' | Out-Null
    Clear-OrchCache $script:DriveColon | Out-Null
}

AfterAll {
    foreach ($p in $script:Src, $script:Dst) {
        Remove-Item -Path $p -Recurse -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -Path $script:Stage -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'Copy-OrchBucketItem parameter contract' {
    It 'Name, FullPath and Destination are mandatory' {
        $p = (Get-Command Copy-OrchBucketItem).Parameters
        foreach ($n in 'Name', 'FullPath', 'Destination') {
            $a = ($p[$n].Attributes | Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] })[0]
            $a.Mandatory | Should -BeTrue -Because "$n must be mandatory on a mutating cmdlet"
        }
    }

    It 'positions are Name=0, FullPath=1, Destination=2 (Export-OrchBucketItem order)' {
        $p = (Get-Command Copy-OrchBucketItem).Parameters
        $pos = { param($n) (($p[$n].Attributes | Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] })[0]).Position }
        (& $pos 'Name')        | Should -Be 0
        (& $pos 'FullPath')    | Should -Be 1
        (& $pos 'Destination') | Should -Be 2
    }
}

Describe 'Copy-OrchBucketItem behavior' {
    It '-WhatIf copies nothing' {
        Copy-OrchBucketItem $script:Bucket * $script:Dst -Path $script:Src -WhatIf
        Clear-OrchCache $script:DriveColon | Out-Null
        # An empty bucket still lists one directory-marker entry with an empty FullPath
        # (the cmdlets filter it out); count only real files.
        @((Get-OrchBucketItem -Path $script:Dst -Name $script:Bucket).FullPath | Where-Object { $_ }).Count | Should -Be 0
    }

    It 'copies all files into the same-named destination bucket, and Get-OrchBucketItem sees them without a manual Clear-OrchCache' {
        # Prime the destination file cache with an empty read, so that if Copy failed to
        # invalidate the cache this read-back would stay stale. Then copy WITHOUT a manual
        # Clear-OrchCache and confirm Get-OrchBucketItem reflects the new files purely via
        # Copy's own BucketFiles.ClearCache. This verifies the copy result the way a user
        # would -- by querying Get-OrchBucketItem -- not via Copy's emitted output.
        @((Get-OrchBucketItem -Path $script:Dst -Name $script:Bucket).FullPath | Where-Object { $_ }).Count | Should -Be 0

        Copy-OrchBucketItem $script:Bucket * $script:Dst -Path $script:Src

        $files = @((Get-OrchBucketItem -Path $script:Dst -Name $script:Bucket).FullPath | Where-Object { $_ })
        $files | Should -Contain 'meta.json'
        $files | Should -Contain 'readme.txt'
        $files.Count | Should -Be 2
    }

    It 'preserves file content byte-for-byte' {
        $out = Join-Path $script:Stage 'verify'
        New-Item -Path $out -ItemType Directory -Force | Out-Null
        Export-OrchBucketItem -Path $script:Dst -Name $script:Bucket -FullPath 'meta.json' -Destination $out
        $dl = Get-ChildItem $out -Recurse -Filter 'meta.json' | Select-Object -First 1
        $dl | Should -Not -BeNullOrEmpty
        (Get-Content $dl.FullName -Raw) | Should -Be (Get-Content (Join-Path $script:Stage 'meta.json') -Raw)
    }

    It 'emits the copied source files to the pipeline' {
        $copied = Copy-OrchBucketItem $script:Bucket * $script:Dst -Path $script:Src
        @($copied).Count | Should -Be 2
        @($copied.FullPath) | Should -Contain 'meta.json'
    }

    It 'positional FullPath filter + -DestinationBucket copies only matching files into a renamed bucket' {
        Copy-OrchBucketItem $script:Bucket *.json $script:Dst -Path $script:Src -DestinationBucket $script:RenameBucket
        Clear-OrchCache $script:DriveColon | Out-Null
        $files = @((Get-OrchBucketItem -Path $script:Dst -Name $script:RenameBucket).FullPath)
        $files | Should -Contain 'meta.json'
        $files | Should -Not -Contain 'readme.txt'
    }

    It 'warns and skips when the destination bucket does not exist' {
        Copy-OrchBucketItem $script:Bucket * $script:Dst -Path $script:Src -DestinationBucket 'zzNoSuchBucket' `
            -WarningVariable w -WarningAction SilentlyContinue
        ($w -join "`n") | Should -Match "doesn't exist"
    }

    It '-DestinationBucket cannot be combined with -Recurse' {
        Copy-OrchBucketItem $script:Bucket * $script:Dst -Path $script:Src -DestinationBucket $script:RenameBucket -Recurse `
            -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
        $err[0].FullyQualifiedErrorId | Should -BeLike 'DestinationBucketWithRecurse*'
    }
}

Describe 'Remove-OrchBucketItem parameter contract' {
    It 'Name and FullPath are mandatory' {
        $p = (Get-Command Remove-OrchBucketItem).Parameters
        foreach ($n in 'Name', 'FullPath') {
            $a = ($p[$n].Attributes | Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] })[0]
            $a.Mandatory | Should -BeTrue -Because "$n must be mandatory on a destructive cmdlet"
        }
    }

    It 'removes files when given an explicit selector' {
        Remove-OrchBucketItem $script:Bucket * -Path $script:Dst -Confirm:$false
        Clear-OrchCache $script:DriveColon | Out-Null
        # Count only real files (an empty bucket still lists a directory-marker entry).
        @((Get-OrchBucketItem -Path $script:Dst -Name $script:Bucket).FullPath | Where-Object { $_ }).Count | Should -Be 0
    }
}

Describe 'Copy-OrchBucketItem same-folder / same-bucket' {
    It 'copies to a DIFFERENT bucket in the SAME folder via -DestinationBucket' {
        # The source bucket lives in $Src; create a sibling bucket in the same folder and copy
        # into it. This must work -- a same-folder copy to a different bucket is legitimate and
        # must not be blocked by a coarse "same root folder" guard.
        New-OrchBucket -Path $script:Src -Name 'zzCBIsibling' | Out-Null
        Clear-OrchCache $script:DriveColon | Out-Null
        Copy-OrchBucketItem $script:Bucket * $script:Src -Path $script:Src -DestinationBucket 'zzCBIsibling'
        Clear-OrchCache $script:DriveColon | Out-Null
        $files = @((Get-OrchBucketItem -Path $script:Src -Name 'zzCBIsibling').FullPath | Where-Object { $_ })
        $files | Should -Contain 'meta.json'
        $files | Should -Contain 'readme.txt'
    }

    It 'is a no-op with a warning when source and destination are the same bucket' {
        $out = Copy-OrchBucketItem $script:Bucket * $script:Src -Path $script:Src `
            -WarningVariable w -WarningAction SilentlyContinue
        @($out).Count | Should -Be 0
        ($w -join "`n") | Should -Match 'same bucket'
    }
}

Describe 'Copy-OrchBucketItem move (pipe to Remove-OrchBucketItem)' {
    It 'copies to the destination and deletes the copied files from the source' {
        # Dedicated buckets so the move does not disturb the other tests. The piped BlobFiles must
        # bind Remove-OrchBucketItem's mandatory -Name from their Bucket property (Alias("Bucket")),
        # -Path from Path, and -FullPath from FullPath -- otherwise the move silently fails to delete.
        New-OrchBucket -Path $script:Src -Name 'zzMoveSrc' | Out-Null
        New-OrchBucket -Path $script:Dst -Name 'zzMoveSrc' | Out-Null
        Import-OrchBucketItem -Path $script:Src -Name 'zzMoveSrc' -Source (Join-Path $script:Stage 'meta.json')
        Import-OrchBucketItem -Path $script:Src -Name 'zzMoveSrc' -Source (Join-Path $script:Stage 'readme.txt')
        Clear-OrchCache $script:DriveColon | Out-Null

        Copy-OrchBucketItem 'zzMoveSrc' * $script:Dst -Path $script:Src | Remove-OrchBucketItem -Confirm:$false
        Clear-OrchCache $script:DriveColon | Out-Null

        # Destination got the files...
        @((Get-OrchBucketItem -Path $script:Dst -Name 'zzMoveSrc').FullPath | Where-Object { $_ }).Count | Should -Be 2
        # ...and the source is now empty (the move deleted the copied files).
        @((Get-OrchBucketItem -Path $script:Src -Name 'zzMoveSrc').FullPath | Where-Object { $_ }).Count | Should -Be 0
    }
}

Describe 'Bucket-item pipe composition (Get-OrchBucketItem | Copy/Export via Bucket alias)' {
    BeforeAll {
        # A source bucket with two differently-typed files, plus a same-named destination bucket.
        New-OrchBucket -Path $script:Src -Name 'zzPipe' | Out-Null
        New-OrchBucket -Path $script:Dst -Name 'zzPipe' | Out-Null
        Import-OrchBucketItem -Path $script:Src -Name 'zzPipe' -Source (Join-Path $script:Stage 'meta.json')
        Import-OrchBucketItem -Path $script:Src -Name 'zzPipe' -Source (Join-Path $script:Stage 'readme.txt')
        Clear-OrchCache $script:DriveColon | Out-Null
    }

    It 'Get-OrchBucketItem | Copy-OrchBucketItem copies a Where-Object-filtered set' {
        # Filter by ContentType -- something the cmdlet's own -FullPath wildcard cannot express --
        # then copy exactly those files. The piped BlobFile binds Copy's -Name from its Bucket alias.
        Get-OrchBucketItem -Path $script:Src -Name 'zzPipe' * |
            Where-Object ContentType -eq 'application/json' |
            Copy-OrchBucketItem -Destination $script:Dst
        Clear-OrchCache $script:DriveColon | Out-Null

        $files = @((Get-OrchBucketItem -Path $script:Dst -Name 'zzPipe').FullPath | Where-Object { $_ })
        $files | Should -Contain 'meta.json'
        $files | Should -Not -Contain 'readme.txt'
    }

    It 'Get-OrchBucketItem | Export-OrchBucketItem downloads the piped files' {
        $out = Join-Path $script:Stage 'pipe_export'
        New-Item -Path $out -ItemType Directory -Force | Out-Null
        Get-OrchBucketItem -Path $script:Src -Name 'zzPipe' * | Export-OrchBucketItem -Destination $out
        $dl = @(Get-ChildItem $out -Recurse -File | ForEach-Object { $_.Name })
        $dl | Should -Contain 'meta.json'
        $dl | Should -Contain 'readme.txt'
    }
}
