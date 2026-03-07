---
document type: cmdlet
external help file: UiPathOrch-Help.xml
HelpUri: ''
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestDataQueueItemTable
---

# Get-OrchTestDataQueueItemTable

## SYNOPSIS

Gets test data queue items and displays them in a formatted table.

## SYNTAX

### __AllParameterSets

```
Get-OrchTestDataQueueItemTable [[-Path] <string>] [-Recurse] [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets items from test data queues and displays them in a formatted table view. This function is a wrapper around Get-OrchTestDataQueueItem that expands the JSON content of each item into individual columns, making it easier to view structured test data.

The output is grouped by test data queue path. Each group displays the queue name followed by a table showing the Id, IsConsumed status, and all data columns parsed from the JSON content.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Display all test data queue items as a table

```powershell
PS Orch1:\root> Get-OrchTestDataQueueItemTable
```

Gets all test data queue items from the current folder and displays them in a formatted table grouped by queue.

### Example 2: Display test data queue items from a specific folder

```powershell
PS C:\> Get-OrchTestDataQueueItemTable Orch1:\root
```

Gets all test data queue items from the root folder on Orch1 and displays them in a formatted table.

### Example 3: Display test data queue items recursively

```powershell
PS Orch1:\> Get-OrchTestDataQueueItemTable -Recurse
```

Gets all test data queue items from the current folder and all its subfolders, displaying them in formatted tables grouped by queue.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: false
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -Recurse

Includes the target folder and all its subfolders in the operation.

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

You can pipe a folder path to this cmdlet.

## OUTPUTS

### None

This cmdlet outputs formatted table data to the console. Each test data queue is displayed with its items in a table that includes Id, IsConsumed, and all columns parsed from the JSON content.

## NOTES

This is a PowerShell function that wraps Get-OrchTestDataQueueItem and formats the output using Format-Table for improved readability.

## RELATED LINKS

Get-OrchTestDataQueueItem

Get-OrchTestDataQueue
