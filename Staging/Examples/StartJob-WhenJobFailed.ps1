# Start a specified process when any job fails in the current folder

# HashSet to store the IDs of jobs that have already been processed
$processedJobIds = [System.Collections.Generic.HashSet[string]]::new()

# Store the script start time
$startTime = Get-Date -Format "yyyy/MM/dd HH:mm"
Write-Host "Script started at $startTime."

while ($true) {
    Write-Host "Retrieving Jobs from Orchestrator..."

    try {
        # You can specify the folders you want to monitor with the Path parameter
        $jobs = Get-OrchJob -State Faulted -CreationTimeAfter $startTime

        # Filter jobs that are faulted and not yet processed
        $faultedJobs = $jobs | Where-Object {
            -not $processedJobIds.Contains($_.Id)
        }

        if ($faultedJobs) {
            Write-Host "Starting processes for faulted job(s)..."
            $faultedJobs | ForEach-Object {
                try {
                    $inputArguments = @{
                        argument1 = $_.Id
                    } | ConvertTo-Json -Compress

                    Start-OrchJob "YourProcessName" -InputArguments $inputArguments # Change "YourProcessName" to the name of the process you want to start
                    $null = $processedJobIds.Add($_.Id)
                    Write-Host "Started process for job ID: $($_.Id)"
                } catch {
                    Write-Host "Failed to start process for job ID: $($_.Id). Error: $($_.Exception.Message)"
                }
            }
        } else {
            Write-Host "No faulted jobs to process."
        }

        Write-Host "Waiting for 15 minutes before the next check..."
        Start-Sleep -Seconds 900
    } catch {
        Write-Host "An error occurred: $($_.Exception.Message)"
    }
}
