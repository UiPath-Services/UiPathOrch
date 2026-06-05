---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-PmNotificationSubscription.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/03/2026
PlatyPS schema version: 2024-05-01
title: Copy-PmNotificationSubscription
---

# Copy-PmNotificationSubscription

## SYNOPSIS

Copies your own notification subscriptions to yourself in another organization.

## SYNTAX

### Default (Default)

```
Copy-PmNotificationSubscription [-Path <string>] [-LiteralPath <string>]
 [-Destination] <string[]> [-Confirm] [-WhatIf] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Reads the connected user's notification subscriptions in the source organization and applies the same per-topic, per-mode state to the same person in each destination organization.

Topics are matched **by name** (e.g. `Apps.Shared`), because their GUIDs differ across organizations. A topic that doesn't exist in a destination, a topic the destination marks mandatory (its state can't be changed), and a mode a destination topic doesn't offer are all skipped. Copying within the same organization is a no-op.

Self-only: the notification service uses each drive's token user, so there is no `-UserName`. This is an Automation Cloud feature.

## EXAMPLES

### Example 1: Migrate your subscriptions to another org

```powershell
PS C:\> Copy-PmNotificationSubscription -Path Source: -Destination Dest:
```

### Example 2: Migrate to several orgs

```powershell
PS C:\> Copy-PmNotificationSubscription -Path Source: -Destination Dest1:,Dest2: -WhatIf
```

Shows what would be copied to both destinations without changing anything.

## PARAMETERS

### -Destination

The destination Pm: drive(s) (organizations) to copy the subscriptions into. Positional (position 0).

```yaml
Type: System.String[]
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

### -Path

The source Pm: drive (organization). Defaults to the current drive.

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
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -LiteralPath

Specifies the target folder or drive by literal path -- wildcard metacharacters (`[`, `]`, `*`, `?`) are treated as literal characters rather than patterns. Accepts the same drive-qualified paths as -Path. Its `PSPath` alias also binds the path of items piped from Get-ChildItem / Get-Item, so you can pipe folders directly. Use -LiteralPath instead of -Path when a folder name contains a wildcard metacharacter.

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

### System.String[]

You can pipe destination drive names to this cmdlet via the Destination property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmNotificationSubscription

Returns the subscriptions written to each destination.

## NOTES

Notification subscriptions are per connected user and per organization. Mandatory topics and topics/modes absent in a destination are skipped. This is an Automation Cloud feature.

## RELATED LINKS

[Get-PmNotificationSubscription](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmNotificationSubscription.md)

[Set-PmNotificationSubscription](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmNotificationSubscription.md)
