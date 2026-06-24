---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchWebhook.md
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/07/2026
PlatyPS schema version: 2024-05-01
title: Compare-OrchWebhook
---

# Compare-OrchWebhook

## SYNOPSIS

Compares webhooks between two Orchestrator instances and reports the differences.

## SYNTAX

### __AllParameterSets

```
Compare-OrchWebhook [-Name] <string[]> [-DifferencePath] <string> [[-DifferenceName] <string>]
 [-Path <string>] [-LiteralPath <string>] [-Property <string[]>] [-IncludeEqual]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Compares webhooks between a reference instance (-Path) and a difference instance (-DifferencePath). Webhooks are tenant-level, so the reference and difference are drives, not folders, and there is no -Recurse.

Webhooks are matched by Name (not the tenant-local Id) and these properties are compared: Description, Url, Enabled, SubscribeToAllEvents, AllowInsecureSsl, and Events. The subscribed events are normalized to an order-independent set of event types. The signing Secret is sensitive and is not compared.

Each result is an OrchComparison record with a SideIndicator: "<=" reference only, "=>" difference only, "<>" present on both sides but differing (with a per-property Differences breakdown), and "==" equal (only with -IncludeEqual). Without -DifferenceName each reference webhook is compared to the same-named webhook on the difference instance. With -DifferenceName, every reference webhook is compared to that single named webhook.

Primary Endpoint: GET /odata/Webhooks

OAuth required scopes: OR.Webhooks or OR.Webhooks.Read (both sides)

Required permissions: Webhooks.View (both sides)

## EXAMPLES

### Example 1: Verify webhooks migrated between tenants

```powershell
PS C:\> Compare-OrchWebhook * Orch2: -Path Orch1:
```

Compares every webhook on Orch1 against the same-named webhook on Orch2, showing only the differences (for example a different Url or a changed event subscription).

### Example 2: Check only the subscribed events

```powershell
PS C:\> Compare-OrchWebhook * Orch2: -Path Orch1: -Property Events,SubscribeToAllEvents
```

Restricts the comparison to the event subscription.

## PARAMETERS

### -DifferenceName

Selects broadcast mode: every reference webhook is compared to this single named webhook in -DifferencePath, even when the names differ.

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

Specifies the difference (right) Orchestrator drive. Mandatory. Can be the same instance as -Path (for comparing two webhooks via -DifferenceName) or a different instance.

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

Also emits "==" rows for webhooks that match on every compared property. Off by default.

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

Filters the reference webhooks by name; supports wildcards. In name-match mode the same filter is applied to the difference side.

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

Restricts the comparison to the named properties. Valid names: Description, Url, Enabled, SubscribeToAllEvents, AllowInsecureSsl, Events. Unrecognized names are warned about and ignored.

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

Returns one comparison record per webhook, with SideIndicator, Name, the single-sided Path and DifferencePath, the per-property Differences (on "<>" rows), and the underlying ReferenceObject / DifferenceObject.

## NOTES

Webhooks are matched by Name, case-insensitively. The subscribed events are compared as an order-independent set; the signing Secret is never compared. This cmdlet is read-only and does not support -WhatIf / -Confirm.

## RELATED LINKS

- [Get-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchWebhook.md)
- [Copy-OrchWebhook](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchWebhook.md)
- [Compare-OrchAsset](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Compare-OrchAsset.md)
