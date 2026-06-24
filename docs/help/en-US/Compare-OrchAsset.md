---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchAsset
---

# Compare-OrchAsset

## SYNOPSIS

Compares assets between two folders or Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchAsset [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-ValueType <string[]>] [-Property <string[]>]
 [-UserMappingCsv <string>] [-Recurse] [-Depth <uint>] [-IncludeEqual] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares assets between a reference location (-Path, or the current folder) and a difference location (-DifferencePath) and reports what differs, in the spirit of Compare-Object but resolved over Orchestrator drives and folders. The primary use is migration verification: after copying assets between tenants or folders, confirm they landed with the same values.

Assets are matched by Name (not Id, which is tenant-local), and only migration-relevant properties are compared: ValueType, ValueScope, Value, Description, CredentialUsername, ExternalName, AllowDirectApiAccess, Tags, and per-user values (UserValues). Volatile fields such as Id, Key, creation/modification timestamps, and creator/modifier user ids are deliberately ignored so they do not drown out real differences.

Each result is an OrchComparison record with a SideIndicator:

- "<=" the asset exists only on the reference side (-Path).
- "=>" the asset exists only on the difference side (-DifferencePath).
- "<>" the asset exists on both sides but one or more compared properties differ; the Differences property lists each as "Property: 'reference' => 'difference'".
- "==" the asset is equal on every compared property. Emitted only with -IncludeEqual.

Two modes are selected by -DifferenceName. Without it (name-match), each reference asset is compared to the same-named asset in the corresponding difference folder; with -Recurse the source folder hierarchy is mirrored relative to -DifferencePath. With -DifferenceName (broadcast), every reference asset is compared to that single named asset in -DifferencePath, even when the names differ -- useful for diffing two differently named assets, including within one folder.

Credential and Secret values cannot be read back, so password and secret drift is invisible; the credential username is still compared. For cross-tenant comparisons, pass -UserMappingCsv (the same CSV consumed by Copy-OrchAsset) so per-user values are matched after translating reference user names to their difference-side equivalents.

Primary Endpoint: GET /odata/Assets

OAuth required scopes: OR.Assets or OR.Assets.Read (both sides)

Required permissions: Assets.View (both sides)

## EXAMPLES

### Example 1: Verify a folder migrated correctly

```powershell
PS C:\> Compare-OrchAsset * Orch2:\Finance -Path Orch1:\Finance
```

Compares every asset in Finance on Orch1 against the same-named asset in Finance on Orch2. Only differences are shown: "<=" for assets missing on Orch2, "=>" for extra assets on Orch2, and "<>" for assets whose values differ.

### Example 2: Include matching assets

```powershell
PS C:\> Compare-OrchAsset * Orch2:\Finance -Path Orch1:\Finance -IncludeEqual
```

Adds "==" rows for assets that match on every compared property, giving a full side-by-side picture.

### Example 3: Inspect the per-property differences

```powershell
PS C:\> Compare-OrchAsset * Orch2:\Finance -Path Orch1:\Finance | Where-Object SideIndicator -eq '<>' |
    Select-Object Name -ExpandProperty Differences
```

Lists only the changed assets and expands each one's property-level differences (property name, reference value, difference value).

### Example 4: Compare a whole tree recursively

```powershell
PS C:\> Compare-OrchAsset -Path Orch1:\Shared -DifferencePath Orch2:\Shared -Recurse
```

Walks Shared and all its subfolders on Orch1 and compares each against the mirrored relative folder under Shared on Orch2.

### Example 5: Compare two differently named assets

```powershell
PS C:\> Compare-OrchAsset -Path Orch1:\Finance -Name ApiKey_Old -DifferencePath Orch1:\Finance -DifferenceName ApiKey_New
```

Broadcast mode. Compares the single asset ApiKey_Old against ApiKey_New in the same folder, even though their names differ.

### Example 6: Check a set of assets against one golden asset

```powershell
PS C:\> Compare-OrchAsset -Path Orch1:\Finance -Name Conn* -DifferencePath Orch1:\Templates -DifferenceName GoldenConn
```

Broadcast mode with a wildcard reference. Compares every Conn* asset against the single GoldenConn asset, surfacing any that have drifted from the template.

### Example 7: Cross-tenant comparison with user mapping

```powershell
PS C:\> Compare-OrchAsset * Orch2:\Shared -Path Orch1:\Shared -UserMappingCsv c:user-mapping.csv
```

Compares across tenants and translates reference per-user value owners to their difference-side user names via the CSV, so remapped users do not show up as spurious UserValues differences. The mapping has no effect within one tenant. Use New-OrchUserMappingCsv to generate the file.

## PARAMETERS

### -Depth

Specifies the depth for recursion into the reference folders. A depth of 0 targets only the reference folder. When -Depth is specified, -Recurse is implied.

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

### -DifferenceName

Selects broadcast mode. When set, every reference asset is compared to this single named asset in -DifferencePath, even when the names differ. Single by design; comparing many reference assets against many named targets has no well-defined pairing.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DifferencePath

Specifies the difference (right) folder. This is a mandatory parameter. Can be a folder on the same Orchestrator instance (e.g., Orch1:\Production) or on a different instance (e.g., Orch2:\Shared) for cross-instance comparison.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IncludeEqual

Also emits "==" rows for assets that match on every compared property. Off by default; only differences are surfaced.

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

### -LiteralPath

Specifies the reference folder by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- PSPath
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Name

Filters the reference assets by name; supports wildcards. In name-match mode the same filter is applied to the difference side so that "=>" rows stay within the same name scope. Tab completion lists assets from the current folder.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Path

Specifies the reference (left) folder. If not specified, the current folder is used. Accepts a single string. Also binds the path of assets piped in by property name.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: true
Aliases: []
ParameterSets:
- Name: (All)
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Property

Restricts the comparison to the named properties. Valid names: ValueType, ValueScope, Value, Description, CredentialUsername, ExternalName, AllowDirectApiAccess, Tags, UserValues. Unrecognized names are warned about and ignored.

```yaml
Type: System.String[]
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

### -Recurse

Includes the reference folder and all its subfolders. Each is compared against the mirrored relative folder under -DifferencePath.

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

### -UserMappingCsv

Specifies the path to a user mapping CSV (SourceUserName,DestinationUserName) used to translate reference per-user value owners to their difference-side user names before comparing UserValues. The same CSV consumed by Copy-OrchAsset. No effect within one tenant/organization. Requires a filesystem path (not an Orch: drive path). Use New-OrchUserMappingCsv to generate it.

```yaml
Type: System.String
DefaultValue: ''
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

### -ValueType

Filters the assets to compare by value type (Text, Bool, Integer, Credential, Secret); supports wildcards.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: true
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the reference path to this cmdlet (the Path property).

### System.String[]

You can pipe entity names to this cmdlet (the Name property).

## OUTPUTS

### UiPath.PowerShell.Entities.OrchComparison

Returns one comparison record per asset, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject for downstream piping.

## NOTES

Full, bidirectional diffs come from the -Path / -DifferencePath form. Driving the reference side from the pipeline (Get-OrchAsset ... | Compare-OrchAsset) binds Name per piped item, so the comparison is reference-scoped: it reports "<=", "<>" and "==" for the piped assets but cannot surface "=>" (assets that exist only on the difference side). Use the -Path form to detect difference-side additions.

Assets are matched by Name, case-insensitively. Bool values are compared case-insensitively so a "True" versus "true" representation difference is not reported as drift.

Credential and Secret values cannot be read back, so password and secret drift is not detected; only the credential username, external name, and type are compared.

This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchAsset.md)
- [Copy-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchAsset.md)
- [Set-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-OrchAsset.md)
- [New-OrchUserMappingCsv](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchUserMappingCsv.md)
