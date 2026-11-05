---
title: Other Providers (DU & Test Manager)
nav_order: 60
permalink: /other-providers/
---

# UiPathOrch Module - Other Providers Guide

- [Overview](#overview)
- [Drives and Mounting](#drives-and-mounting)
- [Navigation Model](#navigation-model)
- [Document Understanding (`…Du:`)](#document-understanding-du)
- [Test Manager (`…Tm:`)](#test-manager-tm)
  - [Copying a project to another tenant](#copying-a-project-to-another-tenant)
- [What's Shared with the Orchestrator Provider](#whats-shared-with-the-orchestrator-provider)

## Overview

The module registers **three** PowerShell providers, not one:

| Provider | Drive | Covers |
|---|---|---|
| `UiPathOrch` | `<Name>:` | Orchestrator (folders, processes, queues, assets, triggers, test data queues, …) — the main provider used everywhere else in these docs. |
| `UiPathOrchDu` | `<Name>Du:` | **Document Understanding** — projects and their extractors, document types, classifiers, roles, and users. |
| `UiPathOrchTm` | `<Name>Tm:` | **Test Manager** — projects and their test cases, test sets, requirements, test executions, settings, and permissions. |

The Du and Tm drives are **derived from** an Orchestrator drive — each is a "shadow"
drive of its parent and reuses the parent's connection, authentication, and config
entry. You do not configure them separately.

## Drives and Mounting

`Import-OrchConfig` mounts a `…Du:` and a `…Tm:` drive **alongside every Orchestrator
drive**, named by appending `Du` / `Tm` to the Orchestrator drive name. `Get-OrchPSDrive`
lists only the `UiPathOrch` drives; use `Get-PSDrive` to see all three providers:

```powershell
PS C:\> Get-PSDrive | Where-Object { $_.Provider -match 'UiPathOrch' } |
            Select-Object Name, @{n='Provider';e={$_.Provider.Name}}, Root

Name    Provider     Root
----    --------     ----
Orch1   UiPathOrch   Orch1:\
Orch1Du UiPathOrchDu Orch1Du:\
Orch1Tm UiPathOrchTm Orch1Tm:\
```

Because the shadow drives share the parent's connection, they require the parent
Orchestrator drive to be connected, and the external application must be granted the
**Document Understanding** / **Test Manager** API scopes (in addition to the
Orchestrator scopes) for the corresponding cmdlets to work.

Both are Automation Cloud / Automation Suite features.

## Navigation Model

Each shadow drive is a navigation provider whose **child items are projects**:

```powershell
# Document Understanding projects
PS C:\> Get-ChildItem Orch1Du:\

   Directory: Orch1Du:\

name        createdOn           description
----        ---------           -----------
Invoices    2025/07/21 22:26:19
Receipts    2025/03/11 13:53:34
Predefined  0001/01/01 9:00:00  Pretrained models for standard scenarios.

# Test Manager projects (identified by their prefix)
PS C:\> Get-ChildItem Orch1Tm:\

   Directory: Orch1Tm:\

projectPrefix name              isActive created             description
------------- ----              -------- -------             -----------
ACME          ACME Regression   True     2022/10/19 10:51:54
WEB           Web Checkout      True     2022/10/24 15:01:59
```

A cmdlet is then either **drive-level** or **project-scoped**:

- **Drive-level** cmdlets take the bare drive (`-Path Orch1Du:` / `-Path Orch1Tm:`):
  `Get-DuRole`, `Get-TmServerInfo`, `Get-TmConfiguration`.
- **Project-scoped** cmdlets need a project path (`-Path <Drive>:\<Project>`), where the
  project is the DU project **name** or the TM project **prefix**:
  `Get-DuExtractor`, `Get-DuDocumentType`, `Get-DuClassifier`, `Get-DuUser`,
  `Get-TmTestCase`, `Get-TmTestSet`, `Get-TmRequirement`, `Get-TmTestExecution`,
  `Get-TmProjectSetting`, `Get-TmProjectPermission`.

Running a project-scoped cmdlet against the bare drive fails with *"Use Set-Location …
or specify the target folders using -Path"* — point it at a project (or `Set-Location`
into one first).

## Document Understanding (`…Du:`)

A DU project bundles the ML model pieces and the people who can use them.

```powershell
# Extractors available in a project (pretrained + custom)
PS C:\> Get-DuExtractor -Path Orch1Du:\Predefined

   Project: Orch1Du:\Predefined

id              name                      status
--              ----                      ------
1040            1040                      Available
1040_schedule_c 1040 Schedule C (Preview) Available

# Document types defined in a project
PS C:\> Get-DuDocumentType -Path Orch1Du:\Invoices | Select-Object name, detailsUrl

# Classifiers in a project (may be empty)
PS C:\> Get-DuClassifier -Path Orch1Du:\Invoices

# Roles are organization-level, so they take the bare drive
PS C:\> Get-DuRole -Path Orch1Du:

# Users / role assignments are per project
PS C:\> Get-DuUser -Path Orch1Du:\Invoices | Select-Object displayName, type, roleAssignmentDtos
```

Access can be changed with `Add-DuUser` (assign a directory user/group a DU role on a
project) and `Remove-DuRoleFromDuUser`. See the cmdlet table in
[Cmdlet Reference §24](04-CmdletReference.md) and `Get-Help <Cmdlet>` for parameters.

## Test Manager (`…Tm:`)

```powershell
# Server / configuration are drive-level
PS C:\> Get-TmServerInfo -Path Orch1Tm:
Path    : Orch1Tm:\
version : 26.5.0.12156972
type    : Server
status  : OK

# Test cases in a project (by prefix)
PS C:\> Get-TmTestCase -Path Orch1Tm:\ACME | Select-Object objKey, name, version, updated

# Test sets, requirements, executions, and settings — all per project
PS C:\> Get-TmTestSet      -Path Orch1Tm:\ACME
PS C:\> Get-TmRequirement  -Path Orch1Tm:\ACME
PS C:\> Get-TmTestExecution -Path Orch1Tm:\ACME
PS C:\> Get-TmProjectSetting -Path Orch1Tm:\ACME |
            Select-Object projectPrefix, maxNumberOfTestSteps, projectTimeZone
PS C:\> Get-TmProjectPermission -Path Orch1Tm:\ACME
```

Test Manager can be written to as well: `New-Item Orch1Tm:\PREFIX` creates a project;
`New-`, `Update-` and `Remove-` cmdlets exist for requirements, test cases, test sets and
manual test steps; and `Add-` / `Remove-TmTestSetTestCase` and `-TmRequirementTestCase`
assign test cases. All support `-WhatIf`. See
[Cmdlet Reference §25](04-CmdletReference.md) for the full list.

### Copying a project to another tenant

`Copy-Item` copies a whole project, also to another tenant or organization. The
destination keeps the source's prefix, or takes the one you name:

```powershell
# To another tenant, keeping the prefix ACME
PS C:\> Copy-Item Orch1Tm:\ACME Orch2Tm:\

# Within the same tenant, under a new prefix
PS C:\> Copy-Item Orch1Tm:\ACME Orch1Tm:\ACME2

# Only create (or find) the destination project, without its contents
PS C:\> Copy-Item Orch1Tm:\ACME Orch2Tm:\ -ExcludeEntities
```

It copies in the order of Test Manager's menu:

1. Requirements.
2. Test cases, with their steps, pre-/post-conditions and foreign reference.
3. Test sets, with their coverage setting.
4. The project settings that can be set: time zone and execution name format.

Each item comes with its labels and its test case assignments, which are re-created by id,
not by name.

Copying into a project that already exists adds to it, so a copy that stopped partway can
simply be run again:

- Items are matched with the destination's by name and, for test cases and requirements,
  foreign reference, both exactly. Test sets are matched by name only.
- A one-to-one match is reused rather than created again; a reused test case with no steps
  gets the source's steps.
- Names that are not unique on either side cannot be matched one-to-one. Those items are
  skipped with a warning, which lists their objKeys. `-Force` creates them as new items
  instead.
- A project setting is copied only while the destination still has its default.

Not copied, because they point into the source's Orchestrator:

- Linked automation.
- A test set's folder, robot, machine and environment variables.
- The project's default folder.

Each of these is reported with a warning. Custom fields, attachments, executions and defects
are not copied either, without a warning.

To copy only some items, use `Copy-TmRequirement`, `Copy-TmTestCase` or `Copy-TmTestSet`
with a name pattern. Each asks for confirmation item by item, so `-WhatIf` lists exactly
what would be copied:

```powershell
PS C:\> Copy-TmTestCase -Path Orch1Tm:\ACME Login* Orch2Tm:\ACME -WhatIf
```

Copied test cases join the destination's counterparts of the test sets and requirements
they belong to. `Copy-TmTestSet` and `Copy-TmRequirement` assign the test cases that
already exist at the destination. So the result is the same whichever cmdlet runs first.

## What's Shared with the Orchestrator Provider

Because the shadow drives derive from their parent Orchestrator drive:

- **One connection and config entry.** Editing the parent drive in `UiPathOrchConfig.json`
  and re-running `Import-OrchConfig` re-mounts all three; there is no separate Du/Tm
  configuration. Connection problems are diagnosed exactly as for the parent drive (see
  [Troubleshooting](90-Troubleshooting.md)).
- **`Clear-OrchCache`** clears the Du/Tm caches too — run it before retrying after an
  error or when project/entity lists look stale.
- **The `Path` property** on every emitted object records the originating drive and
  project (e.g. `Orch1Tm:\ACME`), the same way it does on Orchestrator entities — pipe it
  back into a cmdlet to keep context.
- **`Invoke-OrchApi`** can reach the Document Understanding / Test Manager service URLs
  from the parent drive when you need an endpoint no cmdlet wraps yet (use `-Verbose` to
  see the URL a Du/Tm cmdlet calls, then adapt).
