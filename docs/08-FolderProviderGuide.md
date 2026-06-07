---
title: Folder Provider (drives & folders)
nav_order: 8
permalink: /folder-provider/
---

# UiPathOrch Module - Folder Provider Guide

`UiPathOrch` is a PowerShell *navigation provider*: each connected tenant is a drive
(`Orch1:`, `Orch2:`, …) and the tenant's **folders are the provider's items**. That means
you navigate and manage folders with the same built-in cmdlets you use for the file system —
`Set-Location`, `Get-ChildItem`, `New-Item`, `Rename-Item`, `Move-Item`, `Copy-Item`,
`Remove-Item`, and `Get`/`Set`/`Clear-ItemProperty` — instead of folder-specific cmdlets.

Folder *contents* (processes, assets, queues, triggers, …) are managed with the `*-Orch*`
cmdlets documented in the [Cmdlet Reference](02-CmdletReference.md); this guide is about the
**folder tree itself**.

- [Drives and navigation](#drives-and-navigation)
- [Listing folders (`Get-ChildItem`)](#listing-folders-get-childitem)
- [Wildcards and the `-Path` parameter](#wildcards-and-the--path-parameter)
- [Creating folders (`New-Item`)](#creating-folders-new-item)
- [Renaming (`Rename-Item`)](#renaming-rename-item)
- [Moving (`Move-Item`)](#moving-move-item)
- [Copying (`Copy-Item`)](#copying-copy-item)
- [Deleting (`Remove-Item`)](#deleting-remove-item)
- [Folder Description (`Set-ItemProperty`)](#folder-description-set-itemproperty)
- [Cache behavior](#cache-behavior)
- [Quick reference](#quick-reference)

## Drives and navigation

`Import-OrchConfig` mounts one drive per configured tenant. `Get-OrchPSDrive` lists them.

```powershell
PS C:\> Import-OrchConfig
PS C:\> Set-Location Orch1:\      # cd into a tenant's root
PS Orch1:\> cd Shared            # into a subfolder
PS Orch1:\Shared> cd ..          # back to parent
PS Orch1:\> ii .                 # open the current folder in the browser
```

The drive root (`Orch1:\`) is the tenant; its child items are the top-level folders
(including each user's personal workspace). Folders nest arbitrarily.

## Listing folders (`Get-ChildItem`)

`dir` / `Get-ChildItem` lists **subfolders** of the target. Use `-Recurse` for the whole
subtree and `-Depth` to bound how deep:

```powershell
Get-ChildItem Orch1:\                 # top-level folders
Get-ChildItem Orch1:\Shared -Recurse  # Shared and every descendant folder
Get-ChildItem Orch1:\ -Depth 0        # direct children only (same as no -Recurse)
Get-ChildItem Orch1:\ -Depth 1        # children and grandchildren
```

`-Depth 0` is the direct children; each increment adds one more level. `-Depth` implies
recursion, so you do not also need `-Recurse`.

> `Get-ChildItem` returns **folders only** — it never lists processes, assets, or other
> folder contents. Use the corresponding `Get-Orch*` cmdlet for those.

## Wildcards and the `-Path` parameter

Folder paths accept wildcards, both for navigation and for the `-Path` parameter that the
folder-scoped `*-Orch*` cmdlets share:

```powershell
Get-ChildItem Orch1:\Shar*            # folders matching Shar*
Get-OrchAsset -Path Shared*           # assets in every folder matching Shared*
Get-OrchProcess -Path Dept#* -Recurse # processes under each matching folder, recursively
```

Wildcards resolve against the folders that actually exist, so `Shared*` may match several
folders (`Shared`, `Shared - Copy`, …). A literal name that contains a wildcard metacharacter
can be passed with `-LiteralPath`.

## Creating folders (`New-Item`)

```powershell
New-Item Orch1:\Shared\Reports -ItemType Directory
New-Item Orch1:\NewTopLevel -ItemType Directory
```

The parent folder must already exist. `-ItemType Directory` is accepted for familiarity;
folders are the only item type the provider creates.

## Renaming (`Rename-Item`)

`Rename-Item` changes a folder's name **in place** — it is not a move. `-NewName` is a leaf
name, not a path:

```powershell
Rename-Item Orch1:\Shared\Reports Archive       # Reports -> Archive
Rename-Item .\Reports .\Archive                 # the leading .\ (from tab completion) is fine
```

A `-NewName` that points somewhere else is **rejected** rather than silently applied — use
`Move-Item` to relocate a folder:

```powershell
PS Orch1:\src> Rename-Item .\sub3 ..\sub5
Rename-Item: '..\sub5' is not a valid new folder name. Supply a leaf name, not a path
(Rename-Item renames in place; use Move-Item to move).
```

(A fully-qualified new name is accepted only when it stays in the same parent folder, e.g.
`Rename-Item Orch1:\src\sub3 Orch1:\src\sub5`.)

## Moving (`Move-Item`)

`Move-Item` reparents a folder (with everything inside it) under another **existing** folder.
The whole subtree moves, so there is no `-Recurse`:

```powershell
Move-Item Orch1:\A\Reports Orch1:\B    # Reports (and its contents) become B\Reports
Move-Item Orch1:\B\Reports Orch1:\     # move it to the top level
```

The destination must be an existing folder on the **same** tenant. A non-existent destination,
or a folder on a different drive, is reported as an error (moving between tenants is a copy
operation — see below).

## Copying (`Copy-Item`)

`Copy-Item` copies a folder, its folder-scoped entities (processes, assets, queues, …), and —
with `-Recurse` — all of its subfolders. This is the building block of cross-tenant migration
(see the [Migration & Copy Guide](04-MigrationGuide.md)):

```powershell
Copy-Item Orch1:\Finance Orch2:\ -Recurse          # Finance subtree -> Orch2, with contents
Copy-Item Orch1:\Finance Orch2:\ -Recurse -ExcludeEntities   # folders only, no contents
Copy-Item Orch1:\ Orch2:\ -Recurse                 # whole tenant: tenant-level entities + every folder
```

- **`-Recurse`** includes subfolders. Without it, a folder is copied with its own entities but
  not its subfolders; a root-to-root copy without `-Recurse` copies only tenant-level entities
  (libraries, packages, credential stores, roles, users, machines, calendars, webhooks) and
  warns that folders were skipped.
- **`-ExcludeEntities`** copies the folder structure only.
- **`-UserMappingCsv`** translates per-user references (the same CSV used by `Copy-OrchAsset` /
  `Copy-OrchUser`) when copying across tenants with different directory user names.

`-WhatIf` previews what would be copied, including per-type counts for tenant entities.

## Deleting (`Remove-Item`)

`Remove-Item` deletes a folder **and everything it contains** on the server. The confirmation
mirrors the Orchestrator web delete dialog:

| The folder… | What happens |
|---|---|
| is empty | deleted with no prompt |
| has subfolders | PowerShell's standard *"…has children and the Recurse parameter was not specified"* prompt |
| has no subfolders but holds resources | a content-aware prompt listing the count of each resource type that will be removed |

```powershell
PS Orch1:\> Remove-Item .\Finance
Confirm folder deletion
The folder 'Orch1:\Finance' is not empty (Processes: 2, Triggers: 0, Assets: 5, Buckets: 0,
Queues: 1, Action Catalogs: 0). Deleting it permanently removes the folder and all of its
contents. Are you sure you want to continue?
```

`-Recurse` or `-Force` deletes without the prompt:

```powershell
Remove-Item Orch1:\Finance -Recurse           # no prompt
Remove-Item Orch1:\Temp* -Recurse -Force      # wildcard, unattended
```

> Deleting a folder is destructive and cascades to its contents. Prefer `-WhatIf` first, and
> reserve `-Force` for unattended scripts.

## Folder Description (`Set-ItemProperty`)

A folder's editable text fields are **DisplayName** and **Description**. DisplayName is changed
with `Rename-Item` (above); the **Description** is set with the property cmdlets:

```powershell
Set-ItemProperty Orch1:\Shared -Name Description -Value 'Shared automation assets'
Get-ItemProperty  Orch1:\Shared -Name Description       # read it back
Get-ItemProperty  Orch1:\Shared                         # Description + DisplayName
Clear-ItemProperty Orch1:\Shared -Name Description      # clear it (also: -Value '')
```

The Description also shows in the `Description` column of `dir`:

```powershell
PS Orch1:\> dir | Format-Table Id, DisplayName, Description
```

Only `Description` is settable this way. Setting `DisplayName` via `Set-ItemProperty` is
rejected with a pointer to `Rename-Item`.

## Cache behavior

The folder tree is cached after first use; repeated navigation and listing are instant.
After creating, renaming, moving, copying, or deleting folders the provider refreshes the
cache automatically. To force a refresh (for example after changes made in the web UI):

```powershell
Clear-OrchCache
```

> The very first folder listing right after `Import-OrchConfig` can come back empty while the
> connection warms up; a second call (or `Clear-OrchCache`) returns the full list.

## Quick reference

| Task | Command |
|---|---|
| Go to a folder | `Set-Location Orch1:\Shared` (`cd`) |
| List subfolders | `Get-ChildItem` (`dir`), `-Recurse`, `-Depth n` |
| Match folders by wildcard | `Get-ChildItem Orch1:\Shar*`, `-Path Shared*` |
| Create a folder | `New-Item Orch1:\A\B -ItemType Directory` |
| Rename a folder | `Rename-Item Orch1:\A\B NewName` |
| Move a folder | `Move-Item Orch1:\A\B Orch1:\C` |
| Copy a folder subtree | `Copy-Item Orch1:\A Orch2:\ -Recurse` |
| Delete a folder | `Remove-Item Orch1:\A\B` (`-Recurse` / `-Force` to skip prompt) |
| Set a folder's description | `Set-ItemProperty Orch1:\A -Name Description -Value '…'` |
| Read a folder's description | `Get-ItemProperty Orch1:\A -Name Description` |
| Open folder in browser | `ii .` |

See also: [Migration & Copy Guide](04-MigrationGuide.md) ·
[Other Providers (DU & Test Manager)](60-OtherProvidersGuide.md) · `Get-Help about_UiPathOrch`.
