---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmNotificationSubscription.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/02/2026
PlatyPS schema version: 2024-05-01
title: Set-PmNotificationSubscription
---

# Set-PmNotificationSubscription

## SYNOPSIS

Subscribes or unsubscribes you to a notification topic for a delivery mode.

## SYNTAX

### Default (Default)

```
Set-PmNotificationSubscription [-Path <string[]>] [-LiteralPath <string[]>]
 [-Topic] <string> [-Mode] <string> [-Subscribed] <string> [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Sets the connected user's subscription for one notification `(topic, mode)` pair. `-Topic` accepts the topic name (e.g. `Apps.Shared`, as shown by `Get-PmNotificationSubscription`) or its GUID; names are resolved per drive from the topic list. `-Mode` is `InApp` or `Email`. `-Subscribed` is `$true` / `$false`.

The cmdlet always acts on the connected user's *own* subscriptions (there is no `-UserName`). Each pipeline row is one `(topic, mode)`; rows for the same drive are accumulated and sent as a single request, so `Get-PmNotificationSubscription | ... | Set-PmNotificationSubscription` coalesces into one call per drive.

## EXAMPLES

### Example 1: Turn off an email notification

```powershell
PS Orch1:\> Set-PmNotificationSubscription Apps.Shared Email $false
```

### Example 2: Unsubscribe from every Email notification

```powershell
PS Orch1:\> Get-PmNotificationSubscription -Mode Email |
    Where-Object { -not $_.IsMandatory } |
    ForEach-Object { $_.IsSubscribed = $false; $_ } |
    Set-PmNotificationSubscription
```

## PARAMETERS

### -Topic

The topic to change: its name (e.g. `Apps.Shared`) or display name, or its GUID. Positional (position 0).

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
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

### -Mode

Delivery mode: `InApp` or `Email`. Positional (position 1).

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Subscribed

`true` to subscribe, `false` to unsubscribe (tab-completes `true` / `false`; `$true` / `$false` also work). Accepts the `IsSubscribed` property from `Get-PmNotificationSubscription`. Positional (position 2).

```yaml
Type: System.String
DefaultValue: ''
SupportsWildcards: false
Aliases:
- IsSubscribed
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: true
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues:
- 'true'
- 'false'
HelpMessage: ''
```

### -Path

The target Pm: drives (organizations). Defaults to the current drive.

```yaml
Type: System.String[]
DefaultValue: ''
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
  Position: Named
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -WhatIf

Runs the command in a mode that only reports what would happen without performing the actions.

```yaml
Type: System.Management.Automation.SwitchParameter
DefaultValue: ''
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
DefaultValue: ''
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

### System.String

You can pipe rows with Topic, Mode and IsSubscribed properties (e.g. from Get-PmNotificationSubscription).

## OUTPUTS

### UiPath.PowerShell.Entities.PmNotificationSubscription

Returns the subscriptions that were set.

## NOTES

Notification subscriptions are per connected user. Mandatory topics cannot be turned off; the service rejects such changes. This is an Automation Cloud feature.

## RELATED LINKS

[Get-PmNotificationSubscription](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmNotificationSubscription.md)
