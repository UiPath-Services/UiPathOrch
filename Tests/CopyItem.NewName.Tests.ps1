#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Copy-Item of a folder to a destination that does not exist names the copy (`copy .\A B`
    creates B as the copy of A), as Copy-Item does on the file system, plus the cases that are
    refused (no parent, the drive root, into itself or a subfolder).

.DESCRIPTION
    Several sources into one new name behave as on the file system (checked against the
    file system in the last Describe): an array names the copy after the first and puts the
    rest under it; a wildcard is refused from the second match on, because the engine asks
    whether the destination is a container once, before the first copy.

    Seeds folders "zzNN_<pid>_*" on $env:UIPATHORCH_TEST_DRIVE (default Orch2) and removes
    them in AfterAll. Only folders are created; no entities.

.NOTES
    Run with: Invoke-Pester -Path Tests\CopyItem.NewName.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    $script:P = "zzNN_$($PID)_"
    function script:F([string]$rel) { "$($script:Drive):\$($script:P)$rel" }

    foreach ($rel in 'S', 'S\Sub', 'W1', 'W2', 'A1', 'A2', 'Existing') {
        New-Item (F $rel) -ItemType Folder -ErrorAction Stop 3>$null | Out-Null
    }

    # The folders under the prefix, as paths relative to the drive root, after a fresh read.
    function script:Get-Tree([string]$rel) {
        Clear-OrchCache -Path "$($script:Drive):" 3>$null
        @(Get-ChildItem "$($script:Drive):\" -Recurse 3>$null |
            Where-Object { $_.FullyQualifiedName -like "$($script:P)$rel*" } |
            ForEach-Object { $_.FullyQualifiedName.Substring($script:P.Length) } | Sort-Object)
    }

    function script:Get-WhatIfLines([scriptblock]$Action) {
        $tf = Join-Path $env:TEMP "wi_$([guid]::NewGuid().ToString('N')).txt"
        Start-Transcript -Path $tf -Force | Out-Null
        try { & $Action } finally { Stop-Transcript | Out-Null }
        $lines = @(Get-Content $tf | Where-Object { $_ -match 'What if:' })
        Remove-Item $tf -ErrorAction SilentlyContinue
        $lines
    }
}

AfterAll {
    Clear-OrchCache -Path "$($script:Drive):" 3>$null
    Get-ChildItem "$($script:Drive):\" 3>$null | Where-Object { $_.FullyQualifiedName -like "$($script:P)*" } |
        ForEach-Object { Remove-Item $_.PSPath -Recurse -Confirm:$false -ErrorAction SilentlyContinue 3>$null }
}

Describe 'Copy-Item to a folder that does not exist' {
    It 'creates the copy under the given name, subfolders included' {
        Copy-Item (F 'S') (F 'New') -Recurse -Confirm:$false 3>$null
        Get-Tree 'New' | Should -Be @('New', 'New/Sub')
    }

    It 'still copies INTO a folder that exists' {
        Copy-Item (F 'S') (F 'Existing') -Recurse -Confirm:$false 3>$null
        Get-Tree 'Existing' | Should -Be @('Existing', "Existing/$($script:P)S", "Existing/$($script:P)S/Sub")
    }

    It 'names the copy under a parent folder too' {
        Copy-Item (F 'S') (F 'Existing\Named') -Recurse -Confirm:$false 3>$null
        @(Get-Tree 'Existing/Named') | Should -Be @('Existing/Named', 'Existing/Named/Sub')
    }

    It 'with -WhatIf names the new folder and creates nothing' {
        $lines = Get-WhatIfLines { Copy-Item (F 'S') (F 'WhatIfNew') -Recurse -WhatIf 3>$null }
        ($lines -join "`n") | Should -Match ([regex]::Escape("Destination: '$(F 'WhatIfNew')'"))
        Get-Tree 'WhatIfNew' | Should -BeNullOrEmpty
    }

    It 'refuses a destination whose parent does not exist' {
        $err = $null
        Copy-Item (F 'S') (F 'NoParent\X') -Recurse -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue 3>$null
        "$err" | Should -Match 'does not have folder'
        Get-Tree 'NoParent' | Should -BeNullOrEmpty
    }

    It 'refuses the drive root into a new folder' {
        $err = $null
        Copy-Item "$($script:Drive):\" (F 'RootNew') -Recurse -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue 3>$null
        "$err" | Should -Match 'drive root cannot be copied into a new folder'
        Get-Tree 'RootNew' | Should -BeNullOrEmpty
    }

    It 'refuses to copy a folder into one of its own subfolders, new or existing' {
        foreach ($dst in (F 'S\Sub\X'), (F 'S\Sub')) {
            $err = $null
            Copy-Item (F 'S') $dst -Recurse -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue 3>$null
            "$err" | Should -Match 'into itself or one of its subfolders'
        }
        Get-Tree 'S' | Should -Be @('S', 'S/Sub')
    }

    It 'keeps " - Copy" for a copy into the source''s own parent' {
        Copy-Item (F 'W1') "$($script:Drive):\" -Recurse -Confirm:$false 3>$null
        Get-Tree 'W1' | Should -Contain 'W1 - Copy'
    }
}

Describe 'Several sources into one new name, as on the file system' {
    It 'an array: the first is copied as the new name, the rest go under it' {
        Copy-Item (F 'A1'), (F 'A2') (F 'ANew') -Recurse -Confirm:$false 3>$null
        Get-Tree 'ANew' | Should -Be @('ANew', "ANew/$($script:P)A2")
    }

    It 'a wildcard: the first is copied as the new name, the second is refused' {
        $err = $null
        Copy-Item (F 'W?') (F 'WNew') -Recurse -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue 3>$null
        Get-Tree 'WNew' | Should -Be @('WNew')
        "$err" | Should -Match 'Container cannot be copied onto existing leaf item'
    }

    It 'matches what the file system does with the same commands' {
        $t = Join-Path $env:TEMP "zzNN_fs_$PID"
        New-Item $t -ItemType Directory -Force | Out-Null
        try {
            foreach ($n in 'A1', 'A2', 'W1', 'W2') { New-Item "$t\$n" -ItemType Directory | Out-Null }
            Copy-Item "$t\A1", "$t\A2" "$t\ANew" -Recurse
            Copy-Item "$t\W?" "$t\WNew" -Recurse -ErrorAction SilentlyContinue
            @(Get-ChildItem "$t\ANew" -Recurse -Name) | Should -Be @('A2')
            @(Get-ChildItem "$t\WNew" -Recurse -Name) | Should -BeNullOrEmpty
        }
        finally { Remove-Item $t -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

Describe 'IsItemContainer' {
    It 'is true for a folder and the root, false for a path that does not exist' {
        Test-Path (F 'S') -PathType Container | Should -BeTrue
        Test-Path "$($script:Drive):\" -PathType Container | Should -BeTrue
        Test-Path (F 'NoSuchFolder') -PathType Container | Should -BeFalse
    }
}
