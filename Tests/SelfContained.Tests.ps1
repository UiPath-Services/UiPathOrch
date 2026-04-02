#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Self-contained integration tests for UiPathOrch module.
    Creates all test entities from scratch; no dependency on existing data.

.DESCRIPTION
    Requires:
    - Connected OrchTest: drive (run Import-OrchConfig first)
    - Connected Orch1: drive (used to discover a package for Process/Trigger tests)

    These tests create a temporary folder hierarchy and all entities within it.
    Everything is cleaned up in AfterAll blocks.
    Uses a "PesterTest_XXXX_" prefix for all entities.

.NOTES
    Run with: Invoke-Pester -Path Tests\SelfContained.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive = 'OrchTest'
    $script:RefDrive = 'Orch1'
    $script:Prefix = "PesterTest_$(Get-Random -Maximum 9999)_"
    $script:RootFolder = "${script:Drive}:\${script:Prefix}Root"
    $script:SubFolder = "${script:RootFolder}\${script:Prefix}Sub"
    $script:CopyFolder = "${script:RootFolder}\${script:Prefix}Copy"
    $script:TempDir = Join-Path $env:TEMP "PesterTest_$(Get-Random -Maximum 9999)"

    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    # Verify drives are available
    Get-PSDrive $script:Drive -ErrorAction Stop | Out-Null
    Get-PSDrive $script:RefDrive -ErrorAction Stop | Out-Null

    # Create temp directory for CSV files
    New-Item -Path $script:TempDir -ItemType Directory -Force | Out-Null

    # Create test folder hierarchy
    $null = mkdir $script:RootFolder
    $null = mkdir $script:SubFolder
    $null = mkdir $script:CopyFolder

    # Discover a package from Orch1: for Process/Trigger tests
    $script:PackageId = (Get-OrchPackage -Path "${script:RefDrive}:\" |
        Select-Object -First 1).Id
}

AfterAll {
    # Clean up in reverse dependency order:
    # Triggers → Processes → FolderMachines → Machines → Queues → Assets → Buckets → Folders

    Push-Location $script:RootFolder -ErrorAction SilentlyContinue
    Remove-OrchTrigger -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchProcess -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchFolderMachine -Name "${script:Prefix}*" -ErrorAction SilentlyContinue
    Pop-Location

    Remove-OrchMachine -Name "${script:Prefix}*" -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchQueue -Name "${script:Prefix}*" -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchAsset -Name "${script:Prefix}*" -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchAsset -Name "${script:Prefix}*" -ValueType Credential -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue
    Remove-OrchBucket -Name "${script:Prefix}*" -Path $script:RootFolder -Recurse -Confirm:$false -ErrorAction SilentlyContinue

    Remove-Item $script:CopyFolder -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:SubFolder -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $script:RootFolder -Recurse -Force -ErrorAction SilentlyContinue

    Remove-Item $script:TempDir -Recurse -Force -ErrorAction SilentlyContinue
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

# ---------------------------------------------------------------------------
# Folder Navigation
# ---------------------------------------------------------------------------
Describe 'Folder' {
    It 'Test folders were created' {
        Test-Path $script:RootFolder | Should -BeTrue
        Test-Path $script:SubFolder | Should -BeTrue
        Test-Path $script:CopyFolder | Should -BeTrue
    }

    It 'Get-ChildItem lists subfolders' {
        $children = Get-ChildItem $script:RootFolder
        $children.DisplayName | Should -Contain ($script:Prefix + 'Sub')
        $children.DisplayName | Should -Contain ($script:Prefix + 'Copy')
    }

    It 'Set-Location navigates into the test folder' {
        Push-Location $script:SubFolder
        (Get-Location).Path | Should -BeLike "*${script:Prefix}Sub*"
        Pop-Location
    }

    It 'mkdir creates a nested folder and rmdir removes it' {
        $nested = "${script:SubFolder}\${script:Prefix}Nested"
        $null = mkdir $nested
        Test-Path $nested | Should -BeTrue
        Remove-Item $nested -Recurse -Force
        Test-Path $nested | Should -BeFalse
    }
}

# ---------------------------------------------------------------------------
# Machine CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Machine' {
    BeforeAll {
        $script:MachineName = "${script:Prefix}Machine"
        $script:MachinePath = "${script:Drive}:\"
    }

    AfterAll {
        Remove-OrchMachine -Name "${script:Prefix}Machine*" -Path $script:MachinePath -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'New-OrchMachine creates a machine' {
        New-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Description 'Created by Pester'
        Clear-OrchCache
        $m = Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath
        $m | Should -Not -BeNullOrEmpty
        $m.Name | Should -Be $script:MachineName
        $m.Description | Should -Be 'Created by Pester'
    }

    It 'Update-OrchMachine updates Description' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Description 'Updated'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).Description | Should -Be 'Updated'
    }

    It 'Update-OrchMachine updates UnattendedSlots' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -UnattendedSlots 2
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).UnattendedSlots | Should -Be 2
    }

    It 'Update-OrchMachine updates AutomationType' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -AutomationType 'Foreground'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).AutomationType | Should -Be 'Foreground'
    }

    It 'Update-OrchMachine updates TargetFramework' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -TargetFramework 'Windows'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).TargetFramework | Should -Be 'Windows'
    }

    It 'Update-OrchMachine updates Tags' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Tags 'tagA', 'tagB'
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).Tags.Count | Should -Be 2
    }

    It 'Update-OrchMachine clears Tags with empty string' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Tags ''
        Clear-OrchCache
        (Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath).Tags.Count | Should -Be 0
    }

    It 'Update-OrchMachine with multiple properties' {
        Update-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Description 'multi' -AutomationType 'Any' -TargetFramework 'Any'
        Clear-OrchCache
        $m = Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath
        $m.Description | Should -Be 'multi'
        $m.AutomationType | Should -Be 'Any'
        $m.TargetFramework | Should -Be 'Any'
    }

    It 'Get-OrchMachine -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'machines.csv'
        Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:MachineName
    }

    It 'Remove-OrchMachine removes the machine' {
        Remove-OrchMachine -Name $script:MachineName -Path $script:MachinePath -Confirm:$false
        Clear-OrchCache
        Get-OrchMachine -Name $script:MachineName -Path $script:MachinePath -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Queue CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Queue' {
    BeforeAll {
        $script:QueueName = "${script:Prefix}Queue"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchQueue -Name "${script:Prefix}Queue*" -Path $script:RootFolder -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchQueue creates a queue' {
        New-OrchQueue -Name $script:QueueName -Description 'Created by Pester'
        Clear-OrchCache
        $q = Get-OrchQueue -Name $script:QueueName
        $q | Should -Not -BeNullOrEmpty
        $q.Name | Should -Be $script:QueueName
    }

    It 'Update-OrchQueue updates Description' {
        Update-OrchQueue -Name $script:QueueName -Description 'Updated'
        Clear-OrchCache
        (Get-OrchQueue -Name $script:QueueName).Description | Should -Be 'Updated'
    }

    It 'Update-OrchQueue updates MaxNumberOfRetries' {
        Update-OrchQueue -Name $script:QueueName -MaxNumberOfRetries 5
        Clear-OrchCache
        (Get-OrchQueue -Name $script:QueueName).MaxNumberOfRetries | Should -Be 5
    }

    It 'Update-OrchQueue updates AcceptAutomaticallyRetry' {
        Update-OrchQueue -Name $script:QueueName -AcceptAutomaticallyRetry true
        Clear-OrchCache
        (Get-OrchQueue -Name $script:QueueName).AcceptAutomaticallyRetry | Should -Be $true
    }

    It 'Get-OrchQueue -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'queues.csv'
        Get-OrchQueue -Name $script:QueueName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:QueueName
    }

    It 'Remove-OrchQueue removes the queue' {
        Remove-OrchQueue -Name $script:QueueName -Confirm:$false
        Clear-OrchCache
        Get-OrchQueue -Name $script:QueueName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# QueueItem Import
# ---------------------------------------------------------------------------
Describe 'QueueItem Import' {
    BeforeAll {
        $script:QIQueueName = "${script:Prefix}QIQueue"
        Push-Location $script:RootFolder
        New-OrchQueue -Name $script:QIQueueName
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchQueue -Name $script:QIQueueName -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Import-OrchQueueItem imports items from CSV' {
        $csv = Join-Path $script:TempDir 'queueitems.csv'
        "Priority,Reference,CustomerName,Amount`nNormal,REF-001,Alice,100`nHigh,REF-002,Bob,200`nLow,REF-003,Charlie,300" |
            Set-Content -Path $csv -Encoding UTF8 -NoNewline

        $result = Import-OrchQueueItem -Name $script:QIQueueName -ImportCsv $csv
        $result.Success | Should -Be $true
    }

    It 'Imported items can be retrieved' -Tag 'KnownIssue' {
        # Known issue: Get-OrchQueueItem returns "Invalid OData query options" in some tenants.
        # The import itself succeeds (verified by the previous test).
        Clear-OrchCache
        $items = Get-OrchQueueItem -Name $script:QIQueueName -First 10
        $items.Count | Should -Be 3
        $refs = $items.Reference | Sort-Object
        $refs | Should -Contain 'REF-001'
        $refs | Should -Contain 'REF-002'
        $refs | Should -Contain 'REF-003'
    }
}

# ---------------------------------------------------------------------------
# Asset CRUD + CSV Round-Trip
# ---------------------------------------------------------------------------
Describe 'Asset' {
    BeforeAll {
        $script:AssetName = "${script:Prefix}Asset"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}Asset*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Set-OrchAsset creates a Text asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Value 'hello' -Description 'Text asset'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_Text"
        $a | Should -Not -BeNullOrEmpty
        $a.ValueType | Should -Be 'Text'
        $a.Value | Should -Be 'hello'
        $a.Description | Should -Be 'Text asset'
    }

    It 'Set-OrchAsset creates an Integer asset' {
        Set-OrchAsset -ValueType Integer -Name "${script:AssetName}_Int" -Value '42'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_Int"
        $a.Value | Should -Be '42'
    }

    It 'Set-OrchAsset creates a Bool asset' {
        Set-OrchAsset -ValueType Bool -Name "${script:AssetName}_Bool" -Value 'true'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_Bool"
        $a.Value | Should -Be 'True'
    }

    It 'Set-OrchAsset updates the value of an existing asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Value 'updated'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:AssetName}_Text").Value | Should -Be 'updated'
    }

    It 'Set-OrchAsset updates Description' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Description 'New desc'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:AssetName}_Text").Description | Should -Be 'New desc'
    }

    It 'Set-OrchAsset does not overwrite existing ValueType' {
        Set-OrchAsset -ValueType Integer -Name "${script:AssetName}_TypeKeep" -Value '10'
        Clear-OrchCache
        Set-OrchAsset -Name "${script:AssetName}_TypeKeep" -Value '20'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetName}_TypeKeep"
        $a.ValueType | Should -Be 'Integer'
        $a.Value | Should -Be '20'
    }

    It 'Set-OrchAsset with wildcard updates multiple assets' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_WcA" -Value 'before'
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_WcB" -Value 'before'
        Clear-OrchCache
        Set-OrchAsset -Name "${script:AssetName}_Wc*" -Value 'after'
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:AssetName}_Wc*"
        $assets | ForEach-Object { $_.Value | Should -Be 'after' }
    }

    It 'Set-OrchAsset with invalid ValueType writes an error' {
        $err = $null
        Set-OrchAsset -ValueType 'InvalidType' -Name "${script:AssetName}_Err" -Value 'x' -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchAsset -Name wildcard filters correctly' {
        $assets = Get-OrchAsset -Name "${script:AssetName}_*"
        $assets.Count | Should -BeGreaterOrEqual 5
    }

    It 'Get-OrchAsset -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'assets.csv'
        Get-OrchAsset -Name "${script:AssetName}_Text" -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be "${script:AssetName}_Text"
        $rows[0].ValueType | Should -Be 'Text'
    }

    It 'CSV round-trip: Export then re-import preserves values' {
        $exportCsv = Join-Path $script:TempDir 'asset_roundtrip_export.csv'
        Get-OrchAsset -Name "${script:AssetName}_Text" -ExportCsv $exportCsv

        # Change value, then re-import from CSV to restore
        Set-OrchAsset -Name "${script:AssetName}_Text" -Value 'changed'
        Clear-OrchCache

        Import-Csv $exportCsv | Set-OrchAsset
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:AssetName}_Text").Value | Should -Be 'updated'
    }

    It 'Remove-OrchAsset with wildcard removes all test assets' {
        Remove-OrchAsset -Name "${script:AssetName}_*" -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:AssetName}_*" -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Credential Asset CRUD + CSV Round-Trip
# ---------------------------------------------------------------------------
Describe 'Credential Asset' {
    BeforeAll {
        $script:CredName = "${script:Prefix}Cred"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}Cred*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Set-OrchCredentialAsset creates a credential asset' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -CredentialUsername 'testuser' -CredentialPassword 'testpass'
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CredName}_Basic"
        $a | Should -Not -BeNullOrEmpty
        $a.ValueType | Should -Be 'Credential'
        $a.CredentialUsername | Should -Be 'testuser'
    }

    It 'Set-OrchCredentialAsset updates CredentialUsername' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -CredentialUsername 'updated' -CredentialPassword 'updated'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:CredName}_Basic").CredentialUsername | Should -Be 'updated'
    }

    It 'Set-OrchCredentialAsset updates Description' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -Description 'Cred desc' -CredentialUsername 'updated' -CredentialPassword 'updated'
        Clear-OrchCache
        (Get-OrchAsset -Name "${script:CredName}_Basic").Description | Should -Be 'Cred desc'
    }

    It 'Set-OrchCredentialAsset with empty CredentialPassword does not clear Global value' {
        Set-OrchCredentialAsset -Name "${script:CredName}_Basic" -CredentialPassword ''
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CredName}_Basic"
        $a | Should -Not -BeNullOrEmpty
        $a.HasDefaultValue | Should -Be $true
    }

    It 'Get-OrchAsset -ExportCredentialCsv exports credential CSV' {
        $csv = Join-Path $script:TempDir 'creds.csv'
        Get-OrchAsset -Name "${script:CredName}_Basic" -ExportCredentialCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -BeGreaterOrEqual 1
        $rows[0].Name | Should -Be "${script:CredName}_Basic"
    }

    It 'Import-Csv | Set-OrchCredentialAsset creates from CSV' {
        $csv = Join-Path $script:TempDir 'cred_import.csv'
        $path = $script:RootFolder
        @"
Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName
${path},${script:CredName}_CsvA,From CSV,,,,csv_user_a,csv_pass_a,
${path},${script:CredName}_CsvB,From CSV,,,,csv_user_b,csv_pass_b,
"@ | Set-Content -Path $csv -Encoding UTF8

        Import-Csv $csv | Set-OrchCredentialAsset
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:CredName}_Csv*"
        $assets.Count | Should -Be 2
        ($assets | Where-Object Name -like '*_CsvA').CredentialUsername | Should -Be 'csv_user_a'
        ($assets | Where-Object Name -like '*_CsvB').CredentialUsername | Should -Be 'csv_user_b'
    }

    It 'Credential CSV round-trip preserves values' {
        $exportCsv = Join-Path $script:TempDir 'cred_roundtrip.csv'
        Get-OrchAsset -Name "${script:CredName}_CsvA" -ExportCredentialCsv $exportCsv
        $exported = Import-Csv $exportCsv
        $exported.CredentialUsername | Should -Be 'csv_user_a'
    }

    It 'Remove-OrchAsset removes credential assets' {
        Remove-OrchAsset -Name "${script:CredName}_*" -ValueType Credential -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:CredName}_*" -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Bucket CRUD + BucketItem Import/Export
# ---------------------------------------------------------------------------
Describe 'Bucket' {
    BeforeAll {
        $script:BucketName = "${script:Prefix}Bucket"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchBucket -Name "${script:Prefix}Bucket*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchBucket creates a bucket' {
        New-OrchBucket -Name $script:BucketName -Description 'Created by Pester'
        Clear-OrchCache
        $b = Get-OrchBucket -Name $script:BucketName
        $b | Should -Not -BeNullOrEmpty
        $b.Name | Should -Be $script:BucketName
    }

    It 'Get-OrchBucket -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'buckets.csv'
        Get-OrchBucket -Name $script:BucketName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:BucketName
    }

    It 'Import-OrchBucketItem uploads a file' {
        $file = Join-Path $script:TempDir 'testfile.txt'
        'Hello from Pester' | Set-Content -Path $file -Encoding UTF8

        Import-OrchBucketItem -Name $script:BucketName -Source $file
        Clear-OrchCache
        $items = Get-OrchBucketItem -Name $script:BucketName
        $items | Should -Not -BeNullOrEmpty
        ($items | Where-Object FullPath -like '*testfile.txt') | Should -Not -BeNullOrEmpty
    }

    It 'Export-OrchBucketItem downloads the file' {
        $dest = Join-Path $script:TempDir 'bucket_download'
        New-Item -Path $dest -ItemType Directory -Force | Out-Null

        Export-OrchBucketItem -Name $script:BucketName -FullPath 'testfile.txt' -Destination $dest
        # Files are organized under subdirectories by bucket name
        $downloaded = Get-ChildItem $dest -Recurse -Filter 'testfile.txt'
        $downloaded | Should -Not -BeNullOrEmpty
        (Get-Content $downloaded.FullName) | Should -BeLike '*Hello from Pester*'
    }

    It 'Remove-OrchBucket removes the bucket' {
        Remove-OrchBucket -Name $script:BucketName -Confirm:$false
        Clear-OrchCache
        Get-OrchBucket -Name $script:BucketName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# FolderMachine Assignment
# ---------------------------------------------------------------------------
Describe 'FolderMachine' {
    BeforeAll {
        $script:FMMachineName = "${script:Prefix}FMMachine"
        New-OrchMachine -Name $script:FMMachineName -Path "${script:Drive}:\"
        Clear-OrchCache
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchFolderMachine -Name "${script:Prefix}*" -ErrorAction SilentlyContinue
        Pop-Location
        Remove-OrchMachine -Name $script:FMMachineName -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Add-OrchFolderMachine assigns a machine to the folder' {
        Add-OrchFolderMachine -Name $script:FMMachineName
        Clear-OrchCache
        $fm = Get-OrchFolderMachine -Name $script:FMMachineName
        $fm | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchFolderMachine -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'foldermachines.csv'
        Get-OrchFolderMachine -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        ($rows | Where-Object Name -eq $script:FMMachineName) | Should -Not -BeNullOrEmpty
    }

    It 'Remove-OrchFolderMachine unassigns the machine' {
        Remove-OrchFolderMachine -Name $script:FMMachineName
        Clear-OrchCache
        Get-OrchFolderMachine -Name $script:FMMachineName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Process CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Process' {
    BeforeAll {
        if (-not $script:PackageId) {
            Set-ItResult -Skipped -Because 'No package found on reference drive'
        }
        $script:ProcessName = "${script:Prefix}Process"
        Push-Location $script:RootFolder
    }

    AfterAll {
        Remove-OrchProcess -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchProcess creates a process from a package' {
        New-OrchProcess -Id $script:PackageId -Name $script:ProcessName
        Clear-OrchCache
        $p = Get-OrchProcess -Name $script:ProcessName
        $p | Should -Not -BeNullOrEmpty
        $p.Name | Should -Be $script:ProcessName
    }

    It 'Get-OrchProcess -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'processes.csv'
        Get-OrchProcess -Name $script:ProcessName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:ProcessName
    }

    It 'Remove-OrchProcess removes the process' {
        Remove-OrchProcess -Name $script:ProcessName -Confirm:$false
        Clear-OrchCache
        Get-OrchProcess -Name $script:ProcessName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Trigger CRUD + CSV Export
# ---------------------------------------------------------------------------
Describe 'Trigger' {
    BeforeAll {
        if (-not $script:PackageId) {
            Set-ItResult -Skipped -Because 'No package found on reference drive'
        }
        $script:TriggerProcess = "${script:Prefix}TrigProcess"
        $script:TriggerName = "${script:Prefix}Trigger"
        Push-Location $script:RootFolder

        # Create a process for the trigger
        New-OrchProcess -Id $script:PackageId -Name $script:TriggerProcess
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchTrigger -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchProcess -Name $script:TriggerProcess -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'New-OrchTrigger creates a time trigger' {
        New-OrchTrigger -Name $script:TriggerName -ReleaseName $script:TriggerProcess `
            -StartProcessCron '0 0 0 1/1 * ? *' -Enabled false
        Clear-OrchCache
        $t = Get-OrchTrigger -Name $script:TriggerName
        $t | Should -Not -BeNullOrEmpty
        $t.Name | Should -Be $script:TriggerName
        $t.Enabled | Should -Be $false
    }

    It 'Get-OrchTrigger -ExportCsv exports to CSV' {
        $csv = Join-Path $script:TempDir 'triggers.csv'
        Get-OrchTrigger -Name $script:TriggerName -ExportCsv $csv
        $csv | Should -Exist
        $rows = Import-Csv $csv
        $rows.Count | Should -Be 1
        $rows[0].Name | Should -Be $script:TriggerName
    }

    It 'Remove-OrchTrigger removes the trigger' {
        Remove-OrchTrigger -Name $script:TriggerName -Confirm:$false
        Clear-OrchCache
        Get-OrchTrigger -Name $script:TriggerName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Copy Across Folders
# ---------------------------------------------------------------------------
Describe 'Copy Across Folders' {
    BeforeAll {
        Push-Location $script:RootFolder

        # Create entities to copy
        $script:CopyMachine = "${script:Prefix}CopyMachine"
        $script:CopyQueue = "${script:Prefix}CopyQueue"
        $script:CopyAsset = "${script:Prefix}CopyAsset"
        $script:CopyBucket = "${script:Prefix}CopyBucket"

        New-OrchMachine -Name $script:CopyMachine -Path "${script:Drive}:\"
        New-OrchQueue -Name $script:CopyQueue -Description 'Source queue'
        Set-OrchAsset -ValueType Text -Name $script:CopyAsset -Value 'source'
        New-OrchBucket -Name $script:CopyBucket -Description 'Source bucket'
        Clear-OrchCache
    }

    AfterAll {
        # Clean up source entities
        Remove-OrchQueue -Name $script:CopyQueue -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name $script:CopyAsset -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchBucket -Name $script:CopyBucket -Confirm:$false -ErrorAction SilentlyContinue

        # Clean up copied entities in destination folder
        Push-Location $script:CopyFolder -ErrorAction SilentlyContinue
        Remove-OrchQueue -Name $script:CopyQueue -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchAsset -Name $script:CopyAsset -Confirm:$false -ErrorAction SilentlyContinue
        Remove-OrchBucket -Name $script:CopyBucket -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location

        Remove-OrchMachine -Name $script:CopyMachine -Path "${script:Drive}:\" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
    }

    It 'Copy-OrchQueue copies a queue to another folder' {
        Copy-OrchQueue -Name $script:CopyQueue -Destination $script:CopyFolder
        Clear-OrchCache
        $q = Get-OrchQueue -Name $script:CopyQueue -Path $script:CopyFolder
        $q | Should -Not -BeNullOrEmpty
        $q.Description | Should -Be 'Source queue'
    }

    It 'Copy-OrchAsset copies an asset to another folder' {
        Copy-OrchAsset -Name $script:CopyAsset -Destination $script:CopyFolder
        Clear-OrchCache
        Push-Location $script:CopyFolder
        $a = Get-OrchAsset -Name $script:CopyAsset
        Pop-Location
        $a | Should -Not -BeNullOrEmpty
        $a.Value | Should -Be 'source'
    }

    It 'Copy-OrchBucket copies a bucket to another folder' {
        Copy-OrchBucket -Name $script:CopyBucket -Destination $script:CopyFolder
        Clear-OrchCache
        $b = Get-OrchBucket -Name $script:CopyBucket -Path $script:CopyFolder
        $b | Should -Not -BeNullOrEmpty
    }

    It 'Copy-OrchMachine assigns a machine to another folder' {
        Push-Location $script:CopyFolder
        Add-OrchFolderMachine -Name $script:CopyMachine
        Clear-OrchCache
        $fm = Get-OrchFolderMachine -Name $script:CopyMachine
        Pop-Location
        $fm | Should -Not -BeNullOrEmpty
    }
}

# ---------------------------------------------------------------------------
# Read-Only Cmdlets
# ---------------------------------------------------------------------------
Describe 'Read-Only Cmdlets' {
    It 'Get-OrchCurrentUser returns current user' {
        $user = Get-OrchCurrentUser -Path "${script:Drive}:\"
        $user | Should -Not -BeNullOrEmpty
        $user.UserName | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchRole returns roles' {
        $roles = Get-OrchRole -Path "${script:Drive}:\"
        $roles | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchSetting returns settings' {
        $settings = Get-OrchSetting -Path "${script:Drive}:\"
        $settings | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchHelp returns help text' {
        $help = Get-OrchHelp
        $help | Should -Not -BeNullOrEmpty
        $help | Should -Match 'UiPathOrch'
    }

    It 'Get-OrchPSDrive returns drive info' {
        $drv = Get-OrchPSDrive "${script:Drive}:"
        $drv | Should -Not -BeNullOrEmpty
        $drv.Root | Should -Not -BeNullOrEmpty
    }
}
