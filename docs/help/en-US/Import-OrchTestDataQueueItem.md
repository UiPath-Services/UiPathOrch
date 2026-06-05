---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchTestDataQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/25/2026
PlatyPS schema version: 2024-05-01
title: Import-OrchTestDataQueueItem
---

# Import-OrchTestDataQueueItem

## SYNOPSIS

Bulk-adds items to a test data queue from a CSV file, using the same CSV format as the Orchestrator web "Upload Items" feature.

## SYNTAX

### __AllParameterSets

```
Import-OrchTestDataQueueItem [-Path <string[]>] [-LiteralPath <string[]>]
 [-Name] <string[]> [-ImportCsv] <string[]> [-Confirm] [-CsvEncoding <Encoding>] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Reads a CSV file and bulk-adds its rows as items to the named test data queue. The CSV format is identical to the one the Orchestrator web UI accepts when you choose **Upload Items** on a test data queue:

- The **header row** holds the queue's `ContentJsonSchema` property names.
- Each **subsequent row** is one queue item.

For example, a queue whose schema declares the properties `番号` (integer), `辺A`, `辺B`, `辺C`, `コメント`, and `期待値` (strings) accepts a CSV like:

```
番号,辺A,辺B,辺C,コメント,期待値
1,3,4,5,right triangle,直角三角形
2,1,1,1,equilateral,正三角形
```

Each cell is coerced to the JSON type the queue's schema declares — `integer`/`number`/`boolean` become JSON literals, everything else stays a string — and all rows for a queue are sent in a single bulk request. A queue created with the default schema (`{}`, any shape) treats every column as a string. Empty cells are omitted from the item.

Multiple CSV files and wildcard `-ImportCsv` paths are supported; every matched file is uploaded to every queue matched by `-Name`.

This cmdlet supports ShouldProcess. Use -WhatIf to preview or -Confirm to be prompted.

Primary Endpoint: POST /api/TestDataQueueActions/BulkAddItems

OAuth required scopes: OR.TestDataQueues or OR.TestDataQueues.Write

Required permissions: TestDataQueueItems.Create

## EXAMPLES

### Example 1: Upload items from a CSV

```powershell
PS Orch1:\root> Import-OrchTestDataQueueItem -Name "三角形のテストキュー" -ImportCsv triangles.csv
```

Adds every row of `triangles.csv` as an item to the queue "三角形のテストキュー" in the current folder, coercing `番号` to an integer per the queue's schema.

### Example 2: Upload to a specific folder

```powershell
PS C:\> Import-OrchTestDataQueueItem -Path Orch1:\QA -Name MyOrders -ImportCsv orders.csv
```

## PARAMETERS

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

### -CsvEncoding

Specifies the encoding used to read the CSV file. Defaults to UTF-8. Tab completion suggests available system encodings (e.g., utf-8, shift_jis, us-ascii).

```yaml
Type: System.Text.Encoding
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

### -ImportCsv

Path(s) to the CSV file(s) to upload. The header row must be the queue's schema property names. Supports wildcards and multiple paths. Requires a filesystem path (not an Orch: drive path).

```yaml
Type: System.String[]
DefaultValue: ''
SupportsWildcards: true
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

### -Name

Specifies the name(s) of the destination test data queue(s). Supports wildcards; the CSV is uploaded to every matching queue. Tab completion suggests test data queue names from the target folders.

```yaml
Type: System.String[]
DefaultValue: ''
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

Specifies the target folder(s). If not specified, the current folder is targeted. Supports wildcards.

```yaml
Type: System.String[]
DefaultValue: ''
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe queue names to this cmdlet via the Name property.

## OUTPUTS

### None

This cmdlet does not produce output on success. The server endpoint returns no body; use -Verbose to see per-queue item counts.

## NOTES

Main endpoint called: POST /api/TestDataQueueActions/BulkAddItems

The CSV format matches the Orchestrator web "Upload Items" dialog: the header row is the queue's ContentJsonSchema property names. Values are coerced to the schema-declared JSON type (integer/number/boolean), or kept as strings otherwise.

## RELATED LINKS

[Get-OrchTestDataQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueueItem.md)

[Reset-OrchTestDataQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Reset-OrchTestDataQueueItem.md)

[Get-OrchTestDataQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestDataQueue.md)

[Import-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchQueueItem.md)
