#Requires -Modules Pester
# Interactive sign-in checks: they need a person at the browser, so they live outside the
# Invoke-AllTests runner (which only picks up Tests\*.Tests.ps1). Run them directly:
#
#   Invoke-Pester .\Tests\Interactive\SignIn.Tests.ps1 -Output Detailed
#   Invoke-Pester -Container (New-PesterContainer -Path .\Tests\Interactive\SignIn.Tests.ps1 `
#       -Data @{ PkceDrive = 'Orch1'; ConfDrive = 'op2110c' }) -Output Detailed
#
# PkceDrive: a PKCE (non-confidential app) drive on a server with /api/Status/Version (21.10+).
# ConfDrive: a confidential-app drive on Orchestrator 21.10, whose Identity caps the scope at 300.
# Import-OrchConfig runs first, discarding tokens and caches -- every mounted drive signs in again
# on its next use.
param(
    [string]$PkceDrive = 'op2510',
    [string]$ConfDrive = 'op2110c'
)

BeforeAll {
    # The sign-in page's server line, as AuthManager.FormatServerLine writes it.
    function Get-ExpectedServerLine([string]$Edition, [string]$ProductVersion, $ApiVersion) {
        $line = switch ($Edition) {
            'Cloud' { 'Automation Cloud' }
            'AutomationSuite' { 'Automation Suite' }
            default { 'Standalone Orchestrator' }
        }
        if ($ProductVersion) { $line += " $ProductVersion" }
        if ($ApiVersion) { $line += ' (API v{0})' -f ([double]$ApiVersion).ToString('0.0', [cultureinfo]::InvariantCulture) }
        $line
    }

    Import-OrchConfig
}

Describe 'PKCE sign-in learns the server version' {
    BeforeAll {
        Write-Host "Sign in to ${PkceDrive}: in the browser that opens, and leave the success page open." -ForegroundColor Yellow
        Get-ChildItem "${PkceDrive}:\" -ErrorAction Stop | Out-Null
        $script:drive = Get-PSDrive $PkceDrive
        # Get-ChildItem lists folders only; nothing on that path fetches the product version. A
        # value in the cache now was put there by the sign-in's version probe.
        $script:cached = $drive.ProductVersion.CachedValue
        $script:info = Get-OrchPSDrive -Path "${PkceDrive}:\"
    }

    It 'files the product version into the org cache at sign-in' {
        $cached | Should -Not -BeNullOrEmpty
        $cached.version | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchProductVersion answers with that value' {
        (Get-OrchProductVersion -Path "${PkceDrive}:\").version | Should -Be $cached.version
    }

    It 'knows the API version' {
        $info.ApiVersion | Should -BeGreaterThan 0
    }

    It 'showed the server line on the success page (confirm by eye)' {
        $expected = Get-ExpectedServerLine $info.Edition $cached.version $info.ApiVersion
        $answer = Read-Host "Did the sign-in page show the line '$expected' under 'Connected'? (y/n)"
        $answer | Should -Be 'y'
    }
}

Describe 'Confidential app: a Scope over the limit is explained' {
    BeforeAll {
        $config = Get-Content -LiteralPath (Get-OrchConfigPath) -Raw | ConvertFrom-Json
        $source = $config.PSDrives | Where-Object Name -eq $ConfDrive
        if (-not $source.AppSecret) { throw "$ConfDrive is not a confidential-app drive." }

        # Every Orchestrator parent scope plus two more: 307 characters, past 21.10's 300 (nothing
        # here collapses further). Whether the app was granted them all does not matter -- the
        # refusal is invalid_scope either way, and the length is what is explained.
        $longScope = 'OR.Administration OR.Analytics OR.Assets OR.Audit OR.BackgroundTasks OR.Execution ' +
            'OR.Folders OR.Hypervisor OR.Jobs OR.License OR.Machines OR.ML OR.Monitoring OR.Queues OR.Robots ' +
            'OR.Settings OR.Tasks OR.TestDataQueues OR.TestSetExecutions OR.TestSets OR.TestSetSchedules ' +
            'OR.Users OR.Webhooks PM.Group PM.User'
        $script:tempDrive = 'zzScopeTooLong'
        $driveParams = @{
            Name        = $tempDrive
            PSProvider  = 'UiPathOrch'
            Root        = $source.Root
            AppId       = $source.AppId
            AppSecret   = $source.AppSecret
            OAuthScope  = $longScope
            ErrorAction = 'Stop'
        }
        if ($source.IdentityUrl) { $driveParams.IdentityUrl = $source.IdentityUrl }
        if ($source.IgnoreSslErrors) { $driveParams.IgnoreSslErrors = $true }
        New-PSDrive @driveParams | Out-Null
    }

    AfterAll {
        Remove-PSDrive $tempDrive -ErrorAction SilentlyContinue
    }

    It 'the token request is refused, and the error names the length and the limit' {
        { Get-ChildItem "${tempDrive}:\" -ErrorAction Stop } |
            Should -Throw -ExpectedMessage '*invalid_scope*too long for Orchestrator 21.10*the limit is 300*Edit-OrchConfig*'
    }

    It 'the drive with its own Scope still signs in' {
        { Get-ChildItem "${ConfDrive}:\" -ErrorAction Stop | Out-Null } | Should -Not -Throw
    }
}
