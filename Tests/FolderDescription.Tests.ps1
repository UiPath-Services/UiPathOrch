#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    A folder's Description through the provider property cmdlets
    (Set / Get / Clear-ItemProperty -Name Description), and the -Name / -Value argument completers
    registered for them on Orchestrator drives.

.DESCRIPTION
    Requires a connected, writable Orch2: drive (run Import-OrchConfig first). Creates one folder
    under a random "PesterDesc_XXXX" name and removes it in AfterAll.

.NOTES
    Run with: Invoke-Pester -Path Tests\FolderDescription.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    $script:Folder = "${script:Drive}:\PesterDesc_$(Get-Random -Maximum 99999)"
    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    New-Item $script:Folder -ItemType Directory | Out-Null
    foreach ($i in 1..20) { if (Test-Path $script:Folder) { break }; Start-Sleep -Milliseconds 400 }
}

AfterAll {
    Remove-Item $script:Folder -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Folder Description via *-ItemProperty' {
    It 'sets and reads back a Description' {
        Set-ItemProperty $script:Folder -Name Description -Value 'pester desc 1'
        (Get-ItemProperty $script:Folder -Name Description).Description | Should -Be 'pester desc 1'
    }

    It 'updates an existing Description' {
        Set-ItemProperty $script:Folder -Name Description -Value 'pester desc 2'
        (Get-ItemProperty $script:Folder -Name Description).Description | Should -Be 'pester desc 2'
    }

    It 'clears the Description' {
        Clear-ItemProperty $script:Folder -Name Description
        [string]::IsNullOrEmpty((Get-ItemProperty $script:Folder -Name Description).Description) | Should -BeTrue
    }

    It 'rejects setting DisplayName via Set-ItemProperty (use Rename-Item)' {
        { Set-ItemProperty $script:Folder -Name DisplayName -Value 'nope' -ErrorAction Stop } | Should -Throw
    }
}

Describe 'Folder property completers' {
    BeforeAll {
        Set-ItemProperty $script:Folder -Name Description -Value 'completer current value'
    }

    It '-Name completes Description on an Orchestrator folder' {
        $line = "Set-ItemProperty $script:Folder -Name "
        (TabExpansion2 $line $line.Length).CompletionMatches.CompletionText | Should -Contain 'Description'
    }

    It "-Value completes the folder's current Description" {
        $line = "Set-ItemProperty $script:Folder -Name Description -Value "
        $m = (TabExpansion2 $line $line.Length).CompletionMatches.CompletionText
        ($m -join ' ') | Should -Match 'completer current value'
    }

    It 'leaves -Name completion for the FileSystem provider untouched' {
        # Our completer returns nothing for non-Orchestrator paths, so PowerShell keeps its default
        # completion (it does not get suppressed).
        $line = 'Set-ItemProperty C:\Windows -Name '
        (TabExpansion2 $line $line.Length).CompletionMatches.Count | Should -BeGreaterThan 0
    }
}

Describe 'Folder Description on a personal workspace' {
    BeforeAll {
        # Orchestrator lists personal workspaces but rejects editing them through the folder PUT.
        # Skip when the test tenant has none (creating one needs a provisioned user).
        $script:Pw = @(Get-ChildItem "${script:Drive}:\" -ErrorAction SilentlyContinue |
            Where-Object FolderType -eq 'Personal' | Select-Object -First 1).FullyQualifiedName
    }

    It 'rejects Set-ItemProperty Description with a clear error' {
        if ([string]::IsNullOrEmpty($script:Pw)) { Set-ItResult -Skipped -Because 'no personal workspace on this tenant'; return }
        { Set-ItemProperty -LiteralPath "${script:Drive}:\$script:Pw" -Name Description -Value 'x' -ErrorAction Stop } |
            Should -Throw -ExpectedMessage '*personal workspace*'
    }

    It 'rejects Clear-ItemProperty Description with a clear error' {
        if ([string]::IsNullOrEmpty($script:Pw)) { Set-ItResult -Skipped -Because 'no personal workspace on this tenant'; return }
        { Clear-ItemProperty -LiteralPath "${script:Drive}:\$script:Pw" -Name Description -ErrorAction Stop } |
            Should -Throw -ExpectedMessage '*personal workspace*'
    }
}
