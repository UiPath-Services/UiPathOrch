# Monitor jobs across the specified Orch drives every 15 minutes and
# notify via Outlook if any job has been Pending for more than 15 minutes.
#
# Notifications are sent every cycle for as long as a job remains stuck.
# Re-emailing avoids "miss the first mail, miss it forever"; production
# users can layer their own dedup logic if needed.
#
# Outlook COM is Windows-only and requires a configured Outlook profile.
# Adapt Send-Notification for Teams / Slack webhook / SMTP as needed.

$outlook = New-Object -ComObject Outlook.Application

function Write-TimestampedMessage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Write-Host "[$timestamp] $Message"
}

function Send-Notification {
    param($Jobs, $Subject, $BodyPrefix)
    $mail = $outlook.CreateItem(0)
    $mail.To = 'you@example.com'
    $mail.Subject = $Subject
    $mail.Body = "$BodyPrefix`n" + ($Jobs | Out-String)
    $mail.Send()
}

try {
    while ($true) {
        Write-TimestampedMessage 'Retrieving Jobs from Orchestrator...'

        # Adjust -Path / -Recurse to match the folders you want to monitor.
        $jobs = Get-OrchJob -Path Orch1:\,Orch2:\ -Recurse -State Pending

        # "Pending for more than 15 minutes" — based on the job's creation time.
        $stuckJobs = $jobs | Where-Object {
            $_.CreationTime.ToLocalTime() -le (Get-Date).AddMinutes(-15)
        }

        if ($stuckJobs) {
            Write-TimestampedMessage "Found $($stuckJobs.Count) stuck job(s). Sending notification..."
            Send-Notification `
                -Jobs $stuckJobs `
                -Subject 'UiPathOrch Alert: Stuck job(s) detected' `
                -BodyPrefix 'The following job(s) have been pending for more than 15 minutes:'
        }
        else {
            Write-TimestampedMessage 'No stuck jobs.'
        }

        Write-TimestampedMessage 'Waiting 15 minutes before the next check...'
        Start-Sleep -Seconds 900
    }
}
catch {
    Write-TimestampedMessage "An error occurred: $($_.Exception.Message)"
}
finally {
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($outlook) | Out-Null
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
