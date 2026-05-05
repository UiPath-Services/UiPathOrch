#Requires -Modules UiPathOrch, Pester
<#
.SYNOPSIS
    Pester test set for UiPathOrch that runs against a freshly-imported fixture.

.DESCRIPTION
    Assumes the target tenant has been wiped via Tests\Reset-Tenant.ps1 and
    populated via Tests\Import-Fixture.ps1.

    Set the env var UIPATHORCH_TEST_DRIVE to target a specific drive. Defaults
    to 'local'.

    Run:
        $env:UIPATHORCH_TEST_DRIVE = 'local'
        .\Reset-Tenant.ps1   -TargetDrive $env:UIPATHORCH_TEST_DRIVE -Confirm:$false
        .\Import-Fixture.ps1 -TargetDrive $env:UIPATHORCH_TEST_DRIVE
        Invoke-Pester -Path .\CleanTenant.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = if ($env:UIPATHORCH_TEST_DRIVE) { $env:UIPATHORCH_TEST_DRIVE } else { 'local' }
    $script:Root  = "${script:Drive}:\TestFixture_Base"

    Get-PSDrive -Name $script:Drive -ErrorAction Stop | Out-Null
    if (-not (Test-Path $script:Root)) {
        throw "Fixture not found at $($script:Root). Run Reset-Tenant.ps1 + Import-Fixture.ps1 first."
    }

    $global:ConfirmPreference = 'None'
}

Describe 'Fixture sanity' {
    It 'mounts the target drive' {
        Get-PSDrive -Name $script:Drive | Should -Not -BeNullOrEmpty
    }

    It 'has TestFixture_Base root and 5 subfolders' {
        $folders = Get-ChildItem $script:Root -Recurse
        $folders.Count | Should -Be 5
        $names = $folders.FullyQualifiedName | Sort-Object
        $names | Should -Contain 'TestFixture_Base/Production'
        $names | Should -Contain 'TestFixture_Base/Production/SubA'
        $names | Should -Contain 'TestFixture_Base/Production/SubB'
        $names | Should -Contain 'TestFixture_Base/Development'
        $names | Should -Contain 'TestFixture_Base/QA'
    }
}

Describe 'Folders' {
    It 'returns hierarchy with -Recurse' {
        (Get-ChildItem $script:Root -Recurse).Count | Should -Be 5
    }

    It 'returns immediate children only without -Recurse' {
        (Get-ChildItem $script:Root).Count | Should -Be 3
    }

    It 'supports navigation via Set-Location' {
        Push-Location "${script:Root}\Production"
        try {
            (Get-Location).Path | Should -Be "${script:Root}\Production"
            (Get-ChildItem).Count | Should -Be 2  # SubA + SubB
        } finally { Pop-Location }
    }
}

Describe 'Processes' {
    It 'returns 6 processes with -Recurse from root' {
        (Get-OrchProcess -Path $script:Root -Recurse).Count | Should -Be 6
    }

    It 'returns processes for a specific folder' {
        $procs = Get-OrchProcess -Path "${script:Root}\Production"
        $procs.Count | Should -Be 2
        $procs.Name | Should -Contain 'proc-blank-1'
        $procs.Name | Should -Contain 'proc-blank-2'
    }

    It 'all processes reference BlankProcess19 v1.0.3' {
        $procs = Get-OrchProcess -Path $script:Root -Recurse
        $procs | ForEach-Object {
            $_.ProcessKey     | Should -Be 'BlankProcess19'
            $_.ProcessVersion | Should -Be '1.0.3'
        }
    }
}

Describe 'Queues' {
    It 'returns 7 queues with -Recurse' {
        (Get-OrchQueue -Path $script:Root -Recurse).Count | Should -Be 7
    }

    It 'queue-orders has SLA configured and references proc-blank-1' {
        $q = Get-OrchQueue -Path "${script:Root}\Production" -Name 'queue-orders'
        $q.SlaInMinutes     | Should -Be 60
        $q.RiskSlaInMinutes | Should -Be 30
        $q.ReleaseId        | Should -Not -BeNullOrEmpty
    }

    It 'queue-emails has no SLA' {
        $q = Get-OrchQueue -Path "${script:Root}\Production" -Name 'queue-emails'
        $q.SlaInMinutes | Should -Be 0
    }
}

Describe 'Assets' {
    It 'returns 11 assets total (9 standard + 2 credential) with -Recurse' {
        (Get-OrchAsset -Path $script:Root -Recurse).Count | Should -Be 11
    }

    It 'has the four ValueTypes in Production' {
        $assets = Get-OrchAsset -Path "${script:Root}\Production"
        $assets.ValueType | Sort-Object -Unique | Should -Be @('Bool', 'Credential', 'Integer', 'Text')
    }

    It 'asset-host has the expected Text value' {
        $a = Get-OrchAsset -Path "${script:Root}\Production" -Name 'asset-host'
        $a.ValueType | Should -Be 'Text'
        $a.Value     | Should -Be 'https://example.com'
    }

    It 'asset-retries has the expected Integer value' {
        $a = Get-OrchAsset -Path "${script:Root}\Production" -Name 'asset-retries'
        $a.ValueType | Should -Be 'Integer'
        $a.Value     | Should -Be '5'
    }

    It 'asset-debug has the expected Bool value' {
        $a = Get-OrchAsset -Path "${script:Root}\Production" -Name 'asset-debug'
        $a.ValueType | Should -Be 'Bool'
        $a.Value     | Should -Be 'True'
    }

    It 'asset-creds is a Credential asset' {
        $a = Get-OrchAsset -Path "${script:Root}\Production" -Name 'asset-creds'
        $a.ValueType | Should -Be 'Credential'
    }

    It 'updates an asset value via Set-OrchAsset' {
        Set-OrchAsset -Path "${script:Root}\Production" -Name 'asset-host' `
            -ValueType Text -Value 'https://updated.example.com' | Out-Null
        $a = Get-OrchAsset -Path "${script:Root}\Production" -Name 'asset-host'
        $a.Value | Should -Be 'https://updated.example.com'
        # Restore for downstream tests / re-runs
        Set-OrchAsset -Path "${script:Root}\Production" -Name 'asset-host' `
            -ValueType Text -Value 'https://example.com' | Out-Null
    }
}

Describe 'Buckets' {
    It 'has 2 buckets across the fixture' {
        (Get-OrchBucket -Path $script:Root -Recurse).Count | Should -Be 2
    }

    It 'Production/bucket-files has 3 files' {
        $items = Get-OrchBucketItem -Path "${script:Root}\Production" -Name 'bucket-files'
        $items.Count        | Should -Be 3
        $items.FullPath     | Should -Contain 'hello.txt'
        $items.FullPath     | Should -Contain 'config.json'
        $items.FullPath     | Should -Contain 'timestamp.txt'
    }

    It 'Development/bucket-dev has 2 files' {
        (Get-OrchBucketItem -Path "${script:Root}\Development" -Name 'bucket-dev').Count | Should -Be 2
    }

    It 'Export-OrchBucketItem -Recurse downloads everything (the Celine repro)' {
        $dest = Join-Path $env:TEMP "PesterBucket_$(Get-Random -Maximum 99999)"
        try {
            New-Item -ItemType Directory -Path $dest | Out-Null
            Export-OrchBucketItem -Path $script:Root -Recurse -Destination $dest
            (Get-ChildItem $dest -Recurse -File).Count | Should -Be 5
        } finally {
            Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe 'Triggers' {
    It 'has 2 triggers, both Disabled (safety)' {
        $triggers = Get-OrchTrigger -Path $script:Root -Recurse
        $triggers.Count   | Should -Be 2
        $triggers.Enabled | Should -Be @($false, $false)
    }
}

Describe 'Tenant entities' {
    It 'has the two TestFixture machines' {
        $m = Get-OrchMachine -Path "${script:Drive}:\" -Name 'TestFixture-*'
        $m.Count | Should -Be 2
        $m.Type  | Sort-Object | Should -Be @('Standard', 'Template')
    }

    It 'has the TestFixture-ReadOnly tenant role' {
        $r = Get-OrchRole -Path "${script:Drive}:\" -Name 'TestFixture-ReadOnly'
        $r        | Should -Not -BeNullOrEmpty
        $r.Type   | Should -Be 'Tenant'
    }

    It 'has the BlankProcess19 v1.0.3 package in tenant feed' {
        $p = Get-OrchPackage -Path "${script:Drive}:\" | Where-Object Id -eq 'BlankProcess19'
        $p.Version | Should -Contain '1.0.3'
    }
}

Describe 'CSV round-trip' {
    BeforeAll {
        $script:TmpCsv = Join-Path $env:TEMP "PesterCsv_$(Get-Random -Maximum 99999).csv"
    }

    AfterAll {
        Remove-Item $script:TmpCsv -ErrorAction SilentlyContinue
    }

    It 'Get-OrchAsset -ExportCsv produces a CSV with expected columns' {
        Get-OrchAsset -Path $script:Root -Recurse -ExportCsv $script:TmpCsv
        Test-Path $script:TmpCsv | Should -Be $true
        $rows = Import-Csv $script:TmpCsv
        $rows.Count | Should -BeGreaterOrEqual 9
        $cols = $rows[0].PSObject.Properties.Name
        $cols | Should -Contain 'Path'
        $cols | Should -Contain 'Name'
        $cols | Should -Contain 'ValueType'
        $cols | Should -Contain 'Value'
    }
}
