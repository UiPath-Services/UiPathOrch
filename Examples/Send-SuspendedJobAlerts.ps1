# Monitor the jobs in Orch1:\Shared every 15 minutes, and
# if there are any jobs that have been Suspended for more than 1 hour, notify via email.

$outlook = New-Object -ComObject Outlook.Application

function Write-TimestampedMessage {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message"
}

# Function to send email notifications for stuck jobs
function Send-Notification {
    param($Jobs, $Subject, $BodyPrefix)
    $mail = $outlook.CreateItem(0)
    $mail.To = "you@example.com"
    $mail.Subject = $Subject
    $mail.Body = "$BodyPrefix`n" + ($Jobs | Out-String)
    $mail.Send()
}

# Dictionary to store the time when each job was found Suspended
$suspendedJobs = @{}

try {
    while ($true) {
        Write-TimestampedMessage "Retrieving Jobs from Orchestrator..."

        # Retrieve jobs in Suspended state
        $jobs = Get-OrchJob -Path Orch1:\Shared -State Suspended

        # Update dictionary with current Suspended jobs
        foreach ($job in $jobs) {
            $key = ($job.Path, $job.Id)
            if (-not $suspendedJobs.ContainsKey($key)) {
                $suspendedJobs[$key] = Get-Date
            }
        }

        # Notify for jobs that have been Suspended for over an hour
        $jobsToNotify = $suspendedJobs.GetEnumerator() | Where-Object {
             ($_.Value).AddHours(1) -le (Get-Date)
        }

        if ($jobsToNotify) {
            Send-Notification -Jobs $jobsToNotify -Subject "Notification: Jobs Suspended Over An Hour" -BodyPrefix "The following jobs have been suspended for more than an hour:"
        }

        # Check all jobs and remove any that are no longer Suspended
        $currentJobKeys = $jobs | ForEach-Object { ($_.Path, $_.Id) }
        $suspendedJobs.Keys | Where-Object { $_ -notin $currentJobKeys } | ForEach-Object {
            $suspendedJobs.Remove($_)
        }

        Write-TimestampedMessage "Waiting for 15 minutes before the next check..."
        Start-Sleep -Seconds 900
    }
} catch {
    Write-TimestampedMessage "An error occurred: $($_.Exception.Message)"
} finally {
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($outlook) | Out-Null
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
