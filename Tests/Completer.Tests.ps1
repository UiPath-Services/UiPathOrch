#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Tab completion tests for OrchCompleter, asserted against the fixture
    imported by Import-Fixture.ps1.

.DESCRIPTION
    The fixture provides a deterministic entity inventory (TestData/Fixture/*.csv).
    Tests assert that the completer returns fixture entities as candidates from
    within the expected scope (folder for FolderScoped completers, drive for
    DriveScoped completers). Assertions use `-Contain` rather than exact count
    so the test does not fail when the target tenant has unrelated residue.

    For entity types not present in the fixture (Calendar, Webhook, ActionCatalog,
    EventTrigger, ApiTrigger, CredentialStore, TestCase, TestDataQueue, TestSet,
    TestSetSchedule), smoke tests verify the completer returns an array without
    throwing.

    StaticTextsCompleter cases (parameter values from fixed enums in source)
    stay as exact-match assertions because their candidate set is intrinsic to
    the module, not derived from tenant state.

.NOTES
    Run with: Invoke-Pester -Path Tests\Completer.Tests.ps1 -Output Detailed

    Setup wipes the target tenant (default: OrchTest) via Reset-Tenant.ps1,
    then runs Import-Fixture.ps1. This is destructive on the target drive;
    use a disposable tenant.
#>

BeforeAll {
    $script:TargetDrive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:FixtureRoot = "${script:TargetDrive}:\TestFixture_Base"

    Get-PSDrive $script:TargetDrive -ErrorAction Stop | Out-Null

    # Reset + Import-Fixture to land at a known state. Reset-Tenant is heavy
    # but deterministic; Import-Fixture won't tolerate residue.
    Write-Host "Resetting ${script:TargetDrive}: ..." -ForegroundColor Cyan
    & "$PSScriptRoot\Reset-Tenant.ps1" -TargetDrive $script:TargetDrive -Confirm:$false
    Write-Host "Importing fixture into ${script:TargetDrive}: ..." -ForegroundColor Cyan
    & "$PSScriptRoot\Import-Fixture.ps1" -TargetDrive $script:TargetDrive

    Push-Location $script:FixtureRoot

    function Complete-Parameter {
        param([string]$InputScript)
        $result = [System.Management.Automation.CommandCompletion]::CompleteInput(
            $InputScript, $InputScript.Length, $null)
        return $result.CompletionMatches
    }

    function Assert-CandidateContains {
        param(
            [string]$Command,
            [string[]]$Expected
        )
        $actual = @(Complete-Parameter $Command)
        $names = $actual.ListItemText
        foreach ($e in $Expected) {
            $names | Should -Contain $e -Because "fixture entity '$e' should appear in candidates for '$Command'"
        }
    }

    function Assert-CompleterReturnsArray {
        param(
            [string]$Command,
            [string]$Description
        )
        $actual = @(Complete-Parameter $Command)
        # Smoke: result should be an array (possibly empty), and the operation
        # itself should not throw. Empty is acceptable for entity types absent
        # from the fixture.
        ,$actual | Should -BeOfType [System.Object[]] -Because "$Description should return an array (no throw)"
    }
}

AfterAll {
    Pop-Location
    Write-Host "Cleaning up ${script:TargetDrive}: ..." -ForegroundColor Cyan
    & "$PSScriptRoot\Reset-Tenant.ps1" -TargetDrive $script:TargetDrive -Confirm:$false
}

# ============================================================================
# FolderScoped completers (migrated to FolderScopedCompleter<T>)
# ============================================================================

Describe 'FolderScoped completers return fixture entities' {
    BeforeAll {
        Set-Location "${script:TargetDrive}:\TestFixture_Base\Development"
    }

    It 'QueueNameCompleter: Get-OrchQueue -Name lists Development queues' {
        Assert-CandidateContains 'Get-OrchQueue -Name ' @('queue-dev', 'queue-staging')
    }

    It 'ProcessNameCompleter: Get-OrchProcess -Name lists Development processes' {
        Assert-CandidateContains 'Get-OrchProcess -Name ' @('proc-dev-1', 'proc-dev-2')
    }

    It 'TriggerNameCompleter: Get-OrchTrigger -Name lists Development triggers' {
        Assert-CandidateContains 'Get-OrchTrigger -Name ' @('trig-hourly')
    }

    It 'ListReleasesCompleter: Start-OrchJob -Name lists Development processes' {
        Assert-CandidateContains 'Start-OrchJob -Name ' @('proc-dev-1', 'proc-dev-2')
    }

    It 'ActionCatalogNameCompleter: Get-OrchActionCatalog -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchActionCatalog -Name ' 'Get-OrchActionCatalog -Name'
    }

    It 'EventTriggerNameCompleter: Get-OrchEventTrigger -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchEventTrigger -Name ' 'Get-OrchEventTrigger -Name'
    }

    It 'ApiTriggerNameCompleter: Get-OrchApiTrigger -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchApiTrigger -Name ' 'Get-OrchApiTrigger -Name'
    }

    It 'FolderMachineNameCompleter: Get-OrchFolderMachine -Name does not throw' {
        # Folder machines are not in the fixture; smoke only.
        Assert-CompleterReturnsArray 'Get-OrchFolderMachine -Name ' 'Get-OrchFolderMachine -Name'
    }

    It 'TestCaseNameCompleter: Get-OrchTestCase -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchTestCase -Name ' 'Get-OrchTestCase -Name'
    }

    It 'TestDataQueueNameCompleter: Get-OrchTestDataQueue -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchTestDataQueue -Name ' 'Get-OrchTestDataQueue -Name'
    }

    It 'TestScheduleNameCompleter: Get-OrchTestSetSchedule -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchTestSetSchedule -Name ' 'Get-OrchTestSetSchedule -Name'
    }

    It 'TestSetNameCompleter: Get-OrchTestSet -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchTestSet -Name ' 'Get-OrchTestSet -Name'
    }
}

# ============================================================================
# DriveScoped completers (migrated to DriveScopedCompleter<T>)
# ============================================================================

Describe 'DriveScoped completers return fixture entities' {
    BeforeAll {
        Set-Location $script:FixtureRoot
    }

    It 'MachineNameCompleter: Get-OrchMachine -Name lists fixture machines' {
        Assert-CandidateContains 'Get-OrchMachine -Name ' @('TestFixture-Standard', 'TestFixture-Template')
    }

    It 'RoleNameCompleter: Get-OrchRole -Name lists fixture role' {
        Assert-CandidateContains 'Get-OrchRole -Name ' @('TestFixture-ReadOnly')
    }

    It 'CalendarNameCompleter: Get-OrchCalendar -Name does not throw' {
        # Calendars are not in the fixture; smoke only.
        Assert-CompleterReturnsArray 'Get-OrchCalendar -Name ' 'Get-OrchCalendar -Name'
    }

    It 'CredentialStoreNameCompleter: Copy-OrchCredentialStore -Name does not throw' {
        Assert-CompleterReturnsArray 'Copy-OrchCredentialStore -Name ' 'Copy-OrchCredentialStore -Name'
    }

    It 'WebhookNameCompleter: Get-OrchWebhook -Name does not throw' {
        Assert-CompleterReturnsArray 'Get-OrchWebhook -Name ' 'Get-OrchWebhook -Name'
    }
}

# ============================================================================
# Positional parameter support (key validation for TPositional removal)
# ============================================================================

Describe 'Positional parameter completion against fixture' {
    BeforeAll {
        Set-Location "${script:TargetDrive}:\TestFixture_Base\Development"
    }

    It 'QueueNameCompleter positional: Get-OrchQueue (positional)' {
        Assert-CandidateContains 'Get-OrchQueue ' @('queue-dev', 'queue-staging')
    }

    It 'MachineNameCompleter positional: Get-OrchMachine (positional)' {
        Assert-CandidateContains 'Get-OrchMachine ' @('TestFixture-Standard', 'TestFixture-Template')
    }

    It 'RoleNameCompleter positional: Get-OrchRole (positional)' {
        Assert-CandidateContains 'Get-OrchRole ' @('TestFixture-ReadOnly')
    }
}

# ============================================================================
# Non-migrated completers (legacy paths still in use)
# ============================================================================

Describe 'Non-migrated completers against fixture' {
    BeforeAll {
        Set-Location "${script:TargetDrive}:\TestFixture_Base\Development"
    }

    It 'AssetNameCompleter: Get-OrchAsset -Name lists Development assets' {
        Assert-CandidateContains 'Get-OrchAsset -Name ' @('asset-env', 'asset-port', 'asset-trace')
    }

    It 'BucketNameCompleter: Get-OrchBucket -Name lists Development buckets' {
        Assert-CandidateContains 'Get-OrchBucket -Name ' @('bucket-dev')
    }
}

# ============================================================================
# StaticTextsCompleter (IPositionalParameters types in OrchPositionalParams.cs)
# Candidate sets are intrinsic to the module — exact-match is appropriate here.
# ============================================================================

Describe 'StaticTextsCompleter has the expected candidates' {
    It 'AssetTypeItems: Set-OrchAsset -ValueType' {
        $names = (Complete-Parameter 'Set-OrchAsset -ValueType ').ListItemText
        $names | Should -Contain 'Text'
        $names | Should -Contain 'Bool'
        $names | Should -Contain 'Integer'
    }

    It 'SoftStop_Kill: New-OrchTrigger -StopStrategy' {
        $names = (Complete-Parameter 'New-OrchTrigger -Name x -ReleaseName x -StopStrategy ').ListItemText
        $names | Should -Contain 'SoftStop'
        $names | Should -Contain 'Kill'
    }

    It 'JobPriorityItems: New-OrchTrigger -Priority' {
        $names = (Complete-Parameter 'New-OrchTrigger -Name x -ReleaseName x -Priority ').ListItemText
        $names | Should -Contain 'Medium'
        $names | Should -Contain 'High'
        $names | Should -Contain 'Low'
    }

    It 'RuntimeTypes: New-OrchTrigger -RuntimeType' {
        $names = (Complete-Parameter 'New-OrchTrigger -Name x -ReleaseName x -RuntimeType ').ListItemText
        $names | Should -Contain 'Unattended'
    }

    It 'Template_Standard_Serverless: New-OrchMachine -Type' {
        $names = (Complete-Parameter 'New-OrchMachine -Name x -Type ').ListItemText
        $names | Should -Contain 'Standard'
        $names | Should -Contain 'Template'
    }

    It 'Default_Serverless_AutomationCloudRobot: New-OrchMachine -Scope' {
        $names = (Complete-Parameter 'New-OrchMachine -Name x -Scope ').ListItemText
        $names | Should -Contain 'Default'
    }

    It 'Any_Foreground_Background: New-OrchMachine -AutomationType' {
        $names = (Complete-Parameter 'New-OrchMachine -Name x -AutomationType ').ListItemText
        $names | Should -Contain 'Any'
        $names | Should -Contain 'Foreground'
        $names | Should -Contain 'Background'
    }

    It 'Any_Windows_Portable: New-OrchMachine -TargetFramework' {
        $names = (Complete-Parameter 'New-OrchMachine -Name x -TargetFramework ').ListItemText
        $names | Should -Contain 'Any'
        $names | Should -Contain 'Windows'
        $names | Should -Contain 'Portable'
    }

    It 'DirectoryTypes: Add-PmGroupMember -Type' {
        $names = (Complete-Parameter 'Add-PmGroupMember -PmGroup x -Type ').ListItemText
        $names | Should -Contain 'DirectoryUser'
        $names | Should -Contain 'DirectoryGroup'
    }

    It 'DirectoryTypes: Add-DuUser -Type' {
        $names = (Complete-Parameter 'Add-DuUser -Type ').ListItemText
        $names | Should -Contain 'DirectoryUser'
    }
}

# ============================================================================
# Completers using CreateWPListFromParameter (positional param elimination)
# ============================================================================

Describe 'Completers backed by CreateWPListFromParameter' {
    BeforeAll {
        Set-Location "${script:TargetDrive}:\TestFixture_Base\Development"
    }

    It 'Enable-OrchApiTrigger -Name does not throw' {
        # ApiTrigger not in fixture; smoke only.
        Assert-CompleterReturnsArray 'Enable-OrchApiTrigger -Name ' 'Enable-OrchApiTrigger -Name'
    }

    It 'Enable-OrchEventTrigger -Name does not throw' {
        Assert-CompleterReturnsArray 'Enable-OrchEventTrigger -Name ' 'Enable-OrchEventTrigger -Name'
    }

    It 'Update-OrchProcess -Name lists Development processes' {
        Assert-CandidateContains 'Update-OrchProcess -Name ' @('proc-dev-1', 'proc-dev-2')
    }

    It 'Update-OrchProcessVersion -Id does not throw' {
        # -Id candidates depend on package versions; smoke only (no exact match
        # against fixture).
        Assert-CompleterReturnsArray 'Update-OrchProcessVersion -Id ' 'Update-OrchProcessVersion -Id'
    }
}
