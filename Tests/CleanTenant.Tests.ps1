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
    It 'returns 13 queue enumerations with -Recurse (8 owned + 5 link visibilities)' {
        # 8 queues are owned per queues.csv. -Recurse walks each subfolder and
        # lists queues accessible there, so linked queues count once per folder
        # they appear in:
        #   queue-emails  (Production) → Dev, QA              (+2 link enums)
        #   queue-shared3 (Production) → Dev, QA, SubA        (+3 link enums)
        # Total: 8 + 2 + 3 = 13.
        (Get-OrchQueue -Path $script:Root -Recurse).Count | Should -Be 13
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
    It 'returns 18 asset enumerations with -Recurse (12 owned + 6 link visibilities)' {
        # 12 assets owned (10 standard + 2 credential per assets.csv +
        # credentials.csv). -Recurse counts linked assets once per folder they
        # appear in:
        #   asset-host    (Production) → Dev, QA              (+2 link enums)
        #   asset-debug   (Production) → SubA                 (+1 link enum)
        #   asset-shared3 (Production) → Dev, QA, SubA        (+3 link enums)
        # Total: 12 + 2 + 1 + 3 = 18.
        (Get-OrchAsset -Path $script:Root -Recurse).Count | Should -Be 18
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
    #   asset-host    (Production) → Development, QA          (3 accessible folders)
    #   asset-debug   (Production) → Production/SubA          (2 accessible folders)
    #   asset-shared3 (Production) → Development, QA, SubA    (4 accessible folders) — used by Copy-Item link reproduction test below
    # Other assets remain unlinked.

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

        $sharedFromProd = $triples | Where-Object { $_.Name -eq 'asset-shared3' -and $_.Path -eq 'Production' }
        $sharedFromProd.Link | Sort-Object | Should -Be @('Development', 'QA', 'SubA')
    }

    It 'AssetLink rows carry asset name, source folder, and link folder paths' {
        $row = Get-OrchAssetLink -Path "${script:Root}\Production" -Name 'asset-host' | Select-Object -First 1
        $row.Path | Should -Match 'Production$'
        $row.Name | Should -Be 'asset-host'
        $row.Link | Should -Match '(Development|QA)$'
        $row.Id           | Should -BeGreaterThan 0
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
    # Fixture defines:
    #   queue-emails  (Production) → Development, QA         (2 link folders)
    #   queue-shared3 (Production) → Development, QA, SubA   (3 link folders) — used by Copy-Item link reproduction test below
    # Other queues are unlinked.

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

        $sharedFromProd = $triples | Where-Object { $_.Name -eq 'queue-shared3' -and $_.Path -eq 'Production' }
        $sharedFromProd.Link | Sort-Object | Should -Be @('Development', 'QA', 'SubA')
    }

    It 'QueueLink rows carry queue name, source folder, and link folder paths' {
        $row = Get-OrchQueueLink -Path "${script:Root}\Production" -Name 'queue-emails' | Select-Object -First 1
        $row.Path | Should -Match 'Production$'
        $row.Name | Should -Be 'queue-emails'
        $row.Link | Should -Match '(Development|QA)$'
        $row.Id           | Should -BeGreaterThan 0
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
    # Fixture defines:
    #   bucket-files   (Production) → Development, SubA           (2 link folders)
    #   bucket-shared3 (Production) → Development, QA, SubA       (3 link folders) — used by Copy-Item link reproduction test below
    # bucket-dev (Development) is unlinked.

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

        $sharedFromProd = $triples | Where-Object { $_.Name -eq 'bucket-shared3' -and $_.Path -eq 'Production' }
        $sharedFromProd.Link | Sort-Object | Should -Be @('Development', 'QA', 'SubA')
    }

    It 'BucketLink rows carry bucket name, source folder, and link folder paths' {
        $row = Get-OrchBucketLink -Path "${script:Root}\Production" -Name 'bucket-files' | Select-Object -First 1
        $row.Path | Should -Match 'Production$'
        $row.Name | Should -Be 'bucket-files'
        $row.Link | Should -Match '(Development|SubA)$'
        $row.Id           | Should -BeGreaterThan 0
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
    It 'returns 8 bucket enumerations with -Recurse (3 owned + 5 link visibilities)' {
        # 3 buckets owned per buckets.csv. -Recurse counts linked buckets once
        # per folder they appear in:
        #   bucket-files   (Production) → Dev, SubA           (+2 link enums)
        #   bucket-shared3 (Production) → Dev, QA, SubA       (+3 link enums)
        # Total: 3 + 2 + 3 = 8.
        (Get-OrchBucket -Path $script:Root -Recurse).Count | Should -Be 8
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

# Verifies that Copy-Item reproduces the source link graph in dst — for every
# folder that was linked to a multi-link entity in src, the corresponding dst
# folder must end up linked to the dst's copy of that entity. The fixture
# defines asset-shared3 / queue-shared3 / bucket-shared3 as Production-owned
# entities each linked to three other folders (Development, QA, SubA), so each
# entity's reproduced link set in dst must cover all three folder names.
Describe 'Copy-Item link reproduction' {
    BeforeAll {
        $script:CopyDest = "${script:Drive}:\TestFixture_CopyDest"

        # Ensure clean state — earlier failed runs may have left the dst around.
        Remove-Item -Path $script:CopyDest -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path "${script:Drive}:\"

        New-Item -Path $script:CopyDest -ItemType Directory | Out-Null

        # Copy Production first because it owns the multi-link entities. After
        # Production is in dst, copying Development / QA causes LinkAsset /
        # LinkQueue / LinkBucket to find each owned entity in dst's Production
        # and link the new folder to it instead of duplicating. Production's
        # -Recurse pulls in SubA / SubB and establishes the SubA links during
        # that recursion (because Production has been copied moments before).
        Copy-Item -Path "${script:Drive}:\TestFixture_Base\Production"  -Destination $script:CopyDest -Recurse -Confirm:$false
        Copy-Item -Path "${script:Drive}:\TestFixture_Base\Development" -Destination $script:CopyDest -Recurse -Confirm:$false
        Copy-Item -Path "${script:Drive}:\TestFixture_Base\QA"          -Destination $script:CopyDest -Recurse -Confirm:$false
        Clear-OrchCache -Path $script:CopyDest
    }

    AfterAll {
        Remove-Item -Path $script:CopyDest -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
    }

    # Helper: project links to the leaf folder name of the Link column, sorted
    function script:Get-LinkLeaves($links) {
        $links.Link | ForEach-Object { ($_ -split '\\')[-1] } | Sort-Object
    }

    # 2-link case (asset-host: Production → Dev, QA)
    It 'reproduces asset-host links (Development, QA)' {
        $links = Get-OrchAssetLink -Path "${script:CopyDest}\Production" -Name 'asset-host'
        Get-LinkLeaves $links | Should -Be @('Development', 'QA')
    }

    # 3-link case (asset-shared3: Production → Dev, QA, SubA)
    It 'reproduces asset-shared3 links to ALL three folders (Development, QA, SubA)' {
        $links = Get-OrchAssetLink -Path "${script:CopyDest}\Production" -Name 'asset-shared3'
        Get-LinkLeaves $links | Should -Be @('Development', 'QA', 'SubA')
    }

    # 2-link case (queue-emails: Production → Dev, QA)
    It 'reproduces queue-emails links (Development, QA)' {
        $links = Get-OrchQueueLink -Path "${script:CopyDest}\Production" -Name 'queue-emails'
        Get-LinkLeaves $links | Should -Be @('Development', 'QA')
    }

    # 3-link case (queue-shared3: Production → Dev, QA, SubA)
    It 'reproduces queue-shared3 links to ALL three folders (Development, QA, SubA)' {
        $links = Get-OrchQueueLink -Path "${script:CopyDest}\Production" -Name 'queue-shared3'
        Get-LinkLeaves $links | Should -Be @('Development', 'QA', 'SubA')
    }

    # 2-link case (bucket-files: Production → Dev, SubA)
    It 'reproduces bucket-files links (Development, SubA)' {
        $links = Get-OrchBucketLink -Path "${script:CopyDest}\Production" -Name 'bucket-files'
        Get-LinkLeaves $links | Should -Be @('Development', 'SubA')
    }

    # 3-link case (bucket-shared3: Production → Dev, QA, SubA)
    It 'reproduces bucket-shared3 links to ALL three folders (Development, QA, SubA)' {
        $links = Get-OrchBucketLink -Path "${script:CopyDest}\Production" -Name 'bucket-shared3'
        Get-LinkLeaves $links | Should -Be @('Development', 'QA', 'SubA')
    }

    # Subsequent-folder skip: when Development is copied (after Production),
    # asset-host / queue-emails / etc. should be linked to dst Production's
    # copy rather than duplicated in dst Development. So dst Development
    # contains no NEW asset-host of its own — it sees Production's via link.
    It 'subsequent-folder copy did not duplicate the linked entities into Development' {
        # In src, Development owns 3 assets (asset-env, asset-port, asset-trace) +
        # sees asset-host and asset-shared3 via link (5 total enumerable). The
        # dst Development must show the same 5, with asset-host / asset-shared3
        # being the same Id as in dst Production (i.e., linked, not duplicated).
        $devProd = Get-OrchAsset -Path "${script:CopyDest}\Production"  -Name 'asset-host'
        $devDev  = Get-OrchAsset -Path "${script:CopyDest}\Development" -Name 'asset-host'
        $devProd.Id | Should -Be $devDev.Id
    }
}

# Cross-drive variant of the same link-reproduction asserts: src tree on
# $script:Drive, dst tree on a separate $script:DstDrive (set via env var
# UIPATHORCH_TEST_DST_DRIVE). This pins the 1.2.1 commit's claim that
# "Cross-drive copies between two different drives were unaffected" by the
# same-drive FindDstFolders bug — the new (srcAnchor, dstAnchor) rebase
# logic must work for both same-drive AND cross-drive cases.
#
# Run prerequisites:
#   $env:UIPATHORCH_TEST_DRIVE     = 'local'   # source (must hold imported fixture)
#   $env:UIPATHORCH_TEST_DST_DRIVE = 'local2'  # destination (clean / no fixture)
# When UIPATHORCH_TEST_DST_DRIVE is unset, equals the src drive, or names an
# unmounted drive, all tests in this Describe are Set-ItResult -Skipped.
Describe 'Cross-drive Copy-Item link reproduction' {
    BeforeAll {
        $script:DstDrive = $env:UIPATHORCH_TEST_DST_DRIVE
        $script:CrossDriveSkipReason = $null

        if ([string]::IsNullOrEmpty($script:DstDrive)) {
            $script:CrossDriveSkipReason = 'UIPATHORCH_TEST_DST_DRIVE not set'
        } elseif ($script:DstDrive -eq $script:Drive) {
            $script:CrossDriveSkipReason = "UIPATHORCH_TEST_DST_DRIVE ($($script:DstDrive)) equals UIPATHORCH_TEST_DRIVE — same-drive case is covered above"
        } elseif (-not (Get-PSDrive -Name $script:DstDrive -ErrorAction SilentlyContinue)) {
            $script:CrossDriveSkipReason = "drive '$($script:DstDrive)' not mounted"
        }

        if ($script:CrossDriveSkipReason) { return }

        $script:DstRoot = "${script:DstDrive}:\TestFixture_CrossDriveCopyDest"

        # Clean any leftover from a prior failed run.
        Remove-Item -Path $script:DstRoot -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        Clear-OrchCache -Path "${script:DstDrive}:\"

        New-Item -Path $script:DstRoot -ItemType Directory | Out-Null

        # Same Production-first ordering as the same-drive test: the multi-link
        # entities live in Production, so once Production exists in dst, copying
        # Development / QA later finds dst Production's entities and links to
        # them rather than duplicating.
        Copy-Item -Path "${script:Drive}:\TestFixture_Base\Production"  -Destination $script:DstRoot -Recurse -Confirm:$false
        Copy-Item -Path "${script:Drive}:\TestFixture_Base\Development" -Destination $script:DstRoot -Recurse -Confirm:$false
        Copy-Item -Path "${script:Drive}:\TestFixture_Base\QA"          -Destination $script:DstRoot -Recurse -Confirm:$false
        Clear-OrchCache -Path $script:DstRoot
    }

    AfterAll {
        if ($script:DstRoot) {
            Remove-Item -Path $script:DstRoot -Recurse -Force -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    BeforeEach {
        if ($script:CrossDriveSkipReason) {
            Set-ItResult -Skipped -Because $script:CrossDriveSkipReason
        }
    }

    # Helper duplicated locally so this Describe is self-contained even when
    # the same-drive Describe's script-scoped Get-LinkLeaves is out of scope.
    function script:Get-LinkLeavesXD($links) {
        $links.Link | ForEach-Object { ($_ -split '\\')[-1] } | Sort-Object
    }

    # 2-link case (asset-host: Production → Dev, QA)
    It 'reproduces asset-host links across drives (Development, QA)' {
        $links = Get-OrchAssetLink -Path "${script:DstRoot}\Production" -Name 'asset-host'
        Get-LinkLeavesXD $links | Should -Be @('Development', 'QA')
    }

    # 3-link case (asset-shared3: Production → Dev, QA, SubA)
    It 'reproduces asset-shared3 links across drives to ALL three folders (Development, QA, SubA)' {
        $links = Get-OrchAssetLink -Path "${script:DstRoot}\Production" -Name 'asset-shared3'
        Get-LinkLeavesXD $links | Should -Be @('Development', 'QA', 'SubA')
    }

    # 2-link case (queue-emails: Production → Dev, QA)
    It 'reproduces queue-emails links across drives (Development, QA)' {
        $links = Get-OrchQueueLink -Path "${script:DstRoot}\Production" -Name 'queue-emails'
        Get-LinkLeavesXD $links | Should -Be @('Development', 'QA')
    }

    # 3-link case (queue-shared3: Production → Dev, QA, SubA)
    It 'reproduces queue-shared3 links across drives to ALL three folders (Development, QA, SubA)' {
        $links = Get-OrchQueueLink -Path "${script:DstRoot}\Production" -Name 'queue-shared3'
        Get-LinkLeavesXD $links | Should -Be @('Development', 'QA', 'SubA')
    }

    # 2-link case (bucket-files: Production → Dev, SubA)
    It 'reproduces bucket-files links across drives (Development, SubA)' {
        $links = Get-OrchBucketLink -Path "${script:DstRoot}\Production" -Name 'bucket-files'
        Get-LinkLeavesXD $links | Should -Be @('Development', 'SubA')
    }

    # 3-link case (bucket-shared3: Production → Dev, QA, SubA)
    It 'reproduces bucket-shared3 links across drives to ALL three folders (Development, QA, SubA)' {
        $links = Get-OrchBucketLink -Path "${script:DstRoot}\Production" -Name 'bucket-shared3'
        Get-LinkLeavesXD $links | Should -Be @('Development', 'QA', 'SubA')
    }

    It 'cross-drive: dst Production and dst Development share a SINGLE asset-host (linked, not duplicated)' {
        # Same internal Id assertion as the same-drive case, but the dst tree
        # lives on a different drive — the link topology must still be preserved.
        $devProd = Get-OrchAsset -Path "${script:DstRoot}\Production"  -Name 'asset-host'
        $devDev  = Get-OrchAsset -Path "${script:DstRoot}\Development" -Name 'asset-host'
        $devProd.Id | Should -Be $devDev.Id
    }

    It 'cross-drive: dst entity has a DIFFERENT Id from src entity (no leakage of source Id across drives)' {
        # Sanity: cross-drive copy must produce a fresh dst entity with its own
        # Id, NOT somehow share the src entity's Id (which would be impossible
        # anyway since Ids are tenant-scoped, but pinning the assertion catches
        # any future bug where, say, src entity is accidentally returned as the
        # WriteObject result of Copy-Item across drives).
        $srcAsset = Get-OrchAsset -Path "${script:Drive}:\TestFixture_Base\Production" -Name 'asset-host'
        $dstAsset = Get-OrchAsset -Path "${script:DstRoot}\Production" -Name 'asset-host'
        $dstAsset.Id | Should -Not -Be $srcAsset.Id `
            -Because 'cross-drive copy must create a fresh dst entity, not reference the src entity'
    }
}
