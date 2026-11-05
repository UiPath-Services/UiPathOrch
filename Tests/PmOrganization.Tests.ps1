#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Live Pester coverage for Get-PmOrganization, Get-PmTenant and Get-PmTenantService (the
    Organization Management Service, {host}/{org}/organization_).

.DESCRIPTION
    Read-only: nothing is created or changed.

    Requirements:
    - $env:UIPATHORCH_TEST_DRIVE: an Automation Cloud or Automation Suite drive whose sign-in
      can read the organization (organization administrator). Defaults to 'Orch2'.
    - Optional: $env:UIPATHORCH_TEST_ONPREM_DRIVE, a standalone Orchestrator drive, for the
      "not available here" error. Skipped when unset.

.NOTES
    Run with: Invoke-Pester -Path Tests\PmOrganization.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'Orch2' }
    $script:DrivePath = "${script:Drive}:"
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null

    $orchDrive = Get-OrchPSDrive | Where-Object Name -eq $script:Drive
    $script:NoOms = if ($orchDrive.Edition -eq 'OnPremises') {
        "${script:Drive}: is a standalone server, which has no Organization Management Service"
    }
    function script:SkipWithoutOms {
        if ($script:NoOms) { Set-ItResult -Skipped -Because $script:NoOms; return $true }
        return $false
    }
    if ($script:NoOms) { return }

    # The drive's own tenant: the last segment of its root URL (https://host/org/tenant).
    $script:DriveTenant = ([Uri]$orchDrive.Root).Segments[-1].TrimEnd('/')

    $script:Tenants = @(Get-PmTenant -Path $script:DrivePath)

    function Complete-Parameter {
        param([string]$InputScript)
        ([System.Management.Automation.CommandCompletion]::CompleteInput(
            $InputScript, $InputScript.Length, $null)).CompletionMatches
    }
}

Describe 'Get-PmOrganization' {
    It 'returns the organization with its id, name and status' {
        if (script:SkipWithoutOms) { return }
        $org = @(Get-PmOrganization -Path $script:DrivePath)
        $org.Count | Should -Be 1
        $org[0].id | Should -Match '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
        $org[0].name | Should -Not -BeNullOrEmpty
        $org[0].logicalName | Should -Not -BeNullOrEmpty
        $org[0].status | Should -Not -BeNullOrEmpty
        $org[0].Path | Should -Be "${script:Drive}:\"
    }

    It 'lists the organization-level services, portal among them' {
        if (script:SkipWithoutOms) { return }
        $services = (Get-PmOrganization -Path $script:DrivePath).organizationServiceInstances
        $services.serviceType | Should -Contain 'portal'
        $services | Where-Object serviceType -eq 'orchestrator' | Should -BeNullOrEmpty -Because 'orchestrator is a tenant service'
    }

    It 'takes -Path by position' {
        if (script:SkipWithoutOms) { return }
        (Get-PmOrganization $script:DrivePath).id | Should -Be (Get-PmOrganization -Path $script:DrivePath).id
    }
}

Describe 'Get-PmTenant' {
    It 'lists the tenants, the drive''s own among them' {
        if (script:SkipWithoutOms) { return }
        $script:Tenants.Count | Should -BeGreaterThan 0
        $script:Tenants.name | Should -Contain $script:DriveTenant
        foreach ($t in $script:Tenants) {
            $t.id | Should -Match '^[0-9a-f-]{36}$'
            $t.status | Should -Not -BeNullOrEmpty
            $t.Path | Should -Be "${script:Drive}:\"
        }
    }

    It 'returns the tenants in name order' {
        if (script:SkipWithoutOms) { return }
        $names = @($script:Tenants.name)
        $names | Should -Be @($names | Sort-Object)
    }

    It 'filters by -Name, exactly and with wildcards' {
        if (script:SkipWithoutOms) { return }
        $one = @(Get-PmTenant -Path $script:DrivePath -Name $script:DriveTenant)
        $one.Count | Should -Be 1
        $one[0].name | Should -Be $script:DriveTenant
        @(Get-PmTenant -Path $script:DrivePath ($script:DriveTenant.Substring(0, 1) + '*')).name | Should -Contain $script:DriveTenant
        @(Get-PmTenant -Path $script:DrivePath -Name 'NoSuchTenant_Pester_*') | Should -BeNullOrEmpty
    }

    It 'completes -Name with the tenant names' {
        if (script:SkipWithoutOms) { return }
        $completions = Complete-Parameter "Get-PmTenant -Path $($script:DrivePath) -Name "
        $completions.CompletionText | Should -Contain $script:DriveTenant
    }
}

Describe 'Get-PmTenantService' {
    BeforeAll {
        if ($script:NoOms) { return }
        $script:Services = @(Get-PmTenantService -Path $script:DrivePath -Tenant $script:DriveTenant)
    }

    It 'lists the services of a tenant, one row each, with Path and Tenant set' {
        if (script:SkipWithoutOms) { return }
        $script:Services.Count | Should -BeGreaterThan 0
        $script:Services.serviceType | Should -Contain 'orchestrator'
        ($script:Services.serviceType | Sort-Object -Unique).Count | Should -Be $script:Services.Count
        foreach ($s in $script:Services) {
            $s.Tenant | Should -Be $script:DriveTenant
            $s.tenantId | Should -Be ($script:Tenants | Where-Object name -eq $script:DriveTenant).id
            $s.Path | Should -Be "${script:Drive}:\"
            $s.status | Should -Not -BeNullOrEmpty
        }
    }

    It 'gives the orchestrator service its URL by names' {
        if (script:SkipWithoutOms) { return }
        $orch = $script:Services | Where-Object serviceType -eq 'orchestrator'
        $orch.friendlyUrl | Should -Match "/$([regex]::Escape($script:DriveTenant))/orchestrator_/?$"
    }

    It 'filters by -ServiceType, with wildcards' {
        if (script:SkipWithoutOms) { return }
        $rows = @(Get-PmTenantService -Path $script:DrivePath $script:DriveTenant 'orch*')
        $rows.serviceType | Should -Contain 'orchestrator'
        $rows | Where-Object { $_.serviceType -notlike 'orch*' } | Should -BeNullOrEmpty
        @(Get-PmTenantService -Path $script:DrivePath $script:DriveTenant 'no-such-service-*') | Should -BeNullOrEmpty
    }

    It 'without -Tenant returns every tenant''s services' {
        if (script:SkipWithoutOms) { return }
        $all = @(Get-PmTenantService -Path $script:DrivePath -ServiceType orchestrator)
        $all.Tenant | Should -Contain $script:DriveTenant
        foreach ($name in ($all.Tenant | Sort-Object -Unique)) { $script:Tenants.name | Should -Contain $name }
    }
}

Describe 'On a standalone Orchestrator' {
    It 'says the service is not there, naming the drive once' {
        $onprem = $env:UIPATHORCH_TEST_ONPREM_DRIVE
        if (-not $onprem) { Set-ItResult -Skipped -Because 'UIPATHORCH_TEST_ONPREM_DRIVE is not set'; return }
        $err = $null
        Get-PmTenant -Path "${onprem}:" -ErrorVariable err -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        $err.Count | Should -Be 1
        $msg = $err[0].Exception.Message
        $msg | Should -Match 'Automation Cloud and Automation Suite only'
        ([regex]::Matches($msg, [regex]::Escape("${onprem}:"))).Count | Should -BeLessOrEqual 1
    }
}
