#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    The "secret fields are not compared" warning for the tenant-scoped Compare-Orch* cmdlets whose
    entities carry a write-only secret: User (UR_Password), Machine (ClientSecret), Webhook
    (Secret), and CredentialStore. (The folder-scoped Bucket warning is exercised by
    CompareOrchBucket.Tests.ps1, and Compare-OrchAsset's data-driven warning by
    CompareOrchAsset.Tests.ps1.)

.DESCRIPTION
    The warning is emitted in BeginProcessing, so it fires regardless of whether any entity
    matches — a deliberately non-matching name keeps these checks fast and fixture-free. Requires
    a connected Orch2: drive (run Import-OrchConfig first).

.NOTES
    Run with: Invoke-Pester -Path Tests\CompareOrchSecretWarning.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = 'Orch2'
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    # Matches nothing, so no real comparison happens; the BeginProcessing warning still fires.
    $script:NoMatch = "zzPesterNoSuch_$(Get-Random -Maximum 99999)_*"
}

Describe 'Compare-Orch* secret-not-compared warning (tenant entities)' {
    It '<Cmdlet> warns that secret values are not compared' -ForEach @(
        @{ Cmdlet = 'Compare-OrchUser' }
        @{ Cmdlet = 'Compare-OrchMachine' }
        @{ Cmdlet = 'Compare-OrchWebhook' }
        @{ Cmdlet = 'Compare-OrchCredentialStore' }
    ) {
        & $Cmdlet $script:NoMatch -Path "${script:Drive}:" -DifferencePath "${script:Drive}:" `
            -WarningVariable w -WarningAction SilentlyContinue | Out-Null
        ($w -join ' ') | Should -Match 'secret'
    }
}
