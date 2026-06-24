---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchUser.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchUser
---

# Compare-OrchUser

## SYNOPSIS

Compares users between two Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchUser [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-Property <string[]>] [-UserMappingCsv <string>]
 [-IncludeEqual] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares users between a reference instance (-Path) and a difference instance (-DifferencePath). Users are tenant-level, so the reference and difference are drives, not folders, and there is no -Recurse.

Users are matched by user name and these properties are compared: Type, IsActive, LicenseType, ProvisionType, EmailAddress, RolesList (the assigned tenant roles, as an order-independent set), MayHaveUserSession, MayHaveUnattendedSession, and MayHavePersonalWorkspace. Passwords are never compared.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference user is compared to the same-named user on the difference instance. With -DifferenceName, every reference user is compared to that single named user.

For cross-tenant comparisons where the same person has different user names (for example a different directory), pass -UserMappingCsv (the same SourceUserName,DestinationUserName CSV consumed by Copy-OrchAsset) to translate the reference user names to their difference-side equivalents before matching. Without it, a renamed user shows up as a "<=" / "=>" pair. The mapping has no effect within one tenant/organization.

Primary Endpoint: GET /odata/Users

OAuth required scopes: OR.Users or OR.Users.Read (both sides)

Required permissions: Users.View (both sides)

## EXAMPLES

### Example 1: Verify users migrated between tenants

```powershell
PS C:\> Compare-OrchUser * Orch2: -Path Orch1:
```

Compares every user on Orch1 against the same-named user on Orch2, showing only the differences (for example a different role set or license type).

### Example 2: Cross-directory comparison with a user-name mapping

```powershell
PS C:\> Compare-OrchUser * Orch2: -Path Orch1: -UserMappingCsv c:user-mapping.csv
```

Translates reference user names to their difference-side equivalents before matching, so users renamed across directories are paired instead of appearing as missing/extra. Use New-OrchUserMappingCsv to generate the file.

## PARAMETERS

### -DifferenceName

Selects broadcast mode: every reference user is compared to this single named user in -DifferencePath, even when the names differ.

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

Specifies the difference (right) Orchestrator drive. Mandatory. Can be the same instance as -Path (for comparing two users via -DifferenceName) or a different instance.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

Also emits "==" rows for users that match on every compared property. Off by default.

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

Specifies the reference drive by literal path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item.

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

Filters by user name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Specifies the reference (left) Orchestrator drive. If not specified, the current drive is used.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
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

Restricts the comparison to the named properties. Valid names: Type, IsActive, LicenseType, ProvisionType, EmailAddress, RolesList, MayHaveUserSession, MayHaveUnattendedSession, MayHavePersonalWorkspace. Unrecognized names are warned about and ignored.

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

### -UserMappingCsv

Specifies the path to a user mapping CSV (SourceUserName,DestinationUserName) used to translate the reference user names to their difference-side equivalents before matching. The same CSV consumed by Copy-OrchAsset. No effect within one tenant/org. Requires a filesystem path (not an Orch: drive path). Use New-OrchUserMappingCsv to generate it.

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

Returns one comparison record per user, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Users are matched by user name, case-insensitively (after applying -UserMappingCsv to the reference side, if supplied). Passwords are never compared. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchUser](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchUser.md)
- [Compare-OrchRole](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchRole.md)
- [New-OrchUserMappingCsv](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/New-OrchUserMappingCsv.md)
