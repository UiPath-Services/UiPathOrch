# UiPathOrch
## about_UiPathOrch

# SHORT DESCRIPTION
UiPathOrch is a PowerShell provider for managing UiPath Orchestrator entities via cmdlets.

# LONG DESCRIPTION
UiPathOrch mounts multiple Orchestrator tenants as PSDrives, enabling you to navigate their folders using familiar commands such as dir, cd, mkdir, rmdir, ren, and copy. It manages a wide range of Orchestrator entities, including libraries, packages, processes, jobs, assets, queues, triggers, among others. This allows for easy management of Orchestrator through command line and scripting.

## Operate multi-tenants simultaenously
You can mount multiple tenants as PSDrives simultaneously. Use `Edit-OrchConfig` to open the config file. To confirm the PSDrives, use `Get-PSDrive`.

## Available cmdlets
Use `Get-Command -Module UiPathOrch` to list all cmdlets in the module. The nouns in the names of all cmdlets start with `Orch`.

## Target multi-folders simultaneously
Specify the target tenants and folders with the -Path, -Recurse and -Depth parameters. To ensure proper functioning of the autocomplete for subsequent parameters, specify these parameters first on each cmdlet. Please specify a comma-separated path string that includes wildcards for the -Path parameter. If these parameters are not specified, the current folder will be the target of the operation.

## Autocomplete function
The cmdlets names, parameter names, and parameter values can be auto-completed with pressing [Ctrl+Space] or [Tab]. To make this feature work, import the PSReadLine module.

## Operate multi-entities simultaneously
You can specify multiple entities' names with wildcards and commas in parameter values.

## Upgrade the module
Please run `Update-Module UiPathOrch` occasionally to download the latest version of the module.

# EXAMPLES
## Example 1
```powershell
PS Orch1:\> cd Shared
```
The `cd` command (`Set-Location` cmdlet) changes the current location. [Ctrl+Space] displays a list of navigable folder names.

## Example 2
```powershell
PS Orch1:\Shared> cd ..
```
The `..` represents the parent folder. The `.` represents the current folder.

## Example 3
```powershell
PS Orch1:\> dir -Recurse
```
The `-Recurse` parameter specifies that the current folder and all its subfolders are to be targeted. Almost all cmdlets included in this module accept the `-Recurse` parameter.

# NOTE

# TROUBLESHOOTING NOTE
## Changes to entities made via the Orchestrator web interface or other external applications are not reflected
Execute the `Clear-OrchCache` cmdlet to clear the cache that this module holds in memory.

# SEE ALSO

# KEYWORDS
