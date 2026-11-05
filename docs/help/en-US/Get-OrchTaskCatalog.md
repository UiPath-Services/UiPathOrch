---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Get-OrchTaskCatalog.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 03/06/2026
PlatyPS schema version: 2024-05-01
title: Get-OrchTaskCatalog
---

# Get-OrchTaskCatalog

## SYNOPSIS

Gets task catalogs from Orchestrator folders.

## SYNTAX

### __AllParameterSets

```
Get-OrchTaskCatalog [-Path <string[]>] [-LiteralPath <string[]>] [-Recurse]
 [-Depth <uint>] [[-Name] <string[]>] [-CsvEncoding <Encoding>] [-ExportCsv <string>]
 [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

Gets task catalog information from UiPath Orchestrator. Task catalogs define the available actions (tasks) that can be assigned to users for human-in-the-loop automation scenarios.

Formerly Get-OrchActionCatalog. The former name still works once the module is loaded, but tab completion does not offer it.

The -Name parameter supports tab completion. Press [Ctrl+Space] or [Tab] to see available task catalog names. Multiple values can be specified using comma-separated text that includes wildcards.

The cmdlet supports multi-threaded folder processing for improved performance when querying across multiple folders.

When specifying the -Path, -Recurse, and -Depth parameters, place them immediately after the cmdlet name. This placement ensures that autocomplete for subsequent parameters functions correctly.

Primary Endpoint: GET /odata/TaskCatalogs

OAuth required scopes: OR.Tasks or OR.Tasks.Read

Required permissions: TaskCatalogs.View

## EXAMPLES

### Example 1: Get all task catalogs in the current folder

```powershell
PS Orch1:\Shared> Get-OrchTaskCatalog
```

Gets all task catalogs from the current folder.

### Example 2: Get task catalogs by name

```powershell
PS Orch1:\Shared> Get-OrchTaskCatalog xxx*
```

Gets task catalogs whose names match `xxx*` from the current folder.

### Example 3: Get task catalogs recursively

```powershell
PS Orch1:\> Get-OrchTaskCatalog -Recurse
```

Gets all task catalogs from the current folder and all its subfolders.

### Example 4: Get task catalogs from a specific folder

```powershell
PS C:\> Get-OrchTaskCatalog -Path Orch1:\Production
```

Gets all task catalogs from the Production folder on Orch1.

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

Specifies the names of the task catalogs to retrieve. Supports wildcards. Tab completion dynamically suggests task catalog names from the target folders.

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

Exports task catalogs to the specified CSV file path. Columns: Path, Name, Description, Encrypted — matching New-OrchTaskCatalog parameter names so `Import-Csv | New-OrchTaskCatalog` round-trips. Requires a filesystem path (not an Orch: drive path).

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

You can pipe task catalog names to this cmdlet via the Name property.

## OUTPUTS

### UiPath.PowerShell.Entities.Bucket

Returns Bucket objects representing task catalogs with properties including Name and Id.

## NOTES

The cmdlet uses multi-threaded folder processing for improved performance when querying across multiple folders.

## RELATED LINKS

[Remove-OrchTaskCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Remove-OrchTaskCatalog.md)

[Copy-OrchTaskCatalog](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Copy-OrchTaskCatalog.md)
