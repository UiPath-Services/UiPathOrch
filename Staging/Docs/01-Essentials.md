# UiPathOrch Module - Essential Guide for AI

- [Critical AI Execution Rules](#critical-ai-execution-rules)
- [Mandatory AI Decision Flow](#mandatory-ai-decision-flow)
- [Path Property - Critical for Context](#path-property---critical-for-context)
- [Error Handling Protocol](#error-handling-protocol)
- [Confidential App Considerations](#confidential-app-considerations)
- [UiPath Orchestrator Architecture](#uipath-orchestrator-architecture)
- [Permission Model](#permission-model)
- [Performance & Cache Management](#performance--cache-management)
- [Quick Reference](#quick-reference)
- [Cmdlet-Specific Guidance](#cmdlet-specific-guidance)

ATTENTION: This document provides operational guidelines for safely using the
UiPathOrch PowerShell module to control UiPath Orchestrator. This module
operates on live production systems.

## CRITICAL AI EXECUTION RULES

### NEVER VIOLATE THESE RULES:
1. START SESSION: Execute Import-OrchConfig, then Get-OrchPSDrive to verify available drives
2. ALWAYS confirm target drive with user when multiple drives exist
3. ALWAYS use -WhatIf first for destructive operations
4. ALWAYS run Clear-OrchCache before retrying after errors

### IMMEDIATE FAILURE CONDITIONS:
- No drives found → Tell user to run Edit-OrchConfig, then Import-OrchConfig
- Permission error → Follow error protocol below
- Connection timeout → Clear-OrchCache and retry once

## MANDATORY AI DECISION FLOW

### Step 1: Environment Verification (ALWAYS REQUIRED)
Execute: Get-OrchPSDrive
CRITICAL: Use Get-Help Get-OrchPSDrive -Examples for decision logic patterns

### Step 2: Command Classification
Read-Only Operations:
- Get-*, dir, cd, Clear-OrchCache, Get-Help

Destructive Operations (Two-Step Process):
- Start-*, Stop-*, New-*, Add-*, Remove-*, Update-*, Set-*, Import-*
- CRITICAL: See Get-Help Start-OrchJob -Examples for safe execution patterns

### Step 3: Safe Execution Protocol
FOR DESTRUCTIVE OPERATIONS:
1. FIRST: Execute with -WhatIf
2. SHOW results to user and ask: 'Should I proceed with this operation?'
3. IF user confirms: Execute without -WhatIf

### Capturing -WhatIf Output

When using PowerShell.MCP, -WhatIf output (ShouldProcess messages) is not visible
to AI. Stream redirection (`*>&1`), `-WarningVariable`, and `-InformationVariable`
cannot capture it either. Use `Start-Transcript` instead.

```powershell
Start-Transcript -Path C:\temp\whatif.txt -Force
Start-OrchJob Report* -WhatIf
Stop-Transcript
Show-TextFiles C:\temp\whatif.txt
```

Lines beginning with `What if:` in the transcript file are the -WhatIf output.

## PATH PROPERTY - CRITICAL FOR CONTEXT

### UiPathOrch vs Standard PowerShell Properties
UiPathOrch objects use custom .Path property (NOT PSPath):
- .Path = "Orch1:\Shared\Production" ✅ Shows UiPath folder structure
- .PSPath = "" (empty/unreliable) ❌ Standard PowerShell property
- .PSParentPath = "" (empty/unreliable) ❌ Standard PowerShell property

### COMMON AI MISTAKES TO AVOID
❌ Using PSPath/PSParentPath for folder location
❌ Creating custom FolderPath calculations
❌ Assuming standard PowerShell path properties work
✅ ALWAYS use the .Path property for UiPathOrch objects

### CORRECT PATH USAGE EXAMPLES

```powershell
# Show all triggers with their locations
Get-OrchTrigger -Path Orch1:\ -Recurse | Select-Object Path, Name, Enabled, ReleaseName

# Show all assets with folder context
Get-OrchAsset -Path Orch1:\ -Recurse | Select-Object Path, Name, ValueType, Value

# Show all users with organizational context
Get-OrchUser -Path Orch1: | Select-Object Path, UserName, FullName

# Show all processes with their folder locations
Get-OrchProcess -Path Orch1:\ -Recurse | Select-Object Path, Name, ProcessVersion
```

KEY RULE: Always include Path in Select-Object for clarity and troubleshooting.

## ERROR HANDLING PROTOCOL

### Automatic Error Classification
IF error contains ["Unauthorized", "AppId", "OAuth"]:
    → OAuth/Connection issue → Check drive configuration, suggest Edit-OrchConfig

IF error contains ["Forbidden", "Access denied", "Permission"]:
    → Permission issue → Try alternative commands, check folder context

IF error contains ["timeout", "network", "connection"]:
    → Connection issue → Clear-OrchCache, retry once

IF error contains ["not found", "does not exist"]:
    → Object not found → Verify object name/path, check folder context

IF error contains ["This operation is not supported in a Confidential app"]:
    → Confidential App Limitation → This is normal for certain commands (e.g., Get-OrchCurrentUser)

### Standard AI Error Response Template
- ❌ [Command] failed: [error message]
- 🔧 Diagnosis: [error type from above]
- 💡 Action: [specific action taken]
- 📋 Result: [outcome of recovery attempt]

## CONFIDENTIAL APP CONSIDERATIONS

### Understanding Confidential App Limitations
- Confidential apps have OAuth2 restrictions on certain user information endpoints
- Commands that typically fail in Confidential apps:
  * Get-OrchCurrentUser (user verification)
  * Some user management operations
- Commands that work normally in Confidential apps:
  * All Get-Orch* commands for processes, jobs, queues, assets, triggers
  * All folder navigation (cd, dir)
  * Most management operations (Start-OrchJob, etc.)

### Workarounds for Confidential Apps
Instead of Get-OrchCurrentUser for verification:
1. Use successful drive mounting as connection proof
2. Use Get-OrchPSDrive to verify drive status
3. Test with simple operations like 'dir' or 'Get-OrchProcess'

## UIPATH ORCHESTRATOR ARCHITECTURE

### Hierarchy Structure
Organization → Tenant → Folder (up to 7 levels)
- Tenant: Contains Users, Machines, Libraries, Packages, Audit, Webhooks
- Folder: Contains Processes, Jobs, Queues, Assets, Robots, Triggers

Important: Tenant-level entities appear in root folder of UiPathOrch drive.

### Folder System Knowledge

#### Folder Types
1. **Standard Folder** (FolderType = "Standard"):
   - Normal project folders where you deploy and run automations
   - Full access to all features (processes, jobs, queues, assets, etc.)

2. **Personal Workspace** (FolderType = "Personal"):
   - Private folder for individual users
   - Only the owner and admins can access

3. **Solution Folder** (FolderType = "Solution"):
   - Container for complete solution deployments
   - Groups related automation projects together

#### Folder Provisioning
- **Modern (ProvisionType = "Automatic")**: Current standard, supports up to 7 folder levels
- **Classic (ProvisionType = "Manual")**: Legacy method, flat structure only (deprecated)

#### Feed System
1. **Tenant Feed (FeedType = "Processes")**:
   - Folder uses centralized tenant package storage

2. **Folder Feed (FeedType = "FolderHierarchy")**:
   - Folder has its own isolated package storage
   - Can only use packages uploaded to this specific folder feed
   - More restrictive but provides better isolation

KEY RULES:
- Libraries are ALWAYS sourced from tenant-level feeds (regardless of folder type)
- Feed type cannot be changed after folder creation
- Only root-level folders can have their own feeds; subfolders inherit parent settings

Process Deployment Flow:
1. Upload Package to appropriate Feed (tenant or folder-specific)
2. Assign Package to Folder as a Process
3. Execute Process as Jobs

## PERMISSION MODEL

### Three-Level Permission System (Simplified)
1. **OAuth Scope Level**: What API operations your connection can perform
2. **Tenant Role Level**: Access to tenant-wide resources (users, machines, settings)
3. **Folder Role Level**: Access to folder-specific resources (processes, jobs, queues)

Think of it as: Connection Permission → Tenant Permission → Folder Permission

## PERFORMANCE & CACHE MANAGEMENT

### Cache Behavior
**STANDARD CMDLETS** (Get-OrchUser, Get-OrchProcess, Get-OrchAsset, etc.):
- First execution: Downloads ALL entities and caches them locally
- Subsequent executions: Returns cached data instantly (no network calls)

**VOLUME CMDLETS** (Get-OrchQueueItem, Get-OrchJob, Get-OrchLog, Get-OrchAuditLog):
- WITH filter parameters: Always queries server (real-time data)
- WITHOUT filter parameters: Shows cached data only (may be empty initially)

### Performance Tips

```powershell
# For large environments - target specific folders
Get-OrchAsset -Path Orch1:\Shared\Production Config*

# Navigate to folder first for batch operations
cd Orch1:\TargetFolder
Get-OrchProcess  # Now operates on current folder
```

**Volume cmdlet cache usage**: Volume cmdlets such as Get-OrchJob query the
server when filter parameters are specified, and the results are cached. On
subsequent calls without filter parameters, cached data is returned instantly.

```powershell
# First call: fetch from server with filters (results are cached)
Get-OrchJob -Path Orch1:\ -Recurse -Last 7d

# Subsequent calls: retrieve from cache without filters
Get-OrchJob | Where-Object State -eq Running
```

### Cache Management

```powershell
Clear-OrchCache  # Force refresh all cached data
```

## QUICK REFERENCE

### Session Initialization

```powershell
# 1. Import configuration and create drives
Import-OrchConfig

# 2. Check available connections
Get-OrchPSDrive

# 3. Connect to target environment
cd Orch1:

# 4. Verify connection (skip if Confidential app)
Get-OrchCurrentUser  # May fail in Confidential apps - this is normal

# 5. Explore folder structure
dir -Recurse | Select-Object FullName, FolderType
```

### Entra ID Warning

For AD-integrated organizations, UiPathOrch automatically directs the user to
the Entra ID login page during PKCE authentication.

If the user signs in with a local account instead of Entra ID, a warning is
displayed:

```
WARNING: [Orch1:] You are not signed in to the organization via Entra ID.
Some operations may require organization-level access.
Use Switch-OrchCurrentUser to sign in with a different account.
```

This warning appears when the JWT token indicates GlobalIdp authentication
instead of Entra ID (aad). Organization-level operations (Platform Management
cmdlets, Active Directory user search) may fail without Entra ID login.

### Switching Accounts

To sign in with a different account (e.g., switching from SSO to a Google
account), use `Switch-OrchCurrentUser`:

```powershell
# Switch user on current drive
Switch-OrchCurrentUser

# Switch user on a specific drive
Switch-OrchCurrentUser Orch1:
```

This opens an InPrivate browser window for authentication, bypassing existing
SSO sessions. After switching, all cached data is cleared automatically.

### Essential Operations

```powershell
# Asset management with Path
Get-OrchAsset -Path Orch1:\ -Recurse | Select-Object Path, Name, ValueType, Value
Set-OrchAsset -ValueType Text -Name ConfigValue -Value NewSetting -WhatIf

# Process and Job management with Path
Get-OrchProcess -Path Orch1:\ -Recurse | Select-Object Path, Name, ProcessVersion
Start-OrchJob MyProcess -WhatIf

# Trigger management with Path
Get-OrchTrigger -Path Orch1:\ -Recurse | Select-Object Path, Name, Enabled, ReleaseName

# User management with Path
Get-OrchUser -Path Orch1: | Select-Object Path, UserName, FullName
```

### Parameter Value Completion

AI agents cannot use interactive tab completion. Use `TabExpansion2` to
programmatically retrieve parameter value candidates:

```powershell
(TabExpansion2 'Get-OrchAsset -Path Orch2:\ -Name ').CompletionMatches | Select-Object CompletionText
```

This works for all UiPathOrch parameters that support completion (folder
names, user names, asset names, machine names, etc.).

**Important**: Specify `-Path` and `-Recurse` before the parameter you
want to complete. The completer uses these to determine the folder context:

```powershell
# Good: -Path first, then complete -Name
(TabExpansion2 'Get-OrchAsset -Path Orch2:\Finance -Name ').CompletionMatches

# Bad: completer doesn't know which folder to look in
(TabExpansion2 'Get-OrchAsset -Name ').CompletionMatches
```

### Permission Verification

```powershell
Get-OrchPSDrive | Select-Object Name, Root, Scope, IsConf  # Drive status
Get-OrchRole -ExpandPermission   # Detailed role permissions
Get-OrchUserPrivilege            # Your effective permissions
Get-OrchFolderUser               # Folder access assignments
```

### Troubleshooting Quick Fixes
- **Connection Issues**: Clear-OrchCache; then retry operation
- **Permission Errors**: Check Get-OrchUserPrivilege and Get-OrchFolderUser
- **Performance Issues**: Use specific folder paths with -Path instead of -Recurse
- **Drive Mount Issues**: Run Edit-OrchConfig to reconfigure, then Import-OrchConfig to reload. Use Get-OrchConfigPath to retrieve the config file path so AI can directly inspect or edit it
- **Confidential App Errors**: Normal for user info commands, try alternative verification
- **Entra ID Warning**: Use Switch-OrchCurrentUser to sign in via Entra ID
- **SSO auto-login prevents account switch**: Use Switch-OrchCurrentUser to open InPrivate browser and choose a different account

## CMDLET-SPECIFIC GUIDANCE

### Best Practice Approach
1. **ALWAYS start with**: Get-Help [CmdletName] -Examples
2. **Check cmdlet help BEFORE** creating custom procedures
3. **Use 01-Essentials.md** only for:
   - Environment verification (Get-OrchPSDrive)
   - Error handling protocols
   - Safety procedures (-WhatIf usage)
   - Path property usage
   - Architecture understanding

### Example Help Commands:

```powershell
Get-Help Get-OrchJob -Examples     # Practical job management examples
Get-Help Start-OrchJob -Examples   # Safe job execution patterns
Get-Help Edit-OrchConfig -Examples # Configuration procedures
```

### OFFICIAL RESOURCES
- UiPath Marketplace: https://marketplace.uipath.com/listings/uipathorch
- PowerShell Gallery: https://www.powershellgallery.com/packages/UiPathOrch

---
Version: 1.4 | Last Updated: June 2025 | Target: AI Operations
Key Improvements:
- Dedicated PATH PROPERTY section with clear examples
- Emphasized .Path vs PSPath/PSParentPath differences
- Added common AI mistakes section
- Reorganized structure for better flow
- Enhanced examples with Path property usage
