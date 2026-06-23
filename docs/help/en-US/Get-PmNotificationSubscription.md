---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-PmNotificationSubscription.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 06/02/2026
PlatyPS schema version: 2024-05-01
title: Get-PmNotificationSubscription
---

# Get-PmNotificationSubscription

## SYNOPSIS

Reads your own notification subscriptions (which events notify you, by mode).

## SYNTAX

### Default (Default)

```
Get-PmNotificationSubscription [-Path <string[]>] [-LiteralPath <string[]>]
 [[-Publisher] <string[]>] [[-Mode] <string[]>] [-CsvEncoding <Encoding>]
 [-ExportCsv <string>] [-IncludeHidden] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Lists the connected user's notification subscriptions from the notification service, flattened to one row per `(topic, mode)`. Each row carries the publisher (e.g. `Apps`, `Studio`), the topic group, the topic name/display name, the delivery `Mode` (`InApp` or `Email`), and whether you are subscribed.

The cmdlet always acts on the connected user's *own* subscriptions — the service takes no user id, so there is no `-UserName`. The output groups by publisher in the default view. Pipe it to `Set-PmNotificationSubscription` (the property names line up) to round-trip.

Primary Endpoint: GET /{partitionGlobalId}/notificationservice_/usersubscriptionservice/api/v1/UserSubscription

OAuth required scopes: (Notification Service API - no per-endpoint scopes)

## EXAMPLES

### Example 1: List all subscriptions

```powershell
PS Orch1:\> Get-PmNotificationSubscription
```

### Example 2: One publisher, email only

```powershell
PS Orch1:\> Get-PmNotificationSubscription Apps -Mode Email
```

Lists the `Apps` publisher's topics that have an Email delivery mode.

### Example 3: Find what you are unsubscribed from

```powershell
PS Orch1:\> Get-PmNotificationSubscription | Where-Object { -not $_.IsSubscribed }
```

### Example 4: Export to CSV, edit, and re-apply

```powershell
PS Orch1:\> Get-PmNotificationSubscription -ExportCsv C:\temp\notif.csv
# edit the IsSubscribed column in C:\temp\notif.csv, then:
PS Orch1:\> Import-Csv C:\temp\notif.csv | Set-PmNotificationSubscription
```

Exports the subscriptions and re-applies the (edited) `IsSubscribed` values. The CSV stores `true`/`false` as text, which `Set-PmNotificationSubscription` parses back.

## PARAMETERS

### -Publisher

Filter by publisher name or display name (e.g. `Apps`, `Studio`). Supports wildcards. Positional (position 0).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

### -Mode

Filter by delivery mode (`InApp`, `Email`). Positional (position 1).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 1
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -IncludeHidden

Include topics the portal marks as not visible. By default only visible topics are listed.

```yaml
Type: System.Management.Automation.SwitchParameter
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

### -ExportCsv

Writes the results to the given CSV file (columns `Path,Publisher,Group,Topic,DisplayName,Mode,IsSubscribed`) instead of to the pipeline, in a form `Import-Csv | Set-PmNotificationSubscription` accepts. `Path`, `Topic`, `Mode` and `IsSubscribed` are the columns `Set-PmNotificationSubscription` binds; the rest are context for editing.

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

### -CsvEncoding

The text encoding for the `-ExportCsv` file (e.g. `utf8`, `utf8BOM`, `unicode`). Defaults to the module's standard CSV encoding.

```yaml
Type: System.Text.Encoding
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

### System.String[]

You can pipe publisher names to this cmdlet via the Publisher property.

## OUTPUTS

### UiPath.PowerShell.Entities.PmNotificationSubscription

One object per (topic, mode), with Path, Publisher, Group, Topic, DisplayName, Category, Mode, IsSubscribed, IsMandatory and TopicId.

## NOTES

Notification subscriptions are per connected user (the service uses the token's user). This is an Automation Cloud feature.

## RELATED LINKS

[Set-PmNotificationSubscription](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Set-PmNotificationSubscription.md)
