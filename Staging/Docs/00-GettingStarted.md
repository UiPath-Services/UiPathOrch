# UiPathOrch Module - Getting Started

- [Overview](#overview)
- [Installation](#installation)
- [Setting Permissions](#setting-permissions)
- [Quick Start](#quick-start)
- [PowerShell Essentials](#powershell-essentials)
- [Navigating the PSDrive](#navigating-the-psdrive)

## Overview

UiPath Orchestrator allows you to manage, monitor, and execute automation
tasks from a web interface. UiPathOrch brings this control to the PowerShell
command line, enabling scripting and bulk operations that are difficult to
perform through the web interface.

### Key Features

- Mounts Orchestrator tenants as PSDrives, so you can navigate folders with
  `dir`, `cd`, and `mkdir`.
- Mount multiple tenants simultaneously, each as a separate drive with a
  different account.
- Supports Automation Cloud, self-hosted Automation Suite, and on-premises Orchestrator.
- Bulk operations with comma-separated values and wildcards.
- Work across multiple tenants and folders using `-Path` and `-Recurse`.
- Parameter completion for all parameter values (folder names, user names,
  asset values, etc.) via `[Tab]` or `[Ctrl+Space]`.
- Fast: sends parallel requests automatically and caches results in memory.
- Does not require UiPath Assistant.

### Accessing Help

```powershell
Get-Help Get-OrchAsset -Full    # Detailed help for a cmdlet
Get-Help UiPathOrch             # List all UiPathOrch cmdlets
Get-OrchHelp                    # Module documentation summary
```

### Sharing Documentation with Users

To display documentation from this Docs folder to the user, use the
[MarkdownPointer](https://www.powershellgallery.com/packages/MarkdownPointer)
module. Install once:

```powershell
Install-Module MarkdownPointer
```

Then open any Markdown file:

```powershell
mdp <path to .md file>
```

To register as an MCP server (once):

```powershell
Register-MdpToClaudeCode       # Claude Code
Register-MdpToClaudeDesktop    # Claude Desktop
```

If the MCP server is available, use `show_markdown` instead of `mdp`.

## Installation

### Prerequisites

- PowerShell 7.x is required.
- Administrator privileges are not needed (installs for the current user).
- UiPathOrch is published on the
  [PowerShell Gallery](https://www.powershellgallery.com/packages/UiPathOrch/).

### Step 1: Install PowerShell 7.x

From a command prompt:

```
winget install --id Microsoft.Powershell --source winget
```

### Step 2: Install UiPathOrch

From a PowerShell 7 console (`pwsh`):

```powershell
Install-PSResource UiPathOrch
```

### Step 3: Register an OAuth External Application

Create an external application in your Orchestrator's admin page. See:

- [Automation Cloud: Managing External Applications](https://docs.uipath.com/automation-cloud/automation-cloud/latest/admin-guide/managing-external-applications)
- [On-premises: Managing External Applications](https://docs.uipath.com/orchestrator/standalone/2023.10/user-guide/managing-external-applications)

| Connection Type | App Secret | Interactive Login | Scope Type |
|---|---|---|---|
| Non-Confidential App | Not required (safe) | Required | User Scope |
| Confidential App | Required (protect carefully) | Not required | App Scope |

**Non-confidential apps are recommended** because they are more secure.
Confidential apps may be needed for unattended scripts.

Steps for a non-confidential application:

1. Navigate to **External Applications** in the Orchestrator admin page.
2. Click **Add application**.
3. Enter an application name.
4. Check **Non-confidential Application**.
5. Click **Add scope**, select all **User scopes** in **Orchestrator API
   Access**, and click **Save**.
6. Click **Add scope**, select all **User scopes** in **Platform Management
   API Access**, and click **Save**.
7. In **Redirect URL**, enter: `http://localhost:8085/Temporary_Listen_Addresses`
8. Click **Add**. Copy the displayed application ID.

### Step 4: Configure UiPathOrch

1. Open a PowerShell 7 console and run:
   ```powershell
   Import-OrchConfig
   ```
2. On first run, a configuration file opens in Notepad automatically.
   To reopen it later, run `Edit-OrchConfig`.
3. Edit the configuration entry:
   - **Name**: Drive name (recommended: same as tenant name).
   - **Root**: Orchestrator URL.
   - **AppId**: The application ID from Step 3.
4. Save and close the configuration file.
5. Run `Import-OrchConfig` again and verify:
   ```powershell
   Import-OrchConfig
   Get-PSDrive
   ```

`Import-OrchConfig` can be run repeatedly. If the configuration file has
been updated, it remounts the drives. If unchanged, it does nothing.

### AI-Assisted Configuration

AI agents can read and edit the configuration file directly. Get the
file path with:

```powershell
Get-OrchConfigPath
```

The configuration file is JSON with comments (JSONC). It contains sample
entries for all authentication patterns. The structure is:

```jsonc
{
  // Global settings (shared across all PSDrives)
  "RedirectUrl": "http://localhost:8085/Temporary_Listen_Addresses",
  "Scope": "OR.Folders.Read OR.Settings.Read",
  "Logging": { "Level": "Verbose", "Enabled": true },
  "IgnoreSslErrors": false,
  "Proxy": { ... },

  "PSDrives": [
    // Non-Confidential App (recommended)
    {
      "Name": "MyTenant",
      "Root": "https://cloud.uipath.com/YOUR_ORG/YOUR_TENANT",
      "AppId": "YOUR_APP_ID",
      "Scope": "OR.Folders.Read OR.Settings.Read OR.Users.Read",
      "Enabled": true
    },
    // Confidential App (for unattended scripts)
    {
      "Name": "MyTenant",
      "Root": "https://cloud.uipath.com/YOUR_ORG/YOUR_TENANT",
      "AppId": "YOUR_APP_ID",
      "AppSecret": "YOUR_APP_SECRET",
      "Scope": "OR.Folders.Read OR.Settings.Read",
      "Enabled": true
    },
    // On-premises with Identity Server
    {
      "Name": "MyOnPrem",
      "Root": "https://orchestrator.example.com/TENANT",
      "IdentityUrl": "https://identity.example.com/identity",
      "AppId": "YOUR_APP_ID",
      "Scope": "OR.Folders.Read OR.Settings.Read",
      "Enabled": true
    },
    // Legacy username/password (pre-21.4 on-premises only)
    {
      "Name": "OldOnPrem",
      "Root": "https://orchestrator.example.com/TENANT",
      "Username": "USERNAME",
      "Password": "PASSWORD",
      "Scope": "OR.Folders.Read OR.Settings.Read",
      "Enabled": false
    }
  ]
}
```

Key points for AI:
- Ask the user for their Orchestrator URL, App ID, and authentication
  type. Do not guess these values.
- Set `"Enabled": false` for sample entries to prevent connection errors.
- Per-drive `Scope` overrides the global `Scope`.
- After editing, run `Import-OrchConfig` to reload.

**Tip**: To auto-load UiPathOrch at startup, add to your profile
(`notepad $profile`):

```powershell
Import-OrchConfig
```

### Upgrading

```powershell
Update-PSResource UiPathOrch
```

The existing configuration file is preserved.

## Setting Permissions

### OAuth Scopes

Each cmdlet requires specific OAuth scopes. Grant scopes to external
applications in the Orchestrator admin page, and request them in the
UiPathOrch configuration file.

- For non-confidential apps, allow **user scopes**.
- For confidential apps, allow **app scopes**.
- Check required scopes with `Get-Help <cmdlet name>`.
- Scope formats:
  - `OR.XXX` — Read and Write
  - `OR.XXX.Read` — Read only
  - `OR.XXX.Write` — Write only

**Avoid granting unnecessary permissions.** For example, with `OR.Folders.Write`
granted, `rmdir *` can remove all folders without confirmation. Without write
permissions, destructive commands are safely rejected.

### Folder Access

Your account must be added to each folder with an appropriate role:

```powershell
Add-OrchFolderUser -Path Orch1:\ -Recurse DirectoryUser <your name> 'Folder Administrator'
```

Check your current username with:

```powershell
Get-OrchCurrentUser
```

## Quick Start

After setup, open a PowerShell console and try:

```powershell
# Load configuration and mount drives
Import-OrchConfig

# Navigate to an Orchestrator drive
cd Orch1:

# List folders (browser opens for login on first access)
dir

# Auto-complete folder names
cd [Ctrl+Space]

# Filter with wildcards
dir s*

# List all module cmdlets
Get-Command -Module UiPathOrch

# Auto-complete cmdlet names
Get-Orch[Ctrl+Space]

# List assets across all subfolders
Get-OrchAsset -Recurse

# List per-user/machine asset values
Get-OrchAsset -Recurse -ExpandUserValue

# Filter users by pattern
Get-OrchUser a*

# Multiple patterns
Get-OrchUser a*,y*
```

## PowerShell Essentials

### Cmdlets

Commands in PowerShell are called cmdlets, always in `Verb-Noun` form.
Names and parameter names are case-insensitive.

The noun part of UiPathOrch cmdlets always begins with `Orch`, `Pm`, `Du`,
`Tm`, or `License`.

### Parameters

Parameters are preceded by `-`. Some don't take a value (switch parameters
like `-Recurse`, `-WhatIf`, `-Confirm`).

Positional parameters can omit the name:

```powershell
# These are equivalent
Set-Location -Path c:\temp
Set-Location c:\temp
```

### Completion

Press `[Tab]` or `[Ctrl+Space]` to complete cmdlet names, parameter names,
and parameter values. UiPathOrch cmdlets support completion for all parameter
values.

**Important**: When using `-Path`, `-Recurse`, or `-Depth`, specify them
before other parameters. The completer for subsequent parameters uses the
folder context from these parameters to offer correct suggestions.

```powershell
# Good: -Path first, then complete -Name with [Ctrl+Space]
Get-OrchProcess -Path Orch1:\Shared -Name [Ctrl+Space]

# Bad: -Name completer doesn't know which folder to look in
Get-OrchProcess -Name [Ctrl+Space] -Path Orch1:\Shared
```

**Note**: PSReadLine (included with PowerShell 7) must be loaded for
completion to work. If you see "PowerShell detected that you might be using
a screen reader and has disabled PSReadLine" at startup, run
`Import-Module PSReadLine` to re-enable it.

### Wildcards

| Pattern | Matches |
|---|---|
| `*` | Any text of any length |
| `?` | Any single character |
| `[]` | Any one of the enclosed characters |

To use these literally, escape with backtick: `` `* ``

Completion automatically escapes wildcard characters in names.

### Whitespace in Values

Enclose text with spaces in single quotes:

```powershell
Set-OrchAsset Text 'my asset' 'that''s it'
```

### Common Parameters

**Folder scope:**

| Parameter | Description |
|---|---|
| `-Path` | Target folder (supports wildcards and comma-separated values) |
| `-Recurse` | Include all subfolders |
| `-Depth` | Limit subfolder depth (e.g., `1` = immediate children only) |
| `-Name` | Target entity name (positional, supports wildcards) |

**Risk mitigation:**

| Parameter | Description |
|---|---|
| `-WhatIf` | Preview operations without executing |
| `-Confirm` | Prompt before each operation |

**Output control:**

| Parameter | Description |
|---|---|
| `-Verbose` (`-vb`) | Show detailed operation messages |
| `-ErrorAction` (`-ea`) | `Continue` (default) or `Stop` on error |
| `-Skip` | Skip N items from the beginning |
| `-First` | Show only the first N items |

### Processing Output

```powershell
# All fields in list format
Get-OrchAsset | Format-List

# Selected fields only
Get-OrchAsset | Select-Object Name, Value

# All nested fields as JSON (essential for entities with nested properties)
Get-OrchUser | ConvertTo-Json

# Save JSON to file for inspection
Get-OrchUser | ConvertTo-Json > c:\users.json

# Filter by condition
Get-OrchAsset | Where-Object { $_.Value -eq $false }
```

`ConvertTo-Json` is particularly useful for entities like users, triggers,
and processes that contain nested objects (e.g., `RolesList`,
`ProcessSettings`). `Format-List` only shows top-level properties.

## Navigating the PSDrive

UiPathOrch is a PowerShell provider. Orchestrator folders are navigated like
a file system.

### Location Reference

| Path | Description |
|---|---|
| `.` | Current folder |
| `..` | Parent folder |
| `Orch1:` | Current folder of the Orch1: drive |
| `Orch1:\` | Root folder of the Orch1: drive |
| `Orch2:..` | Parent folder on the Orch2: drive |
| `Orch1:sub` | Subfolder under the current folder |
| `Orch1:\sub` | Subfolder under the root |

### Navigation Commands

```powershell
Get-PSDrive         # Show all drives and their current locations
pwd                 # Show current location
cd Orch1:           # Go to Orch1: drive's current folder
cd Orch1:\          # Go to root folder
cd ..               # Go to parent folder
pushd <path>        # Save location, then navigate (popd to return)
dir                 # List subfolders
dir -Recurse        # List all subfolders recursively
ii .                # Open current folder in browser
```

**Note**: `dir` only shows folders. Use entity cmdlets (e.g., `Get-OrchUser`,
`Get-OrchAsset`) to view entities within a folder.

### Working with Multiple Drives

```powershell
# Get all drive paths
$drives = (Get-PSDrive -PSProvider UiPathOrch) | ForEach-Object { "$($_.Name):\" }

# Retrieve processes from all drives
Get-OrchProcess -Recurse -Path $drives | Export-Csv c:\all-processes.csv
```
