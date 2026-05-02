# Monitor jobs in Orch1:\Shared every 15 minutes and notify via Outlook
# whenever any job is in the Suspended state. Notifications are sent
# every cycle for as long as a job remains suspended.
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

        $jobs = Get-OrchJob -Path Orch1:\Shared -State Suspended

        if ($jobs) {
            Write-TimestampedMessage "Found $($jobs.Count) suspended job(s). Sending notification..."
            Send-Notification `
                -Jobs $jobs `
                -Subject 'UiPathOrch Alert: Suspended job(s) detected' `
                -BodyPrefix 'The following job(s) are currently suspended:'
        }
        else {
            Write-TimestampedMessage 'No suspended jobs.'
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
