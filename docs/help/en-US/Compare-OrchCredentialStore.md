---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchCredentialStore.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchCredentialStore
---

# Compare-OrchCredentialStore

## SYNOPSIS

Compares credential stores between two Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchCredentialStore [[-Path] <string>] [-DifferencePath] <string> [-LiteralPath <string>]
 [-DifferenceName <string>] [-Name <string[]>] [-Property <string[]>] [-IncludeEqual]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares credential stores between a reference instance (-Path) and a difference instance (-DifferencePath). Credential stores are tenant-level, so the reference and difference are drives, not folders, and there is no -Recurse.

Credential stores are matched by Name (not the tenant-local Id) and these properties are compared: Type, ProxyType, HostName, and AdditionalConfiguration. Secrets are not stored in Orchestrator and are never compared.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference store is compared to the same-named store on the difference instance. With -DifferenceName, every reference store is compared to that single named store.

Primary Endpoint: GET /odata/CredentialStores

OAuth required scopes: OR.Administration or OR.Administration.Read (both sides)

Required permissions: CredentialStores.View (both sides)

## EXAMPLES

### Example 1: Verify credential stores migrated between tenants

```powershell
PS C:\> Compare-OrchCredentialStore Orch1: Orch2:
```

Compares every credential store on Orch1 against the same-named store on Orch2, showing only the differences.

### Example 2: Include matching stores

```powershell
PS C:\> Compare-OrchCredentialStore Orch1: Orch2: -IncludeEqual
```

Adds "==" rows for stores that match on every compared property.

## PARAMETERS

### -Path

Specifies the reference (left) Orchestrator drive. If not specified, the current drive is used.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
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

### -DifferencePath

Specifies the difference (right) Orchestrator drive. Mandatory. Can be the same instance as -Path (for comparing two stores via -DifferenceName) or a different instance.

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

### -DifferenceName

Selects broadcast mode: every reference store is compared to this single named store in -DifferencePath, even when the names differ.

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

### -Name

Filters the reference stores by name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Property

Restricts the comparison to the named properties. Valid names: Type, ProxyType, HostName, AdditionalConfiguration. Unrecognized names are warned about and ignored.

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

### -IncludeEqual

Also emits "==" rows for stores that match on every compared property. Off by default.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe the reference drive via the Path property.

### System.String[]

You can pipe credential store names via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.OrchComparison

Returns one comparison record per credential store, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Credential stores are matched by Name, case-insensitively. Secrets held by the store are not exposed by the API and are never compared. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

[Get-OrchCredentialStore](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchCredentialStore.md)

[Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
