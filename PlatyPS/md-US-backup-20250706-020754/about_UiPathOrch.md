# UiPathOrch
## about_UiPathOrch

# SHORT DESCRIPTION
UiPathOrch is a PowerShell provider for managing UiPath Orchestrator entities via cmdlets.

# LONG DESCRIPTION
UiPathOrch mounts multiple Orchestrator tenants as PSDrives, enabling you to navigate their folders using familiar commands such as dir, cd, mkdir, rmdir, ren, and copy. It manages a wide range of Orchestrator entities, including libraries, packages, processes, jobs, assets, queues, triggers, robots, machines, users, roles, and many others. This allows for comprehensive management of Orchestrator through command line and scripting.

## Operate multi-tenants simultaneously
You can mount multiple tenants as PSDrives simultaneously. Use `Edit-OrchConfig` to open the configuration file to add multiple tenant connections. To confirm the available PSDrives, use `Get-PSDrive` or `Get-OrchPSDrive`.

## Available cmdlets
Use `Get-Command -Module UiPathOrch` to list all cmdlets in the module. The nouns in the names of all cmdlets start with `Orch`, `Tm` (Test Manager), `Du` (Document Understanding), or `Pm` (Platform Management).

## Target multi-folders simultaneously
Specify the target tenants and folders with the -Path, -Recurse and -Depth parameters. To ensure proper functioning of the autocomplete for subsequent parameters, specify these parameters first on each cmdlet. You can specify a comma-separated path string that includes wildcards for the -Path parameter. If these parameters are not specified, the current folder will be the target of the operation.

## Autocomplete function
The cmdlet names, parameter names, and parameter values can be auto-completed by pressing [Ctrl+Space] or [Tab]. To make this feature work optimally, import the PSReadLine module and ensure proper parameter ordering.

## Operate multi-entities simultaneously
You can specify multiple entities' names with wildcards and commas in parameter values. Many cmdlets support bulk operations for efficient management.

## Entity types
The module handles two main types of entities:
- **Tenant entities**: Operate across the entire tenant (webhooks, roles, machines, users, libraries)
- **Folder entities**: Operate within specific folders (processes, assets, queues, triggers, robots)

## Caching mechanism
The module implements intelligent caching to improve performance. Use `Clear-OrchCache` to refresh cached data when needed.

## Upgrade the module
Run `Update-Module UiPathOrch` regularly to download the latest version of the module with new features and improvements.

# EXAMPLES
## Example 1
`powershell
PS Orch1:\> cd Shared
`
The `cd` command (`Set-Location` cmdlet) changes the current location. [Ctrl+Space] displays a list of navigable folder names.

## Example 2
`powershell
PS Orch1:\Shared> cd ..
`
The `..` represents the parent folder. The `.` represents the current folder.

## Example 3
`powershell
PS Orch1:\> dir -Recurse
`
The `-Recurse` parameter specifies that the current folder and all its subfolders are to be targeted. Almost all cmdlets included in this module accept the `-Recurse` parameter.

## Example 4
`powershell
PS Orch1:\> Get-OrchProcess -Recurse -Name *Invoice*
`
Gets all processes with names containing "Invoice" from the current folder and all subfolders.

## Example 5
`powershell
PS C:\> Get-OrchPSDrive
`
Displays all available Orchestrator PSDrives and their connection status.

## Example 6
`powershell
PS Orch1:\> Get-OrchUser | Export-Csv users.csv -NoTypeInformation
`
Exports all users from the current tenant to a CSV file.

# NOTE
For comprehensive documentation, use `Get-OrchHelp` to display module documentation and quick start guide. Essential documentation is available in the module's Docs folder.

Parameter ordering is important for folder entities - specify -Path, -Recurse, and -Depth parameters before other parameters to enable proper auto-completion.

# TROUBLESHOOTING NOTE
## Changes to entities made via the Orchestrator web interface or other external applications are not reflected
Execute the `Clear-OrchCache` cmdlet to clear the cache that this module holds in memory.

## Permission errors
Ensure your Orchestrator user account has appropriate permissions for the entities you're trying to access. Some operations require specific folder permissions or tenant-level permissions.

## Connection issues
Use `Get-OrchCurrentUser` to verify your connection status. If connection issues persist, review your configuration with `Edit-OrchConfig`.

# SEE ALSO
- Get-OrchHelp
- Get-OrchPSDrive  
- Get-OrchCurrentUser
- Clear-OrchCache
- Edit-OrchConfig

# KEYWORDS
UiPath, Orchestrator, PowerShell, Provider, Automation, RPA
