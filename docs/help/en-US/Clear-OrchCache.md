---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Clear-OrchCache.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/21/2026
PlatyPS schema version: 2024-05-01
title: Clear-OrchCache
---

# Clear-OrchCache

## SYNOPSIS

Clears the in-memory cache on UiPathOrch drives at drive, tenant, or folder scope.

## SYNTAX

### __AllParameterSets

```
Clear-OrchCache [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse] [-Depth <uint>] [-AllDrives]
 [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

The UiPathOrch module caches entities retrieved from the Orchestrator on each drive to optimize response times and reduce the load on the Orchestrator server. In particular, this cache is used to display appropriate candidates for auto-completion of parameter values.

If you want the PowerShell console to reflect updates to entities on Orchestrator (such as creating or removing folders) made via Orchestrator Web or other external applications, clear the in-memory cache with this cmdlet.

### Scope dispatch

The cache exists at three scopes — per-organization, per-tenant (drive), and per-folder. The cmdlet picks the scope to clear from the shape of the `-Path` argument:

| `-Path` form | Cache cleared |
|---|---|
| (no `-Path`, on an Orch drive) | the current drive (full: tenant + all folders) |
| (no `-Path`, off Orch drives) | every drive (full) |
| `-AllDrives` | every drive (full); same as the off-drive default, explicit for scripts |
| `orch1:` | the entire drive (full) — backward-compat carve-out; the usual PowerShell "current folder of the drive" semantics are intentionally overridden so existing scripts that name only the drive continue to clear everything |
| `orch1:\` | tenant cache only (per-tenant + per-organization). Mirrors the root presentation surface: `Get-ChildItem orch1:\` shows tenant-scoped entities, so "clear what's visible at root" is exactly the tenant cache set |
| `orch1:\Shared` | per-folder cache for that folder only. Tenant and organization scopes are untouched |
| `.` | resolves to the current location, then classified as above |

`-Recurse` and `-Depth` extend the per-folder scope to subfolders, mirroring the other folder-aware cmdlets. They are no-ops on the drive-only, drive-root, and `-AllDrives` forms.

`-Path` supports tab completion for drive names (Orchestrator, Document Understanding, and Test Manager). Folder paths must be typed manually.

### Per-organization caches

Some caches (e.g., `PmUsers`, `DuRoles`) are keyed by Automation Cloud organization and are shared across drives in the same org. A drive-level or tenant-level clear on one drive drops these org-shared entries, which means a subsequent fetch on a sibling drive in the same organization will re-hit the API even though that sibling wasn't named on the command line. This is intentional — staleness on one drive implies staleness on all of them — but worth knowing when troubleshooting.

### Document Understanding / Test Manager drives

`orch1Du:` and `orch1Tm:` accept the drive-only and drive-root forms with the same semantics as Orch main drives. Project-path forms (`orch1Du:\<project>`, `orch1Tm:\<project>`) currently fall back to a drive-level clear with a verbose-stream note; per-project clearing is not yet implemented.

Clearing a DU or Tm drive also clears its parent Orch drive because the shadow drives share org/tenant state with the main drive; a stale parent would surface in a follow-up `Get-*` even after the user thought they discarded the data.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Clear cache on the current drive

```powershell
PS Orch1:\> Clear-OrchCache
```

Drive-level full clear on Orch1: (tenant + every cached folder + org-shared entries this drive participates in).

### Example 2: Clear cache on all drives

```powershell
PS C:\> Clear-OrchCache
```

Off Orch drives the default broadens to every registered UiPathOrch drive. Equivalent to `Clear-OrchCache -AllDrives`.

### Example 3: Clear specific drives

```powershell
PS C:\> Clear-OrchCache Orch1:, Orch2:
```

Drive-only form for two drives. The `:` (no path part after) is the marker that triggers the drive-level full clear, regardless of where each drive's current-folder pointer is set.

### Example 4: Clear only the current folder's cache

```powershell
PS Orch1:\Shared> Clear-OrchCache .
```

The common case: you've just modified something in `Shared` via the web UI and want the next `Get-OrchAsset` to re-fetch. `.` resolves to `Orch1:\Shared`, so only that folder's per-folder cache is dropped — the tenant cache (queues catalog, role list, etc.) and other folders' caches stay warm.

### Example 5: Clear only tenant-scoped entities

```powershell
PS C:\> Clear-OrchCache -Path Orch1:\
```

The drive root path form. Drops the per-tenant and per-organization caches (the entities that show up at the root of `Get-ChildItem Orch1:\`) but leaves every folder's per-folder cache intact.

### Example 6: Clear a folder and its subfolders

```powershell
PS C:\> Clear-OrchCache -Path Orch1:\Depts -Recurse
```

Per-folder clear for `Depts` and every descendant. Use `-Depth N` to cap the recursion depth.

### Example 7: Mix scopes in one call

```powershell
PS Orch1:\Shared> Clear-OrchCache ., Orch2:
```

`.` is folder-scope (current folder on Orch1:); `Orch2:` is drive-scope. Each `-Path` element is classified independently.

## PARAMETERS

### -Path

One or more target paths. The cache scope is inferred from the path shape (see DESCRIPTION). Drive-only forms (`orch1:`) clear the entire drive; drive-root forms (`orch1:\`) clear tenant + organization caches; folder paths clear only that folder's per-folder cache. Tab completion suggests drive names; folder paths must be typed manually.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Recurse

Extends a folder-path clear to all subfolders. No effect on drive-only, drive-root, or -AllDrives forms.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Depth

Limits how deep -Recurse walks. 0 means only the named folder (same as no -Recurse). Has no effect without -Recurse.

```yaml
Type: System.UInt32
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -AllDrives

Forces a drive-level full clear on every registered UiPathOrch drive, ignoring -Path and the current location. Equivalent in effect to running the cmdlet off all Orch drives with no -Path; the switch exists so scripts can express the intent explicitly.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- wi
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: False
SupportsWildcards: false
Aliases:
- cf
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

## NOTES

The `orch1:` (drive-only) form intentionally overrides PowerShell's usual "current folder of the drive" path resolution to preserve backward compatibility with existing scripts that named the drive expecting a full clear. To clear only the current folder of a drive, use the `.` form or an explicit folder path.

Per-organization caches are shared across drives in the same Automation Cloud organization; a clear on one drive drops the shared entries that sibling drives would have observed.

When clearing a Document Understanding or Test Manager drive, the parent Orchestrator drive cache is also cleared so that cross-referenced data stays consistent.

## RELATED LINKS

[Get-OrchPSDrive](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchPSDrive.md)
