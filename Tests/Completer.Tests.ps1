#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Tab completion regression tests for OrchCompleter refactoring.
    Compares completion results against a baseline captured from the pre-refactoring module.

.DESCRIPTION
    Baseline is captured by running Tests\Capture-CompleterBaseline.ps1 with the old module.
    Each test verifies that the new module returns exactly the same candidates
    (ListItemText, CompletionText, ResultType, ToolTip) as the old module.

.NOTES
    Run with: Invoke-Pester -Path Tests\Completer.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive1 = 'Orch1'

    # Verify drive is available
    Get-PSDrive $script:Drive1 -ErrorAction Stop | Out-Null

    # Load baseline
    $baselinePath = Join-Path $PSScriptRoot 'CompleterBaseline.clixml'
    if (-not (Test-Path $baselinePath)) {
        throw "Baseline file not found: $baselinePath`nRun Capture-CompleterBaseline.ps1 with the old module first."
    }
    $script:Baseline = Import-Clixml -Path $baselinePath

    # Navigate into a folder on Orch1 (same location as baseline capture)
    Push-Location "${script:Drive1}:\Shared"

    function Complete-Parameter {
        param([string]$InputScript)
        $result = [System.Management.Automation.CommandCompletion]::CompleteInput(
            $InputScript, $InputScript.Length, $null)
        return $result.CompletionMatches
    }

    function Assert-MatchesBaseline {
        <#
        .SYNOPSIS
            Asserts that completion results match the baseline exactly.
        #>
        param(
            [string]$BaselineKey,
            [string]$Command
        )

        $expected = $script:Baseline[$BaselineKey]
        $actual = @(Complete-Parameter $Command)

        # Compare count
        $actual.Count | Should -Be $expected.Count -Because "candidate count for '$BaselineKey'"

        # Compare each candidate
        for ($i = 0; $i -lt $expected.Count; $i++) {
            $actual[$i].ListItemText   | Should -Be $expected[$i].ListItemText   -Because "ListItemText[$i] for '$BaselineKey'"
            $actual[$i].CompletionText | Should -Be $expected[$i].CompletionText -Because "CompletionText[$i] for '$BaselineKey'"
            $actual[$i].ResultType.ToString() | Should -Be $expected[$i].ResultType -Because "ResultType[$i] for '$BaselineKey'"
            $actual[$i].ToolTip        | Should -Be $expected[$i].ToolTip        -Because "ToolTip[$i] for '$BaselineKey'"
        }
    }
}

AfterAll {
    Pop-Location
}

# ============================================================================
# FolderScoped completers (migrated to FolderScopedCompleter<T>)
# ============================================================================

Describe 'FolderScoped completers match baseline' {
    It 'QueueNameCompleter: Get-OrchQueue -Name' {
        Assert-MatchesBaseline 'Get-OrchQueue -Name' 'Get-OrchQueue -Name '
    }

    It 'ProcessNameCompleter: Get-OrchProcess -Name' {
        Assert-MatchesBaseline 'Get-OrchProcess -Name' 'Get-OrchProcess -Name '
    }

    It 'ActionCatalogNameCompleter: Get-OrchActionCatalog -Name' {
        Assert-MatchesBaseline 'Get-OrchActionCatalog -Name' 'Get-OrchActionCatalog -Name '
    }

    It 'TriggerNameCompleter: Get-OrchTrigger -Name' {
        Assert-MatchesBaseline 'Get-OrchTrigger -Name' 'Get-OrchTrigger -Name '
    }

    It 'EventTriggerNameCompleter: Get-OrchEventTrigger -Name' {
        Assert-MatchesBaseline 'Get-OrchEventTrigger -Name' 'Get-OrchEventTrigger -Name '
    }

    It 'ApiTriggerNameCompleter: Get-OrchApiTrigger -Name' {
        Assert-MatchesBaseline 'Get-OrchApiTrigger -Name' 'Get-OrchApiTrigger -Name '
    }

    It 'ListReleasesCompleter: Start-OrchJob -Name' {
        Assert-MatchesBaseline 'Start-OrchJob -Name' 'Start-OrchJob -Name '
    }
}

# ============================================================================
# DriveScoped completers (migrated to DriveScopedCompleter<T>)
# ============================================================================

Describe 'DriveScoped completers match baseline' {
    It 'MachineNameCompleter: Get-OrchMachine -Name' {
        Assert-MatchesBaseline 'Get-OrchMachine -Name' 'Get-OrchMachine -Name '
    }

    It 'RoleNameCompleter: Get-OrchRole -Name' {
        Assert-MatchesBaseline 'Get-OrchRole -Name' 'Get-OrchRole -Name '
    }

    It 'CalendarNameCompleter: Get-OrchCalendar -Name' {
        Assert-MatchesBaseline 'Get-OrchCalendar -Name' 'Get-OrchCalendar -Name '
    }

    It 'CredentialStoreNameCompleter: Copy-OrchCredentialStore -Name' {
        Assert-MatchesBaseline 'Copy-OrchCredentialStore -Name' 'Copy-OrchCredentialStore -Name '
    }

    It 'WebhookNameCompleter: Get-OrchWebhook -Name' {
        Assert-MatchesBaseline 'Get-OrchWebhook -Name' 'Get-OrchWebhook -Name '
    }
}

# ============================================================================
# Positional parameter support (key validation for TPositional removal)
# ============================================================================

Describe 'Positional parameter completion matches baseline' {
    It 'QueueNameCompleter positional' {
        Assert-MatchesBaseline 'Get-OrchQueue (positional)' 'Get-OrchQueue '
    }

    It 'MachineNameCompleter positional' {
        Assert-MatchesBaseline 'Get-OrchMachine (positional)' 'Get-OrchMachine '
    }

    It 'RoleNameCompleter positional' {
        Assert-MatchesBaseline 'Get-OrchRole (positional)' 'Get-OrchRole '
    }
}

# ============================================================================
# Non-migrated completers (should be completely unchanged)
# ============================================================================

Describe 'Non-migrated completers match baseline' {
    It 'AssetNameCompleter: Get-OrchAsset -Name' {
        Assert-MatchesBaseline 'Get-OrchAsset -Name' 'Get-OrchAsset -Name '
    }

    It 'BucketNameCompleter: Get-OrchBucket -Name' {
        Assert-MatchesBaseline 'Get-OrchBucket -Name' 'Get-OrchBucket -Name '
    }

    It 'FolderMachineNameCompleter: Get-OrchFolderMachine -Name' {
        Assert-MatchesBaseline 'Get-OrchFolderMachine -Name' 'Get-OrchFolderMachine -Name '
    }
}
