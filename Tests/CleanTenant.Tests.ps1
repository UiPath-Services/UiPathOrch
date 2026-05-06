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

Describe 'Asset Links' {
    # Fixture defines:
    #   asset-host  (Production)         → Development, QA          (3 accessible folders)
    #   asset-debug (Production)         → Production/SubA          (2 accessible folders)
    # Other assets remain unlinked, so Get-OrchAssetLink emits exactly the rows below.

    It 'Get-OrchAssetLink emits the expected fixture links' {
        $links = Get-OrchAssetLink -Path $script:Root -Recurse | Sort-Object Name, Link
        $links | Should -Not -BeNullOrEmpty

        # Project to (Name, source-leaf, link-leaf) triples for stable matching across drive prefixes
        $triples = $links | ForEach-Object {
            [PSCustomObject]@{
                Name = $_.Name
                Path = ($_.Path -split '\\')[-1]
                Link = ($_.Link -split '\\')[-1]
            }
        }

        $hostFromProd  = $triples | Where-Object { $_.Name -eq 'asset-host'  -and $_.Path -eq 'Production' }
        $hostFromProd.Link | Sort-Object | Should -Be @('Development', 'QA')

        $debugFromProd = $triples | Where-Object { $_.Name -eq 'asset-debug' -and $_.Path -eq 'Production' }
        $debugFromProd.Link | Should -Be 'SubA'
    }

    It 'AssetLink rows carry asset name, source folder, and link folder paths' {
        $row = Get-OrchAssetLink -Path "${script:Root}\Production" -Name 'asset-host' | Select-Object -First 1
        $row.Path | Should -Match 'Production$'
        $row.Name | Should -Be 'asset-host'
        $row.Link | Should -Match '(Development|QA)$'
        $row.AssetId      | Should -BeGreaterThan 0
        $row.FolderId     | Should -BeGreaterThan 0
        $row.LinkFolderId | Should -BeGreaterThan 0
    }

    It 'Get-OrchAssetLink filters by asset Name' {
        $links = Get-OrchAssetLink -Path "${script:Root}\Production" -Name 'asset-debug'
        $links | Should -Not -BeNullOrEmpty
        $links.Name | Sort-Object -Unique | Should -Be 'asset-debug'
    }

    It 'Add-OrchAssetLink + Remove-OrchAssetLink round-trip' {
        # Add link: asset-retries (Production) → SubB
        Add-OrchAssetLink -Path "${script:Root}\Production" -Name 'asset-retries' `
            -Link "${script:Root}\Production\SubB" -Confirm:$false
        Clear-OrchCache -Path $script:Root

        $afterAdd = Get-OrchAssetLink -Path "${script:Root}\Production" -Name 'asset-retries'
        $afterAdd | Should -Not -BeNullOrEmpty
        $afterAdd.Link | Should -Match 'SubB$'

        # Remove the link via pipeline (Get → Remove)
        Get-OrchAssetLink -Path "${script:Root}\Production" -Name 'asset-retries' |
            Remove-OrchAssetLink -Confirm:$false
        Clear-OrchCache -Path $script:Root

        # Should now be back to the asset existing only in Production
        Get-OrchAssetLink -Path "${script:Root}\Production" -Name 'asset-retries' |
            Should -BeNullOrEmpty
    }

    It 'Linked asset is visible from the linked-to folder via Get-OrchAsset' {
        # asset-host lives in Production but is shared into Development → enumerable from Development
        $fromDev = Get-OrchAsset -Path "${script:Root}\Development" -Name 'asset-host'
        $fromDev | Should -Not -BeNullOrEmpty
        $fromDev.Value | Should -Be 'https://example.com'
    }
}

Describe 'Queue Links' {
    # Fixture defines queue-emails (Production) → Development, QA.
    # Other queues are unlinked, so Get-OrchQueueLink emits exactly the
    # rows from queue-emails's Production-side enumeration.

    It 'Get-OrchQueueLink emits the expected fixture links' {
        $links = Get-OrchQueueLink -Path $script:Root -Recurse | Sort-Object Name, Link
        $links | Should -Not -BeNullOrEmpty

        $triples = $links | ForEach-Object {
            [PSCustomObject]@{
                Name = $_.Name
                Path = ($_.Path -split '\\')[-1]
                Link = ($_.Link -split '\\')[-1]
            }
        }

        $emailsFromProd = $triples | Where-Object { $_.Name -eq 'queue-emails' -and $_.Path -eq 'Production' }
        $emailsFromProd.Link | Sort-Object | Should -Be @('Development', 'QA')
    }

    It 'QueueLink rows carry queue name, source folder, and link folder paths' {
        $row = Get-OrchQueueLink -Path "${script:Root}\Production" -Name 'queue-emails' | Select-Object -First 1
        $row.Path | Should -Match 'Production$'
        $row.Name | Should -Be 'queue-emails'
        $row.Link | Should -Match '(Development|QA)$'
        $row.QueueId      | Should -BeGreaterThan 0
        $row.FolderId     | Should -BeGreaterThan 0
        $row.LinkFolderId | Should -BeGreaterThan 0
    }

    It 'Add-OrchQueueLink + Remove-OrchQueueLink round-trip' {
        Add-OrchQueueLink -Path "${script:Root}\Development" -Name 'queue-staging' `
            -Link "${script:Root}\QA" -Confirm:$false
        Clear-OrchCache -Path $script:Root

        $afterAdd = Get-OrchQueueLink -Path "${script:Root}\Development" -Name 'queue-staging'
        $afterAdd | Should -Not -BeNullOrEmpty
        $afterAdd.Link | Should -Match 'QA$'

        Get-OrchQueueLink -Path "${script:Root}\Development" -Name 'queue-staging' |
            Remove-OrchQueueLink -Confirm:$false
        Clear-OrchCache -Path $script:Root

        Get-OrchQueueLink -Path "${script:Root}\Development" -Name 'queue-staging' |
            Should -BeNullOrEmpty
    }

    It 'Linked queue is visible from the linked-to folder via Get-OrchQueue' {
        $fromDev = Get-OrchQueue -Path "${script:Root}\Development" -Name 'queue-emails'
        $fromDev | Should -Not -BeNullOrEmpty
    }
}

Describe 'Bucket Links' {
    # Fixture defines bucket-files (Production) → Development, Production/SubA.
    # bucket-dev (Development) is unlinked, so Get-OrchBucketLink emits exactly the
    # rows from bucket-files's Production-side enumeration.

    It 'Get-OrchBucketLink emits the expected fixture links' {
        $links = Get-OrchBucketLink -Path $script:Root -Recurse | Sort-Object Name, Link
        $links | Should -Not -BeNullOrEmpty

        $triples = $links | ForEach-Object {
            [PSCustomObject]@{
                Name = $_.Name
                Path = ($_.Path -split '\\')[-1]
                Link = ($_.Link -split '\\')[-1]
            }
        }

        $filesFromProd = $triples | Where-Object { $_.Name -eq 'bucket-files' -and $_.Path -eq 'Production' }
        $filesFromProd.Link | Sort-Object | Should -Be @('Development', 'SubA')
    }

    It 'BucketLink rows carry bucket name, source folder, and link folder paths' {
        $row = Get-OrchBucketLink -Path "${script:Root}\Production" -Name 'bucket-files' | Select-Object -First 1
        $row.Path | Should -Match 'Production$'
        $row.Name | Should -Be 'bucket-files'
        $row.Link | Should -Match '(Development|SubA)$'
        $row.BucketId     | Should -BeGreaterThan 0
        $row.FolderId     | Should -BeGreaterThan 0
        $row.LinkFolderId | Should -BeGreaterThan 0
    }

    It 'Add-OrchBucketLink + Remove-OrchBucketLink round-trip' {
        Add-OrchBucketLink -Path "${script:Root}\Development" -Name 'bucket-dev' `
            -Link "${script:Root}\QA" -Confirm:$false
        Clear-OrchCache -Path $script:Root

        $afterAdd = Get-OrchBucketLink -Path "${script:Root}\Development" -Name 'bucket-dev'
        $afterAdd | Should -Not -BeNullOrEmpty
        $afterAdd.Link | Should -Match 'QA$'

        Get-OrchBucketLink -Path "${script:Root}\Development" -Name 'bucket-dev' |
            Remove-OrchBucketLink -Confirm:$false
        Clear-OrchCache -Path $script:Root

        Get-OrchBucketLink -Path "${script:Root}\Development" -Name 'bucket-dev' |
            Should -BeNullOrEmpty
    }

    It 'Linked bucket is visible from the linked-to folder via Get-OrchBucket' {
        $fromDev = Get-OrchBucket -Path "${script:Root}\Development" -Name 'bucket-files'
        $fromDev | Should -Not -BeNullOrEmpty
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

Describe 'Classic-folder cmdlets (read-only legacy surface)' {
    # Our fixture uses modern folders, so these cmdlets should return zero rows
    # without erroring. Useful smoke test on older OCs (e.g. 20.10) where the
    # classic-folder API still exists.
    It 'Get-OrchClassicEnvironment runs without error against the fixture root' {
        { Get-OrchClassicEnvironment -Path $script:Root -Recurse -ErrorAction Stop } |
            Should -Not -Throw
    }

    It 'Get-OrchClassicRobot runs without error against the fixture root' {
        { Get-OrchClassicRobot -Path $script:Root -Recurse -ErrorAction Stop } |
            Should -Not -Throw
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
        $r | Should -Not -BeNullOrEmpty
        # Role.Type was added in a later OC; older builds (e.g. 20.10 / ApiVer 11)
        # don't expose it. Assert only when present.
        if ($r.Type) { $r.Type | Should -Be 'Tenant' }
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
