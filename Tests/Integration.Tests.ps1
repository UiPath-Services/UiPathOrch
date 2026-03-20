#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0' }
#Requires -Modules UiPathOrch

<#
.SYNOPSIS
    Integration tests for UiPathOrch module.
    Requires connected Orch1: and Orch2: drives (run Import-OrchConfig first).

.DESCRIPTION
    These tests create, update, and remove real entities on the Orchestrator.
    They use a "PesterTest_" prefix for all created entities so they can be
    identified and cleaned up if a test fails.

.NOTES
    Run with: Invoke-Pester -Path Tests\Integration.Tests.ps1 -Output Detailed
#>

BeforeAll {
    $script:Drive1 = 'Orch1'
    $script:Drive2 = 'Orch2'
    $script:Prefix = "PesterTest_$(Get-Random -Maximum 9999)_"

    # Suppress all confirmation prompts
    $script:OriginalConfirmPreference = $ConfirmPreference
    $global:ConfirmPreference = 'None'

    # Verify drives are available
    Get-PSDrive $script:Drive1 -ErrorAction Stop | Out-Null
    Get-PSDrive $script:Drive2 -ErrorAction Stop | Out-Null

    # Navigate into a folder on Orch1 (needed for folder-scoped cmdlets)
    Push-Location "${script:Drive1}:\Shared"
}

AfterAll {
    Pop-Location
    $global:ConfirmPreference = $script:OriginalConfirmPreference
}

Describe 'Machine CRUD' {
    BeforeAll {
        $script:MachineName = "${script:Prefix}Machine"
    }

    AfterAll {
        $existing = Get-OrchMachine -Name $script:MachineName -ErrorAction SilentlyContinue
        if ($existing) {
            Remove-OrchMachine -Name $script:MachineName -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'New-OrchMachine creates a machine' {
        New-OrchMachine -Name $script:MachineName -Description 'Created by Pester'
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine | Should -Not -BeNullOrEmpty
        $machine.Name | Should -Be $script:MachineName
        $machine.Description | Should -Be 'Created by Pester'
    }

    It 'Update-OrchMachine updates Description' {
        Update-OrchMachine -Name $script:MachineName -Description 'Updated by Pester'
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.Description | Should -Be 'Updated by Pester'
    }

    It 'Update-OrchMachine updates UnattendedSlots' {
        Update-OrchMachine -Name $script:MachineName -UnattendedSlots 3
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.UnattendedSlots | Should -Be 3
    }

    It 'Update-OrchMachine can set UnattendedSlots to 0' {
        Update-OrchMachine -Name $script:MachineName -UnattendedSlots 0
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.UnattendedSlots | Should -Be 0
    }

    It 'Update-OrchMachine updates AutomationType' {
        Update-OrchMachine -Name $script:MachineName -AutomationType 'Foreground'
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.AutomationType | Should -Be 'Foreground'
    }

    It 'Update-OrchMachine updates TargetFramework' {
        Update-OrchMachine -Name $script:MachineName -TargetFramework 'Windows'
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.TargetFramework | Should -Be 'Windows'
    }

    It 'Update-OrchMachine updates Tags' {
        Update-OrchMachine -Name $script:MachineName -Tags 'tagA', 'tagB'
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.Tags.Count | Should -Be 2
    }

    It 'Update-OrchMachine clears Tags' {
        Update-OrchMachine -Name $script:MachineName -Tags ''
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.Tags.Count | Should -Be 0
    }

    It 'Update-OrchMachine skips no-op (same value)' {
        Update-OrchMachine -Name $script:MachineName -Description 'no-op test'
        Clear-OrchCache
        Update-OrchMachine -Name $script:MachineName -Description 'no-op test'
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.Description | Should -Be 'no-op test'
    }

    It 'Update-OrchMachine with multiple properties' {
        Update-OrchMachine -Name $script:MachineName -Description 'multi' -AutomationType 'Any' -TargetFramework 'Any'
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName
        $machine.Description | Should -Be 'multi'
        $machine.AutomationType | Should -Be 'Any'
        $machine.TargetFramework | Should -Be 'Any'
    }

    It 'Remove-OrchMachine removes the machine' {
        Remove-OrchMachine -Name $script:MachineName -Confirm:$false
        Clear-OrchCache
        $machine = Get-OrchMachine -Name $script:MachineName -ErrorAction SilentlyContinue
        $machine | Should -BeNullOrEmpty
    }
}

Describe 'Queue CRUD' {
    BeforeAll {
        $script:QueueName = "${script:Prefix}Queue"
    }

    AfterAll {
        $existing = Get-OrchQueue -Name $script:QueueName -ErrorAction SilentlyContinue
        if ($existing) {
            Remove-OrchQueue -Name $script:QueueName -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'New-OrchQueue creates a queue' {
        New-OrchQueue -Name $script:QueueName -Description 'Created by Pester'
        Clear-OrchCache
        $queue = Get-OrchQueue -Name $script:QueueName
        $queue | Should -Not -BeNullOrEmpty
        $queue.Name | Should -Be $script:QueueName
        $queue.Description | Should -Be 'Created by Pester'
    }

    It 'Update-OrchQueue updates Description' {
        Update-OrchQueue -Name $script:QueueName -Description 'Updated by Pester'
        Clear-OrchCache
        $queue = Get-OrchQueue -Name $script:QueueName
        $queue.Description | Should -Be 'Updated by Pester'
    }

    It 'Update-OrchQueue updates MaxNumberOfRetries' {
        Update-OrchQueue -Name $script:QueueName -MaxNumberOfRetries 5
        Clear-OrchCache
        $queue = Get-OrchQueue -Name $script:QueueName
        $queue.MaxNumberOfRetries | Should -Be 5
    }

    It 'Update-OrchQueue updates AcceptAutomaticallyRetry' {
        Update-OrchQueue -Name $script:QueueName -AcceptAutomaticallyRetry true
        Clear-OrchCache
        $queue = Get-OrchQueue -Name $script:QueueName
        $queue.AcceptAutomaticallyRetry | Should -Be $true
    }

    It 'Update-OrchQueue skips no-op' {
        Update-OrchQueue -Name $script:QueueName -Description 'Updated by Pester'
        $queue = Get-OrchQueue -Name $script:QueueName
        $queue.Description | Should -Be 'Updated by Pester'
    }

    It 'Remove-OrchQueue removes the queue' {
        Remove-OrchQueue -Name $script:QueueName -Confirm:$false
        Clear-OrchCache
        $queue = Get-OrchQueue -Name $script:QueueName -ErrorAction SilentlyContinue
        $queue | Should -BeNullOrEmpty
    }
}

Describe 'Bucket CRUD' {
    BeforeAll {
        $script:BucketName = "${script:Prefix}Bucket"
    }

    AfterAll {
        $existing = Get-OrchBucket -Name $script:BucketName -ErrorAction SilentlyContinue
        if ($existing) {
            Remove-OrchBucket -Name $script:BucketName -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'New-OrchBucket creates a bucket' {
        New-OrchBucket -Name $script:BucketName -Description 'Created by Pester'
        Clear-OrchCache
        $bucket = Get-OrchBucket -Name $script:BucketName
        $bucket | Should -Not -BeNullOrEmpty
        $bucket.Name | Should -Be $script:BucketName
        $bucket.Description | Should -Be 'Created by Pester'
    }

    It 'Remove-OrchBucket removes the bucket' {
        Remove-OrchBucket -Name $script:BucketName -Confirm:$false
        Clear-OrchCache
        $bucket = Get-OrchBucket -Name $script:BucketName -ErrorAction SilentlyContinue
        $bucket | Should -BeNullOrEmpty
    }
}

Describe 'Folder Navigation' {
    It 'Get-ChildItem lists folders at drive root' {
        $folders = Get-ChildItem "${script:Drive1}:\"
        $folders | Should -Not -BeNullOrEmpty
    }

    It 'Set-Location navigates to a folder' {
        { Set-Location "${script:Drive1}:\Shared" } | Should -Not -Throw
        (Get-Location).Path | Should -BeLike '*Shared*'
    }
}

Describe 'Get Cmdlets (read-only)' {
    It 'Get-OrchUser returns users' {
        $users = Get-OrchUser
        $users | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchRole returns roles' {
        $roles = Get-OrchRole
        $roles | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchMachine returns machines' {
        $machines = Get-OrchMachine
        $machines | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchSetting returns settings' {
        $settings = Get-OrchSetting
        $settings | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchCurrentUser returns current user' {
        $user = Get-OrchCurrentUser
        $user | Should -Not -BeNullOrEmpty
        $user.UserName | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchHelp returns help text' {
        $help = Get-OrchHelp
        $help | Should -Not -BeNullOrEmpty
        $help | Should -Match 'UiPathOrch'
    }
}

Describe 'Asset CRUD' {
    BeforeAll {
        $script:AssetName = "${script:Prefix}Asset"
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}*" -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Set-OrchAsset creates a Text asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Value 'hello' -Description 'Text asset'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:AssetName}_Text"
        $asset | Should -Not -BeNullOrEmpty
        $asset.Value | Should -Be 'hello'
        $asset.Description | Should -Be 'Text asset'
    }

    It 'Set-OrchAsset creates an Integer asset' {
        Set-OrchAsset -ValueType Integer -Name "${script:AssetName}_Int" -Value '42'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:AssetName}_Int"
        $asset | Should -Not -BeNullOrEmpty
        $asset.Value | Should -Be '42'
    }

    It 'Set-OrchAsset creates a Bool asset' {
        Set-OrchAsset -ValueType Bool -Name "${script:AssetName}_Bool" -Value 'true'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:AssetName}_Bool"
        $asset | Should -Not -BeNullOrEmpty
        $asset.Value | Should -Be 'True'
    }

    It 'Set-OrchAsset updates the value of an existing Text asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Value 'updated'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:AssetName}_Text"
        $asset.Value | Should -Be 'updated'
    }

    It 'Set-OrchAsset updates the Description of an existing asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetName}_Text" -Description 'Updated description'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:AssetName}_Text"
        $asset.Description | Should -Be 'Updated description'
    }

    It 'Get-OrchAsset with -Name wildcard filters correctly' {
        $assets = Get-OrchAsset -Name "${script:AssetName}_*"
        $assets.Count | Should -BeGreaterOrEqual 3
    }

    It 'Remove-OrchAsset removes an asset by name' {
        Remove-OrchAsset -Name "${script:AssetName}_Bool" -Confirm:$false
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:AssetName}_Bool" -ErrorAction SilentlyContinue
        $asset | Should -BeNullOrEmpty
    }

    It 'Remove-OrchAsset with wildcard removes remaining assets' {
        Remove-OrchAsset -Name "${script:AssetName}_*" -Confirm:$false
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:AssetName}_*" -ErrorAction SilentlyContinue
        $assets | Should -BeNullOrEmpty
    }
}

Describe 'Asset Advanced' {
    BeforeAll {
        $script:AssetAdv = "${script:Prefix}AssetAdv"
        $script:TestUser = 'ytsuda@gmail.com'
        $script:TestMachine = 'aiai'
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}AssetAdv*" -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Set-OrchAsset with invalid ValueType writes an error' {
        $err = $null
        Set-OrchAsset -ValueType 'InvalidType' -Name "${script:AssetAdv}_Err" -Value 'x' -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }

    It 'Set-OrchAsset with non-parseable Bool value writes an error' {
        $err = $null
        Set-OrchAsset -ValueType Bool -Name "${script:AssetAdv}_BoolErr" -Value 'notabool' -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }

    It 'Set-OrchAsset with non-parseable Integer value writes an error' {
        $err = $null
        Set-OrchAsset -ValueType Integer -Name "${script:AssetAdv}_IntErr" -Value 'notanumber' -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }

    It 'Set-OrchAsset does not overwrite existing ValueType' {
        Set-OrchAsset -ValueType Integer -Name "${script:AssetAdv}_TypeKeep" -Value '10'
        Clear-OrchCache
        # Update value without specifying ValueType
        Set-OrchAsset -Name "${script:AssetAdv}_TypeKeep" -Value '20'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:AssetAdv}_TypeKeep"
        $asset.ValueType | Should -Be 'Integer'
        $asset.Value | Should -Be '20'
    }

    It 'Set-OrchAsset with wildcard name updates multiple assets' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetAdv}_WcA" -Value 'before'
        Set-OrchAsset -ValueType Text -Name "${script:AssetAdv}_WcB" -Value 'before'
        Clear-OrchCache
        Set-OrchAsset -Name "${script:AssetAdv}_Wc*" -Value 'after'
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:AssetAdv}_Wc*"
        $assets | ForEach-Object { $_.Value | Should -Be 'after' }
    }

    It 'Set-OrchAsset creates a PerRobot value with -UserName' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetAdv}_PerRobot" -Value 'user-val' -UserName $script:TestUser
        Clear-OrchCache
        # -ExpandUserValues outputs AssetUserValue objects directly to the pipeline
        $userValues = @(Get-OrchAsset -Name "${script:AssetAdv}_PerRobot" -ExpandUserValues)
        $uv = $userValues | Where-Object { $_.UserName -eq $script:TestUser }
        $uv | Should -Not -BeNullOrEmpty
        $uv.Value | Should -Be 'user-val'
    }

    It 'Set-OrchAsset updates a PerRobot value' {
        Set-OrchAsset -Name "${script:AssetAdv}_PerRobot" -Value 'user-val-updated' -UserName $script:TestUser
        Clear-OrchCache
        $userValues = @(Get-OrchAsset -Name "${script:AssetAdv}_PerRobot" -ExpandUserValues)
        $uv = $userValues | Where-Object { $_.UserName -eq $script:TestUser }
        $uv.Value | Should -Be 'user-val-updated'
    }

    It 'Set-OrchAsset creates a PerRobot value with -UserName and -MachineName' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetAdv}_PerRobotMachine" -Value 'machine-val' -UserName $script:TestUser -MachineName $script:TestMachine
        Clear-OrchCache
        $userValues = @(Get-OrchAsset -Name "${script:AssetAdv}_PerRobotMachine" -ExpandUserValues)
        $uv = $userValues | Where-Object { $_.UserName -eq $script:TestUser -and $_.MachineName -eq $script:TestMachine }
        $uv | Should -Not -BeNullOrEmpty
        $uv.Value | Should -Be 'machine-val'
    }

    It 'Set-OrchAsset with empty Value deletes PerRobot value' {
        Set-OrchAsset -Name "${script:AssetAdv}_PerRobot" -Value '' -UserName $script:TestUser
        Clear-OrchCache
        $userValues = @(Get-OrchAsset -Name "${script:AssetAdv}_PerRobot" -ExpandUserValues)
        $uv = $userValues | Where-Object { $_.UserName -eq $script:TestUser }
        $uv | Should -BeNullOrEmpty
    }

    It 'Set-OrchAsset with Global value then empty Value removes the asset' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetAdv}_ClearGlobal" -Value 'has-value'
        Clear-OrchCache
        Set-OrchAsset -Name "${script:AssetAdv}_ClearGlobal" -Value ''
        Clear-OrchCache
        # When Global value is cleared and no PerRobot values exist, the asset is deleted
        $asset = Get-OrchAsset -Name "${script:AssetAdv}_ClearGlobal" -ErrorAction SilentlyContinue
        if ($asset) {
            $asset.HasDefaultValue | Should -Be $false
        }
    }

    It 'Set-OrchAsset skips no-op when same value is set' {
        Set-OrchAsset -ValueType Text -Name "${script:AssetAdv}_NoOp" -Value 'same'
        Clear-OrchCache
        # Second call with same value should not error
        Set-OrchAsset -Name "${script:AssetAdv}_NoOp" -Value 'same'
        $asset = Get-OrchAsset -Name "${script:AssetAdv}_NoOp"
        $asset.Value | Should -Be 'same'
    }

    It 'Set-OrchAsset with -MachineName but no -UserName writes a warning' {
        $err = $null
        Set-OrchAsset -ValueType Text -Name "${script:AssetAdv}_MachineOnly" -Value 'test' -MachineName $script:TestMachine -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
    }
}

Describe 'Credential Asset CRUD' {
    BeforeAll {
        $script:CredAssetName = "${script:Prefix}CredAsset"
        $script:TestUser = 'ytsuda@gmail.com'
        $script:TestMachine = 'aiai'
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}CredAsset*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Set-OrchCredentialAsset creates a credential asset' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_Basic" -CredentialUsername 'testuser' -CredentialPassword 'testpass'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_Basic"
        $asset | Should -Not -BeNullOrEmpty
        $asset.ValueType | Should -Be 'Credential'
        $asset.CredentialUsername | Should -Be 'testuser'
    }

    It 'Set-OrchCredentialAsset updates CredentialUsername' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_Basic" -CredentialUsername 'updateduser' -CredentialPassword 'updatedpass'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_Basic"
        $asset.CredentialUsername | Should -Be 'updateduser'
    }

    It 'Set-OrchCredentialAsset updates Description' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_Basic" -Description 'Cred description' -CredentialUsername 'updateduser' -CredentialPassword 'updatedpass'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_Basic"
        $asset.Description | Should -Be 'Cred description'
    }

    It 'Set-OrchCredentialAsset creates a PerRobot credential with -UserName' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_PerRobot" -UserName $script:TestUser -CredentialUsername 'peruser' -CredentialPassword 'perpass'
        Clear-OrchCache
        $uv = @(Get-OrchAsset -Name "${script:CredAssetName}_PerRobot" -ExpandUserValues)
        $match = $uv | Where-Object { $_.UserName -eq $script:TestUser }
        $match | Should -Not -BeNullOrEmpty
        $match.CredentialUsername | Should -Be 'peruser'
    }

    It 'Set-OrchCredentialAsset creates a PerRobot credential with -UserName and -MachineName' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_PerRobotMachine" -UserName $script:TestUser -MachineName $script:TestMachine -CredentialUsername 'machuser' -CredentialPassword 'machpass'
        Clear-OrchCache
        $uv = @(Get-OrchAsset -Name "${script:CredAssetName}_PerRobotMachine" -ExpandUserValues)
        $match = $uv | Where-Object { $_.UserName -eq $script:TestUser -and $_.MachineName -eq $script:TestMachine }
        $match | Should -Not -BeNullOrEmpty
        $match.CredentialUsername | Should -Be 'machuser'
    }

    It 'Set-OrchCredentialAsset with empty CredentialPassword deletes PerRobot value' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_PerRobot" -UserName $script:TestUser -CredentialPassword ''
        Clear-OrchCache
        $uv = @(Get-OrchAsset -Name "${script:CredAssetName}_PerRobot" -ExpandUserValues)
        $match = $uv | Where-Object { $_.UserName -eq $script:TestUser }
        $match | Should -BeNullOrEmpty
    }

    It 'Set-OrchCredentialAsset with empty CredentialPassword does not clear Global value' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_Basic" -CredentialPassword ''
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_Basic"
        $asset | Should -Not -BeNullOrEmpty
        $asset.HasDefaultValue | Should -Be $true
        $asset.CredentialUsername | Should -Be 'updateduser'
    }

    It 'Set-OrchCredentialAsset with -CredentialStore sets the credential store' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_WithStore" -CredentialUsername 'storeuser' -CredentialPassword 'storepass' -CredentialStore 'Orchestrator Database'
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_WithStore"
        $asset | Should -Not -BeNullOrEmpty
        $asset.CredentialStoreId | Should -Not -BeNullOrEmpty
    }

    It 'Set-OrchCredentialAsset with empty CredentialPassword from CSV does not destroy existing password' {
        # Simulate Export -> Import round-trip: CSV has CredentialUsername but empty CredentialPassword
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_CsvSafe" -CredentialUsername 'csvuser' -CredentialPassword 'csvpass'
        Clear-OrchCache
        # Import with empty password (as exported CSV would have)
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_CsvSafe" -CredentialUsername 'csvuser' -CredentialPassword ''
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_CsvSafe"
        $asset | Should -Not -BeNullOrEmpty
        $asset.HasDefaultValue | Should -Be $true
        $asset.CredentialUsername | Should -Be 'csvuser'
    }

    It 'Set-OrchCredentialAsset with empty CredentialPassword does not clear the credential' {
        # Create an asset with a password
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_EmptyPass" -CredentialUsername 'keepuser' -CredentialPassword 'keeppass'
        Clear-OrchCache
        # Update with empty CredentialPassword — should NOT delete the Global value
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_EmptyPass" -CredentialUsername 'keepuser' -CredentialPassword ''
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_EmptyPass"
        $asset | Should -Not -BeNullOrEmpty
        $asset.CredentialUsername | Should -Be 'keepuser'
        $asset.HasDefaultValue | Should -Be $true
    }

    It 'Set-OrchCredentialAsset skips no-op when same username is set' {
        Set-OrchCredentialAsset -Name "${script:CredAssetName}_WithStore" -CredentialUsername 'storeuser'
        # Should not error
        $asset = Get-OrchAsset -Name "${script:CredAssetName}_WithStore"
        $asset.CredentialUsername | Should -Be 'storeuser'
    }

    It 'Remove-OrchAsset removes credential assets' {
        Remove-OrchAsset -Name "${script:CredAssetName}_*" -ValueType Credential -Confirm:$false
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:CredAssetName}_*"
        $assets | Should -BeNullOrEmpty
    }
}

Describe 'Credential Asset CSV Import' {
    BeforeAll {
        $script:CredCsvPrefix = "${script:Prefix}CredCsv"
        $script:CredCsvDir = Join-Path $env:TEMP "PesterTest_CredCsv_$(Get-Random -Maximum 9999)"
        New-Item -Path $script:CredCsvDir -ItemType Directory -Force | Out-Null
        $script:SharedPath = "${script:Drive1}:\Shared"
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}CredCsv*" -ValueType Credential -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item $script:CredCsvDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Import-Csv | Set-OrchCredentialAsset creates multiple credential assets' {
        $csvPath = Join-Path $script:CredCsvDir 'new_creds.csv'
        @"
Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName
$($script:SharedPath),${script:CredCsvPrefix}_A,Desc A,,,,cred_user_a,cred_pass_a,
$($script:SharedPath),${script:CredCsvPrefix}_B,Desc B,,,,cred_user_b,cred_pass_b,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchCredentialAsset
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:CredCsvPrefix}_*"
        $assets.Count | Should -Be 2
        ($assets | Where-Object { $_.Name -like '*_A' }).CredentialUsername | Should -Be 'cred_user_a'
        ($assets | Where-Object { $_.Name -like '*_B' }).CredentialUsername | Should -Be 'cred_user_b'
    }

    It 'Import-Csv | Set-OrchCredentialAsset aggregates PerRobot rows for same asset' {
        $csvPath = Join-Path $script:CredCsvDir 'perrobot_creds.csv'
        $user1 = 'ytsuda@gmail.com'
        $user2 = 'ytsuda+c_c@gmail.com'
        @"
Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName
$($script:SharedPath),${script:CredCsvPrefix}_Multi,,,${user1},,per_user1,per_pass1,
$($script:SharedPath),${script:CredCsvPrefix}_Multi,,,${user2},,per_user2,per_pass2,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchCredentialAsset
        Clear-OrchCache
        $uv = @(Get-OrchAsset -Name "${script:CredCsvPrefix}_Multi" -ExpandUserValues)
        $uv.Count | Should -Be 2
        ($uv | Where-Object { $_.UserName -eq $user1 }).CredentialUsername | Should -Be 'per_user1'
        ($uv | Where-Object { $_.UserName -eq $user2 }).CredentialUsername | Should -Be 'per_user2'
    }

    It 'Import-Csv | Set-OrchCredentialAsset with multiple users x machines' {
        $csvPath = Join-Path $script:CredCsvDir 'complex_creds.csv'
        $user1 = 'ytsuda@gmail.com'
        $user2 = 'ytsuda+c_c@gmail.com'
        $machine1 = 'aiai'
        $machine2 = 'bafu'
        @"
Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName
$($script:SharedPath),${script:CredCsvPrefix}_Complex,,,${user1},${machine1},u1m1,pass_u1m1,
$($script:SharedPath),${script:CredCsvPrefix}_Complex,,,${user1},${machine2},u1m2,pass_u1m2,
$($script:SharedPath),${script:CredCsvPrefix}_Complex,,,${user2},${machine1},u2m1,pass_u2m1,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchCredentialAsset
        Clear-OrchCache
        $uv = @(Get-OrchAsset -Name "${script:CredCsvPrefix}_Complex" -ExpandUserValues)
        $uv.Count | Should -Be 3
        ($uv | Where-Object { $_.UserName -eq $user1 -and $_.MachineName -eq $machine1 }).CredentialUsername | Should -Be 'u1m1'
        ($uv | Where-Object { $_.UserName -eq $user1 -and $_.MachineName -eq $machine2 }).CredentialUsername | Should -Be 'u1m2'
        ($uv | Where-Object { $_.UserName -eq $user2 -and $_.MachineName -eq $machine1 }).CredentialUsername | Should -Be 'u2m1'
    }

    It 'Import-Csv | Set-OrchCredentialAsset with Global and PerRobot mixed' {
        $csvPath = Join-Path $script:CredCsvDir 'mixed_creds.csv'
        $user1 = 'ytsuda@gmail.com'
        @"
Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName
$($script:SharedPath),${script:CredCsvPrefix}_Mixed,Mixed cred,,,,global_user,global_pass,
$($script:SharedPath),${script:CredCsvPrefix}_Mixed,,,${user1},,per_user,per_pass,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchCredentialAsset
        Clear-OrchCache

        $asset = Get-OrchAsset -Name "${script:CredCsvPrefix}_Mixed"
        $asset | Should -Not -BeNullOrEmpty
        $asset.CredentialUsername | Should -Be 'global_user'

        $uv = @(Get-OrchAsset -Name "${script:CredCsvPrefix}_Mixed" -ExpandUserValues)
        $globalUv = $uv | Where-Object { -not $_.UserName }
        $perUv = $uv | Where-Object { $_.UserName -eq $user1 }
        $globalUv.CredentialUsername | Should -Be 'global_user'
        $perUv.CredentialUsername | Should -Be 'per_user'
    }

    It 'Import-Csv | Set-OrchCredentialAsset with CredentialStore' {
        $csvPath = Join-Path $script:CredCsvDir 'store_creds.csv'
        @"
Path,Name,Description,CredentialStore,UserName,MachineName,CredentialUsername,CredentialPassword,ExternalName
$($script:SharedPath),${script:CredCsvPrefix}_Store,,Orchestrator Database,,,store_user,store_pass,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchCredentialAsset
        Clear-OrchCache
        $asset = Get-OrchAsset -Name "${script:CredCsvPrefix}_Store"
        $asset | Should -Not -BeNullOrEmpty
        $asset.CredentialUsername | Should -Be 'store_user'
        $asset.CredentialStoreId | Should -Not -BeNullOrEmpty
    }

    It 'Credential CSV round-trip: Export then re-import preserves values' {
        # Create a fresh asset for round-trip
        Set-OrchCredentialAsset -Name "${script:CredCsvPrefix}_RT" -CredentialUsername 'rt_user' -CredentialPassword 'rt_pass'
        Clear-OrchCache

        $exportPath = Join-Path $script:CredCsvDir 'cred_roundtrip.csv'
        Get-OrchAsset -Name "${script:CredCsvPrefix}_RT" -ExportCredentialCsv $exportPath
        Clear-OrchCache

        Remove-OrchAsset -Name "${script:CredCsvPrefix}_RT" -ValueType Credential -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:CredCsvPrefix}_RT" | Should -BeNullOrEmpty

        # Re-import — note: password is not exported, so we need to add it
        $csv = Import-Csv $exportPath
        $csv | ForEach-Object { $_.CredentialPassword = 'rt_pass' }
        $csv | Set-OrchCredentialAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:CredCsvPrefix}_RT"
        $a | Should -Not -BeNullOrEmpty
        $a.CredentialUsername | Should -Be 'rt_user'
    }
}

Describe 'Asset CSV Import' {
    BeforeAll {
        $script:AssetCsvPrefix = "${script:Prefix}CsvAsset"
        $script:CsvImportDir = Join-Path $env:TEMP "PesterTest_CsvImport_$(Get-Random -Maximum 9999)"
        New-Item -Path $script:CsvImportDir -ItemType Directory -Force | Out-Null
        $script:SharedPath = "${script:Drive1}:\Shared"
    }

    AfterAll {
        Remove-OrchAsset -Name "${script:Prefix}CsvAsset*" -Confirm:$false -ErrorAction SilentlyContinue
        Remove-Item $script:CsvImportDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Import-Csv | Set-OrchAsset creates multiple assets from CSV' {
        $csvPath = Join-Path $script:CsvImportDir 'new_assets.csv'
        @"
Path,Name,Description,ValueType,Value,UserName,MachineName
$($script:SharedPath),${script:AssetCsvPrefix}_A,Desc A,Text,ValueA,,
$($script:SharedPath),${script:AssetCsvPrefix}_B,Desc B,Integer,42,,
$($script:SharedPath),${script:AssetCsvPrefix}_C,Desc C,Bool,true,,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchAsset
        Clear-OrchCache
        $assets = Get-OrchAsset -Name "${script:AssetCsvPrefix}_*"
        $assets.Count | Should -Be 3
    }

    It 'CSV-created assets have correct values' {
        $a = Get-OrchAsset -Name "${script:AssetCsvPrefix}_A"
        $a.Value | Should -Be 'ValueA'
        $a.Description | Should -Be 'Desc A'
        $a.ValueType | Should -Be 'Text'

        $b = Get-OrchAsset -Name "${script:AssetCsvPrefix}_B"
        $b.Value | Should -Be '42'
        $b.ValueType | Should -Be 'Integer'

        $c = Get-OrchAsset -Name "${script:AssetCsvPrefix}_C"
        $c.Value | Should -Be 'True'
        $c.ValueType | Should -Be 'Bool'
    }

    It 'Import-Csv | Set-OrchAsset updates existing assets from CSV' {
        $csvPath = Join-Path $script:CsvImportDir 'update_assets.csv'
        @"
Path,Name,Description,ValueType,Value,UserName,MachineName
$($script:SharedPath),${script:AssetCsvPrefix}_A,Updated Desc,Text,UpdatedValue,,
$($script:SharedPath),${script:AssetCsvPrefix}_B,,Integer,99,,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetCsvPrefix}_A"
        $a.Value | Should -Be 'UpdatedValue'
        $a.Description | Should -Be 'Updated Desc'

        $b = Get-OrchAsset -Name "${script:AssetCsvPrefix}_B"
        $b.Value | Should -Be '99'
    }

    It 'Import-Csv | Set-OrchAsset aggregates PerRobot rows for the same asset' {
        $csvPath = Join-Path $script:CsvImportDir 'perrobot_assets.csv'
        $testUser1 = 'ytsuda@gmail.com'
        $testUser2 = 'yoko.2013note.cha@gmail.com'
        @"
Path,Name,Description,ValueType,Value,UserName,MachineName
$($script:SharedPath),${script:AssetCsvPrefix}_PerRobot,,Text,val1,$testUser1,
$($script:SharedPath),${script:AssetCsvPrefix}_PerRobot,,Text,val2,$testUser2,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchAsset
        Clear-OrchCache
        $userValues = @(Get-OrchAsset -Name "${script:AssetCsvPrefix}_PerRobot" -ExpandUserValues)
        $userValues.Count | Should -Be 2
        ($userValues | Where-Object { $_.UserName -eq $testUser1 }).Value | Should -Be 'val1'
        ($userValues | Where-Object { $_.UserName -eq $testUser2 }).Value | Should -Be 'val2'
    }

    It 'Import-Csv | Set-OrchAsset aggregates multiple users x machines for multiple assets' {
        $csvPath = Join-Path $script:CsvImportDir 'complex_perrobot.csv'
        $user1 = 'ytsuda@gmail.com'
        $user2 = 'ytsuda+c_c@gmail.com'
        $machine1 = 'aiai'
        $machine2 = 'bafu'
        @"
Path,Name,Description,ValueType,Value,UserName,MachineName
$($script:SharedPath),${script:AssetCsvPrefix}_Multi1,,Text,u1m1,$user1,$machine1
$($script:SharedPath),${script:AssetCsvPrefix}_Multi1,,Text,u1m2,$user1,$machine2
$($script:SharedPath),${script:AssetCsvPrefix}_Multi1,,Text,u2m1,$user2,$machine1
$($script:SharedPath),${script:AssetCsvPrefix}_Multi2,,Integer,10,$user1,
$($script:SharedPath),${script:AssetCsvPrefix}_Multi2,,Integer,20,$user2,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchAsset
        Clear-OrchCache

        # Multi1: 3 user-machine combinations
        $uv1 = @(Get-OrchAsset -Name "${script:AssetCsvPrefix}_Multi1" -ExpandUserValues)
        $uv1.Count | Should -Be 3
        ($uv1 | Where-Object { $_.UserName -eq $user1 -and $_.MachineName -eq $machine1 }).Value | Should -Be 'u1m1'
        ($uv1 | Where-Object { $_.UserName -eq $user1 -and $_.MachineName -eq $machine2 }).Value | Should -Be 'u1m2'
        ($uv1 | Where-Object { $_.UserName -eq $user2 -and $_.MachineName -eq $machine1 }).Value | Should -Be 'u2m1'

        # Multi2: 2 user values (no machine)
        $uv2 = @(Get-OrchAsset -Name "${script:AssetCsvPrefix}_Multi2" -ExpandUserValues)
        $uv2.Count | Should -Be 2
        ($uv2 | Where-Object { $_.UserName -eq $user1 }).Value | Should -Be '10'
        ($uv2 | Where-Object { $_.UserName -eq $user2 }).Value | Should -Be '20'
    }

    It 'Import-Csv | Set-OrchAsset handles Global and PerRobot rows in same CSV' {
        $csvPath = Join-Path $script:CsvImportDir 'mixed_global_perrobot.csv'
        $user1 = 'ytsuda@gmail.com'
        @"
Path,Name,Description,ValueType,Value,UserName,MachineName
$($script:SharedPath),${script:AssetCsvPrefix}_Mixed,Mixed asset,Text,global-val,,
$($script:SharedPath),${script:AssetCsvPrefix}_Mixed,,Text,user-val,$user1,
"@ | Set-Content -Path $csvPath -Encoding UTF8

        Import-Csv $csvPath | Set-OrchAsset
        Clear-OrchCache

        # Check global value
        $asset = Get-OrchAsset -Name "${script:AssetCsvPrefix}_Mixed"
        $asset | Should -Not -BeNullOrEmpty
        $asset.Description | Should -Be 'Mixed asset'
        $asset.Value | Should -Be 'global-val'

        # Check PerRobot value
        $uv = @(Get-OrchAsset -Name "${script:AssetCsvPrefix}_Mixed" -ExpandUserValues)
        $globalUv = $uv | Where-Object { -not $_.UserName }
        $userUv = $uv | Where-Object { $_.UserName -eq $user1 }
        $globalUv.Value | Should -Be 'global-val'
        $userUv.Value | Should -Be 'user-val'
    }

    It 'PerRobot CSV round-trip: Export then re-import preserves user values' {
        $exportPath = Join-Path $script:CsvImportDir 'perrobot_roundtrip.csv'
        Get-OrchAsset -Name "${script:AssetCsvPrefix}_Multi1" -ExportCsv $exportPath
        Clear-OrchCache

        # Delete and re-import
        Remove-OrchAsset -Name "${script:AssetCsvPrefix}_Multi1" -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:AssetCsvPrefix}_Multi1" | Should -BeNullOrEmpty

        Import-Csv $exportPath | Set-OrchAsset
        Clear-OrchCache

        $uv = @(Get-OrchAsset -Name "${script:AssetCsvPrefix}_Multi1" -ExpandUserValues)
        $uv.Count | Should -Be 3
    }

    It 'Export then re-import round-trip preserves asset values' {
        $exportPath = Join-Path $script:CsvImportDir 'roundtrip_export.csv'
        Get-OrchAsset -Name "${script:AssetCsvPrefix}_A" -ExportCsv $exportPath
        Clear-OrchCache

        # Delete the asset
        Remove-OrchAsset -Name "${script:AssetCsvPrefix}_A" -Confirm:$false
        Clear-OrchCache
        Get-OrchAsset -Name "${script:AssetCsvPrefix}_A" | Should -BeNullOrEmpty

        # Re-import from the exported CSV
        Import-Csv $exportPath | Set-OrchAsset
        Clear-OrchCache
        $a = Get-OrchAsset -Name "${script:AssetCsvPrefix}_A"
        $a | Should -Not -BeNullOrEmpty
        $a.Value | Should -Be 'UpdatedValue'
    }
}

Describe 'Large-Scale PerRobot Asset' {
    BeforeAll {
        $script:LargeAssetName = "${script:Prefix}LargePerRobot"
        $script:LargeCsvDir = Join-Path $env:TEMP "PesterTest_LargeAsset_$(Get-Random -Maximum 9999)"
        New-Item -Path $script:LargeCsvDir -ItemType Directory -Force | Out-Null
        $script:SharedPath = "${script:Drive1}:\Shared"

        # Get all available users and machines for the Shared folder
        Push-Location "${script:Drive1}:\Shared"
        $script:AllUsers = @(Get-OrchFolderUser | Where-Object { $_.UserEntity.Type -ne 'DirectoryGroup' } | ForEach-Object { $_.UserEntity.UserName })
        $script:AllMachines = @(Get-OrchFolderMachine | ForEach-Object { $_.Name })
        Pop-Location
    }

    AfterAll {
        Push-Location "${script:Drive1}:\Shared"
        Remove-OrchAsset -Name "${script:Prefix}LargePerRobot*" -Confirm:$false -ErrorAction SilentlyContinue
        Pop-Location
        Remove-Item $script:LargeCsvDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Creates a large PerRobot asset with all user x machine combinations' {
        $csvPath = Join-Path $script:LargeCsvDir 'large_perrobot.csv'
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine('Path,Name,Description,ValueType,Value,UserName,MachineName')
        $expectedCount = 0
        foreach ($user in $script:AllUsers) {
            foreach ($machine in $script:AllMachines) {
                [void]$sb.AppendLine("$($script:SharedPath),${script:LargeAssetName},,Text,val_${user}_${machine},${user},${machine}")
                $expectedCount++
            }
        }
        [System.IO.File]::WriteAllText($csvPath, $sb.ToString())

        $elapsed = Measure-Command { Import-Csv $csvPath | Set-OrchAsset }
        Clear-OrchCache

        Push-Location "${script:Drive1}:\Shared"
        $uv = @(Get-OrchAsset -Name $script:LargeAssetName -ExpandUserValues)
        Pop-Location
        $uv.Count | Should -Be $expectedCount
        "Created $expectedCount PerRobot values in $($elapsed.TotalSeconds.ToString('F1'))s"
    }

    It 'Updates a subset of PerRobot values without destroying others' {
        # Pick first 5 users and first 2 machines to update
        $updateUsers = $script:AllUsers | Select-Object -First 5
        $updateMachines = $script:AllMachines | Select-Object -First 2

        $csvPath = Join-Path $script:LargeCsvDir 'partial_update.csv'
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine('Path,Name,Description,ValueType,Value,UserName,MachineName')
        foreach ($user in $updateUsers) {
            foreach ($machine in $updateMachines) {
                [void]$sb.AppendLine("$($script:SharedPath),${script:LargeAssetName},,Text,UPDATED_${user}_${machine},${user},${machine}")
            }
        }
        [System.IO.File]::WriteAllText($csvPath, $sb.ToString())

        Import-Csv $csvPath | Set-OrchAsset
        Clear-OrchCache

        Push-Location "${script:Drive1}:\Shared"
        $uv = @(Get-OrchAsset -Name $script:LargeAssetName -ExpandUserValues)
        Pop-Location

        # Total count should be unchanged
        $totalExpected = $script:AllUsers.Count * $script:AllMachines.Count
        $uv.Count | Should -Be $totalExpected

        # Updated values should have new values
        $updatedUv = $uv | Where-Object { $_.Value -like 'UPDATED_*' }
        $updatedUv.Count | Should -Be ($updateUsers.Count * $updateMachines.Count)

        # Non-updated values should be unchanged (still start with 'val_')
        $unchangedUv = $uv | Where-Object { $_.Value -like 'val_*' }
        $unchangedUv.Count | Should -Be ($totalExpected - $updatedUv.Count)
    }

    It 'Deletes a subset of PerRobot values without destroying others' {
        # Pick last 3 users and last 1 machine to delete
        $deleteUsers = $script:AllUsers | Select-Object -Last 3
        $deleteMachines = $script:AllMachines | Select-Object -Last 1
        $deleteCount = $deleteUsers.Count * $deleteMachines.Count

        $csvPath = Join-Path $script:LargeCsvDir 'partial_delete.csv'
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine('Path,Name,Description,ValueType,Value,UserName,MachineName')
        foreach ($user in $deleteUsers) {
            foreach ($machine in $deleteMachines) {
                [void]$sb.AppendLine("$($script:SharedPath),${script:LargeAssetName},,Text,,${user},${machine}")
                # Value is empty → delete this entry
            }
        }
        [System.IO.File]::WriteAllText($csvPath, $sb.ToString())

        Import-Csv $csvPath | Set-OrchAsset
        Clear-OrchCache

        Push-Location "${script:Drive1}:\Shared"
        $uv = @(Get-OrchAsset -Name $script:LargeAssetName -ExpandUserValues)
        Pop-Location

        $totalExpected = ($script:AllUsers.Count * $script:AllMachines.Count) - $deleteCount
        $uv.Count | Should -Be $totalExpected

        # Deleted entries should not exist
        foreach ($user in $deleteUsers) {
            foreach ($machine in $deleteMachines) {
                $match = $uv | Where-Object { $_.UserName -eq $user -and $_.MachineName -eq $machine }
                $match | Should -BeNullOrEmpty
            }
        }
    }
}

Describe 'Folder Provider Operations (mkdir / rmdir)' {
    BeforeAll {
        $script:FolderName = "${script:Prefix}Folder"
        $script:SubFolderName = "${script:Prefix}SubFolder"
    }

    AfterAll {
        $parentPath = "${script:Drive1}:\${script:FolderName}"
        if (Test-Path $parentPath) {
            Remove-Item $parentPath -Recurse -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'New-Item creates a top-level folder' {
        $folder = New-Item -Path "${script:Drive1}:\${script:FolderName}" -ItemType Directory
        $folder | Should -Not -BeNullOrEmpty
        Clear-OrchCache
        $exists = Get-ChildItem "${script:Drive1}:\" | Where-Object { $_.DisplayName -eq $script:FolderName }
        $exists | Should -Not -BeNullOrEmpty
    }

    It 'New-Item creates a subfolder' {
        $sub = New-Item -Path "${script:Drive1}:\${script:FolderName}\${script:SubFolderName}" -ItemType Directory
        $sub | Should -Not -BeNullOrEmpty
        Clear-OrchCache
        $exists = Get-ChildItem "${script:Drive1}:\${script:FolderName}" | Where-Object { $_.DisplayName -eq $script:SubFolderName }
        $exists | Should -Not -BeNullOrEmpty
    }

    It 'Set-Location navigates into the subfolder' {
        { Set-Location "${script:Drive1}:\${script:FolderName}\${script:SubFolderName}" } | Should -Not -Throw
        Set-Location "${script:Drive1}:\Shared"
    }

    It 'Remove-Item removes the subfolder' {
        Remove-Item "${script:Drive1}:\${script:FolderName}\${script:SubFolderName}" -Recurse -Confirm:$false
        Clear-OrchCache
        $exists = Get-ChildItem "${script:Drive1}:\${script:FolderName}" -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -eq $script:SubFolderName }
        $exists | Should -BeNullOrEmpty
    }

    It 'Remove-Item removes the top-level folder' {
        Remove-Item "${script:Drive1}:\${script:FolderName}" -Recurse -Confirm:$false
        Clear-OrchCache
        $exists = Get-ChildItem "${script:Drive1}:\" | Where-Object { $_.DisplayName -eq $script:FolderName }
        $exists | Should -BeNullOrEmpty
    }
}

Describe 'CSV Export' {
    BeforeAll {
        $script:CsvDir = Join-Path $env:TEMP "PesterTest_CSV_$(Get-Random -Maximum 9999)"
        New-Item -Path $script:CsvDir -ItemType Directory -Force | Out-Null
    }

    AfterAll {
        Remove-Item $script:CsvDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Get-OrchMachine -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'machines.csv'
        Get-OrchMachine -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
        $header = Get-Content $csvPath -TotalCount 1
        $header | Should -Match 'Name'
    }

    It 'Get-OrchUser -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'users.csv'
        Get-OrchUser -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
        $header = Get-Content $csvPath -TotalCount 1
        $header | Should -Match 'UserName'
    }

    It 'Get-OrchQueue -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'queues.csv'
        Get-OrchQueue -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
        $header = Get-Content $csvPath -TotalCount 1
        $header | Should -Match 'Name'
    }

    It 'Get-OrchRole -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'roles.csv'
        Get-OrchRole -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
        $header = Get-Content $csvPath -TotalCount 1
        $header | Should -Match 'Name'
    }

    It 'Get-OrchAsset -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'assets.csv'
        Get-OrchAsset -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
    }

    It 'Get-OrchBucket -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'buckets.csv'
        Get-OrchBucket -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
    }

    It 'Get-OrchFolderUser -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'folderusers.csv'
        Get-OrchFolderUser -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
    }

    It 'Get-OrchFolderMachine -ExportCsv creates a CSV file' {
        $csvPath = Join-Path $script:CsvDir 'foldermachines.csv'
        Get-OrchFolderMachine -ExportCsv $csvPath
        Test-Path $csvPath | Should -BeTrue
    }
}

Describe 'Get Cmdlets - Additional Read-Only' {
    It 'Get-OrchQueue returns queues (or empty without error)' {
        { Get-OrchQueue } | Should -Not -Throw
    }

    It 'Get-OrchAsset returns assets (or empty without error)' {
        { Get-OrchAsset } | Should -Not -Throw
    }

    It 'Get-OrchBucket returns buckets (or empty without error)' {
        { Get-OrchBucket } | Should -Not -Throw
    }

    It 'Get-OrchProcess returns processes (or empty without error)' {
        { Get-OrchProcess } | Should -Not -Throw
    }

    It 'Get-OrchTrigger returns triggers (or empty without error)' {
        { Get-OrchTrigger } | Should -Not -Throw
    }

    It 'Get-OrchFolderUser returns folder users' {
        $users = Get-OrchFolderUser
        $users | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchFolderMachine returns folder machines (or empty without error)' {
        { Get-OrchFolderMachine } | Should -Not -Throw
    }

    It 'Get-OrchWebhook returns webhooks (or empty without error)' {
        { Get-OrchWebhook } | Should -Not -Throw
    }

    It 'Get-OrchCalendar returns calendars (or empty without error)' {
        { Get-OrchCalendar } | Should -Not -Throw
    }

    It 'Get-OrchLibrary returns libraries (or empty without error)' {
        { Get-OrchLibrary } | Should -Not -Throw
    }

    It 'Get-OrchPackage returns packages (or empty without error)' {
        { Get-OrchPackage } | Should -Not -Throw
    }

    It 'Get-OrchCredentialStore returns credential stores' {
        $stores = Get-OrchCredentialStore
        $stores | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchLicense returns license info' {
        $license = Get-OrchLicense
        $license | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchConfigPath returns a path string' {
        $path = Get-OrchConfigPath
        $path | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchPSDrive returns connected drives' {
        $drives = Get-OrchPSDrive
        $drives | Should -Not -BeNullOrEmpty
    }
}

Describe 'Error Handling' {
    It 'Get-OrchMachine -Name for non-existent returns empty' {
        $result = Get-OrchMachine -Name 'NonExistent_99999'
        $result | Should -BeNullOrEmpty
    }

    It 'Remove-OrchMachine for non-existent does not throw' {
        { Remove-OrchMachine -Name 'NonExistent_99999' -Confirm:$false } | Should -Not -Throw
    }

    It 'New-OrchQueue with duplicate name writes an error' {
        $dupName = "${script:Prefix}DupQueue"
        New-OrchQueue -Name $dupName
        Clear-OrchCache
        $err = $null
        New-OrchQueue -Name $dupName -ErrorVariable err -ErrorAction SilentlyContinue
        $err | Should -Not -BeNullOrEmpty
        Remove-OrchQueue -Name $dupName -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Set-Location to non-existent folder writes an error' {
        { Set-Location "${script:Drive1}:\NonExistent_Folder_99999" -ErrorAction Stop } | Should -Throw
    }
}

Describe 'Wildcard Support' {
    BeforeAll {
        $script:WcMachineA = "${script:Prefix}WcA"
        $script:WcMachineB = "${script:Prefix}WcB"
        New-OrchMachine -Name $script:WcMachineA -Description 'Wildcard A'
        New-OrchMachine -Name $script:WcMachineB -Description 'Wildcard B'
        Clear-OrchCache
    }

    AfterAll {
        Remove-OrchMachine -Name "${script:Prefix}Wc*" -Confirm:$false -ErrorAction SilentlyContinue
    }

    It 'Get-OrchMachine with * wildcard returns matching machines' {
        $machines = Get-OrchMachine -Name "${script:Prefix}Wc*"
        $machines.Count | Should -Be 2
    }

    It 'Get-OrchMachine with ? wildcard matches single character' {
        $machines = Get-OrchMachine -Name "${script:Prefix}Wc?"
        $machines.Count | Should -Be 2
    }

    It 'Remove-OrchMachine with wildcard removes matching machines' {
        Remove-OrchMachine -Name "${script:Prefix}Wc*" -Confirm:$false
        Clear-OrchCache
        $machines = Get-OrchMachine -Name "${script:Prefix}Wc*"
        $machines | Should -BeNullOrEmpty
    }
}

Describe 'Cross-Tenant Operations' {
    BeforeAll {
        $script:CrossMachineName = "${script:Prefix}CrossMachine"
        $script:CrossQueueName = "${script:Prefix}CrossQueue"
        $script:CrossAssetName = "${script:Prefix}CrossAsset"
        $script:CrossBucketName = "${script:Prefix}CrossBucket"
        $script:Orch1Shared = "${script:Drive1}:\Shared"
        $script:Orch2Shared = "${script:Drive2}:\Shared"
    }

    AfterAll {
        foreach ($drv in @($script:Drive1, $script:Drive2)) {
            Remove-OrchMachine -Name $script:CrossMachineName -Path "${drv}:\" -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchQueue -Name $script:CrossQueueName -Path "${drv}:\Shared" -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchAsset -Name $script:CrossAssetName -Path "${drv}:\Shared" -Confirm:$false -ErrorAction SilentlyContinue
            Remove-OrchBucket -Name $script:CrossBucketName -Path "${drv}:\Shared" -Confirm:$false -ErrorAction SilentlyContinue
        }
    }

    It 'Get-OrchMachine works across multiple drives with -Path' {
        $machines1 = Get-OrchMachine -Path "${script:Drive1}:\"
        $machines2 = Get-OrchMachine -Path "${script:Drive2}:\"
        $machines1 | Should -Not -BeNullOrEmpty
        $machines2 | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchUser works across multiple drives with -Path' {
        $users1 = Get-OrchUser -Path "${script:Drive1}:\"
        $users2 = Get-OrchUser -Path "${script:Drive2}:\"
        $users1 | Should -Not -BeNullOrEmpty
        $users2 | Should -Not -BeNullOrEmpty
    }

    It 'Copy-OrchMachine copies a machine from Orch1 to Orch2' {
        New-OrchMachine -Name $script:CrossMachineName -Description 'Cross-tenant test' -Path "${script:Drive1}:\"
        Clear-OrchCache
        Copy-OrchMachine -Name $script:CrossMachineName -Destination "${script:Drive2}:\" -Path "${script:Drive1}:\"
        Clear-OrchCache
        $copied = Get-OrchMachine -Name $script:CrossMachineName -Path "${script:Drive2}:\"
        $copied | Should -Not -BeNullOrEmpty
        $copied.Description | Should -Be 'Cross-tenant test'
    }

    It 'Copy-OrchQueue copies a queue from Orch1:\Shared to Orch2:\Shared' {
        New-OrchQueue -Name $script:CrossQueueName -Description 'Cross-tenant queue' -Path $script:Orch1Shared
        Clear-OrchCache
        Copy-OrchQueue -Name $script:CrossQueueName -Destination $script:Orch2Shared -Path $script:Orch1Shared
        Clear-OrchCache
        $copied = Get-OrchQueue -Name $script:CrossQueueName -Path $script:Orch2Shared
        $copied | Should -Not -BeNullOrEmpty
        $copied.Description | Should -Be 'Cross-tenant queue'
    }

    It 'Copy-OrchAsset copies an asset from Orch1:\Shared to Orch2:\Shared' {
        Set-OrchAsset -ValueType Text -Name $script:CrossAssetName -Value 'cross-test' -Path $script:Orch1Shared
        Clear-OrchCache
        Copy-OrchAsset -Name $script:CrossAssetName -Destination $script:Orch2Shared -Path $script:Orch1Shared
        Clear-OrchCache
        $copied = Get-OrchAsset -Name $script:CrossAssetName -Path $script:Orch2Shared
        $copied | Should -Not -BeNullOrEmpty
        $copied.Value | Should -Be 'cross-test'
    }

    It 'Copy-OrchBucket copies a bucket from Orch1:\Shared to Orch2:\Shared' {
        New-OrchBucket -Name $script:CrossBucketName -Description 'Cross-tenant bucket' -Path $script:Orch1Shared
        Clear-OrchCache
        Copy-OrchBucket -Name $script:CrossBucketName -Destination $script:Orch2Shared -Path $script:Orch1Shared
        Clear-OrchCache
        $copied = Get-OrchBucket -Name $script:CrossBucketName -Path $script:Orch2Shared
        $copied | Should -Not -BeNullOrEmpty
        $copied.Description | Should -Be 'Cross-tenant bucket'
    }

    It 'Get-ChildItem lists folders on both drives' {
        $folders1 = Get-ChildItem "${script:Drive1}:\"
        $folders2 = Get-ChildItem "${script:Drive2}:\"
        $folders1 | Should -Not -BeNullOrEmpty
        $folders2 | Should -Not -BeNullOrEmpty
    }

    It 'Get-OrchPSDrive lists all connected drives' {
        $drives = Get-OrchPSDrive
        $drives | Should -Not -BeNullOrEmpty
        $driveNames = $drives | ForEach-Object { $_.Name }
        $driveNames | Should -Contain $script:Drive1
        $driveNames | Should -Contain $script:Drive2
    }
}

# ============================================================================
# Tab Completion Smoke Tests (ParallelResults3 migration)
# ============================================================================

Describe 'Tab Completion - ParallelResults3 migration' {
    BeforeAll {
        function Complete-Parameter {
            param([string]$InputScript)
            $result = [System.Management.Automation.CommandCompletion]::CompleteInput(
                $InputScript, $InputScript.Length, $null)
            return $result.CompletionMatches
        }
    }

    It 'Update-OrchTrigger -MachineRobots completes without error' {
        $results = Complete-Parameter 'Update-OrchTrigger -Name * -MachineRobots '
        # MachineRobots completer may return 0 candidates if no triggers have MachineRobots,
        # but it must not throw.
        $results | Should -Not -Be $null
    }

    It 'New-OrchTrigger -MachineRobots completes without error' {
        $results = Complete-Parameter 'New-OrchTrigger -Name test -ReleaseName test -MachineRobots '
        $results | Should -Not -Be $null
    }

    It 'Add-PmGroupMember -UserName completes without error' {
        $results = Complete-Parameter 'Add-PmGroupMember -PmGroup * -Type DirectoryUser -UserName '
        $results | Should -Not -Be $null
    }
}

# ============================================================================
# IdentityUrl auto-generation from Root
# ============================================================================

Describe 'IdentityUrl is derived from Root' {
    BeforeAll {
        $script:allDrives = Get-OrchPSDrive
    }

    It 'Cloud drive IdentityUrl is {org}/identity_' {
        # Cloud: Root = https://{host}/{org}/{tenant} → IdentityUrl = https://{host}/{org}/identity_
        $cloudDrives = $script:allDrives | Where-Object { $_.Root -match 'uipath\.com' }
        $cloudDrives | Should -Not -BeNullOrEmpty -Because 'at least one cloud drive should be connected'

        foreach ($d in $cloudDrives) {
            $rootTrimmed = $d.Root.TrimEnd('/')
            $orgBase = $rootTrimmed.Substring(0, $rootTrimmed.LastIndexOf('/'))
            $expected = "$orgBase/identity_"
            $d.IdentityUrl | Should -Be $expected -Because "drive '$($d.Name)' Root=$($d.Root)"
        }
    }

    It 'On-prem drive IdentityUrl is {authority}/identity' {
        # On-prem: Root = https://{host}/{tenant} or https://{host} → IdentityUrl = https://{host}/identity
        $onpremDrives = $script:allDrives | Where-Object { $_.Root -notmatch 'uipath\.com' }
        if (-not $onpremDrives) { Set-ItResult -Skipped -Because 'no on-prem drives connected'; return }

        foreach ($d in $onpremDrives) {
            $uri = [Uri]$d.Root.TrimEnd('/')
            $expected = "$($uri.Scheme)://$($uri.Authority)/identity"
            $d.IdentityUrl | Should -Be $expected -Because "drive '$($d.Name)' Root=$($d.Root)"
        }
    }

    It 'IdentityUrl is not null or empty for any drive' {
        foreach ($d in $script:allDrives) {
            $d.IdentityUrl | Should -Not -BeNullOrEmpty -Because "drive '$($d.Name)' should have IdentityUrl"
        }
    }
}
