#### Migrate-Tenant.ps1

#### This script migrates all entities from the source tenant drive to the destination tenant drive.
#### Please modify the following variables according to your environment before running this script.

$srcDrive = "Orch1:"
$dstDrive = "Orch2:"

$VerboseLogPath = "c:migrate_verbose.log"
$WarningLogPath = "c:migrate_warning.log"
$ErrorLogPath = "c:migrate_error.log"

#######################################################################################


#### Set the error view to minimize script line information
$ErrorView = 'CategoryView'


#### Validate that source and destination drives are accessible
if (-not (Test-Path $srcDrive)) {
    Write-Host "Source drive $srcDrive is not accessible." -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $dstDrive)) {
    Write-Host "Destination drive $dstDrive is not accessible." -ForegroundColor Red
    exit 1
}


#### Ask the user if they have started exploring the personal workspace folders

Write-Host "To copy the contents of the personal workspace folders, you need to start their exploration in the Orchestrator web interface. Have you started exploring all the personal workspace folders on both the $srcDrive and the $dstDrive drive?" -ForegroundColor Yellow

$response = Read-Host "Please answer Yes or No"

if ($response -ne "Yes") {
    Write-Host "The script will now exit because the exploration has not been started." -ForegroundColor Red
    exit
}


#### Executes a script block and logs its output to specified files.

function SafeCopy {
    param (
        [scriptblock]$ScriptBlock
    )

    # Convert the script block to a string for logging
    $scriptBlockText = $ScriptBlock.ToString()

    # Expand variables manually within the script block text
    $expandedScriptBlockText = $ExecutionContext.InvokeCommand.ExpandString($scriptBlockText)

    # Log the script block text to the console and all logs
    "`n$expandedScriptBlockText" | Out-File -FilePath $VerboseLogPath -Append
    "`n$expandedScriptBlockText" | Out-File -FilePath $WarningLogPath -Append
    "`n$expandedScriptBlockText" | Out-File -FilePath $ErrorLogPath -Append
    Write-Host "`nExecuting:$expandedScriptBlockText"

    # Redirect streams and execute the script block
    try {
        & $ScriptBlock -Verbose 4>&1 3>&1 2>&1 | ForEach-Object {
            $message = $_.ToString()
            switch ($_.GetType().Name) {
                'ErrorRecord' { $message | Tee-Object -FilePath $ErrorLogPath -Append | Write-Host -ForegroundColor Red }
                'WarningRecord' { $message | Tee-Object -FilePath $WarningLogPath -Append | Write-Host -ForegroundColor Yellow }
                Default { $message | Out-File -FilePath $VerboseLogPath -Append }
            }
        }
    } catch {
        $_.Exception.Message | Tee-Object -FilePath $ErrorLogPath -Append | Write-Host -ForegroundColor Red
        exit
    }
}


#### Migrating started! ######################################


#### Clear on-memory cache of UiPathOrch

Clear-OrchCache $srcDrive,$dstDrive


#### Remove log files

Remove-Item $VerboseLogPath -Force -ErrorAction Ignore
Remove-Item $WarningLogPath -Force -ErrorAction Ignore
Remove-Item $ErrorLogPath -Force -ErrorAction Ignore


#### Copying All Tenant Entities

SafeCopy -ScriptBlock { Copy-OrchLibrary -Path $srcDrive * * $dstDrive -Verbose }
SafeCopy -ScriptBlock { Copy-OrchPackage -Path $srcDrive * * $dstDrive -Verbose }
SafeCopy -ScriptBlock { Copy-OrchCredentialStore -Path $srcDrive * $dstDrive -Verbose }
SafeCopy -ScriptBlock { Copy-OrchRole -Path $srcDrive * $dstDrive -Verbose }
SafeCopy -ScriptBlock { Copy-OrchUser -Path $srcDrive * $dstDrive -Verbose }
SafeCopy -ScriptBlock { Copy-OrchMachine -Path $srcDrive * $dstDrive -Verbose }
SafeCopy -ScriptBlock { Copy-OrchCalendar -Path $srcDrive * $dstDrive -Verbose }
SafeCopy -ScriptBlock { Copy-OrchWebhook -Path $srcDrive * $dstDrive -Verbose }


#### Copying All Folder Entities

SafeCopy -ScriptBlock { copy "$srcDrive\*" "$dstDrive\" -Recurse -Verbose }


"`nDone!" | Out-File -FilePath $VerboseLogPath -Append
"`nDone!" | Out-File -FilePath $WarningLogPath -Append
"`nDone!" | Out-File -FilePath $ErrorLogPath -Append
Write-Host "`nDone!`n"


#### Migrating completed! ######################################


#### Following cmdlets are optional.
#### If an error occurred, you can fix the root cause and run it to copy the entities partially.
#### Instead of specifying the -Path parameter, you can also move to that srcFolder using the cd command and then execute the cmdlet.

<#
Copy-OrchProcess       [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchFolderUser    [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchFolderMachine [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchAsset         [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchTrigger       [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchApiTrigger    [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchQueue         [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchTestSet       [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchTestSchedule  [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchTestDataQueue [-Path srcFolder] * [[-Destination] dstFolder]
Copy-OrchBucket        [-Path srcFolder] * [[-Destination] dstFolder]
#>
