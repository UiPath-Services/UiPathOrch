# This script retrieves a list of the installed robot versions.
# To ensure ease of use when outputting to a CSV,
# each version is separated by a semicolon.
# 
# The robot versions are stored in the Version property, nested within
# each element of the RobotVersions array property.
# The script includes an Expression for extracting this information.
# 
# To inspect this nested structure, execute Get-OrchMachine | ConvertTo-Json.


Get-OrchMachine -Path Orch1: | select Name, @{Name='Version';Expression={$_.RobotVersions.Version -join '; '}}
