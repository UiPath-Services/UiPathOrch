#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Live Pester coverage for the tenant writes: New-, Update-, Enable-, Disable-, Remove- and
    Restore-PmTenant, and Add-, Remove-, Enable- and Disable-PmTenantService.

.DESCRIPTION
    Creates one tenant "PmPester<nnn>" in the organization of $env:UIPATHORCH_TEST_TENANT_DRIVE,
    changes it, and removes it (a soft delete). Every test skips unless that variable is set: a
    tenant write changes the organization, and a new tenant takes minutes and may count against
    the organization's tenant limit.

    Restore-PmTenant is checked where the organization allows it (Automation Suite); on
    Automation Cloud, the 403 explanation is checked instead.

.NOTES
    Run with:
      $env:UIPATHORCH_TEST_TENANT_DRIVE = 'AS'
      Invoke-Pester -Path Tests\PmTenantWrite.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = $env:UIPATHORCH_TEST_TENANT_DRIVE
    $script:Skip = if (-not $script:Drive) { 'UIPATHORCH_TEST_TENANT_DRIVE is not set (tenant writes change the organization)' }
    function script:SkipIfOff { if ($script:Skip) { Set-ItResult -Skipped -Because $script:Skip; return $true }; $false }
    if ($script:Skip) { return }

    $script:D = "$($script:Drive):"
    $script:Name = "PmPester$(Get-Random -Minimum 100 -Maximum 999)"
    $script:IsCloud = (Get-OrchPSDrive | Where-Object Name -eq $script:Drive).Edition -eq 'Cloud'
    $ProgressPreference = 'SilentlyContinue'
}

AfterAll {
    if ($script:Skip) { return }
    if (Get-PmTenant -Path $script:D -Name $script:Name -ErrorAction SilentlyContinue) {
        Remove-PmTenant -Path $script:D $script:Name -Confirm:$false -ErrorAction SilentlyContinue
    }
}

Describe 'Tenant writes' {
    It 'creates a tenant and waits until it is usable' {
        if (script:SkipIfOff) { return }
        $t = New-PmTenant -Path $script:D $script:Name -Environment Development -Confirm:$false
        $t.name | Should -Be $script:Name
        $t.status | Should -Be 'Enabled'
        (Get-PmTenant -Path $script:D).name | Should -Contain $script:Name
        $script:Id = $t.id
    }

    It 'updates the color, waiting out an operation still running on the new tenant' {
        if (script:SkipIfOff) { return }
        $t = Update-PmTenant -Path $script:D $script:Name -Color '#3366FF' -Confirm:$false
        $t.color | Should -Be '#3366FF'
    }

    It 'disables and enables, and leaves a tenant already in that state alone' {
        if (script:SkipIfOff) { return }
        (Disable-PmTenant -Path $script:D $script:Name -Reason 'Pester' -Confirm:$false).status | Should -Be 'Disabled'
        $v = Disable-PmTenant -Path $script:D $script:Name -Confirm:$false -Verbose 4>&1 | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
        "$v" | Should -Match 'already disabled'
        (Enable-PmTenant -Path $script:D $script:Name -Confirm:$false).status | Should -Be 'Enabled'
    }

    It 'adds, disables, enables and removes an optional service' {
        if (script:SkipIfOff) { return }
        $svc = 'testmanager'
        Add-PmTenantService -Path $script:D $script:Name $svc -Confirm:$false
        (Get-PmTenantService -Path $script:D $script:Name $svc).status | Should -Be 'Enabled'
        Disable-PmTenantService -Path $script:D $script:Name $svc -Confirm:$false
        (Get-PmTenantService -Path $script:D $script:Name $svc).status | Should -Be 'Disabled'
        Enable-PmTenantService -Path $script:D $script:Name $svc -Confirm:$false
        (Get-PmTenantService -Path $script:D $script:Name $svc).status | Should -Be 'Enabled'
        Remove-PmTenantService -Path $script:D $script:Name $svc -Confirm:$false
        Get-PmTenantService -Path $script:D $script:Name $svc | Should -BeNullOrEmpty
    }

    It 'refuses to disable an always-provisioned service' {
        if (script:SkipIfOff) { return }
        $err = $null
        Disable-PmTenantService -Path $script:D $script:Name orchestrator -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue
        "$err" | Should -Match 'always provisioned'
        (Get-PmTenantService -Path $script:D $script:Name orchestrator).status | Should -Be 'Enabled'
    }

    It 'refuses a tenant name that matches nothing' {
        if (script:SkipIfOff) { return }
        $err = $null
        Disable-PmTenant -Path $script:D 'NoSuchTenantPester' -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue
        "$err" | Should -Match "no tenant .* named 'NoSuchTenantPester'"
    }

    It 'removes the tenant, naming its id with -Verbose' {
        if (script:SkipIfOff) { return }
        $v = Remove-PmTenant -Path $script:D $script:Name -Confirm:$false -Verbose 4>&1 | Where-Object { $_ -is [System.Management.Automation.VerboseRecord] }
        "$v" | Should -Match ([regex]::Escape($script:Id))
        (Get-PmTenant -Path $script:D).name | Should -Not -Contain $script:Name
    }

    It 'restores it by id where the organization allows it (Automation Suite), and explains the refusal elsewhere' {
        if (script:SkipIfOff) { return }
        $err = $null
        $t = Restore-PmTenant -Path $script:D -Id $script:Id -Confirm:$false -ErrorVariable err -ErrorAction SilentlyContinue
        if ($script:IsCloud) {
            "$err" | Should -Match '403'
        }
        else {
            $err | Should -BeNullOrEmpty
            $t.status | Should -Be 'Enabled'
            Remove-PmTenant -Path $script:D $script:Name -Confirm:$false
        }
    }
}
