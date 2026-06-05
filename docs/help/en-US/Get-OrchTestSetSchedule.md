---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTestSetSchedule.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTestSetSchedule
---

# Get-OrchTestSetSchedule

## SYNOPSIS

Gets test set schedules from Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchTestSetSchedule [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [-Depth <uint>] [[-Name] <string[]>] [-CsvEncoding <Encoding>] [-ExportCsv <string>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets test set schedule information from UiPath Orchestrator. Test set schedules define when and how test sets are automatically executed.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available test set schedule names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders. Personal folders are excluded from processing.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TestSetSchedules

OAuth required scopes: OR.TestSetSchedules or OR.TestSetSchedules.Read

Required permissions: TestSetExecutions.View

## EXAMPLES

### Example 1: Get all test set schedules in the current folder

```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule
```

Gets all test set schedules from the current folder.

### Example 2: Get test set schedules by name

```powershell
PS Orch1:\Shared> Get-OrchTestSetSchedule Sample*
```

Gets test set schedules whose names match 'Sample*' from the current folder.

### Example 3: Get test set schedules recursively

```powershell
PS Orch1:\> Get-OrchTestSetSchedule -Recurse
```

Gets all test set schedules from the current folder and all its subfolders.

### Example 4: Get test set schedules from a specific folder

```powershell
PS C:\> Get-OrchTestSetSchedule -Path Orch1:\Shared
```

Gets all test set schedules from the Shared folder on Orch1.

## PARAMETERS

### -Path

Specifies the target folders. If not specified, the current folder is targeted.

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

### -Depth

Specifies the depth for recursion into the target folders. A depth of 0 targets only the current folder with no subfolders included. When -Depth is specified, -Recurse is implied.

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

### -Name

Specifies the names of the test set schedules to retrieve. Supports wildcards. Tab completion dynamically suggests test set schedule names from the target folders.

```yaml
Type: System.String[]
DefaultValue: None
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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

### -CsvEncoding

Specifies the encoding for CSV export. Default is UTF-8 with BOM for Excel compatibility. Tab completion suggests all available system encodings (e.g., utf-8, shift_jis, us-ascii).

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

### -ExportCsv

Exports test set schedules to the specified CSV file path. Columns: Path, Name, Description, Enabled, TestSetName, CronExpression, TimeZoneId, CalendarName — matching New-/Update-OrchTestSetSchedule parameter names. TestSetName and CalendarName are resolved to human-readable names. Requires a filesystem path (not an Orch: drive path). (Note: schedule create/modify is tenant-restricted — see this cmdlet family's NOTES.)

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
  ValueFromPipelineByPropertyName: false
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: ''
```

## INPUTS

### System.String[]

You can pipe test set schedule names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.TestSetSchedule

Returns TestSetSchedule objects representing test set schedules with properties including Name, Id, and Enabled.

## NOTES

The cmdlet uses multi-threaded folder processing for improved performance when querying across multiple folders. Personal folders are excluded from processing.

## RELATED LINKS

[Remove-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTestSetSchedule.md)

[Copy-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTestSetSchedule.md)

[Enable-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Enable-OrchTestSetSchedule.md)

[Disable-OrchTestSetSchedule](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Disable-OrchTestSetSchedule.md)
