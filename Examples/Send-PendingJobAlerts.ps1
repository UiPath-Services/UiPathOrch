# Monitor the jobs in all folders of Orch1: Drive and Orch2: Drive every 15 minutes, and
# if there are any jobs that have been Pending for more than 15 minutes, notify via email.

# Create an Outlook instance outside the loop to be reused for sending emails
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

# HashSet to store the IDs of jobs that have already been notified
$notifiedJobIds = [System.Collections.Generic.HashSet[string]]::new()

try {
    while ($true) {
        Write-TimestampedMessage "Retrieving Jobs from Orchestrator..."

        # You can specify the folders you want to monitor with the Path parameter
        $jobs = Get-OrchJob -Path Orch1:\,Orch2:\ -Recurse -State Pending

        if ($jobs) {
            Write-TimestampedMessage "Current pending job(s):`n$($jobs | Out-String)"
        } else {
            Write-TimestampedMessage "No pending jobs found."
        }

        # Filter jobs pending for over 15 minutes and not yet notified
        $stuckJobs = $jobs | Where-Object {
            $_.CreationTime.ToLocalTime() -le (Get-Date).AddMinutes(-15) -and
            -not $notifiedJobIds.Contains($_.Path + '\' + $_.Id)
        }

        if ($stuckJobs) {
            Write-TimestampedMessage "Job(s) have been pending for over 15 minutes. Preparing to send notification email..."
            Send-Notification -Jobs $stuckJobs -Subject "UiPathOrch Alert: Stuck job(s) detected!" -BodyPrefix "The following job(s) have been pending for more than 15 minutes:"
            $stuckJobs | ForEach-Object { $null = $notifiedJobIds.Add($_.Path + '\' + $_.Id) }
            Write-TimestampedMessage "Notification email sent."
        }

        # Display the IDs of jobs that have been notified
        if ($notifiedJobIds.Count -gt 0) {
            $sortedNotifiedJobs = $notifiedJobIds | Sort-Object
            $notifiedJobsString = $sortedNotifiedJobs -join ', '
            Write-TimestampedMessage "Job(s) notified so far: $notifiedJobsString"
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
