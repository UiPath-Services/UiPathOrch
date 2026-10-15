---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchQueueItem.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Import-OrchQueueItem
---

# Import-OrchQueueItem

## SYNOPSIS

Imports queue items from a CSV file into a queue.

## SYNTAX

### __AllParameterSets

```
Import-OrchQueueItem [-Path <string[]>] [-LiteralPath <string[]>] [-Name] <string[]> [-ImportCsv] <string[]>
 [[-CsvEncoding] <Encoding>] [[-CommitType] <string>] [-DynamicTyping] [-Confirm] [-WhatIf]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Imports queue items from one or more CSV files into UiPath Orchestrator queues. It accepts the same CSV files as the Orchestrator web **Upload Items** and creates the same items from them. The CSV file must follow the RFC 4180 format and supports multi-line field values and quoted fields. As on the web, the delimiter is detected from the first rows: comma, tab, `|` or `;`, whichever splits the rows into the most fields consistently (comma on a tie).

The CSV file must contain a header row. The following column headers have special meaning (case-sensitive, as on the web):

- **Priority** - Sets the queue item priority. Accepts text values (Low, Normal, High, in any case) or numeric values (1 = Low, 2 = Normal, 3 = High); surrounding spaces are ignored. An empty cell leaves the priority to the server.
- **Reference** - Sets the queue item reference string.

All other columns are treated as SpecificContent (custom data) fields and are added to each queue item as key-value pairs. Values are kept as written, including surrounding spaces and line breaks inside quoted fields. A repeated header is renamed as on the web: a second and third `Value` column become `Value_1` and `Value_2` (skipping names already used), and a repeated Reference or Priority becomes an ordinary field such as `Reference_1`.

As on the web, the whole file is rejected, with the row number of each problem, when a row has more or fewer fields than the header, a quote is malformed, a Priority value is invalid, or the file has more than 15,000 rows. Row numbers count the header as row 1; blank lines are not counted, and a row whose values contain line breaks counts as one row (the web numbers an invalid Priority from the first data row instead). One difference: a CSV with a single column (no comma anywhere) is imported, while the web rejects it with "Undetectable Delimiter".

By default, every SpecificContent value is a string. -DynamicTyping does what the web's **Dynamic typing** check box does: an empty cell becomes null; `true`, `false`, `TRUE` and `FALSE` become booleans; a decimal number (optional `-`, optional fraction and exponent, such as `007`, `1.50`, `.5`, `1e3`) becomes a number unless it is an integer beyond 2^53-1; and a date-time of the form `YYYY-MM-DDTHH:mm:ss[.fff]` followed by `Z` or `+HH:MM` becomes the UTC string `YYYY-MM-DDTHH:mm:ssZ`. A number or boolean in Reference is written the way the web writes it (`007` becomes `7`). Two web behaviors are not reproduced: the web turns an out-of-range date-time (month 13, day 32, hour 25) into `undefinedZ`, and a date-time in Reference into the browser's local date string; this cmdlet keeps both as written.

By default, the commit type is "ProcessAllIndependently", which imports each item independently so that individual failures do not affect other items. Use the -CommitType parameter to change this behavior to "AllOrNothing" if all items must succeed or fail together.

Items are sent 1,000 per request, with a progress bar for larger files; the web sends a whole file as one request, which can exceed the server's time limit for large files and leave part of the items created. "AllOrNothing" is the exception: it is sent as one request so that it stays all-or-nothing, and a warning is shown above 5,000 rows. "StopOnFirstFailure" stops sending after the request in which an item failed. If a request fails, or the import is cancelled with Ctrl+C between requests, the error names the CSV rows already sent, the rows that may be partly imported, and the rows not sent; items that failed in the requests already sent are output as FailedQueueItem first. The value of -CommitType is not case-sensitive.

This cmdlet supports ShouldProcess. Use -WhatIf to preview the import operation, or -Confirm to be prompted before importing.

The -Name, -ImportCsv, -CsvEncoding, -CommitType, and -Path parameters support tab completion. Press [Ctrl+Space] or [Tab] to see available values. The -Name completion is dynamically populated from actual queue names in the target folders.

Primary Endpoint: POST /odata/Queues/UiPathODataSvr.BulkAddQueueItems

OAuth required scopes: OR.Queues

Required permissions: Queues.Edit, Transactions.Create

## EXAMPLES

### Example 1: Import queue items from a CSV file

```powershell
PS Orch1:\Shared> Import-OrchQueueItem TestQueue2 C:\Data\items.csv
```

Imports queue items from the items.csv file into TestQueue2 in the current folder (Shared).

### Example 2: Import with a specific encoding

```powershell
PS Orch1:\Shared> Import-OrchQueueItem TestQueue2 C:\Data\items.csv UTF-32
```

Imports queue items from the items.csv file using UTF-32 encoding into TestQueue2.

### Example 3: Import with AllOrNothing commit type

```powershell
PS Orch1:\Shared> Import-OrchQueueItem TestQueue2 C:\Data\items.csv -CommitType AllOrNothing
```

Imports queue items from the items.csv file into TestQueue2 using the AllOrNothing commit type. If any item fails validation, the entire batch is rolled back.

### Example 4: Import with dynamic typing

```powershell
PS Orch1:\Shared> Import-OrchQueueItem TestQueue2 C:\Data\items.csv -DynamicTyping
```

Imports the items with numbers, booleans and date-times typed, as the web upload does with Dynamic typing checked. A cell `1.50` is stored as the number 1.5 instead of the string "1.50".

### Example 5: Preview import with -WhatIf

```powershell
PS C:\> Import-OrchQueueItem -Path Orch1:\Shared -Name TestQueue2 -ImportCsv C:\Data\records.csv -WhatIf
```

```output
What if: Performing the operation "Import QueueItem" on target "Queue: 'TestQueue2 [Shared]' File: 'C:\Data\records.csv'".
```

Shows what would happen without actually importing any items.

## PARAMETERS

### -Path

Specifies the target folder. If not specified, the current folder is targeted.

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

### -CommitType

Specifies how the bulk import operation handles failures. Tab completion suggests available values. The default value is "ProcessAllIndependently", which imports each item independently. Use "AllOrNothing" to roll back the entire batch if any item fails.

```yaml
Type: System.String
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 3
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -DynamicTyping

Converts SpecificContent values the way the web upload's Dynamic typing check box does: empty cells to null, `true` / `false` / `TRUE` / `FALSE` to booleans, decimal numbers to numbers, and ISO 8601 date-times with `Z` or `+HH:MM` to UTC strings. Without it, every value is a string. See the description for the exact rules.

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
  ValueFromPipelineByPropertyName: true
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

### -CsvEncoding

Specifies the character encoding of the CSV file. Defaults to UTF-8 if not specified. Tab completion suggests available encoding values.

```yaml
Type: System.Text.Encoding
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 2
  IsRequired: false
  ValueFromPipeline: false
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ImportCsv

Specifies the path to one or more CSV files to import. The CSV file must contain a header row with column names. Supports wildcards and multiple file paths.

```yaml
Type: System.String[]
DefaultValue: None
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

Specifies the names of the queues into which items are to be imported. Supports wildcards and multiple comma-separated values. Tab completion dynamically suggests queue names from the target folders.

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

### -WhatIf

Shows what would happen if the cmdlet runs. The cmdlet is not run.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]

You can pipe queue names to this cmdlet via the Name property, or CSV file paths via the ImportCsv property.

### System.Text.Encoding

You can pipe an encoding value to this cmdlet via the CsvEncoding property.

### System.String

You can pipe a commit type string to this cmdlet via the CommitType property.

## OUTPUTS

### UiPath.PowerShell.Entities.BulkOperationResponseDtoOfFailedQueueItem

On a successful bulk add the operation response is returned — check `.Success`. Per-row rejections are surfaced separately as FailedQueueItem (below).

### UiPath.PowerShell.Entities.FailedQueueItem

For each row the server rejects, a FailedQueueItem (stamped with QueueName and CsvPath, plus the server's reason) is returned, and an error record carries the failure message.

### UiPath.PowerShell.Commands.CSVParseError

When the CSV itself cannot be parsed (a malformed row, or an invalid Priority value), one CSVParseError per problem is returned and the import is not attempted for that file.

## NOTES

The CSV parser follows RFC 4180 and supports multi-line field values enclosed in double quotes and escaped double quotes within fields.

The CSV header row defines the structure of each queue item. The reserved column names "Priority" and "Reference" map to built-in queue item properties. All other columns become SpecificContent key-value pairs.

Priority values can be specified as text (Low, Normal, High) or as numeric values (1, 2, 3).

The default encoding is UTF-8. Use the -CsvEncoding parameter when the CSV file uses a different encoding such as UTF-32 or ASCII.

Queue items are folder-scoped entities. You must navigate to a folder on the Orch: drive or use -Path to specify the target folder.

## RELATED LINKS

[Get-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueueItem.md)

[Get-OrchQueue](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchQueue.md)

[Copy-OrchQueueItem](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchQueueItem.md)
