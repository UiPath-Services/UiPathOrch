---
title: Migration & Copy
nav_order: 50
permalink: /migration/
---

# UiPathOrch Module - Migration & Copy Guide

- [The migration at a glance](#the-migration-at-a-glance)
- [Choose your path](#choose-your-path)
- [Getting Started](#getting-started)
- [Phase 1: Interview and Migration Planning](#phase-1-interview-and-migration-planning)
- [Phase 2: Environment Preparation](#phase-2-environment-preparation)
- [Phase 3: Migration Execution](#phase-3-migration-execution)
- [Phase 4: Post-Migration Verification](#phase-4-post-migration-verification)
- [Phase 5: Feedback](#phase-5-feedback)
- [Copying Individual Entities](#copying-individual-entities)
- [Appendix](#appendix)
- [Case Study: Two-Phase Migration](#case-study-two-phase-migration)

## The migration at a glance

A whole tenant migration is the steps below, in this order. None of them is safe to run
blind — each row links to the section that states its preconditions.

| # | Step | Command | Details |
|---|---|---|---|
| 0 | Cross-organization only: bring the users across first | `Copy-PmUser -Path OldOrch: * NewOrch: -WhatIf`<br>`Copy-PmRobotAccount -Path OldOrch: * NewOrch: -WhatIf` | [Step 1](#step-1-copy-organization-level-entities-cross-organization-only) |
| 1 | Everything except live data — tenant entities, folders, folder entities | `copy -Recurse OldOrch:\ NewOrch:\ -WhatIf` | [Step 2](#step-2-copy-tenant-level-and-folder-level-entities) |
| 2 | Re-enter the secrets the API cannot read — export, fill in the values, import back. The same export → edit → import shape applies to `Set-OrchSecretAsset`, `Update-OrchBucket` and `Update-OrchUser` | `Get-OrchCredentialAsset -Path NewOrch: -Recurse -ExportCsv c:\cred-assets.csv`<br>`Import-Csv c:\cred-assets.csv \| Set-OrchCredentialAsset -WhatIf` | [Passwords / Secrets](#entities-containing-passwordssecrets) |
| 3 | Queue items with Status `New` | `Copy-OrchQueueItem -Path OldOrch:\ -Recurse * -Destination NewOrch:\ -WhatIf` | [Copying Queue Items](#copying-queue-items) |
| 4 | Storage bucket files | `Copy-OrchBucketItem -Path OldOrch:\ -Recurse * * -Destination NewOrch:\ -WhatIf` | [Copying Bucket Files](#copying-bucket-files) |
| 5 | Start the destination | `Enable-OrchTrigger -Path NewOrch:\ -Recurse * -WhatIf` | [Step 3](#step-3-enable-the-copied-triggers) |
| 6 | Verify against the destination — one `Compare-Orch*` per entity type | `Compare-OrchAsset * NewOrch:\ -Path OldOrch:\ -Recurse` | [Phase 4](#phase-4-post-migration-verification) |

The commands are shown with `-WhatIf`, which reports what would happen and changes nothing —
run each one that way first, then drop `-WhatIf` to perform it. (Step 6 is read-only and has no
`-WhatIf`.)

Before running any of it: make the source drive's scopes read-only
([Step 0](#step-0-safety-check--verify-source-drive-scopes)), decide whether you need a
username mapping CSV ([Choose your path](#choose-your-path)), and check the destination's
library feed setting ([Phase 2](#phase-2-environment-preparation)).

### What `copy` covers

`copy` (`Copy-Item`) scopes itself by the paths you give it and whether you pass
`-Recurse`, so the same command covers a whole tenant or just the folder tree:

```powershell
# Everything: the tenant entities, plus every folder and its folder entities
copy -Recurse OldOrch:\ NewOrch:\ -WhatIf

# Copy the tenant entities only — libraries, tenant-feed packages, credential stores,
# roles, users, machines, calendars, webhooks (copied in that order)
copy OldOrch:\ NewOrch:\ -WhatIf

# Copy every folder and its folder entities (no tenant entities)
copy -Recurse OldOrch:\* NewOrch:\ -WhatIf

# Copy the folder hierarchy only (no tenant entities, no folder entities)
copy -Recurse OldOrch:\ NewOrch:\ -ExcludeEntities -WhatIf
```

Every example above carries `-WhatIf`, so it reports what it would copy and changes
nothing. Drop `-WhatIf` to perform the copy.

The tenant entities are copied by the **root-to-root** form, which is why
`OldOrch:\*` — a wildcard over the top-level folders, not the root itself — leaves them
behind. Queue items, storage bucket files and event triggers are never carried by any of
these; see [Important Notes](#important-notes).

## Choose your path

This guide covers both full migrations and one-off copies. Use the questions below to read only the sections you need.

**What are you doing?**

| Your situation | Go to |
|---|---|
| Migrating a whole tenant (lift-and-shift) | [Getting Started](#getting-started), then Phases 1–4 in order |
| Copying just a few entities (some assets, one folder, queue items, bucket files) | [Copying Individual Entities](#copying-individual-entities) |
| Carrying only **your own** portal settings / notification preferences | [Your Own Portal Preferences](#your-own-portal-preferences-and-notification-subscriptions) |

**For a full migration, three questions decide your route:**

1. **Same organization, or different?**
   - *Same org* — org-level users, groups, and robot accounts are already shared. Skip user copy; start at [Step 2](#step-2-copy-tenant-level-and-folder-level-entities).
   - *Different orgs* — handle users first ([Step 1](#step-1-copy-organization-level-entities-cross-organization-only)), then entities ([Step 2](#step-2-copy-tenant-level-and-folder-level-entities)).

2. **Do the usernames match between source and destination?**
   - *Same* → [Case A](#case-a-no-username-mapping-required-simple-migration) — one command.
   - *They change* (e.g., AD `DOMAIN\jsmith` → Entra `jsmith@contoso.com`) → [Case B](#case-b-username-mapping-required) — a user mapping CSV.

3. **How are users identified on each side — local, or Active Directory / Entra ID?** (only for cross-org migrations; same-org shares users and skips this)
   - Start at [Identity migration scenarios](#identity-migration-scenarios): whether each side is directory-integrated decides whether users are **copied**, **recreated**, or **mapped**.
   - Purely local → local? Go straight to [Migrating local users](#1-2-migrating-local-users-and-robot-accounts) — `Copy-PmUser` preserves the source username and can migrate email-less users, with behavior that depends on the **destination edition**.

> **Directory vs local.** Directory users (AD / Entra ID) are never created by the copy cmdlets — they must already exist in the destination through the identity provider. Only **local users** and **robot accounts** are created by `Copy-PmUser` / `Copy-PmRobotAccount`. Directory user *references* inside entities (folder assignments, per-user assets) are translated by the [Case B](#case-b-username-mapping-required) mapping CSV.

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
Edit-OrchConfig -UseDefaultEditor   # Or open in default editor
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
Get-OrchUser -Path OldOrch: | ConvertTo-Json
Get-OrchCredentialStore -Path OldOrch:
Get-OrchFolderUser -Path OldOrch:\ -Recurse | ConvertTo-Json
Get-OrchAsset -Path OldOrch:\ -Recurse | where ValueType -eq Credential | ConvertTo-Json
```

It is especially important to identify in advance which entities contain
usernames or passwords.

Verify the resolved deployment edition for both source and destination
drives — UiPathOrch builds different URL patterns for Cloud, Automation
Suite, and on-premises:

```powershell
Get-OrchPSDrive OldOrch:, NewOrch: | Select-Object Name, Root, Edition
```

If the `Edition` column shows the wrong value for either drive (rare —
on-premises behind a multi-level reverse proxy that produces a two-segment
path is the known false-positive), pin it explicitly in
`UiPathOrchConfig.json` before proceeding:

```jsonc
"Edition": "OnPremises"   // or "Cloud" / "AutomationSuite"
```

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
  operational mistakes** (e.g., `OldOrch:` and `NewOrch:`). Numbered names
  like `Orch1:` and `Orch2:` risk confusing the source and destination. Drive
  names can be changed in the configuration file (`Edit-OrchConfig -UseDefaultEditor`).

### Library Feed Setting

The destination tenant's library feed setting decides *which feed* copied libraries
land in. Check the current setting and ask the user which option to use:

```powershell
Get-OrchSetting -Path NewOrch: 'Deployment.Libraries.FeedScope'
```

| Value | Description |
|---|---|
| `Host` | Only host feed — copied libraries land in the **host** feed |
| `Tenant` | Only tenant feed |
| `All` | Both host and tenant feeds — copied libraries land in the **tenant** feed |

A copy always writes to whichever feed the destination tenant treats as its default,
because that is the only feed the tenant-level upload endpoint addresses. Two
consequences are worth planning around when the destination is set to `Host`:

- **Verify the destination first.** The host-feed write was measured on standalone
  24.10.0 (API 17). Automation Suite has not been verified — copy one library, confirm
  where it landed, and only then run the whole set.
- **It is one-way.** The tenant API refuses to delete a host-feed package again
  ("Package cannot be deleted. Deployment Settings may be invalid.", errorCode 1005),
  with or without an explicit feed id. Removing what you copied in needs host
  administration.

To keep libraries tenant-scoped at the destination instead, set it before copying:

```powershell
Set-OrchSetting -Path NewOrch: -Name 'Deployment.Libraries.FeedScope' -Value 'Tenant'
```

Reading the source side: on a tenant scoped to `Host`, a plain `Get-OrchLibrary`
already returns the host feed, since that is the tenant's default feed.
`Get-OrchLibrary -HostFeed` addresses the host feed explicitly; it needs Orchestrator
API v17 or later (24.10.0 reports v17), and it reports an error on a tenant scoped to
`Tenant`, which cannot see a host feed at all.

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
   WARNING: [Orch1:] You are signed in with a local user account. This
   organization supports Entra ID directory integration and single sign on.
   ... please sign out and sign in through the organization-specific URL:
   https://cloud.uipath.com/<org> in your browser — then run
   'Import-OrchConfig' here to sign in again with that account.
   ```
   This warning typically appears when the user signs in with a local account
   instead of Entra ID.
3. **Sign in with the directory account**: If the warning appears and you need
   organization-level access, follow the warning's own instructions — in your
   browser, sign out and sign in again through the organization-specific URL,
   then re-trigger the PowerShell sign-in:
   ```powershell
   Import-OrchConfig
   ```
   `Import-OrchConfig` clears the cached sign-in and re-runs the browser PKCE
   flow, which now picks up the directory account. (`Switch-OrchCurrentUser`
   does not work for this — it re-authenticates in a private browser session
   that cannot inherit the directory sign-in you just made.)
   This opens an InPrivate browser window where you can select the appropriate
   identity provider (e.g., Microsoft/Entra ID, Google) and account.

Without Entra ID login, UiPathOrch will fail to search for AD/Entra ID users
(e.g., `Search-OrchDirectory`, `New-OrchUserMappingCsv`, and copy cmdlets
that resolve directory users).

---

## Phase 3: Migration Execution

### Step 0: Safety Check — Verify Source Drive Scopes

Before starting the migration, **always verify that the source drive scopes
are read-only**. This prevents accidental modification or deletion of entities
in the source tenant.

```powershell
Get-OrchPSDrive OldOrch: | Select-Object -ExpandProperty Scope
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
Edit-OrchConfig -UseDefaultEditor
```

Find the source drive's PSDrive entry and change the `Scope` value:
1. Remove all scopes with `.Write` suffix (e.g., `OR.Assets.Write`)
2. Add the `.Read` suffix to scopes that have neither `.Read` nor `.Write`

After editing, run `Import-OrchConfig` to reload the configuration.

> **Note**: Adding Platform Management API scopes (those starting with `PM.`,
> e.g., `PM.User.Read`, `PM.Group.Read`, `PM.RobotAccount.Read`) enables
> migration of organization-level entities, broadening the migration scope.

### Case Determination

You mapped your scenario in [Choose your path](#choose-your-path) above; this is where each route begins.

- **Same organization** — org-level entities (local users, groups, robot accounts) are already shared, so there is nothing to copy at this level. Skip to [Step 2](#step-2-copy-tenant-level-and-folder-level-entities).
- **Different organizations** — start at [Step 1](#step-1-copy-organization-level-entities-cross-organization-only) to bring users across, then [Step 2](#step-2-copy-tenant-level-and-folder-level-entities) for tenant and folder entities.
- **Usernames change between source and destination** (e.g., AD `DOMAIN\jsmith` → Entra `jsmith@contoso.com`) — use [Case B](#case-b-username-mapping-required) within Step 2, generating the mapping CSV with `New-OrchUserMappingCsv` before copying.

### Step 1: Copy Organization-Level Entities (Cross-Organization Only)

`copy -Recurse OldOrch:\ NewOrch:\` copies tenant and folder entities
but does NOT create organization-level users. Users must exist in the
destination organization before their folder assignments can be copied.

#### Identity migration scenarios

How you migrate **users** depends on whether the **source** and the **destination**
are directory-integrated (Active Directory / Entra ID → *directory users*) or not
(→ *local users*). Find your quadrant first — it decides whether users are
**copied**, **recreated**, or **mapped**:

| | **NewOrch: not AD-integrated** (local users) | **NewOrch: AD / Entra integrated** (directory users) |
|---|---|---|
| **OldOrch: not AD-integrated** (local users) | **local → local.** `Copy-PmUser` copies them (see [1-2](#1-2-migrating-local-users-and-robot-accounts)). | **local → local, or → directory.** Either copy them as local users (`Copy-PmUser`; a Cloud destination needs emails) *or*, if those people already exist as Entra users at the destination, treat them as directory users and only map their references ([Case B](#case-b-username-mapping-required)). An intent decision — ask the user. |
| **OldOrch: AD / Entra integrated** (directory users) | **directory → local (recreate).** There is no destination directory to hold them, so recreate each source directory user as a **local** user at the destination, then map references. See below. | **directory → directory.** Directory users are not copied via API — ensure they exist at the destination through its identity provider, then translate references with the mapping CSV ([Case B](#case-b-username-mapping-required)). |

The `local → local` and `directory → directory` diagonals are the well-trodden paths
([1-2](#1-2-migrating-local-users-and-robot-accounts) and [Case B](#case-b-username-mapping-required)).
The two off-diagonal cases need extra care:

**Directory → local** (AD / Entra source → non-AD destination). Recreate the source's
directory users as local users at the destination, using the email and name the
directory already holds:

```powershell
# 1. Export the source directory users' identifying details
Get-OrchUser -Path OldOrch: |
  Where-Object Type -eq 'DirectoryUser' |
  Select-Object @{N='Email';E={$_.EmailAddress}}, @{N='UserName';E={$_.UserName}}, @{N='Name';E={$_.FullName}} |
  Export-Csv C:\Migration\directory-as-local.csv -NoTypeInformation

# 2. Review the CSV — set the Email / UserName each destination local user should have
#    (a Cloud destination keys local users by email, so every row needs one)

# 3. Create them as local users at the destination
Import-Csv C:\Migration\directory-as-local.csv | New-PmUser -Path NewOrch:

# 4. Give each new user a sign-in password (they change it on first login; a
#    per-user temp password is safer — see the password post-processing in Case A/B)
Import-Csv C:\Migration\directory-as-local.csv |
  ForEach-Object { Update-PmUser -Path NewOrch: -UserName $_.Email -Password 'Init!Change1' }
```

Then translate references (folder assignments, per-user assets) from the old directory
name to the new local name with the mapping CSV: `New-OrchUserMappingCsv` already
enumerates the source directory users, so fill each `DestinationUserName` with the new
local user's name and proceed as in [Case B](#case-b-username-mapping-required).

> **Verified on Cloud.** `New-PmUser` (the identity bulk-create API) creates an
> **active**, email-keyed local user at a Cloud destination, and
> `Update-PmUser -Path NewOrch: -UserName <email> -Password <pw>` then sets its
> sign-in password — both calls confirmed to succeed on Automation Cloud (API 20). The
> user signs in with its email and that password. (The interactive browser login itself
> was not exercised here, but it is the standard email + password flow.)

**Local → directory** (non-AD source → AD / Entra destination), when the migrated
people should become directory users. The Entra users must already exist at the
destination through its identity provider — you are not creating them here. Skip
`Copy-PmUser` for them and only map their references (source local name → destination
Entra name) with the mapping CSV ([Case B](#case-b-username-mapping-required)); use
`Copy-PmUser` only for the users you intend to keep as **local** accounts.

#### 1-1: Investigate source users

First, list all users in the source tenant and classify them:

```powershell
Get-OrchUser -Path OldOrch: | Select-Object UserName, Type | Format-Table
```

User types:
| Type | Description | Action |
|---|---|---|
| `DirectoryUser` | AD / Entra ID user | Create in destination directory, or map with UserMappingCsv |
| `DirectoryGroup` | AD / Entra ID group | Create in destination directory |
| `DirectoryRobot` | Robot account | Copy with `Copy-PmRobotAccount`; map with UserMappingCsv if names differ |
| `DirectoryExternalApplication` | External app | Recreate manually |
| `User` | Local user (on-premises / Automation Suite) | Copy with `Copy-PmUser` |

Ask the user:
- Which directory users exist in the destination organization?
- Do any usernames change format (e.g., `admin` → `admin@company.com`)?
- Are there local users that need to be migrated?

#### 1-2: Migrating local users and robot accounts

```powershell
# Copy local users (creates them in the destination org, and creates their groups)
Copy-PmUser -Path OldOrch: * NewOrch:

# Copy robot accounts
Copy-PmRobotAccount -Path OldOrch: * NewOrch:
```

`Copy-PmUser` creates the groups that the copied users belong to, so there is no
need to copy groups separately.

**How the destination edition changes the result.** A local user has a userName
and (optionally) an email. Automation Suite and on-premises identify local users by
userName (the email may differ or be absent); Automation Cloud signs users in by
their email. `Copy-PmUser` follows this automatically:

| Destination | userName at destination | Email-less source user |
|---|---|---|
| **Automation Suite / on-premises** | the **source userName** is preserved (it may differ from the email) | **migrated** — created with just its userName |
| **Automation Cloud** | set to the **email** (Cloud signs in by email) | **skipped** — with no email it could not sign in on Cloud |

(Cloud's bulk-create API *will* accept an email-less or userName≠email account, but on Cloud sign-in is by email, so such an account would be unusable — that is why `Copy-PmUser` follows the email convention there.)

A user that already has an email keeps it in every case; only the userName and the handling of email-less users differ by edition.

- **When to skip `Copy-PmUser` entirely:** if the destination is AD / Entra ID
  integrated (no local users needed), or the source is AD-integrated — only
  directory users need to exist in the destination.
- **Email-less local users → Automation Cloud** are skipped (Cloud needs an
  email). Give them one either way:
  - add the email at the **source** first, then copy —
    `Update-PmUser -Path OldOrch: -UserName <name> -NewEmail <addr>`
    (needs an Automation Suite / on-premises source on a version that supports the
    `Pm*` cmdlets, i.e. API 15+ / 22.4+; older MSI sources cannot, so use the export
    path below), or
  - recreate them at the **destination** with an email via the export path below.
- **Add or change an email after migration** (Automation Suite / on-premises):
  `Update-PmUser -Path NewOrch: -UserName <name> -NewEmail <addr>`.
- **Export → recreate path** (full control, and the way to supply emails for a
  Cloud destination). The export CSV carries a `UserName` column, so a userName
  that differs from the email round-trips:
  ```powershell
  Get-PmUser -Path OldOrch: -ExportCsv C:\Migration\local-users.csv
  # Review/edit the CSV — fill in the Email column where blank; adjust UserName if needed
  Import-Csv C:\Migration\local-users.csv | New-PmUser -Path NewOrch:
  ```
- Directory users (AD / Entra ID) cannot be copied via API — they must be added
  to the destination through its identity provider, or, when the destination is
  **not** directory-integrated, recreated as local users (see
  [Identity migration scenarios](#identity-migration-scenarios) above).

> **Selecting users by name.** `-UserName` on `Get-` / `Update-` / `Copy-PmUser`
> matches a user by **either** its userName **or** its email, so it resolves a
> userName that differs from the email (and userName-only accounts).

#### 1-3: Copy group memberships

If directory users are added to local groups, those groups need to be
migrated:

```powershell
Get-PmGroupMember -Path OldOrch: -ExportCsv c:\groups.csv
```

Have the user review the exported CSV contents, then import after confirming:

```powershell
Import-Csv c:\groups.csv | Add-PmGroupMember -Path NewOrch:
```

### Step 2: Copy Tenant-Level and Folder-Level Entities

### Case A: No Username Mapping Required (Simple Migration)

The following single command automatically copies tenant-level entities
(libraries, packages, credential stores, roles, users, machines, calendars,
webhooks) and folder-level entities in the correct order.

```powershell
copy -Recurse OldOrch:\ NewOrch:\
```

This alone completes most of the migration work.

> **`-Recurse` is what pulls in the folders.** On a root-to-root copy,
> `copy OldOrch:\ NewOrch:\` **without** `-Recurse` copies only the tenant-level
> entities listed above (libraries, packages, credential stores, roles, users, machines,
> calendars, webhooks). The folders themselves and their folder-level entities (assets,
> queues, processes, triggers, buckets, test sets, test data queues, action catalogs,
> etc.) are **not** copied. Add `-Recurse` — as the command above does — to also copy
> every folder and its contents.

#### Post-Processing for Entities Containing Passwords

Since the Web API cannot retrieve passwords, entities containing passwords
(users, credential stores, credential assets, etc.) will not have their
passwords migrated during copy. Address this with the following steps:

1. Export the relevant entities from the destination tenant to CSV
2. Have the user edit the CSV to set the password column
3. Import the edited CSV back to the destination tenant to update

Example (credential assets):

```powershell
Get-OrchCredentialAsset -Path NewOrch: -Recurse -ExportCsv c:\cred-assets.csv
# User edits the CredentialPassword column in the CSV
Import-Csv c:\cred-assets.csv | Set-OrchCredentialAsset
```

> **Shortcut — already maintain the credentials in a CSV?** Then you do not need
> to copy those assets at all. `Set-OrchCredentialAsset` (like the other
> `Set-Orch*Asset` cmdlets) is create-or-update: `Import-Csv | Set-OrchCredentialAsset`
> creates any missing credential assets and per-user values directly, with the real
> passwords, in one pass — no placeholder round-trip. Since the Web API cannot read
> passwords out of the source tenant anyway, a maintained CSV is often the actual
> source of truth for credentials; copying is only useful for these assets when the
> source tenant is your only record of their structure. Per-user rows still require
> the owning user / robot account to be assigned to the destination folder first.

### Case B: Username Mapping Required

> **Maturity note.** The `-UserMappingCsv` workflow (`New-OrchUserMappingCsv`,
> `Test-OrchUserMappingCsv`, and `-UserMappingCsv` on the copy cmdlets) has now
> been exercised in a real cross-organization migration (AD on-prem to Automation
> Cloud), which drove the robot-account support and validation improvements
> described below. It is still prudent to **rehearse on a non-production
> destination**, run `Test-OrchUserMappingCsv`, and verify a small sample of
> migrated entities (a few folder-user assignments and per-user assets) before
> the full run.

When source and destination username formats differ (e.g., AD username
`DOMAIN\jsmith` -> Entra ID email `jsmith@contoso.com`), use the user mapping
CSV to correctly translate user references during migration.

> **Scope of the mapping CSV.** `New-OrchUserMappingCsv` enumerates **directory
> users (AD / Entra ID) and robot accounts**. Local org users are *not* included —
> they are handled separately in Step 1-2. The mapping CSV translates
> **directory-user and robot references inside tenant/folder entities** (folder-user
> assignments, per-user assets, etc.); it is not used to copy local users.
> Robot rows resolve against the destination *tenant user list*: same-named robots
> auto-fill, renamed ones are left empty to map by hand (delete the row if that
> robot's values are not needed). Robot accounts own most per-user asset values in
> practice, so filling these rows is what keeps per-user credentials from being
> dropped during the asset copy.
> Directory **groups** and **external applications** are not generated, but the
> name translation itself is type-agnostic — if such a reference needs renaming
> (e.g., a folder-user assignment to a group whose name differs in the
> destination), add a row for it by hand and the copy cmdlets will apply it.

#### B-1: Generate User Mapping CSV

Generate a user mapping CSV that maps source directory users to destination
directory users:

```powershell
New-OrchUserMappingCsv OldOrch: NewOrch: C:\Migration\UserMapping.csv
```

This cmdlet:
1. Enumerates all directory users **and robot accounts** from the source tenant
   (scanning PmGroup members, tenant users, and folder user assignments)
2. Resolves each source user's details (email, source/provider); robot rows
   carry `robot` in the SourceSource column instead
3. Resolves robot accounts against the destination **tenant user list**
   (same-named robots auto-fill), and searches the destination directory for
   matching directory users (by username, then by email)
4. Exports the mapping to a CSV with columns: SourceUserName, SourceEmail,
   SourceDisplayName, SourceSource, DestinationUserName, Name, SurName,
   DisplayName — directory users first, then robot accounts, each block
   sorted by name

> **Note**: Local users are not in the mapping CSV — they are migrated separately
> in [Step 1-2](#1-2-migrating-local-users-and-robot-accounts) (`Copy-PmUser`, or the
> export → `New-PmUser` path).

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
Test-OrchUserMappingCsv C:\Migration\UserMapping.csv OldOrch: NewOrch:
```

This cmdlet checks, for each row:
- SourceUserName exists in the source tenant
- DestinationUserName is not empty
- DestinationUserName is reachable as a destination **tenant user** — the check
  that predicts the copy's per-user value resolution, and the one that covers
  robot accounts (which the directory search may not return). Reachable rows
  count as **OK**.
- Otherwise, whether it resolves in the destination **directory** — counted as
  **Pending**: expected before folder users are copied, since folder-user copy
  assigns directory users (and creates their tenant user record) automatically
- A name found in neither place is an error, or a warning when the Name /
  DisplayName columns are filled in for `New-PmUser` creation

Fix any errors and re-validate. Pending entries are fine to proceed with, as
long as folder users are copied before assets (see B-5).

#### B-4: Organization-Level Users

Organization-level **local users** are handled as in Step 1-2 (the mapping CSV
does not apply to them). **Directory users** are not copied via API; they must
already exist in the destination organization through the identity provider.
**Robot accounts** must likewise exist in the destination before entities that
reference them are copied — create them with `Copy-PmRobotAccount` (or
`New-PmRobotAccount`) and fill in their mapping rows when the names differ.
The mapping CSV is consumed in the next step, when tenant- and folder-level
entities that *reference* those directory users and robots are copied.

#### B-5: Copy Tenant and Folder Entities with User Mapping

Use the `-UserMappingCsv` parameter with copy cmdlets to automatically
translate user references:

```powershell
copy -Recurse OldOrch:\ NewOrch:\ -UserMappingCsv C:\Migration\UserMapping.csv
```

This single command copies all tenant-level and folder-level entities,
automatically mapping usernames using the CSV for entities that contain user
references (users, folder users, assets, etc.).

Alternatively, copy individual entity types with user mapping:

```powershell
Copy-OrchUser -Path OldOrch: * NewOrch: -UserMappingCsv C:\Migration\UserMapping.csv
Copy-OrchFolderUser -Recurse -Path OldOrch:\ * -Destination NewOrch:\ -UserMappingCsv C:\Migration\UserMapping.csv
Copy-OrchAsset -Recurse -Path OldOrch:\ * -Destination NewOrch:\ -UserMappingCsv C:\Migration\UserMapping.csv
```

> **Order matters** when copying entity types individually: folder users must be
> assigned **before** assets are copied, otherwise per-user asset values cannot
> be re-homed and are dropped with a warning — and an asset whose values all
> drop and that has no global default value is skipped entirely. `copy -Recurse`
> runs the stages in the correct order automatically.

#### B-6: Post-Processing for Entities Containing Passwords

Same as Case A -- since the Web API cannot retrieve passwords, entities
containing passwords require post-processing via CSV:

```powershell
Get-OrchCredentialAsset -Path NewOrch: -Recurse -ExportCsv c:\cred-assets.csv
# User sets the CredentialPassword column in the CSV
Import-Csv c:\cred-assets.csv | Set-OrchCredentialAsset
```

The Case A shortcut applies here too: when the credential values are already
maintained in a CSV, importing it with `Set-OrchCredentialAsset` creates the
missing assets and per-user values directly — no copy / placeholder round-trip
for those assets.

### Step 3: Enable the Copied Triggers

**Time triggers and queue triggers are copied disabled.** `Copy-Item` and
`Copy-OrchTrigger` force `Enabled = false` on the destination trigger, and warn
once for each trigger that was enabled at the source:

```
WARNING: 'NewOrch:\Finance\Nightly': This trigger will be disabled. Please enable it if necessary.
```

That is deliberate. It lets a destination tenant be prepared days ahead without
anything starting there, and it stops the same schedule firing in two tenants
while both still exist. It also means **nothing runs in the destination until
you turn the triggers on** — the last step of the cutover, not of the copy.

```powershell
# Enable every trigger in the destination tree
Enable-OrchTrigger -Path NewOrch:\ -Recurse *
```

To reproduce the source's enabled/disabled state instead of enabling
everything, export the source triggers and enable only those that were enabled
there:

```powershell
Get-OrchTrigger -Path OldOrch:\ -Recurse -ExportCsv C:\Migration\triggers.csv
Import-Csv C:\Migration\triggers.csv |
    Where-Object Enabled -eq 'True' |
    ForEach-Object { $_.Path = $_.Path -replace '^OldOrch:', 'NewOrch:'; $_ } |
    Enable-OrchTrigger
```

> **API triggers are different — they keep the source's state.** `Copy-OrchApiTrigger`
> and the folder copy do *not* reset `Enabled` on an API trigger, so one that is
> enabled at the source arrives enabled and can be invoked over HTTP as soon as it
> exists. If you are copying ahead of the cutover, disable them at the destination
> until you are ready: `Disable-OrchApiTrigger -Path NewOrch:\ -Recurse *`.

Event triggers are not copied at all (see [Important Notes](#important-notes)) —
recreate them in the destination web UI, against a destination Integration
Service connection, and enable them there.

---

## Phase 4: Post-Migration Verification

Verify by **measuring the destination**, not by reading the copy's console
output. A run log records what the tool believed it did; a comparison records
what is actually there. The `Compare-Orch*` family exists for this: each cmdlet
diffs a reference location (`-Path`) against a difference location, matching
entities **by name** (ids are tenant-local) and ignoring volatile fields (Id,
Key, timestamps, creator / modifier).

```powershell
Compare-OrchAsset * NewOrch:\ -Path OldOrch:\ -Recurse
```

Each row carries a SideIndicator:

| Indicator | Meaning | What it usually means after a migration |
|---|---|---|
| `<=` | only in the source | not copied — this is the row to act on |
| `=>` | only in the destination | pre-existing there, or renamed |
| `<>` | on both sides, a compared property differs | the source changed after the copy, or part of the entity was rejected |
| `==` | identical (only shown with `-IncludeEqual`) | nothing to do |

A full sweep, exported as migration evidence:

```powershell
$folderTypes = 'Asset','Queue','Process','Trigger','ApiTrigger','Bucket',
               'FolderUser','FolderMachine','ActionCatalog'
foreach ($t in $folderTypes) {
    & "Compare-Orch$t" * NewOrch:\ -Path OldOrch:\ -Recurse -IncludeEqual |
        Export-Csv "C:\Migration\verify-$t.csv" -NoTypeInformation
}
```

Tenant-level entities take the same shape without `-Recurse`:

```powershell
foreach ($t in 'Role','User','Machine','Calendar','Webhook','CredentialStore') {
    & "Compare-Orch$t" * NewOrch: -Path OldOrch: |
        Export-Csv "C:\Migration\verify-$t.csv" -NoTypeInformation
}
```

Notes:

- **Cross-tenant per-user values**: pass `-UserMappingCsv` (the same CSV the copy
  consumed) to `Compare-OrchAsset`, so remapped owners do not show up as
  spurious `UserValues` differences.
- **Secrets are invisible to the comparison.** Credential and Secret *values*
  cannot be read back through the API, so password drift is never reported (the
  credential username is compared). Verify those by running a job that uses them.
- **No `Compare-*` for packages, libraries, queue items or bucket files.** Diff
  those with `Get-OrchPackage` / `Get-OrchLibrary` / `Get-OrchQueueItem` /
  `Get-OrchBucketItem` on both sides.
- Anything reported as `<=` is missing at the destination. Fix the cause and
  re-run that entity type's copy — re-copying is safe, since entities already at
  the destination are not overwritten (they come back as duplicate-name errors).

For a property-level look at a single entity, `Get-Orch*` with `ConvertTo-Json`
still has its place:

```powershell
Get-OrchAsset -Path NewOrch:\Finance MyAsset | ConvertTo-Json
```

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
5. Submit the approved feedback through the project's GitHub channel:
   - **GitHub Discussions** (preferred): <https://github.com/UiPath-Services/UiPathOrch/discussions>
   - or **GitHub Issues** for a concrete bug or feature request: <https://github.com/UiPath-Services/UiPathOrch/issues>
   - If the AI cannot open a browser, present the draft and ask the user to
     post it manually

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

#### Shared (linked) assets, queues, and buckets

An asset, queue, or bucket can be **linked** into several folders: one entity, visible
from all of them, with a single value that changes everywhere at once. A copy handles
that in one of two ways, depending on where it is going.

**Within one tenant, a copy is always an independent entity.** `Copy-OrchAsset -Path
Orch1:\FolderA * \FolderB` gives `FolderB` its *own* asset that then drifts from the
original. To share the existing one instead — the usual intent — link it rather than copy
it:

```powershell
Add-OrchAssetLink -Path Orch1:\FolderA MyAsset -Link Orch1:\FolderB -WhatIf
```

The copy warns when it does this, so a link that was meant to be preserved doesn't turn
into two diverging entities unnoticed.

**Across tenants, links are reproduced where the destination allows it.** When the source
entity is shared with another folder, the copy looks for that folder's counterpart on the
destination and links to the entity already there instead of creating a second one — so
copying both folders of a shared asset yields one linked asset, not two. This needs the
counterpart folder to exist on the destination with a matching path, and the entity to
have been copied into it (either folder may go first; the second one establishes the
link). When that isn't the case the entity is copied independently and the copy says so
at the end of the run.

To reapply a sharing layout explicitly — after copying folders under new names, or to
repair a migration done with an older version — snapshot it and import it back:

```powershell
Get-OrchAssetLink  -Path OldOrch:\ -Recurse -ExportCsv C:\temp\asset-links.csv
Get-OrchQueueLink  -Path OldOrch:\ -Recurse -ExportCsv C:\temp\queue-links.csv
Get-OrchBucketLink -Path OldOrch:\ -Recurse -ExportCsv C:\temp\bucket-links.csv
# Edit the Path and Link columns in each CSV (e.g. OldOrch:\Shared → NewOrch:\Shared)
Import-Csv C:\temp\asset-links.csv | Add-OrchAssetLink -WhatIf
```

`Add-Orch*Link` shares an entity that already exists — it does not merge duplicates. If a
copy already created a separate entity in the link folder, delete that one first
(`Remove-OrchAsset` / `Remove-OrchQueue` / `Remove-OrchBucket`), keep one, then link.
Linking works for global-scope assets only; per-robot assets cannot be shared across
folders at all. Re-importing an unchanged CSV is a no-op, so the import is safe to repeat,
and `Get-Orch*Link` on the destination verifies the result.

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

`copy` (`Copy-Item`) takes two migration-specific parameters when the path is an
Orchestrator drive:

- **`-ExcludeEntities`** — recreate the folder tree **only**, copying no entities.
  Neither tenant-level entities (libraries, packages, ...) nor folder-level entities
  (assets, queues, processes, ...) are copied, and personal workspaces are skipped.
  Use it to lay down the destination folder hierarchy first and migrate entities
  selectively afterwards.

  ```powershell
  # Recreate Orch1's folder tree in Orch2 — folders only, no entities
  copy -Recurse Orch1:\* Orch2:\ -ExcludeEntities
  ```

- **`-UserMappingCsv`** — map source user names to destination user names while
  copying (see [Case B](#case-b-username-mapping-required) above for generating,
  validating, and using the mapping CSV).

### Copying Queue Items

Queue **definitions** are copied along with their folders by `copy -Recurse`,
but the **items inside** them are not — the Orchestrator API does not move queue
transactions during a folder copy. After the folders are in place, copy the
items separately with `Copy-OrchQueueItem`.

> **⚠ Stop the source queue's processing before you move its items.** Each queue item is
> a transaction, so if a robot keeps pulling items from the source while you copy them to
> the destination — or a trigger starts a new job — the same transaction is processed in
> two places. Before moving: **stop the running jobs** that consume the source queue and
> **disable the triggers that start them** — `Disable-OrchTrigger` covers both time
> triggers and queue triggers. Re-enable processing on the destination side once the move
> is complete.

> **Only `New` items are copied.** Items with any other status (InProgress,
> Failed, Successful, Retried, Abandoned, Deleted) are silently skipped, because
> only pending work is meaningful to carry to a fresh tenant. Processing history
> and logs cannot be migrated via API.

The procedure, end to end:

1. **Stop processing on the source.** Stop the jobs that consume the source queue and
   disable the time/queue triggers that start them (`Disable-OrchTrigger`), so the same
   transaction is never processed in two places during the move.
2. **Copy the folders and queue definitions first.** `copy Orch1:\ Orch2:\ -Recurse`
   brings the folder tree and the queue *definitions* across; or create the destination
   queue with `New-OrchQueue`. The destination queue must already exist before items are
   added.
3. **Copy the items** with `Copy-OrchQueueItem <source-queue> <destination-folder>`.
   **Only items whose Status is `New` are copied** — any other status (InProgress,
   Failed, Successful, Retried, Abandoned, Deleted) is silently skipped.
4. **Move (delete the copied items from the source).** Pipe the output into
   `Remove-OrchQueueItem`; this marks the copied items as `Deleted` so they are not
   processed again (see *Moving items* below).

When the current folder is the source, `-Path` can be omitted. The first
positional argument is the source queue name (wildcards / comma-separated lists
allowed), the second is the destination folder:

```powershell
# Same tenant, current folder (Shared) -> Dept#2, single queue
PS Orch1:\Shared> Copy-OrchQueueItem TestQueue2 Dept#2

# All queues matching a wildcard, current folder -> Dept#2
PS Orch1:\Shared> Copy-OrchQueueItem Test* Dept#2
```

Use `-Path` to name the source folder explicitly, and a drive-qualified
`-Destination` to copy across Orchestrator instances:

```powershell
# Cross-instance: Orch1 Shared -> Orch2 Shared
Copy-OrchQueueItem -Path Orch1:\Shared TestQueue2 Orch2:\Shared

# All folders on Orch1 -> a single destination folder
PS Orch1:\> Copy-OrchQueueItem -Recurse Test* Dept#2
```

Preview first with `-WhatIf`; it lists each queue that would be copied without
moving any items:

```powershell
PS Orch1:\Shared> Copy-OrchQueueItem TestQueue2 Dept#2 -WhatIf
```

Notes:

- The destination queue must already exist (copy the folders first, or create
  the queue with `New-OrchQueue`). Items are added to the **existing** queue,
  not appended to a freshly created one.
- Items are copied in batches of 100 via the `BulkAddQueueItems` API, with a
  601 ms pause between calls to avoid overloading the server. Large queues take
  proportionally longer.
- Required permissions: `Queues.View` + `Transactions.View` on the source,
  `Queues.Edit` + `Transactions.Create` on the destination.

#### Moving items (copy, then delete the copied ones)

`Copy-OrchQueueItem` returns the source items it **successfully copied**. Each queue
item is a transaction, so once it has been copied to another queue or tenant it must be
removed from the source — otherwise it would be processed twice. Pipe the output
straight into `Remove-OrchQueueItem` for a transaction-safe move:

```powershell
PS Orch1:\Shared> Copy-OrchQueueItem TestQueue2 Dept#2 | Remove-OrchQueueItem
```

Only the items that actually copied are deleted. Items the server rejects are reported
as warnings (with the reason) and are **not** included in the output, so they stay in
the source for you to handle — they are never deleted by the piped `Remove`. The most
common rejection is a **duplicate `Reference`** when the destination enforces unique
references; re-copying will not fix it, so inspect the warning and decide per item (the
item may already have been moved).

> **`Remove-OrchQueueItem` marks items as `Deleted`; it does not physically remove them.**
> The source queue still holds them with Status `Deleted`. That is what makes the move
> safe and idempotent: `Deleted` items are skipped by processing and by
> `Copy-OrchQueueItem` (which copies only `New` items), so a moved transaction never runs
> again, and re-running the move never copies an already-moved item a second time.

See [`Copy-OrchQueueItem`](help/en-US/Copy-OrchQueueItem.md) and
[`Import-OrchQueueItem`](help/en-US/Import-OrchQueueItem.md) for the full
parameter reference.

### Copying Bucket Files

Storage bucket **definitions** are copied along with their folders by
`copy -Recurse` (or selectively with `Copy-OrchBucket`), but the **files
inside** them are not — `copy` recreates the bucket *configuration* on the
destination, not its contents. After the buckets are in place, copy the files
separately with `Copy-OrchBucketItem`.

The procedure, end to end:

1. **Copy the folders and bucket definitions first.** `copy OldOrch:\ NewOrch:\ -Recurse`
   brings the folder tree and the bucket *definitions* across (or copy them selectively
   with `Copy-OrchBucket -Recurse`). The destination buckets must exist before files are
   added.
2. **Copy the files** with `Copy-OrchBucketItem`. It streams each file straight from the
   source to the destination — no local staging — preserving the folder/bucket structure
   and each file's full in-bucket path.

```powershell
# 1. Copy folder tree + bucket definitions (also done by the master copy -Recurse)
Copy-OrchBucket     -Path OldOrch:\ -Destination NewOrch:\ -Recurse

# 2. Copy the files into them, drive-to-drive (* * = all buckets, all files)
Copy-OrchBucketItem -Path OldOrch:\ * * -Destination NewOrch:\ -Recurse
```

Preview first with `-WhatIf`; it lists each file that would be copied without
transferring anything.

Notes:

- **The destination bucket must already exist** (step 1). A file whose destination bucket
  is missing is reported as a warning and skipped — create the bucket first with
  `Copy-OrchBucket`.
- **`-DestinationBucket`** redirects a single source bucket's files into a differently-named
  destination bucket (it cannot be combined with `-Recurse`).
- **`Copy-OrchBucketItem` emits the source files it copied**, so you can pipe into
  `Remove-OrchBucketItem` for a copy-then-delete move:
  `Copy-OrchBucketItem MyBucket * NewOrch:\Shared | Remove-OrchBucketItem`.
- **External storage providers** (Amazon S3, Azure Blob, MinIO, etc.): the files live in
  the customer's own storage, not in Orchestrator, so the copy recreates only the bucket
  *configuration*. After copying, reset the bucket's credential with `Update-OrchBucket` —
  the copy sets a placeholder `Password` and warns you — and confirm the destination bucket
  points at the correct external storage. Files already in external storage do not need
  re-uploading unless you are also moving the underlying storage. `Copy-OrchBucketItem`
  handles this for you: when the source and destination buckets resolve to the **same**
  external storage object it skips each file (warning once per bucket) instead of streaming
  it onto itself, and it only transfers bytes when they point to **different** storage.
- **Need the files on local disk** (backup, inspection, or editing before upload)? Use
  `Export-OrchBucketItem` to download them and `Import-OrchBucketItem` to upload them back,
  instead of the direct `Copy-OrchBucketItem`. The export layout
  (`<folder path>\<bucket name>\<file>`) is exactly what `Import-OrchBucketItem -Recurse`
  expects, so the two round-trip without reshuffling the staging folder.

See [`Copy-OrchBucketItem`](help/en-US/Copy-OrchBucketItem.md),
[`Export-OrchBucketItem`](help/en-US/Export-OrchBucketItem.md) and
[`Import-OrchBucketItem`](help/en-US/Import-OrchBucketItem.md) for the full
parameter reference.

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

### Your Own Portal Preferences and Notification Subscriptions

Portal preferences (theme, language, favorites) and notification subscriptions
are **per user** and are *not* part of the tenant copy (`copy -Recurse`). These
cmdlets are also **self-only** — they act on the connected user, so you can carry
**your own** settings to a new organization, but you cannot migrate other users'
preferences.

```powershell
# Portal preferences (theme, language, ...)
Copy-PmUserPreference -Path OldOrch: -Destination NewOrch:

# Notification subscriptions (which events notify you, by InApp / Email)
Copy-PmNotificationSubscription -Path OldOrch: -Destination NewOrch:
```

Both resolve "you" per organization from each drive's token and take effect on
your next sign-in to the destination. Notification topics are matched by name
(mandatory topics are skipped). A preference written this way is stored
correctly, but an already-open browser keeps showing the previous theme/language
until you re-sign-in or clear the site's local storage (see
[Troubleshooting](90-Troubleshooting.md)). Automation Cloud only.

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
feeds, processes, assets, queues, queue items, triggers, API triggers, event
triggers, test sets, test schedules, test data queues, action catalogs

Of these, **queue items**, **storage bucket files** and **event triggers** are not
carried by a folder copy — see [Important Notes](#important-notes).

### Entities Containing Usernames

Entities requiring `-UserMappingCsv` in Case B (username mapping):

| Entity | Properties Containing Usernames |
|---|---|
| User (OrchUser) | UserName, FullName, EmailAddress |
| Folder User (OrchFolderUser) | UserEntity.UserName, UserEntity.FullName |
| Asset (OrchAsset, per-user/per-machine) | UserName, MachineName |
| Credential Asset (OrchAsset, Credential type) | CredentialUsername |

### Entities Containing Passwords/Secrets

Entities requiring post-processing because secrets are never returned by the API:

| Entity | Property |
|---|---|
| User / Unattended Robot (OrchUser) | UR_Password |
| Machine — Confidential (OrchMachine) | ClientSecret |
| Credential Store (OrchCredentialStore) | Secrets in AdditionalConfiguration |
| Credential Asset (OrchAsset, Credential type) | CredentialPassword |
| Secret Asset (OrchAsset, Secret type) | SecretValue — re-set with `Set-OrchSecretAsset` |
| Storage Bucket (OrchBucket) | Password |
| Webhook (OrchWebhook) | Secret |

See [Credentials & Secrets](08-CredentialsAndSecrets.md) for the full table with the
re-set cmdlet for each entity, plus details on credential assets.

### Important Notes

- **Destination tenant library feed settings**: this decides which feed copied
  libraries land in — a copy always writes to the destination tenant's default feed.
  Check and set it with:

  ```powershell
  # Check current setting
  Get-OrchSetting -Path NewOrch: 'Deployment.Libraries.FeedScope'

  # Land copied libraries in the tenant feed
  Set-OrchSetting -Path NewOrch: -Name 'Deployment.Libraries.FeedScope' -Value 'Tenant'
  ```

  Valid values for `Deployment.Libraries.FeedScope`:
  | Value | Description |
  |---|---|
  | `Host` | Only host feed — copied libraries land in the host feed |
  | `Tenant` | Only tenant feed |
  | `All` | Both host and tenant feeds — copied libraries land in the tenant feed |

  See [Library Feed Setting](#library-feed-setting) for the two caveats that apply
  when the destination is set to `Host` (Automation Suite unverified; host-feed
  imports cannot be deleted back out through the tenant API).
- **Entity dependencies**: Some entities reference other entities, so copy
  order matters. For example, processes depend on packages, so packages must
  be copied first.
- **Each copy operation is independent**: If copying one entity fails, it does
  not affect the copying of other entities. Cross-version and cross-platform
  migrations (e.g., Automation Cloud to on-premises) will produce errors for
  entities that are incompatible with the destination. These errors are
  expected and can be reviewed after the migration completes. Fix the
  underlying issues and re-run the migration. An entity already at the
  destination is never overwritten, but it is not *silently* skipped either:
  the copy posts the create, the destination rejects the duplicate name, and
  that surfaces as a non-terminating error for that entity while the run
  continues. So a re-run is safe, and produces one error per entity that was
  already copied. Folders and folder-user assignments are the exception —
  existing ones are reused (roles are merged) — as are queue items and bucket
  files, which are genuinely idempotent and simply resume.
- **Triggers are copied disabled**: every copied time trigger and queue trigger
  arrives with `Enabled = false`, with a warning per trigger. Nothing starts
  running in the destination tenant until you enable them — see
  [Enabling the Copied Triggers](#step-3-enable-the-copied-triggers).
- **Event triggers**: there is no `Copy-OrchEventTrigger`, and event triggers are
  not included in a folder copy. They reference an Integration Service
  connection, which cannot be created through an API (authorising a connection
  is interactive), so they must be recreated at the destination. Time triggers
  and API triggers *are* copied. A copy warns once per folder that holds event
  triggers, with the count, and `-WhatIf` reports it too — so the omission shows
  up in the run rather than as a process that never starts in the new tenant.
  List what you have before migrating:
  `Get-OrchEventTrigger -Path OldOrch:\ -Recurse`.
- **Personal workspaces**: To migrate personal workspaces, exploration must be
  started in the Orchestrator Web UI on both sides, followed by running
  `Clear-OrchCache`. Personal workspaces can only be automatically copied when
  the folder names are the same (i.e., when the usernames are the same). When
  usernames differ, copy folder entities individually:

```powershell
Copy-OrchPackage -Path "OldOrch:\user1's workspace" * * -Destination "NewOrch:\newname's workspace"
Copy-OrchProcess -Path "OldOrch:\user1's workspace" * -Destination "NewOrch:\newname's workspace"
Copy-OrchAsset -Path "OldOrch:\user1's workspace" * -Destination "NewOrch:\newname's workspace"
# ... and other folder entities similarly
```

- **Queue SLA auto-assignment**: When copying a queue that has an associated
  process (ReleaseId) but no SLA, a default SLA of 24 hours is automatically
  set to preserve the process association. Review and adjust the SLA after
  migration if needed.
- **Queue items**: Due to API constraints, queue items are not automatically
  copied when copying folders. Copy them separately using
  `Copy-OrchQueueItem` (only Status=New items can be copied). See
  [Copying Queue Items](#copying-queue-items) for the procedure and examples.
- **Storage bucket files**: Like queue items, the files inside a bucket are not
  copied when copying folders — only the bucket definition is. Copy the files
  separately with `Copy-OrchBucketItem` (drive-to-drive, no local staging). See
  [Copying Bucket Files](#copying-bucket-files) for the procedure and examples.
- **Logs**: Due to API constraints, logs cannot be copied.
- **Choosing between Case A and Case B**: If usernames are the same on source
  and destination, use Case A. If usernames differ, always use Case B with
  `-UserMappingCsv`. Without the mapping, copy cmdlets will fail to find users
  and user information is lost from copied entities.

---

## Case Study: Two-Phase Migration

*When the migration does not fit in one maintenance window.*

**The situation.** A large customer moving from an on-premises MSI Orchestrator to
Automation Suite, with a few hundred unattended robots. Production cannot stop during
the working week, and keeping two Orchestrators alive side by side for a gradual
migration is not acceptable either — so the switch has to happen over a single weekend.
There is far more to move than that window allows if it is all done at once.

**The split.** Migrate configuration during the week before; migrate only live data in
the window. This is not a special mode — it is how the copy already behaves, so it needs
no extra options. In this example the two tenants are mounted as `Msi:` (the source) and
`Suite:` (the destination):

| When | What moves | Command |
|---|---|---|
| Week before (production still running) | tenant entities, folders, and every folder entity — processes, assets, queues, bucket definitions, triggers, ... | `copy -Recurse Msi:\ Suite:\` |
| Week before | re-enter every secret (see [Entities Containing Passwords/Secrets](#entities-containing-passwordssecrets)) | `Import-Csv c:\cred-assets.csv \| Set-OrchCredentialAsset` |
| Cutover weekend | queue items with Status `New` | `Copy-OrchQueueItem -Path Msi:\ -Recurse * -Destination Suite:\` |
| Cutover weekend | storage bucket files | `Copy-OrchBucketItem -Path Msi:\ -Recurse * * -Destination Suite:\` |
| Cutover weekend | package versions published since the pre-copy | `Copy-OrchPackage -Path Msi:\ -Recurse * * -Destination Suite:\` |
| Cutover weekend | enable the destination triggers | `Enable-OrchTrigger -Path Suite:\ -Recurse *` |

Run each of them with `-WhatIf` first.

**Why the pre-copy is safe**

- A folder copy carries queue *definitions* and bucket *definitions*, never their
  contents, so the live data genuinely stays behind until the weekend.
- Time and queue triggers arrive disabled, so the prepared tenant does not start doing
  work of its own. Disable the copied API triggers yourself — they keep the source's
  state (see [Step 3](#step-3-enable-the-copied-triggers)).
- Re-entering secrets is the most manual step of any migration. Moving it into the quiet
  week is the single biggest reason to split the migration at all.

**What the plan must account for**

- **The freeze is mandatory, not a convenience.** The copy does not re-synchronise: an
  entity already at the destination is not updated to match a changed source (see
  [Important Notes](#important-notes)). Anything edited after the pre-copy has to be
  re-applied by hand.
- **Verify that the freeze held.** At the end of the week, run the
  [Phase 4](#phase-4-post-migration-verification) comparison. Rows reported as `<>` are
  exactly the entities that changed despite the freeze; fix those few with `Set-Orch*` /
  `Update-Orch*`.
- **The catch-up copy is cheap.** `Copy-OrchPackage` checks the destination first and
  skips a version that is already there without downloading it, so re-running it with a
  wildcard transfers only what is new.
- **Stop the source before moving queue items.** Each item is a transaction: stop the
  consuming jobs and `Disable-OrchTrigger` on the source first, then
  `Copy-OrchQueueItem | Remove-OrchQueueItem` for a transaction-safe move.
- **Event triggers do not migrate.** Inventory them before the weekend
  (`Get-OrchEventTrigger -Path Msi:\ -Recurse`) and plan to recreate them by hand.
- **Rehearse the whole sequence** end to end against a non-production destination first.
  The individual steps are well-trodden; this particular ordering of them is newer.
