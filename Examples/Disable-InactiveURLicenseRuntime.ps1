$drive = "Orch1:"

# Define the inactive period (3 months)
$inactivePeriod = (Get-Date).AddMonths(-3)

# Get all users and their last login times
$users = Get-OrchUser -Path $drive | Select-Object @{Name="UserName";Expression={$_.UnattendedRobot.UserName}}, LastLoginTime

# Filter out users who haven't logged in for more than the inactive period
$inactiveUsers = $users | Where-Object {
    $_.LastLoginTime -ne $null -and [datetime]$_.LastLoginTime -lt $inactivePeriod
}

Write-Host "Users inactive for more than 3 months:"
foreach ($user in $inactiveUsers) {
    Write-Host "User: $($user.UserName), LastLoginTime: $($user.LastLoginTime)"
}

# Get all runtime licenses
$licenses = Get-OrchLicenseRuntime -Path $drive Unattended

# Loop through each inactive user and disable their license
foreach ($user in $inactiveUsers) {
    if ($user -ne $null -and $user.UserName -ne $null) {
        $userName = $user.UserName.ToLower()

        # Match userName with ServiceUserName ignoring case and allowing partial match
        $userLicenses = $licenses | Where-Object { $_.ServiceUserName.ToLower() -like "*$userName*" }

        foreach ($license in $userLicenses) {
            Disable-OrchLicenseRuntime -Path $drive Unattended $license.Key -Verbose
        }
    }
}
