#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Tests for the -LiteralPath / PSPath surface.

.DESCRIPTION
    The "surface", "binding" and "entity" Describe blocks are SELF-CONTAINED --
    they need only the imported UiPathOrch module (no tenant connection), so they
    run in CI. The "integration" block is tagged 'Integration' and auto-skips when
    no UiPathOrch drive is connected. It asserts INVARIANTS (-LiteralPath behaves
    identically to -Path; a provider-qualified PSPath resolves identically to the
    drive-qualified path) so it holds for any drive/folder regardless of the
    folder's asset permissions.

.NOTES
    Run with: Invoke-Pester -Path Tests\LiteralPath.Tests.ps1 -Output Detailed
    Skip the live block: Invoke-Pester -Path Tests\LiteralPath.Tests.ps1 -ExcludeTagFilter Integration
#>

Describe '-LiteralPath parameter surface (regression guard)' {
    It 'every cmdlet with -Path also exposes -LiteralPath with [Alias PSPath], no wildcards, VFPBPN' {
        $missing = @(); $noAlias = @(); $wild = @(); $noVfpbpn = @()
        foreach ($cmd in Get-Command -Module UiPathOrch -CommandType Cmdlet) {
            if (-not $cmd.Parameters.ContainsKey('Path')) { continue }
            if (-not $cmd.Parameters.ContainsKey('LiteralPath')) { $missing += $cmd.Name; continue }
            $lp = $cmd.Parameters['LiteralPath']
            if ($lp.Aliases -notcontains 'PSPath') { $noAlias += $cmd.Name }
            if ($lp.Attributes | Where-Object { $_ -is [System.Management.Automation.SupportsWildcardsAttribute] }) { $wild += $cmd.Name }
            if (-not ($lp.Attributes | Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] -and $_.ValueFromPipelineByPropertyName })) { $noVfpbpn += $cmd.Name }
        }
        $missing  | Should -BeNullOrEmpty -Because "these -Path cmdlets lack -LiteralPath: $($missing -join ', ')"
        $noAlias  | Should -BeNullOrEmpty -Because "these -LiteralPath lack the PSPath alias: $($noAlias -join ', ')"
        $wild     | Should -BeNullOrEmpty -Because "these -LiteralPath wrongly declare [SupportsWildcards]: $($wild -join ', ')"
        $noVfpbpn | Should -BeNullOrEmpty -Because "these -LiteralPath are not ValueFromPipelineByPropertyName: $($noVfpbpn -join ', ')"
    }

    It 'the count of -LiteralPath cmdlets matches the count of -Path cmdlets' {
        $withPath        = @(Get-Command -Module UiPathOrch -CommandType Cmdlet | Where-Object { $_.Parameters.ContainsKey('Path') })
        $withLiteralPath = @($withPath | Where-Object { $_.Parameters.ContainsKey('LiteralPath') })
        $withLiteralPath.Count | Should -Be $withPath.Count
    }
}

Describe 'PSPath / Path pipeline binding precedence' {
    BeforeAll {
        # Mirrors the cmdlet design: -Path (wildcards) and -LiteralPath ([Alias PSPath]) live in
        # the SAME parameter set. A dir / Get-Item item exposes only PSPath; a content entity
        # (asset/queue/...) exposes only Path -- so the two never collide on one record.
        function script:Test-LpBind {
            param(
                [Parameter(ValueFromPipelineByPropertyName)] [string[]] $Path,
                [Parameter(ValueFromPipelineByPropertyName)] [Alias('PSPath')] [string[]] $LiteralPath
            )
            process { [pscustomobject]@{ Path = $Path; LiteralPath = $LiteralPath } }
        }
    }

    It 'an item exposing only PSPath binds to -LiteralPath' {
        $r = [pscustomobject]@{ PSPath = 'Orch1:\X' } | Test-LpBind
        $r.LiteralPath | Should -Be 'Orch1:\X'
        $r.Path        | Should -BeNullOrEmpty
    }

    It 'an item exposing only Path binds to -Path' {
        $r = [pscustomobject]@{ Path = 'Orch1:\X' } | Test-LpBind
        $r.Path        | Should -Be 'Orch1:\X'
        $r.LiteralPath | Should -BeNullOrEmpty
    }
}

Describe 'Folder entity shape (FileSystem-provider parity)' {
    It 'a folder exposes no Path property (FullName + PSPath only, like FileSystemInfo)' {
        [UiPath.PowerShell.Entities.Folder].GetProperty('Path') | Should -BeNullOrEmpty
    }
    It 'a folder exposes FullName (its own drive-qualified path)' {
        [UiPath.PowerShell.Entities.Folder].GetProperty('FullName') | Should -Not -BeNullOrEmpty
    }
}

Describe 'PSPath resolution against a live drive' -Tag 'Integration' {
    BeforeAll {
        $script:OrchDrive = Get-PSDrive -PSProvider UiPathOrch -ErrorAction SilentlyContinue | Select-Object -First 1
        $script:Folder = $null
        if ($script:OrchDrive) {
            $script:Folder = Get-ChildItem "$($script:OrchDrive.Name):\" -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -notmatch 'workspace' } | Select-Object -First 1
        }
        # Reduce an operation to an outcome string ('OK' or the error message) so the same
        # folder addressed three ways can be compared regardless of its asset permissions.
        function script:Get-Outcome([scriptblock]$op) {
            try { & $op | Out-Null; 'OK' } catch { $_.Exception.Message }
        }
    }
    BeforeEach {
        # -Skip is evaluated at discovery (before BeforeAll), so gate at run time instead.
        if (-not $script:Folder) { Set-ItResult -Skipped -Because 'no connected UiPathOrch drive with a non-workspace folder' }
    }

    It 'Get-Item and dir emit the same drive-qualified PSPath' {
        $script:Folder.PSPath | Should -Match ([regex]::Escape("::$($script:OrchDrive.Name):\"))
        (Get-Item $script:Folder.FullName).PSPath | Should -Be $script:Folder.PSPath
    }

    It '-LiteralPath resolves a folder identically to -Path' {
        $byPath    = Get-Outcome { Get-OrchAsset -Path        $script:Folder.FullName -ErrorAction Stop }
        $byLiteral = Get-Outcome { Get-OrchAsset -LiteralPath $script:Folder.FullName -ErrorAction Stop }
        $byLiteral | Should -Be $byPath
    }

    It 'a provider-qualified PSPath resolves identically to the drive-qualified path (the :: strip)' {
        $byDrive  = Get-Outcome { Get-OrchAsset -LiteralPath $script:Folder.FullName -ErrorAction Stop }
        $byPsPath = Get-Outcome { Get-OrchAsset -LiteralPath $script:Folder.PSPath  -ErrorAction Stop }
        $byPsPath | Should -Be $byDrive
        # -Path accepts the provider-qualified PSPath too (same shared resolver):
        (Get-Outcome { Get-OrchAsset -Path $script:Folder.PSPath -ErrorAction Stop }) | Should -Be $byDrive
    }

    It 'dir | Select PSPath | Export-Csv | Import-Csv preserves PSPath and binds it to -LiteralPath' {
        $csv = Join-Path $env:TEMP "lp_roundtrip_$(Get-Random).csv"
        try {
            $script:Folder | Select-Object PSPath | Export-Csv $csv -NoTypeInformation
            $row = Import-Csv $csv
            $row.PSPath | Should -Be $script:Folder.PSPath
            # The PSPath column binds to -LiteralPath, so importing resolves the SAME folder as a
            # direct -LiteralPath call (both -WhatIf, no change made).
            $viaCsv     = Get-Outcome { $row | Update-OrchProcess * -Description 'lp-probe' -WhatIf -ErrorAction Stop }
            $viaLiteral = Get-Outcome { Update-OrchProcess * -LiteralPath $script:Folder.PSPath -Description 'lp-probe' -WhatIf -ErrorAction Stop }
            $viaCsv | Should -Be $viaLiteral
        }
        finally { Remove-Item $csv -ErrorAction SilentlyContinue }
    }
}
