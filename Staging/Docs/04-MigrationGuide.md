# UiPathOrch Module - Migration & Copy Guide

- [Getting Started](#getting-started)
- [Phase 1: Interview and Migration Planning](#phase-1-interview-and-migration-planning)
- [Phase 2: Environment Preparation](#phase-2-environment-preparation)
- [Phase 3: Migration Execution](#phase-3-migration-execution)
- [Phase 4: Post-Migration Verification](#phase-4-post-migration-verification)
- [Phase 5: Feedback](#phase-5-feedback)
- [Copying Individual Entities](#copying-individual-entities)
- [Appendix](#appendix)

For lift-and-shift migration, start at [Getting Started](#getting-started).
The [Copying Individual Entities](#copying-individual-entities) section
covers selective or partial copying scenarios.

## Getting Started

Before beginning the migration process, set up your environment:

1. Start a PowerShell console using `start_powershell_console`.
2. Mount the Orchestrator drives:

```powershell
Import-OrchConfig
```

3. Verify that the source and destination tenants are mounted as PSDrives:

```powershell
Get-OrchPSDrive
```

Confirm that two drives are listed -- one for the source tenant and one for
the destination tenant. If they are not connected, configure the settings
file. The settings file path can be obtained with `Get-OrchConfigPath`, and
AI can directly read and edit this file to assist with configuration:

```powershell
$configPath = Get-OrchConfigPath
Get-Content $configPath             # Read current configuration
Edit-OrchConfig Default             # Or open in default editor
```

4. Review basic usage of UiPathOrch:

```powershell
Get-OrchHelp
```

Once the environment is ready, proceed to Phase 1.

---

## Phase 1: Interview and Migration Planning

### 1-1: User Interview

Before starting the migration, gather the following information from the user:

- **Source and destination environment details**
  - Source Orchestrator type (Automation Cloud, Automation Suite, On-premises
    MSI) and version
  - Destination Orchestrator type and version
  - Whether the source and destination belong to the same organization
- **User authentication method**
  - Source-side authentication method (Local AD, Entra ID, local users, etc.)
  - Destination-side authentication method
  - Whether the username format changes (e.g., `jsmith` to
    `jsmith@contoso.com`)
- **Migration scope**
  - Scope of tenants, folders, and entities to migrate
  - Whether personal workspaces are included
  - Handling of entities containing passwords (credential stores, credential
    assets)
- **Migration schedule and constraints**
  - Acceptable downtime window
  - Post-migration verification method

### 1-2: Source Tenant Investigation

Connect to the source tenant and investigate the entities to be migrated. Use
`Get-Orch*` cmdlets to inspect each entity's properties. Use `ConvertTo-Json`
to expand nested properties.

```powershell
Get-OrchUser -Path Source: | ConvertTo-Json
Get-OrchCredentialStore -Path Source:
Get-OrchFolderUser -Path Source:\ -Recurse | ConvertTo-Json
Get-OrchAsset -Path Source:\ -Recurse | where ValueType -eq Credential | ConvertTo-Json
```

It is especially important to identify in advance which entities contain
usernames or passwords.

### 1-3: Migration Plan Creation

Based on the interview results, create a migration plan that includes:

- **Case determination**: Whether to use Case A (simple migration) or Case B
  (username mapping required)
- **Target entity inventory**: Review the source tenant's entities in advance
  and clearly define the migration scope
- **Username mapping**: For Case B, generate a user mapping CSV using
  `New-OrchUserMappingCsv` and validate it with `Test-OrchUserMappingCsv`
- **Execution order**: Which steps from this guide to execute and in what order
- **Verification plan**: How to verify each entity after migration
- **Rollback plan**: How to handle issues if they arise

---

## Phase 2: Environment Preparation

### Prerequisites

- UiPathOrch module is imported
- Source and destination drives are connected
- **It is strongly recommended to use descriptive drive names to prevent
  operational mistakes** (e.g., `Source:` and `Destination:`). Numbered names
  like `Orch1:` and `Orch2:` risk confusing the source and destination. Drive
  names can be changed in the configuration file (`Edit-OrchConfig Default`).

### Library Feed Setting

If the destination tenant's library feed is set to "Only host feed",
library copying will fail. Check the current setting and ask the user
which option to use:

```powershell
Get-OrchSetting -Path Destination: 'Deployment.Libraries.FeedScope'
```

| Value | Description |
|---|---|
| `Host` | Only host feed (libraries cannot be copied) |
| `Tenant` | Only tenant feed |
| `Both` | Both host and tenant feeds |

After confirming with the user:

```powershell
Set-OrchSetting -Path Destination: -Name 'Deployment.Libraries.FeedScope' -Value 'Tenant'
```

### Active Directory Integration Prerequisites

If the source or destination tenant is integrated with Active Directory (AD)
or Entra ID, additional setup is required to enable directory user operations:

1. **Configure UiPathOrch with a non-confidential application**: This is
   required to enable the PKCE-based authentication flow.
2. **Verify Entra ID login status**: For AD-integrated organizations,
   UiPathOrch automatically directs the user to the Entra ID login page
   during PKCE authentication. After connecting, UiPathOrch checks whether
   you are signed in via Entra ID. If not, a warning is displayed:
   ```
   WARNING: [Orch1:] You are not signed in to the organization via Entra ID.
   ```
   This warning typically appears when the user signs in with a local account
   instead of Entra ID.
3. **Switch account if needed**: If the warning appears and you need
   organization-level access, use `Switch-OrchCurrentUser` to re-authenticate
   with the correct account:
   ```powershell
   Switch-OrchCurrentUser Orch1:
   ```
   This opens an InPrivate browser window where you can select the appropriate
   identity provider (e.g., Microsoft/Entra ID, Google) and account.

Without Entra ID login, UiPathOrch will fail to search for AD/Entra ID users
(e.g., `Search-OrchDirectory`, `New-OrchUserMappingCsv`, and copy cmdlets
that resolve directory users).

### How to Capture -WhatIf Output

Copy cmdlets support the `-WhatIf` switch parameter, which outputs logs
without actually performing the copy. However, when running via PowerShell.MCP,
`-WhatIf` output cannot be captured using stream redirection (`*>`). Use
`Start-Transcript` instead.

```powershell
Start-Transcript -Path C:\temp\migration-whatif.txt -Force
copy -Recurse Source:\ Destination:\ -WhatIf
Stop-Transcript
```

Review the output file:

```powershell
Show-TextFiles C:\temp\migration-whatif.txt
```

After confirming that the `-WhatIf` output shows no issues, remove `-WhatIf`
and run the actual copy.

---

## Phase 3: Migration Execution

### Step 0: Safety Check — Verify Source Drive Scopes

Before starting the migration, **always verify that the source drive scopes
are read-only**. This prevents accidental modification or deletion of entities
in the source tenant.

```powershell
Get-OrchPSDrive Source: | Select-Object -ExpandProperty Scope
```

Example of read-only scopes:
```
OR.Administration.Read OR.Assets.Read OR.Audit.Read OR.Folders.Read ...
```

Example of scopes with write permissions (no `.Read` suffix):
```
OR.Administration OR.Assets OR.Folders OR.Users ...
```

If the scopes include write permissions, edit the configuration file to change
the source drive scopes to read-only:

```powershell
Edit-OrchConfig Default
```

Find the source drive's PSDrive entry and change the `Scope` value:
1. Remove all scopes with `.Write` suffix (e.g., `OR.Assets.Write`)
2. Add the `.Read` suffix to scopes that have neither `.Read` nor `.Write`

After editing, run `Import-OrchConfig` to reload the configuration.

> **Note**: Adding Platform Management API scopes (those starting with `PM.`,
> e.g., `PM.User.Read`, `PM.Group.Read`, `PM.RobotAccount.Read`) enables
> migration of organization-level entities, broadening the migration scope.

### Case Determination

**Source and destination belong to the same organization:**
Organization-level entities (local users, local groups, robot accounts) are
shared, so no copying is needed. Start from Step 2.

**Source and destination belong to different organizations:**
Start from Step 1.

**Username mapping is required:**
If the source and destination username formats differ (e.g., AD username
`DOMAIN\jsmith` -> Entra ID email address `jsmith@contoso.com`), use the
Case B procedure. Generate the user mapping CSV using `New-OrchUserMappingCsv`
before starting the copy.

### Step 1: Copy Organization-Level Entities (Cross-Organization Only)

```powershell
Copy-PmUser -Path Source: * Destination:
Copy-PmRobotAccount -Path Source: * Destination:
```

- `Copy-PmUser` automatically creates the groups that the copied users belong
  to. Therefore, there is no need to explicitly copy groups.
- If the organization is integrated with Active Directory, copying AD users is
  unnecessary. However, if local users exist, copy them as needed.
- However, if directory users are added to local groups, those groups need to
  be migrated:

```powershell
Get-PmGroupMember -Path Source: -ExportCsv c:\groups.csv
```

Have the user review the exported CSV contents, then import after confirming:

```powershell
Import-Csv c:\groups.csv | Add-PmGroupMember -Path Destination:
```

### Step 2: Copy Tenant-Level and Folder-Level Entities

### Case A: No Username Mapping Required (Simple Migration)

The following single command automatically copies tenant-level entities
(libraries, packages, credential stores, roles, users, machines, calendars,
webhooks) and folder-level entities in the correct order.

```powershell
copy -Recurse Source:\ Destination:\
```

This alone completes most of the migration work.

#### Post-Processing for Entities Containing Passwords

Since the Web API cannot retrieve passwords, entities containing passwords
(users, credential stores, credential assets, etc.) will not have their
passwords migrated during copy. Address this with the following steps:

1. Export the relevant entities from the destination tenant to CSV
2. Have the user edit the CSV to set the password column
3. Import the edited CSV back to the destination tenant to update

Example (credential assets):

```powershell
Get-OrchAsset -Path Destination: -Recurse -ExportCredentialCsv c:\cred-assets.csv
# User edits the CredentialPassword column in the CSV
Import-Csv c:\cred-assets.csv | Set-OrchCredentialAsset
```

### Case B: Username Mapping Required

When source and destination username formats differ (e.g., AD username
`DOMAIN\jsmith` -> Entra ID email `jsmith@contoso.com`), use the user mapping
CSV to correctly translate user references during migration.

#### B-1: Generate User Mapping CSV

Generate a user mapping CSV that maps source directory users to destination
directory users:

```powershell
New-OrchUserMappingCsv Source: Destination: C:\Migration\UserMapping.csv
```

This cmdlet:
1. Enumerates all directory users from the source tenant (scanning PmGroup
   members, tenant users, and folder user assignments)
2. Resolves each source user's details (email, source/provider)
3. Searches the destination directory to find matching users (by username,
   then by email)
4. Exports the mapping to a CSV with columns: SourceUserName, SourceEmail,
   SourceDisplayName, SourceSource, DestinationUserName, Name, SurName,
   DisplayName

> **Note**: Only directory users are included. Local users are not mapped
> because they can be recreated directly using `New-PmUser`.

#### B-2: Review and Edit User Mapping CSV

If all destination users were automatically resolved, the CSV is ready to use.
Otherwise:

1. Open the CSV file and review the entries
2. Fill in the `DestinationUserName` column for any unresolved entries
3. Correct any incorrect automatic mappings if needed

Have the user review and confirm the mapping before proceeding.

#### B-3: Validate User Mapping CSV

Validate the completed CSV to ensure all mappings are correct:

```powershell
Test-OrchUserMappingCsv C:\Migration\UserMapping.csv Source: Destination:
```

This cmdlet checks:
- Each SourceUserName exists in the source tenant
- Each DestinationUserName is not empty
- Each DestinationUserName exists in the destination directory

Fix any errors and re-validate until all entries pass.

#### B-4: Copy Organization-Level Entities with User Mapping

When copying organization-level entities across organizations, use
`-UserMappingCsv` to map users:

```powershell
Copy-PmUser -Path Source: * Destination: -UserMappingCsv C:\Migration\UserMapping.csv
```

#### B-5: Copy Tenant and Folder Entities with User Mapping

Use the `-UserMappingCsv` parameter with copy cmdlets to automatically
translate user references:

```powershell
copy -Recurse Source:\ Destination:\ -UserMappingCsv C:\Migration\UserMapping.csv
```

This single command copies all tenant-level and folder-level entities,
automatically mapping usernames using the CSV for entities that contain user
references (users, folder users, assets, etc.).

Alternatively, copy individual entity types with user mapping:

```powershell
Copy-OrchUser -Path Source: * Destination: -UserMappingCsv C:\Migration\UserMapping.csv
Copy-OrchAsset -Recurse -Path Source:\ * -Destination Destination:\ -UserMappingCsv C:\Migration\UserMapping.csv
Copy-OrchFolderUser -Recurse -Path Source:\ * -Destination Destination:\ -UserMappingCsv C:\Migration\UserMapping.csv
```

#### B-6: Post-Processing for Entities Containing Passwords

Same as Case A -- since the Web API cannot retrieve passwords, entities
containing passwords require post-processing via CSV:

```powershell
Get-OrchAsset -Path Destination: -Recurse -ExportCredentialCsv c:\cred-assets.csv
# User sets the CredentialPassword column in the CSV
Import-Csv c:\cred-assets.csv | Set-OrchCredentialAsset
```

---

## Phase 4: Post-Migration Verification

After migration is complete, compare and verify entities between the source
and destination. Use `Get-Orch*` cmdlets to inspect entity properties. Use
`ConvertTo-Json` to expand nested properties.

---

## Phase 5: Feedback

After the migration is complete (whether successful or not), ask the user if
they would like to provide feedback on this migration guide. Feedback helps
improve the guide for future migrations, especially for edge cases and
untested scenarios.

### Feedback Procedure

1. Ask the user: "Would you like to provide feedback on the migration guide?
   Your feedback will help improve the migration process for future users."
2. If the user agrees, compose a feedback email that includes:
   - **Migration scenario**: Source and destination types (Automation Cloud /
     Automation Suite / On-premises), same-org or cross-org, Case A or Case B
   - **Entities migrated**: Which entity types were migrated
   - **Issues encountered**: Any errors, unexpected behavior, or unclear steps
   - **Suggestions**: Improvements to the guide, missing steps, or additional
     scenarios that should be covered
   - **Overall result**: Whether the migration was successful
3. **Privacy rules (mandatory)**:
   - **NEVER include** personally identifiable information: usernames, email
     addresses, display names, organization names, tenant names, URLs, IP
     addresses, passwords, or any data from the migrated entities
   - **NEVER include** the contents of the user mapping CSV or any exported
     CSV files
   - Use generic descriptions instead (e.g., "AD username format to Entra ID
     email format" instead of actual usernames)
4. **Show the draft email to the user** and ask for explicit approval before
   sending
5. Send the approved feedback to: `yoshifumi.tsuda@uipath.com`
   - Subject: `[UiPathOrch Migration Feedback]`
   - If the AI cannot send email directly, present the draft and ask the user
     to send it manually

### Feedback Template

```
Subject: [UiPathOrch Migration Feedback]

Migration Scenario:
- Source type: [Automation Cloud / Automation Suite / On-premises] version [X.Y]
- Destination type: [Automation Cloud / Automation Suite / On-premises] version [X.Y]
- Same organization: [Yes / No]
- Case used: [A (simple) / B (username mapping)]
- UiPathOrch version: [X.Y.Z]

Entities Migrated:
- [List entity types, e.g., users, folders, assets, processes, etc.]

Issues Encountered:
- [Describe any problems, errors, or confusing steps]

Suggestions for Improvement:
- [What would make the guide better?]

Overall Result: [Successful / Partially successful / Failed]

Additional Comments:
- [Any other observations]
```

---

## Copying Individual Entities

For selective or partial copying (not full lift-and-shift migration),
use individual `Copy-Orch*` cmdlets.

### Copying Tenant-Level Entities

When the current drive is the source tenant, `-Path` can be omitted:

```powershell
PS Orch1:\> Copy-OrchCalendar 'My Calendar' Orch2:
PS Orch1:\> Copy-OrchCalendar * Orch2:
```

Otherwise, use `-Path` to specify the source:

```powershell
Copy-OrchCalendar -Path Orch1: * Orch2:
```

### Copying Folder-Level Entities

When the current folder is the source, `-Path` can be omitted:

```powershell
PS Orch1:\Shared> Copy-OrchAsset * \AnotherFolder
```

Otherwise, use `-Path` to specify the source. Use `-Recurse` to include
subfolders:

```powershell
# Copy all assets from a specific folder
Copy-OrchAsset -Path Orch1:\Shared * Orch2:\Shared

# Copy all assets from all folders (preserving folder structure)
Copy-OrchAsset -Path Orch1:\ -Recurse * Orch2:\

# Copy within the same tenant (between folders)
Copy-OrchAsset -Path Orch1:\FolderA * \FolderB
```

### Copying Folders

The `copy` command (`Copy-Item`) copies folders along with all their
contained entities (packages, processes, assets, queues, triggers, etc.):

```powershell
# Copy all folders and their entities
copy -Recurse Orch1:\* Orch2:\

# Copy a specific folder
copy Orch1:\Shared Orch2:\
```

Classic folders are automatically converted to modern folders when copied.

### Copying via CSV

Export entities to CSV, edit as needed, then import into a Copy cmdlet.
This is useful for selective copying or modifying values during the copy.

The `-ExportCsv` CSV does not include a `Destination` column. Add a
`Destination` column to the CSV and fill in the target path for each row:

```powershell
# 1. Export asset names to CSV
Get-OrchAsset -Path Orch1:\Shared | Select-Object Path, Name | Export-Csv C:\temp\assets.csv

# 2. Add a Destination column and set the target path for each row

# 3. Import and copy
Import-Csv C:\temp\assets.csv | Copy-OrchAsset
```

For folder user assignments:

```powershell
Get-OrchFolderUser -Path Orch1:\Shared -ExportCsv C:\temp\folder-users.csv
# Edit the Path column in the CSV (e.g., Orch1:\Shared → Orch2:\Shared)
Import-Csv C:\temp\folder-users.csv | Add-OrchFolderUser
```

### Personal Workspaces

Personal workspaces can be operated on, provided that exploration has been
started in the Orchestrator Web UI. Since starting exploration is not
supported via API, it must be done manually. After starting exploration,
run `Clear-OrchCache` to refresh.

---

## Appendix

### Entity Classification

**Organization-Level Entities:**
Local users, local groups, robot accounts

**Tenant-Level Entities:**
Libraries, packages, credential stores, roles, users, machines, calendars,
webhooks

**Folder-Level Entities:**
Folders, folder users, folder machines, storage buckets, packages in folder
feeds, processes, assets, queues, queue items, triggers, API triggers, test
sets, test schedules, test data queues, action catalogs

### Entities Containing Usernames

Entities requiring `-UserMappingCsv` in Case B (username mapping):

| Entity | Properties Containing Usernames |
|---|---|
| User (OrchUser) | UserName, FullName, EmailAddress |
| Folder User (OrchFolderUser) | UserEntity.UserName, UserEntity.FullName |
| Asset (OrchAsset, per-user/per-machine) | UserName, MachineName |
| Credential Asset (OrchAsset, Credential type) | CredentialUsername |

### Entities Containing Passwords/Secrets

Entities requiring post-processing because passwords are not migrated:

| Entity | Property |
|---|---|
| User (OrchUser) | Password |
| Credential Store (OrchCredentialStore) | Secrets in AdditionalConfiguration |
| Credential Asset (OrchAsset, Credential type) | CredentialPassword |
| Storage Bucket (OrchBucket) | Password |
| Webhook (OrchWebhook) | Secret |

### Important Notes

- **Destination tenant library feed settings**: To copy libraries, the
  destination tenant's library feed must be enabled. Check and set it with:

  ```powershell
  # Check current setting
  Get-OrchSetting -Path Destination: 'Deployment.Libraries.FeedScope'

  # Set to allow tenant feed (required for library copy)
  Set-OrchSetting -Path Destination: -Name 'Deployment.Libraries.FeedScope' -Value 'Tenant'
  ```

  Valid values for `Deployment.Libraries.FeedScope`:
  | Value | Description |
  |---|---|
  | `Host` | Only host feed (libraries cannot be copied) |
  | `Tenant` | Only tenant feed |
  | `Both` | Both host and tenant feeds |
- **Entity dependencies**: Some entities reference other entities, so copy
  order matters. For example, processes depend on packages, so packages must
  be copied first.
- **Each copy operation is independent**: If copying one entity fails, it does
  not affect the copying of other entities. Cross-version and cross-platform
  migrations (e.g., Automation Cloud to on-premises) will produce errors for
  entities that are incompatible with the destination. These errors are
  expected and can be reviewed after the migration completes. Fix the
  underlying issues and re-run the migration — already-copied entities are
  skipped automatically.
- **Personal workspaces**: To migrate personal workspaces, exploration must be
  started in the Orchestrator Web UI on both sides, followed by running
  `Clear-OrchCache`. Personal workspaces can only be automatically copied when
  the folder names are the same (i.e., when the usernames are the same). When
  usernames differ, copy folder entities individually:

```powershell
Copy-OrchPackage -Path "Source:\user1's workspace" * * -Destination "Destination:\newname's workspace"
Copy-OrchProcess -Path "Source:\user1's workspace" * -Destination "Destination:\newname's workspace"
Copy-OrchAsset -Path "Source:\user1's workspace" * -Destination "Destination:\newname's workspace"
# ... and other folder entities similarly
```

- **Queue SLA auto-assignment**: When copying a queue that has an associated
  process (ReleaseId) but no SLA, a default SLA of 24 hours is automatically
  set to preserve the process association. Review and adjust the SLA after
  migration if needed.
- **Queue items**: Due to API constraints, queue items are not automatically
  copied when copying folders. Copy them separately using
  `Copy-OrchQueueItem` (only Status=New items can be copied).
- **Logs**: Due to API constraints, logs cannot be copied.
- **Choosing between Case A and Case B**: If usernames are the same on source
  and destination, use Case A. If usernames differ, always use Case B with
  `-UserMappingCsv`. Without the mapping, copy cmdlets will fail to find users
  and user information is lost from copied entities.
