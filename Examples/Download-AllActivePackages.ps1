# If you want to download all tenant packages, including inactive ones, simply use `Export-OrchPackage -Path Orch1:\`.
# This downloads the package files to the current location on the C: drive.
# To download all tenant packages and all folder packages, use `Export-OrchPackage -Path Orch1:\ -Recurse`.
# 
# To download all active process packages only from the Orch1: drive, use this script.
# To open the current location on the C: drive in File Explorer, please execute `ii c:`.


# Get all package versions
$packages = Get-OrchPackageVersion -Recurse -Path Orch1:\

# Filter for active packages
$activePackages = $packages | Where-Object { $_.IsActive -eq $true }

# Loop through all active packages and download them
foreach ($package in $activePackages) {
    Export-OrchPackage -Id $package.Id -Version $package.Version -Path $package.Path -Verbose
}
