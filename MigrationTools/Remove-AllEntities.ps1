#### Remove-AllEntities.ps1

#### CAUTION: This script is not intended for regular use by end users.
#### This script is designed to remove all entities from a specified tenant, primarily for testing purpose.
#### It supports the development and testing of 'Migrate-Tenant.ps1', which is used to copy all entities between tenants.
#### While end users can use this script if necessary, it is typically not required for normal operations.

$drive = Read-Host "Please enter the drive name"

# Check if the specified drive exists
$driveRoot = "${drive}:\"
if (-not (Test-Path $driveRoot)) {
    Write-Host "The specified drive does not exist." -ForegroundColor Red
    exit 1
}

# Extract provider name from the full path
$provider = Get-PSDrive -Name $drive -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Provider | Select-Object -ExpandProperty Name

if ($provider -notmatch 'UiPathOrch') {
    Write-Host "The drive $drive is not a UiPathOrch provider drive." -ForegroundColor Red
    exit 1
}

$response = Read-Host "Are you sure you want to remove all entities on ${drive}:? (Yes/No)"

if ($response -ne "Yes") {
    Write-Host "Operation cancelled." -ForegroundColor Red
    exit 1
}

# Navigate to the specified drive
pushd $driveRoot

# Confirm and remove all directories and files in the current directory
rmdir * -Recurse -Confirm:$false -Force

# Remove various Orchestrator entities
Remove-OrchLibrary * * -Verbose
Remove-OrchPackage * * -Verbose
Remove-OrchCalendar * -Verbose
Remove-OrchCredentialStore * -Verbose
Remove-OrchRole * -Verbose
Remove-OrchUser * -Verbose
Remove-OrchMachine * -Verbose
Remove-OrchWebhook * -Verbose

# Return to the previous directory
popd
