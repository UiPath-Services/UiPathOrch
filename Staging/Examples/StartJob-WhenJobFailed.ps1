# Monitor the current folder every 15 minutes for jobs that have entered
# the Faulted state since the script started, and start a recovery process
# for each one.
#
# A HashSet tracks the IDs already acted on so the recovery process is
# launched at most once per faulted job (unlike the alert scripts in this
# folder, where re-emailing every cycle is acceptable, starting a job
# multiple times is not).

# Set this to the process you want to start in response to a faulted job.
$RecoveryProcessName = 'YourProcessName'

$startTime = Get-Date
$processedJobIds = [System.Collections.Generic.HashSet[string]]::new()

Write-Host "Script started at $($startTime.ToString('yyyy-MM-dd HH:mm'))."

while ($true) {
    Write-Host 'Retrieving Jobs from Orchestrator...'

    try {
        # Adjust -Path / -Recurse to match the folders you want to monitor.
        $jobs = Get-OrchJob -State Faulted -CreationTimeAfter $startTime

        $faultedJobs = $jobs | Where-Object {
            -not $processedJobIds.Contains($_.Id.ToString())
        }

        if ($faultedJobs) {
            Write-Host "Starting recovery process for $($faultedJobs.Count) faulted job(s)..."
            foreach ($job in $faultedJobs) {
                try {
                    $inputArguments = @{ argument1 = $job.Id } | ConvertTo-Json -Compress
                    Start-OrchJob $RecoveryProcessName -InputArguments $inputArguments
                    $null = $processedJobIds.Add($job.Id.ToString())
                    Write-Host "Started recovery for job ID: $($job.Id)"
                }
                catch {
                    Write-Host "Failed to start recovery for job ID $($job.Id): $($_.Exception.Message)"
                }
            }
        }
        else {
            Write-Host 'No new faulted jobs.'
        }

        Write-Host 'Waiting 15 minutes before the next check...'
        Start-Sleep -Seconds 900
    }
    catch {
        Write-Host "An error occurred: $($_.Exception.Message)"
    }
}
